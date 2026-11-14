# -*- coding: utf-8 -*-
"""CPython Provider 20个固定项目的验收支持库。

功能范围：
1. 解析 tests/projects/manifest.tsv 与 lock.tsv 规范；
2. 构建纯净隔离的 CPython 私有分发目录（排除宿主机 pip 携带的 Windows .exe 杂质）；
3. 部署被测项目及其传递依赖载荷（包括 markdown 的 egg-info entry points）；
4. 为主代理依赖闭包提供清晰接入点（register_dependency_resolver）；
5. 实施独立 cwd 与同次 CPython 3.12.13 Oracle 对照执行与搬迁审计。
"""

import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field
from typing import Callable, Dict, List, Optional, Tuple

_REPO_ROOT = Path(__file__).resolve().parents[2]
if str(_REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPO_ROOT))

# 引用共用 support 中的环境与路径工具
from tests.cpython.support import (
    ROOT,
    VERSION,
    require_python,
    environment,
    audit_native_paths,
    inspect_host_report,
)

PROJECTS_DIR = ROOT / "third_party" / "projects"
MANIFEST_FILE = ROOT / "tests" / "projects" / "manifest.tsv"
LOCK_FILE = ROOT / "tests" / "projects" / "lock.tsv"

# 默认缓存与外部 CPython 路径
DEFAULT_CPYTHON = Path(os.path.expanduser("~/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none"))
DEFAULT_METADATA = Path(os.path.expanduser("~/.cache/ryupy/cpython-provider/pbs-20260623-metadata/python"))
DEFAULT_NUMPY = Path(os.path.expanduser("~/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64"))
DEFAULT_RYUPY_CLI = ROOT / "target" / "debug" / "ryupy"


@dataclass
class ProjectSpec:
    """固定项目元数据定义。"""
    project_id: str
    category: str
    pypi_name: str
    version: str
    import_name: str
    entry_rel: str
    test_scope: str
    deps_raw: str
    entry_abs: Path
    source_dir: Path
    direct_dependencies: List[str] = field(default_factory=list)


# 主代理自动依赖闭包扩展接入点
_dependency_resolver_hook: Optional[Callable[[ProjectSpec], List[Tuple[str, Path]]]] = None


def register_dependency_resolver(hook: Callable[[ProjectSpec], List[Tuple[str, Path]]]):
    """供主代理或依赖生成模块接入通用依赖解析闭包。"""
    global _dependency_resolver_hook
    _dependency_resolver_hook = hook


def load_manifest(manifest_path: Optional[Path] = None) -> List[ProjectSpec]:
    """解析 tests/projects/manifest.tsv 并返回 ProjectSpec 列表。"""
    path = manifest_path or MANIFEST_FILE
    assert path.is_file(), f"找不到清单文件: {path}"
    projects = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            parts = line.split("\t")
            if len(parts) < 8:
                continue
            pid, cat, pypi, ver, imp_name, entry_rel, scope, deps = parts[:8]
            if pid == "id":
                continue
            entry_abs = ROOT / "tests" / "projects" / entry_rel
            source_dir = PROJECTS_DIR / f"{pid}-{ver}"

            # 解析显式声明的直接依赖
            deps_list = []
            if deps and deps != "-":
                for item in deps.split(","):
                    item = item.strip()
                    if item:
                        dep_name = item.split("==")[0].strip()
                        deps_list.append(dep_name)

            spec = ProjectSpec(
                project_id=pid,
                category=cat,
                pypi_name=pypi,
                version=ver,
                import_name=imp_name,
                entry_rel=entry_rel,
                test_scope=scope,
                deps_raw=deps,
                entry_abs=entry_abs,
                source_dir=source_dir,
                direct_dependencies=deps_list,
            )
            projects.append(spec)
    expected_ids = [
        "six", "sortedcontainers", "toml", "tomli", "pyparsing",
        "attrs", "networkx", "python-dateutil", "click", "docopt",
        "pyfiglet", "tabulate", "texttable", "markdown", "chardet",
        "wcwidth", "colorama", "termcolor", "binaryornot", "progress",
    ]
    actual_ids = [p.project_id for p in projects]
    assert len(projects) == 20, f"项目数必须恰好为20项，实际读取: {len(projects)}"
    assert actual_ids == expected_ids, f"项目ID顺序或内容不匹配，实际: {actual_ids}"
    locked = {}
    for row in LOCK_FILE.read_text(encoding='utf-8').splitlines():
        if row and not row.startswith('#'):
            fields = row.split('\t')
            locked[fields[0].lower().replace('_', '-')] = fields[1]
    for project in projects:
        assert locked[project.pypi_name.lower().replace('_', '-')] == project.version, project.project_id
    return projects


def get_project_by_id(project_id: str, manifest_path: Optional[Path] = None) -> ProjectSpec:
    """按项目ID查询 ProjectSpec。"""
    all_projects = load_manifest(manifest_path)
    for p in all_projects:
        if p.project_id == project_id:
            return p
    raise KeyError(f"未在 manifest 中找到项目: {project_id}")


def prepare_clean_distribution(
    target_dir: Path,
    base_cpython: Optional[Path] = None,
    metadata_dir: Optional[Path] = None,
) -> Path:
    """构建用于测试的纯净私有 CPython 发行目录。

    关键措施：清除 site-packages 内部宿主机安装的杂质（如 Windows .exe），
    避免 driver 原生架构扫描器误报架构不匹配。
    """
    base = base_cpython or DEFAULT_CPYTHON
    assert base.is_dir(), f"基础 CPython 不存在: {base}"
    require_python(base / "bin/python3.12", target_dir.parent, environment())
    if target_dir.exists():
        shutil.rmtree(target_dir)

    # 复制基础发行
    shutil.copytree(base, target_dir, symlinks=False, ignore=shutil.ignore_patterns("__pycache__", "*.pyc"))

    # 补充完整 PBS 元数据与第三方协议
    meta = metadata_dir or DEFAULT_METADATA
    if meta.is_dir():
        py_json = meta / "PYTHON.json"
        if py_json.is_file():
            shutil.copy2(py_json, target_dir / "PYTHON.json")
        licenses_dir = meta / "licenses"
        if licenses_dir.is_dir():
            shutil.copytree(licenses_dir, target_dir / "licenses", dirs_exist_ok=True)

    # 清空 site-packages 中的非标准包，保证输入干净
    site_packages = target_dir / "lib" / "python3.12" / "site-packages"
    if site_packages.is_dir():
        shutil.rmtree(site_packages)
    site_packages.mkdir(parents=True, exist_ok=True)
    return target_dir


def resolve_source_root(base_dir: Path) -> Path:
    """穿透第三库解压目录中可能存在的一层同名嵌套目录。"""
    if not base_dir.is_dir():
        return base_dir
    subdirs = [d for d in base_dir.iterdir() if d.is_dir() and not d.name.startswith(".")]
    if len(subdirs) == 1 and not any(f.is_file() for f in base_dir.iterdir()):
        return subdirs[0]
    return base_dir


def locate_installed_module(module_name: str) -> Optional[Path]:
    """仅从固定清单版本选择依赖，禁止遍历任意版本目录作为回退。"""
    for project in load_manifest():
        if module_name not in (project.import_name, 'attr' if project.project_id == 'attrs' else ''):
            continue
        root = resolve_source_root(project.source_dir)
        for candidate in (root / module_name, root / f'{module_name}.py', root / 'src' / module_name):
            if candidate.is_file() or (candidate.is_dir() and (candidate / '__init__.py').is_file()):
                return candidate
    return None


def collect_project_payloads(project: ProjectSpec) -> List[Tuple[str, Path]]:
    """收集被测项目及其传递依赖所需的全部 payload（名称, 源路径）。"""
    if _dependency_resolver_hook is not None:
        return _dependency_resolver_hook(project)

    payloads: List[Tuple[str, Path]] = []

    # 1. 查找项目本身的主包（穿透一层嵌套）
    src_dir = resolve_source_root(project.source_dir)
    main_target = None
    if (src_dir / project.import_name).is_dir():
        main_target = src_dir / project.import_name
    elif (src_dir / f"{project.import_name}.py").is_file():
        main_target = src_dir / f"{project.import_name}.py"
    elif (src_dir / "src" / project.import_name).is_dir():
        main_target = src_dir / "src" / project.import_name
    elif (src_dir / f"{project.project_id}.py").is_file():
        main_target = src_dir / f"{project.project_id}.py"

    if main_target is not None:
        payloads.append((project.import_name, main_target))
    else:
        global_found = locate_installed_module(project.import_name)
        if global_found:
            payloads.append((project.import_name, global_found))

    # 2. 项目特殊关联资源与 egg-info
    if project.project_id == "markdown":
        egg_info = src_dir / "Markdown.egg-info"
        if egg_info.is_dir():
            payloads.append(("Markdown-3.5.2.egg-info", egg_info))

    # 3. 补充显式关联的第三方依赖
    dep_mapping = {
        "attrs": ["attr"],
        "python-dateutil": ["six"],
        "tabulate": ["wcwidth"],
        "texttable": ["wcwidth"],
        "binaryornot": ["chardet"],
        "click": ["colorama"],
    }
    required_deps = list(project.direct_dependencies)
    for extra in dep_mapping.get(project.project_id, []):
        if extra not in required_deps:
            required_deps.append(extra)

    for dep in required_deps:
        dep_path = locate_installed_module(dep)
        if dep_path:
            payloads.append((dep, dep_path))

    return payloads


def deploy_payloads_to_distribution(payloads: List[Tuple[str, Path]], dist_dir: Path):
    """将收集到的 payloads 安装到分发的 site-packages。"""
    site_packages = dist_dir / "lib" / "python3.12" / "site-packages"
    site_packages.mkdir(parents=True, exist_ok=True)
    for name, src_path in payloads:
        dest = site_packages / name
        if dest.exists():
            if dest.is_dir():
                shutil.rmtree(dest)
            else:
                dest.unlink()
        if src_path.is_dir():
            shutil.copytree(src_path, dest, symlinks=True, ignore=shutil.ignore_patterns("__pycache__", "*.pyc"))
        else:
            shutil.copy2(src_path, dest)


def create_declaration_file(
    output_path: Path,
    dist_dir: Path,
    host_report_path: Path,
    payloads: List[Tuple[str, Path]],
) -> Path:
    """生成给 ryupy 编译器使用的标准 provider declaration.json。"""
    payload_defs = []
    for name, path in payloads:
        dest_name = path.name if path.is_file() else name
        payload_defs.append({
            "source": str(path.resolve()),
            "destination": dest_name,
        })
    modules = [p[0] for p in payloads if p[0].isidentifier()]
    from tests.cpython.support import declaration as make_declaration, write_json
    decl = make_declaration(
        distribution=dist_dir,
        payloads=payload_defs,
        modules=modules,
    )
    write_json(output_path, decl)
    return output_path


def run_oracle_process(
    project: ProjectSpec,
    work_dir: Path,
    cpython_bin: Optional[Path] = None,
    extra_env: Optional[Dict[str, str]] = None,
) -> Tuple[int, str, str]:
    """使用固定 CPython 3.12.13 在独立隔离的 work_dir 中运行 entry 生成 Oracle 输出。"""
    cpython = cpython_bin or (DEFAULT_CPYTHON / "bin" / "python3")
    assert cpython.is_file(), f"CPython 执行文件不存在: {cpython}"

    env = environment()
    if extra_env:
        env.update(extra_env)

    resolved_src = resolve_source_root(project.source_dir)
    pythonpaths = [str(resolved_src.resolve())]
    if (resolved_src / "src").is_dir():
        pythonpaths.append(str((resolved_src / "src").resolve()))
    for dep_name, dep_path in collect_project_payloads(project):
        pythonpaths.append(str(dep_path.parent.resolve()))
    env["PYTHONPATH"] = os.pathsep.join(pythonpaths)

    cmd = [str(cpython), "-X", "utf8", str(project.entry_abs.resolve())]
    proc = subprocess.run(cmd, cwd=str(work_dir), env=env, capture_output=True, timeout=120)
    stdout = proc.stdout.decode("utf-8")
    stderr = proc.stderr.decode("utf-8")
    return proc.returncode, stdout, stderr


def run_ryupy_process(project: ProjectSpec, work_dir: Path, dist_dir: Path,
                      mode: str = 'jit', ryupy_cli: Optional[Path] = None,
                      extra_env: Optional[Dict[str, str]] = None) -> Tuple[int, str, str, Dict[str, str]]:
    """构建使用私有输入，执行使用独立cwd；搬迁臂删除私有源码、基础发行及构建目录。"""
    cli = ryupy_cli or DEFAULT_RYUPY_CLI
    assert cli.is_file(), f'RyuPy CLI不存在：{cli}'
    env = environment()
    if extra_env:
        env.update(extra_env)
    inputs = work_dir / '构建 输入'
    inputs.mkdir()
    entry = inputs / project.entry_abs.name
    shutil.copy2(project.entry_abs, entry)
    payloads = []
    for index, (name, source) in enumerate(collect_project_payloads(project)):
        private = inputs / 'payloads' / str(index) / source.name
        private.parent.mkdir(parents=True)
        if source.is_dir():
            shutil.copytree(source, private, symlinks=False, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
        else:
            shutil.copy2(source, private)
        payloads.append((name, private))
    declaration = inputs / 'provider.json'
    create_declaration_file(declaration, dist_dir, work_dir / 'host_report.json', payloads)
    build_dir = work_dir / 'publish'
    command = [cli, '--cpython-provider', declaration, 'build', entry, '--out-dir', build_dir]
    if mode != 'jit':
        command.append('--aot')
    build = subprocess.run(list(map(str, command)), cwd=work_dir, env=env, capture_output=True, timeout=600)
    if build.returncode:
        return build.returncode, build.stdout.decode('utf-8'), '[BUILD FAILED]\n' + build.stderr.decode('utf-8'), {}
    configs = list(build_dir.rglob('ryupy-cpython.json'))
    if mode != 'jit':
        configs = [path for path in configs if path.parent.name.endswith('.dist')]
    assert len(configs) == 1, configs
    product = configs[0].parent
    suffix = '.dll' if mode == 'jit' else ('.exe' if sys.platform == 'win32' else '')
    executable = product / (entry.stem + suffix)
    assert executable.is_file(), executable
    if mode == 'reloc':
        relocated = work_dir / '搬迁 目录 & 中文'
        shutil.move(product, relocated)
        executable = relocated / executable.name
        product = relocated
        for path in (inputs, build_dir, dist_dir):
            if path.exists():
                shutil.rmtree(path)
            assert not path.exists(), path
    audit_native_paths(product)
    runtime_env = environment()
    runtime_env.update(PYTHONHOME='/不可访问 Python', PYTHONPATH='/不可访问 依赖')
    host_report = work_dir / 'host_report.json'
    runtime_env['RYUPY_CPYTHON_REPORT'] = str(host_report)
    # AOT直接执行；JIT显式使用开发机dotnet，保持两种后端边界。
    run_command = [str(executable)]
    if mode == 'jit':
        run_command.insert(0, shutil.which('dotnet'))
    else:
        empty_path = work_dir / 'empty-path'
        empty_path.mkdir()
        runtime_env['PATH'] = str(empty_path)
    run_cwd = work_dir / 'run'
    run_cwd.mkdir()
    actual = subprocess.run(run_command, cwd=run_cwd, env=runtime_env, capture_output=True, timeout=180)
    if actual.returncode == 0:
        inspect_host_report(host_report, product)
    aliases = {str(source.resolve()): '<site-packages>/' + (source.name if source.is_file() else name)
               for name, source in collect_project_payloads(project)}
    aliases[str(product / 'python/lib/python3.12/site-packages')] = '<site-packages>'
    return actual.returncode, actual.stdout.decode('utf-8'), actual.stderr.decode('utf-8'), aliases
