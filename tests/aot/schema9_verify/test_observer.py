"""透明观察器的轻量合同：命令与返回对象不变，观察失败不吞，资源改名负例自洽。"""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import Mock, patch
import observer
import importlib.util
spec = importlib.util.spec_from_file_location("schema9_runner", Path(__file__).with_name("run.py"))
runner = importlib.util.module_from_spec(spec); spec.loader.exec_module(runner)
from common import digest, save, tree


class ObserverContracts(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.root = Path(self.temp.name)
        self.watch = observer.Observer(self.root/'cli', self.root/'verify', self.root, 'linux-x64', {}, self.root/'marker')
        self.real_run = subprocess.run

    def tearDown(self):
        subprocess.run = self.real_run; self.temp.cleanup()

    def call(self, command, completed):
        environment = {'PATH': 'original'}; self.watch.real_run = Mock(return_value=completed)
        actual = self.watch(command, cwd=self.root, env=environment, capture_output=True, timeout=240)
        sent = self.watch.real_run.call_args
        self.assertIs(sent.args[0], command); self.assertIs(sent.kwargs['env'], environment)
        self.assertIs(actual, completed); self.assertEqual(sent.kwargs['timeout'], 240)
        return actual

    def test_success_preserves_exact_call_and_result(self):
        self.call(['cc','arg with &'], subprocess.CompletedProcess([],0,b'real stdout',b'real stderr'))
        self.assertEqual(len(self.watch.calls), 1)

    def test_failure_not_replaced(self):
        command = [str(self.watch.cli),'build','app.py','--aot','-o',str(self.root/'out')]
        self.call(command, subprocess.CompletedProcess([],23,b'',b'actual error'))
        self.assertEqual(self.watch.observed, {})

    def test_auxiliary_process_passes_without_recording(self):
        completed = subprocess.CompletedProcess([],0,'text',None)
        self.watch.real_run = Mock(return_value=completed)
        self.assertIs(self.watch(['auxiliary'],text=True), completed)
        self.assertEqual(self.watch.calls, [])

    def test_observation_error_propagates(self):
        self.watch.real_run = Mock(return_value=subprocess.CompletedProcess([],0,b'ok',b''))
        self.watch.observe_package = Mock(side_effect=ValueError('observation failed'))
        command = [str(self.root/'中文 & relocated'/'app')]
        with self.assertRaisesRegex(ValueError,'observation failed'):
            self.watch(command,cwd=self.root,env={},capture_output=True)

    def package(self):
        package = self.root/'package'; package.mkdir(); (package/'app').write_bytes(b'native')
        (package/'data.txt').write_bytes(b'payload')
        resource = {'relative_path':'data.txt','size':7,'sha256':digest(package/'data.txt')}
        manifest = {'schema_version':9,'kind':'ryupy-native-aot-artifact',
            'scope':{'declared_resources_complete':True,'automatic_module_locations_complete':True},
            'resources':[resource], 'module_locations':[{'kind':'source'}], 'default_source_plan':{'mode':'audit'},
            'sources':[{'module_name':'colorsys'}],
            'native_bindings':[{'module':'native_probe','linkage':'aot_static'}],
            'artifacts':[dict(resource,role='declared_resource'),{'relative_path':'app','size':6,
                'sha256':digest(package/'app'),'role':'native_executable'}]}
        save(package/'ryupy-artifact.json',manifest); return package

    def test_own_verify_uses_saved_real_run(self):
        package = self.package(); self.watch.real_run = Mock()
        def checked(*args):
            self.assertIs(subprocess.run,self.watch.real_run)
            return {'actual_test_double':True}
        with patch('observer.verify',side_effect=checked):
            self.watch.observe_package('original',package,4)
        self.assertIs(subprocess.run,self.watch)
        self.assertEqual(tree(package),tree(self.root/'raw/original-package'))

    def test_illegal_name_preserves_hash_and_closed_file_set(self):
        package = self.package(); baseline = tree(package); checked=[]
        def check(_cli,target,label,*_):
            if label=='illegal-resource-name':
                manifest=json.loads((target/'ryupy-artifact.json').read_text())
                resource=manifest['resources'][0]; self.assertFalse(resource['relative_path'].isascii())
                self.assertEqual(digest(target/resource['relative_path']),resource['sha256'])
                log=self.root/'raw/verify-logs'/(label+'.stderr');log.parent.mkdir(parents=True,exist_ok=True)
                log.write_text('资源相对目标首切仅支持有界 ASCII，不能自动改名')
                self.assertEqual({a['relative_path'] for a in manifest['artifacts']}|{'ryupy-artifact.json'},
                                 {p.name for p in target.iterdir()})
            checked.append(label); return {'case':label}
        with patch.object(runner,'verify',side_effect=check):
            rows=runner.negatives(self.root/'cli',package,self.root,{},self.root/'marker')
        self.assertEqual(len(rows),5); self.assertIn('illegal-resource-name',checked)
        self.assertEqual(tree(package),baseline)


if __name__=='__main__': unittest.main()
