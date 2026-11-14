"""固定旧源、单Host双Runtime的Invoke分配门；不做时间A/B、不代替真实Python/AOT。"""
import json
import os
from pathlib import Path
import shutil
import sys
import time
import xml.etree.ElementTree as ET
import runner as w

ROOT, HERE = w.ROOT, w.HERE
BASE = '6923573d78f7b504dcbe6d6067dd145cd1a7bd65'
TREE = 'c27b271f3223e215a3c4fcc1618664ad3acf7968'
PENDING_SHA = '3cc93c5aee26f9a7e9ac1130e27d9f57e50ad6098708743a7f695733765f0eb8'
NEW_FACTS = {
    'EqualStringsKeepIdentityOrderAndTrueLruEviction',
    'RealCollisionKeepsStoredLeftOrderTruthinessAndHashOnce',
    'FalseNotImplementedAndThrowRemainRealCollisionOutcomes',
    'ClearAndInternalArgsMutationKeepSnapshotAndCompleteComparison',
    'EqualityRecursiveInsertionRestartsAndDoesNotReturnRemovedEntry',
    'BodyRecursionKeepsInnerEntryAndExceptionDoesNotInsert',
    'ConcurrentSameKeyComputesOutsideLockWithoutOverwritingWinner',
    'ClearWhileBodyIsOutsideLockKeepsOriginalPostClearInsertion',
    'TypedKeywordsDisabledAndSingleFastKeysKeepFallback',
    'PairStorageReleasesBothStringsAfterClear',
}


def check_context(env, event):
    ref = 'refs/heads/ci/wcwidth-legacy-aot-2909056'
    w.require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted' and env.get('GITHUB_ACTIONS') == 'true'
              and env.get('GITHUB_EVENT_NAME') == 'push' and env.get('GITHUB_RUN_ATTEMPT') == '1'
              and env.get('GITHUB_REF') == event.get('ref') == ref, '非授权hosted首轮专用分支')
    head = event.get('head_commit', {})
    w.require(head.get('message') == 'validation: wcwidth-warm-kernel-001'
              and head.get('id') == event.get('after') == env.get('GITHUB_SHA'), '事件标题/HEAD不一致')


def verify_trx(path):
    rows = ET.parse(path).findall('.//{*}UnitTestResult')
    prefix = 'RyuPy.Runtime.Tests.PyLruStringPairTests.'
    selected = [r.attrib['testName'][len(prefix):] for r in rows if r.attrib['testName'].startswith(prefix)]
    w.require(len(selected) == 10 and set(selected) == NEW_FACTS, '十个LRU新Fact缺失/重复')
    w.require(rows and all(r.get('outcome') == 'Passed' for r in rows), 'LRU/原functools失败或跳过')
    for owner in ('PyLruCacheTests.', 'PyFunctoolsTests.'):
        w.require(any(owner in r.attrib['testName'] for r in rows), '旧LRU保护未运行')
    return {'all_passed': True, 'total': len(rows), 'new_facts': sorted(selected)}


def parse_sample(raw, folder, runtime_hash, host_hash):
    value = json.loads(raw)
    w.require(value['schema'] == 1 and value['warm'] == 20000 and value['count'] == 100000, '参数漂移')
    w.require(type(value['pid']) is int and value['pid'] > 0, 'PID缺失')
    w.require(value['runtime_version'] == '1.0.0.0', 'Runtime程序集版本改变')
    for key, name, digest in [('runtime', 'RyuPy.Runtime.dll', runtime_hash), ('host', 'AllocationHost.dll', host_hash)]:
        w.require(value[key + '_path'] == str((folder / name).resolve()) and
                  value[key + '_sha256'] == digest, '实际装载身份不符')
    domains = value['domains']
    w.require(len(domains) == 2 and all(type(d['typed']) is bool for d in domains) and [d['typed'] for d in domains] == [False, True], '域缺失/调换')
    for domain in domains:
        w.require(len(domain['samples']) == 3, '批数改变')
        for i, row in enumerate(domain['samples']):
            w.require(row['batch'] == i and row['count'] == 100000 and row['hits'] == 20000 + (i + 1) * 100000
                      and row['misses'] == 1 and row['size'] == 1 and row['maximum'] == 128
                      and row['calls'] == 1 and row['identity'] is True, '真实调用/结果/缓存状态不符')
            w.require(type(row['allocated']) is int and row['allocated'] >= 0
                      and type(row['thread']) is int and row['thread'] > 0, '分配/线程字段无效')
    return value


def compare(rows):
    w.require(len(rows) == 4 and [r['label'] for r in rows] == ['old', 'new', 'new', 'old'], '必须ABBA四进程')
    w.require(len({r['sample']['pid'] for r in rows}) == 4, '不是四独立进程')
    values = {}
    for label in ('old', 'new'):
        values[label] = []
        for domain in range(2):
            batch = [s['allocated'] for row in rows if row['label'] == label
                     for s in row['sample']['domains'][domain]['samples']]
            w.require(len(batch) == 6 and len(set(batch)) == 1, '分配不稳定，不丢弃首批或补采')
            values[label].append(batch[0])
    w.require(values['old'][1] == values['new'][1], 'typed旧路控制改变')
    w.require(values['new'][0] < values['old'][0], '本轮未证双字符串命中分配减少')
    return {'bytes_per_invoke': {k: [x / 100000 for x in v] for k, v in values.items()},
            'saved_bytes_per_hit': (values['old'][0] - values['new'][0]) / 100000,
            'time_ratio': None, 'whole_kernel_allocation': None, 'native_allocation': None}


def run_pending(out, cli, runtime, dotnet, env):
    """复用warm已构建普通CLI；保持pending标记并真实编译/运行一次，任何失败保红。"""
    out.mkdir(parents=True, exist_ok=False)
    fixture = ROOT / 'tests/e2e/stdlib/functools_lru_string_pair.py'
    state = {'passed': False, 'cp_rerun': False, 'jit_attempts': 0}
    inputs = [fixture, cli, runtime, dotnet]
    frozen = w.identities(inputs)
    w.save(out / 'inputs.json', frozen)
    try:
        w.require(w.sha(fixture) == PENDING_SHA and fixture.read_text().startswith('# ryupy: pending\n'), '原pending源改变')
        entry = out / 'lru_string_pair.py'
        shutil.copy2(fixture, entry)
        products = out / 'products'
        w.capture(out, 'compile', [cli, 'build', entry, '--diagnostic-stages', '--out-dir', products], env, 180, ROOT)
        dlls = list(products.rglob('lru_string_pair.dll'))
        w.require(len(dlls) == 1, '普通CLI真实业务PE缺失/重复')
        pe = dlls[0]
        w.require((pe.parent / 'lru_string_pair.runtimeconfig.json').is_file(), '缺真实runtimeconfig')
        w.require(w.sha(pe.parent / 'RyuPy.Runtime.dll') == w.sha(runtime), 'pending实际Runtime不符')
        w.capture(out, 'pe', [sys.executable, '-B', ROOT / 'tests/validation/instance_globals/read_entry_identity.py', pe], env, 30, ROOT)
        identity = json.loads((out / 'pe.stdout').read_text())
        w.require(identity['sha256'] == w.sha(pe) and identity['method_name'] == 'Main'
                  and identity['owner']['name'] == 'Module' and identity['owner']['namespace'] == 'RyuPy.Generated'
                  and identity['signature_blob'] == '0001081d0e', 'pending实际入口不符')
        products_before = w.identities([p for p in products.rglob('*') if p.is_file()])
        w.save(out / 'products-before.json', products_before)
        state['jit_attempts'] = 1
        w.capture(out, 'jit', [dotnet, pe], env, 30, out)
        w.require((out / 'jit.stdout').read_bytes() == b'lru-string-pair-ok\n' and
                  not (out / 'jit.stderr').read_bytes(), 'pending与已冻CP19B双流不等')
        w.require(w.identities([p for p in products.rglob('*') if p.is_file()]) == products_before, 'pending产品漂移')
        w.require(w.identities(inputs) == frozen, 'pending输入漂移')
        state['passed'] = True
    except Exception as error:
        state['error'] = str(error)
        raise
    finally:
        w.save(out / 'RESULT.json', state)


def run(out):
    out.mkdir(parents=True, exist_ok=False)
    state = {'passed': False, 'measurement_processes': 0, 'measurement_attempts': 0, 'retries': 0, 'samples': []}
    env = w.allowed_env(os.environ)
    old = ROOT / 'target/lru-baseline-source'
    dotnet = Path(env['DOTNET']).resolve()
    repo = tools = None
    try:
        check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
        w.require(sys.flags.optimize == 0, '不得-O')
        inputs = [p for p in HERE.rglob('*') if p.is_file() and
                  not {'obj', 'bin', '__pycache__'}.intersection(p.parts)]
        input_identity = w.identities(inputs)
        w.save(out / 'inputs-before.json', input_identity)
        repo = w.repository_identity(out, 'before', env)
        w.require(repo[0] == env['GITHUB_SHA'], '候选非事件HEAD')
        for label, path in [('commit', 'HEAD'), ('tree', 'HEAD^{tree}')]:
            w.capture(out, 'old-' + label, ['git', 'rev-parse', path], env, 30, old)
        w.require((out / 'old-commit.stdout').read_text().strip() == BASE and
                  (out / 'old-tree.stdout').read_text().strip() == TREE, '旧完整源身份不符')
        w.capture(out, 'old-clean', ['git', 'status', '--porcelain'], env, 30, old)
        w.require(not (out / 'old-clean.stdout').read_bytes(), '旧源不clean')
        old_files = sorted(p for p in (old / 'runtime/RyuPy.Runtime').rglob('*') if p.is_file())
        new_files = sorted(p for p in (ROOT / 'runtime/RyuPy.Runtime').rglob('*') if p.is_file()
                           and not {'bin', 'obj'}.intersection(p.parts))
        old_map = {str(p.relative_to(old)): w.sha(p) for p in old_files}
        new_map = {str(p.relative_to(ROOT)): w.sha(p) for p in new_files}
        changed = {p for p in set(old_map) | set(new_map) if old_map.get(p) != new_map.get(p)}
        w.require(changed == {'runtime/RyuPy.Runtime/' + n for n in ('PyHash.cs', 'PyLruKey.cs', 'PyLruCache.cs')},
                  '生产Runtime差分超出三文件')
        w.save(out / 'source-identity.json', {'old': old_map, 'new': new_map, 'changed': sorted(changed)})
        w.capture(out, 'sdk', [dotnet, '--version'], env, 30, ROOT)
        w.require((out / 'sdk.stdout').read_text().strip() == '10.0.403', '固定SDK改变')
        tools = w.identities([dotnet, Path(sys.executable).resolve()])
        w.save(out / 'tools.json', tools)
        for label, source in [('old', old), ('new', ROOT)]:
            w.capture(out, label + '-build', [dotnet, 'build', source / 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj',
                      '-c', 'Release', '-p:TraceAdapterTest=false'], env, 300, source)
        runtime = ROOT / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
        runtime_hash = w.sha(runtime)
        trx = out / 'trx'
        w.capture(out, 'lru-tests', [dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
                  '-c', 'Release', '-p:TraceAdapterTest=false', '--filter',
                  'FullyQualifiedName~PyLruStringPairTests|FullyQualifiedName~PyLruCacheTests|FullyQualifiedName~PyFunctoolsTests',
                  '--logger', 'trx;LogFileName=lru.trx', '--results-directory', trx], env, 300, ROOT)
        w.save(out / 'trx-verified.json', verify_trx(trx / 'lru.trx'))
        test_root = ROOT / 'runtime/RyuPy.Runtime.Tests/bin/Release/net10.0'
        w.require(w.sha(runtime) == runtime_hash == w.sha(test_root / 'RyuPy.Runtime.dll'), '测试Runtime混用')
        shutil.copy2(test_root / 'RyuPy.Runtime.Tests.dll', out / 'RyuPy.Runtime.Tests.dll')
        baseline = old / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
        host = out / 'host'
        w.capture(out, 'host-build', [dotnet, 'build', HERE / 'allocation_host/AllocationHost.csproj',
                  '-c', 'Release', '-p:BaselineRuntime=' + str(baseline), '-o', host], env, 120, ROOT)
        w.require(w.sha(host / 'RyuPy.Runtime.dll') == w.sha(baseline), 'Host不是引用旧Runtime')
        for label, dll in [('old', baseline), ('new', runtime)]:
            folder = out / label
            shutil.copytree(host, folder)
            shutil.copy2(dll, folder / 'RyuPy.Runtime.dll')
        w.require(w.sha(out / 'old/AllocationHost.dll') == w.sha(out / 'new/AllocationHost.dll'), 'Host重编译')
        frozen_files = [p for name in ('old', 'new') for p in (out / name).rglob('*') if p.is_file()]
        frozen = w.identities(frozen_files)
        w.save(out / 'products.json', frozen)
        deadline = time.monotonic() + 300
        for i, label in enumerate(('old', 'new', 'new', 'old')):
            folder = out / label
            w.require(w.identities(frozen_files) == frozen, '测量前产品漂移')
            state['measurement_attempts'] += 1
            w.capture(out, 'sample-' + str(i), [dotnet, folder / 'AllocationHost.dll'], env,
                      min(60, deadline - time.monotonic()), out)
            state['measurement_processes'] += 1
            w.require(not (out / ('sample-' + str(i) + '.stderr')).read_bytes(), 'Host stderr不空')
            sample = parse_sample((out / ('sample-' + str(i) + '.stdout')).read_bytes(), folder,
                                  w.sha(folder / 'RyuPy.Runtime.dll'), w.sha(host / 'AllocationHost.dll'))
            state['samples'].append({'label': label, 'sample': sample})
            w.save(out / 'samples.json', state['samples'])
            w.require(w.identities(frozen_files) == frozen, '测量后产品漂移')
        w.require(time.monotonic() <= deadline, '测量总deadline超限')
        state.update(compare(state['samples']))
        state['passed'] = True
    except Exception as error:
        state['error'] = str(error)
        raise
    finally:
        try:
            if 'input_identity' in locals():
                w.require(w.identities(inputs) == input_identity, 'Host/runner输入漂移')
                w.save(out / 'inputs-after.json', w.identities(inputs))
            if repo is not None:
                w.require(w.repository_identity(out, 'after', env) == repo, '候选源漂移')
            if tools is not None:
                w.require(w.identities([dotnet, Path(sys.executable).resolve()]) == tools, '工具漂移')
            if 'old_map' in locals():
                w.require({str(p.relative_to(old)): w.sha(p) for p in old_files} == old_map, '旧源漂移')
                w.require({str(p.relative_to(ROOT)): w.sha(p) for p in new_files} == new_map, '新源漂移')
        except Exception as error:
            state.update(passed=False, final_identity_error=str(error))
            raise
        finally:
            records = [json.loads(p.read_text()) for p in sorted(out.glob('sample-*.json'))]
            state['actual_processes'] = {'attempted': len(records), 'started': sum(x['started'] for x in records),
                                         'completed': sum(x.get('exit') is not None for x in records)}
            w.save(out / 'RESULT.json', state)
            if state['passed']:
                from paired_inputs import allocation
                allocation(out)
            w.upload_check(out)


if __name__ == '__main__':
    if sys.argv[1:] == ['--upload-check']:
        w.upload_check(ROOT / 'target/lru-allocation-evidence')
    else:
        w.require(sys.argv[1:] == ['--execute'], '须显式--execute')
        run(ROOT / 'target/lru-allocation-evidence')
