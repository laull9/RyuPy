//! I64 Add短链：当前实际性能源码强命中、固定CP差分及完整用户异常帧。
//! 此测试草稿等待MIR策略接线；不把仅出现拆箱当作整链正确性证据。
use ryupy_driver::{BuildOptions, Session, Stage};
use std::path::{Path, PathBuf};
use std::process::Command;
mod support;

fn section<'a>(mir: &'a str, name: &str) -> &'a str {
    mir.split(&format!("fn {name}("))
        .nth(1)
        .unwrap_or_else(|| panic!("缺少被测函数 {name}"))
        .split("\nfn ")
        .next()
        .unwrap()
}

fn python() -> std::ffi::OsString {
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let output = Command::new(&python)
        .args(["-c", "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"])
        .output().unwrap();
    assert!(output.status.success());
    assert_eq!(
        String::from_utf8(output.stdout).unwrap().trim(),
        "cpython\n3.12.13"
    );
    python
}

fn build(input: &Path, directory: &str) -> PathBuf {
    let root = ryupy_driver::runtime::workspace_root();
    let mut session = Session::default();
    ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: input.into(),
            out_dir: root.join("target").join(directory),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap_or_else(|error| panic!("{error:#}\n{}", session.render_diagnostics(false)))
    .dll
}

#[test]
fn actual_measured_positional_function_has_checked_i64_chain() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/aot/perf_fixtures/calls.py");
    let mir = support::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
    let body = section(&mir, "positional");
    assert_chain(body, 2, 3);
    // 快路不能偷换原回退；两个原动态Add必须保留原顺序。
    assert_eq!(body.matches("dyn.Add(").count(), 2, "{body}");
    assert!(!body.contains("dyn.Mul") && !body.contains("dyn.Sub"));
}

fn assert_chain(body: &str, steps: usize, inputs: usize) {
    assert_eq!(body.matches("is_boxed_i64(").count(), inputs, "{body}");
    assert_eq!(body.matches("unbox_exact_i64(").count(), inputs, "{body}");
    assert_eq!(body.matches("WrappingAdd").count(), steps, "{body}");
    assert_eq!(
        body.matches("box<I64>(").count(),
        1,
        "每条链只物化最终i64：{body}"
    );
    // 每步必须独立检测有符号溢出，且失败都回同一原动态段。
    assert_eq!(body.matches("BitXor").count(), steps * 2, "{body}");
    assert_eq!(body.matches("BitAnd").count(), steps, "{body}");
    assert_eq!(body.matches("Ge(").count(), steps, "{body}");
    let lines: Vec<_> = body.lines().map(str::trim).collect();
    let mut fallback = None;
    for (index, line) in lines.iter().enumerate() {
        if let Some((condition, formula)) = line.split_once(" = Ge(") {
            assert!(formula.ends_with(", const 0_i64);"), "{line}");
            let branch = lines[index + 1];
            assert!(
                branch.starts_with(&format!("branch {condition} -> ")),
                "{branch}"
            );
            let target = branch
                .split_once(", else: ")
                .unwrap()
                .1
                .strip_suffix("];")
                .unwrap();
            if let Some(previous) = fallback {
                assert_eq!(target, previous, "所有overflow失败应回同一原段");
            }
            fallback = Some(target);
        }
    }
    let slow = body
        .split(&format!("    {}: {{", fallback.unwrap()))
        .nth(1)
        .unwrap()
        .split("\n    }")
        .next()
        .unwrap();
    assert_eq!(
        slow.matches("dyn.Add").count(),
        steps,
        "原fallback不完整：{slow}"
    );
}

#[test]
fn original_protocol_fixture_matches_pinned_python_with_real_hits() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/aot_i64_chain.py");
    let mir = support::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
    for (name, steps) in [("chain2", 2), ("chain3", 3), ("chain4", 4)] {
        assert_chain(section(&mir, name), steps, steps + 1);
    }
    for name in ["observed", "interrupted"] {
        assert!(
            !section(&mir, name).contains("WrappingAdd"),
            "观察/调用屏障不得拼链：{name}"
        );
    }
    let expected = Command::new(python()).arg(&input).output().unwrap();
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    let dll = build(&input, "test_integer_chain");
    let actual = ryupy_driver::dotnet::run_dll(&dll).unwrap();
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

/// 保留每一帧的文件basename/行/函数，不以contains掩盖额外或缺失帧。
fn traceback(bytes: &[u8]) -> (Vec<(String, usize, String)>, String) {
    let text = std::str::from_utf8(bytes).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut frames = Vec::new();
    for line in text.lines() {
        if let Some(frame) = line.trim_start().strip_prefix("File \"") {
            let (file, rest) = frame.split_once("\", line ").expect("帧路径格式");
            let (number, function) = rest.split_once(", in ").expect("帧行号/函数格式");
            frames.push((
                Path::new(file)
                    .file_name()
                    .unwrap()
                    .to_str()
                    .unwrap()
                    .to_owned(),
                number.parse().unwrap(),
                function.to_owned(),
            ));
        }
    }
    assert!(!frames.is_empty(), "不允许丢弃整个异常栈：{text}");
    (frames, text.lines().last().unwrap().to_owned())
}

#[test]
fn fallback_and_overflow_preserve_complete_user_traceback() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/fixtures/integer_chain_traceback.py");
    let mir = support::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
    assert_chain(section(&mir, "chain"), 2, 3);
    let dll = build(&input, "test_integer_chain_trace");
    let python = python();
    for (mode, method, message) in [
        ("add", "__add__", "chain-add"),
        ("right", "__radd__", "chain-right"),
    ] {
        let expected = Command::new(&python)
            .arg(&input)
            .arg(mode)
            .output()
            .unwrap();
        let actual = Command::new(ryupy_driver::dotnet::dotnet_exe())
            .arg(&dll)
            .arg(mode)
            .output()
            .unwrap();
        assert_eq!(expected.status.code(), Some(1));
        assert_eq!(actual.status.code(), expected.status.code());
        assert_eq!(actual.stdout, expected.stdout);
        let reference = traceback(&expected.stderr);
        assert_eq!(reference.0.len(), 3);
        assert_eq!(reference.0[2].2, method);
        assert_eq!(reference.1, format!("ValueError: {message}"));
        assert_eq!(traceback(&actual.stderr), reference);
    }
}
