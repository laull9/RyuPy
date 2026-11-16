"""NativeAOT 发布产物识别与原生入口验证。

严格限制于本次运行独占的 root 目录内，按预期模式（standalone 或 onefile）唯一定位真实普通原生入口。
严禁接受 root 外路径、严禁跟随符号链接逃逸、严禁跨 mode 混淆。
遵循 ADR-0182，跳过所有普通文件与源码哈希计算；核验目标平台二进制头（Mach-O / ELF）与执行权限。
宿主环境限定为 POSIX 平台（Linux x64 与 macOS arm64），因后续 runner 依赖 os.wait4。
"""
from __future__ import annotations

import json
import os
from pathlib import Path
import platform
import sys

AOT_TOOLS = Path(__file__).resolve().parents[1] / "aot"
if str(AOT_TOOLS) not in sys.path:
    sys.path.insert(0, str(AOT_TOOLS))
from manifest_verify import MANIFEST_NAME, image_header, require  # noqa: E402


def host_rid() -> str:
    """返回当前宿主平台 RID（仅支持 POSIX 环境：Linux x64 与 macOS arm64）。"""
    value = (sys.platform, platform.machine().lower())
    if value in (("linux", "x86_64"), ("linux", "amd64")):
        return "linux-x64"
    if value == ("darwin", "arm64"):
        return "osx-arm64"
    raise ValueError("AOT bench当前仅验收Linux x64和macOS arm64的POSIX runner（依赖os.wait4）")


def _is_safe_regular_file(path: Path, root: Path) -> bool:
    """验证目标是位于 root 内且非符号链接的真实普通文件。"""
    try:
        # 必须位于 root 内部，禁止向上逃逸
        path.relative_to(root)
    except ValueError:
        return False
    # 同时拒绝祖先链接和 ..：只检查入口本身会接受链接目录及词法逃逸。
    relative = path.relative_to(root)
    if ".." in relative.parts:
        return False
    current = root
    for part in relative.parts:
        current = current / part
        if current.is_symlink():
            return False
    # 必须是真实普通文件
    if not path.is_file():
        return False
    return True


def _find_candidate_from_log(stderr: str, root: Path, stem: str, mode: str) -> Path | None:
    """从 CLI 输出日志中提取已生成原生入口路径，并严格校验 root 与 mode。"""
    expected_parent_name = f"{stem}.onefile" if mode == "onefile" else f"{stem}.dist"
    candidates = []
    for line in stderr.splitlines():
        line = line.strip()
        if line.startswith("已生成 "):
            raw = line[len("已生成 "):].strip()
            candidate = Path(raw)
            if not candidate.is_absolute():
                candidate = root / candidate
            # 严格规范化，但检查是否真实普通文件
            if candidate.name == stem and candidate.parent.name == expected_parent_name:
                if _is_safe_regular_file(candidate, root):
                    candidates.append(candidate)
    if len(candidates) == 1:
        return candidates[0]
    if len(candidates) > 1:
        raise ValueError(f"CLI 日志中发现多个歧义原生入口：{candidates}")
    return None


def _find_candidate_from_layout(root: Path, stem: str, mode: str) -> Path | None:
    """按预期 mode 在独占 root 下精确查找原生入口，拒绝跨 mode 混淆。"""
    if mode == "onefile":
        candidate = root / f"{stem}.onefile" / stem
    elif mode == "standalone":
        candidate = root / f"{stem}.dist" / stem
    else:
        raise ValueError(f"未知 AOT 发布 mode：{mode}")

    if _is_safe_regular_file(candidate, root):
        return candidate
    return None


def verified_native(
    root: Path | str,
    bench,
    stderr: str | bytes,
    *,
    mode: str = "standalone",
) -> Path:
    """识别并核验本次 NativeAOT 发布的唯一原生可执行文件入口。

    1. 强制在本次独占 root 内查找，拒绝逃逸与符号链接；
    2. 按预期 mode（standalone / onefile）严格隔离，拒绝跨形态混淆；
    3. 核验宿主平台二进制头（ELF / Mach-O）与执行权限；
    4. 依 ADR-0182 跳过普通文件哈希计算；若存在清单仅核验基础结构。
    """
    root_path = Path(root).resolve()
    if mode not in ("standalone", "onefile"):
        raise ValueError(f"未知 AOT 发布 mode：{mode}")
    rid = host_rid()
    log_text = stderr.decode("utf-8", errors="replace") if isinstance(stderr, bytes) else str(stderr)

    # 1. 优先从日志定位匹配 mode 的原生入口
    native = _find_candidate_from_log(log_text, root_path, bench.name, mode)

    # 2. 回退到严格的目录布局查找
    if native is None:
        native = _find_candidate_from_layout(root_path, bench.name, mode)

    if native is None:
        raise ValueError(f"未在独占根 {root_path} 中找到符合 {mode} 模式的原生入口：{bench.name}")

    # 3. 再次严格防逃逸与软链接断言
    if not _is_safe_regular_file(native, root_path):
        raise ValueError(f"原生入口非法（逃逸出独占根或为软链接）：{native}")

    # 4. 执行权限与平台二进制头核验
    require(os.access(native, os.X_OK), f"原生入口缺少执行权限：{native}")
    try:
        image_header(native, rid)
    except Exception as exc:
        raise ValueError(f"原生二进制头核验失败（非有效 {rid} 可执行文件）：{exc}") from exc

    # 5. 若伴随生成清单文件，核验清单基础结构（跳过文件哈希）
    manifest_path = native.parent / MANIFEST_NAME
    if manifest_path.is_file():
        try:
            data = json.loads(manifest_path.read_text(encoding="utf-8"))
            version = data.get("schema_version")
            require(version in (1, 2), f"未知清单 schema_version：{version}")
            artifacts = data.get("artifacts", [])
            entries = [x for x in artifacts if x.get("role") == "native_executable"]
            require(len(entries) == 1, "清单必须声明唯一 native_executable 原生入口")
            require(entries[0].get("relative_path") == bench.name, f"清单原生入口名称不匹配：期望 {bench.name}")
        except Exception as exc:
            raise ValueError(f"清单基础结构核验失败：{exc}") from exc

    return native
