#!/usr/bin/env python3
"""真实CPython精确标量接直接C ABI：13标量/void/buffer、拒绝协议、JIT/AOT和离线搬迁。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile

from run import declaration, run


def build(cli, provider, app, binding, output, aot):
    result = run([cli, '--cpython-provider', provider, 'build', app, '--native-binding', binding,
                  *(['--aot'] if aot else []), '-o', output])
    generated = [Path(line[len('已生成 '):]) for line in result.stderr.decode().splitlines()
                 if line.startswith('已生成 ')]
    return next(path for path in generated if path.suffix != '.dll') if aot else generated[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--provider', type=Path, required=True)
    parser.add_argument('--distribution-metadata', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    args = parser.parse_args()
    assert run([args.python, '-c', 'import platform; print(platform.python_version())']).stdout.strip() == b'3.12.13'
    cli, fixture = args.ryupy.resolve(), Path(__file__).resolve().parent
    config_path = args.provider.resolve()
    template = json.loads(config_path.read_text())
    for key in ('distribution_root', 'bridge_directory', 'shim'):
        if key in template:
            template[key] = str((config_path.parent / template[key]).resolve())
    dotnet = shutil.which('dotnet')
    assert dotnet and sys.platform in ('darwin', 'linux')
    mac = sys.platform == 'darwin'
    rid = 'osx-arm64' if mac else 'linux-x64'
    with tempfile.TemporaryDirectory(prefix='ryupy-exact-scalars-') as temporary:
        work = Path(temporary).resolve()
        source = work / '中文 空格 & 输入'
        source.mkdir()
        for name in ('scalar_app.py', 'scalar_limits.py', 'scalar_helper.py'):
            shutil.copyfile(fixture / name, source / name)
        library = source / ('scalars.dylib' if mac else 'scalars.so')
        run(['cc', '-Wall', '-Wextra', '-Werror', '-fPIC', '-dynamiclib' if mac else '-shared',
             fixture / 'scalars.c', '-o', library])
        exports = [(kind, 'echo_' + kind, [kind], kind) for kind in
                   ('i8', 'u8', 'i16', 'u16', 'i32', 'u32', 'i64', 'u64', 'isize', 'usize', 'f32', 'f64')]
        exports.extend([('boolean', 'echo_bool8', ['bool8'], 'bool8'),
                        ('utf8', 'measure_utf8', ['utf8'], 'u64'),
                        ('data', 'measure_data', ['bytes'], 'u64'),
                        ('touch', 'touch', ['i32'], 'void'), ('last', 'last_value', [], 'i32'),
                        ('calls', 'calls_count', [], 'i32')])
        binding = source / 'binding.json'
        binding.write_text(json.dumps(declaration(rid, library.name, exports)))
        reference = run([args.python, '-B', fixture / 'run.py', '--reference',
                         source / 'scalar_app.py', library, binding],
                        cwd=work, env=dict(os.environ, PYTHONPATH=str(source)))
        expected = (reference.returncode, reference.stdout, reference.stderr)
        distribution = work / 'distribution-input'
        shutil.copytree(template['distribution_root'], distribution,
                        ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
        shutil.copyfile(args.distribution_metadata / 'PYTHON.json', distribution / 'PYTHON.json')
        shutil.copytree(args.distribution_metadata / 'licenses', distribution / 'licenses', dirs_exist_ok=True)
        template['distribution_root'] = str(distribution)
        template['modules'] = ['json', 'scalar_helper']
        template['container_transfer'] = 'synchronized'
        template['payloads'] = [{'source': str(source / 'scalar_helper.py'), 'destination': 'scalar_helper.py'}]
        template['source']['licenses'] = ['lib/python3.12/LICENSE.txt'] + [
            str(path.relative_to(distribution)) for path in sorted((distribution / 'licenses').glob('*.txt'))]
        provider = work / 'provider.json'
        provider.write_text(json.dumps(template))
        moved, build_roots = [], []
        for aot in (False, True):
            for limits in (False, True):
                label = ('limits' if limits else 'scalars') + ('-aot' if aot else '-jit')
                output = work / ('build-' + label)
                build_roots.append(output)
                app = source / ('scalar_limits.py' if limits else 'scalar_app.py')
                entry = build(cli, provider, app, binding, output, aot)
                actual = run([entry] if aot else [dotnet, entry], cwd=work)
                if limits:
                    # CP ctypes接受部分转换协议；产品明确严格拒绝，按源码中的类型/副作用断言验收。
                    assert not actual.stderr and actual.stdout == '严格类型/范围/无转换hook/未进入C通过\n'.encode(), actual
                    comparison = (actual.returncode, actual.stdout, actual.stderr)
                else:
                    assert (actual.returncode, actual.stdout, actual.stderr) == expected, (actual, expected)
                    comparison = expected
                print(label, '原位类型/范围/完整三流通过', flush=True)
                destination = work / ('中文 空格 & 搬迁 ' + label)
                shutil.move(entry.parent, destination)
                moved.append((aot, limits, destination / entry.name, comparison))
        shutil.rmtree(source)
        shutil.rmtree(distribution)
        for directory in build_roots:
            if directory.exists():
                shutil.rmtree(directory)
        unrelated, empty_path = work / 'unrelated', work / 'empty-path'
        unrelated.mkdir()
        empty_path.mkdir()
        env = {key: value for key, value in os.environ.items()
               if not key.startswith(('PYTHON', 'RYUPY_', 'DYLD_', 'LD_'))}
        env['PATH'] = str(empty_path)
        for aot, limits, entry, comparison in moved:
            actual = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env)
            assert (actual.returncode, actual.stdout, actual.stderr) == comparison, actual
            print('AOT' if aot else 'JIT', '拒绝协议' if limits else '13标量/void/buffer', '删源离线搬迁通过', flush=True)
            if limits:
                continue
            native_path = entry.parent / '_internal/native/v1/b0' / ('library.dylib' if mac else 'library.so')
            native_path.unlink()
            failure = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env, check=False)
            assert failure.returncode != 0 and b'ImportError' in failure.stderr, failure.stderr
            assert str(native_path).encode() in failure.stderr, failure.stderr
            assert any(reason in failure.stderr for reason in
                       (b'Unable to load', b'dlopen', b'Could not find', b'No such file')), failure.stderr
            print('AOT' if aot else 'JIT', '缺库路径/根因通过', flush=True)


if __name__ == '__main__':
    main()
