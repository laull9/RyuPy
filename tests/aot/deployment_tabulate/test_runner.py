"""第二应用runner负测：公开配置不旁路、oracle无污染、完整输出不可截断。"""
import json
from pathlib import Path
import tempfile
import unittest
from runner import public_command, oracle_environment, verify_task, HERE

class RunnerTests(unittest.TestCase):
    def test_public_project_only(self):
        self.assertEqual(public_command(Path('/tool/ryupy'),Path('/p'),Path('/out')),
            ['/tool/ryupy','build','--project','/p/ryupy.json','--target','table','-o','/out'])
    def test_oracle_two_roots_and_no_stale_resources(self):
        env=oracle_environment({'RYUPY_DEFAULT_AOT_RESOURCES':'stale','RYUPY_DEFAULT_AOT_OTHER':'bad','PATH':'keep'},
                               Path('/p'),Path('/oracle'),Path('/cli'),Path('/runtime'))
        self.assertNotIn('RYUPY_DEFAULT_AOT_RESOURCES',env)
        self.assertNotIn('RYUPY_DEFAULT_AOT_OTHER',env)
        self.assertEqual(json.loads(env['RYUPY_DEFAULT_AOT_ROOTS']),['/p/vendor/table','/p/vendor/width'])
        self.assertEqual(env['PATH'],'keep')
        self.assertEqual(env['RYUPY_DEFAULT_AOT_METADATA'],'/p/distribution.json')
    def test_workflow_is_default_and_independent(self):
        hosted=(HERE/'hosted_tabulate.py').read_text()
        self.assertIn("['cargo','build','--locked','--release','-p','ryupy-cli']",hosted)
        self.assertNotIn('prepare_cp.py',hosted)
        self.assertNotIn('nuitka',hosted.lower())
        workflow=(HERE.parents[2]/'.github/workflows/deployment-tabulate-native.yml').read_text()
        self.assertIn('tests/aot/deployment_tabulate/hosted_tabulate.py verify',workflow)
        self.assertIn('if: always()',workflow)
    def test_exact_outputs_and_failures(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);prefix=root/'run';html=root/'out.html';ok={'exit':0,'failure':None}
            prefix.with_suffix('.stdout').write_bytes((HERE/'grid.stdout.txt').read_bytes())
            prefix.with_suffix('.stderr').write_bytes(b'')
            verify_task('grid',ok,prefix)
            prefix.with_suffix('.stdout').write_bytes(b'')
            with self.assertRaises(ValueError):verify_task('grid',ok,prefix)
            html.write_bytes((HERE/'html.output.txt').read_bytes());verify_task('html-file',ok,prefix,html)
            for bad in [{'exit':1,'failure':None},{'exit':0,'failure':'timeout'}]:
                with self.assertRaises(ValueError):verify_task('html-file',bad,prefix,html)
            html.write_bytes(b'partial')
            with self.assertRaises(ValueError):verify_task('html-file',ok,prefix,html)
            html.write_bytes((HERE/'html.output.txt').read_bytes());prefix.with_suffix('.stderr').write_bytes(b'warning')
            with self.assertRaises(ValueError):verify_task('html-file',ok,prefix,html)

if __name__=='__main__':unittest.main()
