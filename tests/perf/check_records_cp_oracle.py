"""核对固定CP oracle、候选实际事件、精确测试身份及非零快路/回退证据。"""
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
NAME = 'RyuPy.Runtime.Tests.PyDictMaterializedCpOracleTests.ActualCandidateAndFallbackMatchFreshPinnedCPythonEvents'
TAGS = ['identity-miss', 'identity-hit', 'identity-reinsert'] + ['integer']*6 + ['collision-domain']*2 + [
    'add-replace', 'replace-result', 'add-throw', 'exception', 'hash', 'eq', 'hash', 'eq',
    'user-key-result', 'custom-get', 'custom-set', 'custom-result', 'budget-external', 'budget-clear']


def verify(folder, sha, token):
    assert re.fullmatch('[0-9a-f]{40}', sha) and re.fullmatch('[0-9a-f]{32}', token)
    assert (folder/'commit.txt').read_text().strip() == sha
    assert (folder/'sdk.txt').read_text().strip() == '10.0.403'
    assert (folder/'python.txt').read_text().strip() == 'Python 3.12.13'
    oracle = json.loads((folder/'oracle.json').read_text()); actual = json.loads((folder/'actual.json').read_text())
    for payload in (oracle, actual):
        assert type(payload.get('schema')) is int and payload['schema'] == 1
        assert payload.get('scope') == 'runtime-candidate-only' and payload.get('commit') == sha and payload.get('token') == token
        events = payload.get('events')
        assert isinstance(events, list) and len(events) == 25
        assert all(isinstance(row, list) and row and all(type(v) is str for v in row) for row in events)
        assert [row[0] for row in events] == TAGS
    assert oracle.get('python') == [3, 12, 13]
    assert actual.get('passed') is True
    assert type(actual.get('fast')) is int and actual['fast'] == 311
    assert type(actual.get('fallback')) is int and actual['fallback'] == 7
    assert actual['events'] == oracle['events'], 'CP和候选事件不一致'
    root = ET.parse(folder/'oracle-test.trx').getroot(); rows = root.findall('.//t:UnitTestResult', NS)
    assert len(rows) == 1 and rows[0].get('testName') == NAME and rows[0].get('outcome') == 'Passed'
    result = rows[0]; assert result.get('testId') and result.get('executionId')
    definitions = root.findall('.//t:UnitTest', NS)
    assert len(definitions) == 1 and definitions[0].get('id') == result.get('testId')
    execution = definitions[0].find('t:Execution', NS); method = definitions[0].find('t:TestMethod', NS)
    assert execution is not None and execution.get('id') == result.get('executionId')
    assert method is not None and method.get('className') == NAME.rsplit('.', 1)[0] and method.get('name') == NAME.rsplit('.', 1)[1]
    summaries = root.findall('t:ResultSummary', NS)
    assert len(summaries) == 1 and summaries[0].get('outcome') == 'Completed'
    counters = summaries[0].findall('t:Counters', NS); assert len(counters) == 1
    for name in ('total', 'executed', 'passed'): assert counters[0].get(name) == '1'
    for name in ('failed', 'error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted',
                 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'):
        assert counters[0].get(name) == '0'
    expected_line = f'records-cp-oracle commit={sha} token={token} events=25 fast=311 fallback=7 scope=runtime-candidate-only'
    stdout = '\n'.join(n.text or '' for n in result.findall('.//t:StdOut', NS))
    for text in (stdout, (folder/'oracle-test.log').read_text()):
        assert [l.strip() for l in text.splitlines() if l.strip().startswith('records-cp-oracle')] == [expected_line]
    return {'events': 25, 'fast': 311, 'fallback': 7, 'scope': 'runtime-candidate-only'}


if __name__ == '__main__':
    folder = Path(sys.argv[1]); report = {'commit': sys.argv[2], 'ok': False}
    try:
        report.update(verify(folder, sys.argv[2], sys.argv[3])); report['ok'] = True
    finally:
        (folder/'oracle-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
    print(json.dumps(report, ensure_ascii=False, indent=2))
