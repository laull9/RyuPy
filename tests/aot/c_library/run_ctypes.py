#!/usr/bin/env python3
"""随包 ctypes 复杂 ABI：显式 provider 声明模板，CPython/JIT/AOT 同程序与搬迁差分。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


def run(command, *, cwd, env=None, check=True):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                            capture_output=True, timeout=600)
    if check and result.returncode:
        raise AssertionError((command, result.returncode, result.stderr.decode(errors='replace')))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--provider', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    args = parser.parse_args()
    assert sys.platform in ('darwin', 'linux')
    cli = args.ryupy.resolve()
    fixture = Path(__file__).resolve().parent
    config_path = args.provider.resolve()
    template = json.loads(config_path.read_text())
    # 模板原有相对输入按其声明根解析后才搬到临时工作目录。
    for key in ('distribution_root', 'bridge_directory', 'shim'):
        if key in template:
            template[key] = str((config_path.parent / template[key]).resolve())
    dotnet = shutil.which('dotnet')
    assert dotnet
    with tempfile.TemporaryDirectory(prefix='ryupy-ctypes-') as temporary:
        work = Path(temporary).resolve()
        source = work / '中文 & source'
        source.mkdir()
        app = source / 'ctypes_app.py'
        for name in ('ctypes_app.py', 'ffi_demo.py'):
            shutil.copyfile(fixture / name, source / name)
        library = source / ('complex.dylib' if sys.platform == 'darwin' else 'complex.so')
        run(['cc', '-Wall', '-Wextra', '-Werror', '-fPIC',
             '-dynamiclib' if sys.platform == 'darwin' else '-shared', fixture / 'complex.c',
             '-o', library], cwd=work)
        reference = run([args.python, '-B', app], cwd=source)
        baseline = (reference.returncode, reference.stdout, reference.stderr)
        template['modules'] = sorted(set(template['modules'] + ['ffi_demo', 'ctypes']))
        template['payloads'] = [{'source': str(source / 'ffi_demo.py'), 'destination': 'ffi_demo.py'},
                                {'source': str(library), 'destination': library.name}]
        config = work / 'provider.json'
        config.write_text(json.dumps(template))
        moved = []
        for aot in (False, True):
            output = work / ('build-aot' if aot else 'build-jit')
            result = run([cli, '--cpython-provider', config, 'build', app,
                          *(['--aot'] if aot else []), '-o', output], cwd=work)
            generated = [Path(line[len('已生成 '):]) for line in result.stderr.decode().splitlines()
                         if line.startswith('已生成 ')]
            entry = next(path for path in generated if path.suffix != '.dll') if aot else generated[0]
            result = run([entry] if aot else [dotnet, entry], cwd=work)
            assert (result.returncode, result.stdout, result.stderr) == baseline, (aot, result, baseline)
            destination = work / ('moved-aot' if aot else 'moved-jit')
            shutil.move(entry.parent, destination)
            moved.append((aot, destination / entry.name))
        shutil.rmtree(source)
        for name in ('build-jit', 'build-aot'):
            if (work / name).exists():
                shutil.rmtree(work / name)
        unrelated = work / 'unrelated'
        unrelated.mkdir()
        empty_path = work / 'empty-path'
        empty_path.mkdir()
        env = {key: value for key, value in os.environ.items()
               if not key.startswith(('PYTHON', 'DYLD_', 'LD_'))}
        env['PATH'] = str(empty_path)
        for aot, entry in moved:
            result = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env)
            assert (result.returncode, result.stdout, result.stderr) == baseline, (aot, result, baseline)
            print('ctypes', 'AOT' if aot else 'JIT', '结构/所有权/编译回调/异常/搬迁通过', flush=True)
            (entry.parent / 'python/lib/python3.12/site-packages' / library.name).unlink()
            failure = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env, check=False)
            assert failure.returncode != 0 and library.name.encode() in failure.stderr, failure.stderr
            print('ctypes', 'AOT' if aot else 'JIT', '缺C库负例通过', flush=True)


if __name__ == '__main__':
    main()
