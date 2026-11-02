"""不启动SDK或被测程序的轻工具校验：固定矩阵、阈值与逐字输出。"""
import unittest
import os
from pathlib import Path
import tempfile
from unittest.mock import patch
from lazy_build import cargo_proxy
import lazy_schema as schema


def evidence():
    records = []
    for repeat, name, arm in schema.plan():
        candidate = arm.startswith('C-')
        records.append({'repeat': repeat, 'fixture': name, 'arm': arm,
                        'wall_ms': 90 if candidate else 100,
                        'stats': {'alloc_bytes': 70 if candidate else 100}})
    builds = [{'version': v, 'fixture': n, 'native_file_bytes': 1000}
              for v in ('B', 'C') for n in schema.FIXTURES]
    return records, builds


class LazyPairTests(unittest.TestCase):
    def test_plan_and_missing_reordered_records_fail(self):
        self.assertEqual(len(schema.plan()), 120)
        self.assertEqual(len(set(schema.plan())), 120)
        records, builds = evidence()
        self.assertFalse(schema.report(records, builds)['blocking'])
        with self.assertRaises(ValueError):
            schema.report(records[:-1], builds)
        records[0], records[1] = records[1], records[0]
        with self.assertRaises(ValueError):
            schema.report(records, builds)
        with self.assertRaises(ValueError):
            schema.report(evidence()[0], builds[:-1])

    def test_thresholds_are_conjunctive_and_keep_negative_points(self):
        records, builds = evidence()
        for item in records:
            if item['fixture'] == 'import_re' and item['arm'] == 'C-J':
                item['wall_ms'] = 106
        report = schema.report(records, builds)
        self.assertTrue(any('五点同退' in s for s in report['blocking']))
        # 一点更快后仍保坏median，但不伪装满足五点一致退化规则。
        records[next(i for i, r in enumerate(records)
                     if r['fixture'] == 'import_re' and r['arm'] == 'C-J')]['wall_ms'] = 99
        report = schema.report(records, builds)
        self.assertFalse(any('五点同退' in s for s in report['blocking']))
        self.assertGreater(report['cases']['import_re-J']['wall_ratio'], 1)

    def test_adoption_gain_and_size_review_not_automatic_pass(self):
        records, builds = evidence()
        for r in records:
            if r['fixture'] == 'hello' and r['arm'] == 'C-A':
                r['wall_ms'] = 99
                r['stats']['alloc_bytes'] = 99
        builds[-1]['native_file_bytes'] = 1020
        report = schema.report(records, builds)
        self.assertTrue(any('分配未下降20%' in s for s in report['blocking']))
        self.assertTrue(any('wall未下降5%' in s for s in report['blocking']))
        self.assertTrue(any('native文件增长' in s for s in report['review']))
        self.assertIsNone(report['cases']['hello-A']['aot_jit_metrics'])

    def test_cargo_proxy_keeps_argv0_name(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'rustup').write_text('#!/bin/sh\nexit 0\n')
            (root / 'rustup').chmod(0o755)
            (root / 'cargo').symlink_to('rustup')
            with patch.dict(os.environ, {'PATH': directory}):
                self.assertEqual(cargo_proxy(), str(root / 'cargo'))
                self.assertNotEqual(cargo_proxy(), str((root / 'cargo').resolve()))

    def test_stdout_is_exact_bytes(self):
        schema.output(b'hello\n', 'hello\n')
        for bad in (b'hello', b'hello\n\n', b'hello\r\n', b'\xff'):
            with self.assertRaises(ValueError):
                schema.output(bad, 'hello\n')


if __name__ == '__main__':
    unittest.main()
