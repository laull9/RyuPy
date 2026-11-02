#!/usr/bin/env python3
"""固定四kernel+hello的JIT/禁tier/AOT小配对；只观测，不自动优化或追加样本。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import statistics
import tempfile
import time

from perf_runner import Runner, digest, write_json
import perf_schema


CASES = ('matmul', 'calls', 'dataclass', 'exceptions', 'hello')
ARMS = ('jit', 'no-tier', 'aot')


def executable(value):
    found = shutil.which(value)
    path = Path(found or value).resolve()
    if not path.is_file() or not os.access(path, os.X_OK):
        raise ValueError(f'找不到可执行文件: {value}')
    return str(path)


def environment():
    # 不继承调用者的JIT/GC/实验旗标；工具安装位置与依赖缓存仍沿宿主配置。
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('COMPlus_', 'RYUPY_'))
           and (not k.startswith('DOTNET_') or k in ('DOTNET_ROOT', 'DOTNET_ROOT_X64'))}
    env['PYTHONHASHSEED'] = '0'
    # SDK后台节点不得逃逸到测量阶段；所有应用build使用相同设置。
    env['MSBUILDDISABLENODEREUSE'] = '1'
    env['DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER'] = '1'
    env['UseSharedCompilation'] = 'false'
    return env


def published(stderr, output):
    prefix = '已生成 '
    paths = [Path(line[len(prefix):]) for line in stderr.decode('utf-8').splitlines()
             if line.startswith(prefix)]
    candidates = []
    for path in paths:
        if path.suffix == '.dll':
            continue
        absolute = path.resolve()
        absolute.relative_to(output.resolve())
        if path.is_symlink() or not absolute.is_file() or not os.access(absolute, os.X_OK):
            raise ValueError('原生产物不是本次普通可执行文件')
        with absolute.open('rb') as stream:
            if stream.read(4) != b'\x7fELF':
                raise ValueError('原生产物不是ELF')
        candidates.append(absolute)
    if len(candidates) != 1:
        raise ValueError('公开CLI未返回唯一原生产物')
    return candidates[0]


def files_manifest(paths, root):
    return [{'path': str(p.relative_to(root)), 'bytes': p.stat().st_size,
             'sha256': digest(p)} for p in sorted(paths) if p.is_file() and not p.is_symlink()]


def cleanup_build_caches(products, work):
    """所有计时终态后仅删本轮host/bin,obj；失败路径不调用。"""
    def total(root):
        return sum(p.stat().st_size for p in root.rglob('*')
                   if p.is_file() and not p.is_symlink())
    before = total(work)
    removed = []
    for _, native in products.values():
        host = native.parent.parent / 'host'
        host.resolve().relative_to(work.resolve())
        if host.is_symlink():
            raise ValueError('拒绝清理符号链接host')
        for name in ('bin', 'obj'):
            cache = host / name
            if cache.is_symlink():
                raise ValueError('拒绝清理符号链接构建缓存')
            if cache.exists():
                cache.resolve().relative_to(work.resolve())
                removed.append({'path': str(cache), 'bytes': total(cache)})
                shutil.rmtree(cache)
    return {'bytes_before': before, 'bytes_after': total(work), 'removed': removed}


def summaries(records):
    result = {}
    for name in CASES:
        result[name] = {}
        for phase in ('cold', 'hot'):
            if name == 'hello' and phase == 'hot':
                continue
            result[name][phase] = {}
            for arm in ARMS:
                rows = [r for r in records if (r['kernel'], r['phase'], r['arm']) == (name, phase, arm)]
                if len(rows) != 3:
                    raise ValueError('不完整三进程矩阵')
                report = {'wall_ms': [r['wall_ms'] for r in rows],
                          'main_ms': [r['stats']['main_ms'] for r in rows],
                          'process_alloc_bytes': [r['stats']['alloc_bytes'] for r in rows],
                          'maxrss_bytes': [r['maxrss_bytes'] for r in rows],
                          'gc': [{k: r['stats'][k] for k in ('gc0', 'gc1', 'gc2', 'gc_pause_ms')}
                                 for r in rows],
                          'jit_ms': None if arm == 'aot' else [r['stats']['jit_ms'] for r in rows],
                          'jit_methods': None if arm == 'aot' else [r['stats']['jit_methods'] for r in rows]}
                report['wall_median_ms'] = statistics.median(report['wall_ms'])
                if phase == 'hot':
                    report['process_kernel_medians_seconds'] = [statistics.median(r['samples']) for r in rows]
                    report['kernel_median_seconds'] = statistics.median(report['process_kernel_medians_seconds'])
                result[name][phase][arm] = report
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', '--output', dest='out', type=Path, required=True)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--fixtures', type=Path, default=Path(__file__).with_name('perf_fixtures'))
    parser.add_argument('--cli', '--ryupy', dest='cli', default='target/release/ryupy')
    parser.add_argument('--python', default='python3.12')
    parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
    parser.add_argument('--ir-only', action='store_true', help='仅固定CP与IR预审，无build或性能进程')
    args = parser.parse_args()
    if platform.system() != 'Linux' or platform.machine() != 'x86_64':
        raise ValueError('仅Linux x86_64真实宿主')
    root = args.root.resolve()
    cli, python, dotnet = map(executable, (args.cli, args.python, args.dotnet))
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='perf-pair-', dir=args.out.resolve()))
    start = time.monotonic()
    run = Runner(work / 'logs', start + 1800)
    env = environment()
    env['DOTNET'] = dotnet
    records, builds = [], []
    status = 'failed'
    failure = None
    metadata = {'source_scope': 'restored-tree-not-lost-M3.11', 'budget_seconds': 1800,
                'cold_processes': 45, 'hot_processes': 36, 'warmups': 3, 'samples': 7,
                'host': {'system': platform.platform(), 'machine': platform.machine()},
                'cli_sha256': digest(cli), 'jit_metrics_aot': 'N/A (raw retained)',
                'alloc_scope': 'whole process; hot includes initialization and all 10 invocations',
                'tier_scope': 'fixed warmup, not proof of steady Tier1',
                'cold_scope': 'fresh processes; OS file caches are not flushed and builds have warmed files',
                'il_build_comparison': 'hashes recorded only; J/T execute the exact AOT input DLL'}
    print(f'唯一配对证据目录：{work}', flush=True)
    try:
        version = run.run('python-version', [python, '-c',
                          "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"],
                          cwd=work, env=env, limit=120)
        if version['stdout'] != b'cpython\n3.12.13\n':
            raise ValueError('必须固定CPython3.12.13')
        for label, command in (('git-head', ['git', 'rev-parse', 'HEAD']),
                               ('git-status', ['git', 'status', '--porcelain']),
                               ('dotnet-info', [dotnet, '--info']),
                               ('cpu-info', [python, '-c', "from pathlib import Path; print(Path('/proc/cpuinfo').read_text())"])):
            item = run.run(label, command, cwd=root, env=env, limit=120)
            metadata[label] = item['stdout'].decode('utf-8')
        expected = json.loads((args.fixtures / 'expected.json').read_text())
        if expected['warmups'] != 3 or expected['samples'] != 7 or set(expected['kernels']) != set(CASES):
            raise ValueError('固定fixture清单不一致')
        source = work / 'source'
        source.mkdir()
        for name in CASES:
            path = args.fixtures / (name + '.py')
            if digest(path) != expected['kernels'][name]['source_sha256']:
                raise ValueError(f'{name}源码不匹配固定CP清单')
            shutil.copy2(path, source / path.name)
            checksum = expected['kernels'][name]['checksum']
            for mode in ('cold', 'hot') if name != 'hello' else ('cold',):
                item = run.run(f'python-{name}-{mode}', [python, '-B', str(source / path.name),
                               *(['hot'] if mode == 'hot' else [])], cwd=work, env=env, limit=120)
                if item['stderr']:
                    raise ValueError('固定CP基线出现stderr')
                perf_schema.output(item['stdout'], checksum, hot=mode == 'hot', hello=name == 'hello')
            for emit in ('types', 'mir', 'cil'):
                run.run(f'ir-{name}-{emit}', [cli, 'dump', str(source / path.name), '--emit', emit],
                        cwd=work, env=env, limit=180)
        shutil.copy2(args.fixtures / 'expected.json', work / 'expected.json')
        metadata['source_manifest'] = files_manifest(source.glob('*.py'), source)
        if args.ir_only:
            status = 'ir-ready-unreviewed'
            return
        # 只有通过源码/IR预审并冻结的fixture才能由协调者启动本分支。
        products = {}
        for name in CASES:
            src = source / (name + '.py')
            il_out, aot_out = work / ('il-' + name), work / ('aot-' + name)
            il = run.run('build-il-' + name, [cli, 'build', str(src), '--out-dir', str(il_out)],
                         cwd=work, env=env, limit=180)
            aot = run.run('build-aot-' + name, [cli, 'build', str(src), '--out-dir', str(aot_out), '--aot'],
                          cwd=work, env=env, limit=180)
            native = published(aot['stderr'], aot_out)
            dll = aot_out / (name + '.dll')
            # 不将普通build的不同产物混给JIT；三臂共享此次AOT输入CIL。
            products[name] = (dll, native)
            jit_files = [dll, aot_out / (name + '.runtimeconfig.json'), aot_out / 'RyuPy.Runtime.dll']
            if not all(path.is_file() for path in jit_files):
                raise ValueError('JIT部署文件不完整')
            record = {'kernel': name, 'il_build_ms': il['wall_ms'], 'aot_build_ms': aot['wall_ms'],
                      'jit_deploy_files': files_manifest(jit_files, aot_out),
                      'native_file_bytes': native.stat().st_size, 'native_sha256': digest(native),
                      'native_publish_files': files_manifest(native.parent.rglob('*'), native.parent),
                      'same_cil': str(dll), 'cil_sha256': digest(dll),
                      'il_only_cil_sha256': digest(il_out / (name + '.dll'))}
            builds.append(record)
            for log in ('publish.stdout.log', 'publish.stderr.log'):
                shutil.copy2(native.parent.parent / log, work / 'logs' / (name + '-' + log))
        # 一次固定矩阵；预热仅在hot同一进程内，不启动额外热身进程。
        for phase in ('cold', 'hot'):
            kernels = CASES if phase == 'cold' else CASES[:-1]
            for repeat in range(3):
                ordered = kernels[repeat:] + kernels[:repeat]
                arms = ARMS[repeat:] + ARMS[:repeat]
                for name in ordered:
                    dll, native = products[name]
                    for arm in arms:
                        arm_env = dict(env)
                        if arm == 'no-tier':
                            arm_env['DOTNET_TieredCompilation'] = '0'
                        cmd = [str(native)] if arm == 'aot' else [dotnet, str(dll)]
                        if phase == 'hot':
                            cmd.append('hot')
                        item = run.run(f'{phase}-{repeat}-{name}-{arm}', cmd, cwd=work,
                                       env=arm_env, limit=120, stats=True)
                        if item['stderr']:
                            raise ValueError('被测进程出现stderr')
                        samples = perf_schema.output(item['stdout'], expected['kernels'][name]['checksum'],
                                                     hot=phase == 'hot', hello=name == 'hello')
                        data = perf_schema.stats(Path(item['case']) / 'stats.json')
                        records.append({k: v for k, v in item.items() if k not in ('stdout', 'stderr')})
                        records[-1].update(kernel=name, phase=phase, repeat=repeat, arm=arm,
                                           stats=data, samples=samples)
                        write_json(work / 'records.json', records)
        if len(records) != 81:
            raise ValueError('固定81进程矩阵不完整')
        metadata['results'] = summaries(records)
        metadata['build_cache_cleanup'] = cleanup_build_caches(products, work)
        status = 'passed'
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
