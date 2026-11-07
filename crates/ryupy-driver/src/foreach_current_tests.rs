//! 真实PE与独立CLR迭代器联合测试：区分Next/Current，证明unused目标不能删Current。
//! 四场景每次新进程，原始流和实际输入PE永久保存在本轮独占证据根。
use super::{
    compile_linked, dotnet,
    foreach_accumulator_tests::{holders, session},
    runtime,
};
use std::{fs, path::PathBuf, process::Command};

#[test]
fn foreach_native_current_effects_and_exceptions_survive() -> anyhow::Result<()> {
    let workspace = runtime::workspace_root();
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)?
        .as_nanos();
    let base = std::env::var_os("RYUPY_FOREACH_EVIDENCE_DIR")
        .map(PathBuf::from)
        .unwrap_or_else(|| workspace.join("target/foreach-new-run"));
    let root = base.join(format!("current-{}-{stamp}", std::process::id()));
    fs::create_dir_all(root.join("source"))?;
    fs::write(root.join("source/entry.py"), "import subject\n")?;
    fs::write(
        root.join("source/subject.py"),
        "TOKEN=0\ndef fold(seed, values):\n    total=seed\n    for unused in values:\n        total+=TOKEN\n    return total\n",
    )?;
    // 不在源码目录构建C#探针，不修改或重建产品runtime；引用本轮冻结DLL副本。
    let probe = root.join("probe");
    fs::create_dir(&probe)?;
    for name in ["Program.cs", "Probe.csproj"] {
        fs::copy(
            workspace
                .join("tests/validation/foreach_current_probe")
                .join(name),
            probe.join(name),
        )?;
    }
    runtime::copy_runtime_to(&probe)?;
    let built = Command::new(dotnet::dotnet_exe())
        .arg("build")
        .arg(probe.join("Probe.csproj"))
        .args(["-c", "Release", "-nologo", "-v", "q"])
        .arg(format!(
            "-p:RyuPyRuntimePath={}",
            probe.join("RyuPy.Runtime.dll").display()
        ))
        .output()?;
    fs::write(root.join("probe-build.stdout"), &built.stdout)?;
    fs::write(root.join("probe-build.stderr"), &built.stderr)?;
    anyhow::ensure!(
        built.status.success(),
        "Current探针构建失败：{}",
        root.display()
    );
    let host = probe.join("bin/Release/net10.0/Probe.dll");
    let mut off_outputs = vec![];
    for enabled in [false, true] {
        let out = root.join(if enabled { "on" } else { "off" });
        fs::create_dir(&out)?;
        let mut s = session(enabled);
        let (linked, _, _) =
            compile_linked(&mut s, &root.join("source/entry.py"), &[], false, None)
                .map_err(|e| anyhow::anyhow!("{e}: {}", s.render_diagnostics(false)))?;
        assert_eq!(
            holders(&linked),
            usize::from(enabled && cfg!(feature = "ordinary-dynamic-module-pilot"))
        );
        fs::write(out.join("plan.json"), s.module_environment_report_json()?)?;
        fs::write(out.join("linked-mir.txt"), format!("{linked:#?}"))?;
        let options = ryupy_codegen_cil::EmitOptions {
            assembly_name: "current_target".into(),
            entry_source_path: None,
        };
        fs::write(
            out.join("linked.cil"),
            ryupy_codegen_cil::dump_cil(&linked, &options)?,
        )?;
        let dll = out.join("current_target.dll");
        fs::write(&dll, ryupy_codegen_cil::emit_assembly(&linked, &options)?)?;
        for (index, scenario) in ["normal", "current-error", "next-error", "empty"]
            .into_iter()
            .enumerate()
        {
            let actual = Command::new(dotnet::dotnet_exe())
                .arg(&host)
                .arg(&dll)
                .arg(scenario)
                .output()?;
            fs::write(out.join(format!("{scenario}.stdout")), &actual.stdout)?;
            fs::write(out.join(format!("{scenario}.stderr")), &actual.stderr)?;
            fs::write(
                out.join(format!("{scenario}.status")),
                actual.status.to_string(),
            )?;
            anyhow::ensure!(
                actual.status.success(),
                "Current场景{scenario}/{enabled}失败，原流：{}",
                out.display()
            );
            assert!(actual.stderr.is_empty());
            if enabled {
                assert_eq!(actual.stdout, off_outputs[index], "{scenario}");
            } else {
                off_outputs.push(actual.stdout);
            }
        }
    }
    Ok(())
}
