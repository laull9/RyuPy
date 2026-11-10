"""新窄门的纯控制测试；只合成原件与PE读出，不执行CP产品、Rust、SDK或下载。"""
import copy
import hashlib
import os
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest import mock
import keyerror_construction_contract as c
import keyerror_construction_validation as gate
import decimal_percent_contract as old_contract
import decimal_percent_validation as old_gate
import decimal_percent_selftest as old_tests
import textwrap_dedent_selftest as selector
from re_scanner_selftest import write_process


# 保真收录109原控制器，不依赖快照CI不可见的本地Git历史；不得换成当前实现自比。
DECIMAL_BASELINE = Path(__file__).with_name('baselines') / 'decimal_percent_validation_109ac7b.py.txt'
DECIMAL_BASELINE_SHA256 = 'feb781e33eb4137cd004c8f0ea906196be7ec20b173009afa567935b6fd5d4b5'


def decimal_baseline_source(path=DECIMAL_BASELINE):
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != DECIMAL_BASELINE_SHA256:
        raise ValueError('109历史控制器原件缺失或改变，不能跳过或替换基线')
    return raw


class KeyErrorControlTests(unittest.TestCase):
    def test_fixed_graphs_processes_facts_and_sources(self):
        data = c.load_manifest()
        self.assertEqual(len(data['sources']), 5)
        self.assertEqual(len(data['csharp']['facts']), 5)
        self.assertEqual([len(data['rust'][g]) for g in ('mir', 'codegen')], [5, 1])
        self.assertEqual(sum(len(g['cases']) for g in data['graphs']) * 4, 20)
        for graph in data['graphs']:
            for case in c.cases(graph):
                self.assertEqual(case['instance_modules'], ['owner'] if graph['id'] == 'default-owner' else [])
                self.assertTrue(case['cp'])

    def test_exact_selector_keeps_all_old_independent_jobs(self):
        _, values, active = selector.select(c.TITLE)
        self.assertEqual(active, selector.ORDINARY | {'distribution-metadata', 'textwrap-dedent', 're-scanner', 'decimal-percent', 'keyerror-construction'})
        self.assertEqual(values['keyerror'], 'true')
        _, blocks = selector.jobs()
        for name, digest in selector.DATA['unchanged_job_blocks'].items():
            self.assertEqual(hashlib.sha256(blocks[name].encode()).hexdigest(), digest)
        self.assertIn('    needs: select-mode\n', blocks['keyerror-construction'])
        for title in (c.TITLE.upper(), c.TITLE + '\n', c.TITLE.replace('001', '002')):
            self.assertEqual(selector.select(title)[2], selector.ORDINARY)
        for options in ({'event':'workflow_dispatch'}, {'attempt':'2'}):
            self.assertEqual(selector.select(c.TITLE, **options)[2], selector.ORDINARY)
        self.assertNotIn('keyerror', selector.select(old_contract.TITLE)[1])
        env = dict(RUNNER_ENVIRONMENT='github-hosted', GITHUB_ACTIONS='true', GITHUB_EVENT_NAME='push',
            GITHUB_RUN_ATTEMPT='1', GITHUB_REF=selector.c.old.shared.REF, GITHUB_SHA='a'*40,
            RYUPY_VALIDATION_MODE='distribution-metadata')
        event = dict(ref=env['GITHUB_REF'], after=env['GITHUB_SHA'], head_commit=dict(message=c.TITLE,id=env['GITHUB_SHA']))
        before = copy.deepcopy(event)
        for checker in (c.check_context, old_contract.check_context, old_contract.shared.check_context, selector.c.check_context, selector.c.metadata.check_context):
            checker(env, event); self.assertEqual(event, before)
        for value in ('workflow_dispatch', 'pull_request'):
            with self.assertRaises(ValueError): c.check_context(dict(env,GITHUB_EVENT_NAME=value),event)

    def test_direct_source_rejects_pre_scan_binding_and_import_alias(self):
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'entry.py'
            path.write_text("e = KeyError('x')\nsaved = KeyError\nclass E(KeyError):\n    pass\n")
            c.no_binding(path)
            for source in ("KeyError = 1", "def f():\n    global KeyError", "def KeyError():\n    pass", "from a import b as KeyError", "def f(KeyError):\n    pass"):
                path.write_text(source)
                with self.assertRaises(ValueError): c.no_binding(path)

    def plan(self, case):
        return dict(compiled_feature=False,session_policy='explicit-only',modules=[dict(module=name,compiled=True,
            execution='InstanceGlobals' if name=='owner' else 'LegacySingleton',reason='explicit-instance' if name=='owner' else 'policy-disabled',
            ordinary_name_handlers=name=='owner',relative_from_import=name=='owner') for name in case['modules']])

    def test_default_cli_and_exact_owner_plan_no_pilot_escape(self):
        with tempfile.TemporaryDirectory() as temp, mock.patch.object(old_gate, 'ROOT', Path(temp)):
            runner=gate.Runner()
            self.assertEqual(runner.compiler_argv(), ['cargo','build','--locked','-p','ryupy-cli'])
            runner.cli=Path(temp)/'cli'
            for graph in runner.data['graphs']:
                case=c.cases(graph)[0]; plan=self.plan(case); path=Path(temp)/'plan.json'; old_gate.save(path,plan)
                c.verify_plan(path,case,True)
                for aot in (False,True):
                    argv=runner.build_argv(case,Path(temp)/'target',aot)
                    self.assertEqual(argv.count('--instance-module'), int(graph['id']=='default-owner'))
                    self.assertEqual('--aot' in argv,aot)
                if graph['id']=='default-owner':
                    for change in ('consumer-instance','owner-legacy','pilot'):
                        wrong=copy.deepcopy(plan)
                        if change=='pilot': wrong['compiled_feature']=True
                        elif change=='owner-legacy': wrong['modules'][1]['execution']='LegacySingleton'
                        else: wrong['modules'][2]['execution']='InstanceGlobals'
                        old_gate.save(path,wrong)
                        with self.assertRaises(ValueError): c.verify_plan(path,case,True)

    def test_real_pe_calls_not_unused_member_refs_or_owner_substitute(self):
        data=c.parse(c.MANIFEST.read_bytes()); case=c.cases(data['graphs'][2])[0]; plan=self.plan(case)
        with tempfile.TemporaryDirectory() as temp:
            pe=Path(temp)/'entry.dll'; pe.write_bytes(b'fake-pe-only')
            def method(name,target,n=1):
                return dict(Name='RyuPy.Generated.Module::'+name+'[0;0;0]()->Object',IL=[dict(Op='call',Arg=target,Offset=i) for i in range(n)])
            raw=dict(Path=str(pe),Sha256=c.sha(pe).upper(),Methods=[method('m2_call',c.ABI),method('m2_save',c.ABI),
                 method('m2_evaluate',c.ABI),method('m1_old','[RyuPy.Runtime]RyuPy.Runtime.PyModuleInstances::LoadName[0;0;2](Object,String)->Object')])
            self.assertTrue(c.pe_calls(raw,pe,plan,case)['passed'])
            for kind in ('missing','owner','signature','hash'):
                wrong=copy.deepcopy(raw)
                if kind=='missing':wrong['Methods'].pop(0)
                elif kind=='owner':wrong['Methods'][0]['Name']=wrong['Methods'][0]['Name'].replace('m2_','m1_')
                elif kind=='signature':wrong['Methods'][0]['IL'][0]['Arg']=c.ABI.replace('(String)','(Object)')
                else:wrong['Sha256']='0'*64
                with self.assertRaises(ValueError):c.pe_calls(wrong,pe,plan,case)

    def test_true_exit_stderr_and_complete_frames_are_mandatory(self):
        graph=c.parse(c.MANIFEST.read_bytes())['graphs'][0];source=c.ROOT/graph['source']
        for row in graph['cases'][1:]:
            with tempfile.TemporaryDirectory() as temp:
                root=Path(temp);argv=['fake-product',*row['argv']]
                write_process(root,'run',argv,b'',old_tests.trace(row,source),exit=1)
                self.assertTrue(c.verify_row(root/'run',argv,row,source)['passed'])
                for suffix in (b'noise\n',b'The above exception was the direct cause of the following exception:\n'):
                    write_process(root,'run',argv,b'',old_tests.trace(row,source)+suffix,exit=1)
                    with self.assertRaises(ValueError):c.verify_row(root/'run',argv,row,source)
                write_process(root,'run',argv,b'',old_tests.trace(row,source),exit=0)
                with self.assertRaises(ValueError):c.verify_row(root/'run',argv,row,source)

    def test_original_decimal_default_is_equivalent_to_109_source(self):
        # 只加载已确认基线的Python控制器，不执行产品；逐对象比较默认row、argv和阶段计划。
        raw=decimal_baseline_source()
        namespace={'__name__':'decimal_baseline_control','__file__':str(c.ROOT/'scripts/decimal_percent_validation.py')}
        exec(compile(raw,'109-decimal-control','exec'),namespace)
        with tempfile.TemporaryDirectory() as temp,mock.patch.object(old_gate,'ROOT',Path(temp)):
            namespace['ROOT']=Path(temp)
            baseline=namespace['Runner']();current=old_gate.Runner(resume=True)
            self.assertEqual(baseline.data,current.data);self.assertEqual(baseline.result,current.result)
            self.assertEqual(namespace['stages'](baseline.data),old_gate.stages(current.data))
            for runner in (baseline,current):runner.python=Path('/fixed/python');runner.dotnet=Path('/fixed/dotnet');runner.cli=Path('/fixed/cli')
            self.assertEqual(baseline.test_command(),current.test_command())
            self.assertEqual(current.compiler_argv(),['cargo','build','--locked','-p','ryupy-cli','--features',old_gate.helper.old.shared.FEATURE])
            for graph in baseline.data['graphs']:
                self.assertEqual(baseline.graph_target(graph),current.graph_target(graph))
                for case in old_contract.cases(graph):
                    for aot in (False,True):
                        self.assertEqual(old_gate.helper.build_argv(baseline,case,baseline.graph_target(graph),aot),current.build_argv(case,current.graph_target(graph),aot))
                    self.assertEqual(current.cp_argv(case,Path(case['source'])),[baseline.python,'-I','-B',Path(case['source']),*case['argv']])
            for row in baseline.data['old_cases']:self.assertEqual(baseline.old_compile_argv(row),current.old_compile_argv(row))

    def test_history_baseline_missing_or_changed_is_rejected_without_fallback(self):
        raw = decimal_baseline_source()
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'baseline.py.txt'
            with self.assertRaises(FileNotFoundError): decimal_baseline_source(path)
            for wrong in (raw + b'\n# changed\n', raw[:-1], (c.ROOT / 'scripts/decimal_percent_validation.py').read_bytes()):
                path.write_bytes(wrong)
                with self.assertRaises(ValueError): decimal_baseline_source(path)
            path.write_bytes(raw)
            self.assertEqual(decimal_baseline_source(path), raw)

    def fixture(self, root, compile_failed=False, csharp_failed=False):
        with mock.patch.object(old_gate, 'ROOT', root): runner = gate.Runner()
        runner.before = dict(head='a'*40, tree='b'*40)
        runner.python, runner.dotnet = root/'python', root/'dotnet'
        runner.cli, runner.runtime = runner.out/'tools/ryupy', runner.out/'tools/RyuPy.Runtime.dll'
        for path in (runner.python, runner.dotnet, runner.cli, runner.runtime, runner.reader,
                     runner.out/'managed/RyuPy.Runtime.dll', runner.out/'managed/RyuPy.Runtime.Tests.dll'):
            path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(b'fake')
        runner.data['cp_sha256'] = c.sha(runner.python)
        for name in runner.data['sources']:
            path=runner.inputs_dir/name;path.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(c.ROOT/name,path)
        runner.input_inventories={str(runner.inputs_dir):old_gate.base.inventory(runner.inputs_dir)}
        runner.environment=lambda:setattr(runner,'env',{'test-only':'1'})
        runner.environment();runner.identity=lambda label:runner.before;runner.barrier=lambda *a,**k:None
        runner.test_assemblies=lambda:None;runner.persist=lambda:None
        runner.fresh_identity=lambda:dict(head='a'*40,tree='b'*40,run_id='1',run_attempt='1',manifest_sha256='fake',sources=runner.data['sources'],tools={})
        commands={'csharp-build':[runner.dotnet,'build',old_gate.ROOT/'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj','-c','Release','-nologo'],
                  'compiler-build':runner.compiler_argv(),'keyerror-csharp-tests':runner.test_command(),'pe-reader-build':runner.reader_argv()}
        helper=old_tests.PercentControlTests()
        for graph in runner.data['graphs']:
            case=c.cases(graph)[0];target=runner.graph_target(graph);target.mkdir(parents=True)
            for name in ('entry.dll','RyuPy.Runtime.dll'):(target/name).write_bytes(b'fake')
            plan=self.plan(case);old_gate.save(target/'module-environment.json',plan)
            commands[graph['id']+'-compile-command']=runner.build_argv(case,target)
            for row in graph['cases']:
                source=runner.inputs_dir/graph['source']
                helper.fake_run(runner,row['id']+'-cp-run',runner.cp_argv(row,source),row,source)
                if not (compile_failed and graph['id']=='direct'):
                    helper.fake_run(runner,row['id']+'-jit-run',[runner.dotnet,target/'entry.dll',*row['argv']],row,source)
            if not (compile_failed and graph['id']=='direct'):
                count={'direct':17,'shadow':0,'default-owner':3}[graph['id']]
                prefix='m2_call' if graph['id']=='default-owner' else 'm0___module_main'
                methods=[dict(Name='RyuPy.Generated.Module::'+prefix,IL=[dict(Op='call',Arg=c.ABI,Offset=i) for i in range(count)])]
                if graph['id']=='default-owner':methods.append(dict(Name='Module::m1_old',IL=[dict(Op='call',Arg='PyModuleInstances::LoadName',Offset=0)]))
                raw=dict(Path=str((target/'entry.dll').resolve()),Sha256=c.sha(target/'entry.dll'),Methods=methods)
                import json
                write_process(runner.out,graph['id']+'-pe',[runner.dotnet,runner.reader,target/'entry.dll'],json.dumps(raw).encode())
        for name,argv in commands.items():
            failed=(compile_failed and name=='direct-compile-command') or (csharp_failed and name=='keyerror-csharp-tests')
            write_process(runner.out,name,argv,exit=int(failed))
        for group in ('mir','codegen'):
            names=runner.data['rust'][group]
            stdout=''.join('test '+name+' ... ok\n' for name in names)+f'test result: ok. {len(names)} passed; 0 failed; 0 ignored; 0 measured; 0 filtered out\n'
            write_process(runner.out,'rust-'+group,runner.rust_argv(group),stdout.encode())
        trx=runner.out/'trx/result.trx';trx.parent.mkdir()
        trx.write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'+''.join(
            '<UnitTestResult testName="RyuPy.Runtime.Tests.'+name+'" outcome="Passed"/>' for name in runner.data['csharp']['facts'])+'</TestRun>')
        checkpoint=dict(schema_version=1,identity=runner.fresh_identity(),runs=runner.run_records,
            files={str(p.relative_to(runner.out)):c.sha(p) for p in runner.out.rglob('*') if p.is_file()})
        state=dict(schema_version=1,before=runner.before,frozen={},inventories=runner.input_inventories,isolation={},environment=runner.env,labels=[],checkpoint=checkpoint)
        return runner,state

    def resume(self, runner, state):
        old_gate.save(runner.out/'state.json',state);(runner.out/'event.json').write_text('{}')
        env=dict(PYTHON=str(runner.python),DOTNET=str(runner.dotnet),GITHUB_EVENT_PATH=str(runner.out/'event.json'))
        with mock.patch.dict(os.environ,env),mock.patch.object(c,'check_context'),mock.patch.object(c,'load_manifest'), \
             mock.patch.object(old_gate.sys,'executable',str(runner.python)),mock.patch.object(old_gate.sys,'version_info',(3,12,13)):
            return runner.execute('native')

    def test_new_resume_recomputes_csharp_and_three_native_after_local_failures(self):
        for fail in (False,True):
            with self.subTest(fail=fail),tempfile.TemporaryDirectory() as temp:
                runner,state=self.fixture(Path(temp),compile_failed=fail,csharp_failed=fail)
                visited=[];runner.native_graph=lambda graph:visited.append(graph['id'])
                self.assertEqual(self.resume(runner,state),int(fail))
                self.assertEqual(visited,['direct','shadow','default-owner'])
                self.assertEqual(runner.result['stages']['csharp-tests']['passed'],not fail)
                self.assertTrue(runner.result['stages']['default-owner-structure']['passed'])
                if fail:self.assertEqual(runner.result['stages']['direct-all-jit']['status'],'blocked')

    def test_new_checkpoint_missing_process_cannot_silently_skip_native_promises(self):
        with tempfile.TemporaryDirectory() as temp:
            runner,state=self.fixture(Path(temp))
            del state['checkpoint']['runs']['default-owner-all-jit-run']
            visited=[];runner.native_graph=lambda graph:visited.append(graph['id'])
            self.assertEqual(self.resume(runner,state),1)
            self.assertEqual(visited,[]);self.assertTrue(runner.identity_failed)

    def test_original_decimal_saved_trx_success_path_remains_successful(self):
        with tempfile.TemporaryDirectory() as temp:
            runner,_=old_tests.PercentControlTests().fixture(Path(temp))
            self.assertTrue(runner.check_saved_tests()['all_passed'])

    def test_reuses_original_native_gate_and_keeps_failed_uploads(self):
        self.assertEqual(gate.native.__module__,'textwrap_dedent_native')
        self.assertIs(gate.native.__defaults__[0],c.shared)
        workflow=(c.ROOT/'.github/workflows/keyerror-construction.yml').read_text()
        self.assertIn("!cancelled() && hashFiles('target/keyerror-construction-evidence/state.json') != ''",workflow)
        self.assertIn('**/native-dist.tar.gz',workflow)
        self.assertLess(workflow.index('keyerror-checkpoint-'),workflow.index('--phase native'))
        self.assertNotIn('continue-on-error',workflow)


if __name__=='__main__':unittest.main()
