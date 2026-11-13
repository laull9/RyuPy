"""普通consumer与独立来源/argv/只读容器负测，不冒充Native验收。"""
import json
from pathlib import Path
import tempfile
import subprocess
import unittest
from unittest.mock import patch
from prepare import HERE,configuration,metadata_request,prepare,sha
from runner import public_command,oracle_environment,container_command,verify_case
import validate_artifact as validator

class ContractTests(unittest.TestCase):
    def test_raw_consumer_inputs_survive_autocrlf_checkout(self):
        repo=HERE.parents[2]
        contract=json.loads((HERE/'contract.json').read_text(encoding='utf-8'))
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);work=root/'git';work.mkdir();out=root/'checkout';out.mkdir()
            def git(*args):
                return subprocess.run(['git','-C',str(work),'-c','core.autocrlf=true',*args],check=True,capture_output=True)
            git('init');(work/'.gitattributes').write_bytes((repo/'.gitattributes').read_bytes())
            paths=[]
            for name in contract['files']:
                relative='tests/aot/deployment_binaryornot/'+name
                p=work/relative;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes((HERE/name).read_bytes());paths.append(relative)
            git('add','.gitattributes',*paths)
            git('checkout-index','--prefix='+out.as_posix()+'/', '--',*paths)
            for name,digest in contract['files'].items():
                self.assertEqual(sha(out/'tests/aot/deployment_binaryornot'/name),digest)

    def test_consumer_has_no_explicit_modules_or_resources(self):
        config=configuration()
        self.assertEqual(config['search_paths'],['vendor'])
        self.assertNotIn('include_modules',config);self.assertNotIn('resources',config)
        self.assertEqual(config['targets'],{'scan':{'entry':'scanner.py'}})
        self.assertEqual(metadata_request()['roots'],[{'id':'vendor','path':'vendor'}])
        command=public_command('ryupy',Path('/project'),Path('/output'))
        self.assertEqual(command,['ryupy','build','--project','/project/ryupy.json','--target','scan','-o','/output'])
        env=oracle_environment({'RYUPY_DEFAULT_AOT_RESOURCES':'stale','RYUPY_DEFAULT_AOT_ROOTS':'stale'},Path('/project'),Path('/oracle'),'cli','runtime')
        self.assertEqual(env['RYUPY_DEFAULT_AOT_INCLUDE'],'[]')
        self.assertNotIn('RYUPY_DEFAULT_AOT_RESOURCES',env);self.assertNotIn('RYUPY_DEFAULT_AOT_ROOTS',env)

    def test_exact_four_locked_external_inputs(self):
        contract=json.loads((HERE/'contract.json').read_text())
        for name,digest in contract['files'].items():self.assertEqual(sha(HERE/name),digest)
        self.assertEqual([(x['input'],x['stdout']) for x in contract['cases']],
                         [('ascii.txt','False\n'),('utf8.txt','False\n'),('utf16.txt','False\n'),('binary.bin','True\n')])
        self.assertTrue((HERE/'utf16.txt').read_bytes().startswith(b'\xff\xfe'))
        self.assertEqual((HERE/'binary.bin').read_bytes(),b'\x00\x01\x02\xff')

    def test_source_contract_rejects_missing_extra_path_or_hash(self):
        names=validator.EXPECTED_MODULES
        self.assertEqual(len(names),47)
        expected={name:('/project/'+relative,'frozen') for name,relative in names.items()}
        rows=[{'module_name':name,'path':expected[name][0],'sha256':'frozen'} for name in names]
        emitted=[{'module_name':name,'module_id':i,'compiled_text_sha256':'frozen'} for i,name in enumerate(names)]
        validator.check_source_contract(rows,emitted,expected)
        for bad in [rows[:-1],rows+rows[:1],rows+[dict(rows[0],module_name='unlocked')],
                    [dict(rows[0],path='/outside')]+rows[1:],[dict(rows[0],sha256='changed')]+rows[1:]]:
            with self.assertRaises(ValueError):validator.check_source_contract(bad,emitted,expected)
        with self.assertRaises(ValueError):validator.check_source_contract(rows,emitted[:-1],expected)

    def test_request_identity_rejects_hidden_includes_resources_or_root(self):
        good={'entry':'/project/scanner.py','search_roots':['/project/vendor'],'include_modules':[],
              'resources':None,'metadata':'/project/distribution.json'}
        validator.check_request_identity(good,Path('/project'))
        for key,value in [('entry','/elsewhere/scanner.py'),('search_roots',['/other']),
                          ('include_modules',['binaryornot']),('resources','/res.json')]:
            with self.assertRaises(ValueError):validator.check_request_identity(dict(good,**{key:value}),Path('/project'))

    def test_container_has_only_readonly_inputs_and_dist(self):
        command=container_command(Path('/dist'),Path('/data'),'sha256:'+'1'*64,'scanner','binary.bin')
        mounts=[command[i+1] for i,x in enumerate(command) if x=='--mount']
        self.assertEqual(mounts,['type=bind,src=/dist,dst=/app,readonly','type=bind,src=/data,dst=/inputs,readonly'])
        self.assertEqual(command[command.index('--network')+1],'none')
        self.assertIn('--read-only',command);self.assertIn('--cap-drop=ALL',command)
        self.assertIn('--security-opt=no-new-privileges',command)
        self.assertEqual(command[-2:],['/app/scanner','/inputs/binary.bin'])

    def test_classification_requires_complete_raw_stream(self):
        with tempfile.TemporaryDirectory() as folder:
            prefix=Path(folder)/'case';prefix.with_suffix('.stdout').write_bytes(b'False\n');prefix.with_suffix('.stderr').write_bytes(b'')
            good={'exit':0,'failure':None};case={'stdout':'False\n'}
            verify_case(case,good,prefix)
            for bad in [{'exit':1,'failure':None},{'exit':0,'failure':'timeout'}]:
                with self.assertRaises(ValueError):verify_case(case,bad,prefix)
            prefix.with_suffix('.stdout').write_bytes(b'False')
            with self.assertRaises(ValueError):verify_case(case,good,prefix)

    def test_prepare_rejects_tampered_upstream_before_creating_project(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);fake=root/'source';fake.mkdir()
            with self.assertRaises(ValueError):prepare(root/'project',{'binaryornot':fake,'chardet':fake})
            self.assertFalse((root/'project').exists())

if __name__=='__main__':unittest.main()
