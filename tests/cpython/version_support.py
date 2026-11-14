"""自动版本验收支持：真实venv/安装包、严格子进程、同次oracle与字节级副作用比较。"""
from __future__ import annotations
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from onefile_support import ROOT, build_fixture_wheel, external_workspace, make_clean_env, make_polluted_env

# 固定回归版本选择，绝不称为各Python版本的最高NumPy版本；uv根据真实wheel标签校验安装。
NUMPY_VERSION_MAP = {9: "1.26.4", 10: "2.2.3", 11: "2.3.5", 12: "2.3.5", 13: "2.3.5", 14: "2.5.4", 15: "2.5.4"}


def run(command, *, cwd: Path, env: dict, timeout=900, check=True):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env, capture_output=True, timeout=timeout)
    if check:
        assert result.returncode == 0, (command, result.returncode, result.stdout.decode(errors="replace"), result.stderr.decode(errors="replace"))
    return result


def venv_python(directory: Path) -> Path:
    return directory / ("Scripts/python.exe" if sys.platform == "win32" else "bin/python")


def activated(directory: Path, env: dict) -> dict:
    updated = {key: value for key, value in env.items() if key not in ("VIRTUAL_ENV", "CONDA_PREFIX")}
    updated["VIRTUAL_ENV"] = str(directory)
    updated["PATH"] = str(venv_python(directory).parent) + os.pathsep + env.get("PATH", "")
    return updated


def create_venv(python: Path, directory: Path, workspace: Path, env: dict) -> Path:
    run(["uv", "venv", "--python", python, directory], cwd=workspace, env=env)
    return venv_python(directory)


def install(python: Path, requirements: list, workspace: Path, env: dict):
    run(["uv", "pip", "install", "--python", python, "--only-binary=:all:", *requirements], cwd=workspace, env=env)


def assert_result(actual, expected):
    assert actual.returncode == expected.returncode, (actual.returncode, expected.returncode, actual.stderr)
    assert actual.stdout == expected.stdout, (actual.stdout, expected.stdout)
    assert actual.stderr == expected.stderr, (actual.stderr, expected.stderr)


def run_priority_matrix(cli: Path, python: Path, workspace: Path, env: dict):
    """三个真实venv安装不同包数据；实际编译运行，验证项目、激活与PATH选择顺序。"""
    project = workspace / "priority-project"
    project.mkdir()
    entry = project / "priority.py"
    entry.write_text("import priority_pkg\nprint(priority_pkg.VALUE)\n", encoding="utf-8")
    environments = {"project": project / ".venv", "active": workspace / "priority-active", "path": workspace / "priority-path"}
    for name, directory in environments.items():
        interpreter = create_venv(python, directory, workspace, env)
        wheel_dir = workspace / ("wheel-" + name)
        wheel_dir.mkdir()
        wheel = build_fixture_wheel(wheel_dir, "priority_pkg", "1.0.0", {"priority_pkg/__init__.py": f"VALUE = {name!r}\n"})
        install(interpreter, ["packaging==25.0", wheel], workspace, env)
    selected = activated(environments["active"], env)
    selected["PATH"] = str(venv_python(environments["path"]).parent) + os.pathsep + env.get("PATH", "")
    for name in ["project", "active", "path"]:
        if name == "active":
            shutil.rmtree(environments["project"])
        if name == "path":
            selected.pop("VIRTUAL_ENV", None)
        output = workspace / ("priority-output-" + name)
        run([cli, "--cpython-provider", "auto", "build", entry, "--out-dir", output], cwd=project, env=selected)
        expected = run([venv_python(environments[name]), "-I", "-X", "utf8", entry], cwd=workspace, env=env)
        actual = run([shutil.which("dotnet"), output / "priority.dll"], cwd=workspace, env=env)
        assert_result(actual, expected)
        assert actual.stdout == (name + os.linesep).encode()
        shutil.rmtree(output)
    print("[version] 三个真实venv的项目/激活/PATH选择与产品运行均通过", flush=True)
