"""第二应用配置与原件锁纯测试，不把脚本测试冒充Native执行。"""
import json
import unittest
from prepare import HERE, configuration, sha

class ContractTests(unittest.TestCase):
    def test_frozen_inputs(self):
        contract = json.loads((HERE/'contract.json').read_text())
        for name, digest in contract['files'].items(): self.assertEqual(sha(HERE/name), digest)
        self.assertEqual(contract['entry_point']['value'], 'tabulate:_main')
    def test_distinct_metadata_only_layout(self):
        config = configuration()
        self.assertNotIn('resources', config)
        self.assertEqual(config['search_paths'], ['vendor/table', 'vendor/width'])
        self.assertEqual(config['targets']['table']['entry'], 'tools/tabulate_console.py')
        self.assertEqual(config['include_modules'], ['tabulate', 'wcwidth'])
    def test_dependency_sources_are_fully_locked(self):
        rows = json.loads((HERE/'inputs.json').read_text())['packages']
        self.assertEqual(set(rows), {'tabulate','wcwidth'})
        for name, row in rows.items():
            self.assertIn(name+'/__init__.py', row['files'])
            self.assertIn(name+'.egg-info/PKG-INFO', row['files'])
            self.assertTrue(all('..' not in p.split('/') for p in row['files']))

if __name__ == '__main__': unittest.main()
