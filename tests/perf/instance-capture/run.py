"""固定旧新CLI的实例捕获差分、PIC开关、搬迁与独立冷/暖性能和分配；不自动晋升。"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from benchlib.artifact import MANIFEST_NAME, verified_native
from benchlib.suite import Benchmark

BASELINE = '5d36db1e952f1db07aee6563d7a800cc1df94548'
CP_CAPTURE = b'old-get x 0\nset x 9\ncaptured 2 9\nhash-error\nunchanged hash failed 5 1\n'
OLD_CAPTURE = b'new-get x 0\nset x 101\ncaptured 2 101\nhash-error\nunchanged hash failed 5 1\n'
# 固定负例不授予一般失败豁免；基线和候选各后端仍须真实重现才能登记边界。
CP_MAPPING = b"boundary-result 8 ['keys', 'item:x', 'body']\n"
PRODUCT_MAPPING = b"boundary-error argument after ** must be a mapping, not 'Keywords' []\n"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def normalized(stdout, warm):
    """时间行不属于语义oracle，其余所有字节仍精确比较。"""
    lines = stdout.splitlines(keepends=True)
    times = []
    fixed = []
    for line in lines:
        if warm and line.startswith(b'WARM '):
            tag, batch, elapsed = line.split()
            assert int(batch) == len(times) + 2
            value = float(elapsed)
            assert 0 <= value < float('inf')
            times.append(value * 1000)
        else:
            fixed.append(line)
    if warm:
        assert len(times) == 4
    return b''.join(fixed), times


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['old', 'new', 'baseline', 'output']:
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--semantic-source', type=Path, action='append', default=[])
    args = parser.parse_args()
    args.output = args.output.resolve()
    assert not args.output.exists() and not args.output.is_relative_to(ROOT)
    args.output.mkdir(parents=True)
    assert sys.version_info[:3] == (3, 12, 13)
    assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=args.baseline, text=True).strip() == BASELINE
    assert subprocess.check_output(['dotnet', '--version'], cwd=ROOT, text=True).strip() == '10.0.403'
    env = os.environ.copy()
    for key in ['RYUPY_RUNTIME_DIR', 'RYUPY_STATS', 'RYUPY_PIC', 'RYUPY_RECORDS_GUARD_TRACE',
                'RYUPY_RECORDS_GUARD_TOKEN', 'DOTNET_TieredCompilation', 'COMPlus_TieredCompilation']:
        env.pop(key, None)
    report = {'ok': False, 'baseline': BASELINE,
              'candidate': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
              'cli_hashes': {'old': digest(args.old), 'new': digest(args.new)},
              'commands': [], 'semantics': [], 'performance': [], 'fixture_hashes': {},
              'scope': 'Linux shared runner; no cross-platform or promotion claim; process allocation is not per-call warm allocation'}
    raw = args.output / 'raw'
    raw.mkdir()
    products = args.output / 'products'
    products.mkdir()
    retained = args.output / 'retained'
    retained.mkdir()

    def run(label, command, extra=None, cwd=ROOT, timeout=1200, expected=None, warm=False):
        command = list(map(str, command))
        row = {'label': label, 'command': command, 'cwd': str(cwd), 'env': extra or {}}
        report['commands'].append(row)
        started = time.perf_counter()
        try:
            result = subprocess.run(command, cwd=cwd, env=env | (extra or {}), capture_output=True, timeout=timeout)
            row.update(exit=result.returncode, wall_ms=(time.perf_counter() - started) * 1000)
            (raw / (label + '.stdout')).write_bytes(result.stdout)
            (raw / (label + '.stderr')).write_bytes(result.stderr)
            assert result.returncode == 0, (label, result.returncode, result.stderr[-2000:])
            if expected is not None:
                actual, times = normalized(result.stdout, warm)
                assert (actual, result.stderr) == (expected, b''), (label, 'semantic mismatch')
                row['warm_ms'] = times
            return result, row
        except subprocess.TimeoutExpired as error:
            (raw / (label + '.stdout')).write_bytes(error.stdout or b'')
            (raw / (label + '.stderr')).write_bytes(error.stderr or b'')
            row['error'] = 'timeout'
            raise
        except Exception as error:
            row['error'] = f'{type(error).__name__}: {error}'
            raise

    def build(label, cli, source, backend):
        directory = products / label
        directory.mkdir()
        inputs = directory / 'input'
        inputs.mkdir()
        copied = inputs / source.name
        shutil.copyfile(source, copied)
        output = directory / 'build'
        command = [cli, 'build', copied, '--native', '-o', output]
        if backend == 'aot':
            command.append('--aot')
        result, _ = run(label + '-build', command)
        if backend == 'aot':
            entry = verified_native(output, Benchmark(source.stem, copied), result.stderr, mode='standalone')
            manifest = json.loads((entry.parent / MANIFEST_NAME).read_text())
            assert manifest['toolchain']['sdk_version'] == '10.0.403'
            (raw / (label + '-manifest.json')).write_text(json.dumps(manifest, indent=2))
            cmd = [entry]
        else:
            entry = output / (source.stem + '.dll')
            assert entry.is_file()
            cmd = [env['DOTNET_HOST_PATH'], entry]
        (raw / (label + '-entry.json')).write_text(json.dumps({'path': str(entry), 'sha256': digest(entry)}))
        return directory, inputs, entry, cmd

    try:
        fixtures = [('capture', Path(__file__).with_name('capture.py')),
                    ('mapping-boundary', Path(__file__).with_name('custom_mapping_boundary.py'))]
        fixtures += [('supported', source) for source in args.semantic_source]
        original = ROOT / 'tests/e2e/phase3/ordinary_instance_method_capture.py'
        report['original_pending'] = {'path': str(original), 'sha256': digest(original),
            'failure_run': 43827788852, 'status': 'original unchanged; not claimed fixed by adapted supported fixture'}
        for index, (kind, source) in enumerate(fixtures):
            source = source.resolve()
            report['fixture_hashes'][str(source)] = digest(source)
            cp, _ = run(f'sem-{index}-cp', [sys.executable, '-B', source])
            assert not cp.stderr
            if kind == 'capture':
                assert cp.stdout == CP_CAPTURE
            elif kind == 'mapping-boundary':
                assert cp.stdout == CP_MAPPING and CP_MAPPING != PRODUCT_MAPPING
            for arm, cli in [('old', args.old), ('new', args.new)]:
                # 原始捕获和独立mapping边界都实测旧新；支持域合同只验候选。
                if kind == 'supported' and arm == 'old':
                    continue
                expected = (PRODUCT_MAPPING if kind == 'mapping-boundary' else
                            OLD_CAPTURE if kind == 'capture' and arm == 'old' else cp.stdout)
                for backend in ['jit', 'aot']:
                    label = f'sem-{index}-{arm}-{backend}'
                    directory, inputs, entry, command = build(label, cli, source, backend)
                    for pic in ['1', '0']:
                        run(label + '-pic-' + pic, command, {'RYUPY_PIC': pic}, expected=expected)
                    if backend == 'aot':
                        moved = retained / ('中文 空格 ' + label)
                        shutil.copytree(entry.parent, moved)
                        # 隐藏本次实际输入与原发布目录；源码checkout仍可见，不冒称清洁机器。
                        inputs.rename(inputs.with_name('input.hidden'))
                        entry.parent.rename(entry.parent.with_name(entry.parent.name + '.hidden'))
                        for pic in ['1', '0']:
                            run(label + '-moved-pic-' + pic, [moved / entry.name],
                                {'RYUPY_PIC': pic, 'PATH': '/nonexistent'}, cwd=moved, expected=expected)
                    report['semantics'].append({'fixture': str(source), 'kind': kind, 'arm': arm, 'backend': backend,
                        'cp_equivalent': expected == cp.stdout,
                        'boundary': ('custom ** mapping rejected before keys callback; not fixed by instance capture'
                                     if kind == 'mapping-boundary' else None)})
                    shutil.rmtree(directory)

        # 四轮各自重建两臂，保留全部不利样本；构建、语义与统计不混入正式计时。
        cases = [('methods', Path(__file__).with_name('methods.py'), ['30000'], True),
                 ('list_ops', ROOT / 'tests/benchmarks/list_ops.py', [], False)]
        for round_, order in enumerate([['old', 'new'], ['new', 'old'], ['new', 'old'], ['old', 'new']]):
            for backend in ['jit', 'aot']:
                for name, source, arguments, warm in cases:
                    source = source.resolve()
                    report['fixture_hashes'][str(source)] = digest(source)
                    cp, _ = run(f'perf-{round_}-{backend}-{name}-cp', [sys.executable, '-B', source, *arguments])
                    expected, _ = normalized(cp.stdout, warm)
                    assert not cp.stderr
                    built = {}
                    for arm in order:
                        cli = args.old if arm == 'old' else args.new
                        label = f'perf-{round_}-{backend}-{name}-{arm}'
                        built[arm] = build(label, cli, source, backend)
                    for pic in ['1', '0']:
                        for sample in range(6):
                            sequence = order if sample % 2 == 0 else order[::-1]
                            for arm in sequence:
                                label = f'perf-{round_}-{backend}-{name}-{arm}-pic{pic}-{sample}'
                                _, row = run(label, built[arm][3] + arguments,
                                             {'RYUPY_PIC': pic}, expected=expected, warm=warm)
                                report['performance'].append({'round': round_, 'backend': backend, 'case': name, 'arm': arm,
                                    'pic': pic, 'sample': sample, 'wall_ms': row['wall_ms'], 'warm_ms': row['warm_ms']})
                        for arm in order:
                            label = f'perf-{round_}-{backend}-{name}-{arm}-pic{pic}-allocation'
                            path = raw / (label + '.json')
                            run(label, built[arm][3] + arguments,
                                {'RYUPY_PIC': pic, 'RYUPY_STATS': str(path)}, expected=expected, warm=warm)
                            stats = json.loads(path.read_text())
                            assert {'main_ms', 'alloc_bytes', 'gc0', 'gc1', 'gc2'} <= stats.keys()
                            assert stats['alloc_bytes'] >= 0
                    for arm in order:
                        shutil.rmtree(built[arm][0])
                    (args.output / 'result.json').write_text(json.dumps(report, indent=2))
        report['ok'] = True
    finally:
        (args.output / 'result.json').write_text(json.dumps(report, indent=2))
        # 仅保留语义搬迁发布物；性能原生二进制已经删除，不声称可离线复核其内容。
        (args.output / 'retention.json').write_text(json.dumps({
            'retained': 'semantic relocated standalone distributions and all logs/manifests',
            'not_retained': 'all product-trial/products directories excluded from upload, including partial failure builds; successful performance builds deleted; only logs/manifests/identity metadata remain',
            'promotion': 'none; review regression time and allocations manually before any acceptance'}, indent=2))


if __name__ == '__main__':
    main()
