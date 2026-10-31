//! 项目级运行入口（M2.0）：编译并运行固定项目集的入口样例，对照 CPython 基线。
//!
//! 与 `tests/e2e.rs` 的区别：
//! - e2e 关注**单个特性**，一个 `.py` 一个用例；
//! - 这里关注**项目**：入口样例会 `import` 一个真实项目的源码，只有整条链路（模块系统、
//!   容器、对象、异常）都通了才可能通过。所以它同时是「下一项阻塞」的报告工具。
//!
//! 状态口径（与 docs/compatibility.md 一致）：
//!
//! | 状态 | 含义 |
//! | --- | --- |
//! | `native` | 编译并运行通过，stdout 与 CPython 基线逐字节一致 |
//! | `unsupported` | 编译被拒（明确诊断），属于已知未实现能力 |
//! | `failed` | 编译通过但运行失败或输出不一致 |
//! | `not-evaluated` | 缺基线或入口样例，未运行 |
//!
//! 编译成功本身不算通过：必须真的运行并比对输出。
//!
//! 运行：
//! ```text
//! cargo run -p ryupy-e2e --bin projects            # 全部
//! cargo run -p ryupy-e2e --bin projects -- --only six,click
//! ```
//!
//! 结果写 `target/e2e/projects.jsonl`，同时打印汇总表。

use std::collections::BTreeMap;
use std::io::Write as _;
use std::path::{Path, PathBuf};
use std::process::ExitCode;

use ryupy_driver::dotnet::RunOptions;
use ryupy_driver::{BuildOptions, Session, dotnet, runtime::workspace_root};
use ryupy_e2e::json_escape;

/// 项目级状态。刻意与用例状态分开：项目的「未实现」是 `unsupported`，不是 `pending`。
#[derive(Clone, Copy, PartialEq, Eq, PartialOrd, Ord)]
enum ProjectStatus {
    Native,
    Unsupported,
    Failed,
    NotEvaluated,
}

impl ProjectStatus {
    fn as_str(self) -> &'static str {
        match self {
            ProjectStatus::Native => "native",
            ProjectStatus::Unsupported => "unsupported",
            ProjectStatus::Failed => "failed",
            ProjectStatus::NotEvaluated => "not-evaluated",
        }
    }
}

/// manifest 的一行。列定义见 `tests/projects/manifest.tsv` 表头。
struct Project {
    id: String,
    category: String,
    pypi: String,
    version: String,
    import_name: String,
    entry: PathBuf,
    scope: String,
    deps: String,
}

fn main() -> ExitCode {
    let mut only: Option<Vec<String>> = None;
    let mut args = std::env::args().skip(1);
    while let Some(arg) = args.next() {
        match arg.as_str() {
            "--only" => {
                let value = args.next().unwrap_or_default();
                only = Some(value.split(',').map(|s| s.trim().to_owned()).collect());
            }
            other => {
                eprintln!("未知参数：{other}（只支持 --only <id,id>）");
                return ExitCode::from(2);
            }
        }
    }

    let root = workspace_root();
    let projects_dir = root.join("tests/projects");
    let projects = match read_manifest(&projects_dir.join("manifest.tsv")) {
        Ok(list) => list,
        Err(err) => {
            eprintln!("读取项目清单失败：{err}");
            return ExitCode::from(2);
        }
    };

    let mut results = Vec::new();
    let mut counts: BTreeMap<ProjectStatus, usize> = BTreeMap::new();
    println!("{:<18} {:<14} {:<14} 阻塞 / 说明", "项目", "类别", "状态");
    for project in &projects {
        if let Some(only) = &only
            && !only.iter().any(|id| id == &project.id)
        {
            continue;
        }
        let (status, detail) = run_project(&root, &projects_dir, project);
        *counts.entry(status).or_insert(0) += 1;
        println!(
            "{:<18} {:<14} {:<14} {}",
            project.id,
            project.category,
            status.as_str(),
            detail
        );
        results.push((project, status, detail));
    }

    let path = root.join("target/e2e/projects.jsonl");
    if let Err(err) = write_results(&path, &results) {
        eprintln!("写入结果失败：{err}");
    }

    println!("\n汇总：");
    for (status, count) in &counts {
        println!("  {:<14} {count}", status.as_str());
    }
    let native = counts.get(&ProjectStatus::Native).copied().unwrap_or(0);
    println!(
        "结果：{}；native {native}/{}（分母是固定项目集，不因失败而缩小）",
        path.display(),
        results.len()
    );

    // 项目集在 M2.0 阶段本来就不该全通过：这里只做进度报告，不作为提交门禁。
    ExitCode::SUCCESS
}

fn run_project(root: &Path, projects_dir: &Path, project: &Project) -> (ProjectStatus, String) {
    let baseline = projects_dir.join("baselines").join(&project.id);
    let expected_stdout = baseline.join("stdout.txt");
    if !project.entry.exists() || !expected_stdout.exists() {
        return (
            ProjectStatus::NotEvaluated,
            format!(
                "缺 {} 或基线（先跑 tests/run_projects_baseline.py）",
                project.entry.display()
            ),
        );
    }
    let expected = match std::fs::read(&expected_stdout) {
        Ok(bytes) => bytes,
        Err(err) => return (ProjectStatus::Failed, format!("读取基线失败：{err}")),
    };

    let out_dir = root.join("target/e2e/projects").join(&project.id);
    let mut sess = Session::default();
    let opts = BuildOptions {
        input: project.entry.clone(),
        out_dir: out_dir.clone(),
        aot: false,
    };
    let artifacts = match ryupy_driver::build(&mut sess, &opts) {
        Ok(artifacts) => artifacts,
        Err(err) => {
            // 编译被拒是可预期状态：把第一条诊断当作「下一项阻塞」。
            // 诊断文本里既有源码片段也有 `[E0100] Error: ...` 这样的标题行，优先取后者。
            let rendered = sess.render_diagnostics(false);
            let first = rendered
                .lines()
                .find(|line| {
                    let trimmed = line.trim_start();
                    trimmed.starts_with('[') || trimmed.starts_with("Error:")
                })
                .or_else(|| rendered.lines().find(|line| !line.trim().is_empty()))
                .unwrap_or("（无诊断文本）");
            return (
                ProjectStatus::Unsupported,
                format!("{}（{err}）", first.trim()),
            );
        }
    };

    let run_opts = RunOptions {
        cwd: Some(out_dir.clone()),
        ..RunOptions::default()
    };
    match dotnet::run_dll_with(&artifacts.dll, &run_opts) {
        Ok(output) if !output.status.success() => {
            let stderr = String::from_utf8_lossy(&output.stderr);
            let last = stderr
                .lines()
                .rev()
                .find(|l| !l.trim().is_empty())
                .unwrap_or("（无 stderr）");
            (ProjectStatus::Failed, format!("运行失败：{last}"))
        }
        Ok(output) if output.stdout == expected => (
            ProjectStatus::Native,
            format!("输出一致（import {}）", project.import_name),
        ),
        Ok(output) => (
            ProjectStatus::Failed,
            format!(
                "输出不一致：期望 {} 字节，实际 {} 字节",
                expected.len(),
                output.stdout.len()
            ),
        ),
        Err(err) => (ProjectStatus::Failed, format!("启动 dotnet 失败：{err}")),
    }
}

fn write_results(
    path: &Path,
    results: &[(&Project, ProjectStatus, String)],
) -> std::io::Result<()> {
    if let Some(parent) = path.parent() {
        std::fs::create_dir_all(parent)?;
    }
    let mut buffer = Vec::new();
    for (project, status, detail) in results {
        let detail: String = detail.chars().take(2000).collect();
        writeln!(
            buffer,
            "{{\"id\":\"{}\",\"pypi\":\"{}\",\"version\":\"{}\",\"status\":\"{}\",\"entry\":\"{}\",\"scope\":\"{}\",\"deps\":\"{}\",\"detail\":\"{}\"}}",
            json_escape(&project.id),
            json_escape(&project.pypi),
            json_escape(&project.version),
            status.as_str(),
            json_escape(&project.entry.display().to_string()),
            json_escape(&project.scope),
            json_escape(&project.deps),
            json_escape(&detail),
        )?;
    }
    std::fs::write(path, buffer)
}

fn read_manifest(path: &Path) -> Result<Vec<Project>, String> {
    let text = std::fs::read_to_string(path).map_err(|e| format!("{}：{e}", path.display()))?;
    let text = text.trim_start_matches('\u{feff}');
    let mut out = Vec::new();
    for (lineno, raw) in text.lines().enumerate() {
        let line = raw.trim_end();
        if line.trim().is_empty() || line.starts_with('#') || line.starts_with("id\t") {
            continue;
        }
        let cols: Vec<&str> = line.split('\t').collect();
        if cols.len() < 8 {
            return Err(format!("第 {} 行列数不足（需要 8 列）：{line}", lineno + 1));
        }
        out.push(Project {
            id: cols[0].to_owned(),
            category: cols[1].to_owned(),
            pypi: cols[2].to_owned(),
            version: cols[3].to_owned(),
            import_name: cols[4].to_owned(),
            entry: path.parent().unwrap_or(Path::new(".")).join(cols[5]),
            scope: cols[6].to_owned(),
            deps: cols[7].to_owned(),
        });
    }
    if out.is_empty() {
        return Err("清单为空".to_owned());
    }
    Ok(out)
}
