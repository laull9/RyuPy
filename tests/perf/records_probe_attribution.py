#!/usr/bin/env python3
"""独立testhost进程运行探针归因；源码与默认产品不变，证据必须写仓库外。"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import uuid
from check_records_attribution import CASES, identity, verify_case, verify_all


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--expected-sha", required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    output = args.output.resolve()
    if output.is_relative_to(root):
        parser.error("证据目录必须位于仓库外")
    if output.exists() or args.output.is_symlink():
        parser.error("拒绝复用已有证据目录")
    output.mkdir(parents=True, exist_ok=False)
    project = root / "runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj"
    sdk = subprocess.check_output([args.dotnet, "--version"], cwd=root, text=True).strip()
    if sdk != "10.0.403":
        parser.error(f"需要固定SDK10.0.403，实际{sdk}")
    sha = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    if sha != args.expected_sha:
        parser.error("工作树提交与预期不一致")
    token = uuid.uuid4().hex
    manifest = {"commit": sha, "sdk": sdk, "token": token}
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2))
    results = []
    for case, tier, reverse in CASES:
        name = identity(case, tier, reverse)
        env = dict(os.environ)
        # 关闭或默认只在新子进程生效；不在已有JIT进程里切换开关。
        for key in ("DOTNET_TieredCompilation", "COMPlus_TieredCompilation",
                    "DOTNET_TieredPGO", "COMPlus_TieredPGO"):
            env.pop(key, None)
        if tier == "off":
            env["DOTNET_TieredCompilation"] = "0"
        env["RYUPY_RECORDS_DIAG_REVERSE"] = reverse
        env["RYUPY_RECORDS_DIAG_RUN_TOKEN"] = token
        command = [args.dotnet, "test", str(project), "-c", "Release", "--no-build", "--no-restore",
                   "--filter", f"FullyQualifiedName=RyuPy.Runtime.Tests.PyDictProbeAttributionTests.{case}",
                   "--logger", "console;verbosity=detailed", "--logger", f"trx;LogFileName={name}.trx",
                   "--results-directory", str(output)]
        passed = False
        exit_code = None
        try:
            with (output / f"{name}.log").open("x") as log:
                run = subprocess.run(command, cwd=root, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=600)
            exit_code = run.returncode
            if exit_code != 0:
                raise ValueError(f"测试进程退出{exit_code}")
            verify_case(output, case, tier, reverse, token)
            passed = True
        except Exception as error:
            (output / "failure.txt").write_text(f"{name}: {type(error).__name__}: {error}")
            raise
        finally:
            results.append({"case": name, "exit": exit_code, "passed": passed})
            (output / "summary.json").write_text(json.dumps({**manifest, "scope": "CLR host归因，非产品收益", "runs": results}, indent=2))
    verify_all(output, sha)
    print("13个独立testhost通过；12个诊断进程各72阶段样本，仅host归因")


if __name__ == "__main__":
    main()
