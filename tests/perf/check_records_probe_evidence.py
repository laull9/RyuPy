"""严格核对独立 records 探针 TRX，拒绝空跑、跳过、缺诊断及错 SDK/提交。"""
import json
import math
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

EXPECTED = {
    'HitKeepsOriginalKeyValueIdentityAndInsertionOrder',
    'RealStringHashCollisionDoesNotMergeDistinctKeys',
    'DeleteReinsertCompactAndClearShareTheGeneralStorage',
    'HeterogeneousMutationInvalidatesAndRemovalReestablishesProof',
    'SameCountDeleteInsertInvalidatesByVersionNotCount',
    'UserEqualitySeesRealFallbackTupleAndNeverAnAlternateProbe',
    'NestedUserElementsAndCustomComparersAreRejected',
    'RepeatedHitsDoNotAllocateTupleOrReplacementValues',
    'OwnGrowthKeepsProofButExternalLargeMutationStaysRejected',
    'UserCallbackBetweenGetAndSetCannotReuseStaleDomain',
    'ActiveIteratorAllowsOverwriteButRejectsStructuralChange',
    'TupleSubclassAndUserElementRejectWithoutCallingEquality',
    'LongUnicodeStringsUseTheSameTupleHashAndOrdinalEquality',
}
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}


def verify(folder, sha):
    assert (folder / 'commit.txt').read_text().strip() == sha, '实际提交不匹配'
    assert (folder / 'sdk.txt').read_text().strip() == '10.0.403', 'SDK不匹配'
    root = ET.parse(folder / 'records.trx').getroot()
    results = root.findall('.//t:UnitTestResult', NS)
    assert len(results) == 15, f'预期15例，实际{len(results)}'
    assert all(r.get('outcome') == 'Passed' for r in results), '存在失败或跳过'
    for field in ('testId', 'executionId'):
        ids = [r.get(field) for r in results]
        assert all(ids) and len(set(ids)) == 15, f'{field}缺失或重复'
    names = [r.get('testName', '').rsplit('.', 1)[-1] for r in results]
    ordinary = [n for n in names if not n.startswith('HighCardinalityDiagnostic(')]
    assert len(ordinary) == 13 and set(ordinary) == EXPECTED, '普通测试集合不完整'
    assert sorted(n for n in names if n.startswith('HighCardinalityDiagnostic(')) == [
        'HighCardinalityDiagnostic(count: 16384)', 'HighCardinalityDiagnostic(count: 4096)'
    ], '诊断测试集合错误'
    summaries = root.findall('t:ResultSummary', NS)
    assert len(summaries) == 1 and summaries[0].get('outcome') == 'Completed', '汇总状态非成功'
    counters = summaries[0].find('t:Counters', NS)
    assert counters is not None, '缺少汇总'
    assert all(counters.get(k) == '15' for k in ('total', 'executed', 'passed')), '汇总计数错误'
    for key in ('failed', 'error', 'timeout', 'aborted', 'inconclusive',
                'passedButRunAborted', 'notRunnable', 'notExecuted', 'disconnected',
                'warning', 'completed', 'inProgress', 'pending'):
        assert counters.get(key) == '0', f'非通过计数{key}异常或缺失'
    rows = []
    for result in results:
        for node in result.findall('.//t:StdOut', NS):
            rows.extend(re.findall(
                r'records-probe count=(\d+) probe=(False|True) ms=([0-9.]+) bytes=(\d+)',
                node.text or ''))
    assert len(rows) == 4, '缺少或重复高基数诊断'
    assert {(int(n), p) for n, p, _, _ in rows} == {
        (n, p) for n in (4096, 16384) for p in ('False', 'True')}, '诊断臂不完整'
    assert all(math.isfinite(float(ms)) and float(ms) >= 0 and int(b) > 0
               for _, _, ms, b in rows), '诊断值无效'
    return {'passed': 15, 'diagnostics': rows,
            'scope': 'Linux x64 Release C#探针合同；无MIR、CP差分、NativeAOT或性能收益结论'}


if __name__ == '__main__':
    folder = Path(sys.argv[1])
    report = {'commit': sys.argv[2], 'ok': False}
    try:
        report.update(verify(folder, sys.argv[2]))
        report['ok'] = True
    finally:
        (folder / 'evidence.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
    print(json.dumps(report, ensure_ascii=False, indent=2))
