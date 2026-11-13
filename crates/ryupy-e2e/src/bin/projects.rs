//! 固定项目入口的默认 JIT 运行器：复用 driver，保留 JSONL 字段和完整诊断/运行原流。
//! 此二进制是进度报告器，不自行决定产品准入；`make projects-preserve/promote` 包装完整20项。
//! `--only` 仅供定向诊断。门禁按项独占目录并用外部期限监督本进程，拒绝旧报告与不完整结果。

#[path = "../project_manifest.rs"]
mod project_manifest;

use std::collections::BTreeMap;
use std::io::Write as _;
use std::path::Path;
use std::process::{Command, ExitCode, Stdio};

use project_manifest::{Options, Project, read_manifest, search_paths};
use ryupy_driver::{BuildOptions, HasErrors, Session, dotnet, runtime::workspace_root};
use ryupy_e2e::json_escape;

struct Outcome {
    status: &'static str,
    kind: &'static str,
    detail: String,
    run_exit: Option<i32>,
}

impl Outcome {
    fn new(status: &'static str, kind: &'static str, detail: impl Into<String>) -> Self {
        Self {
            status,
            kind,
            detail: detail.into(),
            run_exit: None,
        }
    }
}

fn main() -> ExitCode {
    match execute() {
        Ok(()) => ExitCode::SUCCESS,
        Err(err) => {
            eprintln!("项目运行器失败：{err}");
            ExitCode::from(2)
        }
    }
}

fn execute() -> Result<(), String> {
    let root = workspace_root();
    let options = Options::parse(&root)?;
    let projects_dir = root.join("tests/projects");
    let projects = read_manifest(&projects_dir.join("manifest.tsv"))?;
    if let Some(only) = &options.only {
        for id in only {
            if !projects.iter().any(|p| &p.id == id) {
                return Err(format!("未知项目 ID：{id}"));
            }
        }
    }
    // run-id 模式要求独占输出目录；无论成功失败都不得复用旧产物/报告。
    if options.run_id.is_empty() {
        std::fs::create_dir_all(&options.output).map_err(|e| e.to_string())?;
    } else {
        std::fs::create_dir(&options.output).map_err(|e| e.to_string())?;
    }
    let mut results = Vec::new();
    let mut counts = BTreeMap::new();
    let mut infrastructure_failure = false;
    for project in &projects {
        if options
            .only
            .as_ref()
            .is_some_and(|ids| !ids.contains(&project.id))
        {
            continue;
        }
        let directory = options.output.join(&project.id);
        std::fs::create_dir_all(&directory).map_err(|e| e.to_string())?;
        let outcome = match run_project(&options, project, &directory) {
            Ok(outcome) => outcome,
            Err(err) => Outcome::new("not-evaluated", "infrastructure", err),
        };
        infrastructure_failure |=
            matches!(outcome.kind, "infrastructure" | "compiler-error" | "crash");
        *counts.entry(outcome.status).or_insert(0usize) += 1;
        println!(
            "{:<18} {:<14} {}",
            project.id, outcome.status, outcome.detail
        );
        results.push((project, outcome));
    }
    let path = options.output.join("projects.jsonl");
    write_results(&path, &options.run_id, &results).map_err(|e| e.to_string())?;
    println!(
        "结果：{}；native {}/{}；仅进度报告，未执行保留/晋升判定",
        path.display(),
        counts.get("native").copied().unwrap_or(0),
        results.len()
    );
    if infrastructure_failure {
        return Err("至少一项存在运行器/编译器异常".to_owned());
    }
    Ok(())
}

/// 固定CPython即时生成预期；每臂独立cwd，绝不读取历史stdout或忽略基线失败。
fn project_oracle(
    options: &Options,
    project: &Project,
    out: &Path,
) -> Result<std::process::Output, String> {
    let version = include_str!("../../../../tests/pinned-python.txt").trim();
    let python = if let Ok(value) = std::env::var("PYTHON") {
        value
    } else {
        let found = Command::new("uv")
            .args(["python", "find", version])
            .output()
            .map_err(|e| e.to_string())?;
        if !found.status.success() {
            return Err("缺少固定CPython".into());
        }
        String::from_utf8(found.stdout)
            .map_err(|e| e.to_string())?
            .trim()
            .to_owned()
    };
    let actual = Command::new(&python)
        .args([
            "-S",
            "-B",
            "-c",
            "import platform; print(platform.python_version())",
        ])
        .output()
        .map_err(|e| e.to_string())?;
    if !actual.status.success() || String::from_utf8_lossy(&actual.stdout).trim() != version {
        return Err(format!("项目oracle要求CPython {version}"));
    }
    let cwd = out.join("cpython-cwd");
    std::fs::create_dir(&cwd).map_err(|e| e.to_string())?;
    let paths =
        std::env::join_paths(search_paths(&options.store, project)?).map_err(|e| e.to_string())?;
    let expected = Command::new(&python)
        .args(["-S", "-B"])
        .arg(&project.entry)
        .env("PYTHONPATH", paths)
        .env("PYTHONHASHSEED", "0")
        .env("PYTHONDONTWRITEBYTECODE", "1")
        .env("PYTHON_COLORS", "0")
        .env("NO_COLOR", "1")
        .current_dir(cwd)
        .stdin(Stdio::null())
        .output()
        .map_err(|e| format!("CPython oracle执行失败：{e}"))?;
    std::fs::write(out.join("cpython.stdout"), &expected.stdout).map_err(|e| e.to_string())?;
    std::fs::write(out.join("cpython.stderr"), &expected.stderr).map_err(|e| e.to_string())?;
    if !expected.status.success() {
        return Err(format!(
            "CPython oracle失败：{}",
            String::from_utf8_lossy(&expected.stderr)
        ));
    }
    Ok(expected)
}

/// 只允许省略本轮CP真实SyntaxWarning块；产品新增/改写warning仍须失败。
fn stderr_matches(actual: &[u8], expected: &[u8]) -> bool {
    let lines: Vec<_> = expected.split_inclusive(|byte| *byte == b'\n').collect();
    let mut offsets = vec![0usize];
    let mut index = 0;
    while index < lines.len() {
        let line = lines[index];
        let optional = line.starts_with(b"SyntaxWarning:")
            || line
                .windows(16)
                .position(|part| part == b": SyntaxWarning:")
                .is_some_and(|position| {
                    let prefix = &line[..position];
                    prefix.contains(&b':')
                        && prefix
                            .rsplit(|byte| *byte == b':')
                            .next()
                            .is_some_and(|number| {
                                !number.is_empty() && number.iter().all(u8::is_ascii_digit)
                            })
                });
        let mut block = line.to_vec();
        index += 1;
        if optional && index < lines.len() && matches!(lines[index].first(), Some(b' ' | b'\t')) {
            block.extend_from_slice(lines[index]);
            index += 1;
        }
        let mut next: Vec<_> = offsets
            .iter()
            .filter_map(|offset| {
                actual[*offset..]
                    .starts_with(&block)
                    .then_some(offset + block.len())
            })
            .collect();
        if optional {
            next.extend_from_slice(&offsets);
        }
        next.sort_unstable();
        next.dedup();
        offsets = next;
        if offsets.is_empty() {
            return false;
        }
    }
    offsets.contains(&actual.len())
}

fn run_project(options: &Options, project: &Project, out: &Path) -> Result<Outcome, String> {
    let expected = project_oracle(options, project, out)?;
    if !project.entry.is_file() {
        return Err("入口缺失".to_owned());
    }
    let opts = BuildOptions {
        input: project.entry.clone(),
        out_dir: out.to_path_buf(),
        aot: false,
        search_paths: search_paths(&options.store, project)?,
    };
    // 所有显式实验、额外字体、模块实例配置保持 Session 默认值，不读取外部配置冒充默认。
    let mut session = Session::default();
    let build = ryupy_driver::build(&mut session, &opts);
    // 额外审计文件不改变P0的ID/默认CLI mode或成功标准，失败图也保计划与完成相位。
    std::fs::write(
        out.join("module-environment.json"),
        session
            .module_environment_report_json()
            .map_err(|e| e.to_string())?,
    )
    .map_err(|e| e.to_string())?;
    let diagnostics = session.render_diagnostics(false);
    std::fs::write(out.join("compile.diagnostics.txt"), &diagnostics).map_err(|e| e.to_string())?;
    let artifacts = match build {
        Ok(artifacts) => artifacts,
        Err(error) => {
            std::fs::write(out.join("compile.error.txt"), format!("{error:#}"))
                .map_err(|e| e.to_string())?;
            let first = diagnostics
                .lines()
                .find(|line| line.trim_start().starts_with('['))
                .unwrap_or("无源码诊断");
            return Ok(
                if error.is::<HasErrors>() && session.diagnostics.has_errors() {
                    Outcome::new("unsupported", "diagnostic", first)
                } else {
                    Outcome::new("failed", "compiler-error", format!("{error:#}"))
                },
            );
        }
    };
    // 原流直接落盘，父门禁可监测大小并终止整个进程组；不让 wait_with_output 无限积内存。
    let stdout_file = std::fs::File::create(out.join("jit.stdout")).map_err(|e| e.to_string())?;
    let stderr_file = std::fs::File::create(out.join("jit.stderr")).map_err(|e| e.to_string())?;
    let jit_cwd = out.join("jit-cwd");
    std::fs::create_dir(&jit_cwd).map_err(|e| e.to_string())?;
    let status = Command::new(dotnet::dotnet_exe())
        .arg(&artifacts.dll)
        .current_dir(&jit_cwd)
        .stdin(Stdio::null())
        .stdout(stdout_file)
        .stderr(stderr_file)
        .status()
        .map_err(|e| format!("启动/等待 dotnet 失败：{e:#}"))?;
    let stdout = std::fs::read(out.join("jit.stdout")).map_err(|e| e.to_string())?;
    let stderr = std::fs::read(out.join("jit.stderr")).map_err(|e| e.to_string())?;
    let mut outcome = if !status.success() {
        let text = String::from_utf8_lossy(&stderr);
        let detail = text
            .lines()
            .rev()
            .find(|line| !line.trim().is_empty())
            .unwrap_or("无 stderr");
        // 非 Python 的 CLR/宿主崩溃不可被当作普通未支持项掩盖。
        let python_error = text
            .lines()
            .any(|line| line.starts_with("Traceback (most recent call last):"));
        Outcome::new(
            "failed",
            if status.code().is_some() && python_error {
                "runtime-error"
            } else {
                "crash"
            },
            detail,
        )
    } else if stdout != expected.stdout || !stderr_matches(&stderr, &expected.stderr) {
        Outcome::new(
            "failed",
            "output-mismatch",
            "运行成功但原始流与同次CPython合同不一致",
        )
    } else {
        Outcome::new("native", "", "运行成功，原始流与同次固定CPython一致")
    };
    outcome.run_exit = status.code();
    Ok(outcome)
}

fn write_results(
    path: &Path,
    run_id: &str,
    results: &[(&Project, Outcome)],
) -> std::io::Result<()> {
    let mut buffer = Vec::new();
    for (project, result) in results {
        let fields = [
            ("id", project.id.as_str()),
            ("pypi", &project.pypi),
            ("version", &project.version),
            ("status", result.status),
            ("entry", &project.entry.display().to_string()),
            ("scope", &project.scope),
            ("deps", &project.deps),
            ("detail", &result.detail),
            ("failure_kind", result.kind),
            ("run_id", run_id),
            ("mode", "default-jit"),
        ];
        write!(buffer, "{{")?;
        for (index, (key, value)) in fields.iter().enumerate() {
            if index > 0 {
                write!(buffer, ",")?;
            }
            write!(buffer, "\"{}\":\"{}\"", key, json_escape(value))?;
        }
        writeln!(
            buffer,
            ",\"run_exit\":{}}}",
            result
                .run_exit
                .map_or_else(|| "null".to_owned(), |v| v.to_string())
        )?;
    }
    // 原子发布标记此调用已完成；产品准入继续由包装门禁逐ID判定。
    let temporary = path.with_extension("jsonl.tmp");
    std::fs::write(&temporary, buffer)?;
    std::fs::rename(temporary, path)
}

#[cfg(test)]
mod tests {
    use super::stderr_matches;

    #[test]
    fn only_current_oracle_warnings_can_be_omitted() {
        let expected = b"/source.py:4: SyntaxWarning: known\n  x is 1\nbusiness\n";
        assert!(stderr_matches(expected, expected));
        assert!(stderr_matches(b"business\n", expected));
        assert!(!stderr_matches(
            b"SyntaxWarning: unknown\nbusiness\n",
            expected
        ));
        assert!(!stderr_matches(
            b"/source.py:4: SyntaxWarning: changed\n  x is 1\nbusiness\n",
            expected
        ));
        assert!(!stderr_matches(
            b"/source.py:5: SyntaxWarning: known\n  x is 1\nbusiness\n",
            expected
        ));
        assert!(!stderr_matches(b"changed business\n", expected));
        assert!(!stderr_matches(b"", b"business: SyntaxWarning: keep\n"));
    }
}
