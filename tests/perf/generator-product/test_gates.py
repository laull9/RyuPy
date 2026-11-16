"""合成门禁正负例，不替代固定Python/C#/Rust/NativeAOT实际运行。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import mir_gate
import baseline
import balanced
import feature_gate
import itertools
import hashlib
from collections import Counter
import semantic
import aot_hotspot_pair
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
        families.update({n:1 for n in runtime_gate.COMPILED | runtime_gate.HISTORICAL})
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
                runtime_gate.check(path,'full');runtime_gate.compiled(path);runtime_gate.historical(path)
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

    def test_fixed_dev_required_before_known(self):
        cp=(0,b'CP output\n',b'');unsupported=baseline.UNSUPPORTED
        self.assertEqual(baseline.classify(cp,unsupported,unsupported,unsupported)['unsupported_cases'],1)
        self.assertEqual(baseline.classify(cp,cp,cp,cp)['unsupported_cases'],0)
        for dev,off,on in [(cp,unsupported,unsupported),(unsupported,cp,unsupported),
                           ((1,b'',b'other'),unsupported,unsupported),(unsupported,unsupported,(1,b'extra',unsupported[2]))]:
            with self.assertRaises(AssertionError):baseline.classify(cp,dev,off,on)

    def test_balanced_order_is_opt_in_and_complete(self):
        orders=aot_hotspot_pair.execution_orders(12,True)[1:]
        self.assertEqual(Counter(orders),Counter({p:2 for p in itertools.permutations(('CP','A','B'))}))
        self.assertEqual(aot_hotspot_pair.execution_orders(2),[('CP','A','B'),('CP','B','A'),('CP','A','B')])
        with self.assertRaises(ValueError):aot_hotspot_pair.execution_orders(10,True)

    def test_cross_module_binds_source_and_distinct_targets(self):
        sources=[];modules=[]
        for index,name in enumerate(('main.py','producer.py','consumer.py')):
            path=semantic.ROOT/'tests/fixtures/generator_sum/cross_module'/name
            sources.append({'module_id':index,'module_name':'__main__' if index == 0 else name[:-3],
                            'logical_source':('entry/' if index == 0 else 'modules/')+name,
                            'compiled_text_sha256':''})
            modules.append({'id':index,'name':'__main__' if index == 0 else name[:-3]})
        refs=[]
        site={'source_method':{'name':'m1_<genexpr>','owner':'Module',
              'signature':'static [System.Runtime]System.Object ([System.Runtime]System.Object, [System.Runtime]System.Object)'}}
        for i in (4,5,6):
            refs.append({'role':'generator_resume','site':site,'target':{'name':f'm1_<genexpr>$state_resume{i}',
                'owner':'Module','signature':'static [System.Runtime]System.Object ([System.Runtime]System.Object)'}})
        for i in (4,6):
            refs.append({'role':'generator_sum_resume','site':site,'target':{'name':f'm1_<genexpr>$state_resume{i}$sum_batch',
                'owner':'Module','signature':site['source_method']['signature']}})
        data={'sources':sources,'modules':modules,'emitted_delegate_references':refs}
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'manifest.json'
            def check(d):path.write_text(json.dumps(d));semantic.roles(path,True,True)
            check(data)
            bad=copy.deepcopy(data);bad['sources'][0]['compiled_text_sha256']='wrong'
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['emitted_delegate_references'][-1]['target']=copy.deepcopy(bad['emitted_delegate_references'][-2]['target'])
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['sources'][0]['logical_source']='wrong/main.py'
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['sources'].pop()
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['modules'].append(copy.deepcopy(bad['modules'][0]))
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['emitted_delegate_references'][-1]['target']['name']='m1_<genexpr>$state_resume5$sum_batch'
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['emitted_delegate_references'][-1]['site']={'source_method':{'name':'wrong'}}
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['emitted_delegate_references'].pop(1)
            with self.assertRaises(AssertionError):check(bad)
            bad=copy.deepcopy(data);bad['modules'][1]['name']='wrong'
            with self.assertRaises(AssertionError):check(bad)

    def test_external_inputs_detect_mutation_and_wrong_command(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp)/'repo';root.mkdir()
            fixture=root/'tests/fixtures/generator_sum/cross_module';fixture.mkdir(parents=True)
            for name in semantic.CROSS_FILES:(fixture/name).write_bytes(name.encode())
            cli=root/'cli';cli.write_bytes(b'fake cli')
            def git(args, **kwargs):
                if args[1]=='show':return Path(root/args[2].split(':',1)[1]).read_bytes()
                return 'fixed-ref\n'
            with patch.object(semantic,'ROOT',root),patch.object(semantic.subprocess,'check_output',side_effect=git):
                folder=Path(temp)/'seal';data=semantic.seal_inputs(folder,cli,'aot')
                cmd={'command':[str(cli),'build',str(fixture/'main.py'),'-o','output','--aot','--native'],'cwd':str(root)}
                (folder/'command.json').write_text(json.dumps(cmd));semantic.verify_inputs(folder,data)
                (fixture/'producer.py').write_bytes(b'tampered')
                with self.assertRaises(AssertionError):semantic.verify_inputs(folder,data)
                (fixture/'producer.py').write_bytes(b'producer.py')
                cmd['command'][2]=str(fixture/'consumer.py');(folder/'command.json').write_text(json.dumps(cmd))
                with self.assertRaises(AssertionError):semantic.verify_inputs(folder,data)
                (folder/'consumer.py').unlink()
                with self.assertRaises(FileNotFoundError):semantic.verify_inputs(folder,data)

    def test_balanced_roles_respect_normalized_arms(self):
        for name in ('generators','genexpr_sum','ordinary_for','delegated_for'):
            for enabled in (False,True):
                hits=[{'role':'generator_sum_resume','target':{'token':1}}] if enabled and name in ('generators','genexpr_sum') else []
                data={'emitted_delegate_references':hits};balanced.batch_roles(data,enabled,name)
                bad=copy.deepcopy(data)
                if hits:bad['emitted_delegate_references']=[]
                else:bad['emitted_delegate_references']=[{'role':'generator_sum_resume','target':{}}]
                with self.assertRaises(AssertionError):balanced.batch_roles(bad,enabled,name)

    def test_actual_feature_resolve(self):
        data={'packages':[{'name':n,'id':n} for n in ('ryupy-driver','ryupy-mir','ryupy-e2e')],
              'resolve':{'nodes':[{'id':n,'features':[],'dependencies':['ryupy-driver'] if n=='ryupy-e2e' else []}
                                  for n in ('ryupy-driver','ryupy-mir','ryupy-e2e')]}}
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'metadata.json';path.write_text(json.dumps(data));feature_gate.verify(path,'off')
            with self.assertRaises(AssertionError):feature_gate.verify(path,'on')
            for n in data['resolve']['nodes'][:2]:n['features']=['generator-sum-experiment']
            path.write_text(json.dumps(data));feature_gate.verify(path,'on')
            with self.assertRaises(AssertionError):feature_gate.verify(path,'off')

    def test_balanced_raw_report_identity_and_order(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp)
            for index in (1,2,3,4):
                for selected,names in balanced.EXPECTED.items():
                    folder=root/f'round-{index}'/selected;folder.mkdir(parents=True);rows=[]
                    for name in names:
                        rows.append({'name':name,'samples':{a:[{'ms':1,'rss':1} for _ in range(12)] for a in ('CP','A','B')},
                                     'build_order':['A','B'],'execution_order':aot_hotspot_pair.execution_orders(12,True)[1:],
                                     'free_bytes_before_build':{a:4*1024**3 for a in ('A','B')},
                                     'stats':{a:{'main_ms':1,'alloc_bytes':1} for a in ('A','B')}})
                        for arm in ('A','B'):
                            m=folder/'manifests'/name/arm/'ryupy-artifact.json';m.parent.mkdir(parents=True)
                            hits=[{'role':'generator_sum_resume','target':{'token':1}}] if arm == ('A' if index%2==0 else 'B') and name in ('generators','genexpr_sum') else []
                            m.write_text(json.dumps({'toolchain':{'sdk_version':'10.0.403','target_framework_major':10},'emitted_delegate_references':hits}))
                    old,new=('on','off') if index%2==0 else ('off','on')
                    (folder/'paired.json').write_text(json.dumps({'old':f'/tmp/frozen-{old}/ryupy','new':f'/tmp/frozen-{new}/ryupy',
                                                                 'runs':12,'warmup':1,'rows':rows}))
            self.assertEqual(len(balanced.verify(root)['results']),4)
            path=root/'round-2/original/paired.json';valid=json.loads(path.read_text())
            for mutate in ('order','identity','samples'):
                bad=copy.deepcopy(valid)
                if mutate=='order':bad['rows'][0]['execution_order'][0]=['CP','A','B']*2
                if mutate=='identity':bad['old']='/tmp/frozen-off/ryupy'
                if mutate=='samples':bad['rows'][0]['samples']['A'].pop()
                path.write_text(json.dumps(bad))
                with self.assertRaises(AssertionError):balanced.verify(root)
            path.write_text(json.dumps(valid))

    def test_actual_abi_artifact_is_required(self):
        with tempfile.TemporaryDirectory() as temp:
            evidence=Path(temp);root=evidence/'check-off';(root/'pe').mkdir(parents=True)
            path=root/'pe/generator-frozen-dev-abi.json'
            frozen=evidence/'frozen-dev-abi';dll=frozen/'build/bin/RyuPy.Runtime/release/RyuPy.Runtime.dll'
            dll.parent.mkdir(parents=True);dll.write_bytes(b'mocked baseline bytes, not PE')
            loaded=root/'pe/generator-loaded-candidate.dll';loaded.write_bytes(b'mocked loaded bytes, not PE')
            additions=[str(i) for i in range(8)];actual=['old']+additions
            data={'commit':'81758bb477729aee4636f2ca6d6b0815b949849a','sdk':'10.0.403',
                  'oldSignatures':1,'candidateSignatures':9,'additions':additions,'removed':[],
                  'added':additions,'expected':actual,'actual':actual,
                  'sha256':hashlib.sha256(dll.read_bytes()).hexdigest(),
                  'candidateSha256':hashlib.sha256(loaded.read_bytes()).hexdigest(),
                  'baseline':str(dll),'baselineManifest':str(frozen/'manifest.json'),'baselineBytes':dll.stat().st_size,
                  'candidate':'/runner/candidate.dll','candidateArchive':loaded.name,'candidateBytes':loaded.stat().st_size}
            manifest={'commit':data['commit'],'sdk':data['sdk'],'configuration':'Release',
                      'dll':str(dll),'sha256':data['sha256'],'bytes':data['baselineBytes']}
            (frozen/'manifest.json').write_text(json.dumps(manifest))
            path.write_text(json.dumps(data));runtime_gate.historical_artifact(root)
            for key,value in [('removed',['old']),('candidateArchive','../../outside'),('baselineBytes',1),('sha256','f'*64)]:
                bad=copy.deepcopy(data);bad[key]=value;path.write_text(json.dumps(bad))
                with self.assertRaises(AssertionError):runtime_gate.historical_artifact(root)
            path.write_text(json.dumps(data));loaded.write_bytes(b'changed')
            with self.assertRaises(AssertionError):runtime_gate.historical_artifact(root)
            path.unlink()
            with self.assertRaises(FileNotFoundError):runtime_gate.historical_artifact(root)

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
