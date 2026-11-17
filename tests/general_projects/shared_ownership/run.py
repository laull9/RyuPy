#!/usr/bin/env python3
"""通用共享归属真实产品门：两独立发行组、三形态、metadata/许可/资源/原生与删除输入搬迁。"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

from fixtures import prepare, wheel

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tests/cpython'))
from support import built_product, product_layout, provider_data


def invoke(command, cwd, env, log, success=True):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                            capture_output=True, timeout=900)
    log.write_bytes(result.stdout + result.stderr)
    if success and result.returncode:
        raise AssertionError((command, result.returncode, result.stderr.decode()))
    return result


def negatives(case, entry, venv, cli, work, env, evidence):
    """破坏私有安装副本；每臂恢复原输入，验证共享合并未放宽安装和边界检查。"""
    site = venv / 'lib/python3.12/site-packages'
    command = [cli, entry, '--mode', 'jit', '-o', work / 'negative-output']

    def rejected(name, expected):
        result = invoke(command, work, env, evidence / (case['id'] + '-' + name + '-negative.log'), False)
        assert result.returncode and expected.encode() in result.stderr, result.stderr

    record = site / (case['owners'][0] + '-1.0.dist-info/RECORD')
    original = record.read_bytes()
    for name, extra, expected in [
        ('duplicate', case['module'] + '/assets/data.body,,\n', 'RECORD包含重复'),
        ('traversal', '../escape_shared.txt,,\n', 'RECORD路径越界'),
    ]:
        try:
            record.write_bytes(original + extra.encode())
            rejected(name, expected)
        finally:
            record.write_bytes(original)
    resource = site / case['module'] / 'assets/data.body'
    content = resource.read_bytes()
    try:
        resource.unlink()
        rejected('missing-source', 'RECORD文件缺失')
    finally:
        resource.write_bytes(content)
    info = site / (case['owners'][1] + '-1.0.dist-info')
    hidden = work / ('hidden-' + case['id'])
    try:
        info.rename(hidden)
        rejected('missing-owner-metadata', '缺少传递依赖')
    finally:
        hidden.rename(info)
    if sys.platform != 'win32':
        alias = site / case['module'] / 'assets/data.bin'
        outside = work / 'outside-resource'
        outside.write_text('site外文件')
        try:
            alias.unlink()
            alias.symlink_to(outside)
            rejected('escaping-link', '符号链接越界')
        finally:
            alias.unlink()
            alias.symlink_to('data.body')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--mode', choices=['jit', 'standalone', 'onefile'], action='append')
    parser.add_argument('--expect-rejected', action='store_true', help='实现前红证：两组合成发行都被旧规则拒绝')
    parser.add_argument('--evidence', type=Path, default=Path.home() / '.cache/ryupy/validation/shared-ownership')
    args = parser.parse_args()
    evidence = args.evidence.expanduser().resolve() / datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    assert not evidence.is_relative_to(ROOT)
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_', 'VIRTUAL_ENV', 'CONDA'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0', TZ='UTC')
    results = []
    with tempfile.TemporaryDirectory(prefix='ryupy-shared-ownership-') as temporary:
        work = Path(temporary).resolve()
        assert not work.is_relative_to(ROOT)
        inputs = work / '构建 输入'
        inputs.mkdir()
        venv = inputs / 'venv'
        invoke(['uv', 'venv', '--python', args.python, venv], work, env, evidence / 'venv.log')
        python = venv / ('Scripts/python.exe' if sys.platform == 'win32' else 'bin/python')
        version = invoke([python, '-I', '-c', 'import platform; print(platform.python_version())'],
                         work, env, evidence / 'version.log')
        assert version.stdout.strip() == b'3.12.13', version.stdout
        cases = prepare(inputs, python, env)
        # 环境中的无关破损发行不得污染选中闭包：仅其RECORD内部重复，应用从未导入该根。
        unrelated = wheel(inputs, 'unrelated_drifter', {'unrelated_item.py': 'raise RuntimeError("不得导入")\n'})
        invoke(['uv', 'pip', 'install', '--python', python, '--no-deps', unrelated],
               work, env, evidence / 'unrelated-install.log')
        unrelated_record = venv / 'lib/python3.12/site-packages/unrelated_drifter-1.0.dist-info/RECORD'
        with unrelated_record.open('a') as stream:
            stream.write('unrelated_item.py,,\n')
        products = []
        for case, wheels, shared in cases:
            label = case['id']
            invoke(['uv', 'pip', 'install', '--python', python, '--no-deps', *wheels],
                   work, env, evidence / (label + '-install.log'))
            if sys.platform != 'win32':
                # 保留既有site内安全链接合同；真实产品必须把该共享资源物化为普通文件。
                site = venv / 'lib/python3.12/site-packages'
                resource = site / case['module'] / 'assets/data.bin'
                resource.unlink()
                resource.symlink_to('data.body')
            entry = inputs / (label + '.py')
            entry.write_text('"""手写入口由CIL编译，第三方包按通用安装闭包提供。"""\n'
                             'import ' + case['module'] + '\nprint(' + case['module'] + '.run())\n')
            oracle_cwd = work / ('oracle-' + label)
            oracle_cwd.mkdir()
            oracle = invoke([python, '-B', entry], oracle_cwd, env, evidence / (label + '-oracle.log'))
            if not args.expect_rejected:
                negatives(case, entry, venv, args.ryupy.resolve(), work,
                          dict(env, VIRTUAL_ENV=str(venv)), evidence)
                results.append({'case': label, 'status': 'five_installation_negatives_pass'})
            for mode in (['jit'] if args.expect_rejected else args.mode or ['jit', 'standalone', 'onefile']):
                name = label + '-' + mode
                output = work / ('build-' + name)
                built = invoke([args.ryupy.resolve(), entry, '--mode', mode, '-o', output],
                               work, dict(env, VIRTUAL_ENV=str(venv)), evidence / (name + '-build.log'),
                               success=not args.expect_rejected)
                if args.expect_rejected:
                    assert built.returncode and '多个发行拥有同一文件'.encode() in built.stderr, built.stderr
                    results.append({'case': name, 'status': 'old_duplicate_rule_reproduced'})
                    print(name, '旧重复归属规则红证通过', flush=True)
                    continue
                generated = [Path(line[len('已生成 '):]) for line in built.stderr.decode().splitlines()
                             if line.startswith('已生成 ')]
                assert len(generated) == 1, built.stderr
                executable = generated[0]
                # onefile的同一构建也生成完整目录发布，可在删除所有输入前审计其清单。
                data = provider_data(output, entry.stem, mode)
                report_path = data / 'cpython-provider.json'
                report = json.loads(report_path.read_text())
                if mode != 'onefile':
                    assert built_product(built.stderr, output, entry.stem, mode).entry == executable
                collection = report['dependency_collection']
                owners = {owner.replace('_', '-') for owner in case['owners']}
                distributions = {d['name']: d for d in collection['distributions']}
                assert set(distributions) == owners, set(distributions)
                for target in shared:
                    row = next(row for row in collection['shared_files'] if row['destination'] == target)
                    assert set(row['owners']) == owners, row
                    assert report['payloads'].count(target) == 1, (target, report['payloads'])
                    assert all(target in distributions[owner]['files'] for owner in owners)
                assert all(distributions[owner]['licenses'] for owner in owners)
                for target in shared:
                    staged = data / 'python/lib/python3.12/site-packages' / target
                    assert staged.is_file() and not staged.is_symlink(), staged
                shutil.copy2(report_path, evidence / (name + '-provider.json'))
                relocated = work / ('搬迁 空格 ' + name)
                relocated.mkdir()
                if mode == 'onefile':
                    shutil.copy2(executable, relocated / executable.name)
                else:
                    shutil.rmtree(relocated)
                    shutil.copytree(executable.parent, relocated)
                products.append((name, mode, relocated / executable.name, oracle, case))
                shutil.rmtree(output)
        shutil.rmtree(inputs)
        empty_path = work / 'empty-path'
        empty_path.mkdir()
        polluted = dict(env, PATH=str(empty_path), PYTHONHOME='/missing-python', PYTHONPATH='/missing-site')
        for name, mode, executable, oracle, case in products:
            cwd = work / ('run-' + name)
            cwd.mkdir()
            command = [executable]
            actual = invoke(command, cwd, polluted, evidence / (name + '-run.log'))
            assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr)
            if mode != 'onefile':
                package = product_layout(executable.parent).data / 'python/lib/python3.12/site-packages' / case['module']
                missing = next(package.glob('_pulse*.so')) if case['id'] == 'native' else package / 'assets/data.bin'
                content = missing.read_bytes()
                try:
                    missing.unlink()
                    failed = invoke(command, cwd, polluted, evidence / (name + '-runtime-negative.log'), False)
                    expected = b'_pulse' if case['id'] == 'native' else b'FileNotFoundError'
                    assert failed.returncode and expected in failed.stderr, failed.stderr
                finally:
                    missing.write_bytes(content)
            results.append({'case': name, 'status': 'deleted_inputs_relocated_pass',
                            'execution': 'cil_entry_cpython_dependency'})
            print(name, '删除输入搬迁通过', flush=True)
    (evidence / 'results.json').write_text(json.dumps(results, ensure_ascii=False, indent=2) + '\n')
    (evidence / 'cleanup.json').write_text(json.dumps({'temporary_tree_removed': not work.exists()}))


if __name__ == '__main__':
    main()
