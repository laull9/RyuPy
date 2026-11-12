"""schema4纯结构反例；不冒充实际PE/Native执行。"""
import copy
import hashlib
import json
from pathlib import Path
import sys
import unittest
ROOT=Path(__file__).resolve().parents[3]
sys.path[:0]=[str(ROOT/'tests/aot'),str(ROOT/'tests/validation')]
import schema4 as current
from manifest_options import BOOL_OPTIONS


def sample():
    method={'owner':'RyuPy.Generated.Module','name':'init','signature':'void ()'}
    sources=[{'module_id':0,'module_name':'__main__','package':'','logical_source':'entry/main.py','compiled_text_sha256':'0'*64},
             {'module_id':1,'module_name':'sample','package':'','logical_source':'modules/sample.py','compiled_text_sha256':'1'*64}]
    locations=[{'module_id':s['module_id'],'module_name':s['module_name'],'kind':'executable_relative','relative_file':'_internal/resources/v1/'+s['logical_source']} for s in sources]
    candidate={'schema_version':3,'roots':['sample'],'dependencies':{'sample':['external']},
       'locations':{'__main__':{'package':'','file':'main.py','kind':2,'path':locations[0]['relative_file'],'is_package':False,'registration':'active'},
                    'sample':{'package':'','file':'sample.py','kind':2,'path':locations[1]['relative_file'],'is_package':False,'registration':'candidate'}},
       'deferred':{},'outside':[],'protected':[],'closed_unplanned':[],'external_dependencies':['external']}
    raw=json.dumps(candidate,separators=(',',':'))
    options={k:False for k in BOOL_OPTIONS};options.update(instance_modules=[],module_environment_policy='ordinary-dynamic-module-pilot',distribution_metadata={'checked':True})
    data={'schema_version':4,'kind':'ryupy-native-aot-artifact',
      'compiler':{'package_version':'0.1.0','build_revision':'a'*40,'codegen_report_version':2,'options':options},
      'target':{'rid':'linux-x64','native_format':'ELF','architecture':'x64'},'toolchain':{'sdk_version':'10.0.403','target_framework_major':10},
      'entry':{'module_id':0,'managed_entry':method},'sources':sources,
      'modules':[{'id':0,'name':'__main__','init':method,'getter':method,'setter':method,'exports':[],'environment_reason':'aot-not-migrated'},
                 {'id':1,'name':'sample','init':method,'getter':None,'setter':None,'exports':[],'environment_reason':'compiler-catalog-authenticated-source'}],
      'emitted_delegate_references':[],
      'build_inputs':[{'relative_path':'managed-entry.dll','role':'compiled_managed_input','size':1,'sha256':'2'*64},
                      {'relative_path':'RyuPy.Runtime.dll','role':'runtime_managed_input','size':1,'sha256':'3'*64}],
      'artifacts':[{'relative_path':'app','role':'native_executable','size':64,'sha256':'4'*64}],
      'scope':{**{k:False for k in current.SCOPE},'declared_resources_complete':False,'automatic_module_locations_complete':True},
      'module_locations':locations,'resources':[],
      'default_source_plan':{'schema_version':1,'mode':'automatic_locations','candidate_payload_sha256':hashlib.sha256(raw.encode()).hexdigest(),'automatic_instance_modules':['sample'],'deferred_sources':[]}}
    return data,raw,candidate


def validate(data):
    current.validate_schema4(data,'linux-x64','10.0.403',expected_automatic=('sample',),metadata_report={'checked':True})


def bind(data,payload):
    raw=json.dumps(payload,separators=(',',':'))
    data['default_source_plan']['candidate_payload_sha256']=hashlib.sha256(raw.encode()).hexdigest()
    current.bind_candidate(data,raw,payload)


class SchemaTests(unittest.TestCase):
    def test_valid_and_unchanged(self):
        data,raw,payload=sample();old=copy.deepcopy(data);validate(data);current.bind_candidate(data,raw,payload)
        self.assertEqual(data,old)

    def test_nonempty_reference_path(self):
        data,_,_=sample(); method=data['modules'][0]['init']
        ref={'site':{'source_method':method,'instruction':1},'target':method,
             'target_display':'Module.init','role':'module_init','module_id':0,
             'function_id':1,'class_id':None}
        data['emitted_delegate_references']=[ref]; validate(data)
        data['emitted_delegate_references'].append(copy.deepcopy(ref))
        with self.assertRaises(ValueError):validate(data)
        data['emitted_delegate_references']=[ref];ref['module_id']=99
        with self.assertRaises(ValueError):validate(data)

    def test_manifest_mutations(self):
        mutations=[lambda x:x.update(schema_version=2),lambda x:x.update(extra=True),
          lambda x:x['scope'].update(resources_included=True),lambda x:x['scope'].update(declared_resources_complete=True),
          lambda x:x['scope'].update(automatic_module_locations_complete=False),lambda x:x['scope'].update(native_retention_proven=True),
          lambda x:x['compiler']['options'].update(unknown=True),lambda x:x['compiler']['options'].update(instance_modules=['sample']),
          lambda x:x['compiler']['options'].update(distribution_metadata={}),lambda x:x['sources'][1].update(module_id=4),
          lambda x:x['modules'][1].update(getter=x['modules'][0]['getter']),lambda x:x['modules'][1].update(environment_reason='explicit-instance'),
          lambda x:x['module_locations'][1].update(relative_file='_internal/resources/v1/modules/no.py'),
          lambda x:x['module_locations'].pop(),lambda x:x['default_source_plan'].update(automatic_instance_modules=[]),
          lambda x:x['artifacts'].append(copy.deepcopy(x['artifacts'][0])),lambda x:x['build_inputs'].pop(),
          lambda x:x['resources'].append({}),lambda x:x['toolchain'].update(sdk_version='10.0.404')]
        for mutate in mutations:
            data,_,_=sample();mutate(data)
            with self.subTest(mutate=mutate),self.assertRaises(ValueError):validate(data)

    def test_candidate_mutations(self):
        mutations=[lambda p:p.update(schema_version=2),lambda p:p.update(unknown=True),lambda p:p['locations']['sample'].update(kind=1),
          lambda p:p['locations']['sample'].update(file='/bad'),lambda p:p['locations']['sample'].update(package=None),
          lambda p:p['locations']['sample'].update(path='_internal/resources/v1/bad.py'),lambda p:p.update(roots=['__main__']),
          lambda p:p.update(outside=['sample']),lambda p:p.update(closed_unplanned=['sample']),
          lambda p:p.update(external_dependencies=[]),lambda p:p.update(external_dependencies=['external','extra']),
          lambda p:p.update(protected=['external']),lambda p:p.update(roots=['sample','sample'])]
        for mutate in mutations:
            data,_,payload=sample();mutate(payload)
            with self.subTest(mutate=mutate),self.assertRaises(ValueError):bind(data,payload)

    def test_protected_overlap_not_permission(self):
        data,_,payload=sample();payload['protected']=['sample','other'];payload['outside']=['other'];bind(data,payload)
        # 配置合法不等于Activate已通过；新external仍须精确剩余。
        payload['external_dependencies'].append('sample')
        with self.assertRaises(ValueError):bind(data,payload)

    def test_deferred_coverage(self):
        data,_,payload=sample();d={'module_name':'bad','package':'','logical_source':'modules/bad.py','source_sha256':'5'*64,
          'relative_file':'_internal/resources/v1/modules/bad.py','is_package':False,'reason':'unsupported'}
        data['default_source_plan']['deferred_sources']=[d]
        payload['deferred']={'bad':'unsupported'};payload['roots'].append('bad')
        payload['locations']['bad']={'package':'','file':'bad.py','kind':2,'path':d['relative_file'],'is_package':False,'registration':'deferred'}
        bind(data,payload);validate(data)
        payload['locations']['bad']['registration']='candidate'
        with self.assertRaises(ValueError):bind(data,payload)


if __name__=='__main__':unittest.main()
