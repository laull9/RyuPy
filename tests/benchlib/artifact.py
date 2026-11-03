"""只接受本次独占输出根的NativeAOT发布；核清单/输入/原生头，不执行产物或回退dotnet。"""
import hashlib
import json
import os
from pathlib import Path
import platform
import sys

# 复用已独立验收的清单协议，避免bench再实现一套较弱校验。
AOT_TOOLS = Path(__file__).resolve().parents[1] / "aot"
if str(AOT_TOOLS) not in sys.path:
    sys.path.insert(0, str(AOT_TOOLS))
from manifest_files import check_artifacts, digest, plain_stat  # noqa: E402
from aot_layout import reported_aot  # noqa: E402
from manifest_verify import (MANIFEST_NAME, image_header, no_duplicate_keys,
    read_sdk_version, require, validate_schema)  # noqa: E402


def host_rid():
    """原runner依赖POSIX wait4；不因为识别PE就宣称Windows测量可用。"""
    value = (sys.platform, platform.machine().lower())
    if value in (("linux", "x86_64"), ("linux", "amd64")):
        return "linux-x64"
    if value == ("darwin", "arm64"):
        return "osx-arm64"
    raise ValueError("AOT bench当前只验收Linux x64和macOS arm64的POSIX runner")


def verified_native(root, bench, stderr):
    root = Path(root).absolute()
    rid = host_rid()
    # 复用同次报告/目录身份核验，兼容宿主规范化路径但不接受旧attempt。
    try:
        layout = reported_aot(stderr.encode("utf-8"), root, bench.name, rid)
    except AssertionError as exc:
        raise ValueError("CLI本次AOT布局不匹配") from exc
    managed, native, manifest, dist = layout.managed, layout.native, layout.manifest, layout.dist
    limit = 4 * 1024 * 1024
    require(plain_stat(manifest).st_size <= limit, "清单过大")
    with manifest.open("rb") as stream:
        raw = stream.read(limit + 1)
    require(len(raw) <= limit, "读取中清单增长")
    data = json.loads(raw, object_pairs_hook=no_duplicate_keys)
    sdk = read_sdk_version(layout.host / "sdk.version")
    # 原基准没有native-binding声明；不将其它schema假当普通AOT清单。
    validate_schema(data, rid, sdk)
    actual = check_artifacts(dist, data["artifacts"], MANIFEST_NAME)
    entry = next(x for x in data["artifacts"] if x["role"] == "native_executable")
    require(entry["relative_path"] == bench.name and os.path.samefile(actual[bench.name], native), "非预期唯一native入口")
    image_header(native, rid)
    require(os.access(native, os.X_OK), "native缺少执行权限")
    inputs = {x["role"]: x for x in data["build_inputs"]}
    for role, path in (("compiled_managed_input", managed), ("runtime_managed_input", managed.parent / "RyuPy.Runtime.dll")):
        size, hashed = digest(path)
        require((size, hashed) == (inputs[role]["size"], inputs[role]["sha256"]), "本次托管输入hash不匹配")
    source = next((x for x in data["sources"] if x["module_name"] == "__main__"), None)
    require(source is not None, "清单缺入口源码")
    require(source["compiled_text_sha256"] == hashlib.sha256(bench.path.read_bytes()).hexdigest(), "入口源码hash不匹配")
    return native
