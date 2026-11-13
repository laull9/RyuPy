//! ForEach完整手写源矩阵：当次固定CPython、真实off/on PE与逐函数结构断言。
//! 三臂各用独占工作目录，临时诊断和发布树随测试作用域清理。
use super::{compile_linked, dotnet, foreach_accumulator_tests::session, runtime};
use anyhow::{Context, Result};
use ryupy_mir::{Callee, RuntimeFn, Rvalue, Statement};
use serde_json::{Value, json};
use std::{
    fs,
    path::{Path, PathBuf},
    process::{Command, Output},
};

fn write_json(path: &Path, value: &Value) -> Result<()> {
    fs::write(path, serde_json::to_vec_pretty(value)?)?;
    Ok(())
}

/// 保留原始字节；JSON只记状态，不经过文本归一化掩盖差异。
fn save_output(root: &Path, output: &Output) -> Result<()> {
    fs::create_dir_all(root)?;
    fs::write(root.join("stdout.bin"), &output.stdout)?;
    fs::write(root.join("stderr.bin"), &output.stderr)?;
    write_json(
        &root.join("status.json"),
        &json!({
            "success": output.status.success(), "code": output.status.code(),
            "display": output.status.to_string(),
        }),
    )
}

fn same_output(left: &Output, right: &Output) -> bool {
    left.status.code() == right.status.code()
        && left.status.success() == right.status.success()
        && left.stdout == right.stdout
        && left.stderr == right.stderr
}

/// 从实际linked MIR逐函数计数，不把文本dump中的名称出现次数当调用数。
fn count_functions(linked: &ryupy_mir::LinkedProgram) -> (Vec<Value>, usize) {
    let mut total = 0;
    let rows = linked
        .functions
        .iter()
        .map(|(id, function)| {
            let mut counts = [0_usize; 3];
            for (_, block) in function.body.blocks.iter() {
                for statement in &block.stmts {
                    let value = match statement {
                        Statement::Assign { value, .. } | Statement::Eval(value) => value,
                        _ => continue,
                    };
                    if let Rvalue::Call {
                        callee: Callee::Runtime(call),
                        ..
                    } = value
                    {
                        match call {
                            RuntimeFn::NumericAccumulatorCreate => counts[0] += 1,
                            RuntimeFn::NumericAccumulatorAdd => counts[1] += 1,
                            RuntimeFn::NumericAccumulatorValue => counts[2] += 1,
                            _ => {}
                        }
                    }
                }
            }
            total += counts[0];
            json!({"id": format!("{id:?}"), "name": function.name,
            "codegen_name": function.codegen_name, "source_file": function.source_file,
            "NumericAccumulatorCreate": counts[0], "NumericAccumulatorAdd": counts[1],
            "NumericAccumulatorValue": counts[2]})
        })
        .collect();
    (rows, total)
}

struct NativeRun {
    output: Output,
    holders: usize,
    subject_execution: String,
}

fn native(entry: &Path, out: &Path, enabled: bool) -> Result<NativeRun> {
    fs::create_dir_all(out)?;
    let mut session = session(enabled);
    let compiled = compile_linked(&mut session, entry, &[], false, None);
    write_json(
        &out.join("compile-meta.json"),
        &json!({
            "diagnostic_count": session.diagnostics.iter().count(),
            "compiled": compiled.is_ok(),
        }),
    )?;
    fs::write(
        out.join("diagnostics.txt"),
        session.render_diagnostics(false),
    )?;
    fs::write(
        out.join("module-plan.json"),
        session.module_environment_report_json()?,
    )?;
    let subject_execution = session
        .module_environment_report
        .iter()
        .find(|module| module.module == "subject")
        .map(|module| module.execution.clone())
        .unwrap_or_default();
    let (linked, _, _) = compiled.context("linked MIR编译失败")?;
    fs::write(out.join("linked-mir.txt"), format!("{linked:#?}"))?;
    let (counts, holders) = count_functions(&linked);
    write_json(
        &out.join("function-counts.json"),
        &json!({
            "experimental_foreach_accumulator": enabled,
            "compiled_feature": cfg!(feature = "ordinary-dynamic-module-pilot"),
            "functions": counts, "total_create": holders,
        }),
    )?;
    let options = ryupy_codegen_cil::EmitOptions {
        assembly_name: "foreach_fixture".into(),
        entry_source_path: Some(entry.to_string_lossy().into_owned()),
    };
    let emitted = ryupy_codegen_cil::emit_assembly_with_report(&linked, &options)?;
    let dll = out.join("foreach_fixture.dll");
    fs::write(&dll, &emitted.bytes)?;
    fs::write(
        out.join("emission-report.txt"),
        format!("{:#?}", emitted.report),
    )?;
    fs::write(
        out.join("linked.cil"),
        ryupy_codegen_cil::dump_cil(&linked, &options)?,
    )?;
    fs::write(
        out.join("foreach_fixture.runtimeconfig.json"),
        ryupy_codegen_cil::runtimeconfig::render(),
    )?;
    runtime::copy_runtime_to(out)?;
    let cwd = out.join("cwd");
    fs::create_dir(&cwd)?;
    let output = clean_command(dotnet::dotnet_exe())
        .arg(&dll)
        .current_dir(cwd)
        .output()?;
    save_output(&out.join("run"), &output)?;
    Ok(NativeRun {
        output,
        holders,
        subject_execution,
    })
}

fn copy_source(source: &Path, target: &Path) -> Result<()> {
    fs::create_dir_all(target)?;
    for name in ["entry.py", "subject.py"] {
        fs::copy(source.join(name), target.join(name))?;
    }
    Ok(())
}

// 比较子进程共享固定locale/时区且不继承开发机Python路径；cwd分别新建。
fn clean_command(executable: impl AsRef<std::ffi::OsStr>) -> Command {
    let mut command = Command::new(executable);
    command
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C")
        .env("TZ", "UTC")
        .env("PYTHONDONTWRITEBYTECODE", "1")
        .env("PYTHON_COLORS", "0");
    command
}

/// 既有边界只接受明确的原生原流或单一诊断；未知失败与off/on差异仍令测试失败。
fn known_boundary(
    case: &Value,
    oracle: &Output,
    off: &Result<NativeRun>,
    on: &Result<NativeRun>,
    root: &Path,
) -> Result<bool> {
    let expected = &case["known_expectation"];
    if !oracle.status.success() || !oracle.stderr.is_empty() {
        return Ok(false);
    }
    if let Some(stdout) = expected["native_stdout"].as_str() {
        return Ok(matches!((off, on), (Ok(off), Ok(on))
            if same_output(&off.output, &on.output) && off.output.status.success()
                && off.output.stderr.is_empty() && off.output.stdout == stdout.as_bytes()
                && !same_output(oracle, &off.output)));
    }
    let Some(message) = expected["compile_diagnostic"].as_str() else {
        return Ok(false);
    };
    if !matches!((off, on), (Err(left), Err(right)) if format!("{left:#}") == format!("{right:#}"))
    {
        return Ok(false);
    }
    let mut diagnostics = Vec::new();
    for mode in ["off", "on"] {
        let directory = root.join(mode);
        let metadata: Value =
            serde_json::from_slice(&fs::read(directory.join("compile-meta.json"))?)?;
        let diagnostic = fs::read_to_string(directory.join("diagnostics.txt"))?;
        if metadata["diagnostic_count"] != 1
            || !diagnostic.contains("E0100")
            || !diagnostic.contains(message)
            || directory.join("foreach_fixture.dll").exists()
        {
            return Ok(false);
        }
        diagnostics.push(diagnostic);
    }
    Ok(diagnostics[0] == diagnostics[1])
}

fn run_case(case: &Value, fixture: &Path, root: &Path, python: &Path) -> Result<Value> {
    let id = case["id"].as_str().context("缺少case id")?;
    let out = root.join(id);
    let source = out.join("source");
    copy_source(&fixture.join(id), &source)?;
    let entry = source.join("entry.py");
    let cwd = out.join("cpython-cwd");
    fs::create_dir(&cwd)?;
    let oracle = clean_command(python)
        .arg("-B")
        .arg(&entry)
        .current_dir(cwd)
        .output()
        .context("无法启动CPython oracle")?;
    save_output(&out.join("cpython"), &oracle)?;
    // 即使oracle或off失败仍执行on，完整保存三方原流及各阶段错误。
    let off = native(&entry, &out.join("off"), false);
    let on = native(&entry, &out.join("on"), true);
    let mut row = json!({"id": id, "selection_expectation": case["selection"],
        "ledger": case["ledger"], "cpython_success": oracle.status.success(),
        "cp_equal": false, "off_on_equal": false, "structure_ok": false,
        "classification": "NI"});
    for (name, result) in [("off", &off), ("on", &on)] {
        if let Err(error) = result {
            let message = format!("{error:#}");
            fs::create_dir_all(out.join(name))?;
            fs::write(out.join(name).join("failure.txt"), &message)?;
            row[format!("{name}_error")] = json!(message);
        }
    }
    if let (Ok(off), Ok(on)) = (&off, &on) {
        let equal = oracle.status.success()
            && same_output(&oracle, &off.output)
            && same_output(&oracle, &on.output);
        let expected = case["selection"].as_str().unwrap_or_default();
        let feature = cfg!(feature = "ordinary-dynamic-module-pilot");
        let structure = off.holders == 0
            && match expected {
                "candidate" if feature => on.holders > 0,
                "candidate" | "reject" => on.holders == 0,
                "legacy" => on.holders == 0 && on.subject_execution == "LegacySingleton",
                _ => false,
            };
        row["cp_equal"] = json!(equal);
        row["off_on_equal"] = json!(same_output(&off.output, &on.output));
        row["structure_ok"] = json!(structure);
        row["off_create"] = json!(off.holders);
        row["on_create"] = json!(on.holders);
        row["subject_execution"] = json!(on.subject_execution);
        row["classification"] = json!(if !equal {
            "semanticMismatch"
        } else if on.subject_execution == "LegacySingleton" {
            "plannerLegacy"
        } else if on.holders == 0 {
            "matcherRejectOrFeatureOff"
        } else {
            "success"
        });
    }
    let required = case["ledger"] == "required";
    row["passed"] = json!(row["cp_equal"] == true && row["structure_ok"] == true);
    row["required_failure"] = json!(required && row["passed"] != true);
    if case["ledger"] == "known-existing-boundary" {
        // 已有语义边界不是跳过结构合同的理由；只有准确前端NI不要求不存在的PE。
        let matched = known_boundary(case, &oracle, &off, &on, &out)?
            && (case["known_expectation"]["compile_diagnostic"].is_string()
                || row["structure_ok"] == true);
        row["known_boundary_matched"] = json!(matched);
        row["required_failure"] = json!(!matched);
        row["classification"] = json!(if matched {
            "knownExistingBoundary"
        } else {
            "unexpectedBoundaryOutcome"
        });
        // 即使将来边界行为改变，也不自动将此账计入普通CP通过。
        row["passed"] = json!(false);
    }
    write_json(&out.join("result.json"), &row)?;
    Ok(row)
}

#[test]
fn foreach_reconstructed_semantic_matrix() -> Result<()> {
    let workspace = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../..")
        .canonicalize()?;
    let fixtures = workspace.join("tests/fixtures/foreach_accumulator_reconstructed");
    let manifest_bytes = fs::read(fixtures.join("manifest.json"))?;
    let manifest: Value = serde_json::from_slice(&manifest_bytes)?;
    let root = crate::test_temp::Root::new("foreach-matrix");
    fs::write(root.join("manifest.json"), &manifest_bytes)?;
    let python = PathBuf::from(
        std::env::var_os("PYTHON").context("请显式设置固定CPython 3.12.13的PYTHON绝对路径")?,
    );
    anyhow::ensure!(python.is_absolute(), "PYTHON必须是绝对路径");
    for (name, mut command) in [
        ("python-version", clean_command(&python)),
        ("dotnet-version", clean_command(dotnet::dotnet_exe())),
    ] {
        match command.arg("--version").output() {
            Ok(output) => {
                save_output(&root.join(name), &output)?;
                if name == "python-version" {
                    anyhow::ensure!(
                        output.status.success() && output.stdout == b"Python 3.12.13\n",
                        "CPython版本必须为3.12.13；临时诊断目录{}",
                        root.display()
                    );
                }
            }
            Err(error) => fs::write(root.join(format!("{name}-error.txt")), error.to_string())?,
        }
    }
    let mut rows = Vec::new();
    for case in manifest["cases"].as_array().context("缺少cases")? {
        let result = std::panic::catch_unwind(|| run_case(case, &fixtures, &root, &python));
        let row = match result {
            Ok(Ok(row)) => row,
            Ok(Err(error)) => json!({"id": case["id"], "required_failure": true,
                "classification": "harnessFailure", "error": format!("{error:#}")}),
            Err(_) => json!({"id": case["id"], "required_failure": true,
                "classification": "compilerPanic", "error": "编译器panic，保留已有证据继续后续case"}),
        };
        rows.push(row);
        // 每例落盘，后续进程异常时也不会丢失已经完成的矩阵结果。
        write_json(&root.join("summary.json"), &json!({"cases": rows}))?;
    }
    let failed: Vec<_> = rows
        .iter()
        .filter(|row| row["required_failure"] == true)
        .map(|row| row["id"].as_str().unwrap_or("未知"))
        .collect();
    anyhow::ensure!(
        failed.is_empty(),
        "ForEach矩阵失败：{failed:?}；各用例结果：{rows:#?}"
    );
    Ok(())
}
