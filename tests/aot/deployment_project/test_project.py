"""项目fixture和公开argv纯合同；这些测试不冒充真实Native构建成功。"""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import hashlib
import subprocess
import sys
from prepare import configuration, resource_configuration


from public_build import project_command, inputs, relocate_project
import public_build
import prepare as project_prepare

class ProjectTests(unittest.TestCase):
    def test_whole_project_relocation_preserves_locked_inputs(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);app=root/'contract';app.mkdir();project=root/'project';project.mkdir()
            (project/'package').mkdir();(project/'package/source.py').write_bytes(b'print(1)\n')
            digest=hashlib.sha256(b'print(1)\n').hexdigest()
            (app/'inputs.json').write_text(json.dumps({'files':{'source.py':digest}}), encoding='utf-8')
            for name in ('resources.json','metadata-request.json','console_entry.py'):
                (project/name).write_text('{}', encoding='utf-8')
            (project/'ryupy.json').write_text(json.dumps(configuration()), encoding='utf-8')
            with patch.object(public_build,'APP',app):
                before=inputs(project);moved=root/'new-location';project.rename(moved)
                self.assertEqual(before,inputs(moved))
                (moved/'package/source.py').write_bytes(b'print(2)\n')
                with self.assertRaises(ValueError):inputs(moved)

    def test_relocation_preserves_owned_build_outside_new_project(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);project=root/'project';project.mkdir()
            (project/'ryupy.json').write_text('input', encoding='utf-8')
            build=project/'build/cli';build.mkdir(parents=True)
            (build/'owner.json').write_text('{"source":"/old/project/entry.py"}', encoding='utf-8')
            (build/'native').write_bytes(b'old-original')
            moved=root/'moved';evidence=root/'preserved'
            self.assertEqual(relocate_project(project,moved,evidence),moved)
            self.assertFalse((moved/'build').exists())
            self.assertEqual((moved/'ryupy.json').read_text(encoding='utf-8'),'input')
            self.assertEqual((evidence/'cli/native').read_bytes(),b'old-original')
            self.assertEqual((evidence/'cli/owner.json').read_text(encoding='utf-8'),'{"source":"/old/project/entry.py"}')
            self.assertFalse(project.exists())
            with self.assertRaises(ValueError):relocate_project(moved,root/'again',evidence)

    def test_locked_fixtures_keep_lf_under_autocrlf_checkout(self):
        repo=Path(__file__).resolve().parents[3]
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);work=root/'git';work.mkdir();out=root/'checkout';out.mkdir()
            def git(*argv):
                return subprocess.run(['git','-C',str(work),'-c','core.autocrlf=true',*argv],check=True,capture_output=True)
            git('init')
            (work/'.gitattributes').write_bytes((repo/'.gitattributes').read_bytes())
            paths=['tests/aot/standalone_app/inputs.json','tests/aot/standalone_app/console_entry.py',
                   'tests/aot/standalone_app/stdout.txt','tests/aot/deployment_tabulate/inputs.json',
                   'tests/aot/deployment_tabulate/console_entry.py','tests/aot/deployment_tabulate/html.output.txt']
            payload=b'first\nsecond\n'
            for name in paths:
                p=work/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(payload)
            git('add','.gitattributes',*paths)
            git('checkout-index','--prefix='+out.as_posix()+'/', '--',*paths)
            for name in paths:self.assertEqual((out/name).read_bytes(),payload)

    def test_frozen_pe_reader_checkout_keeps_exact_hash_and_rejects_tampering(self):
        # 用真实冻结读取器而非替代文本验证checkout；SHA断言不允许换行归一化。
        repo=Path(__file__).resolve().parents[3]
        relative=Path('tests/validation/source_provenance/ci/pe_metadata.py')
        decoder=repo/relative.with_name('read_pe_decoder.py')
        expected='a43bf4bb9e89ceb169b89a67c8817e8cf2312429cb251a0f36f1b9da8f469bc3'
        payload=(repo/relative).read_bytes()
        self.assertEqual(hashlib.sha256(payload).hexdigest(),expected)
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);work=root/'git';work.mkdir();out=root/'checkout';out.mkdir()
            def git(*argv):
                return subprocess.run(['git','-C',str(work),'-c','core.autocrlf=true',*argv],check=True,capture_output=True)
            git('init')
            (work/'.gitattributes').write_bytes((repo/'.gitattributes').read_bytes())
            source=work/relative;source.parent.mkdir(parents=True);source.write_bytes(payload)
            git('add','.gitattributes',relative.as_posix())
            git('checkout-index','--prefix='+out.as_posix()+'/', '--',relative.as_posix())
            checked=out/relative
            self.assertEqual(checked.read_bytes(),payload)
            self.assertEqual(hashlib.sha256(checked.read_bytes()).hexdigest(),expected)
            wrapper=checked.with_name('read_pe_decoder.py');wrapper.write_bytes(decoder.read_bytes())
            dummy=root/'not-a-product.dll';dummy.write_bytes(b'not a PE')
            # 两种改写仍必须在加载读取器之前拒绝，不能通过规范化弱化原字节身份。
            for changed in (payload.replace(b'\n',b'\r\n'),payload+b'\n# mutation\n'):
                checked.write_bytes(changed)
                result=subprocess.run([sys.executable,'-B',str(wrapper),str(dummy),'old','hash-negative'],capture_output=True)
                self.assertNotEqual(result.returncode,0)
                self.assertIn(b'AssertionError',result.stderr)
                self.assertIn(b'BASE_SHA',result.stderr)
                self.assertEqual(result.stdout,b'')

    def test_prepare_mismatch_keeps_actual_digest_evidence(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);app=root/'app';app.mkdir()
            expected=hashlib.sha256(b'line\n').hexdigest()
            (app/'inputs.json').write_text('{"files":{}}',encoding='utf-8')
            (app/'contract.json').write_text(json.dumps({'files':{'golden.txt':expected}}),encoding='utf-8')
            (app/'golden.txt').write_bytes(b'line\r\n')
            with patch.object(project_prepare,'APP',app):
                with self.assertRaises(ValueError):project_prepare.prepare(root/'project',root/'package')
            record=json.loads((root/'project-input-check.json').read_text(encoding='utf-8'))['golden.txt']
            self.assertEqual(record['expected_sha256'],expected)
            self.assertEqual(record['actual_sha256'],hashlib.sha256(b'line\r\n').hexdigest())
            self.assertEqual(record['CRLF'],1)
            self.assertFalse((root/'project').exists())

    def test_configuration_has_no_workspace_paths_or_build_helpers(self):
        config = configuration()
        self.assertEqual(config['mode'], 'aot')
        self.assertEqual(set(config['targets']), {'cli','metadata'})
        for path in config['search_paths']+[config['resources'], config['distribution_metadata']]+[v['entry'] for v in config['targets'].values()]:
            self.assertFalse(Path(path).is_absolute())
            self.assertNotIn('..', Path(path).parts)
        self.assertEqual(config['include_modules'], ['pyfiglet','pyfiglet.fonts'])
        self.assertNotIn('oracle', json.dumps(config))

    def test_compact_resource_selector_matches_independent_locked_list(self):
        # 只对冻结输入作独立集合核验；不调用产品展开器，也不更新旧benchmark清单。
        app=Path(__file__).resolve().parent.parent/'standalone_app'
        locked=json.loads((app/'inputs.json').read_text(encoding='utf-8'))['files']
        old=json.loads((app/'resources.json').read_text(encoding='utf-8'))['modules']
        config=resource_configuration()
        module=config['modules'][0]
        self.assertEqual(module['module'],'pyfiglet.fonts')
        self.assertNotIn('files',module)
        tree=module['trees'][0]
        self.assertEqual(tree['directory'],'.')
        prefix='pyfiglet/fonts/'
        selected=sorted(name[len(prefix):] for name in locked
                        if name.startswith(prefix) and name.endswith(tuple(tree['suffixes'])))
        self.assertEqual(len(selected),553)
        self.assertEqual(selected,sorted(old[0]['files']))

    def test_public_command_does_not_inject_legacy_inputs(self):
        argv = project_command('/tools/ryupy',Path('/moved/project'),'metadata')
        self.assertEqual(argv, ['/tools/ryupy','build','--project',str(Path('/moved/project')/'ryupy.json'),'--target','metadata'])
        for flag in ('--aot','--resources','--include-module','-I','--distribution-metadata'):
            self.assertNotIn(flag,argv)
        self.assertEqual(project_command('ryupy',Path('project'),'cli','caller-output')[-2:], ['-o','caller-output'])


if __name__ == '__main__': unittest.main()
