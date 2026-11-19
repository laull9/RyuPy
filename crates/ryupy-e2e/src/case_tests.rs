//! 用例清单解析与运行期结果检查的单元测试。

use super::*;

#[test]
fn tokenizes_quoted_args() {
    assert_eq!(
        tokenize_args("--name value \"a b\" 'c d' e\\ f").unwrap(),
        vec!["--name", "value", "a b", "c d", "e f"]
    );
    assert_eq!(tokenize_args("").unwrap(), Vec::<String>::new());
    assert!(tokenize_args("\"unterminated").is_err());
}

#[test]
fn unescapes_stdin() {
    assert_eq!(unescape("12\\n34\\t\\\\").unwrap(), "12\n34\t\\");
    // 未知转义保持原样，避免吞掉正则等文本里的反斜杠
    assert_eq!(unescape("\\d+").unwrap(), "\\d+");
}

#[test]
fn parses_exception_declaration() {
    assert_eq!(
        parse_exception("ZeroDivisionError: division by zero").unwrap(),
        (
            "ZeroDivisionError".to_owned(),
            "division by zero".to_owned()
        )
    );
    assert!(parse_exception("ZeroDivisionError").is_err());
}

#[test]
fn parses_case_dir_manifest() {
    let dir = std::env::temp_dir().join("ryupy-e2e-parse-case");
    let _ = std::fs::remove_dir_all(&dir);
    std::fs::create_dir_all(&dir).unwrap();
    std::fs::write(dir.join("entry.py"), "print(1)\n").unwrap();
    std::fs::write(
            dir.join("case.txt"),
            "# 注释\nentry = entry.py\ninstance_module = a\ninstance_module = b.child\nargs = a \"b c\"\nenv = K=V\nstdin = 1\\n2\n# 未知转义原样保留\nexpect_exit = nonzero\nexpect_exception = ValueError: bad\n",
        )
        .unwrap();

    let spec = CaseSpec::from_case_dir(&dir, &dir).unwrap();
    assert_eq!(spec.instance_modules, ["a", "b.child"]);
    assert_eq!(spec.args, vec!["a", "b c"]);
    assert_eq!(spec.env, vec![("K".to_owned(), "V".to_owned())]);
    assert_eq!(spec.stdin.as_deref(), Some("1\n2"));
    assert_eq!(spec.expected.exit, ExitExpect::NonZero);
    assert_eq!(
        spec.expected.exception,
        Some(("ValueError".to_owned(), "bad".to_owned()))
    );

    std::fs::remove_dir_all(&dir).unwrap();
}

/// 辅助文件不能被当成入口：同目录多个 .py 且未声明 entry 时必须报错。
#[test]
fn rejects_ambiguous_entry() {
    let dir = std::env::temp_dir().join("ryupy-e2e-ambiguous");
    let _ = std::fs::remove_dir_all(&dir);
    std::fs::create_dir_all(&dir).unwrap();
    std::fs::write(dir.join("case.txt"), "expect_exit = 0\n").unwrap();
    std::fs::write(dir.join("a.py"), "print(1)\n").unwrap();
    std::fs::write(dir.join("b.py"), "print(2)\n").unwrap();
    assert!(CaseSpec::from_case_dir(&dir, &dir).is_err());
    std::fs::remove_dir_all(&dir).unwrap();
}

fn output(code: i32, stdout: &str, stderr: &str) -> Output {
    use std::os::unix::process::ExitStatusExt;
    Output {
        status: std::process::ExitStatus::from_raw(code << 8),
        stdout: stdout.as_bytes().to_vec(),
        stderr: stderr.as_bytes().to_vec(),
    }
}

#[test]
fn matches_error_expectations() {
    let expect = Expectation {
        exit: ExitExpect::NonZero,
        stdout: StdoutExpect::Exact,
        exception: Some(("ValueError".to_owned(), "boom".to_owned())),
        cause: Some(("KeyError".to_owned(), "missing".to_owned())),
    };
    let stderr = "Traceback (most recent call last):\n  ...\nKeyError: 'missing'\n\nThe above exception was the direct cause of the following exception:\n\nTraceback (most recent call last):\nValueError: boom happened\n";
    assert!(check(&expect, &output(1, "", stderr)).is_ok());

    // 异常类型不符要失败
    assert!(check(&expect, &output(1, "", "ValueError: other\n")).is_err());
    // 退出码为 0 要失败
    assert!(check(&expect, &output(0, "", stderr)).is_err());
    // 缺 cause 要失败
    assert!(check(&expect, &output(1, "", "ValueError: boom happened\n")).is_err());
}

#[test]
fn escapes_json() {
    assert_eq!(json_escape("a\"b\\c\nd"), "a\\\"b\\\\c\\nd");
}
