"""冻结旧新CLI的原始AOT配对：先核验退出/完整双流，独占计时，统计和搬迁另轮执行。"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import statistics
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tests"))
from benchlib import measure, suite


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--old", required=True, type=Path)
    parser.add_argument("--new", required=True, type=Path)
    parser.add_argument("--python", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--only", default="", help="逗号分隔的原始基准名，默认全部")
    parser.add_argument("--runs", default=5, type=int)
    args = parser.parse_args()
    if args.runs < 1:
        parser.error("--runs必须为正整数")
    for name in ("old", "new", "python", "output"):
        setattr(args, name, getattr(args, name).resolve())
    if args.output.is_relative_to(ROOT):
        parser.error("原始报告必须写入仓库外目录")
    benches = suite.discover(ROOT / "tests/benchmarks")
    requested = set(args.only.split(",")) if args.only else {b.name for b in benches}
    unknown = requested - {b.name for b in benches}
    if unknown:
        parser.error(f"未知基准名: {sorted(unknown)}")
    return args, [b for b in benches if b.name in requested]


def main():
    args, benches = arguments()
    env = measure._base_env()
    env.pop("RYUPY_RUNTIME_DIR", None)
    # build_case继承父环境；禁止意外借用当前checkout的运行时覆盖冻结CLI载荷。
    os.environ.pop("RYUPY_RUNTIME_DIR", None)
    expected_version = (ROOT / "tests/pinned-python.txt").read_text().strip()
    version = subprocess.run(
        [str(args.python), "-c", "import platform; print(platform.python_version())"],
        capture_output=True, check=True, text=True, env=env,
    ).stdout.strip()
    if version != expected_version:
        raise RuntimeError(f"基准Python版本须为{expected_version}，实际{version}")

    def checked(command, expected=None, stats=None):
        result = measure.run_once(command, env, ROOT, stats)
        if result.exit_code or expected is not None and (result.stdout, result.stderr) != expected:
            raise RuntimeError((command, result.exit_code, result.stdout, result.stderr))
        return result

    rows = []
    with tempfile.TemporaryDirectory(prefix="ryupy-hotspot-pair-", dir=measure.get_safe_external_dir(ROOT)) as temp:
        root = Path(temp)
        for bench in benches:
            paths = {}
            for label, cli in (("A", args.old), ("B", args.new)):
                exe, _, error, status = measure.build_case(ROOT, cli, bench, root / label, backend="aot", native=True)
                if exe is None:
                    raise RuntimeError((bench.name, label, status, error))
                paths[label] = exe
            commands = {"CP": [str(args.python), "-B", str(bench.path)],
                        "A": [str(paths["A"])], "B": [str(paths["B"])]}
            oracle = checked(commands["CP"])
            expected = (oracle.stdout, oracle.stderr)
            samples = {label: [] for label in commands}
            # 构建两臂后才计时；下一项构建必须等本项计时和验证结束。
            for index in range(args.runs + 1):
                for label in (("CP", "A", "B") if index % 2 == 0 else ("CP", "B", "A")):
                    result = checked(commands[label], expected)
                    if index:
                        samples[label].append({"ms": result.wall_ms, "rss": result.max_rss_bytes})
            stats = {}
            for label in ("A", "B"):
                result = checked(commands[label], expected, "RYUPY_STATS")
                if not {"main_ms", "alloc_bytes"} <= result.stats.keys():
                    raise RuntimeError((bench.name, label, "统计字段缺失"))
                stats[label] = result.stats
                dist = paths[label].parent
                moved = root / "中文 空格搬迁" / label / bench.name
                shutil.copytree(dist, moved)
                dist.rename(dist.with_name(dist.name + ".hidden"))
                result = measure.run_once([str(moved / paths[label].name)], env | {"PATH": "/nonexistent"}, moved, None)
                if (result.exit_code, result.stdout, result.stderr) != (0, *expected):
                    raise RuntimeError((bench.name, label, "搬迁失败", result.stderr))
            rows.append({"name": bench.name, "samples": samples, "stats": stats})
            args.output.write_text(json.dumps({"old": str(args.old), "new": str(args.new),
                "python": str(args.python), "runs": args.runs, "warmup": 1, "rows": rows}, indent=2))
            medians = {k: round(statistics.median(v["ms"] for v in values), 3) for k, values in samples.items()}
            print(bench.name, medians, flush=True)
    print(f"ALL {len(rows)}通过，临时构建根已清理", flush=True)


if __name__ == "__main__":
    main()
