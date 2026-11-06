//! 端到端测试框架的公共部分：用例发现、清单解析、期望匹配与结果记录。
//!
//! # 为什么要有这一层
//!
//! Phase 1 的 e2e 只做一件事：单文件、无参数、比较 stdout 与退出成功。Phase 2 起需要
//! 覆盖多文件程序、命令行参数、环境变量、标准输入，以及**错误路径**（退出码、异常类型、
//! 消息与异常链）。这些能力要同时供两处使用：
//!
//! - `tests/e2e.rs`（libtest-mimic，开发循环里的差分测试）；
//! - `src/bin/projects.rs`（项目级运行入口，产出机器可读结果）。
//!
//! 所以发现、解析、匹配、记录都放在这个库里，两边只负责「怎么跑」。
//!
//! # 用例形态
//!
//! ## 单文件用例
//!
//! `tests/e2e/phaseN/*.py`：一个文件就是一个入口，无参数、期望成功退出、stdout 与
//! CPython 逐字节一致。文件首部注释区出现 `# ryupy: pending` 表示功能未实现。
//!
//! ## 目录用例
//!
//! `tests/cases/**/case.txt`：清单加同目录的入口文件（默认 `entry.py`）。目录里其它
//! `.py` 是**辅助文件**（被入口导入的 fixture），不会被当成独立用例；这样多文件 fixture
//! 与测试入口的界限由清单决定，而不是靠目录名约定。
//!
//! 清单是行式键值格式，便于人工审阅，也不引入额外依赖：
//!
//! ```text
//! # 注释
//! entry = entry.py                 # 相对用例目录；缺省时自动选择
//! pending = true                   # 功能未实现，RyuPy 侧跳过
//! args = --name value "a b"        # 命令行参数（支持引号）
//! instance_module = example        # 已在图内源码的实例能力，可重复
//! env = KEY=VALUE                  # 可重复
//! stdin = 12\n34                   # 支持 \n \t \r \\ \" 转义
//! cwd = work                       # case（默认，隔离目录）| <相对路径>
//! expect_exit = 0 | nonzero | 1    # 默认 0
//! expect_exception = ZeroDivisionError: division by zero
//! expect_cause = ValueError: source
//! expect_stdout = exact | ignore   # 默认 exact
//! timeout = 30
//! ```
//!
//! `expect_exception` 的消息按**子串**匹配：CPython 的异常消息常带额外上下文。
//! traceback 只比较最后一行与 `cause` 链，不比较框架自身栈帧的差异。

use std::io::{ErrorKind, Write};
use std::path::{Path, PathBuf};
use std::process::{Command, Output, Stdio};

mod results;
pub use results::{CaseResult, Status, json_escape, write_jsonl, write_results};

// ───────────────────────────── 用例模型 ─────────────────────────────

/// 一个可执行的用例。
#[derive(Debug, Clone)]
pub struct CaseSpec {
    /// 显式编译能力，不是Python运行实参，也不自动包含源码。
    pub instance_modules: Vec<String>,
    /// 相对用例根目录的名字，例如 `cases/errors/unhandled_chain`。
    pub name: String,
    pub dir: PathBuf,
    pub entry: PathBuf,
    pub args: Vec<String>,
    pub env: Vec<(String, String)>,
    pub stdin: Option<String>,
    /// 相对路径表示「相对用例目录」；`None` 表示由调用方提供隔离工作目录。
    pub cwd: Option<PathBuf>,
    pub pending: bool,
    pub expected: Expectation,
    pub timeout_secs: u64,
}

/// 错误路径的期望。
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Expectation {
    pub exit: ExitExpect,
    pub stdout: StdoutExpect,
    /// 期望的异常类型与消息子串，例如 `("ZeroDivisionError", "division by zero")`。
    pub exception: Option<(String, String)>,
    /// 期望在 stderr 任意位置出现的 cause 行，例如 `("ValueError", "source")`。
    pub cause: Option<(String, String)>,
}

impl Default for Expectation {
    fn default() -> Self {
        Self {
            exit: ExitExpect::Code(0),
            stdout: StdoutExpect::Exact,
            exception: None,
            cause: None,
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ExitExpect {
    Code(i32),
    NonZero,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum StdoutExpect {
    Exact,
    Ignore,
}

impl CaseSpec {
    /// 单文件用例：把 `.py` 文件包成默认期望的用例。
    pub fn from_single_file(root: &Path, path: &Path) -> Self {
        let name = path
            .strip_prefix(root)
            .unwrap_or(path)
            .with_extension("")
            .display()
            .to_string();
        Self {
            name,
            dir: path.parent().unwrap_or(Path::new(".")).to_path_buf(),
            entry: path.to_path_buf(),
            instance_modules: Vec::new(),
            args: Vec::new(),
            env: Vec::new(),
            stdin: None,
            cwd: None,
            pending: file_is_pending(path),
            expected: Expectation::default(),
            timeout_secs: 30,
        }
    }

    /// 解析 `case.txt` 所在的目录。
    pub fn from_case_dir(root: &Path, dir: &Path) -> Result<Self, String> {
        let manifest = dir.join("case.txt");
        let text = std::fs::read_to_string(&manifest)
            .map_err(|e| format!("无法读取 {}：{e}", manifest.display()))?;
        let mut spec = CaseSpec {
            name: dir.strip_prefix(root).unwrap_or(dir).display().to_string(),
            dir: dir.to_path_buf(),
            entry: dir.join("entry.py"),
            instance_modules: Vec::new(),
            args: Vec::new(),
            env: Vec::new(),
            stdin: None,
            cwd: None,
            pending: false,
            expected: Expectation::default(),
            timeout_secs: 30,
        };
        let mut entry_set = false;

        for (lineno, raw) in text.lines().enumerate() {
            let line = raw.trim();
            if line.is_empty() || line.starts_with('#') {
                continue;
            }
            let Some((key, value)) = line.split_once('=') else {
                return Err(format!("case.txt 第 {} 行缺少 `=`：{line}", lineno + 1));
            };
            let key = key.trim();
            let value = value.trim();
            match key {
                "instance_module" => {
                    if value.is_empty() {
                        return Err("instance_module 不能为空".into());
                    }
                    spec.instance_modules.push(value.to_owned());
                }
                "entry" => {
                    spec.entry = dir.join(value);
                    entry_set = true;
                }
                "pending" => spec.pending = parse_bool(value)?,
                "args" => spec.args = tokenize_args(value)?,
                "env" => {
                    let Some((k, v)) = value.split_once('=') else {
                        return Err(format!("env 需要 NAME=VALUE 形式：{value}"));
                    };
                    spec.env.push((k.trim().to_owned(), v.trim().to_owned()));
                }
                "stdin" => spec.stdin = Some(unescape(value)?),
                "cwd" => {
                    spec.cwd = if value == "case" {
                        None
                    } else {
                        Some(PathBuf::from(value))
                    }
                }
                "expect_exit" => {
                    spec.expected.exit = match value {
                        "nonzero" => ExitExpect::NonZero,
                        other => match other.parse::<i32>() {
                            Ok(code) => ExitExpect::Code(code),
                            Err(_) => return Err(format!("expect_exit 取值非法：{other}")),
                        },
                    }
                }
                "expect_exception" => spec.expected.exception = Some(parse_exception(value)?),
                "expect_cause" => spec.expected.cause = Some(parse_exception(value)?),
                "expect_stdout" => {
                    spec.expected.stdout = match value {
                        "exact" => StdoutExpect::Exact,
                        "ignore" => StdoutExpect::Ignore,
                        other => return Err(format!("expect_stdout 取值非法：{other}")),
                    }
                }
                "timeout" => {
                    spec.timeout_secs = value
                        .parse()
                        .map_err(|_| format!("timeout 取值非法：{value}"))?
                }
                other => return Err(format!("case.txt 出现未知键 `{other}`")),
            }
        }

        if !entry_set {
            spec.entry = default_entry(dir)?;
        }
        if !spec.entry.exists() {
            return Err(format!("入口文件不存在：{}", spec.entry.display()));
        }
        Ok(spec)
    }

    /// 运行时的工作目录：`None` 时使用调用方给的隔离目录。
    pub fn resolve_cwd(&self, isolated: &Path) -> PathBuf {
        match &self.cwd {
            Some(rel) => self.dir.join(rel),
            None => isolated.to_path_buf(),
        }
    }
}

fn default_entry(dir: &Path) -> Result<PathBuf, String> {
    let candidate = dir.join("entry.py");
    if candidate.exists() {
        return Ok(candidate);
    }
    let mut pys: Vec<PathBuf> = std::fs::read_dir(dir)
        .map_err(|e| format!("无法读取用例目录 {}：{e}", dir.display()))?
        .flatten()
        .map(|e| e.path())
        .filter(|p| p.is_file() && p.extension().is_some_and(|e| e == "py"))
        .collect();
    pys.sort();
    match pys.len() {
        1 => Ok(pys.remove(0)),
        0 => Err(format!("{} 下没有 .py 入口文件", dir.display())),
        _ => Err(format!(
            "{} 下有多个 .py，但未用 `entry = ` 声明入口（其余视为辅助文件）",
            dir.display()
        )),
    }
}

fn parse_bool(value: &str) -> Result<bool, String> {
    match value {
        "true" | "yes" => Ok(true),
        "false" | "no" => Ok(false),
        other => Err(format!("布尔取值非法：{other}")),
    }
}

/// `Type: message`，消息允许为空。
fn parse_exception(value: &str) -> Result<(String, String), String> {
    let Some((ty, msg)) = value.split_once(':') else {
        return Err(format!("需要 `类型: 消息` 形式：{value}"));
    };
    if ty.trim().is_empty() {
        return Err(format!("异常类型不能为空：{value}"));
    }
    Ok((ty.trim().to_owned(), msg.trim().to_owned()))
}

/// 简易 shell 风格切分：空白分隔，支持单/双引号包裹与 `\` 转义。
pub fn tokenize_args(input: &str) -> Result<Vec<String>, String> {
    let mut out = Vec::new();
    let mut cur = String::new();
    let mut in_token = false;
    let mut chars = input.chars().peekable();
    while let Some(ch) = chars.next() {
        match ch {
            '\\' => {
                if let Some(next) = chars.next() {
                    cur.push(next);
                    in_token = true;
                }
            }
            '"' | '\'' => {
                let quote = ch;
                in_token = true;
                loop {
                    match chars.next() {
                        Some(c) if c == quote => break,
                        Some(c) => cur.push(c),
                        None => return Err(format!("引号未闭合：{input}")),
                    }
                }
            }
            c if c.is_whitespace() => {
                if in_token {
                    out.push(std::mem::take(&mut cur));
                    in_token = false;
                }
            }
            c => {
                cur.push(c);
                in_token = true;
            }
        }
    }
    if in_token {
        out.push(cur);
    }
    Ok(out)
}

/// 处理 `\n` `\t` `\r` `\\` `\"` 五种转义，其余原样保留（避免吞掉正则里的反斜杠）。
pub fn unescape(input: &str) -> Result<String, String> {
    let mut out = String::new();
    let mut chars = input.chars();
    while let Some(ch) = chars.next() {
        if ch != '\\' {
            out.push(ch);
            continue;
        }
        match chars.next() {
            Some('n') => out.push('\n'),
            Some('t') => out.push('\t'),
            Some('r') => out.push('\r'),
            Some('\\') => out.push('\\'),
            Some('"') => out.push('"'),
            Some(other) => {
                out.push('\\');
                out.push(other);
            }
            None => out.push('\\'),
        }
    }
    Ok(out)
}

/// 文件首部注释区里是否有 `# ryupy: pending`。
pub fn file_is_pending(path: &Path) -> bool {
    std::fs::read_to_string(path).is_ok_and(|src| {
        src.lines()
            .take_while(|l| l.starts_with('#'))
            .any(|l| l.trim() == "# ryupy: pending")
    })
}

// ───────────────────────────── 发现 ─────────────────────────────

/// 收集单文件用例：`<root>/**/*.py`，跳过 `fixtures` 目录。
pub fn discover_single_files(root: &Path, out: &mut Vec<CaseSpec>) {
    collect_py(root, root, out);
}

fn collect_py(root: &Path, dir: &Path, out: &mut Vec<CaseSpec>) {
    let Ok(entries) = std::fs::read_dir(dir) else {
        return;
    };
    for entry in entries.flatten() {
        let path = entry.path();
        if path.is_dir() {
            if path.file_name().is_some_and(|n| n == "fixtures") {
                continue;
            }
            collect_py(root, &path, out);
        } else if path.extension().is_some_and(|e| e == "py") {
            out.push(CaseSpec::from_single_file(root, &path));
        }
    }
}

/// 收集目录用例：`<root>/**/case.txt` 所在目录。
pub fn discover_case_dirs(root: &Path) -> Result<Vec<CaseSpec>, String> {
    let mut dirs = Vec::new();
    collect_case_dirs(root, &mut dirs);
    dirs.sort();
    let mut out = Vec::new();
    for dir in dirs {
        out.push(CaseSpec::from_case_dir(root, &dir)?);
    }
    Ok(out)
}

fn collect_case_dirs(root: &Path, out: &mut Vec<PathBuf>) {
    let Ok(entries) = std::fs::read_dir(root) else {
        return;
    };
    for entry in entries.flatten() {
        let path = entry.path();
        if !path.is_dir() {
            continue;
        }
        if path.file_name().is_some_and(|n| n == "fixtures") {
            continue;
        }
        if path.join("case.txt").exists() {
            out.push(path);
        } else {
            collect_case_dirs(&path, out);
        }
    }
}

// ───────────────────────────── 执行 ─────────────────────────────

/// 用 CPython 运行用例。`cwd` 必须是调用方准备好的隔离目录。
pub fn run_cpython(python: &str, case: &CaseSpec, cwd: &Path) -> std::io::Result<Output> {
    let mut cmd = Command::new(python);
    cmd.arg("-B")
        .arg(&case.entry)
        .args(&case.args)
        .current_dir(cwd);
    for (k, v) in &case.env {
        cmd.env(k, v);
    }
    // 不写 pycache、不染色，避免环境差异混进比较结果。
    cmd.env("PYTHONDONTWRITEBYTECODE", "1");
    cmd.env("PYTHON_COLORS", "0");
    if case.stdin.is_some() {
        cmd.stdin(Stdio::piped());
    }
    cmd.stdout(Stdio::piped()).stderr(Stdio::piped());
    let mut child = cmd.spawn()?;
    if let Some(text) = &case.stdin {
        if let Some(mut stdin) = child.stdin.take() {
            stdin.write_all(text.as_bytes())?;
        }
    }
    child.wait_with_output()
}

/// 检查一次运行是否满足期望；返回 `Ok(())` 或失败原因。
pub fn check(expect: &Expectation, output: &Output) -> Result<(), String> {
    let stderr = String::from_utf8_lossy(&output.stderr);

    let exit_ok = match expect.exit {
        ExitExpect::Code(code) => output.status.code() == Some(code),
        ExitExpect::NonZero => !output.status.success(),
    };
    if !exit_ok {
        return Err(format!(
            "退出码不符：期望 {:?}，实际 {:?}\n--- stderr ---\n{stderr}",
            expect.exit,
            output.status.code()
        ));
    }

    if let Some((ty, msg)) = &expect.exception {
        let last = stderr
            .lines()
            .rev()
            .find(|l| !l.trim().is_empty())
            .unwrap_or_default();
        let ok = last.starts_with(&format!("{ty}:")) && (msg.is_empty() || last.contains(msg));
        if !ok {
            return Err(format!(
                "异常行不符：期望 `{ty}: {msg}`，实际最后一行 `{last}`\n--- stderr ---\n{stderr}"
            ));
        }
        if let Some((c_ty, c_msg)) = &expect.cause {
            let found = stderr.lines().any(|l| {
                l.contains(&format!("{c_ty}:")) && (c_msg.is_empty() || l.contains(c_msg))
            });
            if !found {
                return Err(format!(
                    "未找到异常链 `{c_ty}: {c_msg}`\n--- stderr ---\n{stderr}"
                ));
            }
        }
    }

    Ok(())
}

/// 比较两边的 stdout（成功用例的差分依据）。
pub fn diff_stdout(expected: &[u8], actual: &[u8]) -> Result<(), String> {
    let (exp, act) = (
        String::from_utf8_lossy(expected),
        String::from_utf8_lossy(actual),
    );
    if exp == act {
        Ok(())
    } else {
        Err(format!(
            "输出不一致\n--- CPython ---\n{exp}\n--- RyuPy ---\n{act}"
        ))
    }
}

/// 清空并创建隔离工作目录。
pub fn fresh_dir(path: &Path) -> std::io::Result<()> {
    match std::fs::remove_dir_all(path) {
        Ok(()) => {}
        Err(e) if e.kind() == ErrorKind::NotFound => {}
        Err(e) => return Err(e),
    }
    std::fs::create_dir_all(path)
}

#[cfg(test)]
#[path = "case_tests.rs"]
mod tests;
