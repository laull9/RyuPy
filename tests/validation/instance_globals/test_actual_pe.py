# 实际已发射PE的结构变异反例；只JSON纯测，不执行程序集。
import copy
import json
import os
from pathlib import Path
import unittest
from check_actual_pe import check
from metadata_contract import expected_for_origin

HOSTED = os.environ.get('A1_EXPECTED_HOSTED') == '1'
DATA = json.loads(Path(os.environ['A1_DECODED_PE']).read_text())

class PeChecks(unittest.TestCase):
    def mutate(self, change):
        data = copy.deepcopy(DATA)
        for method in data['methods']:
            ins = method['body']['instructions']
            for n, row in enumerate(ins):
                if row.get('target', {}).get('name') == 'CallDirectGlobals':
                    change(ins, n)
                    with self.assertRaises(ValueError): check(data, hosted=HOSTED)
                    return
        self.fail('原件无helper')
    def test_actual_four(self): self.assertEqual(len(check(DATA, hosted=HOSTED)['actual_helper_calls']), 6 if HOSTED else 4)
    def test_wrong_owner(self): self.mutate(lambda ins, n: ins[n]['target'].update(owner='Wrong'))
    def test_wrong_signature(self): self.mutate(lambda ins, n: ins[n]['target'].update(signature_hex='000001'))
    def test_memberref_not_call(self): self.mutate(lambda ins, n: ins[n].update(opcode='0xfe06'))
    def test_wrong_context(self): self.mutate(lambda ins, n: ins[n-2].update(opcode='0x14'))
    def test_legacy_wrong_signature(self):
        if not HOSTED: self.skipTest('old graph has no Legacy callback')
        data = copy.deepcopy(DATA)
        method = next(m for m in data['methods'] if m['method']['name'].endswith('_invoke'))
        call = next(i for i in method['body']['instructions'] if i.get('target', {}).get('name') == 'CallValue0')
        call['target']['signature_hex'] = '000001'
        with self.assertRaises(ValueError): check(data, hosted=True)
    def test_legacy_one_wrong_callee(self):
        if not HOSTED: self.skipTest('old graph has no Legacy callback')
        data = copy.deepcopy(DATA)
        method = next(m for m in data['methods'] if m['method']['name'].endswith('_invoke_one'))
        ins = method['body']['instructions']
        n = next(n for n, i in enumerate(ins) if i.get('target', {}).get('name') == 'CallValue1')
        ins[n-2]['opcode'] = '0x14'
        with self.assertRaises(ValueError): check(data, hosted=True)
    def test_metadata_source(self):
        values = expected_for_origin('/deployment/subject.py')
        self.assertEqual(len(values['functional']), 4)
        self.assertEqual(values['default'][0][-1], ['__cached__', ['str', '/deployment/__pycache__/subject.cpython-312.pyc']])
        self.assertEqual(values['indirect'], [])
        with self.assertRaises(ValueError): expected_for_origin('relative.py')

if __name__ == '__main__': unittest.main()
