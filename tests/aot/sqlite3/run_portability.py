"""实际编译并搬迁 JIT/NativeAOT SQLite 应用；不增加普通文件摘要校验流程。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sqlite3
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests/aot"))
from runtime_dependencies import MANAGED, NATIVE, validate_runtime_dependencies


def run(command, cwd, environment, log, expected=0):
    result = subprocess.run([str(part) for part in command], cwd=cwd, env=environment,
                            capture_output=True, text=True, timeout=600)
    with log.open("a", encoding="utf-8") as stream:
        stream.write("$ " + " ".join(map(str, command)) + "\n" + result.stdout + result.stderr)
    if result.returncode != expected:
        raise AssertionError(f"退出码 {result.returncode} != {expected}，见 {log}")
    return result.stdout


def inspect_database(path, expected):
    with sqlite3.connect(path) as connection:
        rows = connection.execute("select * from records order by id").fetchall()
    assert rows == expected, rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cli", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    options = parser.parse_args()
    output = options.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    log = output / "portability.log"
    environment = os.environ.copy()
    for key in ("RYUPY_RUNTIME_DIR", "DOTNET_ADDITIONAL_DEPS", "DOTNET_SHARED_STORE",
                "DOTNET_STARTUP_HOOKS", "DYLD_LIBRARY_PATH", "LD_LIBRARY_PATH"):
        environment.pop(key, None)
    runtime = ROOT / "runtime/RyuPy.Runtime/bin/Release/net10.0"
    rid = run(["dotnet", "msbuild", ROOT / "runtime/RyuPy.Runtime", "-nologo",
               "-getProperty:NETCoreSdkRuntimeIdentifier"], ROOT, environment, log).strip()
    library = NATIVE[rid]
    dotnet = shutil.which("dotnet")
    assert dotnet
    # 模拟发行 CLI 布局，强制相对可执行文件定位随包 runtime，不借开发目录兜底。
    release = output / "release"
    (release / "bin").mkdir(parents=True)
    packaged = release / "runtime"
    packaged.mkdir()
    cli = release / "bin" / options.cli.name
    shutil.copy2(options.cli.resolve(), cli)
    for name in ["RyuPy.Runtime.dll", "RuntimeDependencies.props", "ThirdPartyNotices.sqlite3.txt", *MANAGED]:
        shutil.copy2(runtime / name, packaged / name)
    shutil.copytree(runtime / "runtimes" / rid / "native", packaged / "runtimes" / rid / "native")
    assert [path.name for path in (packaged / "runtimes").iterdir()] == [rid]
    source = output / "portable.py"
    shutil.copy2(Path(__file__).with_name("portable.py"), source)
    expected = [(1, "原目录", b"\x00\xff"), (2, "待搬迁", b"abc")]
    for kind in ("jit", "aot"):
        build = output / (kind + "-build")
        command = [cli, "build", source, "-o", build]
        if kind == "aot":
            command.append("--aot")
        run(command, ROOT, environment, log)
        bundle = build / "portable.dist" if kind == "aot" else build
        artifact = "portable" + (".exe" if os.name == "nt" else "") if kind == "aot" else "portable.dll"
        invocation = [bundle / artifact] if kind == "aot" else [dotnet, bundle / artifact]
        run([*invocation, "seed"], bundle, environment, log)
        inspect_database(bundle / "portable.sqlite3", expected)
        if kind == "aot":
            manifest = json.loads((bundle / "ryupy-artifact.json").read_text())
            validate_runtime_dependencies(manifest["artifacts"], rid)
        assert (bundle / library).is_file()
        assert (bundle / "ThirdPartyNotices.sqlite3.txt").is_file()
        assert not (bundle / "runtimes").exists()
        moved = output / (kind + "-moved")
        shutil.copytree(bundle, moved)
        # 原目录消失、工作目录改变；AOT 运行环境无 dotnet/SDK 搜索路径。
        shutil.move(str(build), output / (kind + "-original-hidden"))
        isolated = environment.copy()
        isolated["PATH"] = str(output / "empty-path")
        isolated["DOTNET_ROOT"] = str(output / "no-sdk")
        isolated["DOTNET_ROOT_ARM64"] = isolated["DOTNET_ROOT"]
        invocation = [moved / artifact] if kind == "aot" else [dotnet, moved / artifact]
        # 工作目录放同名无效库：实际解析必须先找到应用目录内的成熟 provider 资产。
        working = output / (kind + "-working")
        working.mkdir()
        (working / library).write_bytes("无效的当前目录同名库".encode())
        shutil.move(str(moved / "portable.sqlite3"), working / "portable.sqlite3")
        run([*invocation, "append"], working, isolated, log)
        inspect_database(working / "portable.sqlite3", [*expected, (3, "搬迁后", b"\x01\x02")])
        missing = output / (kind + "-missing")
        shutil.copytree(moved, missing)
        (missing / library).unlink()
        invocation = [missing / artifact] if kind == "aot" else [dotnet, missing / artifact]
        text = run([*invocation, "read"], missing, isolated, log, expected=23)
        assert text.strip() == "sqlite native dependency missing", text
        print(kind + "：随包/搬迁/既有数据库读写/缺库 ImportError 通过")
    # 发行 runtime 缺少宿主 native 时，driver 在编译阶段给出明确错误。
    (packaged / "runtimes" / rid / "native" / library).unlink()
    result = subprocess.run([str(cli), "build", str(source), "-o", str(output / "missing-build")],
                            cwd=output, env=environment, capture_output=True, text=True)
    assert result.returncode != 0 and "原生依赖" in result.stderr, result.stderr
    print("发行 CLI：缺库构建错误通过；证据：" + str(log))


if __name__ == "__main__":
    main()
