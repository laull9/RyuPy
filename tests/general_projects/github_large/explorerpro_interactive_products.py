#!/usr/bin/env python3
"""准备可由CUA真实操控的Cocoa交互产品；标准.app身份、原生主进程、删除输入搬迁，UI动作另由CUA执行。

此驱动只准备产物，不合成鼠标、按键或Qt信号。launch使用exec保留LaunchServices启动PID；
oracle使用固定原venv，产品完整闭包放Contents/MacOS，供实际NSBundle与辅助功能识别。
"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import plistlib
import shlex
import shutil
import subprocess
import tempfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]


def app_bundle(path, identifier, engine, command, environment, evidence):
    macos = path / 'Contents/MacOS'
    macos.mkdir(parents=True, exist_ok=True)
    binary = 'dotnet' if engine.name == 'dotnet' else 'engine'
    shutil.copy2(engine, macos / binary)
    if binary == 'dotnet':
        # JIT仍消费已安装的官方.NET共享运行时；AOT/onefile完全不加这些外部入口。
        for name in ('host', 'shared'):
            (macos / name).symlink_to(engine.parent / name, target_is_directory=True)
    info = {'CFBundleIdentifier': identifier, 'CFBundleName': path.stem,
            'CFBundleDisplayName': path.stem, 'CFBundleExecutable': 'launch',
            'CFBundlePackageType': 'APPL', 'CFBundleVersion': '1', 'NSHighResolutionCapable': True,
            'NSPrincipalClass': 'NSApplication'}
    (path / 'Contents/Info.plist').write_bytes(plistlib.dumps(info))
    lines = ['#!/bin/sh', 'ryupy_appdir=${0%/*}']
    lines += ['export ' + key + '=' + shlex.quote(str(value)) for key, value in environment.items()]
    lines += ['exec "$ryupy_appdir/' + binary + '" ' + command + ' >' + shlex.quote(str(evidence / 'stdout.log'))
              + ' 2>' + shlex.quote(str(evidence / 'stderr.log'))]
    launch = macos / 'launch'
    launch.write_text('\n'.join(lines) + '\n')
    launch.chmod(0o755)
    return macos


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--mode', choices=('jit', 'standalone', 'onefile'), default='jit')
    args = parser.parse_args()
    python = args.python.absolute()
    venv = python.parent.parent
    assert (venv / 'pyvenv.cfg').is_file()
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    evidence = Path.home() / '.cache/ryupy/validation/explorerpro-interactive' / (stamp + '-' + args.mode)
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    clean = {key: value for key, value in os.environ.items()
             if not key.startswith(('PYTHON', 'RYUPY_', 'QT_', 'LD_', 'DYLD_', 'VIRTUAL_ENV', 'CONDA'))}
    oracle = evidence / 'oracle'
    product = evidence / 'product'
    for directory in (oracle, product):
        directory.mkdir()
        (directory / 'profile').mkdir()
    version = subprocess.check_output([python, '-I', '-c', 'import platform; print(platform.python_version())']).decode().strip()
    assert version == '3.12.13', version
    subprocess.run(['uv', 'pip', 'freeze', '--python', python], stdout=(evidence / 'freeze.txt').open('w'), check=True)
    base_home = python.resolve().parent.parent
    probe = HERE / 'explorerpro_interactive_smoke.py'
    oracle_app = oracle / 'RyuPy Cocoa Oracle.app'
    macos = app_bundle(oracle_app, 'dev.ryupy.validation.oracle.' + stamp.lower(), python.resolve(),
                       '"$ryupy_appdir/probe.py" ' + shlex.quote(str(oracle / 'profile')) + ' ' + shlex.quote(str(oracle)),
                       {'PYTHONHOME': base_home, 'PYTHONPATH': venv / 'lib/python3.12/site-packages',
                        'PYTHONDONTWRITEBYTECODE': '1', 'QT_QPA_PLATFORM': 'cocoa', 'HOME': oracle / 'profile'}, oracle)
    shutil.copy2(probe, macos / 'probe.py')
    # 官方发行的可执行文件使用../lib/libpython；仅oracle保原固定解释器基础库。
    (oracle_app / 'Contents/lib').symlink_to(base_home / 'lib', target_is_directory=True)
    with tempfile.TemporaryDirectory(prefix='ryupy-interactive-build-') as temporary:
        work = Path(temporary)
        inputs = work / 'inputs'
        inputs.mkdir()
        shutil.copy2(probe, inputs / 'entry.py')
        shutil.copytree(venv, inputs / '.venv', symlinks=True, ignore=shutil.ignore_patterns('__pycache__'))
        output = work / 'output'
        command = [str(args.ryupy.resolve()), str(inputs / 'entry.py'), '--mode', args.mode, '-o', str(output),
                   '--experimental-stock-module-namespaces', '--cpython-native-components',
                   str(HERE / 'explorerpro-native-selection.json')]
        environment = dict(clean, VIRTUAL_ENV=str(inputs / '.venv'), PYTHONDONTWRITEBYTECODE='1')
        (evidence / 'build-command.json').write_text(json.dumps(command, ensure_ascii=False))
        result = subprocess.run(command, cwd=work, env=environment, capture_output=True, timeout=900)
        (evidence / 'build-stdout.log').write_bytes(result.stdout)
        (evidence / 'build-stderr.log').write_bytes(result.stderr)
        assert result.returncode == 0, result.stderr.decode(errors='replace')
        generated = [Path(line[4:]) for line in result.stderr.decode().splitlines() if line.startswith('已生成 ')]
        assert len(generated) == 1
        executable = generated[0]
        report_path = next((path for path in output.rglob('cpython-provider.json') if path.parent.name.endswith('.dist')),
                           next(output.rglob('cpython-provider.json')))
        report = json.loads(report_path.read_text())
        assert report['entry_provider'] == 'ryupy_compiled_source' and not report['delegated_local_modules']
        shutil.copy2(report_path, evidence / 'cpython-provider.json')
        app = product / ('RyuPy Cocoa ' + args.mode + '.app')
        macos = app / 'Contents/MacOS'
        macos.mkdir(parents=True)
        if args.mode == 'onefile':
            shutil.copy2(executable, macos / executable.name)
        else:
            shutil.copytree(executable.parent, macos, dirs_exist_ok=True)
        flags = {'PATH': '', 'PYTHONHOME': '/missing-home', 'PYTHONPATH': '/missing-site',
                 'QT_QPA_PLATFORM': 'cocoa', 'HOME': product / 'profile',
                 'RYUPY_CPYTHON_REPORT': product / 'host.json'}
        engine = Path(shutil.which('dotnet')).resolve() if args.mode == 'jit' else macos / executable.name
        if args.mode == 'jit':
            flags['DOTNET_ROOT'] = engine.parent
        command = ('"$ryupy_appdir/' + executable.name + '" ' if args.mode == 'jit' else '')
        command += shlex.quote(str(product / 'profile')) + ' ' + shlex.quote(str(product))
        app_bundle(app, 'dev.ryupy.validation.product.' + stamp.lower(), engine, command, flags, product)
        shutil.rmtree(inputs)
        shutil.rmtree(output)
    prepared = {'mode': args.mode, 'oracle_app': str(oracle_app), 'product_app': str(app),
                'deleted_inputs': True, 'ui_driver': 'cua_repl', 'python_version': version,
                'oracle': str(oracle), 'product': str(product)}
    (evidence / 'prepared.json').write_text(json.dumps(prepared, ensure_ascii=False, indent=2))
    print(json.dumps(prepared, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
