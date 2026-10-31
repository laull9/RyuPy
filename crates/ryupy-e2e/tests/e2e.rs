//! 端到端差分测试：同一份 `.py` 分别用 CPython 和 RyuPy 运行，比较 stdout 与退出状态。
//!
//! 用例位于 `<workspace>/tests/e2e/**/*.py`，每个文件是一个独立测试。
//! 文件开头的注释指令：
//! - `# ryupy: pending` —— 功能尚未实现，测试标记为 ignored（`make e2e-all` 可强制运行）。
//!
//! 运行：
//! ```text
//! cargo test -p ryupy-e2e            # 全部非 pending 用例
//! cargo test -p ryupy-e2e -- fib     # 按名字过滤
//! cargo test -p ryupy-e2e -- --include-ignored
//! ```

use std::path::{Path, PathBuf};
use std::process::Command;

use libtest_mimic::{Arguments, Failed, Trial};
use ryupy_driver::{BuildOptions, Session, dotnet, runtime::workspace_root};

fn main() {
    let args = Arguments::from_args();
    let root = workspace_root();
    let cases_dir = root.join("tests/e2e");

    let mut cases = Vec::new();
    collect_py_files(&cases_dir, &mut cases);
    cases.sort();

    let trials = cases
        .into_iter()
        .map(|path| {
            let name = path
                .strip_prefix(&cases_dir)
                .unwrap()
                .with_extension("")
                .display()
                .to_string();
            let pending = is_pending(&path);
            let out_dir = root.join("target/e2e").join(&name);
            Trial::test(name, move || run_case(&path, &out_dir)).with_ignored_flag(pending)
        })
        .collect();

    libtest_mimic::run(&args, trials).exit();
}

fn collect_py_files(dir: &Path, out: &mut Vec<PathBuf>) {
    let Ok(entries) = std::fs::read_dir(dir) else {
        return;
    };
    for entry in entries.flatten() {
        let path = entry.path();
        if path.is_dir() {
            collect_py_files(&path, out);
        } else if path.extension().is_some_and(|e| e == "py") {
            out.push(path);
        }
    }
}

fn is_pending(path: &Path) -> bool {
    std::fs::read_to_string(path).is_ok_and(|src| {
        src.lines()
            .take_while(|l| l.starts_with('#'))
            .any(|l| l.trim() == "# ryupy: pending")
    })
}

fn run_case(path: &Path, out_dir: &Path) -> Result<(), Failed> {
    // 1. 期望结果：CPython
    let python = std::env::var("PYTHON").unwrap_or_else(|_| "python3".to_owned());
    let expected = Command::new(&python)
        .arg(path)
        .output()
        .map_err(|e| format!("无法运行 {python}：{e}"))?;
    if !expected.status.success() {
        return Err(format!(
            "用例本身在 CPython 下失败：\n{}",
            String::from_utf8_lossy(&expected.stderr)
        )
        .into());
    }

    // 2. 实际结果：RyuPy 编译 + dotnet 运行
    let mut sess = Session::default();
    let opts = BuildOptions {
        input: path.to_path_buf(),
        out_dir: out_dir.to_path_buf(),
        aot: false,
    };
    let artifacts = ryupy_driver::build(&mut sess, &opts)
        .map_err(|e| format!("编译失败：{e:#}\n{}", sess.render_diagnostics(false)))?;
    let actual = dotnet::run_dll(&artifacts.dll).map_err(|e| format!("{e:#}"))?;

    // 3. 比较
    let (exp, act) = (
        String::from_utf8_lossy(&expected.stdout),
        String::from_utf8_lossy(&actual.stdout),
    );
    if exp != act || !actual.status.success() {
        return Err(format!(
            "输出不一致\n--- CPython ---\n{exp}\n--- RyuPy (exit: {}) ---\n{act}\n--- RyuPy stderr ---\n{}",
            actual.status,
            String::from_utf8_lossy(&actual.stderr)
        )
        .into());
    }
    Ok(())
}
