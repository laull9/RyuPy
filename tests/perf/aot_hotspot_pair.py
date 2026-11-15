"""冻结旧新CLI的原始AOT配对：先核验退出/完整双流，独占计时，统计和搬迁另轮执行。"""
from __future__ import annotations

import argparse
import json
import hashlib
import itertools
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
from aot_pair_evidence import save_manifest, require_space, cleanup_case


def execution_orders(runs, balanced=False):
    if balanced:
        if runs % 6: raise ValueError("平衡三臂必须6的整数倍样本")
        # 每组六个排列使CP/A/B各位置相同，A/B先后各半；首个单独预热不计样本。
        cycle = list(itertools.permutations(("CP", "A", "B")))
        return [("CP", "A", "B")] + [cycle[i % 6] for i in range(runs)]
    return [("CP", "A", "B") if i % 2 == 0 else ("CP", "B", "A") for i in range(runs + 1)]


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--old", required=True, type=Path)
    parser.add_argument("--new", required=True, type=Path)
    parser.add_argument("--python", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--raw-evidence", type=Path, help="显式归档各次完整双流和构建身份，保持默认行为")
    parser.add_argument("--only", default="", help="逗号分隔的原始基准名，默认全部")
    parser.add_argument("--suite", choices=("original", "generator-consumers"), default="original")
    parser.add_argument("--balanced-order", action="store_true", help="显式六排列平衡三臂；runs须6的倍数")
    parser.add_argument("--reverse-cases", action="store_true", help="显式反转本轮用例顺序")
    parser.add_argument("--runs", default=5, type=int)
    parser.add_argument("--manifest-evidence", type=Path, help="逐用例两臂实际AOT清单，校验固定SDK")
    parser.add_argument("--min-free-gib", type=int, default=0, help="显式容量门；0保持原默认不设门")
    parser.add_argument("--cleanup-completed", action="store_true", help="显式逐项清理，须同时保存清单证据")
    args = parser.parse_args()
    if args.balanced_order and args.runs % 6:
        parser.error("平衡三臂runs须为6的倍数")
    if args.runs < 1 or args.min_free_gib < 0:
        parser.error("--runs须为正整数，--min-free-gib不能为负")
    if args.cleanup_completed and not args.manifest_evidence:
        parser.error("逐项清理必须同时指定--manifest-evidence")
    for name in ("old", "new", "python", "output"):
        setattr(args, name, getattr(args, name).resolve())
    if args.output.is_relative_to(ROOT):
        parser.error("原始报告必须写入仓库外目录")
    if args.manifest_evidence:
        args.manifest_evidence = args.manifest_evidence.resolve()
        if args.manifest_evidence.is_relative_to(ROOT):
            parser.error("清单证据必须位于仓库外")
    if args.raw_evidence:
        args.raw_evidence = args.raw_evidence.resolve()
        if args.raw_evidence.is_relative_to(ROOT): parser.error("原始证据必须在仓库外")
        args.raw_evidence.mkdir(parents=True, exist_ok=False)
    source_dir = (ROOT / "tests/benchmarks" if args.suite == "original"
                  else ROOT / "tests/perf/generator_consumers")
    benches = suite.discover(source_dir)
    requested = set(args.only.split(",")) if args.only else {b.name for b in benches}
    unknown = requested - {b.name for b in benches}
    if unknown:
        parser.error(f"未知基准名: {sorted(unknown)}")
    selected = [b for b in benches if b.name in requested]
    if args.reverse_cases: selected.reverse()
    return args, selected


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

    def archive(tag, command, result, cwd=ROOT):
        if args.raw_evidence:
            folder = args.raw_evidence/bench.name; folder.mkdir(parents=True, exist_ok=True)
            (folder/(tag+'.stdout')).write_bytes(result.stdout)
            (folder/(tag+'.stderr')).write_bytes(result.stderr)
            (folder/(tag+'.json')).write_text(json.dumps({'command':command, 'cwd':str(cwd),
                'exit':result.exit_code, 'ms':result.wall_ms, 'rss':result.max_rss_bytes, 'stats':result.stats}, indent=2))

    def checked(command, expected=None, stats=None, tag='oracle'):
        result = measure.run_once(command, env, ROOT, stats)
        archive(tag, command, result)
        if result.exit_code or expected is not None and (result.stdout, result.stderr) != expected:
            raise RuntimeError((command, result.exit_code, result.stdout, result.stderr))
        return result

    rows = []
    with tempfile.TemporaryDirectory(prefix="ryupy-hotspot-pair-", dir=measure.get_safe_external_dir(ROOT)) as temp:
        root = Path(temp)
        for bench in benches:
            paths = {}
            case_root = root / bench.name
            case_root.mkdir()
            disk = {}
            source_bytes = bench.path.read_bytes() if args.raw_evidence else None
            if args.raw_evidence:
                folder = args.raw_evidence/bench.name; folder.mkdir()
                (folder/'source.py').write_bytes(source_bytes)
                identities = {'source':str(bench.path),'source_sha256':hashlib.sha256(source_bytes).hexdigest(),'arms':{}}
            for label, cli in (("A", args.old), ("B", args.new)):
                if args.min_free_gib:
                    disk[label] = require_space(root, args.min_free_gib * 1024**3)
                exe, _, error, status = measure.build_case(ROOT, cli, bench, case_root / label, backend="aot", native=True,
                    command_evidence=folder/(label+'-build-command.json') if args.raw_evidence else None)
                if args.raw_evidence:
                    (folder/(label+'-build.log')).write_text(error)
                if exe is None:
                    raise RuntimeError((bench.name, label, status, error))
                paths[label] = exe
                if args.raw_evidence:
                    identities['arms'][label] = {'cli':str(cli), 'cli_sha256':hashlib.sha256(cli.read_bytes()).hexdigest(),
                        'executable':str(exe), 'executable_sha256':hashlib.sha256(exe.read_bytes()).hexdigest()}
                    (folder/'identities.json').write_text(json.dumps(identities,indent=2))
                if args.manifest_evidence:
                    save_manifest(exe, args.manifest_evidence, label, bench.name)
            commands = {"CP": [str(args.python), "-B", str(bench.path)],
                        "A": [str(paths["A"])], "B": [str(paths["B"])]}
            oracle = checked(commands["CP"])
            expected = (oracle.stdout, oracle.stderr)
            samples = {label: [] for label in commands}
            # 构建两臂后才计时；下一项构建必须等本项计时和验证结束。
            orders = execution_orders(args.runs, args.balanced_order)
            for index, order in enumerate(orders):
                for label in order:
                    result = checked(commands[label], expected, tag=f"sample-{index}-{label}")
                    if index:
                        samples[label].append({"ms": result.wall_ms, "rss": result.max_rss_bytes})
            stats = {}
            for label in ("A", "B"):
                result = checked(commands[label], expected, "RYUPY_STATS", tag="stats-"+label)
                if not {"main_ms", "alloc_bytes"} <= result.stats.keys():
                    raise RuntimeError((bench.name, label, "统计字段缺失"))
                stats[label] = result.stats
                dist = paths[label].parent
                moved = case_root / "中文 空格搬迁" / label
                shutil.copytree(dist, moved)
                dist.rename(dist.with_name(dist.name + ".hidden"))
                if args.raw_evidence:
                    assert hashlib.sha256((moved/paths[label].name).read_bytes()).hexdigest() == identities['arms'][label]['executable_sha256']
                result = measure.run_once([str(moved / paths[label].name)], env | {"PATH": "/nonexistent"}, moved, None)
                archive("moved-"+label, [str(moved / paths[label].name)], result, moved)
                if (result.exit_code, result.stdout, result.stderr) != (0, *expected):
                    raise RuntimeError((bench.name, label, "搬迁失败", result.stderr))
            if args.raw_evidence:
                assert bench.path.read_bytes() == source_bytes, "测量期间输入改变"
                for label, cli in (("A", args.old),("B",args.new)):
                    assert hashlib.sha256(cli.read_bytes()).hexdigest() == identities['arms'][label]['cli_sha256']
                (folder/'complete.txt').write_text('COMPLETE\n')
            rows.append({"name": bench.name, "samples": samples, "stats": stats, "free_bytes_before_build": disk, "execution_order": orders[1:], "build_order": ["A", "B"]})
            args.output.write_text(json.dumps({"old": str(args.old), "new": str(args.new),
                "python": str(args.python), "runs": args.runs, "warmup": 1, "rows": rows}, indent=2))
            medians = {k: round(statistics.median(v["ms"] for v in values), 3) for k, values in samples.items()}
            print(bench.name, medians, flush=True)
            # 样本JSON已写盘、两臂清单已复制、统计和搬迁通过后才清理本项。
            if args.cleanup_completed:
                cleanup_case(case_root, root)
    print(f"ALL {len(rows)}通过，临时构建根已清理", flush=True)


if __name__ == "__main__":
    main()
