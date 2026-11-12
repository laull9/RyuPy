"""独立来源/工具角色及metadata-only负合同；合成清单不冒充实际PE或Native。"""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('tabulate_artifact_consumer',HERE/'validate_artifact.py')
v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
from test_schema4 import sample


def fixture():
    data,_,_=sample()
    data['compiler']['options']['module_environment_policy']='explicit-only'
    data['default_source_plan']['automatic_instance_modules']=[]
    first=copy.deepcopy(data['modules'][0]); data['modules']=[]; data['sources']=[]; data['module_locations']=[]
    for i,(name,path) in enumerate(v.EXPECTED_MODULES.items()):
        logical='entry/main.py' if i==0 else 'modules/'+name.replace('.','/')+'.py'
        data['sources'].append({'module_id':i,'module_name':name,'package':'','logical_source':logical,'compiled_text_sha256':'0'*64})
        data['modules'].append(dict(first,id=i,name=name,environment_reason='policy-disabled'))
        data['module_locations'].append({'module_id':i,'module_name':name,'kind':'executable_relative','relative_file':'_internal/resources/v1/'+logical})
    return data


class ValidatorTests(unittest.TestCase):
    def test_metadata_only_and_no_mutation(self):
        data=fixture(); old=copy.deepcopy(data)
        v.validate_metadata_only(data,'10.0.403',{'checked':True})
        self.assertEqual(old,data)

    def test_schema_scope_policy_resource_and_metadata_negatives(self):
        changes=[lambda d:d.update(schema_version=2),
            lambda d:d['compiler']['options'].update(module_environment_policy='ordinary-dynamic-module-pilot'),
            lambda d:d['modules'][0].update(environment_reason='aot-not-migrated'),
            lambda d:d['modules'][1].update(getter=None,setter=None),
            lambda d:d['scope'].update(declared_resources_complete=True),
            lambda d:d['scope'].update(resources_included=True),
            lambda d:d['scope'].update(dynamic_dependency_closure_complete=True),
            lambda d:d['default_source_plan'].update(automatic_instance_modules=['tabulate']),
            lambda d:d['compiler']['options'].update(distribution_metadata={'different':True}),
            lambda d:d['resources'].append({'fake':1}),
            lambda d:d['sources'].pop(),
            lambda d:d['target'].update(rid='win-x64'),
            lambda d:d.update(extra='unknown')]
        for change in changes:
            data=fixture();change(data)
            with self.subTest(change=change),self.assertRaises(ValueError):
                v.validate_metadata_only(data,'10.0.403',{'checked':True})

    def test_exact_source_paths_digests_and_set(self):
        expected={name:(Path('/project')/path,'a'*64) for name,path in v.EXPECTED_MODULES.items()}
        rows=[{'module_name':n,'path':str(p),'sha256':h} for n,(p,h) in expected.items()]
        emitted=[{'module_name':r['module_name'],'module_id':i,'compiled_text_sha256':r['sha256']} for i,r in enumerate(rows)]
        v.check_source_contract(rows,emitted,expected)
        for mutate in [lambda r,e:r.pop(),lambda r,e:r.append(r[0]),lambda r,e:e.pop(),
            lambda r,e:e.append(e[0]),lambda r,e:r[1].update(path='/elsewhere/tabulate.py'),
            lambda r,e:r[1].update(sha256='b'*64),lambda r,e:e[1].update(compiled_text_sha256='b'*64),
            lambda r,e:e[1].update(module_id=0),lambda r,e:r[1].update(module_name='unlocked')]:
            r,e=copy.deepcopy((rows,emitted));mutate(r,e)
            with self.subTest(mutate=mutate),self.assertRaises(ValueError):v.check_source_contract(r,e,expected)

    def test_request_roots_order_and_metadata_only(self):
        project=Path('/project')
        identity={'entry':str(project/'tools/tabulate_console.py'),
            'search_roots':[str(project/'vendor/table'),str(project/'vendor/width')],
            'include_modules':['tabulate','wcwidth'],'resources':None,
            'metadata':str(project/'distribution.json')}
        v.check_request_identity(identity,project)
        for change in [lambda d:d['search_roots'].reverse(),lambda d:d['search_roots'].append('/extra'),
            lambda d:d.update(resources='empty-resource-request.json'),
            lambda d:d.update(metadata='/other/distribution.json'),
            lambda d:d.update(entry='/other/tools/tabulate_console.py'),
            lambda d:d.update(include_modules=['tabulate'])]:
            bad=copy.deepcopy(identity);change(bad)
            with self.subTest(change=change),self.assertRaises(ValueError):v.check_request_identity(bad,project)

    def test_frozen_role_swap_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            cli=Path(folder)/'cli';runtime=Path(folder)/'runtime';cli.write_bytes(b'compiler');runtime.write_bytes(b'runtime')
            tools={str(p.resolve()):v.sha(p) for p in (cli,runtime)}
            identity={'cli_sha256':v.sha(cli),'runtime_sha256':v.sha(runtime)}
            v.check_tool_roles(identity,tools,cli,runtime)
            identity['cli_sha256'],identity['runtime_sha256']=identity['runtime_sha256'],identity['cli_sha256']
            with self.assertRaises(ValueError):v.check_tool_roles(identity,tools,cli,runtime)

    def test_missing_oracle_fails_before_receipt(self):
        with tempfile.TemporaryDirectory() as folder:
            p=Path(folder);tool=p/'tool';tool.write_bytes(b'x')
            with self.assertRaises(ValueError):
                v.validate_tabulate_artifact(None,p,{str(tool):v.sha(tool)},python=tool,dotnet=tool,
                    cli=tool,runtime=tool,oracle_dir=p/'absent',output=p/'audit')
            self.assertFalse((p/'audit/receipt.json').exists())


if __name__=='__main__':unittest.main()
