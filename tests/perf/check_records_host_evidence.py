"""严格核对六场景36轮host原语筛选，保留原始样本，不生成产品加速声明。"""
import json
import math
from pathlib import Path
import re
import statistics
import sys
import xml.etree.ElementTree as ET
from check_records_probe_evidence import NS

SCENARIOS = {(32, 0, False, False), (32, 1, False, False), (32, 10, False, False),
             (4096, 0, False, False), (32, 0, True, False), (32, 0, False, True)}
NAME = re.compile(r'RyuPy.Runtime.Tests.PyDictStringPairProbeDiagnosticTests\.'
                  r'PairedGetIncrementSetHostDiagnostic\(cardinality: (\d+), missPercent: (\d+), '
                  r'collision: (False|True), invalidate: (False|True)\)')
ROW = re.compile(r'^host-probe keys=(\d+) missPercent=(\d+) collision=(False|True) '
                 r'invalidate=(False|True) round=(\d+) order=(AB|BA) A_ms=([0-9.]+) '
                 r'B_ms=([0-9.]+) A_bytes=(\d+) B_bytes=(\d+) fallback=(\d+)$', re.M)
MEDIAN = re.compile(r'^host-probe median A_ms=([0-9.]+) B_ms=([0-9.]+);', re.M)


def scenario(values):
    n, miss, collision, invalidate = values
    return int(n), int(miss), collision == 'True', invalidate == 'True'


def verify(folder, sha):
    assert (folder / 'commit.txt').read_text().strip() == sha, '提交不匹配'
    assert (folder / 'sdk.txt').read_text().strip() == '10.0.403', 'SDK不匹配'
    root = ET.parse(folder / 'host.trx').getroot()
    results = root.findall('.//t:UnitTestResult', NS)
    assert len(results) == 6 and all(r.get('outcome') == 'Passed' for r in results), '必须六例全部通过'
    for key in ('testId', 'executionId'):
        ids = [r.get(key) for r in results]
        assert all(ids) and len(set(ids)) == 6, '测试身份缺失或重复'
    summaries = root.findall('t:ResultSummary', NS)
    assert len(summaries) == 1 and summaries[0].get('outcome') == 'Completed', '汇总失败或重复'
    counter = summaries[0].find('t:Counters', NS)
    assert counter is not None
    for key in ('total', 'executed', 'passed'):
        assert counter.get(key) == '6', '汇总计数不符'
    for key in ('failed', 'error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted',
                'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'):
        assert counter.get(key) == '0', '非通过计数非零或缺失'
    seen, evidence = set(), []
    for result in results:
        name = NAME.fullmatch(result.get('testName', ''))
        assert name, '未识别测试名称'
        current = scenario(name.groups())
        assert current in SCENARIOS and current not in seen, '场景重复或未知'
        seen.add(current)
        stdout = '\n'.join(node.text or '' for node in result.findall('.//t:StdOut', NS))
        rows = ROW.findall(stdout)
        assert len(rows) == 6 and stdout.count('host-probe keys=') == 6, '场景不满六轮或重复输出'
        assert {int(row[4]) for row in rows} == set(range(6)), '轮次缺失或重复'
        for row in rows:
            assert scenario(row[:4]) == current, '样本归属错误'
            round_no = int(row[4])
            assert row[5] == ('AB' if round_no % 2 == 0 else 'BA'), '执行次序错误'
            assert all(math.isfinite(float(v)) and float(v) >= 0 for v in row[6:8]), '耗时无效'
            assert all(int(v) > 0 for v in row[8:10]), '分配无效'
            assert int(row[10]) == (30000 if current[3] else 0), 'fallback不符'
        medians = MEDIAN.findall(stdout)
        assert len(medians) == 1 and stdout.count('host-probe median ') == 1, '缺少或重复中位数'
        for i, value in enumerate(medians[0]):
            assert math.isfinite(float(value)) and abs(float(value) - statistics.median(float(r[6+i]) for r in rows)) <= 0.002, '中位数与样本不符'
        evidence.append({'scenario': current, 'raw_rows': rows, 'reported_medians_ms': medians[0]})
    assert seen == SCENARIOS
    return {'scenarios': evidence, 'samples': 36,
            'scope': '仅同进程CLR host筛选，非MIR接线、真实records、NativeAOT或产品加速验收'}


if __name__ == '__main__':
    folder = Path(sys.argv[1]); report = {'commit': sys.argv[2], 'ok': False}
    try:
        report.update(verify(folder, sys.argv[2])); report['ok'] = True
    finally:
        (folder / 'host-evidence.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
    print(json.dumps(report, ensure_ascii=False, indent=2))
