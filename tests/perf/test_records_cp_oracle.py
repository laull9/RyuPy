"""候选对CP证据门合成正反例；本地事件自检不能替代固定CP进程或实际C#运行。"""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import sys
import xml.etree.ElementTree as ET
from check_records_cp_oracle import NAME, KNOWN_NAME, NS, verify
from records_materialized_cp_oracle import build_events

SHA = 'a'*40
TOKEN = 'b'*32

def tag(name): return '{'+NS['t']+'}'+name


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup); self.folder=Path(self.temp.name)
        for file, text in [('commit.txt', SHA), ('sdk.txt', '10.0.403'), ('python.txt', 'Python 3.12.13')]:
            (self.folder/file).write_text(text)
        header = {'schema': 1, 'scope': 'runtime-candidate-only', 'commit': SHA, 'token': TOKEN, 'events': build_events()}
        self.oracle = {**header, 'python': [3,12,13]}
        self.actual = {**header, 'events': [r for r in header['events'] if not r[0].startswith('custom-')], 'passed': True, 'fast': 311, 'fallback': 6}
        self.root = ET.Element(tag('TestRun')); rows=ET.SubElement(self.root,tag('Results'))
        result=ET.SubElement(rows,tag('UnitTestResult'),testName=NAME,outcome='Passed',testId='test',executionId='execution')
        out=ET.SubElement(result,tag('Output'))
        line=f'records-cp-oracle commit={SHA} token={TOKEN} events=22 fast=311 fallback=6 scope=runtime-candidate-only'
        ET.SubElement(out,tag('StdOut')).text=line
        (self.folder/'oracle-test.log').write_text(line)
        definitions=ET.SubElement(self.root,tag('TestDefinitions'));definition=ET.SubElement(definitions,tag('UnitTest'),id='test')
        ET.SubElement(definition,tag('Execution'),id='execution')
        ET.SubElement(definition,tag('TestMethod'),className=NAME.rsplit('.',1)[0],name=NAME.rsplit('.',1)[1])
        summary=ET.SubElement(self.root,tag('ResultSummary'),outcome='Completed')
        counter=ET.SubElement(summary,tag('Counters'),total='1',executed='1',passed='1')
        for key in ('failed', 'error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted', 'notRunnable',
                    'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'):
            counter.set(key,'0')

    def check(self):
        (self.folder/'oracle.json').write_text(json.dumps(self.oracle));(self.folder/'actual.json').write_text(json.dumps(self.actual))
        ET.ElementTree(self.root).write(self.folder/'oracle-test.trx')
        return verify(self.folder,SHA,TOKEN)

    def test_valid_and_independent_event_obligations(self):
        self.assertEqual(self.check()['events'],22)
        events=build_events();self.assertEqual(events[1],['identity-hit','2','true','false'])
        self.assertEqual(events[6],['integer','max','9223372036854775808','int','false'])
        self.assertEqual(events[15:19],[['hash','query'],['eq','stored','query']]*2)

    def test_wrong_identity_version_and_fake_all_fallback(self):
        for key,bad in [('commit','c'*40),('token','d'*32),('scope','product'),('fast',0),('fallback',318),('passed',False),('fast',True)]:
            old=self.actual.get(key);self.actual[key]=bad
            with self.subTest(key=key), self.assertRaises(AssertionError):self.check()
            self.actual[key]=old
        self.oracle['python']=[3,12,14]
        with self.assertRaises(AssertionError):self.check()

    def test_missing_reordered_or_wrong_event(self):
        for events in (self.actual['events'][:-1],list(reversed(self.actual['events'])),[row[:] for row in self.actual['events']]):
            if len(events)==22 and events[0][0]=='identity-miss':events[0][-1]='false'
            old=self.actual['events'];self.actual['events']=events
            with self.assertRaises(AssertionError):self.check()
            self.actual['events']=old

    def test_known_incompatibility_is_separate_and_exact(self):
        self.check()
        actual={**self.actual, 'scope':'known-incompatibility', 'fast':0, 'fallback':1,
                'events':[['custom-get','ann','buy'],['custom-set','6'],
                          ['custom-rejected','NotImplementedError','dict layout with non-scalar keys is not supported','0']]}
        for path,key,val in [('.//t:UnitTestResult','testName',KNOWN_NAME),
                             ('.//t:TestMethod','name',KNOWN_NAME.rsplit('.',1)[1])]:
            self.root.find(path,NS).set(key,val)
        line=f'records-cp-oracle commit={SHA} token={TOKEN} events=3 fast=0 fallback=1 scope=known-incompatibility'
        self.root.find('.//t:StdOut',NS).text=line
        ET.ElementTree(self.root).write(self.folder/'known.trx')
        (self.folder/'known.log').write_text(line)
        def check():
            (self.folder/'known.json').write_text(json.dumps(actual))
            return verify(self.folder,SHA,TOKEN,known=True)
        self.assertFalse(check()['cp_equivalent'])
        for key,bad in [('fast',1),('passed',False),('scope','runtime-candidate-only'),('events',self.oracle['events'][20:23])]:
            old=actual[key];actual[key]=bad
            with self.subTest(key=key),self.assertRaises(AssertionError):check()
            actual[key]=old
        actual['events'][2][-1]='1'
        with self.assertRaises(AssertionError):check()

    def test_generator_refuses_nonpinned_python(self):
        import records_materialized_cp_oracle as generator
        destination=self.folder/'must-not-exist.json'
        with patch.object(sys,'argv',['generator','--output',str(destination),'--expected-sha',SHA,'--token',TOKEN]), \
             patch.object(generator.sys,'version_info',(3,12,14)), \
             patch.object(generator.subprocess,'check_output') as git:
            with self.assertRaises(SystemExit):generator.main()
            git.assert_not_called()
        self.assertFalse(destination.exists())

    def test_test_identity_summary_and_missing_execution(self):
        for path,key,value in [('.//t:UnitTestResult','outcome','NotExecuted'),('.//t:UnitTestResult','testName','wrong'),
                               ('.//t:Execution','id','wrong'),('t:ResultSummary','outcome','Failed'),('.//t:Counters','failed','1')]:
            node=self.root.find(path,NS);old=node.get(key);node.set(key,value)
            with self.subTest(key=key),self.assertRaises(AssertionError):self.check()
            node.set(key,old)
        (self.folder/'oracle-test.log').write_text('')
        with self.assertRaises(AssertionError):self.check()


if __name__=='__main__':unittest.main()
