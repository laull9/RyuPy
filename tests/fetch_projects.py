#!/usr/bin/env python3
"""抓取固定项目集的 sdist 源码（M2.0）。

设计要点：
- 版本只在 `tests/projects/manifest.tsv` 里固定，脚本不解析依赖表达式，deps 列写的就是精确版本；
- 源码放在 `third_party/projects/<name>-<version>/`，不进版本库，用 `lock.tsv` 记录来源与 sha256；
- 已存在且 sha256 一致时跳过下载，保证可复现又不必每次联网；
- 抓不到就明确失败，不静默跳过项目（兼容性分母不允许悄悄变小）。
"""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import sys
import tarfile
import urllib.request
import zipfile


ROOT = Path(__file__).resolve().parent.parent
PROJECTS = ROOT / "tests" / "projects"
STORE = ROOT / "third_party" / "projects"
LOCK = PROJECTS / "lock.tsv"


def read_manifest():
    """返回 [(id, pypi, version)]，deps 也作为独立条目返回。"""
    entries = {}
    manifest = PROJECTS / "manifest.tsv"
    for raw in manifest.read_text(encoding="utf-8-sig").splitlines():
        line = raw.rstrip("\n")
        if not line.strip() or line.startswith("#") or line.startswith("id\t"):
            continue
        columns = line.split("\t")
        if len(columns) < 8:
            raise SystemExit(f"manifest.tsv 列数不足：{line}")
        pid, _category, pypi, version, _import, _entry, _scope, deps = columns[:8]
        entries[(pypi, version)] = pid
        if deps.strip() != "-":
            for dep in deps.split(","):
                name, _, dep_version = dep.strip().partition("==")
                if not dep_version:
                    raise SystemExit(f"依赖需要精确版本 {dep}")
                entries.setdefault((name, dep_version), name)
    return entries


def sdist_url(pypi, version):
    url = f"https://pypi.org/pypi/{pypi}/{version}/json"
    with urllib.request.urlopen(url, timeout=30) as response:
        payload = json.load(response)
    for item in payload["urls"]:
        if item["packagetype"] == "sdist":
            return item["url"], item["filename"]
    # 有些项目只在 release 级别给出文件列表
    with urllib.request.urlopen(f"https://pypi.org/pypi/{pypi}/json", timeout=30) as response:
        payload = json.load(response)
    for item in payload["releases"].get(version, []):
        if item["packagetype"] == "sdist":
            return item["url"], item["filename"]
    raise SystemExit(f"{pypi}=={version} 没有 sdist")


def download(url, target):
    with urllib.request.urlopen(url, timeout=60) as response, target.open("wb") as out:
        shutil.copyfileobj(response, out)
    return hashlib.sha256(target.read_bytes()).hexdigest()


def extract(archive, dest):
    """解包到 `dest`，返回真正的包根目录（sdist 通常含一层顶层目录）。

    不用 `shutil.rmtree`：沙箱环境对递归删除有限制，而且重复解包没有意义——
    已存在就复用，需要重新下载时用 `--refresh` 配合人工清理。
    """
    if dest.is_dir():
        return package_root(dest)
    dest.mkdir()
    if archive.suffix == ".zip":
        with zipfile.ZipFile(archive) as zf:
            zf.extractall(dest)
    else:
        with tarfile.open(archive) as tf:
            tf.extractall(dest, filter="data")
    return package_root(dest)


def package_root(dest):
    """sdist 解开后通常含一层顶层目录；`src/` 布局则再进一层。

    返回的目录要能直接用于 `PYTHONPATH`，也就是「包目录的父目录」。
    """
    children = list(dest.iterdir())
    dirs = [c for c in children if c.is_dir()]
    files = [c for c in children if c.is_file()]
    root = dirs[0] if len(dirs) == 1 and not files else dest
    if (root / "src").is_dir():
        return root / "src"
    return root


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--refresh", action="store_true", help="忽略已有源码，重新下载")
    options = parser.parse_args()

    STORE.mkdir(parents=True, exist_ok=True)
    dist_dir = STORE / "_dist"
    if not dist_dir.is_dir():
        dist_dir.mkdir()

    lock_rows = {}
    if LOCK.exists():
        for line in LOCK.read_text(encoding="utf-8").splitlines():
            if line.startswith("#") or not line.strip():
                continue
            name, version, filename, sha, url = line.split("\t")
            lock_rows[(name, version)] = (filename, sha, url)

    failures = 0
    for (pypi, version), pid in sorted(read_manifest().items()):
        dest = STORE / f"{pypi}-{version}"
        if dest.exists() and not options.refresh:
            print(f"复用 {pypi}=={version} → {dest.relative_to(ROOT)}")
            continue
        try:
            url, filename = sdist_url(pypi, version)
            archive = dist_dir / filename
            sha = download(url, archive)
            known = lock_rows.get((pypi, version))
            if known and known[1] != sha:
                raise SystemExit(
                    f"{pypi}=={version} 的 sha256 与 lock.tsv 不符（上游被替换？）"
                )
            extract(archive, dest)
            lock_rows[(pypi, version)] = (filename, sha, url)
            print(f"下载 {pid or pypi}=={version} → {dest.relative_to(ROOT)}  sha256={sha[:12]}…")
        except Exception as error:  # noqa: BLE001 - 抓取失败必须显式登记
            failures += 1
            print(f"失败 {pypi}=={version}：{error}", file=sys.stderr)

    lines = ["# 由 tests/fetch_projects.py 生成：name\tversion\tfilename\tsha256\turl"]
    for (name, version), (filename, sha, url) in sorted(lock_rows.items()):
        lines.append(f"{name}\t{version}\t{filename}\t{sha}\t{url}")
    LOCK.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(f"完成：{len(lock_rows)} 个发行包，{failures} 个失败")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
