"""证据门负例；合成数据不代替真实C#/Rust或历史DLL。"""
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
from check_records_guard import NS, rust, trx


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


if __name__=='__main__':unittest.main()
