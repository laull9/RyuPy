"""证据门负例；合成数据不代替真实C#/Rust或历史DLL。"""
from pathlib import Path
import tempfile
import json
import hashlib
import unittest
import xml.etree.ElementTree as ET
from check_records_guard import NS, rust, trx, baseline


def tag(n):return '{'+NS['t']+'}'+n


class GuardEvidenceTests(unittest.TestCase):
    def test_exact_trx_identity_and_failure_counters(self):
        root=ET.Element(tag('TestRun'));results=ET.SubElement(root,tag('Results'))
        r=ET.SubElement(results,tag('UnitTestResult'),testName='Class.Fact',testId='a',executionId='b',outcome='Passed')
        ds=ET.SubElement(root,tag('TestDefinitions'));d=ET.SubElement(ds,tag('UnitTest'),id='a')
        e=ET.SubElement(d,tag('Execution'),id='b');ET.SubElement(d,tag('TestMethod'),className='Class',name='Fact')
        summary=ET.SubElement(root,tag('ResultSummary'),outcome='Completed');c=ET.SubElement(summary,tag('Counters'),total='1',executed='1',passed='1')
        for k in ('failed','error','timeout','aborted','inconclusive','passedButRunAborted','notRunnable','notExecuted','disconnected','warning','completed','inProgress','pending'):c.set(k,'0')
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'test.trx'
            def check():ET.ElementTree(root).write(p);return trx(p,{'Class.Fact'})
            self.assertEqual(check(),{'passed':1})
            for node,key,bad in [(r,'outcome','Failed'),(r,'testName','Other.Fact'),(e,'id','stale'),(summary,'outcome','Failed'),(c,'failed','1')]:
                old=node.get(key);node.set(key,bad)
                with self.assertRaises(AssertionError):check()
                node.set(key,old)
            results.append(ET.fromstring(ET.tostring(r)))
            with self.assertRaises(AssertionError):check()

    def test_rust_requires_actual_rows_and_full_scope(self):
        text='test a::case ... ok\n\ntest result: ok. 1 passed; 0 failed; 0 ignored; 0 measured; 0 filtered out; finished in 1s\n'
        self.assertEqual(rust(text,{'a::case'},1),{'passed':1,'ignored':0})
        for bad in [text.replace('1 passed','0 passed'),text.replace('0 filtered','9 filtered'),text.replace('a::case','other'),text+'test a::case ... ok\n',text.replace('... ok','... FAILED'),text.replace('test a::case ... ok\n','')]:
            with self.assertRaises(AssertionError):rust(bad,{'a::case'},1)

    def test_ignored_names_cannot_expand(self):
        text='test a ... ok\ntest known ... ignored\ntest result: ok. 1 passed; 0 failed; 1 ignored; 0 measured; 0 filtered out;\n'
        rust(text,ignored={'known'})
        with self.assertRaises(AssertionError):rust(text,ignored=set())
        with self.assertRaises(AssertionError):rust(text.replace('known','unexpected'),ignored={'known'})

    def test_historical_baseline_identity_paths_bytes_and_signature_binding(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)
            (p/'baseline-commit.txt').write_text('bd5d7dbe1f09bd3ba649e9b1c1928845784e4fe8')
            (p/'baseline-tree.txt').write_text('aff0fc2cf409551b5287c638cefe131a1ddd0e1f')
            identities={}
            for key,blob in [('baseline',b'old'),('candidate',b'new-runtime')]:
                (p/(key+'.dll')).write_bytes(blob)
                identities[key]={'path':str(p/(key+'.dll')),'sha256':hashlib.sha256(blob).hexdigest(),'bytes':len(blob)}
            additions=['T:RyuPy.Runtime.PyRecordsIncrement:1048961:System.Object:',
                       'M:RyuPy.Runtime.PyRecordsIncrement:TryMaterialized:150:[0;0;2](Object,Object)->Boolean:']
            data={'baseline':str(p/'baseline.dll'),'candidate':str(p/'candidate.dll'),
                  'baselineBytes':3,'candidateBytes':11,'signatures':1659,
                  'expected':['historical']+additions,'actual':['historical']+additions,
                  'candidateSignatures':3,'recordsGuardAdditions':additions}
            def check():
                (p/'assembly-identities.json').write_text(json.dumps(identities))
                (p/'baseline-abi.json').write_text(json.dumps(data))
                return baseline(p)
            self.assertEqual(check(),{'historical_signatures':1659,'records_additions':2})
            for key,bad in [('baseline',str(p/'candidate.dll')),('candidate',str(p/'wrong.dll')),
                            ('baselineBytes',11),('candidateBytes',3),('signatures',1658),
                            ('candidateSignatures',4),('actual',['changed']),
                            ('recordsGuardAdditions',additions[:-1])]:
                old=data[key];data[key]=bad
                with self.subTest(key=key),self.assertRaises(AssertionError):check()
                data[key]=old
            for file in ('baseline-commit.txt','baseline-tree.txt'):
                old=(p/file).read_text();(p/file).write_text('0'*40)
                with self.assertRaises(AssertionError):check()
                (p/file).write_text(old)
            for key in ('baseline','candidate'):
                for field,bad in [('path','/wrong/elsewhere.dll'),('sha256','0'*64),('bytes',999)]:
                    old=identities[key][field];identities[key][field]=bad
                    with self.assertRaises(AssertionError):check()
                    identities[key][field]=old
            (p/'candidate.dll').write_bytes(b'old');identities['candidate']=identities['baseline'].copy()
            with self.assertRaises(AssertionError):check()


if __name__=='__main__':unittest.main()
