"""dedent纯控制自测：不启动fixture/产品/SDK/下载；核当前完整DAG、输入、双流和失败前归档。"""
import copy
import hashlib
import json
import os
from pathlib import Path
import re
import tarfile
import tempfile
import types
import unittest
from unittest.mock import Mock, patch
import textwrap_dedent_contract as c
import textwrap_dedent_validation as gate
import textwrap_dedent_native as native

DATA = c.parse(c.MANIFEST.read_bytes())
ORDINARY = {'baseline', 'full-check', 'projects', 'trace-adapter-s2', 'trace-rust-preflight',
            'pyfiglet-explicit-deployment', 'source-import-paths'}


def jobs():
    raw = (c.ROOT / '.github/workflows/wcwidth-legacy-aot.yml').read_text()
    blocks = dict(re.findall(r'^  ([a-z][a-z0-9-]*):\n(.*?)(?=^  [a-z][a-z0-9-]*:|\Z)', raw.split('\njobs:\n', 1)[1], re.M | re.S))
    return raw, blocks


def select(title, event='push', ref=c.old.shared.REF, attempt='1'):
    raw, blocks = jobs()
    selector = blocks['select-mode'].split("python3 - <<'PY'\n", 1)[1].split('\n          PY', 1)[0]
    code = '\n'.join(line[10:] for line in selector.splitlines())
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        payload = {'ref': ref, 'head_commit': {'message': title}}
        (root / 'event').write_text(json.dumps(payload)); (root / 'output').write_text('')
        env = {'GITHUB_EVENT_PATH': str(root / 'event'), 'GITHUB_OUTPUT': str(root / 'output'),
               'GITHUB_REF': ref, 'GITHUB_EVENT_NAME': event}
        with patch.dict(os.environ, env, clear=True): exec(compile(code, 'actual-workflow-selector', 'exec'), {})
        output = (root / 'output').read_text()
    values = dict(line.split('=', 1) for line in output.splitlines()); active = set()
    for name, block in blocks.items():
        if name == 'select-mode': continue
        expression = re.search(r'^    if: (.*)$', block, re.M)[1]
        replacements = {'github.ref': ref, 'github.event_name': event, 'github.run_attempt': attempt}
        replacements.update({'needs.select-mode.outputs.' + key: values.get(key, '') for key in ('diagnostic', 'markdown', 'noreturn', 'search', 'metadata', 'dedent', 'scanner', 'percent', 'keyerror')})
        for key, value in replacements.items(): expression = expression.replace(key, repr(value))
        if eval(expression.replace('&&', 'and'), {'__builtins__': {}}, {}): active.add(name)
    return output, values, active


class Tests(unittest.TestCase):
    def test_manifest_sources_paths_modes_facts_and_old_inputs(self):
        self.assertEqual(c.load_manifest(), DATA)
        for name, digest in DATA['old_protected'].items(): self.assertEqual(c.sha(c.ROOT / name), digest, name)
        text = (c.ROOT / 'runtime/RyuPy.Runtime.Tests/PyTextDedentTests.cs').read_text()
        facts = ['PyTextDedentTests.' + n for n in re.findall(r'\[Fact\]\s+public void (\w+)\(', text)]
        self.assertCountEqual(facts, DATA['facts']); self.assertEqual(len(facts), 10)
        self.assertEqual(len(DATA['fresh_scenarios']), len(set(DATA['fresh_scenarios'])))
        self.assertEqual(len(DATA['fresh_scenarios']), 18)
        self.assertEqual(len(gate.native_groups(DATA)), 8)
        self.assertEqual(len([x for x in DATA['cases'] if x['mode'] == 'cp']), 4)
        self.assertEqual(len([x for x in DATA['cases'] if x['mode'] == 'pending']), 4)
        for value in ('../secret', '/absolute', 'a/../b', 'a\\b', './a', 'a//b', ''):
            with self.assertRaises(ValueError): c.relative(value)

    def test_all_old_job_blocks_are_byte_unchanged_and_independent(self):
        raw, blocks = jobs()
        self.assertEqual(set(blocks), ORDINARY | {'select-mode', 'trace-edge-diagnostic', 'markdown-preflight',
                         'typing-noreturn', 'text-search', 'distribution-metadata', 'textwrap-dedent', 're-scanner', 'decimal-percent', 'keyerror-construction'})
        for name, expected in DATA['unchanged_job_blocks'].items():
            self.assertEqual(hashlib.sha256(blocks[name].encode()).hexdigest(), expected, name)
        for name in ORDINARY | {'distribution-metadata', 'textwrap-dedent'}:
            self.assertIn('    needs: select-mode\n', blocks[name])
            self.assertNotIn('needs: textwrap-dedent', blocks[name])
        self.assertIn('branches: [ci/wcwidth-legacy-aot-2909056]', raw)
        self.assertIn('permissions:\n  contents: read', raw)
        self.assertNotIn('continue-on-error', blocks['textwrap-dedent'])

    def test_actual_selector_old_titles_exact_outputs_and_current_dag(self):
        titles = {c.old.shared.TITLE: 'markdown', 'diagnostic-not-acceptance: trace-v1-first-edge-001': 'diagnostic',
                  'validation: typing-noreturn-001': 'noreturn', 'validation: text-expandtabs-001': 'noreturn',
                  'validation: text-search-002': 'search', 'validation: distribution-metadata-001': 'metadata'}
        job = {'markdown': 'markdown-preflight', 'diagnostic': 'trace-edge-diagnostic', 'noreturn': 'typing-noreturn',
               'search': 'text-search', 'metadata': 'distribution-metadata'}
        for title, key in titles.items():
            with self.subTest(title=title):
                output, values, active = select(title)
                self.assertEqual(output, ''.join(k + '=' + str(k == key).lower() + '\n' for k in ('diagnostic','markdown','noreturn','search','metadata')))
                self.assertEqual(active, {job[key]} | (ORDINARY if key not in ('diagnostic', 'markdown') else set()))
        for title in ('ordinary', 'validation: text-search-001'):
            self.assertEqual(select(title)[2], ORDINARY)
        self.assertEqual(select(c.TITLE)[2], ORDINARY | {'distribution-metadata', 'textwrap-dedent'})
        self.assertEqual(select(c.TITLE)[1], dict(diagnostic='false', markdown='false', noreturn='false', search='false', metadata='true', dedent='true'))
        self.assertEqual(select(c.TITLE, attempt='2')[2], ORDINARY)
        for title, event, ref in [(c.TITLE.upper(), 'push', c.old.shared.REF), (c.TITLE+'\n', 'push', c.old.shared.REF),
                                  (c.TITLE, 'workflow_dispatch', c.old.shared.REF)]:
            self.assertEqual(select(title, event, ref)[2], ORDINARY)
        self.assertEqual(select(c.TITLE, ref=c.old.shared.REF.upper())[2], set())

    def test_context_and_metadata_context_only_add_new_exact_title(self):
        env = {'GITHUB_ACTIONS':'true', 'GITHUB_EVENT_NAME':'push','GITHUB_REF':c.old.shared.REF,
               'GITHUB_SHA':'a'*40,'GITHUB_RUN_ATTEMPT':'1','RUNNER_ENVIRONMENT':'github-hosted',
               'RYUPY_VALIDATION_MODE':'distribution-metadata'}
        event = {'ref':env['GITHUB_REF'],'after':env['GITHUB_SHA'],'head_commit':{'message':c.TITLE,'id':env['GITHUB_SHA']}}
        before = copy.deepcopy(event)
        c.check_context(env,event); c.metadata.check_context(env,event); self.assertEqual(before,event)
        for key,value in [('GITHUB_RUN_ATTEMPT','2'),('GITHUB_EVENT_NAME','workflow_dispatch'),('RUNNER_ENVIRONMENT','self-hosted'),('RUSTFLAGS','-A warnings')]:
            with self.assertRaises(ValueError): c.check_context(dict(env,**{key:value}),event)
        for title in (c.TITLE.upper(),c.TITLE+'\n','ordinary'):
            with self.assertRaises(ValueError): c.check_context(env,dict(event,head_commit=dict(event['head_commit'],message=title)))
            with self.assertRaises(ValueError): c.metadata.check_context(env,dict(event,head_commit=dict(event['head_commit'],message=title)))

    def test_complete_dual_streams_exit_timeout_signal_not_ni(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix=Path(temp)/'run'; prefix.with_suffix('.stdout').write_bytes(b'NI_OK\n'); prefix.with_suffix('.stderr').write_bytes(b'')
            good=dict(exit=0,failure=None,raw_streams_complete=True)
            c.verify_output(good,prefix,'NI_OK\n')
            for change in (dict(exit=1),dict(exit=-9),dict(failure='timeout'),dict(raw_streams_complete=False)):
                with self.assertRaises(ValueError): c.verify_output(dict(good,**change),prefix,'NI_OK\n')
            for stream in ('stdout','stderr'):
                path=prefix.with_suffix('.'+stream); original=path.read_bytes(); path.write_bytes(original+b'extra')
                with self.assertRaises(ValueError): c.verify_output(good,prefix,'NI_OK\n')
                path.write_bytes(original)

    def test_trx_rejects_skip_duplicate_empty_and_missing(self):
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'result.trx'; fact=DATA['fresh_fact']
            def write(names,outcome='Passed'):
                path.write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'+''.join(
                    '<UnitTestResult testName="RyuPy.Runtime.Tests.'+name+'" outcome="'+outcome+'"/>' for name in names)+'</TestRun>')
            write([fact]); self.assertEqual(c.verify_trx(path,[fact])['total'],1)
            for names,status in [([], 'Passed'),([fact,fact],'Passed'),([fact],'NotExecuted'),([fact],'Failed'),(['Wrong'],'Passed')]:
                write(names,status)
                with self.assertRaises(ValueError): c.verify_trx(path,[fact])

    def test_source_re_is_explicit_graph_and_actual_plan_required(self):
        case=next(x for x in DATA['cases'] if x['id']=='source-retry')
        runner=types.SimpleNamespace(cli=Path('/cli'),inputs_dir=Path('/inputs'))
        argv=list(map(str,c.build_argv(runner,case,Path('/output'),True)))
        self.assertEqual(argv.count('--include-module'),2); self.assertIn('re',argv); self.assertIn('state',argv)
        self.assertEqual(argv[-1],'--aot'); self.assertNotIn('--instance-module',argv)
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'plan.json'
            plan=dict(compiled_feature=True,session_policy='ordinary-dynamic-module-pilot',modules=[dict(module=n,compiled=True,
                execution='LegacySingleton',reason='aot-not-migrated',ordinary_name_handlers=False,relative_from_import=False) for n in case['modules']])
            path.write_text(json.dumps(plan)); c.verify_plan(path,case,True)
            for changed in (dict(plan,modules=plan['modules'][:-1]),dict(plan,session_policy='explicit-only'),dict(plan,compiled_feature=False)):
                path.write_text(json.dumps(changed))
                with self.assertRaises(ValueError): c.verify_plan(path,case,True)
            bad=copy.deepcopy(plan); bad['modules'][1]['compiled']=False; path.write_text(json.dumps(bad))
            with self.assertRaises(ValueError): c.verify_plan(path,case)

    def test_native_dist_saved_before_any_validation_and_failed_publish(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); target=root/'build'; dist=target/'entry.dist'; dist.mkdir(parents=True)
            (dist/'entry').write_bytes(b'actual-failed-ELF'); (dist/'ryupy-artifact.json').write_bytes(b'invalid manifest')
            runner=types.SimpleNamespace(out=root,frozen={},preserve_publish_evidence=Mock())
            self.assertEqual(native.preserve_before_validation(runner,target,'entry'),[dist])
            archive=root/'native-dist.tar.gz'; self.assertTrue(archive.exists())
            with tarfile.open(archive) as tar: self.assertEqual(tar.extractfile('entry.dist/entry').read(),b'actual-failed-ELF')
            self.assertEqual(runner.frozen[str(archive)],c.sha(archive))

    def test_failed_domain_does_not_skip_independent_one_and_identity_is_sticky(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(gate,'ROOT',Path(temp)):
            r=gate.Runner(); r.before={'head':'fixed'}; r.identity=Mock(return_value=r.before); r.env={}
            self.assertFalse(r.phase('first',lambda: (_ for _ in ()).throw(ValueError('real failure'))))
            self.assertTrue(r.phase('second',lambda: dict(actual=True)))
            self.assertFalse(r.result['stages']['first']['passed'])
            r.invalidate(c.IdentityError('identity'))
            with self.assertRaises(c.IdentityError): r.phase('third',lambda: None)
            self.assertFalse(r.result['stages']['second']['passed'])

    def test_resume_rejects_manifest_or_product_paths_before_native(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(gate,'ROOT',Path(temp)):
            r=gate.Runner(); r.persist()
            state=c.parse((r.out/'state.json').read_bytes()); state['manifest_sha256']='0'*64
            c.save(r.out/'state.json',state)
            with self.assertRaises(c.IdentityError): r.load_state()
            self.assertTrue(r.identity_failed); self.assertFalse(r.result['passed'])

    def test_real_runner_bad_state_shape_overwrites_stale_pass(self):
        for kind in ('null-root', 'list-root', 'null-result', 'list-result', 'null-stages', 'list-stage', 'missing-stage', 'contradiction'):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as temp, patch.object(gate,'ROOT',Path(temp)):
                r=gate.Runner(); r.persist(); path=r.out/'state.json'
                state=c.parse(path.read_bytes())
                if kind=='null-root': state=None
                elif kind=='list-root': state=[]
                elif kind=='null-result': state['result']=None
                elif kind=='list-result': state['result']=[]
                elif kind=='null-stages': state['result']['stages']=None
                elif kind=='list-stage': state['result']['stages']['identity']=[]
                elif kind=='missing-stage': del state['result']['stages']['identity']
                else: state['result']['stages']['identity']['passed']=True
                c.save(path,state); c.save(r.out/'result.json',{'passed':True,'stale':True})
                r.identity=Mock(return_value={'head':'unused'})
                r.native=Mock(side_effect=AssertionError('must not execute product'))
                self.assertEqual(r.execute('native'),1)
                report=c.parse((r.out/'result.json').read_bytes())
                self.assertFalse(report['passed']); self.assertFalse(report['identity_preserved'])
                self.assertNotIn('stale',report); self.assertTrue(r.identity_failed)
                self.assertTrue(all(not row['passed'] for row in report['stages'].values()))
                self.assertFalse(c.parse(path.read_bytes())['result']['passed'])
                self.assertEqual(c.parse((r.out/'resume-state.raw.json').read_bytes()),state)
                r.native.assert_not_called()

    def test_grouped_native_failures_keep_archive_and_later_processes(self):
        for broken in ('validation', 'original', 'publish', 'none'):
            with self.subTest(broken=broken), tempfile.TemporaryDirectory() as temp, patch.object(gate,'ROOT',Path(temp)):
                r=gate.Runner(); root=r.out; r.before={'head':'fixed'}; r.identity=Mock(return_value=r.before)
                r.cli=Path('/fake-cli'); r.env={}; r.runtime=root/'runtime.dll'; r.runtime.write_bytes(b'runtime')
                cases=gate.native_groups(DATA)[5]  # 同一个preimport来源，11个独立argv场景。
                self.assertEqual(len(cases),11)
                for source in r.data['sources']:
                    path=r.inputs_dir/source; path.parent.mkdir(parents=True,exist_ok=True); path.write_bytes(b'fixture')
                r.freeze_inputs(r.inputs_dir)
                calls=[]; layout=None
                def command(name,argv,timeout,env):
                    nonlocal layout
                    target=r.out/'native-build'; dist=target/'negative_preimport.dist'; dist.mkdir(parents=True)
                    (dist/'negative_preimport').write_bytes(b'fake-ELF'); (dist/'ryupy-artifact.json').write_bytes(b'{}')
                    managed=target/'managed'; managed.mkdir(); pe=managed/'negative_preimport.dll'; pe.write_bytes(b'PE')
                    (managed/'RyuPy.Runtime.dll').write_bytes(b'runtime')
                    layout=types.SimpleNamespace(dist=dist,native=dist/'negative_preimport',managed=pe)
                    (r.out/(name+'.stderr')).write_bytes(b'fake report')
                    return dict(exit=1 if broken=='publish' else 0,failure=None,raw_streams_complete=True)
                r.command=command; r.preserve_publish_evidence=lambda *args: None
                def validate(*args):
                    if broken=='validation': raise ValueError('manifest does not bind runtime')
                    return {'mock':True}
                def move(dist,binary,dest,*args):
                    shutil.copytree(dist,dest); return dest/binary.name,{}
                def execute(name,argv,golden,env):
                    self.assertFalse(r.inputs_dir.exists()); self.assertEqual(env['PATH'],'/usr/bin:/bin')
                    calls.append(name)
                    if broken=='original' and name==cases[0]['id']+'-original': raise ValueError('actual original failed')
                import shutil
                r.run_positive=execute
                with patch.object(native.shutil,'disk_usage',return_value=types.SimpleNamespace(free=3*1024**3)), \
                     patch.object(c.old,'reported_aot',side_effect=lambda *args: layout), \
                     patch.object(c,'verify_native',side_effect=validate), patch.object(c.old,'relocate_manifest',side_effect=move):
                    if broken=='none': native.native(r,cases)
                    else:
                        with self.assertRaises(ValueError): native.native(r,cases)
                self.assertEqual(r.out,root); self.assertTrue(r.inputs_dir.exists())
                saved=root/'native-domains'/cases[0]['id']/'native-dist.tar.gz'
                self.assertTrue(saved.exists())
                if broken in ('validation','publish'): self.assertEqual(calls,[])
                if broken=='original':
                    self.assertNotIn(cases[0]['id']+'-relocated',calls)
                    self.assertIn(cases[-1]['id']+'-relocated',calls)
                if broken=='none': self.assertEqual(len(calls),22)

    def test_workflow_keeps_preflight_and_failure_uploads(self):
        raw=(c.ROOT/'.github/workflows/textwrap-dedent.yml').read_text()
        self.assertLess(raw.index('textwrap_dedent_selftest.py'),raw.index('tests/aot/wcwidth_ci/prepare.py'))
        self.assertLess(raw.index('dedent-checkpoint-'),raw.index('--phase native'))
        self.assertIn('target/textwrap-dedent-evidence/**/native-dist.tar.gz',raw)
        self.assertIn('runs-on: ubuntu-24.04',raw); self.assertIn('contents: read',raw)
        self.assertEqual(raw.count('uses: actions/upload-artifact@'),3)
        self.assertNotIn('continue-on-error',raw)


if __name__ == '__main__':
    unittest.main()
