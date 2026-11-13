"""metadata schema4固定fixture字段负测；实际PE另由官方原件回放检查。"""
import copy
import json
from pathlib import Path
import sys
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'aot/default_source_aot'))
from test_schema4 import sample
import distribution_metadata_schema4 as c


def fixture():
    data,_,_=sample(); report={'checked':True}
    source_map={name:str(i)*64 for i,(name,_,_) in enumerate(c.SOURCES)}
    method=data['modules'][0]['getter']
    data['sources']=[{'module_id':i,'module_name':name,'package':package,'logical_source':path,
        'compiled_text_sha256':source_map[name]} for i,(name,package,path) in enumerate(c.SOURCES)]
    data['modules']=[{'id':i,'name':name,'init':method,'getter':method,'setter':method,
        'exports':[],'environment_reason':'aot-not-migrated'} for i,(name,_,_) in enumerate(c.SOURCES)]
    data['module_locations']=[{'module_id':s['module_id'],'module_name':s['module_name'],
        'kind':'executable_relative','relative_file':'_internal/resources/v1/'+s['logical_source']} for s in data['sources']]
    data['default_source_plan']['automatic_instance_modules']=[]
    return data,report,source_map


class MetadataSchemaTests(unittest.TestCase):
    def test_exact_synthetic_schema_and_source_policy(self):
        d,r,s=fixture();before=copy.deepcopy(d);c.validate_plan_contract(d,'10.0.403',r,s)
        self.assertEqual(d,before)
        changes=[lambda x:x['compiler']['options'].update(module_environment_policy='explicit-only'),
            lambda x:x['default_source_plan'].update(automatic_instance_modules=['probe_pkg']),
            lambda x:x['sources'][1].update(compiled_text_sha256='e'*64),
            lambda x:x['sources'][1].update(module_name='replacement'),
            lambda x:x['sources'].pop(),
            lambda x:x['modules'][1].update(environment_reason='other'),
            lambda x:x['modules'][1].update(getter=None,setter=None),
            lambda x:x['scope'].update(declared_resources_complete=True),
            lambda x:x['module_locations'][1].update(relative_file='_internal/resources/v1/fake.py')]
        for change in changes:
            bad=copy.deepcopy(d);change(bad)
            with self.assertRaises(ValueError):c.validate_plan_contract(bad,'10.0.403',r,s)
        with self.assertRaises(ValueError):c.validate_plan_contract(d,'10.0.403',r,{})
        with self.assertRaises(ValueError):c.validate_plan_contract(d,'10.0.403',r,s|{'extra':'f'*64})

    def test_closed_targets_are_independent_exact_fixture_expectation(self):
        expected=c.expected_candidate()
        self.assertEqual(expected['closed_unplanned'],['absent_path_key','absent_target'])
        self.assertEqual(set(expected['locations']),{x[0] for x in c.SOURCES})
        self.assertEqual(expected['roots'],[])
        c.validate_candidate(json.dumps(expected))
        for key,value in [('closed_unplanned',[]),('closed_unplanned',['replacement']),
                          ('roots',['probe_pkg']),('outside',['other']),('protected',[])]:
            bad=copy.deepcopy(expected);bad[key]=value
            with self.assertRaises(ValueError):c.validate_candidate(json.dumps(bad))
        bad=copy.deepcopy(expected);bad['locations']['extra']=bad['locations']['probe_pkg']
        with self.assertRaises(ValueError):c.validate_candidate(json.dumps(bad))

if __name__=='__main__':unittest.main()
