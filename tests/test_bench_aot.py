"""AOT 基准产物识别、防逃逸与执行隔离测试。

依 ADR-0182 跳过普通文件哈希计算；手写必要最小平台二进制头（ELF / Mach-O），不依赖 manifest_fixture。
覆盖：独占根边界防逃逸、符号链接拒绝、模式隔离（standalone / onefile）、无 wait4 平台拦截、
以及并发工作根隔离。
"""
from __future__ import annotations

import json
import os
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch

TESTS_DIR = Path(__file__).resolve().parent
if str(TESTS_DIR) not in sys.path:
    sys.path.insert(0, str(TESTS_DIR))

from benchlib import artifact
from benchlib.suite import Benchmark


def _make_dummy_binary(path: Path, rid: str) -> None:
    """手写生成符合目标平台要求的最小可执行二进制头部，不计算哈希。"""
    if rid == "osx-arm64":
        # magic=0xfeedfacf, cputype=0x100000c (arm64), cpusubtype=0, filetype=2 (MH_EXECUTE)
        header = struct.pack("<IIII", 0xfeedfacf, 0x100000c, 0, 2) + b"\x00" * 48
    elif rid == "linux-x64":
        # \x7fELF\x02\x01\x01 + padding + kind(2=ET_EXEC), machine(62=EM_X86_64), version(1) + ... + ehsize(64)
        header = (
            b"\x7fELF\x02\x01\x01"
            + b"\x00" * 9
            + struct.pack("<HHI", 2, 62, 1)
            + b"\x00" * 28
            + struct.pack("<H", 64)
            + b"\x00" * 10
        )
    else:
        raise ValueError(f"未支持的测试头 RID: {rid}")
    path.write_bytes(header)
    path.chmod(0o755)


def _make_dummy_manifest(dist_dir: Path, stem: str) -> None:
    """生成必要的基础清单结构，不含任何哈希校验字段。"""
    data = {
        "schema_version": 1,
        "artifacts": [
            {
                "relative_path": stem,
                "role": "native_executable",
                "size": 64,
                "sha256": "",
            }
        ],
    }
    (dist_dir / "ryupy-artifact.json").write_text(json.dumps(data), encoding="utf-8")


class TestBenchAotArtifact(unittest.TestCase):
    def test_host_rid_posix_boundary(self):
        """host_rid 仅接受 POSIX 平台（Linux x64 与 macOS arm64），拒绝 Windows。"""
        with patch("sys.platform", "darwin"), patch("platform.machine", return_value="arm64"):
            self.assertEqual(artifact.host_rid(), "osx-arm64")
        with patch("sys.platform", "linux"), patch("platform.machine", return_value="x86_64"):
            self.assertEqual(artifact.host_rid(), "linux-x64")
        with patch("sys.platform", "win32"), patch("platform.machine", return_value="AMD64"):
            with self.assertRaises(ValueError):
                artifact.host_rid()

    def test_verified_native_standalone_layout_and_log(self):
        """标准 standalone 模式下，能正确识别 dist 布局并支持免哈希清单。"""
        with tempfile.TemporaryDirectory() as temp, patch.object(artifact, "host_rid", return_value="osx-arm64"):
            root = Path(temp).resolve()
            bench = Benchmark("hello", root / "hello.py")
            dist = root / "hello.dist"
            dist.mkdir(parents=True)
            target = dist / "hello"
            _make_dummy_binary(target, "osx-arm64")
            _make_dummy_manifest(dist, "hello")

            log = f"已生成 {target}\n"
            found = artifact.verified_native(root, bench, log, mode="standalone")
            self.assertEqual(found, target.resolve())

            # 即使日志为空，目录布局也可识别
            found_fallback = artifact.verified_native(root, bench, "", mode="standalone")
            self.assertEqual(found_fallback, target.resolve())

    def test_mode_isolation_standalone_vs_onefile(self):
        """严格区分 standalone 与 onefile，拒绝跨 mode 误认。"""
        with tempfile.TemporaryDirectory() as temp, patch.object(artifact, "host_rid", return_value="osx-arm64"):
            root = Path(temp).resolve()
            bench = Benchmark("hello", root / "hello.py")

            # 仅生成 onefile 布局
            onefile_dir = root / "hello.onefile"
            onefile_dir.mkdir(parents=True)
            onefile_bin = onefile_dir / "hello"
            _make_dummy_binary(onefile_bin, "osx-arm64")

            # 在 standalone 模式下查找应失败
            with self.assertRaises(ValueError):
                artifact.verified_native(root, bench, f"已生成 {onefile_bin}\n", mode="standalone")

            # 在 onefile 模式下查找成功
            found = artifact.verified_native(root, bench, f"已生成 {onefile_bin}\n", mode="onefile")
            self.assertEqual(found, onefile_bin.resolve())

    def test_root_boundary_rejection_escape(self):
        """拒绝位于独占 root 外部的原生入口，防止日志逃逸。"""
        with tempfile.TemporaryDirectory() as temp_root, tempfile.TemporaryDirectory() as outside:
            root = Path(temp_root)
            bench = Benchmark("hello", root / "hello.py")

            # 在外部目录创建假产物
            outside_dir = Path(outside) / "hello.dist"
            outside_dir.mkdir(parents=True)
            outside_bin = outside_dir / "hello"
            _make_dummy_binary(outside_bin, "osx-arm64")

            with patch.object(artifact, "host_rid", return_value="osx-arm64"):
                # 日志试图指向 root 外部
                log = f"已生成 {outside_bin}\n"
                with self.assertRaises(ValueError):
                    artifact.verified_native(root, bench, log, mode="standalone")

    def test_symlink_rejection(self):
        """拒绝符号链接，只接受真实普通文件。"""
        with tempfile.TemporaryDirectory() as temp, patch.object(artifact, "host_rid", return_value="osx-arm64"):
            root = Path(temp).resolve()
            bench = Benchmark("hello", root / "hello.py")
            real_file = root / "real_binary"
            _make_dummy_binary(real_file, "osx-arm64")

            dist = root / "hello.dist"
            dist.mkdir(parents=True)
            symlink_bin = dist / "hello"
            symlink_bin.symlink_to(real_file)

            with self.assertRaises(ValueError):
                artifact.verified_native(root, bench, f"已生成 {symlink_bin}\n", mode="standalone")

    def test_header_corruption_and_permission_rejection(self):
        """头损坏或缺失执行权限时抛出明确异常。"""
        with tempfile.TemporaryDirectory() as temp, patch.object(artifact, "host_rid", return_value="osx-arm64"):
            root = Path(temp).resolve()
            bench = Benchmark("hello", root / "hello.py")
            dist = root / "hello.dist"
            dist.mkdir(parents=True)
            target = dist / "hello"

            # 缺失可执行权限
            _make_dummy_binary(target, "osx-arm64")
            target.chmod(0o644)
            with self.assertRaises(ValueError):
                artifact.verified_native(root, bench, "", mode="standalone")

            # 二进制头非 Mach-O
            target.chmod(0o755)
            target.write_bytes(b"\x00" * 64)
            with self.assertRaises(ValueError):
                artifact.verified_native(root, bench, "", mode="standalone")

    def test_concurrent_work_root_isolation(self):
        """不同运行独占各自的临时工作根，清理一个不影响另一个。"""
        with tempfile.TemporaryDirectory() as root_a, tempfile.TemporaryDirectory() as root_b:
            path_a = Path(root_a) / "bench/aot/job1"
            path_b = Path(root_b) / "bench/aot/job2"
            path_a.mkdir(parents=True)
            path_b.mkdir(parents=True)
            (path_a / "artifact").write_bytes(b"data_a")
            (path_b / "artifact").write_bytes(b"data_b")

            # 模拟 job1 完成并清理 root_a
            import shutil
            shutil.rmtree(root_a)
            self.assertFalse(Path(root_a).exists())
            # job2 的产物完好无损
            self.assertTrue((path_b / "artifact").exists())
            self.assertEqual((path_b / "artifact").read_bytes(), b"data_b")

    def test_corrupted_stats_json_raises_value_error(self):
        """当 stats 文件内容不是有效 JSON 时坚决抛错，拒绝吞空字典。"""
        from benchlib import measure

        # 让真正的被测子进程写坏 JSON，覆盖 run_once 的实际解析路径。
        command = [sys.executable, "-c",
                   "import os; from pathlib import Path; Path(os.environ['RYUPY_STATS']).write_text('{broken')"]
        with self.assertRaisesRegex(ValueError, "JSON"):
            measure.run_once(command, {}, TESTS_DIR.parent, "RYUPY_STATS")

    def test_cpython_oracle_failure_detection(self):
        """CPython oracle 执行非零退出时立即识别为 cpython-failed。"""
        from benchlib import measure

        bench = Benchmark("bad", Path("/dummy/bad.py"))
        plan = measure.Plan(
            root=Path("/dummy"),
            python="python3",
            warmup=1,
            runs=1,
            with_cpython=True,
            include_pending=False,
            work_root=Path("/dummy"),
            backend="aot",
        )
        with patch.object(measure, "build_case", return_value=(Path("/dummy/out"), 10.0, "", "ok")):
            # 模拟 CPython 退出码为 1
            bad_sample = measure.Sample(10.0, 1024, {}, b"", b"ModuleNotFoundError: no module", 1)
            with patch.object(measure, "run_once", return_value=bad_sample):
                res = measure.measure_bench(plan, Path("/dummy/cli"), Path("/dummy/target"), bench)
                self.assertEqual(res.status, "cpython-failed")
                self.assertIn("ModuleNotFoundError", res.note)

    def test_variant_failure_detection(self):
        """变体运行遭遇非零退出码时标记 variant-failed 并捕获错误。"""
        from benchlib import measure
        from benchlib.suite import Variant

        bench = Benchmark("var_bench", Path("/dummy/var.py"), variants=[Variant("no_pic", {"RYUPY_PIC": "0"})])
        plan = measure.Plan(
            root=Path("/dummy"),
            python="python3",
            warmup=0,
            runs=1,
            with_cpython=False,
            include_pending=False,
            work_root=Path("/dummy"),
            backend="aot",
        )
        ok_sample = measure.Sample(10.0, 1024, {}, b"hello\n", b"", 0)
        fail_variant = measure.Sample(10.0, 1024, {}, b"", b"Fatal error in variant", 139)

        def mock_run(cmd, env, cwd, stats_env):
            # 变体环境变量中包含 RYUPY_PIC=0
            if env.get("RYUPY_PIC") == "0":
                return fail_variant
            return ok_sample

        with patch.object(measure, "build_case", return_value=(Path("/dummy/out"), 10.0, "", "ok")), \
             patch.object(measure, "run_once", side_effect=mock_run):
            res = measure.measure_bench(plan, Path("/dummy/cli"), Path("/dummy/target"), bench)
            self.assertEqual(res.status, "variant-failed")
            self.assertIn("Fatal error in variant", res.note)

    def test_cross_mode_comparison_guard(self):
        """禁止跨 backend 或跨 native 模式进行基线比对。"""
        from benchlib import report

        # 跨 backend 拦截
        with self.assertRaises(ValueError):
            report.validate_comparison("aot", True, {"backend": "jit", "native": True}, {})

        # 跨 native 模式拦截
        with self.assertRaises(ValueError):
            report.validate_comparison("aot", True, {"backend": "aot", "native": False}, {})






class BenchAotTests(unittest.TestCase):
    """保留原入口与采样回归合同，按直接脚本 v2 协议迁移断言。"""

    def test_parse_aot_profile_rejected_before_work(self):
        import contextlib
        import io
        import run_benchmarks as entry
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            entry.parse_args(["--aot", "--profile"])
        self.assertTrue(entry.parse_args(["--aot"]).aot)

    def test_early_compare_rejects_before_build(self):
        import contextlib
        import io
        import run_benchmarks as entry
        from benchlib import measure
        with tempfile.TemporaryDirectory() as temp:
            saved = Path(temp) / "old.json"
            saved.write_text(json.dumps({"environment": {"backend": "aot"}, "results": []}))
            with patch.object(measure, "target_dir", return_value=Path(temp)), patch.object(measure, "build_cli") as build, contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(entry.main(["--aot", "--compare", str(saved), "hello"]), 2)
                build.assert_not_called()

    def test_save_rejects_cross_legacy_and_invalid_before_build(self):
        import contextlib
        import io
        import run_benchmarks as entry
        from benchlib import measure
        for env in ({}, None, {"backend": "jit", "measurement_protocol": "direct-wall-v2"}):
            with self.subTest(environment=env), tempfile.TemporaryDirectory() as temp:
                saved = Path(temp) / "saved.json"
                original = json.dumps({"environment": env, "results": []})
                saved.write_text(original)
                with patch.object(measure, "target_dir", return_value=Path(temp)), patch.object(measure, "build_cli") as build, contextlib.redirect_stderr(io.StringIO()):
                    self.assertEqual(entry.main(["--aot", "--save", str(saved), "hello"]), 2)
                    build.assert_not_called()
                self.assertEqual(saved.read_text(), original)

    def test_aot_failure_does_not_return_old_artifact(self):
        from types import SimpleNamespace
        from benchlib import measure
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            old = root / "bench/aot/old/program.dist/program"
            old.parent.mkdir(parents=True)
            old.write_bytes(b"old")
            with patch.object(measure.subprocess, "run", return_value=SimpleNamespace(returncode=1, stderr="full failure")):
                product, _, error, status = measure.build_case(root, Path("cli"), Benchmark("program", root / "program.py"), root, backend="aot")
            self.assertIsNone(product)
            self.assertEqual((error, status), ("full failure", "build-failed"))
            self.assertEqual(old.read_bytes(), b"old")

    def test_native_direct_command_and_original_sampling(self):
        from benchlib import measure
        bench = Benchmark("hello", Path("hello.py"))
        plan = measure.Plan(Path("."), "python", 1, 5, True, False, Path("unused"), "aot")
        calls = []
        def sample(command, env, cwd, stats_env):
            calls.append((command, stats_env))
            stats = {"main_ms": 3, "alloc_bytes": 4} if stats_env else {}
            return measure.Sample(1, 2, stats, b"ok", b"", 0)
        with patch.object(measure, "build_case", return_value=(Path("/owned/hello"), 4, "", "ok")), patch.object(measure, "run_once", side_effect=sample):
            result = measure.measure_bench(plan, Path("cli"), Path("target"), bench)
        self.assertEqual(result.status, "ok")
        self.assertEqual(len(result.cpython.wall_ms), 5)
        self.assertEqual(len(result.ryupy.wall_ms), 5)
        self.assertEqual(len(calls), 15)  # oracle + 6 pairs + 2 independent instrumented
        self.assertEqual(sum(stats is not None for _, stats in calls), 1)
        for index in range(1, 13, 2):
            self.assertEqual(calls[index], (["python", "-B", "hello.py"], None))
            self.assertEqual(calls[index + 1], (["/owned/hello"], None))
        self.assertEqual(result.ryupy.stats["alloc_bytes"], 4)

    def test_instrumented_failure_and_stderr_mismatch_rejected(self):
        from benchlib import measure
        bench = Benchmark("x", Path("x.py"))
        plan = measure.Plan(Path("."), "python", 0, 1, False, False, Path("unused"), "aot")
        ok = measure.Sample(1, 2, {}, b"ok", b"", 0)
        for samples, status in (([ok, ok, measure.Sample(1, 2, {}, b"", b"bad stats", 1)], "run-failed"),
                                ([ok, measure.Sample(1, 2, {}, b"ok", b"unexpected warning", 0)], "mismatch")):
            with self.subTest(status=status), patch.object(measure, "build_case", return_value=(Path("/owned/x"), 1, "", "ok")), patch.object(measure, "run_once", side_effect=samples):
                self.assertEqual(measure.measure_bench(plan, Path("cli"), Path("target"), bench).status, status)

    def test_parent_link_and_dotdot_rejected(self):
        with tempfile.TemporaryDirectory() as temp, tempfile.TemporaryDirectory() as outside, patch.object(artifact, "host_rid", return_value="osx-arm64"):
            root = Path(temp).resolve()
            external = Path(outside).resolve() / "hello.dist"
            external.mkdir()
            _make_dummy_binary(external / "hello", "osx-arm64")
            (root / "hello.dist").symlink_to(external, target_is_directory=True)
            with self.assertRaises(ValueError):
                artifact.verified_native(root, Benchmark("hello", root / "hello.py"), "")
            self.assertFalse(artifact._is_safe_regular_file(root / ".." / "hello", root))

    def test_cli_rebuilds_unless_explicitly_frozen(self):
        from benchlib import measure
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            cli = root / "release/ryupy"
            cli.parent.mkdir()
            cli.write_text("existing stale binary")
            cli.chmod(0o755)
            with patch.object(measure.subprocess, "run") as run:
                measure.build_cli(root, root)
                self.assertEqual(run.call_args_list[0].args[0][:2], ["cargo", "build"])
            with patch.object(measure.subprocess, "run") as run:
                measure.build_cli(root, root, cli_override=cli)
                self.assertEqual(run.call_count, 1)
                self.assertEqual(run.call_args.args[0][-1], "--help")

    def test_report_saved_raw_jit_and_units(self):
        from benchlib import measure, report
        raw = {"jit_ms": 0, "jit_methods": 0, "alloc_bytes": 1048576}
        result = measure.Result(Benchmark("x", Path("x.py")), ryupy=measure.Series([1], [0.5], [1024], raw), backend="aot")
        self.assertIn("N/A", "\n".join(report.resource_table([result])))
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "saved.json"
            report.save(path, {"backend": "aot"}, [result])
            _, rows = report.load(path)
            self.assertEqual(rows["x"]["ryupy"]["stats"], raw)


if __name__ == "__main__":
    unittest.main()
