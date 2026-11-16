"""合成PE解码数据只检查检查器拒绝路径，绝不作为实际PE/Native证明。"""
import copy
import unittest
from pe_contract import check, definitions


class PeTests(unittest.TestCase):
    def data(self):
        def loc(name, slot): return {'owner': 'Module', 'name': name, 'slot': slot}
        def method(name, token, ins): return {'method': {'owner': 'Module', 'name': name}, 'token': token,
                                             'body': {'instructions': ins}}
        target = {'owner': 'RyuPy.Runtime.PyModuleInstances', 'name': 'CreateOwnedFunction'}
        decoded = {'sha256': 'pe', 'methods': [
            method('init', '0x6000001', [
                {'opcode':'0xfe06','token':'0x6000003','offset':0},
                {'opcode':'0x73','target':{'owner':'RyuPy.Runtime.PyOwnedCall','name':'.ctor'},'offset':6},
                {'opcode':'0x80','field_token':'0x4000001','offset':11},
                {'opcode':'0x7e','field_token':'0x4000001','offset':16},
                {'opcode':'0x28','target':target,'offset':21}]),
            method('core','0x6000002',[{'opcode':'0x2a','offset':0}]),
            method('thunk','0x6000003',[{'opcode':'0x28','token':'0x6000002','offset':0}]) ]}
        expected={'pe':{'sha256':'pe'},'owners':[{'function_id':4,'core':loc('core',1),
            'calls':1,'root':{'source':loc('init',0),'target':loc('thunk',2),'instruction':0}}]}
        mapping=[{'function_id':4,'module':'subject','lexical_qualname':'A.m','firstlineno':2,'source_sha256':'src'}]
        sources={'subject':{'text':'class A:\n def m(self): pass\n','sha256':'src'}}
        return decoded,expected,mapping,sources

    def test_ast_full_not_execution_coverage(self):
        self.assertEqual(definitions('if False:\n class A:\n  def m(self):\n   def nested(): pass\n'), [('A.m',3)])

    def test_synthetic_control(self):
        self.assertEqual(check(*self.data())['owners'],1)

    def test_only_four_timestamp_bytes_may_differ(self):
        from contract import pe_timestamp_equivalence
        a=bytearray(128); a[:2]=b'MZ'; a[60:64]=(64).to_bytes(4,'little');a[64:68]=b'PE\0\0'
        b=bytearray(a);b[72:76]=b'1234'
        self.assertFalse(pe_timestamp_equivalence(bytes(a),bytes(b))['byte_identical'])
        self.assertTrue(pe_timestamp_equivalence(bytes(a),bytes(a))['byte_identical'])
        for offset in [0,60,64,71,76,100]:
            bad=bytearray(b);bad[offset]^=1
            with self.assertRaises(ValueError):pe_timestamp_equivalence(bytes(a),bytes(bad))
        with self.assertRaises(ValueError):pe_timestamp_equivalence(bytes(a),bytes(b[:-1]))

    def test_mutations(self):
        for mutation in ['hash','fnid','line','source','core','root','pointer','ctor','store','call','create','duplicate']:
            d,e,m,s=self.data()
            if mutation=='hash':d['sha256']='other'
            if mutation=='fnid':m[0]['function_id']=5
            if mutation=='line':m[0]['firstlineno']=3
            if mutation=='source':m[0]['source_sha256']='other'
            if mutation=='core':e['owners'][0]['core']['slot']=2
            if mutation=='root':e['owners'][0]['root']['instruction']=1
            if mutation=='pointer':d['methods'][0]['body']['instructions'][0]['token']='0x6000002'
            if mutation=='ctor':d['methods'][0]['body']['instructions'][1]['target']['owner']='Fake'
            if mutation=='store':d['methods'][0]['body']['instructions'][2]['opcode']='0x0'
            if mutation=='call':d['methods'][2]['body']['instructions'][0]['opcode']='0x0'
            if mutation=='create':d['methods'][0]['body']['instructions'][-1]['target']['owner']='Fake'
            if mutation=='duplicate':m.append(copy.deepcopy(m[0]))
            with self.assertRaises(ValueError,msg=mutation):check(d,e,m,s)


if __name__=='__main__':unittest.main()
