#!/usr/bin/env python3
"""通用权威模块命名空间产品门：多模块CIL、即时CP差分、JIT/AOT搬迁与来源/生命周期审计。"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

from support import (
    ROOT, audit_native_paths, built_product, environment, generated_entry,
    inspect_host_report, provider_data, rid, standalone_layout,
)

CASES = ('eager', 'annotations', 'harbor_pair', 'frost_pair', 'generators', 'courier_hooks', 'marsh_hooks', 'sunrise_context', 'quartz_context', 'inlet_handlers', 'quarry_handlers', 'synchronized_maps', 'deferred_callbacks')
FEATURE_CASES = ('asyncio_tasks',)


def audit_sources(report_path, count, version):
    report = json.loads(report_path.read_text())
    assert report['python_version'] == version
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
    parser.add_argument('--case', choices=CASES + FEATURE_CASES, action='append',
                        help='asyncio_tasks 需要 native-coroutines 构建的 CLI')
    parser.add_argument('--mode', choices=('jit', 'standalone', 'onefile', 'all'), default='all',
                        help='all 保持 JIT/目录 AOT 双形态；OneFile 可单独指定')
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
        root = Path(temporary).resolve()
        def run(label, command, cwd, process_env=env, timeout=180, check=True):
            process = subprocess.run(list(map(str, command)), cwd=cwd, env=process_env,
                                     capture_output=True, timeout=timeout)
            row = {'label': label, 'argv': list(map(str, command)), 'exit': process.returncode,
                   'stdout': process.stdout.decode(errors='backslashreplace'),
                   'stderr': process.stderr.decode(errors='backslashreplace')}
            result['steps'].append(row)
            logfile.write_text(json.dumps(result, ensure_ascii=True, indent=2))
            print(label, process.returncode, flush=True)
            if check: assert process.returncode == 0, row
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
                    if mode == 'onefile': command.append('--onefile')
                    built = run(label + '-build', command, work, timeout=600)
                    if mode == 'onefile':
                        executable = generated_entry(built.stderr)
                        bundle, filename = executable.parent, executable.name
                        assert bundle == output / 'entry.onefile' and list(bundle.iterdir()) == [executable]
                    elif mode == 'standalone':
                        layout = standalone_layout(built.stderr, output, 'entry', rid())
                        bundle, filename = layout.dist, layout.native.name
                    else:
                        layout = built_product(built.stderr, output, 'entry', 'jit')
                        bundle, filename = layout.root, layout.entry.name
                    payload = provider_data(output, 'entry', mode)
                    source_record = audit_sources(payload / 'cpython-provider.json',
                                                  len(list(fixture.rglob('*.py'))), version)
                    # OneFile 删除私有目录前审计载荷；运行时只允许从本臂独占的解包根加载。
                    if mode == 'onefile': audit_native_paths(payload)
                    moved = work / '已搬迁 产品'
                    shutil.move(bundle, moved)
                    shutil.rmtree(inputs)
                    if output.exists(): shutil.rmtree(output)
                    if mode != 'onefile': audit_native_paths(moved)
                    report = work / 'host.json'
                    process_env = environment()
                    process_env.update(PATH='', PYTHONHOME='/missing-home', PYTHONPATH='/missing-path',
                                       RYUPY_CPYTHON_REPORT=str(report))
                    runtime_root = moved
                    if mode == 'onefile':
                        runtime_root = work / 'private-extraction'
                        runtime_root.mkdir()
                        process_env.update(TMPDIR=str(runtime_root), TMP=str(runtime_root), TEMP=str(runtime_root))
                    actual = run(label + '-relocated', [moved / filename],
                                 work, process_env, check=False)
                    if report.exists():
                        observed = json.loads(report.read_text())
                        result['products'].append({'label': label, 'source': source_record, 'host': observed})
                    assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr), label
                    observed = inspect_host_report(report, runtime_root)
                    carriers = observed['call_carriers']
                    assert carriers['native_calls'] > 0, carriers
                    for key in ('native_live', 'managed_handles', 'code_origin_records', 'code_origin_active'):
                        assert carriers[key] == 0, (key, carriers)
                    print('PASS', version, case, mode, flush=True)
                    shutil.rmtree(moved)
            result['status'] = 'passed'
        finally:
            result['temporary_products_cleaned'] = True
            logfile.write_text(json.dumps(result, ensure_ascii=True, indent=2))
    print(logfile, flush=True)


if __name__ == '__main__':
    main()
