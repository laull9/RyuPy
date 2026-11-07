"""三分门的失效关闭自测；假数据只验证判定，不计入产品证据。"""
from pathlib import Path
import tempfile
import shutil
from copy import deepcopy
from super_caller_gate import encode_role_inputs
import unittest
from super_contract import (BOUNDARY, CALLER_SOURCES, audit_caller, audit_bindings,
                            check_caller_plan, check_caller_sources, check_rejection, check_test_count)


class SuperContractTests(unittest.TestCase):
    def test_caller_full_ast_and_expression_contract(self):
        path = Path(__file__).parent / 'super_caller/caller.py'
        self.assertEqual(len(audit_caller(path)['functions']), 5)
        with tempfile.TemporaryDirectory() as directory:
            other = Path(directory) / 'caller.py'
            other.write_text(path.read_text().replace('proxy.call(callback())', 'callback()'))
            with self.assertRaises(RuntimeError):
                audit_caller(other)
            other.write_text(path.read_text() + '\ndel alias\n')
            with self.assertRaises(RuntimeError):
                audit_caller(other)

    def test_exact_negative_and_no_stale_outputs(self):
        stderr = '\n'.join('[E0100] Error: ' + BOUNDARY + '\n [ /tmp/subject.py:'
                           + str(line) + ':9 ]' for line in [58, 96])
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'jit'
            check_rejection(1, b'', stderr, output)
            for code, out, err in [(0, b'', stderr), (2, b'', stderr),
                                   (1, b'x', stderr), (1, b'', stderr.replace('58', '59')),
                                   (1, b'', stderr + '\n[E0100] Error: other'),
                                   (1, b'', stderr + '\npanicked')]:
                with self.assertRaises(RuntimeError):
                    check_rejection(code, out, err, output)
            output.mkdir()
            (output / 'entry.dll').write_bytes(b'stale')
            with self.assertRaises(RuntimeError):
                check_rejection(1, b'', stderr, output)

    def test_role_argument_slots_fail_closed(self):
        role = {'role': 'construct', 'function_id': 71, 'state_slot': 2,
                'parameter_slots': {'start': 0, 'receiver': 1}}
        self.assertEqual(encode_role_inputs([role], {'module_init_state_slot': 0}),
                         ('construct\t71\t2\t0\t1\n', '0\n'))
        for change in [{'state_slot': 0}, {'state_slot': -1},
                       {'parameter_slots': {'start': 0}},
                       {'parameter_slots': {'start': 0, 'receiver': 0}}]:
            wrong = deepcopy(role)
            wrong.update(change)
            with self.assertRaises(RuntimeError):
                encode_role_inputs([wrong], {'module_init_state_slot': 0})
        with self.assertRaises(RuntimeError):
            encode_role_inputs([role], {'module_init_state_slot': 65535})

    def test_four_source_bindings_reject_duplicate_classes_and_definitions(self):
        original = Path(__file__).parent / 'super_caller'
        audit = audit_bindings(original)
        self.assertEqual(set(audit['modules']), set(CALLER_SOURCES))
        self.assertEqual(audit['modules']['model']['bindings']['Middle'][0]['kind'], 'class')
        self.assertEqual(audit['modules']['other_model']['bindings']['Middle'][0]['kind'], 'class')
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / 'sources'
            shutil.copytree(original, root)
            model = root / 'model.py'
            before = model.read_text()
            for addition in ['\nclass Middle:\n    pass\n', '\ndef old(self, value):\n    pass\n',
                             '\nMiddle = 1\n', '\nclass Extra(Missing):\n    pass\n']:
                model.write_text(before + addition)
                with self.assertRaises(RuntimeError):
                    audit_bindings(root)

    def test_four_module_plan_and_source_identity_are_required(self):
        plan = {'modules': [{'module': name, 'compiled': True,
                             'execution': 'InstanceGlobals' if name == 'caller' else 'LegacySingleton'}
                            for name in CALLER_SOURCES]}
        check_caller_plan(plan)
        for change in ['missing', 'duplicate', 'instance-peer', 'not-compiled']:
            wrong = deepcopy(plan)
            if change == 'missing':
                wrong['modules'].pop()
            elif change == 'duplicate':
                wrong['modules'][-1] = deepcopy(wrong['modules'][0])
            elif change == 'instance-peer':
                wrong['modules'][-1]['execution'] = 'InstanceGlobals'
            else:
                wrong['modules'][-1]['compiled'] = False
            with self.assertRaises(RuntimeError):
                check_caller_plan(wrong)
        hashes = {name: 'test-only-' + name for name in CALLER_SOURCES}
        sources = [{'module_name': name, 'compiled_text_sha256': digest} for name, digest in hashes.items()]
        check_caller_sources(sources, hashes)
        for wrong in [sources[:-1], sources[:-1] + [sources[0]],
                      sources[:-1] + [{'module_name': 'other_model', 'compiled_text_sha256': 'wrong'}]]:
            with self.assertRaises(RuntimeError):
                check_caller_sources(wrong, hashes)
        with self.assertRaises(RuntimeError):
            check_caller_sources(sources, {'caller': hashes['caller']})

    def test_named_actual_nonzero_tests(self):
        good = 'test module::actual_gate ... ok\ntest result: ok. 1 passed; 0 failed; 0 ignored;'
        check_test_count(good, 'actual_gate')
        for bad in [good.replace('actual_gate', 'wrong'), good.replace('1 passed', '0 passed'), '']:
            with self.assertRaises(RuntimeError):
                check_test_count(bad, 'actual_gate')


if __name__ == '__main__':
    unittest.main()
