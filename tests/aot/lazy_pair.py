#!/usr/bin/env python3
"""固定120新进程启动对照；复用阻塞wait4采样器，不改原81/180实验。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import tempfile
import time

from perf_runner import Runner, digest, write_json
from perf import executable, environment, published, files_manifest, cleanup_build_caches
import perf_schema
import lazy_schema as schema


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for version in ('baseline', 'candidate'):
        parser.add_argument('--' + version + '-root', type=Path, required=True)
        parser.add_argument('--' + version + '-cli', required=True)
    parser.add_argument('--python', default='python3.12')
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--started-at', type=float, required=True,
                        help='CI首CLI构建前的epoch秒；总1200s不可从此脚本重置')
    parser.add_argument('--fixtures', type=Path, required=True)
    parser.add_argument('--expected', type=Path, required=True)
    args = parser.parse_args()
    import math
    age = time.time() - args.started_at
    if not math.isfinite(age) or age < 0 or age >= 1200:
        raise ValueError('总预算起点非法或已耗尽')
    if platform.system() != 'Linux' or platform.machine() != 'x86_64':
        raise ValueError('首切仅实际Linux x86_64，不外推其他平台')
    roots = {'B': args.baseline_root.resolve(), 'C': args.candidate_root.resolve()}
    if roots['B'] == roots['C']:
        raise ValueError('版本checkout不能共享')
    cli = {'B': executable(args.baseline_cli), 'C': executable(args.candidate_cli)}
    for v in roots:
        Path(cli[v]).relative_to(roots[v])
    python, dotnet = executable(args.python), executable(os.environ.get('DOTNET', 'dotnet'))
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='lazy-pair-', dir=args.out.resolve()))
    begin = time.monotonic()
    run = Runner(work / 'logs', begin + 1200 - age)
    env = environment()
    env['DOTNET'] = dotnet
    records, builds, products = [], [], {}
    status, failure = 'failed', None
    metadata = {'budget_seconds': 1200, 'started_at': args.started_at,
                'expected_processes': 120, 'expected_app_builds': 16,
                'external_cli_builds': 2, 'versions': {}, 'host': platform.platform(),
                'cold_scope': 'new process; OS file cache warm from builds; zero warmup',
                'allocation_scope': 'whole process', 'aot_jit_metrics': None,
                'main_ms_scope': 'RunMain onward; excludes native registration',
                'scope': '13 fresh factories deferred; 16 including sys/static Lazy unchanged'}
    print('唯一启动配对证据目录：' + str(work), flush=True)
    try:
        item = run.run('python-version', [python, '-c',
            "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"],
            cwd=work, env=env, limit=30)
        if item['stdout'] != b'cpython\n3.12.13\n':
            raise ValueError('要求固定CP3.12.13')
        for label, command in [('dotnet-info', [dotnet, '--info']),
                ('cpu-info', [python, '-c', "from pathlib import Path; print(Path('/proc/cpuinfo').read_text())"])]:
            run.run(label, command, cwd=work, env=env, limit=30)
        version_env, runtime_hash = {}, {}
        for v, root in roots.items():
            head = run.run(v + '-head', ['git', 'rev-parse', 'HEAD'], cwd=root, env=env, limit=30)
            revision = head['stdout'].decode().strip()
            if v == 'B' and revision != schema.BASELINE:
                raise ValueError('固定baseline不符')
            dirty = run.run(v + '-status', ['git', 'status', '--porcelain'], cwd=root, env=env, limit=30)
            if dirty['stdout']:
                raise ValueError('不允许未冻结checkout')
            runtime = root / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
            runtime_hash[v] = digest(runtime)
            version_env[v] = {**env, 'RYUPY_RUNTIME_DIR': str(runtime.parent)}
            metadata['versions'][v] = {'revision': revision, 'root': str(root),
                'cli': cli[v], 'cli_sha256': digest(cli[v]), 'runtime_sha256': runtime_hash[v]}
        expected_path = args.expected
        expected = json.loads(expected_path.read_text(), object_pairs_hook=perf_schema.pairs,
                             parse_constant=perf_schema.constant)
        if set(expected['fixtures']) != set(schema.FIXTURES):
            raise ValueError('四固定负载名单不符')
        source = work / 'source'
        source.mkdir()
        shutil.copy2(expected_path, work / 'expected.json')
        for name in schema.FIXTURES:
            src = args.fixtures / (name + '.py')
            wanted = expected['fixtures'][name]
            if name == 'hello' and digest(src) != digest(roots['B'] / 'tests/aot/perf_fixtures/hello.py'):
                raise ValueError('hello必须与固定baseline源逐字相同')
            if digest(src) != wanted['source_sha256']:
                raise ValueError('冻结源码SHA不符：' + name)
            shutil.copy2(src, source / src.name)
            item = run.run('python-' + name, [python, '-B', str(source / src.name)], cwd=work, env=env, limit=30)
            if item['stderr']:
                raise ValueError('CP校准stderr非空')
            schema.output(item['stdout'], wanted['stdout'])
        metadata['source_manifest'] = files_manifest(source.glob('*.py'), source)
        # 所有构建先结束；测量阶段不启动SDK/IR等并行负载。
        for v in ('B', 'C'):
            for name in schema.FIXTURES:
                src = source / (name + '.py')
                il_out, aot_out = work / f'il-{v}-{name}', work / f'aot-{v}-{name}'
                il = run.run(f'build-il-{v}-{name}', [cli[v], 'build', str(src), '--out-dir', str(il_out)],
                             cwd=work, env=version_env[v], limit=180)
                aot = run.run(f'build-aot-{v}-{name}', [cli[v], 'build', str(src), '--out-dir', str(aot_out), '--aot'],
                              cwd=work, env=version_env[v], limit=180)
                native = published(aot['stderr'], aot_out)
                dll, runtime = aot_out / (name + '.dll'), aot_out / 'RyuPy.Runtime.dll'
                if digest(runtime) != runtime_hash[v] or digest(il_out / runtime.name) != runtime_hash[v]:
                    raise ValueError('runtime跨版本串线')
                jit_files = [dll, aot_out / (name + '.runtimeconfig.json'), runtime]
                if not all(p.is_file() for p in jit_files):
                    raise ValueError('同CIL部署缺失')
                products[(v, name)] = (dll, native)
                builds.append({'version': v, 'fixture': name, 'il_build_ms': il['wall_ms'],
                    'aot_build_ms': aot['wall_ms'], 'cil_sha256': digest(dll),
                    'il_only_cil_sha256': digest(il_out / dll.name), 'same_cil': str(dll),
                    'jit_deploy_files': files_manifest(jit_files, aot_out),
                    'native_sha256': digest(native), 'native_file_bytes': native.stat().st_size,
                    'native_publish_files': files_manifest(native.parent.rglob('*'), native.parent)})
                for log in ('publish.stdout.log', 'publish.stderr.log'):
                    shutil.copy2(native.parent.parent / log, work / 'logs' / f'{v}-{name}-{log}')
                write_json(work / 'builds.json', builds)
        for repeat, name, arm in schema.plan():
            v, backend = arm.split('-')
            dll, native = products[(v, name)]
            selected_env = dict(env)
            if backend == 'T':
                selected_env['DOTNET_TieredCompilation'] = '0'
            command = [str(native)] if backend == 'A' else [dotnet, str(dll)]
            item = run.run(f'cold-{repeat}-{name}-{arm}', command, cwd=work,
                           env=selected_env, limit=30, stats=True)
            if item['stderr']:
                raise ValueError('被测stderr非空')
            schema.output(item['stdout'], expected['fixtures'][name]['stdout'])
            record = {k: val for k, val in item.items() if k not in ('stdout', 'stderr')}
            record.update(fixture=name, repeat=repeat, arm=arm,
                stats=perf_schema.stats(Path(item['case']) / 'stats.json'),
                stdout_sha256=hashlib.sha256(item['stdout']).hexdigest(),
                stderr_sha256=hashlib.sha256(item['stderr']).hexdigest())
            records.append(record)
            write_json(work / 'records.json', records)
        metadata['results'] = schema.report(records, builds)
        if time.monotonic() >= run.deadline:
            raise TimeoutError('汇总阶段已耗尽总预算')
        metadata['build_cache_cleanup'] = cleanup_build_caches(products, work)
        if time.monotonic() >= run.deadline:
            raise TimeoutError('清理阶段已耗尽总预算')
        status = 'measured-not-approved'
    except BaseException as error:
        failure = f'{type(error).__name__}: {error}'
        raise
    finally:
        write_json(work / 'summary.json', {'status': status, 'failure': failure,
            'elapsed_in_tool_seconds': time.monotonic() - begin,
            'elapsed_total_seconds': time.time() - args.started_at,
            'metadata': metadata, 'builds': builds, 'measured_processes': len(records)})
        write_json(work / 'records.json', records)


if __name__ == '__main__':
    main()
