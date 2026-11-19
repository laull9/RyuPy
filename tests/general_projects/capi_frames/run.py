#!/usr/bin/env python3
"""固定CPython双扩展机械门：直接复用产品native header，产物全在仓外且保留真实三流日志。"""
import argparse
from datetime import datetime, timezone
from pathlib import Path
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    args = parser.parse_args()
    source = Path(__file__).resolve().parent
    root = source.parents[2]
    python = args.python.resolve()
    config = subprocess.check_output([str(python), '-I', '-c',
        'import sys;print(sys.base_prefix);print(".".join(map(str,sys.version_info[:2])))'], text=True).splitlines()
    home, series = Path(config[0]), config[1]
    logs = Path.home()/'.cache/ryupy/validation/capi-carrier'
    logs.mkdir(parents=True, exist_ok=True)
    path = logs/(datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')+'-'+series+'.log')
    print('LOG', path, flush=True)
    with tempfile.TemporaryDirectory(prefix='ryupy-carrier-', dir='/tmp') as tmp, path.open('x') as log:
        def run(command):
            log.write('COMMAND '+repr(command)+'\n'); log.flush()
            result = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            log.write(f'EXIT {result.returncode}\nSTDOUT\n{result.stdout}\nSTDERR\n{result.stderr}\n'); log.flush()
            if result.returncode:
                raise RuntimeError('机械门失败：'+str(path))
            print(result.stdout, end='', flush=True)
        for name in ('native_scope_probe', 'native_callback_probe', 'native_carrier_harness'):
            run(['clang', '-bundle', '-undefined', 'dynamic_lookup', '-O2', '-Wall', '-Wextra', '-Werror',
                 '-I'+str(home/'include'/f'python{series}'), '-I'+str(root/'runtime/RyuPy.CPython/native'),
                 str(source/(name+'.c')), '-o', str(Path(tmp)/(name+'.so'))])
        for script in ('baseline.py', 'mechanics.py'):
            file = str(source/script)
            run([str(python), '-I', '-B', '-c',
                 f'import sys;sys.path.insert(0,{tmp!r});__file__={file!r};exec(compile(open(__file__).read(),__file__,"exec"))'])


if __name__ == '__main__':
    main()
