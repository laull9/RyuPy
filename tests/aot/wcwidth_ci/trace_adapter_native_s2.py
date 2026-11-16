#!/usr/bin/env python3
"""三例专用静态入口Native门；六JIT先行，源码/managed输入固定，不经过产品--aot发射。"""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import sys
import time

import trace_adapter_s2 as h

ROOT = h.ROOT
S2 = ROOT / "target/trace-adapter-s2"
OUT = ROOT / "target/trace-adapter-native-s2"
# 构建树绝不放进证据root；只复制明确列出的受测产物和事实。
WORK = ROOT / "target/trace-adapter-native-s2-work"
HOST = ROOT / "tests/trace_adapter_s2_host/TraceAdapterHost.csproj"
SELECTED = ("explicit_inner", "native_local", "native_outer")
NOT_RUN = ("explicit_module", "recursive", "reused")
BOUNDARIES = ["outside-source", "outside-handle", "no-live-frame", "unbound-handle",
              "legacy-history", "unfinished-source", "cross-scope-source", "cross-scope-handle", "unfinished-close"]
NATIVE_EXCLUDED = {"ManagedInputChecks.cs", "GeneratedPeChecks.cs", "GeneratedPeChecksTests.cs"}
SHARED = {"Program.cs", "ExecutionChecks.cs", "ObservationWriter.cs", "NativeChecks.cs"}


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n")


def checked(record):
    path = Path(record["path"])
    if h.digest(path) != record:
        raise RuntimeError("冻结输入变化: " + str(path))
    return path


def properties(runtime, generated, evidence, preflight_only=False):
    values = {"TraceNativeEntry": "true", "PublishAot": "true", "RuntimeIdentifier": "linux-x64",
              "SelfContained": "true", "ProbeRuntime": str(runtime), "ProbeGenerated": str(generated),
              "TreatWarningsAsErrors": "true", "IlcTreatWarningsAsErrors": "true",
              "ILLinkTreatWarningsAsErrors": "true", "TrimmerSingleWarn": "false",
              "TraceEvidence": str(evidence), "TracePreflightOnly": "true" if preflight_only else "false"}
    return ["-p:" + key + "=" + value for key, value in values.items()]


def lines(path):
    return [line.strip() for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def inspect_build(evidence, artifacts, runtime, generated, preflight_only):
    """实际Csc集合/引用和已导入targets落盘在断言之前；不是SDK名称推测。"""
    compile_items = [Path(item) for item in lines(evidence / "compile-items.txt")]
    references = [Path(item) for item in lines(evidence / "references.txt")]
    imports = [Path(item) for item in lines(evidence / "imports.txt")]
    props = dict(line.split("=", 1) for line in lines(evidence / "properties.txt"))
    defs = lines(evidence / "defines.txt")
    host = h.unique(artifacts / "bin/TraceAdapterHost", "**/TraceAdapterHost.dll")
    resolved = {p.name: p for p in references if p.name in {runtime.name, generated.name}}
    facts = {"compile": [h.digest(p) for p in compile_items],
             "references": [h.digest(p) for p in references],
             "imports": [h.digest(p) for p in dict.fromkeys(imports) if p.is_file()],
             "properties": props, "defines": defs, "host": h.digest(host)}
    save(evidence / "build-facts.json", facts)
    compiled = {p.name for p in compile_items}
    if not SHARED <= compiled or compiled & NATIVE_EXCLUDED or "TRACE_NATIVE_ENTRY" not in defs:
        raise RuntimeError("Native Csc实际编译集合/常量不符")
    for path in compile_items:
        if path.name in SHARED:
            if path.read_bytes() != (HOST.parent / path.name).read_bytes():
                raise RuntimeError("Native Csc共享源码不是冻结字节")
        elif not path.resolve().is_relative_to((artifacts / "obj").resolve()):
            raise RuntimeError("Native Csc包含未授权外部源码: " + str(path))
    required = {"TraceNativeEntry": "true", "PublishAot": "true", "RuntimeIdentifier": "linux-x64",
                "SelfContained": "true", "TreatWarningsAsErrors": "true", "IlcTreatWarningsAsErrors": "true",
                "ILLinkTreatWarningsAsErrors": "true", "TrimmerSingleWarn": "false",
                "TracePreflightOnly": "true" if preflight_only else "false", "NETCoreSdkVersion": "10.0.403"}
    if any(props.get(key) != value for key, value in required.items()):
        raise RuntimeError("Native配置实际MSBuild属性不符")
    for path in (runtime, generated):
        if path.name not in resolved or resolved[path.name].read_bytes() != path.read_bytes():
            raise RuntimeError("MSBuild解析到错误managed引用: " + path.name)
        copied = host.parent / path.name
        if copied.read_bytes() != path.read_bytes():
            raise RuntimeError("Host实际复制managed字节不同: " + path.name)
    # 只保此配置实际Host，生成引用和runtime由外层输入归档各保一份。
    shutil.copy2(host, evidence / "TraceAdapterHost.dll")
    assets = h.unique(artifacts / "obj/TraceAdapterHost", "**/project.assets.json")
    shutil.copy2(assets, evidence / "project.assets.json")
    asset_data = json.loads(assets.read_text())
    ilc = {key: value for key, value in asset_data["libraries"].items() if "ilcompiler" in key.lower()}
    if not ilc:
        raise RuntimeError("未解析实际ILCompiler包")
    # 通过已解析assets的packageFolder/path核目标原件，而非下载另一版本来冒充。
    target_records = []
    for key, value in ilc.items():
        folders = [Path(folder) / value["path"] for folder in asset_data["packageFolders"]
                   if (Path(folder) / value["path"]).is_dir()]
        if len(folders) != 1:
            raise RuntimeError("ILCompiler包目录不唯一: " + key)
        for target in sorted(folders[0].rglob("Microsoft.NETCore.Native*.targets")):
            destination = evidence / "targets" / key.replace("/", "_") / target.name
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(target, destination)
            target_records.append({"package": key, **h.digest(target), "copy": str(destination)})
    save(evidence / "ilcompiler-targets.json", target_records)
    if not any(p["path"].endswith("/Microsoft.NETCore.Native.Publish.targets") for p in target_records):
        raise RuntimeError("缺少实际Native发布targets原件")
    return host


def preflight(dotnet, runtime, generated_inputs, env, runner):
    """由原managed脚本调用；三配置只Build，所有成功前禁止任何Native publish。"""
    WORK.mkdir(parents=True, exist_ok=False)
    frozen = {"runtime": runner.digest(runtime),
              "generated": {name: runner.digest(generated_inputs[name]) for name in SELECTED},
              "host_sources": [runner.digest(p) for p in sorted(HOST.parent.iterdir()) if p.is_file()],
              "fixtures": [runner.digest(p) for p in sorted(h.FIXTURES.iterdir()) if p.is_file()],
              "selected": list(SELECTED), "native_not_run": list(NOT_RUN)}
    original_generated = json.loads((S2 / "generated-identities.json").read_text())
    original_runtime = json.loads((S2 / "runtime-identities.json").read_text())
    if frozen["runtime"] not in original_runtime:
        raise RuntimeError("probe runtime与JIT前冻结不同")
    for name in SELECTED:
        if frozen["generated"][name] != original_generated[name]:
            raise RuntimeError("generated与JIT前冻结不同: " + name)
        identities = json.loads((S2 / "observed" / name / "identity.json").read_text())
        for record in (frozen["runtime"], frozen["generated"][name]):
            matches = [item for item in identities if item["Path"] == record["path"]]
            if len(matches) != 1 or matches[0]["Sha256"].lower() != record["sha256"]:
                raise RuntimeError("JIT加载检查与Native输入身份不符: " + name)
    save(S2 / "native-inputs.json", frozen)
    for name in SELECTED:
        evidence = S2 / "native-preflight" / name
        artifacts = WORK / "preflight" / name
        args = properties(runtime, generated_inputs[name], evidence, True)
        result = runner.run("native-preflight-" + name, [dotnet, "build", HOST, "-c", "Release",
            "--artifacts-path", artifacts, "-v:diag", "-nologo", *args], env, timeout=180)
        # 实际Csc必须发生，已有新建独立工作树不会因上轮缓存而只报incremental通过。
        log = result.stdout.decode(errors="replace") + result.stderr.decode(errors="replace")
        forbidden = [line for line in log.splitlines() if re.search(r'Target "(?:Publish|IlcCompile|LinkNative)(?:[":])', line)
                     and "skipped" not in line.lower()]
        objects = [str(p) for p in artifacts.rglob("*") if p.is_file() and p.suffix in {".o", ".obj"}]
        save(evidence / "preflight-no-ilc.json", {"forbidden_target_lines": forbidden, "native_objects": objects,
             "csc_task_present": 'Task "Csc"' in log})
        inspect_build(evidence, artifacts, runtime, generated_inputs[name], True)
        if forbidden or objects or 'Task "Csc"' not in log or "Generating native code" in log:
            raise RuntimeError("轻量预门没有实际Csc或意外执行ILC")
        # 预处理只导出本次真实导入目标；不运行Build/Publish或任何目标。
        runner.run("native-preprocess-" + name, [dotnet, "msbuild", HOST, "-p:Configuration=Release",
            "-p:ArtifactsPath=" + str(artifacts), "-preprocess:" + str(evidence / "imported-project.xml"),
            *args], env, timeout=60)
        for record in [frozen["runtime"], frozen["generated"][name], *frozen["host_sources"], *frozen["fixtures"]]:
            checked(record)
    save(S2 / "native-preflight-result.json", {"status": "passed", "cases": list(SELECTED),
         "scope": "Native-configuration managed compilation only; no ILC; no product execution"})


def verify_observation(directory, name, expected_events):
    actual = json.loads((directory / "actual.json").read_text())
    expected = json.loads((h.FIXTURES / (name + ".expected.json")).read_text())
    if actual != expected or json.loads((directory / "events.json").read_text()) != expected_events:
        raise RuntimeError("实际Native/JIT事件或冻结CP结果不符: " + name)
    status = json.loads((directory / "execution-status.json").read_text())
    if status != {"Stage": "passed", "Error": None, "DisposeError": None}:
        raise RuntimeError("真实断言/Dispose没有成功: " + name)
    if json.loads((directory / "boundaries.json").read_text())["Passed"] != BOUNDARIES:
        raise RuntimeError("手工边界未全部具名拒绝: " + name)
    return actual


def preserved_inputs(frozen):
    for record in [frozen["runtime"], *frozen["generated"].values(), *frozen["host_sources"], *frozen["fixtures"]]:
        checked(record)


class DockerRunner:
    """对接已有固定镜像身份核验；原命令和流仍走统一有限预算。"""
    @property
    def logs(self):
        return OUT

    def run(self, label, argv, **kwargs):
        kwargs.pop("check", None)
        return h.run(label, argv, **kwargs)


def strict(label, argv, env, cwd, timeout):
    result = h.run(label, argv, env, cwd=cwd, timeout=timeout)
    if result.stderr:
        raise RuntimeError(label + "正向stderr非空")
    return result


def capture_publish(case, artifacts, dist):
    """白名单先归档可用发布原物；未验证不是通过，缺失/归档错误逐项记录。"""
    root = case / "publish-capture"
    root.mkdir(parents=True, exist_ok=True)
    record = {"verification": "unverified", "files": [], "missing": [], "errors": []}
    wanted = [("elf", dist / "TraceAdapterHost", root / "elf/TraceAdapterHost")]
    for kind, folder, pattern in (("host", artifacts / "bin/TraceAdapterHost", "**/TraceAdapterHost.dll"),
                                  ("runtime", artifacts / "bin/TraceAdapterHost", "**/RyuPy.Runtime.dll"),
                                  ("generated", artifacts / "bin/TraceAdapterHost", "**/" + case.name + ".dll"),
                                  ("assets", artifacts / "obj/TraceAdapterHost", "**/project.assets.json"),
                                  ("ilc-rsp", artifacts, "**/*.ilc.rsp")):
        candidates = sorted(folder.glob(pattern)) if folder.exists() else []
        if not candidates:
            record["missing"].append({"kind": kind, "pattern": str(folder / pattern)})
        wanted.extend((kind, path, root / kind / path.relative_to(artifacts)) for path in candidates)
    total = 0
    for kind, source, target in wanted:
        partial = target.with_name(target.name + ".capture-partial")
        try:
            if not source.exists():
                record["missing"].append({"kind": kind, "path": str(source)})
                continue
            if source.is_symlink() or not source.is_file():
                raise RuntimeError("归档不接受链接或特殊文件")
            total += source.stat().st_size
            if total > 256 * 1024 * 1024 or len(record["files"]) >= 128:
                raise RuntimeError("发布原件超过冻结256MiB/128文件上限")
            before = h.digest(source)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, partial)
            partial.replace(target)
            copied = h.digest(target)
            record["files"].append({"kind": kind, "source": before, "copy": copied, "verification": "unverified"})
            if copied["sha256"] != before["sha256"] or h.digest(source) != before:
                raise RuntimeError("归档期间来源字节改变")
        except Exception as error:
            record["errors"].append({"kind": kind, "path": str(source), "error": repr(error)})
        finally:
            if partial.exists():
                partial.unlink()
    # 实际Csc事实已由MSBuild直接写入证据root，先清点已有事实，即便构建未成功。
    record["available_build_facts"] = [h.digest(p) for p in sorted((case / "build").rglob("*")) if p.is_file()]
    save(root / "capture.json", record)
    return record


def publish_with_capture(action, case, artifacts, dist):
    """finally先保存实际产物；capture失败不覆盖publish首错，不重试构建。"""
    primary = None
    try:
        return action()
    except BaseException as error:
        primary = error
        raise
    finally:
        try:
            captured = capture_publish(case, artifacts, dist)
            if captured["errors"]:
                raise RuntimeError("发布原件归档不完整，见capture.json")
        except Exception as capture_error:
            try:
                save(case / "capture-failure.json", {"primary_error": repr(primary) if primary else None,
                     "capture_error": repr(capture_error), "verification": "failed-capture"})
            except Exception as report_error:
                print("capture报告也失败: " + repr(report_error), file=sys.stderr)
            print("发布原件归档错误: " + repr(capture_error), file=sys.stderr)
            if primary is None:
                raise


def evidence_inventory(root):
    """成功/失败都列已保存原件；排除清单本身及未完成临时文件。"""
    save(root / "evidence-files.json", [h.digest(p) for p in sorted(root.rglob("*"))
         if p.is_file() and p.name != "evidence-files.json" and not p.name.endswith(".capture-partial")])


def main():
    h.OUT = OUT
    h.DEADLINE = time.monotonic() + 30 * 60
    OUT.mkdir(parents=True, exist_ok=False)
    frozen = json.loads((S2 / "native-inputs.json").read_text())
    prerequisite = json.loads((S2 / "native-preflight-result.json").read_text())
    if prerequisite["status"] != "passed" or prerequisite["cases"] != list(SELECTED):
        raise RuntimeError("三Native配置轻量编译没有全部通过")
    if json.loads((S2 / "result.json").read_text())["status"] != "passed":
        raise RuntimeError("原六例managed预门没有全部通过")
    preserved_inputs(frozen)
    save(OUT / "frozen-inputs.json", frozen)
    save(OUT / "coverage.json", {"native_selected": list(SELECTED), "native_not_run": list(NOT_RUN),
        "mode": "same ordinary probe PE JIT -> dedicated static-entry ILC; not product --aot qualification",
        "production": "closed", "original_dynamic448_and_generator_frames": "unresolved"})
    # 证据只保显式输入和源码；不复制CLI/obj/全SDK。原六JIT输入仍由S2 artifact保存。
    inputs = OUT / "managed-inputs"
    inputs.mkdir()
    runtime = checked(frozen["runtime"])
    shutil.copy2(runtime, inputs / runtime.name)
    for name in SELECTED:
        path = checked(frozen["generated"][name])
        shutil.copy2(path, inputs / path.name)
    sources = OUT / "host-sources"
    sources.mkdir()
    for record in frozen["host_sources"]:
        path = checked(record)
        shutil.copy2(path, sources / path.name)
    env = os.environ.copy()
    dotnet = env.get("DOTNET", "dotnet")
    h.run("dotnet-info", [dotnet, "--info"], env, timeout=30)
    h.run("git-head", ["git", "rev-parse", "HEAD", "HEAD^{tree}"], env, timeout=30)
    sys.path.insert(0, str(ROOT / "tests/aot"))
    from docker_image import prepare_image
    from native_evidence import snapshot
    image = prepare_image(DockerRunner(), "docker", WORK, env)
    for name in SELECTED:
        preserved_inputs(frozen)
        case = OUT / name
        case.mkdir()
        generated = checked(frozen["generated"][name])
        artifacts = WORK / "publish-build" / name
        dist = WORK / "publish" / name
        evidence = case / "build"
        args = properties(runtime, generated, evidence)
        publish_with_capture(lambda: h.run(name + "-publish", [dotnet, "publish", HOST, "-c", "Release",
            "--artifacts-path", artifacts, "-o", dist, "-v:diag", "-nologo", *args], env, timeout=360),
            case, artifacts, dist)
        inspect_build(evidence, artifacts, runtime, generated, False)
        preserved_inputs(frozen)
        # ELF与ILC实际rsp共同约束静态输入，不用Assembly.Location冒充Native加载身份。
        response_files = sorted(artifacts.rglob("*.ilc.rsp"))
        if len(response_files) != 1:
            raise RuntimeError("实际ILC response文件不唯一")
        rsp = response_files[0]
        shutil.copy2(rsp, evidence / "actual.ilc.rsp")
        text = rsp.read_text(encoding="utf-8-sig")
        main_inputs = [Path(line.strip('"')) for line in text.splitlines()
                       if line.strip('"').endswith("/TraceAdapterHost.dll") and not line.startswith("-")]
        if len(main_inputs) != 1 or main_inputs[0].read_bytes() != (evidence / "TraceAdapterHost.dll").read_bytes():
            raise RuntimeError("ILC主Host输入不是已记录编译字节")
        save(evidence / "ilc-host-identity.json", h.digest(main_inputs[0]))
        if runtime.name not in text or generated.name not in text:
            raise RuntimeError("实际ILC引用缺少冻结runtime/generated")
        # response中的真实路径必须逐字重hash，不以文件名匹配代替身份。
        linked = {}
        for line in text.splitlines():
            if line.startswith("-r:"):
                path = Path(line[3:].strip('"'))
                if path.name in {runtime.name, generated.name}:
                    if path.name in linked:
                        raise RuntimeError("ILC同名引用不唯一")
                    linked[path.name] = h.digest(path)
        save(evidence / "ilc-managed-identities.json", linked)
        for original in (runtime, generated):
            record = linked.get(original.name)
            if record is None or record["sha256"] != h.digest(original)["sha256"]:
                raise RuntimeError("ILC实际managed字节与JIT输入不同")
        exe = dist / "TraceAdapterHost"
        if exe.read_bytes()[:4] != b"\x7fELF":
            raise RuntimeError("publish不是实际ELF")
        before, _ = snapshot(dist)
        save(case / "publish-dist.json", before)
        # 实际受测ELF只保一份；debug符号列hash但不复制，不能称整个dist可字节重放。
        tested = case / "publish-capture/elf"
        if (tested / exe.name).read_bytes() != exe.read_bytes():
            raise RuntimeError("先归档的未验ELF与当前发布字节不同")
        save(case / "tested-elf.json", {"verification": "input-and-ELF-checks-passed; execution-pending",
             **h.digest(tested / exe.name)})
        h.run(name + "-elf-header", ["readelf", "-h", exe], env, timeout=30)
        dynamic = h.run(name + "-elf-dynamic", ["readelf", "-d", exe], env, timeout=30)
        dependencies = h.run(name + "-dependencies", ["ldd", exe], env, timeout=30)
        if any(item in (dynamic.stdout + dependencies.stdout).lower() for item in (b"libcoreclr", b"hostfxr", b"hostpolicy")):
            raise RuntimeError("不是独立Native入口")
        jit_events = json.loads((S2 / "observed" / name / "events.json").read_text())
        verify_observation(S2 / "observed" / name, name, jit_events)
        for mode in ("native", "relocated", "container"):
            observed = case / mode
            observed.mkdir()
            if mode == "native":
                result = strict(name + "-native", [exe, name, observed], env, dist, 30)
            elif mode == "relocated":
                moved = WORK / "relocated" / name
                moved.parent.mkdir(exist_ok=True)
                shutil.move(dist, moved)
                if dist.exists() or snapshot(moved)[0] != before:
                    raise RuntimeError("搬迁改变dist或原路径仍存在")
                result = strict(name + "-relocated", [moved / exe.name, name, observed], env, moved, 30)
            else:
                # 容器只见单个真正受测ELF及空可写证据目录；无managed/Python/SDK挂载。
                script = ('for tool in dotnet python python3 python3.12; do '
                          'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; exec "$1" "$2" /evidence')
                result = strict(name + "-container", ["docker", "run", "--pull=never", "--rm", "--network", "none",
                    "--read-only", "--user", f"{os.getuid()}:{os.getgid()}",
                    "--mount", f"type=bind,src={tested},dst=/app,readonly",
                    "--mount", f"type=bind,src={observed},dst=/evidence", "--workdir", "/app", "--entrypoint", "/bin/sh",
                    image, "-c", script, "trace-native", "/app/TraceAdapterHost", name], env, WORK, 60)
            actual = verify_observation(observed, name, jit_events)
            if json.loads(result.stdout) != actual:
                raise RuntimeError("实际stdout与证据不同")
            preserved_inputs(frozen)
            if snapshot(moved if mode != "native" else dist)[0] != before:
                raise RuntimeError("执行改变dist")
            if (tested / exe.name).read_bytes() != (moved / exe.name if mode != "native" else exe).read_bytes():
                raise RuntimeError("受测ELF备份与执行字节不同")
        save(case / "result.json", {"status": "passed", "case": name, "elf": h.digest(tested / exe.name)})
    h.write("result.json", {"status": "passed", "native_cases": list(SELECTED), "native_not_run": list(NOT_RUN),
        "scope": "test-only static entry; production closed; original failures unresolved"})


if __name__ == "__main__":
    primary_failure = None
    try:
        main()
    except BaseException as error:
        primary_failure = error
        if OUT.exists():
            try:
                h.write("failure.json", {"type": type(error).__name__, "message": str(error)})
            except Exception as report_error:
                print("failure报告也失败: " + repr(report_error), file=sys.stderr)
        raise
    finally:
        if OUT.exists():
            try:
                evidence_inventory(OUT)
            except Exception as inventory_error:
                print("最终原件清单失败: " + repr(inventory_error), file=sys.stderr)
                if primary_failure is None:
                    raise
