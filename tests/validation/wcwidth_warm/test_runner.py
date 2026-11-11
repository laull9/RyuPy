"""纯模拟门反例；不运行任何CP业务、编译器、CLR或Native。"""
import copy
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import runner as r


def raw():
    lines = []
    for i in range(16):
        lines.append('batch %d %s %s %s' % (i, 0.0 if i < 4 else 1.0,
            (300000, 400000, 200000, 100000),
            (((i + 1) * 800000, 8, 1000, 8), (3, 1, 8, 1), (0, 0, 128, 0))))
    return ('\n'.join(lines) + '\ncomplete 16\n').encode()


class Gates(unittest.TestCase):
    def test_secret_environment_not_passed_or_saved(self):
        env = r.allowed_env({'PATH': '/bin', 'GITHUB_TOKEN': 'FAKE_SECRET', 'ACTIONS_RUNTIME_TOKEN': 'FAKE_SECRET'})
        self.assertEqual(env, {'PATH': '/bin'})
        with tempfile.TemporaryDirectory() as d:
            out = Path(d)
            seen = []
            def launch(argv, **kw):
                seen.append(kw['env'])
                return self.fake()(argv, **kw)
            r.capture(out, 'safe', ['fake'], env, 1, out, launch)
            self.assertEqual(seen, [{'PATH': '/bin'}])
            self.assertNotIn('FAKE_SECRET', (out / 'safe.json').read_text())
            r.upload_check(out)
            r.save(out / 'unsafe.json', {'environment': {'TOKEN': 'FAKE_SECRET'}})
            with self.assertRaises(RuntimeError):
                r.upload_check(out)

    def test_complete_dist_equivalence(self):
        with tempfile.TemporaryDirectory() as d:
            a, b = Path(d) / 'a', Path(d) / 'b'
            a.mkdir(); (a / 'data').write_bytes(b'content')
            r.shutil.copytree(a, b)
            self.assertEqual(r.dist_identity(a), r.dist_identity(b))
            (b / 'data').chmod(0o700)
            self.assertNotEqual(r.dist_identity(a), r.dist_identity(b))
            (b / 'extra').write_bytes(b'extra')
            self.assertNotEqual(r.dist_identity(a), r.dist_identity(b))

    def test_repository_identity_clean_required(self):
        with tempfile.TemporaryDirectory() as d:
            out = Path(d)
            def fake(out, label, argv, env, timeout, cwd):
                (out / (label + '.stdout')).write_text('head\ntree\n' if 'identity' in label else '')
            with patch.object(r, 'capture', fake):
                self.assertEqual(r.repository_identity(out, 'test', {}), ['head', 'tree'])
            def dirty(out, label, argv, env, timeout, cwd):
                (out / (label + '.stdout')).write_text('head\ntree\n' if 'identity' in label else ' M source')
            with patch.object(r, 'capture', dirty), self.assertRaises(RuntimeError):
                r.repository_identity(out, 'dirty', {})

    def test_tools_start_change_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            tool = Path(d) / 'tool'; tool.write_bytes(b'original')
            first = r.identities([tool]); tool.write_bytes(b'changed')
            with self.assertRaises(RuntimeError):
                r.require(r.identities([tool]) == first, 'C前工具漂移')

    def test_complete(self):
        self.assertEqual(r.parse_warm(raw()), [1.0] * 12)
        self.assertTrue(r.reliability([[1.] * 12 for _ in range(6)])['reliable'])

    def test_short(self):
        with self.assertRaises(RuntimeError):
            r.parse_warm(raw().replace(b'batch 4 1.0', b'batch 4 0.09'))

    def test_nonfinite(self):
        for value in (b'nan', b'inf', b'-inf'):
            with self.subTest(value=value), self.assertRaises(RuntimeError):
                r.parse_warm(raw().replace(b'batch 4 1.0', b'batch 4 ' + value))

    def test_bad_cache_and_checksum(self):
        for value in (raw().replace(b'800000', b'800001', 1), raw().replace(b'300000', b'300001', 1)):
            with self.assertRaises(RuntimeError):
                r.parse_warm(value)

    def test_missing_samples(self):
        for rows in ([[1.] * 12] * 5, [[1.] * 11] * 6):
            with self.assertRaises(RuntimeError):
                r.reliability(rows)
        with self.assertRaises(RuntimeError):
            r.parse_warm(raw().split(b'complete')[0])

    def test_drift(self):
        rows = [[1.] * 6 + [1.2] * 6 for _ in range(6)]
        with self.assertRaises(RuntimeError):
            r.reliability(rows)

    def test_wrong_pe(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / 'pe'; p.write_bytes(b'pe')
            manifest = {'build_inputs': [{'role': 'compiled_managed_input', 'sha256': 'bad'},
                         {'role': 'runtime_managed_input', 'sha256': r.sha(p)}]}
            with self.assertRaises(RuntimeError):
                r.binding(manifest, p, p)

    def test_identity_drift(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / 'pe'; p.write_bytes(b'a'); before = r.identities([p])
            p.write_bytes(b'b')
            with self.assertRaises(RuntimeError):
                r.require(r.identities([p]) == before, 'drift')

    def fake(self, code=0, timeout=False):
        class Child:
            pid = 99999999
            calls = 0
            def wait(self, timeout=None):
                self.calls += 1
                if self.calls == 1 and timeout_error:
                    raise subprocess.TimeoutExpired('mock', 1)
                return code
        timeout_error = timeout
        def launch(argv, **kw):
            kw['stdout'].write(b'raw-before-verdict')
            kw['stderr'].write(b'error-evidence' if code else b'')
            return Child()
        return launch

    def test_negative_exit_raw_first(self):
        with tempfile.TemporaryDirectory() as d:
            out = Path(d)
            with self.assertRaises(RuntimeError):
                r.capture(out, 'negative', ['fake'], {}, 1, out, self.fake(-11))
            self.assertEqual((out / 'negative.stdout').read_bytes(), b'raw-before-verdict')
            self.assertEqual(json.loads((out / 'negative.json').read_text())['exit'], -11)
            (out / 'negative.json').rename(out / 'jit.json')
            self.assertEqual(r.execution_counts(out)['contract'],
                             {'attempted': 1, 'started': 1, 'completed': 1, 'timed_out': 0})

    def test_timeout_raw_first(self):
        with tempfile.TemporaryDirectory() as d, patch.object(r.os, 'killpg') as kill:
            out = Path(d)
            with self.assertRaises(RuntimeError):
                r.capture(out, 'timeout', ['fake'], {}, 1, out, self.fake(-9, True))
            self.assertTrue(json.loads((out / 'timeout.json').read_text())['timeout'])
            kill.assert_called_once()
            self.assertTrue((out / 'timeout.stdout').exists())

    def test_launch_error(self):
        with tempfile.TemporaryDirectory() as d:
            out = Path(d)
            def broken(*args, **kw):
                raise OSError('mock')
            with self.assertRaises(RuntimeError):
                r.capture(out, 'launch', ['fake'], {}, 1, out, broken)
            self.assertFalse(json.loads((out / 'launch.json').read_text())['started'])

    def test_deadline_no_launch(self):
        with tempfile.TemporaryDirectory() as d:
            with self.assertRaises(RuntimeError):
                r.capture(Path(d), 'none', ['fake'], {}, 0, Path(d), self.fake())
            self.assertEqual(list(Path(d).iterdir()), [])

    def test_whole_failure_kept(self):
        with tempfile.TemporaryDirectory() as d:
            out = Path(d) / 'evidence'
            with patch.dict(r.os.environ, {}, clear=True), self.assertRaises(KeyError):
                r.run(out)
            self.assertFalse(json.loads((out / 'RESULT.json').read_text())['passed'])


if __name__ == '__main__':
    unittest.main()
