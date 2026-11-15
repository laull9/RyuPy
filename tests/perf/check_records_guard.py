"""实验守卫结构/Runtime/真实历史ABI证据；不等于编译源码CP/JIT/AOT差分。"""
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
from check_records_cp_oracle import NS

ROOT = 'RyuPy.Runtime.Tests.'
CASES = {
    'guard': {ROOT+'PyRecordsIncrementGuardTests.'+n for n in (
        'PublicGuardKeepsMaterializedIdentityAndCachedBox',
        'RejectedGuardDoesNotRunUserProtocolsOrWrite',
        'CachedDomainRejectsLargeExternalChangesAndRecoversAfterClear',
        'WeakAccessorCacheDoesNotKeepOwnerAlive')},
    'abi': {ROOT+'PyTraceStateArtifactTests.'+n for n in (
        'RecordsGuardCompiledAbiMatchesExactDeclaredIncrement',
        'RecordsGuardIncrementRejectsMissingChangedDuplicateAndUnknownEntries')},
    'baseline': {ROOT+'PyTraceStateArtifactTests.ProducedAssemblyPreservesEveryBaselinePublicSignature'},
}
MIR = {'records_increment_contract_tests::'+n for n in (
    'records_guard_keeps_capture_and_original_fallback_cfg',
    'records_guard_rejects_observation_unreachable_and_invalid_abi',
    'records_guard_multiple_same_block_windows_keep_each_original_slow_path')}
IGNORED = {'trace_frame_tests::original_frames_has_pc_partitioned_retry_frame_obligations',
           'trace_frame_tests::renamed_generator_and_factory_alias_keep_frame_proof',
           'trace_frame_tests::repeated_actual_match_block_converges_without_epoch_growth',
           'trace_frame_dataflow::tests::actual_mir_diamond_reaches_fixed_point'}


def trx(path, names):
    root=ET.parse(path).getroot(); rows=root.findall('.//t:UnitTestResult',NS)
    assert len(rows)==len(names) and {r.get('testName') for r in rows}==names
    for field in ('testId','executionId'):
        values=[r.get(field) for r in rows];assert all(values) and len(set(values))==len(rows)
    definitions=root.findall('.//t:UnitTest',NS)
    assert len(definitions)==len(rows)
    defs={d.get('id'):d for d in definitions};assert len(defs)==len(rows)
    for row in rows:
        assert row.get('outcome')=='Passed'
        d=defs[row.get('testId')];e=d.find('t:Execution',NS);m=d.find('t:TestMethod',NS)
        assert e is not None and e.get('id')==row.get('executionId')
        assert m is not None and m.get('className')+'.'+m.get('name')==row.get('testName')
    summaries=root.findall('t:ResultSummary',NS);assert len(summaries)==1 and summaries[0].get('outcome')=='Completed'
    cs=summaries[0].findall('t:Counters',NS);assert len(cs)==1
    for k in ('total','executed','passed'):assert cs[0].get(k)==str(len(rows))
    for k in ('failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending'):assert cs[0].get(k)=='0'
    return {'passed':len(rows)}


def rust(text, expected=None, passed=None, ignored=frozenset(), focused=False):
    rows=re.findall(r'^test ([\w:]+) \.\.\. (ok|FAILED|ignored)\b',text,re.M)
    assert len({n for n,_ in rows})==len(rows)
    if expected is not None:assert {n for n,_ in rows}==expected
    assert {n for n,r in rows if r=='ignored'}==set(ignored)
    assert all(r in ('ok','ignored') for _,r in rows)
    summaries=re.findall(r'^test result: ok\. (\d+) passed; (\d+) failed; (\d+) ignored; (\d+) measured; (\d+) filtered out;',text,re.M)
    assert len(summaries)==1
    p,f,i,m,filtered=map(int,summaries[0]);assert p==len(rows)-len(ignored) and p>0 and f==m==0 and i==len(ignored)
    if not focused:assert filtered==0
    if passed is not None:assert p==passed
    return {'passed':p,'ignored':i}


def baseline(folder):
    assert (folder/'baseline-commit.txt').read_text().strip()=='bd5d7dbe1f09bd3ba649e9b1c1928845784e4fe8'
    assert (folder/'baseline-tree.txt').read_text().strip()=='aff0fc2cf409551b5287c638cefe131a1ddd0e1f'
    identity=json.loads((folder/'assembly-identities.json').read_text())
    for key in ('baseline','candidate'):
        blob=(folder/(key+'.dll')).read_bytes()
        assert set(identity[key]) == {'path','sha256','bytes'}
        assert identity[key]['sha256']==hashlib.sha256(blob).hexdigest() and identity[key]['bytes']==len(blob)
        assert Path(identity[key]['path']).is_absolute() and Path(identity[key]['path']).name==key+'.dll'
    assert Path(identity['baseline']['path']).parent == Path(identity['candidate']['path']).parent
    assert identity['baseline']['sha256']!=identity['candidate']['sha256']
    data=json.loads((folder/'baseline-abi.json').read_text())
    for key in ('baseline','candidate'):
        # 下载工件后的本地目录不同；绑定本次runner清单路径而非当前检查器目录。
        assert data[key] == identity[key]['path']
        assert type(data[key+'Bytes']) is int and data[key+'Bytes'] == identity[key]['bytes']
    assert type(data['signatures']) is int and data['signatures']==1659
    assert data['expected']==data['actual']
    assert len(data['actual']) == data['candidateSignatures']
    assert len(set(data['actual'])) == len(data['actual'])
    assert data['recordsGuardAdditions']==[
        'T:RyuPy.Runtime.PyRecordsIncrement:1048961:System.Object:',
        'M:RyuPy.Runtime.PyRecordsIncrement:TryMaterialized:150:[0;0;2](Object,Object)->Boolean:']
    return {'historical_signatures':1659,'records_additions':2}


def verify(folder,sha):
    assert (folder/'commit.txt').read_text().strip()==sha
    assert (folder/'sdk.txt').read_text().strip()=='10.0.403'
    assert (folder/'rustc.txt').read_text().startswith('rustc 1.99.1 ')
    assert json.loads((folder/'rust-exits.json').read_text())==dict(fmt=0,mir=0,full=0,cil=0,cil_full=0,feature=0)
    report={k:trx(folder/(k+'.trx'),v) for k,v in CASES.items()}
    report['mir']=rust((folder/'guard-mir.log').read_text(),MIR,3,focused=True)
    report['mir_full']=rust((folder/'mir-full.log').read_text(),passed=396,ignored=IGNORED)
    report['cil']=rust((folder/'guard-cil.log').read_text(),{'records_guard_tests::records_guard_emits_bool_object_object_runtime_call'},1,focused=True)
    report['cil_full']=rust((folder/'cil-full.log').read_text(),ignored={'explicit_super_pe_tests::explicit_super_caller_actual_pe_gate','instance_pe_tests::explicit_instance_actual_pe_gate','instance_pe_tests::instance_class_actual_pe_gate'})
    report['historical_abi']=baseline(folder)
    return report


if __name__=='__main__':
    folder=Path(sys.argv[1]);result={'ok':False,'commit':sys.argv[2],'scope':'isolated-guard-structure-runtime-not-source-aot'}
    try:result['checks']=verify(folder,sys.argv[2]);result['ok']=True
    finally:(folder/'guard-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
    print(json.dumps(result,ensure_ascii=False,indent=2))
