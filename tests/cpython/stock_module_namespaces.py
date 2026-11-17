#!/usr/bin/env python3
"""通用权威模块命名空间产品门：多模块CIL、即时CP差分、JIT/AOT搬迁与来源/生命周期审计。"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

from support import ROOT, environment, audit_native_paths, inspect_host_report, standalone_layout, rid

CASES = ('eager', 'annotations', 'harbor_pair', 'frost_pair')


def audit_sources(bundle, count):
    report = json.loads((bundle / 'cpython-provider.json').read_text())
    assert report['execution'] == 'ryupy_compiled_entry'
    assert report['entry_provider'] == 'ryupy_compiled_source'
    assert not report['delegated_local_modules']
    plan = report['dependency_collection']['module_namespaces']
    assert plan['profile'] == 'experimental-stock-module-namespaces'
    rows = plan['modules']
    assert len(rows) == count
    assert all(row['compiled'] and row['execution'] == 'InstanceGlobals'
               and row['provider_namespace'] and row['reason'] == plan['profile'] for row in rows), rows
    return {'modules': rows, 'compiled_count': len(rows), 'delegated_count': 0}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, default=ROOT / 'target/debug/ryupy')
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--case', choices=CASES, action='append')
    parser.add_argument('--mode', choices=('jit', 'standalone', 'all'), default='all')
    args = parser.parse_args()
    args.cli, args.python = args.cli.resolve(), args.python.resolve()
    logs = Path.home() / '.cache/ryupy/validation/stock-module-namespaces'
    logs.mkdir(parents=True, exist_ok=True)
    logfile = logs / (datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ') + '.json')
    result = {'status': 'failed', 'steps': [], 'products': []}
    env = environment()
    dotnet = shutil.which('dotnet')
    assert dotnet
    print(logfile, flush=True)
    with tempfile.TemporaryDirectory(prefix='ryupy-module-namespaces-') as temporary:
        root = Path(temporary)
        def run(label, command, cwd, process_env=env, timeout=180):
            process = subprocess.run(list(map(str, command)), cwd=cwd, env=process_env,
                                     capture_output=True, timeout=timeout)
            row = {'label': label, 'argv': list(map(str, command)), 'exit': process.returncode,
                   'stdout': process.stdout.decode(errors='backslashreplace'),
                   'stderr': process.stderr.decode(errors='backslashreplace')}
            result['steps'].append(row)
            logfile.write_text(json.dumps(result, ensure_ascii=True, indent=2))
            print(label, process.returncode, flush=True)
            assert process.returncode == 0, row
            return process
        try:
            version = run('fixed-cpython', [args.python, '-I', '-c', 'import platform; print(platform.python_version())'], root).stdout.decode().strip()
            result['python_version'] = version
            assert version in ('3.12.13', '3.13.3'), version
            for case in args.case or CASES:
                fixture = ROOT / 'tests/cases/stock_module_namespaces' / case
                for mode in ('jit', 'standalone') if args.mode == 'all' else (args.mode,):
                    label = case + '-' + mode
                    work = root / label
                    work.mkdir()
                    inputs, output = work / 'private input', work / 'build output'
                    shutil.copytree(fixture, inputs, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
                    run(label + '-venv', ['uv', 'venv', '--python', args.python, inputs / '.venv'], work)
                    oracle = run(label + '-cpython', [args.python, '-B', inputs / 'entry.py'], work)
                    command = [args.cli, '--cpython-provider', 'auto', '--experimental-stock-module-namespaces',
                               'build', inputs / 'entry.py', '-o', output]
                    if mode == 'standalone': command.append('--aot')
                    built = run(label + '-build', command, work, timeout=600)
                    if mode == 'standalone':
                        layout = standalone_layout(built.stderr, output, 'entry', rid())
                        bundle, filename = layout.dist, layout.native.name
                    else:
                        bundle, filename = output, 'entry.dll'
                    source_record = audit_sources(bundle, len(list(fixture.glob('*.py'))))
                    moved = work / '已搬迁 产品'
                    shutil.move(bundle, moved)
                    shutil.rmtree(inputs)
                    if output.exists(): shutil.rmtree(output)
                    audit_native_paths(moved)
                    report = work / 'host.json'
                    process_env = environment()
                    process_env.update(PATH='', PYTHONHOME='/missing-home', PYTHONPATH='/missing-path',
                                       RYUPY_CPYTHON_REPORT=str(report))
                    actual = run(label + '-relocated', [dotnet, moved / filename] if mode == 'jit' else [moved / filename],
                                 work, process_env)
                    if report.exists():
                        observed = json.loads(report.read_text())
                        result['products'].append({'label': label, 'source': source_record, 'host': observed})
                    assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr), label
                    inspect_host_report(report, moved)
                    print('PASS', version, case, mode, flush=True)
                    shutil.rmtree(moved)
            result['status'] = 'passed'
        finally:
            result['temporary_products_cleaned'] = True
            logfile.write_text(json.dumps(result, ensure_ascii=True, indent=2))
    print(logfile, flush=True)


if __name__ == '__main__':
    main()
