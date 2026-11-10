"""诊断分流与采集器的纯mock自测；不构建Rust、不执行原A或任何产品。"""
import contextlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import Mock, patch
import diagnostic_once as d

HERE = Path(__file__).resolve().parent
HEAD = '1' * 40


def stderr_fixture(nodes=1):
    events = []
    for index, name in enumerate(('simplify_cfg', 'dead_values', 'numeric_accumulator',
                                  'tuple_scalar', 'dead_values', 'interval_narrow', 'float_chain'), 1):
        for event, stage in [('begin', 'pass-run'), ('end', 'pass-run'),
                             ('begin', 'pass-validate'), ('end', 'pass-validate')]:
            events.append(f'[ryupy-stage] event={event} stage={stage} module="__main__" pass_index={index} pass="{name}" elapsed_us=1\n')
    text = ''.join(events) + '[E0310] Error: trace V1 整产物拒绝：' + d.REASON + '\n'
    text += '[edge-witness-v1 fn=0 block=bb2]\n'
    for label in ('captured', 'final', 'old-target-in-final'):
        text += label + '.terminator=Goto(bb3)\n'
        text += label + '.chain[0] target=bb3 stop=terminal chain_truncated=false\n'
        text += ''.join(f'  bb{i} statements=0 empty=true Unreachable\n' for i in range(nodes))
    return (text + '[edge-witness-v1 total_truncated=false]\n').encode()


class DiagnosticTests(unittest.TestCase):
    def test_three_witness_labels_are_distinct_and_seven_passes_complete(self):
        raw = stderr_fixture()
        self.assertLessEqual(d.witness(raw)['utf8_bytes'], 8192)
        d.seven_passes(raw)
        with self.assertRaises(ValueError):
            d.witness(stderr_fixture(nodes=9))
        for bad in [raw + b'[E0310]', raw.replace(b'captured.terminator=', b'missing.terminator='),
                    raw.replace(b'[edge-witness-v1 total_truncated=false]', b'no-end'),
                    raw.replace(b'[edge-witness-v1 fn=', ('中' * 3000 + '[edge-witness-v1 fn=').encode())]:
            with self.subTest(bad=bad[-70:]), self.assertRaises(ValueError):
                d.witness(bad)
        with self.assertRaises(ValueError):
            d.seven_passes(raw.replace(b'pass="float_chain"', b'pass="changed"'))

    def test_selector_exact_title_case_variants_dispatch_and_missing_head(self):
        # 执行workflow内唯一、无源码依赖的selector脚本，避免另写一个等价但未受测的判断器。
        workflow = (HERE.parents[2] / '.github/workflows/wcwidth-legacy-aot.yml').read_text()
        block = workflow.split("          python3 - <<'PY'\n", 1)[1].split('\n          PY', 1)[0]
        code = '\n'.join(line[10:] for line in block.splitlines())
        markdown_title = 'diagnostic-not-acceptance: markdown-preflight-001'
        noreturn_title = 'validation: typing-noreturn-001'
        expand_title = 'validation: text-expandtabs-001'
        cases = [(d.TITLE, 'push', d.REF, True), (d.TITLE.upper(), 'push', d.REF, False),
                 (d.TITLE + '\n', 'push', d.REF, False), ('normal commit', 'push', d.REF, False),
                 (d.TITLE, 'workflow_dispatch', d.REF, False), (None, 'push', d.REF, False),
                 (d.TITLE, 'push', d.REF.upper(), False),
                 (markdown_title, 'push', d.REF, False),
                 (markdown_title.upper(), 'push', d.REF, False),
                 (markdown_title + '\n', 'push', d.REF, False),
                 (markdown_title, 'workflow_dispatch', d.REF, False),
                 (markdown_title, 'push', d.REF.upper(), False),
                 (noreturn_title, 'push', d.REF, False),
                 (noreturn_title.upper(), 'push', d.REF, False),
                 (noreturn_title + '\n', 'push', d.REF, False),
                 (noreturn_title, 'workflow_dispatch', d.REF, False),
                 (noreturn_title, 'push', d.REF.upper(), False)]
        cases += [(title, event, ref, False) for title, event, ref in [
            (expand_title, 'push', d.REF), (expand_title.upper(), 'push', d.REF),
            (expand_title + '\n', 'push', d.REF), (expand_title, 'workflow_dispatch', d.REF),
            (expand_title, 'push', d.REF.upper())]]
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for title, event_name, ref, expected in cases:
                with self.subTest(title=title, event_name=event_name, ref=ref):
                    event = {'ref': ref}
                    if title is not None:
                        event['head_commit'] = {'message': title}
                    (root / 'event').write_text(json.dumps(event))
                    (root / 'output').write_text('')
                    with patch.dict(os.environ, {'GITHUB_EVENT_PATH': str(root / 'event'),
                         'GITHUB_OUTPUT': str(root / 'output'), 'GITHUB_EVENT_NAME': event_name,
                         'GITHUB_REF': ref}, clear=True):
                        exec(compile(code, 'workflow-selector', 'exec'), {})
                    noreturn = title in (noreturn_title, expand_title) and event_name == 'push' and ref == d.REF
                    markdown = title == markdown_title and event_name == 'push' and ref == d.REF
                    self.assertEqual((root / 'output').read_text(),
                                     'diagnostic=' + str(expected).lower() + '\n'
                                     + 'markdown=' + str(markdown).lower() + '\n'
                                     + 'noreturn=' + str(noreturn).lower() + '\n')
        self.assertNotIn('${{', block)
        self.assertNotIn('checkout', block)

    def test_runner_context_is_stricter_than_expression_strings(self):
        env = {'GITHUB_ACTIONS': 'true', 'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': d.REF,
               'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_SHA': HEAD}
        event = {'ref': d.REF, 'after': HEAD, 'head_commit': {'id': HEAD, 'message': d.TITLE}}
        d.check_context(env, event)
        for key, value in [('GITHUB_EVENT_NAME', 'workflow_dispatch'), ('GITHUB_REF', d.REF.upper()),
                           ('GITHUB_RUN_ATTEMPT', '2'), ('RUSTFLAGS', '-A warnings')]:
            with self.subTest(key=key), self.assertRaises(ValueError):
                d.check_context(dict(env, **{key: value}), event)
        for message in [d.TITLE.upper(), d.TITLE + '\n', 'ordinary']:
            with self.assertRaises(ValueError):
                d.check_context(env, dict(event, head_commit={'id': HEAD, 'message': message}))

    @contextlib.contextmanager
    def fixture(self, compile_exit=1, compile_failure=None, build_exit=0, bad_identity=False,
                bad_test=False, raw=None, title=None, unexpected_product=False):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            source = repo / 'tests/validation/generator_product/goldens/a_all_miss.py'
            source.parent.mkdir(parents=True)
            source.write_bytes((HERE / 'goldens/a_all_miss.py').read_bytes())
            cli = repo / 'target/debug/ryupy'
            cli.parent.mkdir(parents=True)
            cli.write_bytes(b'fake-cli')
            for tool in ('rustc', 'cargo'):
                (repo / tool).write_bytes(('fake-' + tool).encode())
            event = {'ref': d.REF, 'after': HEAD, 'head_commit': {'id': HEAD, 'message': title or d.TITLE}}
            event_path = repo / 'event.json'
            event_path.write_text(json.dumps(event))
            calls = []
            def fake_capture(prefix, argv, cwd, timeout):
                calls.append((prefix.name, list(map(str, argv)), timeout))
                stdout, stderr, code, failure = b'', b'', 0, None
                if prefix.name.endswith('-version'):
                    stdout = (('rustc 1.99.1 (58cf8f9c3 2026-10-13)' if prefix.name.startswith('rustc')
                              else 'cargo 1.99.1 (5f94df478 2026-08-27)') + '\n').encode()
                if prefix.name == 'compiler-build':
                    code = build_exit
                if prefix.name.startswith('diagnostic-test-'):
                    name = argv[6]
                    stdout = (f'test {name} ... ok\ntest result: ok. {0 if bad_test else 1} passed; 0 failed; 0 ignored; 0 measured; 255 filtered out; finished in 0.00s\n').encode()
                if prefix.name == 'compile':
                    code, failure, stderr = compile_exit, compile_failure, raw if raw is not None else stderr_fixture()
                    if unexpected_product:
                        output = Path(argv[-1]); output.mkdir(); (output / 'entry.dll').write_bytes(b'MZ')
                record = {'argv': list(map(str, argv)), 'cwd': str(cwd), 'timeout_seconds': timeout,
                          'exit': code, 'failure': failure, 'raw_streams_complete': True}
                prefix.with_suffix('.stdout').write_bytes(stdout)
                prefix.with_suffix('.stderr').write_bytes(stderr)
                d.save(prefix.with_suffix('.json'), record)
                return record
            before = {'head': HEAD, 'tree': '2' * 40, 'status': '', 'files': {}}
            env = {'GITHUB_ACTIONS': 'true', 'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': d.REF,
                   'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_SHA': HEAD, 'GITHUB_EVENT_PATH': str(event_path)}
            with patch.dict(os.environ, env, clear=True), patch.object(d, 'capture', side_effect=fake_capture), \
                 patch.object(d.shutil, 'which', side_effect=lambda name: str(repo / name)), \
                 patch.object(d.Runner, 'identity', side_effect=[before, dict(before, status='dirty') if bad_identity else before]):
                runner = d.Runner(repo)
                yield runner, calls

    def test_only_one_compile_keeps_real_failure_and_never_runs_product(self):
        with self.fixture() as (runner, calls):
            self.assertEqual(runner.execute(), 1)
            result = json.loads((runner.out / 'result.json').read_text())
            self.assertTrue(result['diagnostic_collected'])
            self.assertFalse(result['accepted'])
            self.assertEqual(result['compile_exit'], 1)
            self.assertEqual(result['compile_attempts'], 1)
            self.assertEqual(result['product_execution_count'], 0)
            compile_calls = [row for row in calls if row[0] == 'compile']
            self.assertEqual(len(compile_calls), 1)
            self.assertEqual(compile_calls[0][2], 60)
            self.assertEqual(compile_calls[0][1][1:3], ['--trace-product-v1', 'build'])
            self.assertEqual(sum(row[0] == 'compiler-build' for row in calls), 1)
            self.assertEqual(sum(row[0].startswith('diagnostic-test-') for row in calls), 3)
            self.assertTrue(all('--exact' in row[1] for row in calls if row[0].startswith('diagnostic-test-')))
            self.assertNotIn('--aot', str(calls))
            self.assertNotIn('dotnet', str(calls))

    def test_invalid_compile_outcomes_and_identity_never_count_as_collected(self):
        for options in [dict(compile_exit=0), dict(compile_failure='timeout', compile_exit=-9),
                        dict(raw=b'other error'), dict(bad_identity=True), dict(unexpected_product=True)]:
            with self.subTest(options=options), self.fixture(**options) as (runner, calls):
                self.assertEqual(runner.execute(), 2)
                result = json.loads((runner.out / 'result.json').read_text())
                self.assertFalse(result['diagnostic_collected'])
                self.assertEqual(result['compile_exit'], options.get('compile_exit', 1))
                self.assertEqual(sum(row[0] == 'compile' for row in calls), 1)
                self.assertEqual((runner.out / 'compile.stderr').read_bytes(), options.get('raw', stderr_fixture()))

    def test_context_build_or_test_failure_prevents_compile(self):
        for options in [dict(title=d.TITLE.upper()), dict(build_exit=101), dict(bad_test=True)]:
            with self.subTest(options=options), self.fixture(**options) as (runner, calls):
                self.assertEqual(runner.execute(), 2)
                self.assertFalse(any(row[0] == 'compile' for row in calls))

    def test_capture_timeout_preserves_bytes_and_does_not_retry(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix = Path(temp) / 'compile'
            process = Mock(pid=123)
            process.wait.side_effect = [subprocess.TimeoutExpired(['cli'], 60), -9]
            def start(*args, **kwargs):
                kwargs['stdout'].write(b'original-out')
                kwargs['stderr'].write(b'original-err')
                return process
            with patch.object(d.subprocess, 'Popen', side_effect=start) as popen, patch.object(d.os, 'killpg') as kill:
                result = d.capture(prefix, ['cli', 'build'], Path(temp), 60)
            popen.assert_called_once(); kill.assert_called_once_with(123, d.signal.SIGKILL)
            self.assertEqual(result['exit'], -9)
            self.assertEqual(result['failure'], 'timeout')
            self.assertEqual(prefix.with_suffix('.stderr').read_bytes(), b'original-err')


if __name__ == '__main__':
    unittest.main()
