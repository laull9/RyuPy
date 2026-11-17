#!/usr/bin/env python3
"""用固定 stock micro 自身头/共享库构建真实 C 嵌入探针；所有产物仓外清理。"""
import argparse
import json
from pathlib import Path
import platform
import subprocess
import tempfile

QUERY = """
import json, sys, sysconfig
print(json.dumps(dict(build=sys.version, version=list(sys.version_info[:3]), home=sys.base_prefix,
 include=sysconfig.get_path('include'), lib=sysconfig.get_config_var('LIBDIR'),
 library=sysconfig.get_config_var('LDLIBRARY'), implementation=sys.implementation.name,
 gil_disabled=bool(sysconfig.get_config_var('Py_GIL_DISABLED')))))
"""


def run(command, cwd, timeout=60):
    try:
        result = subprocess.run(command, cwd=cwd, capture_output=True, text=True, timeout=timeout)
    except subprocess.TimeoutExpired as error:
        raise RuntimeError(f"命令超时 {command}\nstdout={error.stdout!r}\nstderr={error.stderr!r}") from error
    if result.returncode or result.stderr:
        raise RuntimeError(f"命令失败 exit={result.returncode} {command}\n{result.stdout}\n{result.stderr}")
    print(f'CHECK exit={result.returncode} stderr-bytes={len(result.stderr.encode())} tool={Path(command[0]).name}')
    return result.stdout


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    args = parser.parse_args()
    if platform.system() != 'Darwin' or platform.machine() != 'arm64':
        parser.error('当前只验收 macOS arm64')
    source = Path(__file__).resolve().parent
    repo = source.parents[2]
    parent = Path(tempfile.gettempdir()).resolve()
    if parent == repo or repo in parent.parents:
        parent = repo.parent
    with tempfile.TemporaryDirectory(prefix='ryupy-thin-guard-', dir=parent) as temporary:
        output = Path(temporary)
        cfg = json.loads(run([str(args.python.resolve()), '-I', '-B', '-c', QUERY], output))
        if (cfg['version'] not in ([3, 12, 13], [3, 13, 3]) or cfg['gil_disabled']
                or cfg['implementation'] != 'cpython'):
            parser.error('要求固定常规 GIL stock CPython 3.12.13 / 3.13.3')
        library = Path(cfg['lib']) / cfg['library']
        executable = output / 'probe'
        run(['clang', '-std=c11', '-O2', '-Wall', '-Wextra', '-Werror',
             '-Wno-deprecated-declarations', f"-I{cfg['include']}", str(source / 'probe.c'),
             str(library), f"-Wl,-rpath,{cfg['lib']}", '-o', str(executable)], output)
        result = run([str(executable), cfg['home'], str(source / 'scenarios.py')], output)
        if 'RESULT failed=0 finalize=0 live=0' not in result:
            raise RuntimeError(result)
        print(f"BUILD {cfg['build']}")
        print(f"STOCK {cfg['version']} {platform.machine()} include={cfg['include']} library={library}")
        print(result, end='')
    print('CLEANUP 仓库外构建产物已清理')


if __name__ == '__main__':
    main()
