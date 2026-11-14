"""新重建的纯模型反例；不冒真实PE或Native。"""
import copy
import hashlib
import json
import unittest
from pe_checks import cold_registration, validate_cold_abi, signature


def fixture(instance=False):
    def ins(op, **kw): return dict(opcode=op, **kw)
    def text(value): return ins('0x72', string=value)
    def loc(name, slot): return dict(owner='Module', name=name, slot=slot, signature='locator only')
    def method(name, token, body, sig='000001'): return dict(method=dict(owner='Module',name=name,signature_hex=sig), flags=0x10, token=token, body=dict(instructions=body))
    location = dict(package='', file='entry.py', kind=2, path='sources/entry.py', is_package=False, registration='candidate')
    candidate = dict(schema_version=3, locations={'entry':location}, deferred={})
    payload = json.dumps(candidate)
    oracle = dict(modules=[dict(module_id=0,name='entry',init_fn=8,execution='InstanceGlobals' if instance else 'LegacySingleton')],
                  functions=[dict(function_id=8,codegen_name='entry_init')], sources=[dict(module_id=0,module_name='entry',package='')],
                  source_to_emitted=[0],candidate_payload_sha256=hashlib.sha256(payload.encode()).hexdigest())
    body = [text('entry'),text(''),text('entry.py'),ins('0x21',operand_hex='0200000000000000'),text('sources/entry.py')]
    if not instance: body.append(ins('0x17'))
    body.append(ins('0x16'))
    roots=[]; methods=[method('__init_functions','0x6000001',body)]
    roles = ['module_init'] if instance else ['module_init','module_getter','module_setter']
    types={'0x1000001':'RyuPy.Runtime.PyModuleInit','0x1000002':'RyuPy.Runtime.PyModuleGet',
           '0x1000003':'RyuPy.Runtime.PyModuleSet','0x1000004':'RyuPy.Runtime.PyModuleExecute','0x1000005':'RyuPy.Runtime.PyModule'}
    for i,role in enumerate(roles):
        name = 'entry_init' if i==0 else role
        token=hex(0x6000002+i);body.append(ins('0x14'));pos=len(body)
        delegate='PyModuleExecute' if instance else ['PyModuleInit','PyModuleGet','PyModuleSet'][i]
        body.extend([ins('0xfe06',token=token),ins('0x73',target=dict(owner='RyuPy.Runtime.'+delegate,name='.ctor',signature_hex='2002011c18'))])
        roots.append(dict(role=role,module_id=0,function_id=None,source=loc('__init_functions',0),target=loc(name,i+1),instruction=pos))
        methods.append(method(name,token,[ins('0x2a')], '0001011c' if instance else ['000001','00011c0e','00030112150e1c'][i]))
    register='RegisterCandidateInstance' if instance else 'RegisterCandidateSource'
    sig='0007010e0e0e0a0e021211' if instance else '000a010e0e0e0a0e020212051209120d'
    body.extend([ins('0x28',target=dict(owner='RyuPy.Runtime.PyModules',name=register,signature_hex=sig)),text(payload),
                 ins('0x28',target=dict(owner='RyuPy.Runtime.PyModules',name='ConfigureOptionalSources',signature_hex='0001010e')),ins('0x2a')])
    source_token='0x6000010'; wrapper_token='0x6000011'; main_token='0x6000012'
    body.insert(len(body)-1,ins('0x28',token=source_token))
    code_body=[text('entry'),text('entry.py'),ins('0x14')]
    if not instance:
        code_body += [ins('0xfe06',token='0x6000003'),ins('0x73',target=dict(owner='RyuPy.Runtime.PyModuleGet',name='.ctor',signature_hex='2002011c18'))]
    code_body += [ins('0x28',target=dict(owner='RyuPy.Runtime.PyTraceback',name='CreateCodeSource' if instance else 'CreateLegacyCodeSourceV1',signature_hex='00031c0e0e1209')),
                  ins('0x80',field_token='0x4000001'),ins('0x2a')]
    methods += [method('__init_code_sources',source_token,code_body),
                method('__run_main',wrapper_token,[ins('0x28',target=dict(owner='RyuPy.Runtime.PyModules',name='RunMain',signature_hex='000001')),ins('0x2a')]),
                method('Main',main_token,[ins('0x28',token='0x6000001'),ins('0x28',token=wrapper_token),ins('0x2a')])]
    # Mock仅模拟顺序；实际fixture的完整前置序列允许冷常量/委托。
    body.pop(-2);body.insert(0,ins('0x28',token=source_token))
    for root in roots: root['instruction']+=1
    oracle['static_globals']=[dict(global_id=0,name='source',storage='CodeSource'),dict(global_id=1,name='name',storage='LegacyStatic')]
    oracle['code_sources']=[dict(global_id=0,name='source',module_id=0,entry_fn=8,source_file='entry.py',legacy_entry=None if instance else dict(initial_name='entry',name_global=1))]
    layout={'entry_token':main_token,'fields':{'0x4000001':dict(owner='Module',slot=0,name='source',flags=17,signature_hex='061c'),
                                           '0x4000002':dict(owner='Module',slot=1,name='name',flags=17,signature_hex='061c')}}
    if not instance:
        roots.append(dict(role='module_getter',module_id=0,function_id=None,source=loc('__init_code_sources',len(roles)+1),target=loc('module_getter',2),instruction=3))
    return dict(sha256='synthetic',methods=methods,_layout=layout),oracle,roots,payload,types


class PeTests(unittest.TestCase):
    def test_models_both_domains(self):
        for instance in (False,True):
            d,o,r,p,t=fixture(instance)
            self.assertEqual(cold_registration(d,o,r,p,t,d['_layout'])['static_roots'],1 if instance else 3)
            validate_cold_abi(d,t)

    def test_oracle_mutations(self):
        for change in (lambda o:o['modules'][0].update(execution='unknown'),
                       lambda o:o['functions'].append(copy.deepcopy(o['functions'][0])),
                       lambda o:o['functions'][0].update(codegen_name='wrong'),
                       lambda o:o.update(source_to_emitted=[0,0]),
                       lambda o:o['sources'][0].update(module_name='other'),
                       lambda o:o.update(candidate_payload_sha256='wrong')):
            with self.subTest(change=change):
                d,o,r,p,t=fixture();change(o)
                with self.assertRaises(ValueError): cold_registration(d,o,r,p,t,d['_layout'])

    def test_locator_mutations(self):
        for change in (lambda r:r.append(copy.deepcopy(r[0])),lambda r:r.pop(),
                       lambda r:r[0].update(role='unclassified'),lambda r:r[0].update(module_id=99),
                       lambda r:r[0]['target'].update(slot=99),lambda r:r[0].update(instruction=1)):
            d,o,r,p,t=fixture();change(r)
            with self.assertRaises(ValueError): cold_registration(d,o,r,p,t,d['_layout'])

    def test_actual_edges_and_arguments(self):
        for index,update in ((1,dict(string='other')),(4,dict(operand_hex='0100000000000000')),
                             (8,dict(opcode='0x16')),(9,dict(token='0x6000004')),
                             (10,dict(opcode='0x28')),(17,dict(opcode='0x6f'))):
            d,o,r,p,t=fixture();d['methods'][0]['body']['instructions'][index].update(update)
            with self.assertRaises(ValueError): cold_registration(d,o,r,p,t,d['_layout'])

    def test_extra_actual_source_root(self):
        d,o,r,p,t=fixture();d['methods'][1]['body']['instructions'].insert(0,dict(opcode='0xfe06',token='0x6000002'))
        with self.assertRaises(ValueError): cold_registration(d,o,r,p,t,d['_layout'])

    def test_extra_registration(self):
        d,o,r,p,t=fixture();body=d['methods'][0]['body']['instructions'];body.append(copy.deepcopy(body[17]))
        with self.assertRaises(ValueError): cold_registration(d,o,r,p,t,d['_layout'])

    def test_abi_changes(self):
        for sig in ('000001','0001010e00','2001010e','00010112ff'):
            d,_,_,_,t=fixture();d['methods'][0]['body']['instructions'][-2]['target']['signature_hex']=sig
            with self.assertRaises(ValueError): validate_cold_abi(d,t)

    def test_target_methoddef_abi(self):
        for instance in (False, True):
            for slot in range(1, 2 if instance else 4):
                for field, value in (('flags', 0), ('signature_hex', '200001'),
                                     ('signature_hex', '0001010e')):
                    d,o,r,p,t=fixture(instance)
                    if field == 'flags': d['methods'][slot][field] = value
                    else: d['methods'][slot]['method'][field] = value
                    with self.subTest(instance=instance,slot=slot,field=field,value=value), self.assertRaises(ValueError):
                        cold_registration(d,o,r,p,t,d['_layout'])

    def test_unknown_type_and_truncated(self):
        for sig in ('','00010112','0001011205'):
            with self.assertRaises(ValueError): signature(sig,{})


if __name__ == '__main__': unittest.main()
