"""项目门禁合同：冻结输入、原源核验、完整结果校验与按 ID 的两级判定。

这些校验防止误用、陈旧与缺项，不是防同权限恶意操作者伪造证据的安全边界。
不维护第二份项目清单；版本/入口/依赖继续由现有 manifest 解析器提供。
"""

import hashlib
import json
from pathlib import Path, PurePosixPath
import tarfile
import zipfile

from run_projects_baseline import read_manifest, source_dirs


class GateError(ValueError):
    """输入或证据不满足合同，不能生成通过结论。"""


class IdentityError(GateError):
    """候选、工具或结果归属不可信：必须停止整轮，不当作项目局部失败。"""


def digest(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def load_inputs(root):
    contract = json.loads((root / "tests/projects/protected.json").read_text())
    if contract["schema"] != 1 or contract["mode"] != "default-jit":
        raise GateError("未知门禁合同或非默认模式")
    for name, expected in contract["inputs"].items():
        if digest(root / name) != expected:
            raise GateError(f"冻结输入改变：{name}；需要单独审阅，不可自动更新基线")
    rows = read_manifest(root / "tests/projects")
    ids = [row["id"] for row in rows]
    if len(ids) != 20 or len(set(ids)) != 20 or set(ids) != set(contract["project_ids"]):
        raise GateError("项目清单必须为固定20个不重复 ID")
    for key in ("protected_native", "promotion_requires"):
        values = contract[key]
        if not values or len(values) != len(set(values)) or not set(values) <= set(ids):
            raise GateError(f"非法保护集合：{key}")
    return contract, rows


def _archive_files(path):
    """只接受安全的普通文件；不解包，也不信任解压目录里的自报清单。"""
    result = {}

    def add(name, stream):
        pure = PurePosixPath(name)
        if pure.is_absolute() or ".." in pure.parts or str(pure) in result:
            raise GateError(f"归档存在非法/重复路径：{name}")
        result[str(pure)] = hashlib.file_digest(stream, "sha256").hexdigest()

    if path.suffix == ".zip":
        with zipfile.ZipFile(path) as archive:
            for member in archive.infolist():
                if member.is_dir():
                    continue
                if (member.external_attr >> 16) & 0o170000 == 0o120000:
                    raise GateError("归档含符号链接")
                with archive.open(member) as stream:
                    add(member.filename, stream)
    else:
        with tarfile.open(path) as archive:
            for member in archive:
                if member.isdir():
                    continue
                if not member.isfile():
                    raise GateError(f"归档含非普通文件：{member.name}")
                with archive.extractfile(member) as stream:
                    add(member.name, stream)
    if not result:
        raise GateError("空源码归档")
    return result


def audit_sources(root, store, rows):
    """锁定归档逐文件比对全部解压源，既不裁剪包，也不把 lock 中非项目包加入分母。"""
    lock = {}
    for line in (root / "tests/projects/lock.tsv").read_text().splitlines():
        if not line.strip() or line.startswith("#"):
            continue
        name, version, filename, sha, _url = line.split("\t")
        if (name, version) in lock:
            raise GateError("lock 含重复包")
        lock[name, version] = (filename, sha)
    packages = {(row["pypi"], row["version"]) for row in rows}
    for row in rows:
        if row["deps"] != "-":
            packages.update(tuple(dep.strip().split("==")) for dep in row["deps"].split(","))
    sources = {}
    for name, version in sorted(packages):
        filename, sha = lock[name, version]
        archive = store / "_dist" / filename
        if digest(archive) != sha:
            raise GateError(f"锁定归档 hash 不匹配：{filename}")
        expected = _archive_files(archive)
        directory = store / f"{name}-{version}"
        actual = {}
        for path in directory.rglob("*"):
            if path.is_symlink():
                raise GateError(f"源码内部含符号链接：{path}")
            if path.is_file():
                actual[path.relative_to(directory).as_posix()] = digest(path)
        if actual != expected:
            raise GateError(f"源码与锁定归档不一致（缺失/增加/修改）：{name}=={version}")
        sources[f"{name}=={version}"] = actual
    return sources, source_dirs(rows, store)


def validate_result(row, project, run_id, directory, golden, runtime_sha):
    """不只相信 native 标签：核对运行身份、退出码、完整原 stdout 和本轮产物。"""
    identity = {key: project[key] for key in ("id", "pypi", "version", "scope", "deps")}
    identity.update(run_id=run_id, mode="default-jit", entry=str(golden.parents[2] / project["entry"]))
    if any(row.get(key) != value for key, value in identity.items()):
        raise IdentityError(f"陈旧/错误身份或非默认报告：{project['id']}")
    status, kind = row.get("status"), row.get("failure_kind")
    allowed = {"native": {""}, "unsupported": {"diagnostic"}, "failed": {"runtime-error"}}
    if not isinstance(status, str) or not isinstance(kind, str) or status not in allowed or kind not in allowed[status]:
        raise GateError(f"非能力阻塞（运行器/崩溃/输出错误）：{project['id']} {status}/{kind}")
    diagnostics = (directory / "compile.diagnostics.txt").read_bytes()
    if status == "unsupported":
        if not diagnostics or not (directory / "compile.error.txt").is_file() or row.get("run_exit") is not None:
            raise GateError("编译未支持缺少完整错误证据")
        if (directory / "jit.stdout").exists():
            raise GateError("编译失败却混入运行结果")
        return
    if (directory / "compile.error.txt").exists():
        raise GateError("运行标签与编译错误矛盾")
    stem = Path(project["entry"]).stem
    for artifact in (f"{stem}.dll", f"{stem}.runtimeconfig.json"):
        if not (directory / artifact).is_file() or not (directory / artifact).stat().st_size:
            raise GateError("缺少本轮编译产物")
    if digest(directory / "RyuPy.Runtime.dll") != runtime_sha:
        raise IdentityError("执行产物的 runtime 与本轮冻结工具不符")
    stdout = (directory / "jit.stdout").read_bytes()
    stderr = (directory / "jit.stderr").read_bytes()
    if status == "native" and (type(row.get("run_exit")) is not int or row["run_exit"] != 0 or stdout != golden.read_bytes()):
        raise GateError("native 标签与退出码/原 stdout 不一致")
    if status == "failed" and (type(row.get("run_exit")) is not int or row["run_exit"] <= 0
                               or b"Traceback (most recent call last):" not in stderr):
        raise GateError("运行失败缺少正常 Python 错误证据")


def evaluate(contract, rows):
    ids = [row["id"] for row in rows]
    if len(ids) != 20 or len(set(ids)) != 20 or set(ids) != set(contract["project_ids"]):
        raise GateError("拒绝重复/未知/缺失 ID 或半矩阵")
    native = {row["id"] for row in rows if row["status"] == "native"}
    lost = sorted(set(contract["protected_native"]) - native)
    blockers = sorted(set(contract["promotion_requires"]) - native)
    return {"mode": "default-jit", "denominator": 20, "native": len(native),
            "protected_lost": lost, "historical_regressions_open": blockers,
            "additional_native": sorted(native - set(contract["protected_native"])),
            "preserve_pass": not lost, "promotion_pass": not lost and not blockers,
            "aot": "not-evaluated", "explicit_configurations": "not-evaluated"}
