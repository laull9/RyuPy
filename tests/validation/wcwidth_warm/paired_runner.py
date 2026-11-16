"""同run同PE两Runtime严格NativeAOT：C3先行、A/A校准后平衡A/B；失败不补采。"""
import difflib
import json
import math
import os
from pathlib import Path
import shutil
import statistics as st
import sys
import tarfile
import time
import paired_inputs as inputs
import runner as w

HERE = Path(__file__).resolve().parent
ORDER = ('ABBA', 'BAAB') * 3


def analyze(blocks):
    w.require(len(blocks) == 6, '必须六完整块')
    ratios, by_order = [], {'ABBA': [], 'BAAB': []}
    for order, rows in zip(ORDER, blocks):
        w.require(len(rows) == 4 and [r['arm'] for r in rows] == list(order), '块次序不符')
        for r in rows:
            values = r['values']
            w.require(len(values) == 12 and all(math.isfinite(v) and v >= .1 for v in values), '批值非法')
            w.require(.95 <= st.median(values[6:]) / st.median(values[:6]) <= 1.05, '进程内漂移')
        medians = {arm: [st.median(r['values']) for r in rows if r['arm'] == arm] for arm in 'AB'}
        ratio = st.geometric_mean(medians['B']) / st.geometric_mean(medians['A'])
        ratios.append(ratio)
        by_order[order].append(ratio)
    # 同一arm跨块早晚、块内第一次/第二次位置门，不把真实A/B差异当噪声。
    for arm in 'AB':
        values = [st.median(r['values']) for block in blocks for r in block if r['arm'] == arm]
        w.require(.95 <= st.geometric_mean(values[6:]) / st.geometric_mean(values[:6]) <= 1.05, 'arm早晚漂移')
        positions = [st.median([r for r in block if r['arm'] == arm][1]['values']) /
                     st.median([r for r in block if r['arm'] == arm][0]['values']) for block in blocks]
        w.require(.95 <= st.geometric_mean(positions) <= 1.05 and all(.9 <= r <= 1.1 for r in positions), 'arm位置效应')
    aggregate = st.geometric_mean(ratios)
    strata = {k: st.geometric_mean(v) for k, v in by_order.items()}
    w.require(.95 <= strata['ABBA'] / strata['BAAB'] <= 1.05, '顺序层差异')
    return {'block_ratios_B_over_A': ratios, 'geometric_ratio': aggregate, 'order_strata': strata,
            'observed_window_reduction_at_least_5_percent':
                aggregate <= .95 and all(r < 1 for r in ratios) and all(r < 1 for r in strata.values()),
            'confidence_lower_bound': None, 'generalized_speedup': None}


def check_inputs(warm, alloc):
    for root in (warm, alloc):
        w.require(json.loads((root / 'RESULT.json').read_text())['passed'] is True, '上游未通过')
    w.require((alloc / 'before-identity.stdout').read_text().splitlines()[0] == os.environ['GITHUB_SHA'], 'allocation HEAD不同')
    w.require((alloc / 'old-commit.stdout').read_text().strip() == '6923573d78f7b504dcbe6d6067dd145cd1a7bd65', '旧源不同')
    w.require((alloc / 'old-tree.stdout').read_text().strip() == 'c27b271f3223e215a3c4fcc1618664ad3acf7968', '旧树不同')
    w.require((alloc / 'sdk.stdout').read_text().strip() == '10.0.403', '上游SDK不同')
    source = json.loads((alloc / 'source-identity.json').read_text())
    actual = {p for p in set(source['old']) | set(source['new']) if source['old'].get(p) != source['new'].get(p)}
    w.require(actual == set(source['changed']) == {'runtime/RyuPy.Runtime/' + n for n in ('PyHash.cs', 'PyLruKey.cs', 'PyLruCache.cs')}, '生产差异越界')
    for name, digest in source['new'].items():
        w.require(w.sha(w.ROOT / name) == digest, '新Runtime源码身份改变')
    w.require(w.sha(warm / 'warm-Runtime.dll') == w.sha(alloc / 'new/RyuPy.Runtime.dll'), '两job新Runtime非同字节')
    manifest = json.loads((warm / 'manifest.json').read_text())
    w.binding(manifest, warm / 'managed/kernel.dll', warm / 'warm-Runtime.dll')
    entry = json.loads((warm / 'entry.json').read_text())
    w.require(entry['sha256'] == w.sha(warm / 'managed/kernel.dll') and entry['method_name'] == 'Main'
              and entry['owner']['name'] == 'Module' and entry['owner']['namespace'] == 'RyuPy.Generated'
              and entry['signature_blob'] == '0001081d0e', 'PE静态入口不同')
    w.require((warm / 'C3.stdout').read_bytes() == (HERE / 'EXPECTED.txt').read_bytes(), '上游C3不同')
    w.require((warm / 'host/Program.cs').read_text().strip() ==
              'internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }', 'Host非原静态入口')


def run(out, downloads):
    out.mkdir(parents=True, exist_ok=False)
    state = {'passed': False, 'contracts': 0, 'aa': {}, 'blocks': [], 'retry': 0, 'publish_attempts': 0}
    frozen = None
    total_deadline = time.monotonic() + 1800
    def capture(label, argv, command_env, budget, cwd):
        remaining = total_deadline - time.monotonic()
        w.require(remaining > 0, 'controller总预算1800秒')
        result = w.capture(out, label, argv, command_env, min(budget, remaining), cwd)
        w.require(time.monotonic() <= total_deadline, 'controller总预算1800秒')
        return result
    env = w.allowed_env(os.environ)
    dotnet = Path(env['DOTNET']).resolve()
    try:
        w.require(os.environ['RUNNER_OS'] == 'Linux' and os.environ['RUNNER_ARCH'] == 'X64', '须hosted Linux x64')
        warm, alloc = out / 'inputs-warm', out / 'inputs-allocation'
        inputs.unpack(downloads / 'warm/paired-warm.tar', warm, 'warm')
        inputs.unpack(downloads / 'allocation/paired-allocation.tar', alloc, 'allocation')
        check_inputs(warm, alloc)
        capture('sdk', [dotnet, '--version'], env, 30, out)
        w.require((out / 'sdk.stdout').read_text().strip() == '10.0.403', 'SDK不符')
        clang = Path(shutil.which('clang', path=env['PATH'])).resolve()
        capture('clang', [clang, '--version'], env, 30, out)
        tools = [dotnet, clang, Path(sys.executable).resolve()]
        tools += [dotnet.parent / 'shared/Microsoft.NETCore.App/10.0.14' / n
                  for n in ('libcoreclr.so', 'System.Private.CoreLib.dll')]
        project, assembly = inputs.project((warm / 'host/Host.original.csproj').read_text())
        (out / 'project.patch').write_text(''.join(difflib.unified_diff(
            (warm / 'host/Host.original.csproj').read_text().splitlines(True), project.splitlines(True))))
        artifacts, executables, managed_hosts = [], {}, {}
        initial_inputs = tools + [p for root in (warm, alloc) for p in root.rglob('*') if p.is_file()]
        initial = w.identities(initial_inputs)
        w.save(out / 'inputs-before.json', initial)
        for arm, label in [('A', 'old'), ('B', 'new')]:
            root = out / arm
            shutil.copytree(warm / 'managed', root / 'managed')
            shutil.copy2(alloc / label / 'RyuPy.Runtime.dll', root / 'managed/RyuPy.Runtime.dll')
            (root / 'host').mkdir()
            shutil.copy2(warm / 'host/Program.cs', root / 'host/Program.cs')
            (root / 'host/Host.csproj').write_text(project)
            state['publish_attempts'] += 1
            try:
                capture('publish-' + arm, [dotnet, 'publish', root / 'host/Host.csproj', '-c', 'Release',
                          '-r', 'linux-x64', '--self-contained', 'true', '-o', root / 'dist',
                          '-p:RuntimeFrameworkVersion=10.0.14', '-p:Deterministic=true',
                          '-p:PathMap=' + str(root) + '=/_/paired', '-p:ImportDirectoryBuildProps=false',
                          '-p:ImportDirectoryBuildTargets=false'], env, 360, root / 'host')
            finally:
                # 即使publish失败也留ILC响应和实际managed Host；不把存在等同成功。
                evidence = root / 'build-inputs'
                evidence.mkdir()
                for path in sorted((root / 'host').rglob('*')):
                    if path.is_file() and (path.suffix == '.rsp' or path.name in
                                          (assembly + '.dll', 'RyuPy.Runtime.dll', 'project.assets.json')):
                        destination = evidence / path.relative_to(root / 'host')
                        destination.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copy2(path, destination)
                with tarfile.open(root / 'build-inputs.tar', 'w') as tar:
                    tar.add(evidence, arcname='build-inputs')
            w.require(w.identities(initial_inputs) == initial, '构建输入或工具漂移')
            managed_host = root / 'host/bin/Release/net10.0/linux-x64' / (assembly + '.dll')
            w.require(managed_host.is_file(), '缺本次实际managed Host')
            managed_hosts[arm] = w.sha(managed_host)
            w.require(w.sha(managed_host.parent / 'RyuPy.Runtime.dll') == w.sha(root / 'managed/RyuPy.Runtime.dll'),
                      '实际构建Runtime不同')
            w.require('Generating native code' in (out / ('publish-' + arm + '.stdout')).read_text(), '缺本轮ILC运行记录')
            ilc = inputs.ilc_binding(root / 'host', {assembly + '.dll': managed_host,
                    'kernel.dll': root / 'managed/kernel.dll', 'RyuPy.Runtime.dll': root / 'managed/RyuPy.Runtime.dll'})
            w.save(out / ('ilc-inputs-' + arm + '.json'), ilc)
            artifacts += [p for p in evidence.rglob('*') if p.is_file()]
            w.require((root / 'host/sdk.version').read_text(encoding='utf-8-sig').strip() == '10.0.403', '发布SDK不符')
            assets = json.loads((root / 'host/obj/project.assets.json').read_text())
            w.save(out / ('resolved-packages-' + arm + '.json'), assets['libraries'])
            exe = root / 'dist' / assembly
            w.require(exe.read_bytes()[:4] == b'\x7fELF' and os.access(exe, os.X_OK), '缺实际ELF')
            shutil.copytree(root / 'dist', root / 'moved')
            w.require(w.dist_identity(root / 'dist') == w.dist_identity(root / 'moved'), '搬移完整性不符')
            with tarfile.open(root / 'dist.tar', 'w') as tar:
                tar.add(root / 'dist', arcname='dist')
            executables[arm] = exe
            artifacts += [p for folder in ('managed', 'dist', 'moved') for p in (root / folder).rglob('*') if p.is_file()]
            artifacts += [root / 'host/Program.cs', root / 'host/Host.csproj']
        w.save(out / 'managed-hosts.json', managed_hosts)
        host_paths = [out / arm / 'host/bin/Release/net10.0/linux-x64' / (assembly + '.dll') for arm in 'AB']
        w.save(out / 'HOST-COMPARISON.json', inputs.compare_hosts(*host_paths))
        w.require((out / 'resolved-packages-A.json').read_bytes() == (out / 'resolved-packages-B.json').read_bytes(), 'AOT解析包不同')
        artifacts += tools + [p for root in (warm, alloc) for p in root.rglob('*') if p.is_file()]
        artifacts += [HERE / name for name in ('paired_runner.py', 'paired_inputs.py', 'runner.py', 'EXPECTED.txt')]
        frozen = w.identities(artifacts)
        w.save(out / 'FROZEN.json', {'identities': frozen, 'order': ORDER, 'count': 100000, 'warm_batches': 4,
               'timed_batches': 12, 'aa_processes': 12, 'ab_processes': 24, 'measurement_seconds': 900,
               'child_seconds': 60, 'publish_seconds_each': 360, 'controller_seconds': 1800})
        product_env = {k: env[k] for k in ('PATH', 'DOTNET_ROOT') if k in env}
        product_env.update(LC_ALL='C.UTF-8', LANG='C.UTF-8', TZ='UTC', UNICODE_VERSION='latest', PYTHONHASHSEED='0')
        def product(label, argv, seconds):
            w.require(w.identities(artifacts) == frozen, '执行前身份漂移')
            capture(label, argv, product_env, seconds, out)
            w.require(not (out / (label + '.stderr')).read_bytes(), '产品stderr非空')
            w.require(w.identities(artifacts) == frozen, '执行后身份漂移')
            return (out / (label + '.stdout')).read_bytes()
        for arm in 'AB':
            root = out / arm
            for kind, argv in [('jit', [dotnet, '--fx-version', '10.0.14', root / 'managed/kernel.dll', 'contract']),
                               ('native', [executables[arm], 'contract']),
                               ('moved', [root / 'moved' / assembly, 'contract'])]:
                w.require(product(arm + '-' + kind, argv, 30) == (HERE / 'EXPECTED.txt').read_bytes(), 'C3不等CP438B')
                state['contracts'] += 1
        deadline = min(time.monotonic() + 900, total_deadline)
        def measure(label, arm):
            remaining = deadline - time.monotonic()
            w.require(remaining > 0, '计量总预算')
            values = w.parse_warm(product(label, [executables[arm], 'warm'], min(60, remaining)))
            w.require(time.monotonic() <= deadline, '计量总预算')
            w.require(json.loads((out / (label + '.json')).read_text())['pid'] not in measurement_pids, '计量PID重复')
            measurement_pids.append(json.loads((out / (label + '.json')).read_text())['pid'])
            return values
        measurement_pids = []
        for arm in 'AB':
            rows = []
            for i in range(6):
                rows.append(measure('aa-' + arm + '-' + str(i), arm))
                w.save(out / ('aa-' + arm + '.json'), rows)
            state['aa'][arm] = w.reliability(rows)
        for i, order in enumerate(ORDER):
            rows = []
            state['blocks'].append(rows)
            for j, arm in enumerate(order):
                rows.append({'arm': arm, 'values': measure('ab-' + str(i) + '-' + str(j), arm)})
                w.save(out / 'blocks.json', state['blocks'])
        w.require(len(measurement_pids) == 36, '计量进程数不符')
        state['measurement_pids'] = measurement_pids
        state.update(analyze(state['blocks']))
        state['passed'] = True
    except Exception as error:
        state['error'] = str(error)
        raise
    finally:
        if frozen is not None:
            state['identity_unchanged'] = w.identities(artifacts) == frozen
            if not state['identity_unchanged']:
                state['passed'] = False
        records = {p.stem: json.loads(p.read_text()) for p in out.glob('*.json')
                   if p.stem.startswith(('aa-A-', 'aa-B-', 'ab-', 'A-jit', 'B-jit', 'A-native', 'B-native', 'A-moved', 'B-moved'))}
        state['process_groups'] = {group: {'attempted': len(rows), 'started': sum(x['started'] for x in rows),
              'completed': sum(x.get('exit') is not None for x in rows)}
              for group, rows in [('contracts', [v for k, v in records.items() if k.startswith(('A-', 'B-'))]),
                                  ('measurement', [v for k, v in records.items() if k.startswith(('aa-', 'ab-'))])]}
        state['actual_processes'] = {'attempted': len(records), 'started': sum(x['started'] for x in records.values()),
                                     'completed': sum(x.get('exit') is not None for x in records.values())}
        w.save(out / 'RESULT.json', state)
        w.upload_check(out)
        w.require(state['passed'], '成对门失败，保原流且不补采')


if __name__ == '__main__':
    out = w.ROOT / 'target/wcwidth-paired-evidence'
    if sys.argv[1:] == ['--upload-check']:
        w.upload_check(out)
    else:
        w.require(sys.argv[1:] == ['--execute'] and sys.flags.optimize == 0, '须明确execute且禁-O')
        run(out, w.ROOT / 'target/paired-downloads')
