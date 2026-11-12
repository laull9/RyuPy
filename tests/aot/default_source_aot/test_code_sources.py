"""来源边和阶段顺序有限反例；实际旧PE诊断另存，不冒充发射前MIR。"""
import copy
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
from test_pe_checks import fixture
from pe_checks import cold_registration, signature
from code_source_checks import source_blocks, object_tokens
sys.path.insert(0, str(Path(__file__).resolve().parent))
import run as runner
from support import Runner


def validate(d,o,r,p,t):
    return cold_registration(d,o,r,p,t,d['_layout'])


class CodeSourceTests(unittest.TestCase):
    def test_real_class_object_encoding(self):
        self.assertEqual(signature('000112050e',{'0x1000001':'System.Object','__trusted_object_tokens__':['0x1000001']}),(0,'object',('string',)))
        self.assertEqual(signature('00001205',{'0x1000001':'System.Object','__trusted_object_tokens__':['0x1000001']}),(0,'object',()))
        self.assertNotEqual(signature('000112050e',{'0x1000001':'Other.Object'}),(0,'object',('string',)))

    def test_object_assembly_scope(self):
        strings={0:'',1:'Object',2:'System',3:'System.Runtime',4:'Other.Runtime'}
        def model():
            return {'tables':{1:[[6,1,2]],2:[],35:[[10,0,0,0,0,1,3,0,0]]},
                    'string':strings.__getitem__,'signature':lambda _: 'b03f5f7f11d50a3a'}
        self.assertEqual(object_tokens(model()),['0x1000001'])
        mutations=[lambda m:m['tables'][2].append([0,1,2,0,1,1]),
                   lambda m:m['tables'][1][0].__setitem__(0,4),
                   lambda m:m['tables'][35][0].__setitem__(6,4),
                   lambda m:m['tables'][35][0].__setitem__(0,9),
                   lambda m:m.update(signature=lambda _: 'wrong')]
        for mutate in mutations:
            m=model();mutate(m)
            with self.subTest(mutate=mutate),self.assertRaises(ValueError):object_tokens(m)
        with self.assertRaises(ValueError):signature('000112050e',{'0x1000001':'System.Object'})

    def test_old_oracle_is_not_silently_completed(self):
        d,o,r,p,t=fixture();del o['code_sources']
        with self.assertRaises(ValueError):validate(d,o,r,p,t)

    def test_preemit_and_field_mutations(self):
        mutations=[lambda d,o:o['code_sources'][0].update(entry_fn=99),
          lambda d,o:o['code_sources'][0].update(source_file='wrong.py'),
          lambda d,o:o['code_sources'][0].update(global_id=1),
          lambda d,o:o['code_sources'].append(copy.deepcopy(o['code_sources'][0])),
          lambda d,o:o['static_globals'].reverse(),
          lambda d,o:d['_layout']['fields']['0x4000001'].update(owner='Other'),
          lambda d,o:d['_layout']['fields']['0x4000001'].update(slot=2),
          lambda d,o:d['_layout']['fields']['0x4000001'].update(flags=6),
          lambda d,o:d['_layout']['fields']['0x4000001'].update(signature_hex='060e')]
        for mutate in mutations:
            d,o,r,p,t=fixture();mutate(d,o)
            with self.subTest(mutate=mutate),self.assertRaises(ValueError):validate(d,o,r,p,t)

    def test_actual_sources_mutations(self):
        for index,update in [(0,{'string':'wrong'}),(1,{'string':'wrong.py'}),
                             (3,{'token':'0x6000002'}),(4,{'opcode':'0x28'}),
                             (5,{'opcode':'0x6f'}),(6,{'field_token':'0x4000002'})]:
            d,o,r,p,t=fixture();source=next(m for m in d['methods'] if m['method']['name']=='__init_code_sources')
            source['body']['instructions'][index].update(update)
            with self.subTest(index=index),self.assertRaises(ValueError):validate(d,o,r,p,t)
        d,o,r,p,t=fixture(True);source=next(m for m in d['methods'] if m['method']['name']=='__init_code_sources')
        source['body']['instructions'][2]['opcode']='0xfe06'
        with self.assertRaises(ValueError):validate(d,o,r,p,t)

    def test_call_chain_and_extra_store(self):
        for mutation in ('missing','early','branch','repeat','store','early-body','unknown-framework'):
            d,o,r,p,t=fixture();main=d['methods'][-1]
            if mutation=='missing':main['body']['instructions'][0]['token']='0x6000002'
            if mutation=='early':main['body']['instructions'][:2]=main['body']['instructions'][:2][::-1]
            if mutation=='branch':main['body']['instructions'].insert(0,dict(opcode='0x2b'))
            if mutation=='repeat':main['body']['instructions'].insert(0,dict(opcode='0x28',token='0x6000010'))
            if mutation=='early-body':main['body']['instructions'].insert(0,dict(opcode='0x28',token='0x6000002',target=d['methods'][1]['method']))
            if mutation=='unknown-framework':main['body']['instructions'].insert(0,dict(opcode='0x28',token='0xa000001',target=dict(owner='Unknown',name='Invoke',signature_hex='000001')))
            if mutation=='store':main['body']['instructions'].insert(-1,dict(opcode='0x80',field_token='0x4000001'))
            with self.subTest(mutation=mutation),self.assertRaises(ValueError):validate(d,o,r,p,t)

    def test_phase_order_and_failure_retention(self):
        for failure in (None,'prepare','default-jit','structure'):
            with tempfile.TemporaryDirectory() as d:
                r=Runner.__new__(Runner);r.out=Path(d);r.result={'stages':{},'passed':False};r.before=[]
                seen=[]
                def step(name):
                    def action(_):
                        seen.append(name)
                        if name==failure:raise ValueError('synthetic-stop')
                    return action
                with patch.object(runner,'context'),patch.object(runner,'Runner',return_value=r), \
                     patch.object(runner,'prepare',step('prepare')),patch.object(runner,'managed',step('default-jit')), \
                     patch.object(runner,'structure',step('structure')),patch.object(runner,'native',step('default-native')), \
                     patch.object(runner,'source_identity',return_value=[]):
                    if failure:
                        with self.assertRaises(ValueError):runner.main()
                    else:runner.main()
                sequence=['prepare','default-jit','structure','default-native']
                self.assertEqual(seen,sequence if failure is None else sequence[:sequence.index(failure)+1])
                self.assertEqual(r.result['passed'],failure is None)
                if failure=='structure':self.assertTrue(r.result['stages']['default-jit']['passed'])
                self.assertTrue((r.out/'result.json').is_file())


if __name__=='__main__':unittest.main()
