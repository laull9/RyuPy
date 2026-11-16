#!/usr/bin/env python3
"""真实Python版本的自动provider验收：同次oracle、JIT/AOT、删除输入搬迁、NumPy与干净容器。"""
from __future__ import annotations
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
from version_support import (ROOT, NUMPY_VERSION_MAP, activated, assert_result, create_venv,
                             external_workspace, install, make_clean_env, make_polluted_env,
                             run, run_priority_matrix)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ryupy", type=Path, default=ROOT / "target/debug" / ("ryupy.exe" if sys.platform == "win32" else "ryupy"))
    parser.add_argument("--python", type=Path, default=Path(sys.executable), help="测试准备解释器；不作为版本参数传给产品")
    parser.add_argument("--python-version", help="CI期望的major.minor，实际探测值必须匹配")
    parser.add_argument("--numpy-version", help="固定回归NumPy版本；缺失时按测试矩阵选择")
    parser.add_argument("--priority", action="store_true", help="额外执行三个真实venv的优先级验收")
    parser.add_argument("--linux-container", help="额外执行无Python/.NET/SDK目标")
    parser.add_argument("--keep", action="store_true")
    args = parser.parse_args()
    cli = args.ryupy.resolve()
    python = args.python.resolve()
    assert cli.is_file(), cli
    workspace = external_workspace("ryupy-versions-")
    env = make_clean_env()
    try:
        probe = run([python, "-I", "-c", "import json,sys; print(json.dumps(list(sys.version_info[:3])))"], cwd=workspace, env=env)
        version = json.loads(probe.stdout)
        tag = ".".join(map(str, version[:2]))
        assert version[0] == 3 and version[1] in NUMPY_VERSION_MAP, version
        if args.python_version:
            assert tag == args.python_version, (version, args.python_version)
        numpy = args.numpy_version or NUMPY_VERSION_MAP[version[1]]
        print(f"[version] 实际测试Python {version}，固定NumPy {numpy}", flush=True)
        if args.priority:
            run_priority_matrix(cli, python, workspace, env)
        inputs = workspace / "构建 输入"
        inputs.mkdir()
        project = inputs / "项目"
        project.mkdir()
        venv = project / ".venv"
        interpreter = create_venv(python, venv, workspace, env)
        install(interpreter, ["packaging==25.0", "numpy==" + numpy], workspace, env)
        entry = project / "版本应用.py"
        shutil.copyfile(ROOT / "tests/cpython/fixtures/version_fixtures/app_version_probe.py", entry)
        oracle_cwd = workspace / "oracle"
        oracle_cwd.mkdir()
        oracle = run([interpreter, "-I", "-X", "utf8", entry], cwd=oracle_cwd, env=env)
        expected_effect = (oracle_cwd / "版本 业务副作用.txt").read_bytes()
        assert json.loads(oracle.stdout)["python"] == version
        build_env = activated(venv, env)
        jit = workspace / "jit"
        run([cli, "--cpython-provider", "auto", "build", entry, "--out-dir", jit], cwd=project, env=build_env)
        jit_cwd = workspace / "run-jit"
        jit_cwd.mkdir()
        actual = run([shutil.which("dotnet"), jit / "版本应用.dll"], cwd=jit_cwd, env=env)
        assert_result(actual, oracle)
        assert (jit_cwd / "版本 业务副作用.txt").read_bytes() == expected_effect
        print(f"[version] Python {tag} 自动探测、JIT三流和副作用通过", flush=True)
        output = workspace / "aot"
        run([cli, "--cpython-provider", "auto", "build", entry, "--aot", "--out-dir", output], cwd=project, env=build_env)
        dist = output / "版本应用.dist"
        config = json.loads((dist / "ryupy-cpython.json").read_text(encoding="utf-8"))
        assert config["python_version"] == ".".join(map(str, version)), config
        relocated = workspace / "搬迁 空格目录"
        shutil.move(dist, relocated)
        shutil.rmtree(jit)
        shutil.rmtree(output)
        shutil.rmtree(inputs)
        assert not inputs.exists() and not jit.exists() and not output.exists()
        empty = workspace / "empty-path"
        empty.mkdir()
        polluted = make_polluted_env(empty)
        cwd = workspace / "run-aot"
        cwd.mkdir()
        executable_name = "版本应用.exe" if sys.platform == "win32" else "版本应用"
        actual = run([relocated / executable_name], cwd=cwd, env=polluted)
        assert_result(actual, oracle)
        assert (cwd / "版本 业务副作用.txt").read_bytes() == expected_effect
        print(f"[version] Python {tag} NativeAOT、版本冻结与删除项目/venv后搬迁通过", flush=True)
        if args.linux_container:
            assert sys.platform == "linux"
            docker = shutil.which("docker")
            assert docker
            base = [docker, "run", "--rm", "--pull=never", "--network=none", "--user=" + str(os.getuid()) + ":" + str(os.getgid())]
            probe = 'for tool in python python3 dotnet; do if command -v "$tool"; then exit 1; fi; done; test ! -d /usr/share/dotnet; test ! -d /usr/local/share/dotnet'
            run(base + [args.linux_container, "/bin/sh", "-c", probe], cwd=workspace, env=env)
            clean_cwd = workspace / "clean-target"
            clean_cwd.mkdir()
            command = base + ["--mount", f"type=bind,source={relocated},target=/app,readonly",
                              "--mount", f"type=bind,source={clean_cwd},target=/work", "--workdir=/work",
                              "--env=PATH=/empty", "--env=PYTHONHOME=/污染", "--env=PYTHONPATH=/污染",
                              "--env=LC_ALL=C.UTF-8", "--env=TZ=UTC", args.linux_container, "/app/" + executable_name]
            actual = run(command, cwd=workspace, env=env)
            assert_result(actual, oracle)
            assert (clean_cwd / "版本 业务副作用.txt").read_bytes() == expected_effect
            print(f"[version] Python {tag} 无Python/.NET/SDK容器通过", flush=True)
    except BaseException:
        print("[version] 失败现场：", workspace, flush=True)
        raise
    else:
        if not args.keep:
            shutil.rmtree(workspace)


if __name__ == "__main__":
    main()
