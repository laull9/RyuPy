"""比较已编译的相同数据类基准：三个独立进程、分层JIT开/关分别保存，不编译或替换运行时。"""
import argparse
import json
import os
from pathlib import Path
import statistics
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("variants", nargs="+", help="标签=含dataclass_hot.dll的独立目录")
parser.add_argument("--output", required=True, type=Path)
parser.add_argument("--repeats", type=int, default=3)
args = parser.parse_args()
if args.repeats < 1:
    parser.error("repeats必须至少为1")
report = {}
expected = {}
for tier in ("0", "1"):
    for item in args.variants:
        label, directory = item.split("=", 1)
        runs = []
        for _ in range(args.repeats):
            run = subprocess.run(
                ["dotnet", str(Path(directory) / "dataclass_hot.dll")],
                env=dict(os.environ, DOTNET_TieredCompilation=tier),
                check=True, text=True, capture_output=True,
            )
            samples = {}
            for line in run.stdout.splitlines():
                if line.startswith("semantics "):
                    # 只对正确实现进行这种等价比较；错误旧eq基线须单独记录。
                    if "semantics" in expected and expected["semantics"] != line:
                        raise ValueError("语义输出不一致，停止性能比较")
                    expected["semantics"] = line
                    continue
                name, elapsed, result = line.split()
                if name in expected and expected[name] != result:
                    raise ValueError(f"{name}校验结果不一致")
                expected[name] = result
                samples.setdefault(name, []).append(float(elapsed))
            runs.append({name: statistics.median(values) for name, values in samples.items()})
        report[tier + "_" + label] = runs
args.output.write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
