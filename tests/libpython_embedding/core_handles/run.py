#!/usr/bin/env python3
"""仓库外构建 C shim + 独立 C# JIT/NativeAOT；固定 stock micro，失败也清理。

仅功能验证，无计时或产品执行覆盖率主张；不引用或构建产品 Runtime。
"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import tempfile

QUERY = """
import json, sys, sysconfig
print(json.dumps(dict(version=list(sys.version_info[:3]), home=sys.base_prefix,
 include=sysconfig.get_path('include'), lib=sysconfig.get_config_var('LIBDIR'),
 library=sysconfig.get_config_var('LDLIBRARY'),
 gil_disabled=bool(sysconfig.get_config_var('Py_GIL_DISABLED')))))
"""


def run(command, cwd, env=None, timeout=240, strict_stderr=False):
    result = subprocess.run(command, cwd=cwd, env=env, capture_output=True,
                            text=True, timeout=timeout)
    if result.returncode or strict_stderr and result.stderr:
        raise RuntimeError(f"命令失败 {command}\n{result.stdout}\n{result.stderr}")
    return result.stdout


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    args = parser.parse_args()
    if platform.system() != 'Darwin' or platform.machine() != 'arm64':
        parser.error('当前驱动仅验收 macOS arm64')
    source = Path(__file__).resolve().parent
    repo = source.parents[2]
    parent = Path(tempfile.gettempdir()).resolve()
    if parent == repo or repo in parent.parents:
        parent = repo.parent
    with tempfile.TemporaryDirectory(prefix='ryupy-core-handles-', dir=parent) as tmp:
        output = Path(tmp)
        cfg = json.loads(run([str(args.python.resolve()), '-I', '-B', '-c', QUERY], output))
        if cfg['version'] not in ([3, 12, 13], [3, 13, 3]) or cfg['gil_disabled']:
            parser.error('只开放固定常规 GIL stock 3.12.13 / 3.13.3')
        for name in ('CoreHandles.csproj', 'Native.cs', 'Program.cs'):
            shutil.copyfile(source / name, output / name)
        library = Path(cfg['lib']) / cfg['library']
        run(['clang', '-dynamiclib', '-std=c11', '-O2', '-Wall', '-Wextra',
             '-Wno-deprecated-declarations', '-Werror', f"-I{cfg['include']}", str(source / 'bridge.c'),
             str(library), f"-Wl,-rpath,{cfg['lib']}",
             '-o', str(output / 'libcore_handles.dylib')], output)
        run(['dotnet', 'build', 'CoreHandles.csproj', '-c', 'Release',
             '-o', str(output / 'jit')], output)
        env = os.environ.copy()
        env['DYLD_LIBRARY_PATH'] = str(output)
        for mode in ('jit', 'aot'):
            if mode == 'aot':
                run(['dotnet', 'publish', 'CoreHandles.csproj', '-c', 'Release',
                     '-r', 'osx-arm64', '-p:PublishAot=true', '-o', str(output / 'aot')], output)
            command = ([str(output / 'aot/CoreHandles')] if mode == 'aot' else
                       ['dotnet', str(output / 'jit/CoreHandles.dll')])
            result = run(command + [cfg['home'], str(source / 'scenarios.py'), mode], output, env,
                         strict_stderr=True)
            if 'RESULT cycles=12 functions=48' not in result:
                raise RuntimeError(result)
            print(f"STOCK {cfg['version']} {platform.machine()} {mode}\n{result}", flush=True)
    print('CLEANUP 仓库外构建产物已清理')


if __name__ == '__main__':
    main()
