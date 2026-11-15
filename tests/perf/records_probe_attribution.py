#!/usr/bin/env python3
"""独立testhost进程运行探针归因；源码与默认产品不变，证据必须写仓库外。"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    output = args.output.resolve()
    if output.is_relative_to(root):
        parser.error("证据目录必须位于仓库外")
    output.mkdir(parents=True, exist_ok=True)
    project = root / "runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj"
    sdk = subprocess.check_output([args.dotnet, "--version"], cwd=root, text=True).strip()
    if sdk != "10.0.403":
        parser.error(f"需要固定SDK10.0.403，实际{sdk}")
    results = []
    cases = [("FusedIncrementPreservesStoredKeyAndRejectsOverflowOrNonLong", "default", "0")]
    cases += [(case, tier, reverse) for case in ("SmallTable", "LargeTable", "CollisionTable")
              for tier in ("default", "off") for reverse in ("0", "1")]
    for case, tier, reverse in cases:
        identity = f"{case}-{tier}-reverse{reverse}"
        env = dict(os.environ)
        # 关闭或默认只在新子进程生效；不在已有JIT进程里切换开关。
        for key in ("DOTNET_TieredCompilation", "COMPlus_TieredCompilation",
                    "DOTNET_TieredPGO", "COMPlus_TieredPGO"):
            env.pop(key, None)
        if tier == "off":
            env["DOTNET_TieredCompilation"] = "0"
        env["RYUPY_RECORDS_DIAG_REVERSE"] = reverse
        command = [args.dotnet, "test", str(project), "-c", "Release", "--no-build", "--no-restore",
                   "--filter", f"FullyQualifiedName=RyuPy.Runtime.Tests.PyDictProbeAttributionTests.{case}",
                   "--logger", "console;verbosity=detailed", "--logger", f"trx;LogFileName={identity}.trx",
                   "--results-directory", str(output)]
        with (output / f"{identity}.log").open("w") as log:
            run = subprocess.run(command, cwd=root, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=600)
        trx = output / f"{identity}.trx"
        counters = ET.parse(trx).find(".//{*}Counters") if trx.exists() else None
        passed = (run.returncode == 0 and counters is not None and counters.get("total") == "1"
                  and counters.get("passed") == "1" and counters.get("failed") == "0")
        if passed and case != cases[0][0]:
            lines = (output / f"{identity}.log").read_text().splitlines()
            # 每个独立进程12阶段×6轮，不将缺日志当成功。
            passed = sum(line.strip().startswith("attribution keys=") for line in lines) == 72
        results.append({"case": identity, "exit": run.returncode, "passed": passed})
        (output / "summary.json").write_text(json.dumps({"sdk": sdk, "scope": "CLR host归因，非产品收益", "runs": results}, indent=2))
        if not passed:
            raise SystemExit(f"归因门失败，保留证据：{identity}")
    print("13个独立testhost通过；12个诊断进程各72阶段样本，仅host归因")


if __name__ == "__main__":
    main()
