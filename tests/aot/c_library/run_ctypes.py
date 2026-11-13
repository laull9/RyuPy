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


def build(cli, config, app, output, aot, work):
    result = run([cli, '--cpython-provider', config, 'build', app,
                  *(['--aot'] if aot else []), '-o', output], cwd=work)
    generated = [Path(line[len('已生成 '):]) for line in result.stderr.decode().splitlines()
                 if line.startswith('已生成 ')]
    return next(path for path in generated if path.suffix != '.dll') if aot else generated[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--provider', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--distribution-metadata', type=Path)
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
        # 基础发行私有复制，搬迁前删除输入；完整上游metadata和第三方许可证同时部署。
        distribution = work / 'distribution-input'
        shutil.copytree(template['distribution_root'], distribution,
                        ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
        if args.distribution_metadata:
            shutil.copyfile(args.distribution_metadata / 'PYTHON.json', distribution / 'PYTHON.json')
            shutil.copytree(args.distribution_metadata / 'licenses', distribution / 'licenses', dirs_exist_ok=True)
        template['distribution_root'] = str(distribution)
        template['source']['licenses'] = ['lib/python3.12/LICENSE.txt'] + [
            str(path.relative_to(distribution)) for path in sorted((distribution / 'licenses').glob('*.txt'))]
        source = work / '中文 & source'
        source.mkdir()
        app = source / 'ctypes_app.py'
        for name in ('ctypes_app.py', 'exception_limits.py', 'proxy_app.py', 'proxy_limits.py',
                     'set_limits.py', 'ffi_demo.py', 'first_exception.py', 'second_exception.py'):
            shutil.copyfile(fixture / name, source / name)
        library = source / ('complex.dylib' if sys.platform == 'darwin' else 'complex.so')
        run(['cc', '-Wall', '-Wextra', '-Werror', '-fPIC',
             '-dynamiclib' if sys.platform == 'darwin' else '-shared', fixture / 'complex.c',
             '-o', library], cwd=work)
        reference = run([args.python, '-B', app], cwd=source)
        baseline = (reference.returncode, reference.stdout, reference.stderr)
        template['modules'] = sorted(set(template['modules'] + ['ffi_demo', 'ctypes', 'urllib', 'sys', 'types']))
        template['payloads'] = [{'source': str(source / 'ffi_demo.py'), 'destination': 'ffi_demo.py'},
                                {'source': str(library), 'destination': library.name}]
        config = work / 'provider.json'
        config.write_text(json.dumps(template))
        proxy_config = work / 'proxy-provider.json'
        proxy_config.write_text(json.dumps(dict(template, container_transfer='proxy')))
        proxy_reference = run([args.python, '-B', source / 'proxy_app.py'], cwd=source)
        proxy_baseline = (proxy_reference.returncode, proxy_reference.stdout, proxy_reference.stderr)
        moved = []
        build_roots = []
        for aot in (False, True):
            output = work / ('build-aot' if aot else 'build-jit')
            build_roots.append(output)
            entry = build(cli, config, app, output, aot, work)
            result = run([entry] if aot else [dotnet, entry], cwd=work)
            assert (result.returncode, result.stdout, result.stderr) == baseline, (aot, result, baseline)
            # 明确不支持的导出合同单独核诊断；正常程序继续按完整CPython三流比较。
            limits_output = work / ('limits-aot' if aot else 'limits-jit')
            build_roots.append(limits_output)
            limit_entry = build(cli, config, source / 'exception_limits.py', limits_output, aot, work)
            limit_result = run([limit_entry] if aot else [dotnet, limit_entry], cwd=work)
            assert not limit_result.stderr and '异常类型边界诊断通过' in limit_result.stdout.decode(), limit_result
            destination = work / ('moved-aot' if aot else 'moved-jit')
            shutil.move(entry.parent, destination)
            moved.append((aot, destination / entry.name, baseline, 'ctypes'))
            proxy_output = work / ('proxy-aot' if aot else 'proxy-jit')
            build_roots.append(proxy_output)
            proxy_entry = build(cli, proxy_config, source / 'proxy_app.py', proxy_output, aot, work)
            result = run([proxy_entry] if aot else [dotnet, proxy_entry], cwd=work)
            assert (result.returncode, result.stdout, result.stderr) == proxy_baseline, (aot, result, proxy_baseline)
            proxy_limits = work / ('proxy-limits-aot' if aot else 'proxy-limits-jit')
            build_roots.append(proxy_limits)
            limit_entry = build(cli, proxy_config, source / 'proxy_limits.py', proxy_limits, aot, work)
            limit_result = run([limit_entry] if aot else [dotnet, limit_entry], cwd=work)
            assert not limit_result.stderr and '活代理比较边界诊断通过' in limit_result.stdout.decode(), limit_result
            destination = work / ('moved-proxy-aot' if aot else 'moved-proxy-jit')
            shutil.move(proxy_entry.parent, destination)
            moved.append((aot, destination / proxy_entry.name, proxy_baseline, 'proxy'))
        shutil.rmtree(source)
        shutil.rmtree(distribution)
        for directory in build_roots:
            if directory.exists():
                shutil.rmtree(directory)
        unrelated = work / 'unrelated'
        unrelated.mkdir()
        empty_path = work / 'empty-path'
        empty_path.mkdir()
        env = {key: value for key, value in os.environ.items()
               if not key.startswith(('PYTHON', 'DYLD_', 'LD_'))}
        env['PATH'] = str(empty_path)
        for aot, entry, baseline, label in moved:
            result = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env)
            assert (result.returncode, result.stdout, result.stderr) == baseline, (aot, result, baseline)
            print(label, 'AOT' if aot else 'JIT', '容器/结构/所有权/编译回调/异常/搬迁通过', flush=True)
            (entry.parent / 'python/lib/python3.12/site-packages' / library.name).unlink()
            failure = run([entry] if aot else [dotnet, entry], cwd=unrelated, env=env, check=False)
            assert failure.returncode != 0 and library.name.encode() in failure.stderr, failure.stderr
            print(label, 'AOT' if aot else 'JIT', '缺C库负例通过', flush=True)


if __name__ == '__main__':
    main()
