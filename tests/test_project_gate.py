"""离线门禁自检：用合成证据覆盖净数掩盖回归、缺项、陈旧、原流/归档篡改与进程监管。

不联网、不启动 Rust/.NET，也不将这些框架测试称为真实项目通过。
"""

import copy
import contextlib
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

from project_gate_contract import (GateError, audit_sources, digest, evaluate,
                                   load_inputs, validate_result)
import run_projects_gate as gate
from run_projects_gate import main, run_process

ROOT = Path(__file__).resolve().parent.parent


class ProjectGateTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.contract, cls.projects = load_inputs(ROOT)

    def matrix(self):
        return [{"id": pid, "status": "native" if pid in self.contract["protected_native"] else "unsupported"}
                for pid in self.contract["project_ids"]]

    def test_policy_keeps_ten_including_recovered_progress(self):
        self.assertEqual(set(self.contract["protected_native"]), {
            "tomli", "docopt", "tabulate", "texttable", "chardet", "wcwidth", "colorama", "termcolor", "binaryornot", "progress"})
        self.assertEqual(self.contract["promotion_requires"], ["progress"])
        rows = self.matrix()
        next(row for row in rows if row["id"] == "progress")["status"] = "failed"
        result = evaluate(self.contract, rows)
        self.assertFalse(result["preserve_pass"])
        self.assertFalse(result["promotion_pass"])
        self.assertEqual(result["historical_regressions_open"], ["progress"])
        self.assertEqual(result["native"], 9)
        self.assertEqual(result["aot"], "not-evaluated")

    def test_same_count_cannot_hide_lost_id(self):
        rows = self.matrix()
        for row in rows:
            if row["id"] == "tomli": row["status"] = "failed"
            if row["id"] == "six": row["status"] = "native"
        result = evaluate(self.contract, rows)
        self.assertEqual(result["native"], 10)
        self.assertEqual(result["protected_lost"], ["tomli"])
        self.assertFalse(result["preserve_pass"])
        self.assertFalse(result["promotion_pass"])

    def test_progress_recovery_allows_promotion_without_rewriting_policy(self):
        before = copy.deepcopy(self.contract)
        rows = self.matrix()
        next(row for row in rows if row["id"] == "progress")["status"] = "native"
        result = evaluate(self.contract, rows)
        self.assertTrue(result["promotion_pass"])
        self.assertEqual(result["additional_native"], [])
        self.assertEqual(self.contract, before)

    def test_new_pyfiglet_cannot_offset_a_progress_regression(self):
        rows = self.matrix()
        next(row for row in rows if row['id'] == 'progress')['status'] = 'failed'
        next(row for row in rows if row['id'] == 'pyfiglet')['status'] = 'native'
        result = evaluate(self.contract, rows)
        self.assertEqual(result['native'], 10)
        self.assertEqual(result['protected_lost'], ['progress'])
        self.assertFalse(result['preserve_pass'])

    def test_partial_duplicate_and_unknown_ids(self):
        for rows in (self.matrix()[:-1], self.matrix() + [self.matrix()[0]],
                     [{"id": "unknown", "status": "native"}] + self.matrix()[1:],
                     [self.matrix()[0]] + self.matrix()[:-1]):
            with self.subTest(ids=[row["id"] for row in rows]), self.assertRaises(GateError):
                evaluate(self.contract, rows)

    def evidence(self, root):
        project = next(row for row in self.projects if row["id"] == "tomli")
        golden = root / "tests/projects/baselines/tomli/stdout.txt"
        golden.parent.mkdir(parents=True)
        golden.write_bytes(b"original\r\n")
        directory = root / "native"
        directory.mkdir()
        for name, content in {"compile.diagnostics.txt": b"", "jit.stdout": b"original\r\n",
                              "jit.stderr": b"", "tomli_sample.dll": b"assembly",
                              "tomli_sample.runtimeconfig.json": b"{}", "RyuPy.Runtime.dll": b"runtime"}.items():
            (directory / name).write_bytes(content)
        row = {key: project[key] for key in ("id", "pypi", "version", "scope", "deps")}
        row.update(entry=str(root / "tests/projects" / project["entry"]), status="native", failure_kind="",
                   run_exit=0, run_id="fresh", mode="default-jit")
        return row, project, directory, golden, digest(directory / "RyuPy.Runtime.dll")

    def test_result_rejects_stale_or_explicit_and_false_native(self):
        changes = ({"run_id": "old"}, {"mode": "explicit-jit"}, {"mode": "aot"}, {"id": "six"},
                   {"run_exit": 1}, {"run_exit": None}, {"run_exit": False}, {"failure_kind": "stdout-mismatch"},
                   {"status": "not-evaluated"}, {"status": []}, {"failure_kind": []},
                   {"status": "failed", "failure_kind": "compiler-error"})
        for change in changes:
            with self.subTest(change=change), tempfile.TemporaryDirectory() as tmp:
                row, project, directory, golden, sha = self.evidence(Path(tmp))
                row.update(change)
                with self.assertRaises(GateError):
                    validate_result(row, project, "fresh", directory, golden, sha)

    def test_original_bytes_and_complete_artifacts_are_required(self):
        changes = ("jit.stdout", "jit.stderr", "compile.diagnostics.txt", "tomli_sample.dll", "tomli_sample.runtimeconfig.json")
        for name in changes:
            with self.subTest(name=name), tempfile.TemporaryDirectory() as tmp:
                row, project, directory, golden, sha = self.evidence(Path(tmp))
                (directory / name).unlink()
                with self.assertRaises((GateError, OSError)):
                    validate_result(row, project, "fresh", directory, golden, sha)
        for name, data in (("jit.stdout", b"original\n"), ("RyuPy.Runtime.dll", b"other runtime"),
                           ("compile.error.txt", b"error")):
            with self.subTest(name=name), tempfile.TemporaryDirectory() as tmp:
                row, project, directory, golden, sha = self.evidence(Path(tmp))
                (directory / name).write_bytes(data)
                with self.assertRaises(GateError):
                    validate_result(row, project, "fresh", directory, golden, sha)

    def test_warning_stderr_is_preserved_without_becoming_failure(self):
        with tempfile.TemporaryDirectory() as tmp:
            row, project, directory, golden, sha = self.evidence(Path(tmp))
            (directory / "jit.stderr").write_bytes(b"SyntaxWarning: example\n")
            validate_result(row, project, "fresh", directory, golden, sha)

    def test_source_audit_checks_archive_and_all_extracted_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            store = root / "store"
            (store / "_dist").mkdir(parents=True)
            (root / "tests/projects").mkdir(parents=True)
            archive = store / "_dist/pkg.tar.gz"
            with tarfile.open(archive, "w:gz") as tar:
                member = tarfile.TarInfo("pkg-1/pkg.py")
                member.size = 6
                tar.addfile(member, io.BytesIO(b"x = 1\n"))
            (root / "tests/projects/lock.tsv").write_text(f"pkg\t1\tpkg.tar.gz\t{digest(archive)}\thttps://example.invalid/pkg\n")
            source = store / "pkg-1/pkg-1/pkg.py"
            source.parent.mkdir(parents=True)
            source.write_bytes(b"x = 1\n")
            projects = [{"pypi": "pkg", "version": "1", "deps": "-"}]
            audit_sources(root, store, projects)
            source.write_bytes(b"x = 2\n")
            with self.assertRaises(GateError): audit_sources(root, store, projects)
            source.unlink()
            with self.assertRaises(GateError): audit_sources(root, store, projects)
            source.write_bytes(b"x = 1\n")
            extra = source.with_name("extra.py")
            extra.write_bytes(b"extra")
            with self.assertRaises(GateError): audit_sources(root, store, projects)
            extra.unlink()
            archive.write_bytes(b"not original")
            with self.assertRaises(GateError): audit_sources(root, store, projects)

    @unittest.skipUnless(os.name == "posix", "当前进程组监管只支持 POSIX")
    def test_runner_nonzero_signal_timeout_and_stream_limit(self):
        scripts = ("raise SystemExit(2)", "import os,signal; os.kill(os.getpid(),signal.SIGTERM)",
                   "import time; time.sleep(10)", "print('x'*2000)")
        for script in scripts:
            with self.subTest(script=script), tempfile.TemporaryDirectory() as tmp:
                directory = Path(tmp)
                with patch("run_projects_gate.MAX_STREAM", 1024), self.assertRaises(GateError):
                    run_process([sys.executable, "-c", script], directory, os.environ,
                                directory / "process", 0.15, directory)
                self.assertTrue((directory / "process.json").is_file())

    @unittest.skipUnless(os.name == "posix", "当前进程组监管只支持 POSIX")
    def test_timeout_also_stops_descendant(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            child = "import time,pathlib; time.sleep(0.5); pathlib.Path('escaped').write_text('bad')"
            script = f"import subprocess,sys,time; subprocess.Popen([sys.executable,'-c',{child!r}]); time.sleep(10)"
            with self.assertRaises(GateError):
                run_process([sys.executable, "-c", script], directory, os.environ,
                            directory / "process", 0.15, directory)
            import time
            time.sleep(0.6)
            self.assertFalse((directory / "escaped").exists())

    def test_existing_output_is_never_reused(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp)
            prior = path / "summary.json"
            prior.write_text('{"valid": true}')
            with patch.object(sys, "argv", ["gate", "--gate", "preserve", "--runner", "absent", "--output", tmp]), patch("run_projects_gate.resolve", return_value=sys.executable):
                self.assertEqual(main(), 2)
            self.assertEqual(prior.read_text(), '{"valid": true}')

    def test_infrastructure_failure_cannot_publish_green(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "fresh"
            with patch.object(sys, "argv", ["gate", "--gate", "preserve", "--runner", "absent", "--output", str(path)]), patch("run_projects_gate.resolve", return_value=sys.executable), patch("run_projects_gate.execute", side_effect=GateError("缺源或运行器失败")):
                self.assertEqual(main(), 2)
            report = json.loads((path / "summary.json").read_text())
            self.assertFalse(report["valid"])
            self.assertFalse(report["preserve_pass"])
            self.assertFalse(report["promotion_pass"])


class WrapperInjectionTests(unittest.TestCase):
    def run_case(self, scenario, selected='promote'):
        contract, projects = load_inputs(ROOT)
        by_id = {p['id']: p for p in projects}
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            runtime = base / 'RyuPy.Runtime.dll'
            runtime.write_bytes(b'synthetic runtime')
            output = base / 'output'
            argv = ['gate', '--gate', selected, '--runner', sys.executable,
                    '--python', sys.executable, '--dotnet', sys.executable,
                    '--runtime', str(runtime), '--output', str(output)]
            if scenario in ('nan-timeout', 'inf-timeout'):
                argv += ['--timeout', scenario.split('-')[0]]

            def fake_process(command, cwd, env, prefix, timeout, watch):
                # 原始进程边界保留小文件，但没有启动任何子进程。
                prefix.with_suffix('.stdout').write_bytes(b'')
                prefix.with_suffix('.stderr').write_bytes(b'')
                gate.write_json(prefix.with_suffix('.json'), {'exit': 0, 'failure': None})
                if prefix.name == 'python-version':
                    prefix.with_suffix('.stdout').write_text('3.12.13\nCPython\n' if scenario != 'wrong-python' else '3.13.0\nCPython\n')
                    return
                pid = prefix.parent.name
                project = by_id[pid]
                golden = ROOT / 'tests/projects/baselines' / pid / 'stdout.txt'
                if prefix.name == 'cp':
                    prefix.with_suffix('.stdout').write_bytes(golden.read_bytes())
                    if scenario == 'cp-mismatch' and pid == 'six':
                        prefix.with_suffix('.stdout').write_bytes(b'wrong')
                    return
                native_root = Path(command[command.index('--out-dir') + 1])
                directory = native_root / pid
                directory.mkdir(parents=True)
                run_id = command[command.index('--run-id') + 1]
                native = pid in contract['protected_native'] or pid == 'progress'
                if scenario == 'historical-baseline' and pid == 'progress':
                    native = False
                status = 'native' if native else ('failed' if pid in ('progress', 'pyfiglet', 'markdown') else 'unsupported')
                row = {k: project[k] for k in ('id', 'pypi', 'version', 'scope', 'deps')}
                row.update(entry=str(ROOT / 'tests/projects' / project['entry']),
                           mode='default-jit', run_id=run_id, status=status,
                           failure_kind={'native': '', 'failed': 'runtime-error', 'unsupported': 'diagnostic'}[status],
                           run_exit={'native': 0, 'failed': 1, 'unsupported': None}[status], detail='synthetic')
                (directory / 'compile.diagnostics.txt').write_bytes(b'[E0100] Error: unsupported' if status == 'unsupported' else b'')
                if status == 'unsupported':
                    (directory / 'compile.error.txt').write_bytes(b'HasErrors')
                else:
                    stem = Path(project['entry']).stem
                    for name in (stem + '.dll', stem + '.runtimeconfig.json'):
                        (directory / name).write_bytes(b'synthetic artifact')
                    (directory / 'RyuPy.Runtime.dll').write_bytes(runtime.read_bytes())
                    (directory / 'jit.stdout').write_bytes(golden.read_bytes() if native else b'')
                    (directory / 'jit.stderr').write_bytes(b'' if native else b'Traceback (most recent call last):\nNotImplementedError: synthetic\n')
                if pid == 'progress':
                    if scenario == 'stale-last':
                        row['run_id'] = 'stale'
                    if scenario == 'empty-last':
                        (native_root / 'projects.jsonl').write_text('')
                        return
                    if scenario == 'partial-json-last':
                        (native_root / 'projects.jsonl').write_text('{')
                        return
                    if scenario in ('list-json-last', 'null-json-last'):
                        (native_root / 'projects.jsonl').write_text('[]' if scenario == 'list-json-last' else 'null')
                        return
                    if scenario == 'runner-nonzero-last':
                        raise gate.GateError('runner exit=2')
                (native_root / 'projects.jsonl').write_text(json.dumps(row) + '\n')

            save = gate.write_json
            def checked_write(path, value):
                if scenario == 'final-run-write-failure' and path.name == 'run.json' and value.get('completed'):
                    raise OSError('模拟最终运行记录写盘失败')
                if scenario == 'summary-write-failure' and path.name == 'summary.json':
                    raise OSError('模拟成功和失败摘要均不可写')
                save(path, value)

            with patch.object(gate, 'write_json', side_effect=checked_write), patch.object(sys, 'argv', argv), patch.object(gate, 'resolve', return_value=sys.executable), \
                 patch.object(gate, 'candidate_identity', return_value={'head': 'synthetic', 'files': {}}), \
                 patch.object(gate, 'audit_sources', return_value=({}, [])), \
                 patch.object(gate, 'run_process', side_effect=fake_process), \
                 contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                error = None
                try:
                    code = gate.main()
                except Exception as caught:
                    code, error = None, type(caught).__name__
            summary_path = output / 'summary.json'
            run_path = output / 'run.json'
            return {'exit': code, 'uncaught': error,
                    'summary': json.loads(summary_path.read_text()) if summary_path.exists() else None,
                    'completed': json.loads(run_path.read_text()).get('completed') if run_path.exists() else None}

    def test_historical_progress_failure_now_fails_both_gates(self):
        preserve = self.run_case('historical-baseline', 'preserve')
        promote = self.run_case('historical-baseline', 'promote')
        self.assertEqual(preserve['exit'], 1)
        self.assertEqual(promote['exit'], 1)
        self.assertFalse(preserve['summary']['promotion_pass'])

    def test_fresh_complete_mock_promotion(self):
        result = self.run_case('valid')
        self.assertEqual(result['exit'], 0)
        self.assertTrue(result['completed'])

    def test_stale_partial_and_process_fail_closed(self):
        for name in ('stale-last', 'empty-last', 'partial-json-last', 'runner-nonzero-last', 'cp-mismatch', 'list-json-last', 'null-json-last', 'wrong-python', 'final-run-write-failure'):
            with self.subTest(name=name):
                result = self.run_case(name)
                self.assertEqual(result['exit'], 2)
                self.assertFalse(result['summary']['valid'])
                self.assertFalse(result['summary']['promotion_pass'])

    def test_invalid_timeout_and_unwritable_summary_fail_closed(self):
        for name in ('nan-timeout', 'inf-timeout', 'summary-write-failure'):
            with self.subTest(name=name):
                result = self.run_case(name)
                self.assertEqual(result['exit'], 2)
                self.assertIsNone(result['summary'])


if __name__ == "__main__":
    unittest.main()
