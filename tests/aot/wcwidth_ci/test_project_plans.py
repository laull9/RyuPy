"""计划核验纯脚本测试：不运行编译器，不把模拟计划当项目验收。"""
import json
from pathlib import Path
import tempfile
import unittest
from project_gate import audit_plans, require_progress_promotion


class PlanTests(unittest.TestCase):
    def test_same_matrix_promotion_requires_progress_not_old_counts(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            rows = [{'id': str(i), 'status': 'native'} for i in range(19)]
            rows.append({'id': 'progress', 'status': 'native'})
            summary = {'valid': True, 'denominator': 20, 'cp_passed': 20,
                       'preserve_pass': True, 'promotion_pass': True,
                       'protected_lost': [], 'historical_regressions_open': [], 'run_id': 'one'}
            (root / 'summary.json').write_text(json.dumps(summary))
            (root / 'matrix.json').write_text(json.dumps(rows))
            self.assertTrue(require_progress_promotion(root)['same_matrix'])
            rows[-1]['status'] = 'failed'
            (root / 'matrix.json').write_text(json.dumps(rows))
            with self.assertRaises(RuntimeError):
                require_progress_promotion(root)
            rows[-1]['status'] = 'native'
            (root / 'matrix.json').write_text(json.dumps(rows))
            summary['promotion_pass'] = False
            (root / 'summary.json').write_text(json.dumps(summary))
            with self.assertRaises(RuntimeError):
                require_progress_promotion(root)

    def test_matching_plan_and_reject_feature_off_or_lost_selection(self):
        selected = [{'module': 'example', 'execution': 'InstanceGlobals', 'compiled': True,
                     'reason': 'ordinary-dynamic-pilot:1-functions',
                     'relative_from_import': False, 'ordinary_name_handlers': False}]
        expected = {'session_policy': 'ordinary-dynamic-module-pilot', 'selected': {'sample': selected}}
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); path = root / 'sample/native/sample/module-environment.json'
            path.parent.mkdir(parents=True)
            plan = {'compiled_feature': True, 'session_policy': expected['session_policy'], 'modules': selected}
            path.write_text(json.dumps(plan))
            self.assertEqual(audit_plans(root, expected)['sample']['selected'], selected)
            plan['compiled_feature'] = False; path.write_text(json.dumps(plan))
            with self.assertRaises(RuntimeError): audit_plans(root, expected)
            plan['compiled_feature'] = True; plan['modules'] = []; path.write_text(json.dumps(plan))
            with self.assertRaises(RuntimeError): audit_plans(root, expected)


if __name__ == '__main__':
    unittest.main()
