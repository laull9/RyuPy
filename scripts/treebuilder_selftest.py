"""TreeBuilder纯控制反例：不启动CP/产品/构建，错误原件与身份漂移不得变通过。"""
import ast
import copy
import hashlib
import json
import os
from pathlib import Path
from types import SimpleNamespace
import tempfile
import tarfile
import textwrap
import unittest
from unittest.mock import patch
import treebuilder_contract as c
import treebuilder_validation as v
import keyerror_construction_contract as keyerror
import decimal_percent_contract as percent
import re_scanner_contract as scanner


class ContractTests(unittest.TestCase):
    def setUp(self):
        self.data = c.load_manifest()

    def test_all_frozen_sources_and_cp_streams(self):
        c.qualified_oracles(self.data)
        self.assertEqual((c.ROOT / self.data['oracles']['markdown-full']['stdout_file']).stat().st_size, 77)
        self.assertEqual((c.ROOT / self.data['oracles']['markdown-html']['stdout_file']).stat().st_size, 102)
        self.assertEqual((c.ROOT / self.data['oracles']['all']['stdout_file']).stat().st_size, 1467)
        plan = self.data['process_plan']
        self.assertEqual((plan['core']['cp'], plan['core']['jit']), (7, 7))
        self.assertEqual(plan['totals'], dict(cp=10, jit=11, native_core=2, native_markdown_conditional=2, aot_build=2))
        for group in c.GROUPS:
            self.assertEqual(self.data['oracles'][group]['source_sha256'], self.data['sources'][c.FIXTURE + 'core.py'])

    def test_manifest_config_source_and_oracle_mutations(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'manifest.json'
            for key, value in [('default_project_delta', 1), ('includes', []), ('groups', ['all']), ('cp_sha256', '0' * 64)]:
                bad = copy.deepcopy(self.data); bad[key] = value
                path.write_text(json.dumps(bad))
                with self.subTest(key=key), self.assertRaises(ValueError): c.load_manifest(path=path)
            bad = copy.deepcopy(self.data); bad['sources'][c.ORIGINAL] = '0' * 64
            path.write_text(json.dumps(bad))
            with self.assertRaises(ValueError): c.load_manifest(path=path)
        for mutation in [('source_sha256', '0' * 64), ('status', 'predicted'), ('exit', 1), ('failure', 'timeout')]:
            bad = copy.deepcopy(self.data); bad['oracles']['core'][mutation[0]] = mutation[1]
            with self.subTest(mutation=mutation), self.assertRaises(ValueError): c.qualified_oracles(bad)

    def test_all_is_exact_six_functions_no_extra_eval_or_copy(self):
        tree = ast.parse((c.ROOT / (c.FIXTURE + 'core.py')).read_text())
        calls = [n.body[0].value.func.id for n in tree.body if isinstance(n, ast.If)]
        self.assertEqual(calls, list(c.GROUPS[:-1]))
        self.assertFalse(any(isinstance(n, ast.Call) and isinstance(n.func, ast.Name) and
                             n.func.id in ('exec', 'eval', 'compile') for n in ast.walk(tree)))
        body = next(n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name == 'factories')
        self.assertTrue(any(isinstance(n, ast.Try) and n.finalbody for n in ast.walk(body)))

    def test_native_independent_of_jit_but_not_identity(self):
        stages = {n: dict(passed=True) for n in ('identity', 'tools', 'sourceprep', 'core-compile', 'all-cp')}
        stages['core-jit'] = dict(passed=False, status='failed')
        self.assertTrue(c.native_eligible(stages))
        for key in ('identity', 'tools', 'sourceprep', 'core-compile', 'all-cp'):
            bad = copy.deepcopy(stages); bad[key]['passed'] = False
            with self.subTest(key=key): self.assertFalse(c.native_eligible(bad))

    def test_real_stage_graph_retains_jit_red_and_runs_independent_native(self):
        runner=object.__new__(v.Runner)
        runner.data=dict(subclass_messages={},boundary_stdout='NI complete\n')
        runner.inputs_dir=Path('/inputs');runner.identity_failed=False
        runner.result=dict(passed=False,stages={})
        def phase(name,operation):
            try:
                operation();runner.result['stages'][name]=dict(passed=True,status='passed')
            except ValueError as error:
                runner.result['stages'][name]=dict(passed=False,status='failed',error=str(error))
            return runner.result['stages'][name]['passed']
        runner.phase=phase;runner.audit=lambda fn:fn();runner.golden=lambda name:b'complete\n'
        for name in ('identities','build_tools','prepare_sources','business','persist'):
            setattr(runner,name,lambda:None)
        runner.types=lambda *args:None;runner.cp=lambda *args:None;runner.compile=lambda *args:None
        runner.barrier=lambda *args:None
        def jit(name,*args,**kwargs):
            if name=='core':raise ValueError('actual JIT mismatch')
        runner.jit=jit
        def rejected():raise ValueError('actual Markdown AOT E0100')
        runner.markdown_aot=rejected
        with patch.object(v,'native',return_value=dict(cp_equivalent=True)) as product:
            self.assertEqual(v.Runner.execute(runner),1)
            product.assert_called_once()
        self.assertFalse(runner.result['stages']['core-jit']['passed'])
        self.assertTrue(runner.result['stages']['core-native']['passed'])
        self.assertFalse(runner.result['stages']['markdown-aot']['passed'])
        self.assertFalse(runner.result['passed'])

    def test_complete_26_anchors_not_old_missing_resources(self):
        request = c.resource_request(self.data)
        self.assertEqual(len(request['modules']), 26)
        self.assertEqual({x['module'] for x in request['modules']}, set(self.data['markdown_modules']) - {'__main__'})
        self.assertIn('html', {x['module'] for x in request['modules']})
        self.assertIn('markdown.extensions', {x['module'] for x in request['modules']})
        for change in ('missing', 'duplicate', 'payload'):
            bad = copy.deepcopy(self.data)
            if change == 'missing': bad['markdown_resources']['modules'].pop()
            if change == 'duplicate': bad['markdown_resources']['modules'][-1] = bad['markdown_resources']['modules'][0]
            if change == 'payload': bad['markdown_resources']['modules'][0]['files'] = ['source.py']
            with self.subTest(change=change), self.assertRaises(ValueError): c.resource_request(bad)

    def test_real_business_argv_is_same_plus_aot_resources(self):
        runner = SimpleNamespace(cli=Path('/tool/ryupy'), inputs_dir=Path('/inputs'))
        case = dict(source='markdown_sample.py', business=True)
        jit = list(map(str, c.build_argv(runner, case, Path('/jit'))))
        aot = list(map(str, c.build_argv(runner, case, Path('/jit'), True)))
        self.assertEqual(aot, jit + ['--aot', '--resources', '/inputs/markdown-resources.json'])
        self.assertEqual([jit[i+1] for i,x in enumerate(jit) if x == '--include-module'], self.data['includes'])
        self.assertEqual([jit[i+1] for i,x in enumerate(jit) if x == '--instance-module'], ['html.parser'])
        self.assertNotIn('--runtime', jit)
        self.assertEqual(jit[-2:], ['--distribution-metadata', '/inputs/metadata-request.json'])

    def markdown_archive_runner(self, root, code, failure=None, count=1):
        # 只合成发布原流及小文件；真实旧归档/原流验证照跑，不启动CLI/SDK/产品。
        runner = object.__new__(v.Runner)
        runner.out = root; runner.inputs_dir = root / 'inputs'; runner.inputs_dir.mkdir()
        (runner.inputs_dir / 'markdown-resources.json').write_text('{}')
        runner.cli = Path('/tool/ryupy'); runner.env = {}; runner.frozen = {}
        runner.audit = lambda operation: operation()
        target = root / 'markdown-aot-build'
        def command(name, argv, timeout, env):
            for index in range(count):
                dist = target / ('markdown_sample.dist' if index == 0 else 'other.dist')
                dist.mkdir(parents=True)
                (dist / 'markdown_sample').write_bytes(b'synthetic ELF bytes ' + str(index).encode())
                (dist / 'ryupy-artifact.json').write_bytes(b'invalid manifest retained')
            (target / 'publish.stderr.log').write_bytes(b'synthetic SDK diagnostic')
            prefix, _, record = self.make_record(root, stdout=b'', stderr=b'incomplete layout report', code=code)
            record.update(argv=list(map(str, argv)), failure=failure)
            prefix.with_suffix('.json').write_text(json.dumps(record))
            prefix.with_suffix('.argv.json').write_text(json.dumps(dict(argv=record['argv'])))
            for suffix in ('json', 'argv.json', 'stdout', 'stderr'):
                prefix.with_suffix('.' + suffix).rename(root / (name + '.' + suffix))
            return record
        runner.command = command
        def barrier(label):
            if label == 'markdown-aot-after':
                self.assertTrue((root / 'raw-native-dist.json').is_file())
        runner.barrier = barrier
        return runner

    def assert_markdown_raw_archives(self, runner, count):
        records = json.loads((runner.out / 'raw-native-dist.json').read_text())
        self.assertEqual(len(records), count)
        for record in records:
            archive = Path(record['archive']); dist = Path(record['path'])
            self.assertEqual(record['sha256'], c.sha(archive))
            self.assertEqual(runner.frozen[str(archive)], c.sha(archive))
            with tarfile.open(archive) as tar:
                for name in ('markdown_sample', 'ryupy-artifact.json'):
                    self.assertEqual(tar.extractfile(dist.name + '/' + name).read(), (dist / name).read_bytes())
        self.assertEqual((runner.out / 'sdk-evidence/publish.stderr.log').read_bytes(), b'synthetic SDK diagnostic')

    def test_markdown_failed_timeout_or_bad_layout_preserves_dist_first(self):
        for code, failure, count in [(1, None, 1), (-9, 'timeout', 1), (0, None, 1), (1, None, 2)]:
            with self.subTest(code=code, failure=failure, count=count), tempfile.TemporaryDirectory() as temp:
                runner = self.markdown_archive_runner(Path(temp), code, failure, count)
                expected = ('不是允许的完整自然退出' if failure else
                            '完整Markdown AOT真实失败保红' if code else '必须报告唯一 managed/native')
                with patch.object(v.old, 'preserve_dist', wraps=v.old.preserve_dist) as archive:
                    with self.assertRaisesRegex((ValueError, AssertionError), expected):
                        runner.markdown_aot()
                    self.assertEqual(archive.call_count, count)
                self.assert_markdown_raw_archives(runner, count)
                if code != 0:
                    self.assertFalse((runner.out / 'markdown-native-dist-files.json').exists())

    def test_markdown_success_requires_unique_archived_identity_without_repacking(self):
        for count, matches in [(1, True), (1, False), (2, True)]:
            with self.subTest(count=count, matches=matches), tempfile.TemporaryDirectory() as temp:
                runner = self.markdown_archive_runner(Path(temp), 0, count=count)
                dist = runner.out / 'markdown-aot-build/markdown_sample.dist'
                wrong = runner.out / 'unrelated'; wrong.mkdir()
                layout = SimpleNamespace(dist=dist if matches else wrong,
                                         managed=runner.out / 'managed/markdown_sample.dll')
                expected = 'stop after archive identity' if count == 1 and matches else '真实reported dist'
                with patch.object(v.old, 'reported_aot', return_value=layout), \
                     patch.object(v.old, 'preserve_dist', wraps=v.old.preserve_dist) as archive, \
                     patch.object(v.base.product.contract, 'audit_pe', side_effect=ValueError('stop after archive identity')) as audit:
                    with self.assertRaisesRegex(ValueError, expected): runner.markdown_aot()
                    self.assertEqual(archive.call_count, count)
                    self.assertEqual(audit.call_count, int(count == 1 and matches))
                self.assert_markdown_raw_archives(runner, count)

    def test_actual_module_plan_not_reconstructed_from_argv(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'plan.json'; path.write_text(json.dumps(self.data['markdown_plan']))
            case = dict(business=True, plan=self.data['markdown_plan'])
            c.verify_plan(path, case)
            for field, value in [('compiled_feature', False), ('session_policy', 'explicit-only')]:
                bad = copy.deepcopy(self.data['markdown_plan']); bad[field] = value; path.write_text(json.dumps(bad))
                with self.assertRaises(ValueError): c.verify_plan(path, case)
            bad = copy.deepcopy(self.data['markdown_plan']);bad['modules'][4]['execution'] = 'LegacySingleton'
            path.write_text(json.dumps(bad))
            with self.assertRaises(ValueError): c.verify_plan(path, case)

    def make_record(self, root, stdout=b'complete\n', stderr=b'', code=0):
        prefix = root / 'run'; argv = ['/tool/program', 'all']
        record = dict(argv=argv, exit=code, failure=None, raw_streams_complete=True,
            streams={n: dict(bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
                     for n,raw in [('stdout',stdout),('stderr',stderr)]})
        for suffix, raw in [('stdout',stdout),('stderr',stderr)]: prefix.with_suffix('.'+suffix).write_bytes(raw)
        prefix.with_suffix('.json').write_text(json.dumps(record))
        prefix.with_suffix('.argv.json').write_text(json.dumps(dict(argv=argv)))
        return prefix, argv, record

    def test_strict_complete_stream_exit_and_identity(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); prefix, argv, record = self.make_record(root)
            c.positive(prefix, argv, b'complete\n')
            for field,value in [('exit',-9),('exit',None),('exit',1),('failure','timeout'),('raw_streams_complete',False),('argv',['fake'])]:
                bad=copy.deepcopy(record);bad[field]=value;prefix.with_suffix('.json').write_text(json.dumps(bad))
                with self.subTest(field=field,value=value),self.assertRaises(ValueError):c.positive(prefix,argv,b'complete\n')
            self.make_record(root,stderr=b'warning')
            with self.assertRaises(ValueError): c.positive(prefix,argv,b'complete\n')
            self.make_record(root,stdout=b'complete\nextra\n')
            with self.assertRaises(ValueError): c.positive(prefix,argv,b'complete\n')
            self.make_record(root,stdout=b'')
            with self.assertRaises(ValueError): c.positive(prefix,argv,b'')
            self.make_record(root);prefix.with_suffix('.stdout').write_bytes(b'changed')
            with self.assertRaises(ValueError): c.positive(prefix,argv,b'complete\n')
            self.make_record(root);prefix.with_suffix('.stderr').unlink()
            with self.assertRaises(FileNotFoundError): c.positive(prefix,argv,b'complete\n')

    def test_exact_runtime_guard_marker_not_compile_failure(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix,argv,_=self.make_record(Path(temp),stderr=b'[E0100] irrelevant',code=1)
            with self.assertRaises(ValueError): c.positive(prefix,argv,self.data['boundary_stdout'].encode())
        self.assertIn('argument direct\nNI syntax-direct-tuple',self.data['boundary_stdout'])
        self.assertIn('argument alias\nNI syntax-alias-tuple',self.data['boundary_stdout'])
        self.assertIn('argument direct-keyword\ndirect-keyword SyntaxError() takes no keyword arguments',self.data['boundary_stdout'])
        self.assertTrue(self.data['boundary_stdout'].endswith('boundaries complete\n'))

    def test_subclass_must_reach_correct_hir_guard(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source=Path('subclass_syntax.py');message=self.data['subclass_messages']['syntax']
            text=('[E0100] Error: '+message+'\nsubclass_syntax.py:2:13\n').encode()
            prefix,argv,_=self.make_record(root,stdout=b'',stderr=text,code=1)
            c.subclass_rejection(prefix,argv,source,message)
            for wrong in [text+b'[E0100] another',b'[E0100] UnknownName',text.replace(message.encode(),b'other')]:
                prefix,argv,_=self.make_record(root,stdout=b'',stderr=wrong,code=1)
                with self.assertRaises(ValueError):c.subclass_rejection(prefix,argv,source,message)

    def context(self,title):
        env=dict(RUNNER_ENVIRONMENT='github-hosted',GITHUB_ACTIONS='true',GITHUB_EVENT_NAME='push',
                 GITHUB_RUN_ATTEMPT='1',GITHUB_REF=c.shared.old.shared.REF,GITHUB_SHA='a'*40,
                 RYUPY_VALIDATION_MODE='distribution-metadata')
        event=dict(ref=env['GITHUB_REF'],after=env['GITHUB_SHA'],head_commit=dict(message=title,id=env['GITHUB_SHA']))
        return env,event

    def test_all_context_consumers_accept_exact_new_only(self):
        consumers=[c.check_context,keyerror.check_context,percent.check_context,scanner.check_context,
                   c.shared.check_context,v.base.check_context]
        for check in consumers:
            with self.subTest(check=check.__module__):
                check(*self.context(c.TITLE))
                for title in [c.TITLE+'\n',c.TITLE.upper(),'validation: treebuilder-state-002']:
                    with self.assertRaises(ValueError):check(*self.context(title))
        # 旧标题逐项保持原接受语义，不用新标题替换旧值。
        for check,title in [(keyerror.check_context,keyerror.TITLE),(percent.check_context,percent.TITLE),
                            (scanner.check_context,scanner.TITLE),(c.shared.check_context,c.shared.TITLE),
                            (v.base.check_context,v.base.DATA['title'])]:check(*self.context(title))

    def test_workflow_selector_and_old_dependency_graph(self):
        text=(c.ROOT/'.github/workflows/wcwidth-legacy-aot.yml').read_text()
        selector=text.split("python3 - <<'PY'\n",1)[1].split('\n          PY',1)[0]
        with tempfile.TemporaryDirectory() as temp:
            output=Path(temp)/'output';event_file=Path(temp)/'event'
            for title,new in [(c.TITLE,True),(keyerror.TITLE,False),(c.TITLE.upper(),False)]:
                env,event=self.context(title);env.update(GITHUB_EVENT_PATH=str(event_file),GITHUB_OUTPUT=str(output))
                event_file.write_text(json.dumps(event));output.write_text('')
                with patch.dict(os.environ,env,clear=True):exec(compile(textwrap.dedent(selector),'<selector>','exec'),{})
                actual=dict(line.split('=',1) for line in output.read_text().splitlines())
                if new:
                    for name in ['metadata','dedent','scanner','percent','keyerror']:self.assertEqual(actual[name],'true')
                    self.assertEqual(actual['diagnostic'],actual['markdown'])
                    self.assertEqual(actual['diagnostic'],'false')
        parallel=(c.ROOT/'.github/workflows/keyerror-construction.yml').read_text().split('  keyerror-construction:',1)[0]
        self.assertIn('  treebuilder-state:',parallel)
        self.assertIn('uses: ./.github/workflows/treebuilder.yml',parallel)
        self.assertNotIn('    needs:',parallel)
        self.assertNotIn('needs: treebuilder',text)
        gate=(c.ROOT/'tests/aot/wcwidth_ci/project_gate.py').read_text()
        self.assertIn("'--runtime', runtime",gate)
        self.assertIn("(result / 'run.json')",gate)
        self.assertIn("actual['tools'].get(p) != digest",gate)

    def test_reusable_three_levels_no_cycle_permissions_or_false_dependency(self):
        root=(c.ROOT/'.github/workflows/wcwidth-legacy-aot.yml').read_text()
        parent=(c.ROOT/'.github/workflows/keyerror-construction.yml').read_text()
        child=(c.ROOT/'.github/workflows/treebuilder.yml').read_text()
        self.assertIn('uses: ./.github/workflows/keyerror-construction.yml',root)
        self.assertIn('uses: ./.github/workflows/treebuilder.yml',parent)
        self.assertNotIn('uses: ./.github/workflows/',child)
        for text in (parent,child):
            self.assertIn('  workflow_call:',text)
            self.assertIn('permissions:\n  contents: read',text)
            self.assertNotIn('contents: write',text)
            self.assertNotIn('continue-on-error',text)
        new=parent.split('  treebuilder-state:',1)[1].split('  keyerror-construction:',1)[0]
        self.assertNotIn('needs:',new)
        self.assertIn("github.event.head_commit.message == 'validation: treebuilder-state-001'",new)
        self.assertNotIn('workflow_dispatch:',child)
        self.assertIn('self.business()',Path(v.__file__).read_text())
        self.assertIn("if c.native_eligible(self.result['stages'])",Path(v.__file__).read_text())

    def test_public_inputs_have_no_private_paths(self):
        for path in [c.MANIFEST,*list((c.ROOT/c.FIXTURE).rglob('*'))]:
            if path.is_file():
                raw=path.read_bytes()
                self.assertNotIn(b'/workspace/',raw)
                self.assertNotIn(b'/home/runner/',raw)


if __name__=='__main__':unittest.main()
