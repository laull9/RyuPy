#!/usr/bin/env python3
"""CPython 3.12.13 严格零字节码内核原型的锚点数据结构与执行引擎。

本文件负责定义不可变锚点模型 `AnchorEdit` / `NewHeaderFile` 以及原子校验执行器 `apply_anchors`：
1. 不生成、不保留任何 `.patch` / `.diff` 文件。
2. 对全部目标文件先执行只读加载与精确出现次数校验（必须 `count == 1`），任何版本或上下文
   失配在改写磁盘之前立即抛出 `AnchorTransformError` 硬失败。
3. 具体的 CPython 3.12.13 锚点规则分层存放在 `anchors_eval_import.py` 与
   `anchors_frozen_build.py` 中，便于多版本扩展时按版本独立参数化并分别审计。
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


TARGET_VERSION = "3.12.13"


class AnchorTransformError(RuntimeError):
    """锚点校验、版本锁定或源码变换失败时抛出的硬错误。"""


@dataclass(frozen=True)
class AnchorEdit:
    """单条精确锚点替换规则；`old` 必须在目标文件中精确出现 1 次。"""

    rel_path: str
    name: str
    old: str
    new: str


@dataclass(frozen=True)
class NewHeaderFile:
    """需要注入到源码树中的新增头文件。"""

    rel_path: str
    content: str


def build_anchors() -> list[AnchorEdit]:
    """汇总 CPython 3.12.13 严格零字节码内核所需的全部精确锚点。"""
    from anchors_eval_import import build_eval_import_anchors
    from anchors_frozen_build import build_frozen_build_anchors
    from anchors_guards import build_guard_anchors
    from anchors_import_router import build_import_router_anchors
    from anchors_stdio_codecs import build_stdio_codecs_anchors

    return (
        build_eval_import_anchors()
        + build_frozen_build_anchors()
        + build_guard_anchors()
        + build_stdio_codecs_anchors()
        + build_import_router_anchors()
    )


def default_headers() -> list[NewHeaderFile]:
    """返回需要写入 CPython 源码树的新增头文件清单。"""
    from anchors_eval_import import RYUPY_KERNEL_HEADER
    from anchors_import_router import build_import_router_headers
    from anchors_stdio_codecs import RYUPY_UTF8_CODEC_HEADER, RYUPY_UTF8_INCREMENTAL_HEADER

    return [
        RYUPY_KERNEL_HEADER,
        RYUPY_UTF8_CODEC_HEADER,
        RYUPY_UTF8_INCREMENTAL_HEADER,
        *build_import_router_headers(),
    ]


def apply_anchors(
    source_root: Path,
    anchors: list[AnchorEdit] | None = None,
    headers: list[NewHeaderFile] | None = None,
    *,
    dry_run: bool = False,
) -> dict[str, int]:
    """对 `source_root` 执行严格单次匹配的锚点变换；任何失配在写入前直接失败。

    返回每个目标相对路径应用的锚点计数，便于日志审计。
    """
    edits = build_anchors() if anchors is None else anchors
    new_headers = default_headers() if headers is None else headers
    if not source_root.is_dir():
        raise AnchorTransformError(f"源码目录不存在：{source_root}")

    # 第一遍：只读全量加载并逐条验证 count == 1，防止部分文件已写入后才发现失配。
    staged: dict[Path, str] = {}
    per_file_counts: dict[str, int] = {}
    for edit in edits:
        target = source_root / edit.rel_path
        if not target.is_file():
            raise AnchorTransformError(
                f"锚点 [{edit.name}] 目标文件缺失：{edit.rel_path}"
            )
        if target not in staged:
            staged[target] = target.read_text(encoding="utf-8")
        current = staged[target]
        hits = current.count(edit.old)
        if hits != 1:
            raise AnchorTransformError(
                f"锚点 [{edit.name}] 匹配失败（{edit.rel_path}）：期望出现 1 次，实际 {hits} 次"
            )
        staged[target] = current.replace(edit.old, edit.new, 1)
        per_file_counts[edit.rel_path] = per_file_counts.get(edit.rel_path, 0) + 1

    for hdr in new_headers:
        hdr_path = source_root / hdr.rel_path
        if hdr_path.exists():
            raise AnchorTransformError(f"注入头文件已存在，拒绝覆盖：{hdr.rel_path}")

    if dry_run:
        return per_file_counts

    for target, updated in staged.items():
        target.write_text(updated, encoding="utf-8")
    for hdr in new_headers:
        hdr_path = source_root / hdr.rel_path
        hdr_path.parent.mkdir(parents=True, exist_ok=True)
        hdr_path.write_text(hdr.content, encoding="utf-8")
    return per_file_counts


def main() -> int:
    """命令行入口：支持独立对已解压的官方 CPython 3.12.13 源码树执行校验或变换。"""
    import argparse

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source_root", type=Path, help="官方 CPython 3.12.13 解压目录")
    parser.add_argument("--dry-run", action="store_true", help="仅验证锚点匹配，不改写文件")
    args = parser.parse_args()
    counts = apply_anchors(args.source_root, dry_run=args.dry_run)
    mode = "dry-run 预检通过" if args.dry_run else "锚点变换完成"
    print(f"{mode}（目标 CPython {TARGET_VERSION}）：{counts}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
