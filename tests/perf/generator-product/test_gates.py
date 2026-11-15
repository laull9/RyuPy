"""合成门禁正负例，不替代固定Python/C#/Rust/NativeAOT实际运行。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import mir_gate
import semantic
import performance_gate
import runtime_gate
import xml.etree.ElementTree as ET
from aot_pair_evidence import cleanup_case, require_space, save_manifest


class Gates(unittest.TestCase):
    def test_runtime_exact_contracts_executions_and_summaries(self):
        ns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010'
        def tag(name):return '{'+ns+'}'+name
        root=ET.Element(tag('TestRun'));results=ET.SubElement(root,tag('Results'))
        definitions=ET.SubElement(root,tag('TestDefinitions'))
        families={runtime_gate.PREFIX+n:c for n,c in runtime_gate.CONTRACTS.items()}
        families.update(runtime_gate.TRACE_CONTRACTS)
        families.update({n:1 for n in runtime_gate.COMPILED})
        for family,count in sorted(families.items()):
            for variant in range(count):
                index=len(results);testid=str(index);execution='run-'+testid
                name=family+(f'(case: {variant})' if count>1 else '')
                ET.SubElement(results,tag('UnitTestResult'),testId=testid,executionId=execution,testName=name,outcome='Passed')
                definition=ET.SubElement(definitions,tag('UnitTest'),id=testid)
                ET.SubElement(definition,tag('Execution'),id=execution)
                cls,method=family.rsplit('.',1)
                ET.SubElement(definition,tag('TestMethod'),className=cls,name=method)
        summary=ET.SubElement(root,tag('ResultSummary'),outcome='Completed')
        ET.SubElement(summary,tag('Counters'),total=str(len(results)),executed=str(len(results)),passed=str(len(results)),notExecuted='0',failed='0')
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'runtime.trx'
            def check(tree):
                ET.ElementTree(tree).write(path)
                runtime_gate.check(path,'full');runtime_gate.compiled(path)
            check(root)
            mutations=[]
            bad=copy.deepcopy(root);bad[0][0].set('executionId','wrong');mutations.append(bad)
            bad=copy.deepcopy(root);bad.append(copy.deepcopy(summary));mutations.append(bad)
            bad=copy.deepcopy(root);bad[2].append(copy.deepcopy(summary[0]));mutations.append(bad)
            bad=copy.deepcopy(root)
            for r in bad[0]:
                if r.get('testName') in runtime_gate.COMPILED:r.set('outcome','NotExecuted');break
            mutations.append(bad)
            bad=copy.deepcopy(root)
            for r in bad[0]:
                if r.get('testName') in runtime_gate.COMPILED:r.set('testName',r.get('testName')+'Wrong');break
            mutations.append(bad)
            for bad in mutations:
                with self.assertRaises(AssertionError):check(bad)

    def test_mir_default_feature_and_missing(self):
        for feature in (False, True):
            names = mir_gate.EXPECTED | (mir_gate.FEATURE if feature else set())
            log = '\n'.join('test '+n+' ... ok' for n in sorted(names))
            log += f'\ntest result: ok. {len(names)} passed; 0 failed; 0 ignored; 0 measured; 1 filtered out;'
            self.assertEqual(mir_gate.validate(log, True, feature)['passed'], len(names))
            with self.assertRaises(AssertionError): mir_gate.validate(log, True, not feature)

    def test_semantics_known_is_not_success_class(self):
        semantic.compare((0,b'ok',b''),(0,b'ok',b''))
        semantic.compare((0,b'False\n',b''),(0,b'True\n',b''),True)
        for actual in ((0,b'False\n',b''),(1,b'True\n',b''),(0,b'True\n',b'noise')):
            with self.assertRaises(AssertionError): semantic.compare((0,b'False\n',b''),actual,True)
        with self.assertRaises(AssertionError): semantic.compare((0,b'False\n',b''),(0,b'True\n',b''))

    def test_roles_actual_emission(self):
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'manifest.json'
            for candidate in (False,True):
                refs=[{'role':'generator_sum_resume'}] if candidate else []
                path.write_text(json.dumps({'emitted_delegate_references':refs}))
                semantic.roles(path,candidate)
                with self.assertRaises(AssertionError): semantic.roles(path,not candidate)

    def test_sdk_and_cleanup(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); case=root/'case'; case.mkdir(); other=root/'other'; other.mkdir()
            exe=case/'app'; raw={'toolchain':{'sdk_version':'11.0.100','target_framework_major':10}}
            (case/'ryupy-artifact.json').write_text(json.dumps(raw))
            with self.assertRaises(AssertionError): save_manifest(exe,other,'A','bad')
            self.assertTrue((other/'bad/A/ryupy-artifact.json').is_file())
            cleanup_case(case,root); self.assertTrue(other.exists())
            with self.assertRaises(AssertionError):cleanup_case(root,root)
            link=root/'link';link.symlink_to(other,target_is_directory=True)
            with self.assertRaises(AssertionError):cleanup_case(link,root)
        with patch('aot_pair_evidence.shutil.disk_usage') as disk:
            disk.return_value.free=1
            with self.assertRaises(RuntimeError):require_space(Path('/tmp'),2)

    def test_performance_complete_and_corruption(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); path=root/'paired.json'
            names={b.name for b in performance_gate.suite.discover(performance_gate.ROOT/'tests/perf/generator_consumers')}
            rows=[]
            for name in names:
                rows.append({'name':name,'samples':{a:[{'ms':1} for _ in range(5)] for a in ('CP','A','B')},
                             'stats':{a:{'main_ms':1,'alloc_bytes':1} for a in ('A','B')},
                             'free_bytes_before_build':{a:4*1024**3 for a in ('A','B')}})
                for arm in ('A','B'):
                    manifest=root/'manifests'/name/arm/'ryupy-artifact.json';manifest.parent.mkdir(parents=True)
                    manifest.write_text(json.dumps({'toolchain':{'sdk_version':'10.0.403','target_framework_major':10}}))
            data={'rows':rows,'runs':5,'warmup':1};path.write_text(json.dumps(data))
            self.assertEqual(performance_gate.verify(path,'generator-consumers'),3)
            bad=copy.deepcopy(data);bad['rows'][0]['samples']['B'].pop();path.write_text(json.dumps(bad))
            with self.assertRaises(AssertionError):performance_gate.verify(path,'generator-consumers')
            data['rows'].pop();path.write_text(json.dumps(data))
            with self.assertRaises(AssertionError):performance_gate.verify(path,'generator-consumers')


if __name__=='__main__':unittest.main()
