"""计划核验纯脚本测试：不运行编译器，不把模拟计划当项目验收。"""
import json
import copy
import sys
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from project_gate import audit_plans, require_progress_promotion
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "scripts"))
import default_source_deployment_validation_plans as strict


class PlanTests(unittest.TestCase):
    def test_accepted_full_twenty_and_independent_selected(self):
        expected = json.loads((Path(__file__).parent / 'project-pilot-plans.json').read_text())
        originals = strict.baseline(expected)
        self.assertEqual(len(originals), 20)
        for pid, original in originals.items():
            actual = copy.deepcopy(original)
            for row in actual['modules']:
                row.update(candidate=False, hard=True, catalog_authenticated=False)
            self.assertEqual(strict.audit_old_rows(actual, original, expected['selected'][pid]), {})
            # 旧字段、三bool、资源锚与旧选择各自拒绝，不能仅靠总数偶然失败。
            bad = copy.deepcopy(actual); bad['modules'].pop()
            with self.assertRaises(RuntimeError): strict.audit_old_rows(bad, original, expected['selected'][pid])
            for key, value in [('compiled', not actual['modules'][0]['compiled']),
                               ('reason', 'changed'), ('candidate', True), ('hard', False),
                               ('catalog_authenticated', True), ('ordinary_name_handlers', True)]:
                bad = copy.deepcopy(actual); bad['modules'][0][key] = value
                with self.assertRaises(RuntimeError): strict.audit_old_rows(bad, original, expected['selected'][pid])
            for flag in strict.FLAGS:
                for value in (None, 0, 1, 'false'):
                    bad = copy.deepcopy(actual); bad['modules'][0][flag] = value
                    with self.assertRaises(RuntimeError): strict.audit_old_rows(bad, original, expected['selected'][pid])
                bad = copy.deepcopy(actual); del bad['modules'][0][flag]
                with self.assertRaises(RuntimeError): strict.audit_old_rows(bad, original, expected['selected'][pid])
            bad = copy.deepcopy(actual); bad['modules'].append(copy.deepcopy(bad['modules'][0]))
            with self.assertRaises(RuntimeError): strict.audit_old_rows(bad, original, expected['selected'][pid])
            bad = copy.deepcopy(actual); bad['modules'][0]['unknown'] = False
            with self.assertRaises(RuntimeError): strict.audit_old_rows(bad, original, expected['selected'][pid])
            if expected['selected'][pid]:
                wrong = copy.deepcopy(expected['selected'][pid]); wrong[0]['reason'] = 'changed'
                with self.assertRaises(RuntimeError): strict.audit_old_rows(actual, original, wrong)
        fonts = originals['pyfiglet']['modules'][-1]
        self.assertTrue(any(row['module'] == 'pyfiglet.fonts' and 'resource_discovery' in row
                            for row in originals['pyfiglet']['modules']))
        actual = copy.deepcopy(originals['pyfiglet'])
        for row in actual['modules']:
            row.update(candidate=False, hard=True, catalog_authenticated=False)
            if 'resource_discovery' in row: row['resource_discovery']['anchor'] = 'changed'
        with self.assertRaises(RuntimeError):
            strict.audit_old_rows(actual, originals['pyfiglet'], expected['selected']['pyfiglet'])

    def test_full_contract_missing_source_duplicate_or_bad_digest_rejected(self):
        expected = json.loads((Path(__file__).parent / 'project-pilot-plans.json').read_text())
        contract = json.loads(strict.CONTRACT.read_text())
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'contract.json'
            for mutation in ('source', 'duplicate', 'digest', 'raw'):
                bad = copy.deepcopy(contract)
                if mutation == 'source': del bad['source_run']
                if mutation == 'duplicate': bad['plans'][-1] = bad['plans'][0]
                if mutation == 'digest': bad['plans'][0]['sha256'] = '0' * 64
                if mutation == 'raw': bad['plans'][0]['raw'] += ' '
                path.write_text(json.dumps(bad))
                with self.assertRaises(RuntimeError): strict.baseline(expected, path)

    def test_finite_candidates_and_deployment_cross_checks(self):
        import tarfile
        import hashlib
        sources = strict.source_contract()
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); inputs = root / 'inputs'; output = root / 'output'
            output.mkdir()
            family = b''
            for module in ('_markupbase', 'html', 'html.entities', 'html.parser'):
                src = sources[module]; family += src['relative'].encode() + b'\0' + src['sha256'].encode()
            family += b'3b2f81fe21d181c499c59a256c8e1968455d6689d269aa85373bfb6af41da3bf'
            catalog = output / 'compiler-sources' / hashlib.sha256(family).hexdigest()
            locations = {}
            with tarfile.open(strict.ROOT / 'tests/validation/markdown_closure_inputs.tar.gz') as archive:
                for module, src in sources.items():
                    path = (catalog if src['catalog'] else inputs / 'markdown') / src['relative']
                    path.parent.mkdir(parents=True, exist_ok=True)
                    prefix = 'inputs/stdlib/' if src['catalog'] else 'inputs/markdown/'
                    path.write_bytes(archive.extractfile(prefix + src['relative']).read())
                    locations[module] = dict(path=str(path), package=src['package'], file=src['file'],
                                             is_package=src['is_package'])
            rows = strict.candidate_rows('markdown')
            report = dict(schema_version=1, roots=sorted(set(rows) - {'html', '_markupbase'}),
                dependencies={n: sources[n]['dependencies'] for n in rows}, locations=locations,
                deferred={}, diagnostics={}, outside=[], metadata_unavailable=None,
                protected=['json', 'zipfile', 'optparse', 'getopt', 're', 'dataclasses', 'unittest',
                           'enum', 'io', 'typing', 'xml.etree.ElementTree'])
            target = output / 'optional-sources.json'; target.write_text(json.dumps(report))
            self.assertEqual(len(strict.audit_deployment(output, rows, 'markdown', inputs / 'markdown')['sources']), 22)
            expected = json.loads((Path(__file__).parent / 'project-pilot-plans.json').read_text())
            product = copy.deepcopy(strict.baseline(expected)['markdown'])
            for row in product['modules']: row.update(candidate=False, hard=True, catalog_authenticated=False)
            product['modules'] += list(rows.values())
            self.assertEqual(len(strict.audit_product_plan(output, product, inputs, 'markdown')['sources']), 22)
            changed = copy.deepcopy(product)
            next(row for row in changed['modules'] if row['module'] == 'html.parser')['hard'] = True
            with self.assertRaises(RuntimeError): strict.audit_product_plan(output, changed, inputs, 'markdown')

            for module in rows:
                for key in ('compiled', 'candidate', 'hard', 'catalog_authenticated', 'relative_from_import',
                            'ordinary_name_handlers'):
                    bad = copy.deepcopy(rows); bad[module][key] = not bad[module][key]
                    with self.assertRaises(RuntimeError): strict.audit_candidates(bad, 'markdown')
            bad = copy.deepcopy(rows); bad['unexpected'] = bad['html']
            with self.assertRaises(RuntimeError): strict.audit_candidates(bad, 'markdown')
            for key, value in [('roots', []), ('dependencies', {}), ('locations', {}),
                               ('deferred', {'html.parser': 'unsupported'}), ('protected', []), ('outside', ['extra'])]:
                bad = copy.deepcopy(report); bad[key] = value; target.write_text(json.dumps(bad))
                with self.assertRaises(RuntimeError): strict.audit_deployment(output, rows, 'markdown', inputs / 'markdown')
            target.write_text(json.dumps(report))
            path = Path(locations['html.parser']['path']); path.write_bytes(path.read_bytes() + b'#changed')
            with self.assertRaises(RuntimeError): strict.audit_deployment(output, rows, 'markdown', inputs / 'markdown')
            target.unlink()
            with self.assertRaises(FileNotFoundError): strict.audit_deployment(output, rows, 'markdown', inputs / 'markdown')

    def test_normal_unsupported_is_planned_only_not_arbitrary_failure(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); output = root / 'pyparsing/native/pyparsing'; output.mkdir(parents=True)
            (root / 'pyparsing/runner.json').write_text(json.dumps(dict(exit=0, failure=None, raw_streams_complete=True)))
            (output / 'compile.error.txt').write_text('编译失败，存在错误')
            (output / 'compile.diagnostics.txt').write_text('[E0100] Error: 源码边界\n  --> core.py:1:1\n')
            row = dict(id='pyparsing', status='unsupported', failure_kind='diagnostic', run_id='same',
                       mode='default-jit', run_exit=None, detail='[E0100] Error: 源码边界', evidence={})
            def persist(value):
                (root / 'pyparsing/native/projects.jsonl').write_text(json.dumps(value) + '\n')
                value['evidence'] = {str(path.relative_to(root)): strict.digest(path.read_bytes())
                    for path in root.rglob('*') if path.is_file()}
            persist(row)
            self.assertEqual(strict.evidence_layer(root, 'pyparsing', [row]), 'planned-only')
            for key, value in [('status', 'failed'), ('failure_kind', 'compiler-error'),
                               ('failure_kind', 'crash'), ('run_exit', -11), ('run_exit', 134)]:
                bad = dict(row); bad[key] = value; persist(bad)
                with self.assertRaises(RuntimeError): strict.evidence_layer(root, 'pyparsing', [bad])
            persist(row)
            for value in (dict(exit=-11, failure=None, raw_streams_complete=True),
                          dict(exit=0, failure='timeout', raw_streams_complete=True),
                          dict(exit=0, failure=None, raw_streams_complete=False)):
                (root / 'pyparsing/runner.json').write_text(json.dumps(value))
                with self.assertRaises(RuntimeError): strict.evidence_layer(root, 'pyparsing', [row])
            (root / 'pyparsing/runner.json').write_text(json.dumps(dict(exit=0, failure=None, raw_streams_complete=True)))
            future = dict(row, status='native', failure_kind='', run_exit=0); persist(future)
            self.assertEqual(strict.evidence_layer(root, 'pyparsing', [future]), 'deployment')
            with self.assertRaises(FileNotFoundError):
                strict.audit_deployment(output, strict.candidate_rows('pyparsing'), 'pyparsing')

    def test_real_gate_entry_rejects_regression_future_missing_report_and_extra_candidate(self):
        expected = json.loads((Path(__file__).parent / 'project-pilot-plans.json').read_text())
        originals = strict.baseline(expected)
        known = {'six', 'attrs', 'click', 'toml', 'networkx', 'pyparsing', 'python-dateutil', 'sortedcontainers'}
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); matrix = []
            for pid, original in originals.items():
                output = root / pid / 'native' / pid; output.mkdir(parents=True)
                actual = copy.deepcopy(original)
                for row in actual['modules']: row.update(candidate=False, hard=True, catalog_authenticated=False)
                if pid in {'markdown', 'pyparsing'}:
                    actual['modules'] += list(strict.candidate_rows(pid, pid == 'markdown').values())
                (output / 'module-environment.json').write_text(json.dumps(actual))
                (root / pid / 'runner.json').write_text(json.dumps(dict(exit=0, failure=None, raw_streams_complete=True)))
                row = dict(id=pid, status='unsupported' if pid in known else 'native',
                           failure_kind='diagnostic' if pid in known else '', run_id='same', mode='default-jit',
                           run_exit=None if pid in known else 0,
                           detail='[E0100] Error: 源码边界' if pid in known else 'ok')
                if pid in known:
                    (output / 'compile.error.txt').write_text('编译失败，存在错误')
                    (output / 'compile.diagnostics.txt').write_text(row['detail'] + '\n  --> source.py:1:1\n')
                matrix.append(row)
            def persist():
                for row in matrix:
                    pid = row['id']; clean = {k: v for k, v in row.items() if k != 'evidence'}
                    (root / pid / 'native/projects.jsonl').write_text(json.dumps(clean) + '\n')
                    row['evidence'] = {str(path.relative_to(root)): strict.digest(path.read_bytes())
                        for path in (root / pid).rglob('*') if path.is_file()}
                (root / 'matrix.json').write_text(json.dumps(matrix))
                (root / 'summary.json').write_text(json.dumps(dict(valid=True, denominator=20,
                    cp_passed=20, preserve_pass=True, protected_lost=[], run_id='same')))
            # Markdown完整部署另有锁字节反例；这里仅替代该分支以隔离真实入口的其它接线。
            real_deployment = strict.audit_deployment
            def isolated(output, rows, name, markdown_root=None):
                return {'level': 'synthetic-markdown-only'} if name == 'markdown' else real_deployment(output, rows, name, markdown_root)
            persist()
            with patch.object(strict, 'audit_deployment', side_effect=isolated):
                self.assertEqual(len(audit_plans(root, expected)), 20)
                unexpected_report = root / 'tabulate/native/tabulate/optional-sources.json'
                unexpected_report.write_text('{}'); persist()
                with self.assertRaises(RuntimeError): audit_plans(root, expected)
                unexpected_report.unlink(); persist()
                row = next(r for r in matrix if r['id'] == 'tabulate')
                original_row = copy.deepcopy(row); row.update(status='unsupported', failure_kind='diagnostic', run_exit=None)
                persist()
                with self.assertRaises(RuntimeError): audit_plans(root, expected)
                row.clear(); row.update(original_row)
                row = next(r for r in matrix if r['id'] == 'pyparsing')
                original_row = copy.deepcopy(row); row.update(status='native', failure_kind='', run_exit=0)
                # 保旧compiled=false行；只将新增两行变为成功计划，随后必须要求跨报告。
                path = root / 'pyparsing/native/pyparsing/module-environment.json'
                plan = json.loads(path.read_text()); previous_plan = copy.deepcopy(plan)
                for item in plan['modules']:
                    if item['candidate']: item['compiled'] = True
                path.write_text(json.dumps(plan)); persist()
                with self.assertRaises(FileNotFoundError): audit_plans(root, expected)
                row.clear(); row.update(original_row); path.write_text(json.dumps(previous_plan))
                path = root / 'tabulate/native/tabulate/module-environment.json'
                plan = json.loads(path.read_text()); extra = strict.candidate_rows('parser')['html']; plan['modules'].append(extra)
                path.write_text(json.dumps(plan)); persist()
                with self.assertRaises(RuntimeError): audit_plans(root, expected)

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
            strict.validate_plan(plan, new=False)
            actual = copy.deepcopy(plan)
            for row in actual['modules']: row.update(candidate=False, hard=True, catalog_authenticated=False)
            self.assertEqual(strict.audit_old_rows(actual, plan, selected), {})
            plan['compiled_feature'] = False; path.write_text(json.dumps(plan))
            with self.assertRaises(RuntimeError): strict.audit_old_rows(plan, plan, selected)
            plan['compiled_feature'] = True; plan['modules'] = []; path.write_text(json.dumps(plan))
            with self.assertRaises(RuntimeError): strict.audit_old_rows(plan, plan, selected)


if __name__ == '__main__':
    unittest.main()
