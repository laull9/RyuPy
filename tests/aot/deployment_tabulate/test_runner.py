"""第二应用runner负测：公开配置不旁路、oracle无污染、完整输出不可截断。"""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from runner import public_command, oracle_environment, verify_task, HERE, container_command, container_security

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
    def test_container_matches_uid_without_weakening_isolation(self):
        with patch('runner.os.getuid',return_value=1001), patch('runner.os.getgid',return_value=1002):
            command=container_command(Path('/dist'),Path('/inputs'),Path('/out'),'sha256:image','tabulate',['-1'])
        check=lambda cmd: container_security(cmd,1001,1002,Path('/dist'),Path('/inputs'),Path('/out'))
        check(command)
        mutations=[]
        wrong=command.copy();wrong[wrong.index('--user')+1]='0:0';mutations.append(wrong)
        absent=command.copy();i=absent.index('--user');del absent[i:i+2];mutations.append(absent)
        for flag in ('--cap-drop=ALL','--read-only','--security-opt=no-new-privileges'):
            value=command.copy();value.remove(flag);mutations.append(value)
        for mount in ('dst=/app,readonly','dst=/inputs,readonly'):
            mutations.append([x.replace(mount,mount.replace(',readonly','')) for x in command])
        extra=command.copy();extra[2:2]=['--mount','type=bind,src=/source,dst=/source'];mutations.append(extra)
        network=command.copy();network[network.index('--network')+1]='host';mutations.append(network)
        for mutation in mutations:
            with self.assertRaises(ValueError):check(mutation)
        for uid in (-1, '1001', True):
            with self.assertRaises(ValueError):container_security(command,uid,1002,Path('/dist'),Path('/inputs'),Path('/out'))
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
