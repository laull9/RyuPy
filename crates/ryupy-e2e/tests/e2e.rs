//! 端到端差分测试：同一份用例分别用 CPython 和 RyuPy 运行，比较输出与退出状态。
//!
//! 用例由两部分组成（见 `ryupy_e2e` 库文档）：
//! - `tests/e2e/**/*.py`：单文件成功用例，stdout 与 CPython 逐字节一致；
//! - `tests/cases/**/case.txt`：目录用例，可声明入口、参数、环境、标准输入与错误路径期望。
//!
//! 运行：
//! ```text
//! cargo test -p ryupy-e2e                 # 全部非 pending 用例
//! cargo test -p ryupy-e2e -- fib          # 按名字过滤
//! cargo test -p ryupy-e2e -- --include-ignored   # 含 pending，看整体进度
//! cargo test -p ryupy-e2e -- --test-threads=8    # 并行跑，输出顺序不再保证
//! E2E_PYTHON_ONLY=1 cargo test -p ryupy-e2e      # 只自检用例与框架（不编译）
//! ```
//!
//! 默认**串行**执行：用例按名字排序，输出顺序稳定，每条前面带 `[已跑/总数]` 进度。
//! 并行能让 `dotnet` 调用重叠，但完成顺序随机，输出会乱序；需要提速时再显式传
//! `--test-threads=N`。
//!
//! 每次运行都会把结果写进 `target/e2e/results.jsonl`（`name` / `status` / `detail`），
//! 供脚本统计；`pending` 不计入通过数，`unexpected-pass` 表示该去掉 pending 标记了。

use std::path::Path;
use std::sync::Mutex;

use libtest_mimic::{Arguments, Failed, Trial};
use ryupy_driver::dotnet::RunOptions;
use ryupy_driver::{BuildOptions, Session, dotnet, runtime::workspace_root};
use ryupy_e2e::{CaseResult, CaseSpec, Status, diff_stdout, fresh_dir};

/// 结果按完成顺序收集，最后统一写盘。libtest-mimic 会在多个线程里跑用例，
/// 所以必须是全局锁而不是 thread_local。
static RESULTS: Mutex<Vec<CaseResult>> = Mutex::new(Vec::new());

fn main() {
    let mut args = Arguments::from_args();
    // 默认串行：libtest-mimic 并行时按「完成顺序」打印，用例耗时不同就会乱序，
    // 也不方便判断还剩多少。串行开销很小（用例本身多在毫秒级），
    // 需要重叠 `dotnet` 调用时用户可显式传 `--test-threads=N`。
    args.test_threads.get_or_insert(1);
    let root = workspace_root();
    let python_only = std::env::var("E2E_PYTHON_ONLY").is_ok_and(|v| v == "1");

    let mut cases = Vec::new();
    ryupy_e2e::discover_single_files(&root.join("tests/e2e"), &mut cases);
    match ryupy_e2e::discover_case_dirs(&root.join("tests/cases")) {
        Ok(mut dirs) => cases.append(&mut dirs),
        Err(err) => {
            eprintln!("用例清单解析失败：{err}");
            std::process::exit(2);
        }
    }
    cases.sort_by(|a, b| a.name.cmp(&b.name));

    // 框架自检模式不依赖扩展包；Phase 4 用例需要 NumPy，走 `make python-extensions`。
    if python_only {
        cases.retain(|case| !case.name.starts_with("phase4/"));
    }

    let total = cases.len();
    // 前缀宽度取总数位数，保证 `[ 3/67]` 与 `[67/67]` 对齐。
    let index_width = total.to_string().len();
    let trials: Vec<Trial> = cases
        .into_iter()
        .enumerate()
        .map(|(index, case)| {
            let root = root.clone();
            // 名字前缀就是进度：串行模式下 libtest-mimic 会在用例**开始前**打印这一行，
            // 因此既能看到当前跑到第几个，也保证输出与名字排序一致。
            // 过滤仍按完整名字做子串匹配，前缀不影响 `cargo test -- fib` 这类用法。
            let label = format!(
                "[{:>width$}/{total}] {}",
                index + 1,
                case.name,
                width = index_width
            );
            let ignored = case.pending && !python_only;
            Trial::test(label, move || run_case(&root, &case, python_only))
                .with_ignored_flag(ignored)
        })
        .collect();

    let conclusion = libtest_mimic::run(&args, trials);

    // 汇总结果文件；即使有失败也写出，便于定位。
    let results = RESULTS.lock().unwrap_or_else(|e| e.into_inner());
    let path = root.join("target/e2e/results.jsonl");
    if let Err(err) = ryupy_e2e::write_results(&path, &results) {
        eprintln!("写入结果文件失败：{err}");
    } else {
        let passed = results
            .iter()
            .filter(|r| r.status == Status::Passed)
            .count();
        let pending = results
            .iter()
            .filter(|r| r.status == Status::Pending)
            .count();
        let unexpected = results
            .iter()
            .filter(|r| r.status == Status::UnexpectedPass)
            .count();
        println!(
            "\n结果记录：{} 通过，{pending} pending，{unexpected} 可转正 → {}",
            passed,
            path.display()
        );
    }
    drop(results);

    conclusion.exit();
}

fn run_case(root: &Path, case: &CaseSpec, python_only: bool) -> Result<(), Failed> {
    let out_dir = root.join("target/e2e").join(sanitize(&case.name));
    let work_dir = out_dir.join("work");
    fresh_dir(&work_dir).map_err(|e| format!("无法准备工作目录：{e}"))?;
    let cwd = case.resolve_cwd(&work_dir);

    // 1. 期望结果：CPython。顺带校验清单本身写得对不对（用例自检）。
    let python = std::env::var("PYTHON").unwrap_or_else(|_| "python3".to_owned());
    let expected = ryupy_e2e::run_cpython(&python, case, &cwd)
        .map_err(|e| format!("无法运行 {python}：{e}"))?;
    let case_self_check = if case.expected.exception.is_some() {
        // 错误用例：清单声明的退出码 / 异常 / 链必须与 CPython 实际行为一致。
        ryupy_e2e::check(&case.expected, &expected)
            .map_err(|e| format!("用例清单与 CPython 行为不符：{e}"))
    } else if expected.status.success() {
        Ok(())
    } else {
        Err(format!(
            "用例本身在 CPython 下失败：\n{}",
            String::from_utf8_lossy(&expected.stderr)
        ))
    };
    case_self_check?;

    if python_only {
        record(case, Status::Passed, "CPython 侧自检通过".to_owned());
        return Ok(());
    }

    // 2. 实际结果：RyuPy 编译 + dotnet 运行
    let mut sess = Session::default();
    let opts = BuildOptions {
        input: case.entry.clone(),
        out_dir: out_dir.clone(),
        aot: false,
        search_paths: Vec::new(),
    };
    let artifacts = match ryupy_driver::build(&mut sess, &opts) {
        Ok(a) => a,
        Err(e) => {
            let detail = format!("编译失败：{e:#}\n{}", sess.render_diagnostics(false));
            return report(case, &detail);
        }
    };

    let run_opts = RunOptions {
        args: case.args.clone(),
        env: case.env.clone(),
        cwd: Some(cwd),
        stdin: case.stdin.clone(),
    };
    let actual = dotnet::run_dll_with(&artifacts.dll, &run_opts).map_err(|e| format!("{e:#}"))?;

    // 3. 比较
    let mut failures = Vec::new();
    if let Err(e) = ryupy_e2e::check(&case.expected, &actual) {
        failures.push(e);
    }
    if case.expected.stdout == ryupy_e2e::StdoutExpect::Exact {
        if let Err(e) = diff_stdout(&expected.stdout, &actual.stdout) {
            failures.push(e);
        }
    }
    if failures.is_empty() {
        report_pass(case);
        return Ok(());
    }

    let detail = format!(
        "{}\n--- RyuPy (exit: {}) ---\n{}",
        failures.join("\n"),
        actual.status,
        String::from_utf8_lossy(&actual.stderr)
    );
    report(case, &detail)
}

/// 失败时按 pending 标记决定「预期未实现」还是「真失败」。
fn report(case: &CaseSpec, detail: &str) -> Result<(), Failed> {
    if case.pending {
        record(case, Status::Pending, detail.to_owned());
        Ok(())
    } else {
        record(case, Status::Failed, detail.to_owned());
        Err(detail.to_owned().into())
    }
}

fn report_pass(case: &CaseSpec) {
    if case.pending {
        // 功能已经可用：提示去掉 pending 标记，否则它会被长期跳过。
        eprintln!("\n[可转正] {} 已通过，请去掉 `# ryupy: pending`", case.name);
        record(
            case,
            Status::UnexpectedPass,
            "已通过，待去 pending".to_owned(),
        );
    } else {
        record(case, Status::Passed, String::new());
    }
}

fn record(case: &CaseSpec, status: Status, detail: String) {
    let mut guard = RESULTS.lock().unwrap_or_else(|e| e.into_inner());
    guard.push(CaseResult {
        name: case.name.clone(),
        status,
        detail,
    });
}

/// 用例名里的 `/` 用来分层目录，落到产物路径需要保持可读且合法。
fn sanitize(name: &str) -> String {
    name.replace("..", "_")
}
