#!/usr/bin/env python3
"""精确Unicode字面量实机门：普通CIL与provider桥独立JIT/AOT，删除输入后搬迁并比较三流。"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests/cpython'))
from support import (environment, standalone_layout, rid, audit_native_paths, inspect_host_report,
                     built_product, product_layout)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    args = parser.parse_args()
    args.cli, args.python = args.cli.resolve(), args.python.resolve()
    destination = Path.home() / '.cache/ryupy/validation/unicode-literals'
    destination.mkdir(parents=True, exist_ok=True)
    log = destination / (datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ') + '.json')
    record = {'status': 'failed', 'steps': []}
    env = environment()

    with tempfile.TemporaryDirectory(prefix='ryupy-unicode-') as temporary:
        work = Path(temporary)
        def run(label, command, *, cwd=work, process_env=env, timeout=120):
            result = subprocess.run(list(map(str, command)), cwd=cwd, env=process_env,
                                    capture_output=True, timeout=timeout)
            record['steps'].append({'label': label, 'argv': list(map(str, command)),
                'exit': result.returncode, 'stdout': result.stdout.decode(errors='backslashreplace'),
                'stderr': result.stderr.decode(errors='backslashreplace')})
            log.write_text(json.dumps(record, ensure_ascii=True, indent=2), encoding='utf-8')
            print(label, result.returncode, flush=True)
            assert result.returncode == 0, record['steps'][-1]
            return result
        try:
            for family, fixture in [('native', ROOT / 'tests/e2e/phase3/m3_8_surrogate_literals.py'),
                                    ('bridge', ROOT / 'tests/cases/stock_thin_compiler/unicode_bridge.py')]:
                for mode in ['jit', 'aot']:
                    label = family + '-' + mode
                    inputs = work / (label + '-inputs')
                    inputs.mkdir()
                    entry = inputs / 'entry.py'
                    shutil.copy2(fixture, entry)
                    if family == 'bridge':
                        run(label + '-venv', ['uv', 'venv', '--python', args.python, inputs / '.venv'])
                    oracle = run(label + '-cpython', [args.python, '-I', '-B', entry])
                    output = work / (label + '-output')
                    command = [args.cli, *(['--cpython-provider', 'auto'] if family == 'bridge' else []),
                               'build', entry, '-o', output, *(['--aot'] if mode == 'aot' else [])]
                    built = run(label + '-build', command, timeout=600)
                    if mode == 'aot':
                        layout = standalone_layout(built.stderr, output, 'entry', rid())
                        bundle, name = layout.dist, layout.native.name
                    else:
                        layout = built_product(built.stderr, output, 'entry', mode)
                        bundle, name = layout.root, layout.entry.name
                    moved = work / ('已搬迁 ' + label)
                    shutil.move(bundle, moved)
                    product_layout(moved)
                    shutil.rmtree(inputs)
                    if output.exists(): shutil.rmtree(output)
                    if family == 'bridge': audit_native_paths(moved)
                    run_env = environment()
                    report = work / (label + '-host.json')
                    run_env.update(PATH='', PYTHONHOME='/missing-python-home', PYTHONPATH='/missing-python-path',
                                   RYUPY_CPYTHON_REPORT=str(report))
                    actual = run(label + '-relocated', [moved / name],
                                 process_env=run_env)
                    assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr), label
                    if family == 'bridge':
                        record[label + '-host'] = inspect_host_report(report, moved)
                    shutil.rmtree(moved)
            record['status'] = 'passed'
        finally:
            record['temporary_products_cleaned'] = True
            log.write_text(json.dumps(record, ensure_ascii=True, indent=2), encoding='utf-8')
    print(log, flush=True)


if __name__ == '__main__':
    main()
