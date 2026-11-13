#!/usr/bin/env python3
"""CP1/CP3 独立探针：当次 CPython oracle、编译入口和搬迁负例逐项执行。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys

from support import (ROOT, audit_native_paths, checked, declaration, environment, execute,
                     external_workspace, inspect_host_report, private_distribution,
                     require_python, rid, write_json)

sys.path.insert(0, str(ROOT / 'tests/aot'))
from aot_layout import reported_aot

MODULES = ['sys', 'base64', 'json', 'sqlite3', 'zlib', 'collections',
           'contextlib', 'bridge_fixture', 'atexit', 'numpy', 'math', 'urllib']


def matching(actual, expected, label):
    """stdout、stderr、退出码一起比较；路径和异常内容不作过滤。"""
    assert (actual.returncode, actual.stdout, actual.stderr) == (
        expected.returncode, expected.stdout, expected.stderr), (
            label, actual.returncode, actual.stdout, actual.stderr,
            expected.returncode, expected.stdout, expected.stderr)


def provider_report(directory, modules):
    plans = list(directory.rglob('*.source-plan.json'))
    assert len(plans) == 1, ('必须有唯一源码执行域报告', plans)
    report = json.loads(plans[0].read_text())
    assert report['modules'], '模块执行域报告为空'
    for name, value in report['modules'].items():
        if name.split('.')[0] in modules:
            assert value['provider'] == 'cpython', (name, value)


def negative_cases(executable, dist, cwd, env, run):
    """每个负例独立恢复变更；缺依赖必须失败，禁止静默寻找系统 Python。"""
    configs = list(dist.rglob('ryupy-cpython.json'))
    assert len(configs) == 1, configs
    config = configs[0]
    bad = json.loads(config.read_bytes())
    bad['python_version'] = '0.0.0'
    libraries = list(dist.rglob('libpython3.12.dylib')) + list(dist.rglob('libpython3.12.so*'))
    libraries += list(dist.rglob('python312.dll'))
    assert libraries, '没有随包 libpython'
    # 首个真实文件（其余项可为符号链接）移出整个分发根。
    library = next(path for path in libraries if not path.is_symlink())
    cases = [('negative-declaration-version', config, json.dumps(bad).encode(), [b'CPython', b'3.12.13']),
             ('negative-libpython', library, None, [library.name.encode()])]
    # 真实机器架构错误：改Mach-O头中的CPU类型，独立进程必须在装载阶段拒绝。
    if sys.platform == 'darwin':
        original_library = library.read_bytes()
        malformed = bytearray(original_library)
        assert malformed[:4] == b'\xcf\xfa\xed\xfe', '只接受本机64位小端Mach-O'
        cpu = int.from_bytes(malformed[4:8], 'little')
        malformed[4:8] = (0x01000007 if cpu == 0x0100000C else 0x0100000C).to_bytes(4, 'little')
        cases.append(('negative-machine-abi', library, bytes(malformed), [library.name.encode()]))
    encodings = list(dist.rglob('encodings/__init__.py'))
    assert len(encodings) == 1, encodings
    cases.append(('negative-startup', encodings[0].parent, None, [b'encodings']))
    failures = []
    for label, path, content, tokens in cases:
        hidden = cwd / (label + '.withheld')
        original = path.read_bytes() if content is not None else None
        negative_cwd = cwd / label
        negative_cwd.mkdir()
        try:
            if content is None:
                path.rename(hidden)
            else:
                path.write_bytes(content)
            result = run(label, [executable], cwd=negative_cwd, env=env, check=False)
            assert result.returncode != 0 and all(token in result.stderr for token in tokens), result.stderr
            if label == 'negative-machine-abi':
                assert any(word in result.stderr.lower() for word in (b'architecture', b'mach-o', b'load')), result.stderr
        except Exception as error:
            failures.append((label, str(error)))
        finally:
            if content is None:
                hidden.rename(path)
            else:
                path.write_bytes(original)
    assert not failures, failures


def numpy_negative(executable, dist, cwd, env, run):
    """真实NumPy核心扩展缺失必须由该次导入失败，禁止系统路径补包。"""
    extensions = list(dist.rglob('_multiarray_umath*.so')) + list(dist.rglob('_multiarray_umath*.pyd'))
    assert len(extensions) == 1, extensions
    path = extensions[0]
    hidden = cwd / (path.name + '.withheld')
    path.rename(hidden)
    try:
        result = run('negative-numpy-extension', [executable], cwd=cwd, env=env, check=False)
        assert result.returncode != 0 and b'ImportError' in result.stderr, result.stderr
        assert b'numpy' in result.stderr.lower(), result.stderr
    finally:
        hidden.rename(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--ryupy', type=Path)
    parser.add_argument('--distribution', type=Path)
    parser.add_argument('--numpy-root', type=Path)
    parser.add_argument('--out', type=Path)
    parser.add_argument('--keep', action='store_true', help='失败调试时显式保留仓库外临时目录')
    parser.add_argument('--mode', choices=['baseline', 'jit', 'aot', 'all'], default='baseline')
    parser.add_argument('--case', action='append', help='稳定用例名，不含.py；默认发现全部')
    args = parser.parse_args()
    fixture = Path(__file__).resolve().parent
    cases = sorted((fixture / 'cases').glob('*.py'))
    if args.case:
        known = {path.stem for path in cases}
        assert set(args.case) <= known, set(args.case) - known
        cases = [path for path in cases if path.stem in args.case]
    if args.mode != 'baseline':
        assert args.ryupy and args.distribution, '产品验收需要 --ryupy 与 --distribution'
        args.ryupy = args.ryupy.resolve()
    root = external_workspace('ryupy-cpython-', args.out)
    env = environment()
    records = []
    failures = []

    def run(label, command, *, cwd=root, env=env, check=True):
        result = execute(command, cwd=cwd, env=env, timeout=600)
        (root / (label + '.stdout')).write_bytes(result.stdout)
        (root / (label + '.stderr')).write_bytes(result.stderr)
        records.append({'step': label, 'exit': result.returncode})
        if check:
            checked(result, label)
        return result

    try:
        require_python(args.python, root, env)
        distribution = private_distribution(args.distribution, root) if args.distribution else None
        for case in cases:
            try:
                application_args = ['参数 中文 &'] if case.stem == 'provider_state' else []
                source = root / (case.stem + ' 中文 & source')
                source.mkdir()
                shutil.copy2(case, source / 'app.py')
                shutil.copytree(fixture / 'fixtures/bridge_fixture', source / 'bridge_fixture')
                baseline_cwd = root / (case.stem + '-cpython-cwd')
                baseline_cwd.mkdir()
                baseline_env = dict(env)
                if args.numpy_root:
                    shutil.copytree(args.numpy_root, source / 'numpy-input')
                    baseline_env['PYTHONPATH'] = str(source / 'numpy-input')
                expected = run(case.stem + '-cpython', [args.python, '-B', source / 'app.py'] + application_args,
                               cwd=baseline_cwd, env=baseline_env)
                if args.mode == 'baseline':
                    print(case.stem, 'CPython通过', flush=True)
                    continue
                payloads = [{'source': str(source / 'bridge_fixture'), 'destination': 'bridge_fixture'}]
                if args.numpy_root:
                    payloads.extend({'source': str(path), 'destination': path.name}
                                    for path in (source / 'numpy-input').iterdir())
                provider = source / 'provider.json'
                write_json(provider, declaration(distribution, payloads, MODULES))
                common = [args.ryupy, '--cpython-provider', provider, 'build', source / 'app.py']
                if args.mode in ('jit', 'all'):
                    jit = root / (case.stem + '-jit')
                    build = run(case.stem + '-jit-build', common + ['-o', jit])
                    dlls = [Path(line.removeprefix('已生成 ')) for line in build.stderr.decode().splitlines()
                            if line.startswith('已生成 ')]
                    assert len(dlls) == 1, dlls
                    jit_cwd = root / (case.stem + '-jit-cwd')
                    jit_cwd.mkdir()
                    jit_env = dict(env, RYUPY_CPYTHON_REPORT=str(jit_cwd / 'host.json'))
                    matching(run(case.stem + '-jit', [shutil.which('dotnet'), dlls[0]] + application_args, cwd=jit_cwd, env=jit_env), expected, case.stem)
                    provider_report(jit, MODULES)
                    inspect_host_report(jit_cwd / 'host.json', jit)
                if args.mode in ('aot', 'all'):
                    publish = root / (case.stem + '-publish')
                    build = run(case.stem + '-aot-build', common + ['--aot', '-o', publish])
                    layout = reported_aot(build.stderr, publish, 'app', rid())
                    native_cwd = root / (case.stem + '-native-cwd')
                    native_cwd.mkdir()
                    native_env = dict(env, RYUPY_CPYTHON_REPORT=str(native_cwd / 'host.json'))
                    matching(run(case.stem + '-native', [layout.native] + application_args, cwd=native_cwd, env=native_env), expected, case.stem)
                    inspect_host_report(native_cwd / 'host.json', layout.dist)
                    moved = root / (case.stem + ' 中文 & relocated')
                    shutil.move(layout.dist, moved)
                    shutil.rmtree(layout.build)
                    shutil.rmtree(source)
                    hidden_distribution = root / 'distribution-hidden'
                    distribution.rename(hidden_distribution)
                    empty = root / (case.stem + '-empty-cwd')
                    empty.mkdir()
                    clean = environment()
                    clean['PATH'] = str(empty)
                    # 显式污染探测隔离配置是否忽略外部Python配置。
                    clean['PYTHONHOME'] = clean['PYTHONPATH'] = str(empty / '不存在')
                    clean['RYUPY_CPYTHON_REPORT'] = str(empty / 'host.json')
                    executable = moved / layout.native.name
                    try:
                        audit_native_paths(moved)
                        matching(run(case.stem + '-relocated', [executable] + application_args, cwd=empty, env=clean), expected, case.stem)
                        inspect_host_report(empty / 'host.json', moved)
                        if case.stem == 'host':
                            negative_cases(executable, moved, empty, clean, run)
                        if case.stem == 'numpy_arrays':
                            numpy_negative(executable, moved, empty, clean, run)
                    finally:
                        hidden_distribution.rename(distribution)
                print(case.stem, args.mode, '通过', flush=True)
            except Exception as error:
                failures.append((case.stem, str(error)))
                print(case.stem, '失败', str(error)[:6000], flush=True)
        write_json(root / 'result.json', {'steps': records, 'failures': failures, 'rid': rid()})
        assert not failures, [(case, message[:800]) for case, message in failures]
    finally:
        if args.keep:
            print('调试临时目录：', root)
        else:
            shutil.rmtree(root)


if __name__ == '__main__':
    main()
