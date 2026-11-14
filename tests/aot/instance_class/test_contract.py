"""重建纯反例草稿；旧10项通过原流已不可读，本文件尚未重新执行。"""
import copy
import tempfile
import unittest
from pathlib import Path
from contract import anchors, no_resources, positive, verify_plan, parse


class ContractTests(unittest.TestCase):
    def setUp(self):
        self.sources = {'pkg': {'path': 'pkg/__init__.py', 'sha256': 'a' * 64},
                        'pkg.child': {'path': 'pkg/child.py', 'sha256': 'b' * 64}}
        self.record = {'exit': 0, 'failure': None, 'raw_streams_complete': True}

    def test_complete_virtual_anchors(self):
        self.assertEqual(anchors(self.sources), {'schema_version': 1, 'modules': [
            {'module': 'pkg', 'files': []}, {'module': 'pkg.child', 'files': []}]})

    def test_missing_parent(self):
        del self.sources['pkg']
        with self.assertRaises(ValueError): anchors(self.sources)

    def test_parent_is_not_package(self):
        self.sources['pkg']['path'] = 'pkg.py'
        with self.assertRaises(ValueError): anchors(self.sources)

    def test_empty_total(self):
        with self.assertRaises(ValueError): anchors({})

    def test_path_escape(self):
        self.sources['pkg']['path'] = '../pkg/__init__.py'
        with self.assertRaises(ValueError): anchors(self.sources)

    def test_duplicate_json(self):
        with self.assertRaises(ValueError): parse('{"modules":[],"modules":[]}')

    def test_exact_streams(self):
        positive(self.record, b'ok\n', b'', b'ok\n')
        for stdout, stderr in [(b'ok', b''), (b'ok\n', b'warning')]:
            with self.assertRaises(ValueError): positive(self.record, stdout, stderr, b'ok\n')

    def test_crash_and_capture_failure_never_pass(self):
        for code in [-11, -6, 134, 139, 2, None]:
            with self.assertRaises(ValueError): positive(dict(self.record, exit=code), b'ok', b'', b'ok')
        for mutation in [{'failure': 'launch failed'}, {'raw_streams_complete': False}]:
            with self.assertRaises(ValueError): positive(dict(self.record, **mutation), b'ok', b'', b'ok')

    def test_original_no_resources_is_specific(self):
        diagnostic = '[E0100] Error: 显式实例 AOT 需要资源清单且不支持原生绑定\n'.encode()
        with tempfile.TemporaryDirectory() as root:
            good = dict(self.record, exit=1)
            no_resources(good, b'', diagnostic, root)
            for code in [-11, -6, 134, 139, 2, 0]:
                with self.assertRaises(ValueError): no_resources(dict(good, exit=code), b'', diagnostic, root)
            with self.assertRaises(ValueError): no_resources(good, b'', b'[E0100] unrelated', root)
            Path(root, 'entry.dll').write_bytes(b'not PE')
            with self.assertRaises(ValueError): no_resources(good, b'', diagnostic, root)

    def test_actual_plan_not_jit_selection(self):
        plan = {'modules': [{'module': n, 'compiled': True, 'execution':
                            'InstanceGlobals' if n == 'pkg.child' else 'LegacySingleton',
                            'reason': 'explicit-instance' if n == 'pkg.child' else 'aot-not-migrated'}
                           for n in ['__main__', 'pkg', 'pkg.child']]}
        verify_plan(plan, self.sources, ['pkg.child'])
        for change in ['automatic', 'missing', 'duplicate', 'fallback', 'not-compiled']:
            bad = copy.deepcopy(plan)
            if change == 'automatic': bad['modules'][1]['execution'] = 'InstanceGlobals'
            if change == 'missing': bad['modules'].pop()
            if change == 'duplicate': bad['modules'].append(bad['modules'][0])
            if change == 'fallback': bad['modules'][-1]['execution'] = 'LegacySingleton'
            if change == 'not-compiled': bad['modules'][1]['compiled'] = False
            with self.assertRaises(ValueError, msg=change): verify_plan(bad, self.sources, ['pkg.child'])


if __name__ == '__main__':
    unittest.main()
