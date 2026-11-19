"""分配检查器纯反例；合成样本不构成真实分配/产品通过，不启动任何构建。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import allocation_runner as a


def sample(folder, pid=1, amount=24000000):
    domains = []
    for typed in (False, True):
        domains.append({'typed': typed, 'samples': [dict(batch=i, allocated=amount, count=100000,
            hits=20000 + (i + 1) * 100000, misses=1, size=1, maximum=128,
            calls=1, identity=True, thread=1) for i in range(3)]})
    return dict(schema=1, pid=pid, warm=20000, count=100000, runtime_version='1.0.0.0',
                runtime_path=str((folder / 'RyuPy.Runtime.dll').resolve()), runtime_sha256='runtime',
                host_path=str((folder / 'AllocationHost.dll').resolve()), host_sha256='host', domains=domains)


def rows():
    result = []
    for i, label in enumerate(('old', 'new', 'new', 'old')):
        value = sample(Path('/tmp/synthetic-only') / label, i + 1)
        if label == 'new':
            for row in value['domains'][0]['samples']: row['allocated'] = 0
        result.append({'label': label, 'sample': value})
    return result


class Gates(unittest.TestCase):
    def test_hosted_context_and_event_identity(self):
        env = dict(RUNNER_ENVIRONMENT='github-hosted', GITHUB_ACTIONS='true', GITHUB_EVENT_NAME='push',
                   GITHUB_RUN_ATTEMPT='1', GITHUB_REF='refs/heads/ci/wcwidth-legacy-aot-2909056', GITHUB_SHA='a' * 40)
        event = dict(ref=env['GITHUB_REF'], after=env['GITHUB_SHA'],
                     head_commit=dict(id=env['GITHUB_SHA'], message='validation: wcwidth-warm-kernel-001'))
        a.check_context(env, event)
        for key, value in [('GITHUB_RUN_ATTEMPT', '2'), ('GITHUB_REF', 'refs/heads/main'),
                           ('GITHUB_EVENT_NAME', 'pull_request'), ('RUNNER_ENVIRONMENT', 'self-hosted')]:
            with self.assertRaises(RuntimeError): a.check_context(dict(env, **{key: value}), event)
        for key, value in [('id', 'b' * 40), ('message', 'validation: wcwidth-warm-kernel-001\n')]:
            with self.assertRaises(RuntimeError):
                a.check_context(env, dict(event, head_commit=dict(event['head_commit'], **{key: value})))

    def test_valid_fixture_not_measurement(self):
        folder = Path('/tmp/synthetic-only')
        self.assertEqual(a.parse_sample(json.dumps(sample(folder)), folder, 'runtime', 'host')['count'], 100000)
        self.assertEqual(a.compare(rows())['saved_bytes_per_hit'], 240)
        self.assertIsNone(a.compare(rows())['time_ratio'])

    def test_loaded_runtime_and_same_host_required(self):
        folder = Path('/tmp/synthetic-only')
        for key, value in [('runtime_path', '/wrong'), ('runtime_sha256', 'other'),
                           ('host_path', '/other-host'), ('host_sha256', 'rebuilt'),
                           ('runtime_version', '2.0.0.0')]:
            data = sample(folder); data[key] = value
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                a.parse_sample(json.dumps(data), folder, 'runtime', 'host')

    def test_missing_reordered_or_changed_domains(self):
        folder = Path('/tmp/synthetic-only')
        for changed in ([], [sample(folder)['domains'][0]], list(reversed(sample(folder)['domains']))):
            data = sample(folder); data['domains'] = changed
            with self.assertRaises(RuntimeError): a.parse_sample(json.dumps(data), folder, 'runtime', 'host')

    def test_precise_calls_and_allocation_fields(self):
        folder = Path('/tmp/synthetic-only')
        for key, value in [('allocated', -1), ('allocated', 1.5), ('count', 99999), ('hits', 119999),
                           ('misses', 2), ('size', 2), ('maximum', 129), ('calls', 2),
                           ('identity', False), ('thread', 0), ('batch', 1)]:
            data = sample(folder); data['domains'][0]['samples'][0][key] = value
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                a.parse_sample(json.dumps(data), folder, 'runtime', 'host')

    def test_missing_or_extra_samples(self):
        for n in (2, 4):
            folder = Path('/tmp/synthetic-only'); data = sample(folder)
            data['domains'][0]['samples'] = [data['domains'][0]['samples'][0]] * n
            with self.assertRaises(RuntimeError): a.parse_sample(json.dumps(data), folder, 'runtime', 'host')
        for count in (0, 3, 5):
            with self.assertRaises(RuntimeError): a.compare((rows() * 2)[:count])

    def test_abba_order_and_distinct_pid(self):
        data = rows(); data[0], data[1] = data[1], data[0]
        with self.assertRaises(RuntimeError): a.compare(data)
        data = rows(); data[3]['sample']['pid'] = data[0]['sample']['pid']
        with self.assertRaises(RuntimeError): a.compare(data)

    def test_no_discarding_first_sample(self):
        data = rows(); data[0]['sample']['domains'][0]['samples'][0]['allocated'] += 8
        with self.assertRaises(RuntimeError): a.compare(data)

    def test_typed_control_must_match(self):
        data = rows()
        for row in data:
            if row['label'] == 'new':
                for batch in row['sample']['domains'][1]['samples']: batch['allocated'] += 8
        with self.assertRaises(RuntimeError): a.compare(data)

    def test_equal_or_regressed_not_improvement(self):
        for value in (24000000, 24000008):
            data = rows()
            for row in data:
                if row['label'] == 'new':
                    for batch in row['sample']['domains'][0]['samples']: batch['allocated'] = value
            with self.assertRaises(RuntimeError): a.compare(data)

    def test_trx_requires_all_ten_and_old_tests(self):
        names = [('PyLruStringPairTests.' + n, 'Passed') for n in sorted(a.NEW_FACTS)]
        names += [('PyLruCacheTests.existing', 'Passed'), ('PyFunctoolsTests.existing', 'Passed')]
        def write(path, values):
            path.write_text('<TestRun><Results>' + ''.join(
                '<UnitTestResult testName="RyuPy.Runtime.Tests.%s" outcome="%s" />' % pair
                for pair in values) + '</Results></TestRun>')
        with tempfile.TemporaryDirectory() as d:
            path = Path(d) / 'unit.trx'; write(path, names)
            self.assertEqual(a.verify_trx(path)['total'], 12)
            for bad in (names[:-1], names[1:], names + names[:1],
                        [(names[0][0], 'NotExecuted')] + names[1:], [(names[0][0], 'Failed')] + names[1:]):
                write(path, bad)
                with self.assertRaises(RuntimeError): a.verify_trx(path)

    def test_pending_compile_failure_stays_red_with_no_jit(self):
        with tempfile.TemporaryDirectory() as d:
            root = Path(d); fixture = root / 'tests/e2e/stdlib/functools_lru_string_pair.py'
            fixture.parent.mkdir(parents=True)
            fixture.write_text('# ryupy: pending\nprint("lru-string-pair-ok")\n')
            cli, runtime, dotnet = [root / name for name in ('cli', 'runtime', 'dotnet')]
            for file in (cli, runtime, dotnet): file.write_bytes(b'not executable')
            with patch.object(a, 'ROOT', root), patch.object(a, 'PENDING_SHA', a.w.sha(fixture)), \
                    patch.object(a.w, 'capture', side_effect=RuntimeError('compile unsupported')) as call:
                with self.assertRaises(RuntimeError): a.run_pending(root / 'out', cli, runtime, dotnet, {})
            self.assertEqual(call.call_count, 1)
            result = json.loads((root / 'out/RESULT.json').read_text())
            self.assertFalse(result['passed']); self.assertEqual(result['jit_attempts'], 0)
            self.assertTrue(fixture.read_text().startswith('# ryupy: pending'))

    def test_pending_source_is_frozen_and_not_executed_as_cp(self):
        self.assertEqual(a.w.sha(a.ROOT / 'tests/e2e/stdlib/functools_lru_string_pair.py'), a.PENDING_SHA)
        source = (a.HERE / 'allocation_runner.py').read_text()
        self.assertIn("'cp_rerun': False", source)
        self.assertNotIn('GC.Collect', (a.HERE / 'allocation_host/Program.cs').read_text())

    def test_warm_contract_and_measurement_parameters_unchanged(self):
        source = (a.HERE / 'runner.py').read_text()
        c3 = source.index("outcome['accepted_contracts'] += 1")
        pending = source.index('run_pending(out')
        deadline = source.index('deadline = time.monotonic() + 600')
        measured = source.index('for i in range(6):')
        self.assertLess(c3, pending)
        self.assertLess(pending, deadline)
        self.assertLess(deadline, measured)
        self.assertIn('range(6)', source); self.assertIn('time.monotonic() + 600', source)
        self.assertEqual(len((a.HERE / 'EXPECTED.txt').read_bytes()), 438)


if __name__ == '__main__':
    unittest.main()
