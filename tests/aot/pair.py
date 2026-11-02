#!/usr/bin/env python3
"""冻结双版本六臂180进程配对；复用既有采样器，只报告，不自动再测/启用优化。"""
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
import pair_schema as schema


def section(mir, name):
    return mir.split('fn ' + name + '(', 1)[1].split('\nfn ', 1)[0]


def validate_hit(raw, version):
    body = section(raw.decode('utf-8'), 'positional')
    expected = {'WrappingAdd': 2, 'is_boxed_i64(': 3, 'unbox_exact_i64(': 3,
                'box<I64>(': 1, 'BitXor': 4, 'BitAnd': 2, 'Ge(': 2}
    if version == 'C':
        for marker, count in expected.items():
            if body.count(marker) != count:
                raise ValueError(f'实际配对positional未命中 {marker} × {count}')
    elif 'WrappingAdd' in body:
        raise ValueError('固定baseline意外包含候选I64链')
    if body.count('dyn.Add(') != 2:
        raise ValueError('必须保原两步动态fallback')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for version in ('baseline', 'candidate'):
        parser.add_argument('--' + version + '-root', type=Path, required=True)
        parser.add_argument('--' + version + '-cli', required=True)
    parser.add_argument('--python', default='python3.12')
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    if platform.system() != 'Linux' or platform.machine() != 'x86_64':
        raise ValueError('只支持真实Linux x86_64配对')
    roots = {'B': args.baseline_root.resolve(), 'C': args.candidate_root.resolve()}
    if roots['B'] == roots['C']:
        raise ValueError('两版本必须独立checkout')
    cli = {'B': executable(args.baseline_cli), 'C': executable(args.candidate_cli)}
    for version in roots:
        Path(cli[version]).relative_to(roots[version])
    python = executable(args.python)
    dotnet = executable(os.environ.get('DOTNET', 'dotnet'))
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='i64-pair-', dir=args.out.resolve()))
    start = time.monotonic()
    run = Runner(work / 'logs', start + 1800)
    env = environment()
    env['DOTNET'] = dotnet
    records, builds, products = [], [], {}
    status, failure = 'failed', None
    metadata = {'budget_seconds': 1800, 'expected_processes': 180, 'expected_builds': 24,
                'expected_hot_samples': 1008, 'versions': {}, 'host': platform.platform(),
                'alloc_scope': 'whole process; guard contains all four scenarios, never split per label',
                'cold_scope': 'fresh process, warm OS file cache; no cache flushing',
                'tier_scope': '3warm7sample is not proof of steady Tier1',
                'aot_jit_metrics': 'N/A (raw retained)', 'code_size_review': 'pending-static-disassembly'}
    print(f'唯一固定配对证据目录：{work}', flush=True)
    try:
        checked = run.run('python-version', [python, '-c',
                          "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"],
                          cwd=work, env=env, limit=120)
        if checked['stdout'] != b'cpython\n3.12.13\n':
            raise ValueError('必须CPython3.12.13')
        for label, command in [('dotnet-info', [dotnet, '--info']),
                               ('cpu-info', [python, '-c', "from pathlib import Path; print(Path('/proc/cpuinfo').read_text())"])]:
            run.run(label, command, cwd=work, env=env, limit=120)
        runtimes, version_env = {}, {}
        for version, root in roots.items():
            head = run.run(version + '-head', ['git', 'rev-parse', 'HEAD'], cwd=root, env=env, limit=120)
            revision = head['stdout'].decode().strip()
            if version == 'B' and revision != schema.BASELINE:
                raise ValueError('不是事前冻结baseline SHA')
            dirty = run.run(version + '-status', ['git', 'status', '--porcelain'], cwd=root, env=env, limit=120)
            if dirty['stdout']:
                raise ValueError('配对源码必须先冻结提交，不能用dirty checkout测量')
            runtime = root / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
            runtimes[version] = digest(runtime)
            version_env[version] = {**env, 'RYUPY_RUNTIME_DIR': str(runtime.parent)}
            metadata['versions'][version] = {'root': str(root), 'revision': revision,
                'cli': cli[version], 'cli_sha256': digest(cli[version]),
                'runtime': str(runtime), 'runtime_sha256': runtimes[version]}
        fixture_dir = roots['C'] / 'tests/aot/perf_fixtures'
        expected_path = fixture_dir / 'expected.json'
        if digest(expected_path) != digest(roots['B'] / 'tests/aot/perf_fixtures/expected.json'):
            raise ValueError('原CP期望清单不得随候选更改')
        expected = json.loads(expected_path.read_text())
        if (set(expected['kernels']) != set(schema.KERNELS) or expected['warmups'] != 3
                or expected['samples'] != 7):
            raise ValueError('原五源码固定schema改变')
        source = work / 'source'
        source.mkdir()
        for name in schema.KERNELS:
            path = fixture_dir / (name + '.py')
            wanted = expected['kernels'][name]['source_sha256']
            if digest(path) != wanted or digest(roots['B'] / 'tests/aot/perf_fixtures' / path.name) != wanted:
                raise ValueError('原五源码不允许版本间或相对清单改变：' + name)
            shutil.copy2(path, source / path.name)
            for phase in (('cold',) if name == 'hello' else ('cold', 'hot')):
                item = run.run(f'python-{name}-{phase}', [python, '-B', str(source / path.name),
                               *(['hot'] if phase == 'hot' else [])], cwd=work, env=env, limit=120)
                if item['stderr']:
                    raise ValueError('CP基线stderr非空')
                perf_schema.output(item['stdout'], expected['kernels'][name]['checksum'],
                                   hot=phase == 'hot', hello=name == 'hello')
        guard_dir = Path(__file__).with_name('pair_fixtures')
        guard = guard_dir / 'guard.py'
        frozen_guard = json.loads((guard_dir / 'guard_expected.json').read_text())
        if digest(guard) != frozen_guard['sha256'] or frozen_guard['checksums'] != schema.GUARDS:
            raise ValueError('guard源码或原CP期望改变')
        shutil.copy2(guard, source / 'guard.py')
        item = run.run('python-guard', [python, '-B', str(source / 'guard.py')], cwd=work, env=env, limit=120)
        if item['stderr']:
            raise ValueError('guard CP基线stderr非空')
        schema.guard_output(item['stdout'])
        shutil.copy2(fixture_dir / 'expected.json', work / 'expected.json')
        shutil.copy2(guard_dir / 'guard_expected.json', work / 'guard_expected.json')
        metadata['source_manifest'] = files_manifest(source.glob('*.py'), source)
        # 先全部IR/构建，之后测量区禁止再启动任何build或诊断进程。
        for version in ('B', 'C'):
            for name in (*schema.KERNELS, 'guard'):
                src = source / (name + '.py')
                selected_env = version_env[version]
                for emit in ('types', 'mir', 'cil'):
                    dumped = run.run(f'ir-{version}-{name}-{emit}',
                                     [cli[version], 'dump', str(src), '--emit', emit],
                                     cwd=work, env=selected_env, limit=180)
                    if emit == 'mir' and name in ('calls', 'guard'):
                        validate_hit(dumped['stdout'], version)
                il_out, aot_out = work / f'il-{version}-{name}', work / f'aot-{version}-{name}'
                il = run.run(f'build-il-{version}-{name}', [cli[version], 'build', str(src), '--out-dir', str(il_out)],
                             cwd=work, env=selected_env, limit=180)
                aot = run.run(f'build-aot-{version}-{name}',
                              [cli[version], 'build', str(src), '--out-dir', str(aot_out), '--aot'],
                              cwd=work, env=selected_env, limit=180)
                native = published(aot['stderr'], aot_out)
                dll = aot_out / (name + '.dll')
                runtime = aot_out / 'RyuPy.Runtime.dll'
                if digest(runtime) != runtimes[version] or digest(il_out / runtime.name) != runtimes[version]:
                    raise ValueError('运行时版本串线')
                jit_files = [dll, aot_out / (name + '.runtimeconfig.json'), runtime]
                if not all(path.is_file() for path in jit_files):
                    raise ValueError('AOT输入CIL部署不完整')
                products[(version, name)] = (dll, native)
                builds.append({'version': version, 'kernel': name, 'il_build_ms': il['wall_ms'],
                    'aot_build_ms': aot['wall_ms'], 'cil_sha256': digest(dll),
                    'il_only_cil_sha256': digest(il_out / dll.name), 'same_cil': str(dll),
                    'jit_deploy_files': files_manifest(jit_files, aot_out),
                    'native_sha256': digest(native), 'native_file_bytes': native.stat().st_size,
                    'native_publish_files': files_manifest(native.parent.rglob('*'), native.parent)})
                for log in ('publish.stdout.log', 'publish.stderr.log'):
                    shutil.copy2(native.parent.parent / log, work / 'logs' / f'{version}-{name}-{log}')
                write_json(work / 'builds.json', builds)
        for phase, repeat, name, arm in schema.plan():
            version, backend = arm.split('-')
            dll, native = products[(version, name)]
            arm_env = dict(env)
            if backend == 'T':
                arm_env['DOTNET_TieredCompilation'] = '0'
            command = [str(native)] if backend == 'A' else [dotnet, str(dll)]
            if phase == 'hot':
                command.append('hot')
            item = run.run(f'{phase}-{repeat}-{name}-{arm}', command, cwd=work,
                           env=arm_env, limit=120, stats=True)
            if item['stderr']:
                raise ValueError('被测进程stderr非空')
            samples, guards = [], {}
            if phase == 'guard':
                guards = schema.guard_output(item['stdout'])
            else:
                samples = perf_schema.output(item['stdout'], expected['kernels'][name]['checksum'],
                                             hot=phase == 'hot', hello=name == 'hello')
            record = {k: v for k, v in item.items() if k not in ('stdout', 'stderr')}
            record.update(kernel=name, phase=phase, repeat=repeat, arm=arm, samples=samples,
                          guard_samples=guards, stats=perf_schema.stats(Path(item['case']) / 'stats.json'),
                          stdout_sha256=hashlib.sha256(item['stdout']).hexdigest(),
                          stderr_sha256=hashlib.sha256(item['stderr']).hexdigest())
            records.append(record)
            write_json(work / 'records.json', records)
        if len(builds) != 12 or sum(len(r['samples']) + sum(map(len, r['guard_samples'].values())) for r in records) != 1008:
            raise ValueError('固定24build/1008samples预算不完整')
        metadata['results'] = schema.report(records, builds)
        metadata['build_cache_cleanup'] = cleanup_build_caches(products, work)
        status = 'measured-not-approved'
    except BaseException as error:
        failure = f'{type(error).__name__}: {error}'
        raise
    finally:
        write_json(work / 'summary.json', {'status': status, 'failure': failure,
                   'elapsed_seconds': time.monotonic() - start, 'metadata': metadata,
                   'builds': builds, 'measured_processes': len(records)})
        write_json(work / 'records.json', records)


if __name__ == '__main__':
    main()
