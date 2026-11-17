#!/usr/bin/env python3
"""oct与mapping-only字典**的真实纯CIL发布：同次CP、两独立mapping、暂停与删除输入搬迁。"""
import argparse
from datetime import datetime, timezone
from pathlib import Path
import shutil
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tests/cpython'))
from support import environment, standalone_layout
from stock_thin_compiler import Recorder, require_success, rid
from onefile_support import audit_onefile_output

CASES = ('aot_oct_integer_protocol', 'aot_oct_rebinding',
         'aot_dict_unpack_mapping_order', 'aot_dict_unpack_protocol_pause')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--case', choices=CASES, action='append')
    parser.add_argument('--mode', choices=['jit', 'standalone', 'onefile'], action='append')
    args = parser.parse_args()
    logs = Path.home() / '.cache/ryupy/validation/container-protocols'
    logs.mkdir(parents=True, exist_ok=True)
    log = logs / (datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ') + '.log')
    print(log, flush=True)
    with log.open('x') as stream, tempfile.TemporaryDirectory(prefix='ryupy-container-products-') as temporary:
        recorder = Recorder(stream)
        work = Path(temporary).resolve()
        assert not work.is_relative_to(ROOT)
        for case in args.case or CASES:
            for mode in args.mode or ('jit', 'standalone'):
                root = work / (case + '-' + mode)
                source, output, relocated = root / '输入', root / 'out', root / '搬迁 产品'
                source.mkdir(parents=True)
                entry = source / 'entry.py'
                shutil.copy2(ROOT / 'tests/e2e/phase3' / (case + '.py'), entry)
                env = environment()
                oracle = require_success(recorder.run([args.python.resolve(), '-I', '-B', entry], root, env), 'oracle')
                command = [args.cli.resolve(), '--native', 'build', entry, '--out-dir', output]
                if mode == 'standalone': command.append('--aot')
                if mode == 'onefile': command.append('--onefile')
                built = require_success(recorder.run(command, root, env, 600), 'build')
                if mode == 'standalone':
                    layout = standalone_layout(built.stderr, output, 'entry', rid())
                    shutil.move(layout.dist, relocated)
                    executable = relocated / layout.native.name
                elif mode == 'onefile':
                    executable = audit_onefile_output(output, 'entry')
                    relocated.mkdir()
                    shutil.move(executable, relocated / executable.name)
                    executable = relocated / executable.name
                else:
                    shutil.move(output, relocated)
                    executable = relocated / 'entry.dll'
                shutil.rmtree(source)
                if output.exists(): shutil.rmtree(output)
                private = root / '临时解包'; private.mkdir()
                env.update(PATH='', PYTHONHOME='/missing-python', PYTHONPATH='/missing-site', TMPDIR=str(private))
                command = [shutil.which('dotnet'), executable] if mode == 'jit' else [executable]
                actual = recorder.run(command, root, env)
                assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr), (case, mode)
                assert not list(private.iterdir()), '退出后解包目录未清理'
                print('PASS', case, mode, flush=True)
        stream.write('CLEANED on context exit\n')


if __name__ == '__main__':
    main()
