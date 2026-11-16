#!/usr/bin/env python3
"""四次独立构建，JIT/AOT 与 PIC 双臂；只诊断 Runtime，不代表产品验收。"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(sys.argv[1]).resolve()
OUT.mkdir(parents=True, exist_ok=False)
def is_diagnostic_variable(key):
    """只清除两套别名的输出诊断旗标，不改变PGO/OSR等优化策略。"""
    for prefix in ("DOTNET_", "COMPlus_"):
        if key.startswith(prefix):
            setting = key[len(prefix):]
            return setting.startswith(("JitDisasm", "JitDump")) or setting == "JitStdOutFile"
    return False


ENV = {k: v for k, v in os.environ.items() if k not in
       {"RYUPY_PIC", "RYUPY_PIC_STATS", "RYUPY_RUNTIME_DIR",
        "DOTNET_TieredCompilation", "COMPlus_TieredCompilation", "RYUPY_CAPTURE_DRIVER", "RYUPY_CAPTURE_SCENARIO"}
       and not is_diagnostic_variable(k)}
REPORT = {"commands": [], "rows": [], "entries": [], "diagnostics": []}
SCENARIOS = ("compiler_shape", "mro_depth8", "polymorphic4", "invalidate_miss")
CASES = [("legacy", None), ("isolated", None)] + [("scenario", s) for s in SCENARIOS]
DISASM = ("Program:* AblationDrivers:* ScenarioDrivers:* RyuPy.Runtime.PyPic:CaptureMethodPlan* "
          "RyuPy.Runtime.PyPic:IsPlainCaptureClass* RyuPy.Runtime.PyPic:CallExperimentalScalar2")


def run(argv, tag, extra=None):
    """所有命令保留双流与退出码；异常时由 finally 保存已完成证据。"""
    result = subprocess.run(argv, cwd=ROOT, env=ENV | (extra or {}),
                            capture_output=True, text=True, timeout=900)
    (OUT / (tag + ".stdout")).write_text(result.stdout)
    (OUT / (tag + ".stderr")).write_text(result.stderr)
    REPORT["commands"].append({"argv": argv, "tag": tag, "exit": result.returncode})
    if result.returncode:
        raise RuntimeError(f"{tag}: {result.returncode}")
    return result.stdout


try:
    assert run(["dotnet", "--version"], "sdk").strip() == "10.0.403"
    REPORT["source"] = run(["git", "rev-parse", "HEAD", "HEAD^{tree}"], "source").splitlines()
    for round_id in range(4):
        for backend in ("jit", "aot"):
            tag = f"{round_id}-{backend}"
            product = OUT / tag
            obj = OUT / ("obj-" + tag)
            cmd = ["dotnet", "publish", str(ROOT / "tests/perf/capture-plan/Host.csproj"),
                   "-c", "Release", "-o", str(product), "--artifacts-path", str(obj)]
            if backend == "aot":
                cmd += ["-r", "linux-x64", "-p:PublishAot=true"]
            run(cmd, "publish-" + tag)
            shutil.rmtree(obj)
            entry = product / ("RyuPy.CapturedPlanPrototypeHost.dll" if backend == "jit" else "RyuPy.CapturedPlanPrototypeHost")
            REPORT["entries"].append({"path": str(entry.relative_to(OUT)),
                "sha256": hashlib.sha256(entry.read_bytes()).hexdigest()})
            # 正常分层与禁分层分别记录；不能把后者当作默认产品配置。
            for profile in (("tiered", "no-tier") if backend == "jit" else ("aot",)):
                for pic in ("1", "0"):
                    argv = ["dotnet", str(entry)] if backend == "jit" else [str(entry)]
                    extra = {"RYUPY_PIC": pic}
                    if backend == "jit":
                        extra["DOTNET_TieredCompilation"] = "0" if profile == "no-tier" else "1"
                    for driver, scenario in CASES:
                        suffix = driver + ("-" + scenario if scenario else "")
                        settings = extra | {"RYUPY_CAPTURE_DRIVER": driver}
                        if scenario:
                            settings["RYUPY_CAPTURE_SCENARIO"] = scenario
                        stdout = run(argv, f"run-{tag}-{profile}-pic{pic}-{suffix}", settings)
                        rows = [json.loads(line) for line in stdout.splitlines()]
                        arms = (("late", "bound", "plan", "scalar", "scalar_split",
                                 "scalar_narrow", "capture_only", "invoke_only",
                                 "scalar_capture_split", "capture_split_only") if driver == "legacy"
                                else (("original", "legacy_split", "c0i0", "c0i1", "c1i0", "c1i1")
                                      if driver == "isolated" else ("original", "c0i0", "c0i1", "c1i0", "c1i1")))
                        assert len(rows) == 20 * len(arms)
                        assert {(r["batch"], r["arm"]) for r in rows} == {
                            (b, a) for b in range(20) for a in arms}
                        for row in rows:
                            assert row["count"] == 100000 and row["ms"] >= 0 and row["allocation"] >= 0
                            if scenario:
                                assert row["scenario"] == scenario
                            REPORT["rows"].append(row | {"round": round_id, "backend": backend,
                                                        "profile": profile, "pic": pic, "driver": driver})
            # 诊断单独进程：原始stdout混合机器码与host输出，绝不纳入性能行。
            # 不使用JitStdOutFile，避免已登记的退出时后台编译写流竞态。
            if backend == "jit":
                for profile in ("tiered", "no-tier"):
                    for driver, scenario in CASES:
                        suffix = driver + ("-" + scenario if scenario else "")
                        diag_tag = f"disasm-{tag}-{profile}-{suffix}"
                        settings = {"RYUPY_PIC": "1", "RYUPY_CAPTURE_DRIVER": driver,
                                    "DOTNET_TieredCompilation": "0" if profile == "no-tier" else "1",
                                    "DOTNET_JitDisasm": DISASM, "DOTNET_JitDisasmDiffable": "1",
                                    "DOTNET_JitDisasmOnlyOptimized": "0"}
                        if scenario:
                            settings["RYUPY_CAPTURE_SCENARIO"] = scenario
                        stdout = run(["dotnet", str(entry)], diag_tag, settings)
                        headers = [line for line in stdout.splitlines() if "Assembly listing for method" in line]
                        REPORT["diagnostics"].append({"tag": diag_tag, "round": round_id,
                            "profile": profile, "driver": driver, "scenario": scenario,
                            "headers": headers, "has_tier1": any("Tier1" in h for h in headers),
                            "stdout_sha256": hashlib.sha256((OUT / (diag_tag + ".stdout")).read_bytes()).hexdigest(),
                            "stderr_sha256": hashlib.sha256((OUT / (diag_tag + ".stderr")).read_bytes()).hexdigest()})
                        # 缺Tier1不自动等价于优化失败，保留所有层并由审计明确标为证据缺口。
                        assert headers, f"{diag_tag}: no assembly listings captured"

finally:
    (OUT / "report.json").write_text(json.dumps(REPORT, ensure_ascii=False, indent=2))
