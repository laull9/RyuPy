//! 叶内联必须真实命中身份/类型/溢出守卫；多模块公开重绑定与异常帧用执行结果验收。
use ryupy_driver::{Session, Stage};
mod support;

const SOURCE: &str = "def twice(value):\n    return value + value\nalias = twice\ndef invoke():\n    return alias(7)\nprint(invoke())\n";
#[test]
fn alias_call_has_guarded_inline_and_original_dynamic_fallback() {
    let input = std::env::temp_dir().join("ryupy_leaf_inline.py");
    std::fs::write(&input, SOURCE).unwrap();
    let mut session = Session {
        experimental_leaf_inline: true,
        ..Session::default()
    };
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    let caller = mir
        .split("fn invoke(")
        .nth(1)
        .unwrap()
        .split("fn main(")
        .next()
        .unwrap();
    assert!(
        caller.contains("WrappingAdd")
            && caller.contains("is_boxed_i64")
            && caller.contains("unbox_exact_i64"),
        "未真正命中叶调用特化：{caller}"
    );
    assert!(caller.contains("dyn.call("), "不能丢失原动态fallback");
    insta::assert_snapshot!("guarded_leaf_mir", mir);
    std::fs::remove_file(input).unwrap();
}
#[test]
fn identity_fallback_preserves_original_callable_error_frames() {
    let root = ryupy_driver::runtime::workspace_root().join("target/test_leaf_inline_traceback");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("source.py");
    std::fs::write(&input,"def twice(value):\n    return value + value\nalias = twice\ndef invoke():\n    return alias(7)\nclass Raises:\n    def __call__(self, value):\n        raise ValueError('leaf failure')\nalias = Raises()\ninvoke()\n").unwrap();
    let mut session = Session {
        experimental_leaf_inline: true,
        ..Session::default()
    };
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(mir.contains("WrappingAdd"));
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert_eq!(actual.status.code(), Some(1));
    let error = String::from_utf8_lossy(&actual.stderr);
    for expected in [
        "line 8, in __call__",
        "line 5, in invoke",
        "ValueError: leaf failure",
    ] {
        assert!(error.contains(expected), "{error}");
    }
    assert!(!error.contains("System."));
}
#[test]
fn public_rebinding_and_argument_side_effects_match_pinned_python() {
    let root = ryupy_driver::runtime::workspace_root();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = std::process::Command::new(&python)
        .arg("--version")
        .output()
        .unwrap();
    let pinned = std::fs::read_to_string(root.join("tests/pinned-python.txt")).unwrap();
    assert!(String::from_utf8_lossy(&version.stdout).contains(pinned.trim()));
    for (index, path) in [
        "tests/e2e/phase3/m3_9_leaf_calls.py",
        "tests/cases/optimization/leaf_identity_rebinding/entry.py",
    ]
    .into_iter()
    .enumerate()
    {
        let input = root.join(path);

        let expected = std::process::Command::new(&python)
            .arg(&input)
            .output()
            .unwrap();
        assert!(
            expected.status.success(),
            "{}",
            String::from_utf8_lossy(&expected.stderr)
        );
        for enabled in [false, true] {
            let mut session = Session {
                experimental_leaf_inline: enabled,
                ..Session::default()
            };
            let artifact = ryupy_driver::build(
                &mut session,
                &ryupy_driver::BuildOptions {
                    input: input.clone(),
                    out_dir: root.join(format!("target/test_leaf_identity/{index}/{enabled}")),
                    aot: false,
                    search_paths: vec![],
                },
            )
            .unwrap();
            let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
            assert_eq!(
                actual.status.code(),
                expected.status.code(),
                "{}",
                String::from_utf8_lossy(&actual.stderr)
            );
            assert_eq!(actual.stdout, expected.stdout);
            assert_eq!(actual.stderr, expected.stderr);
        }
    }
}

#[test]
fn inlined_success_still_obeys_original_call_recursion_limit() {
    let root = ryupy_driver::runtime::workspace_root().join("target/test_leaf_depth");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("source.py");
    std::fs::write(&input, SOURCE).unwrap();
    let mut session = Session {
        experimental_leaf_inline: true,
        ..Session::default()
    };
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(mir.contains("WrappingAdd") && mir.contains("TraceCheckDepth"));
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    for (limit, success) in [(2, false), (3, true)] {
        let actual = ryupy_driver::dotnet::run_dll_with(
            &artifact.dll,
            &ryupy_driver::dotnet::RunOptions {
                env: vec![("RYUPY_RECURSION_LIMIT".into(), limit.to_string())],
                ..Default::default()
            },
        )
        .unwrap();
        assert_eq!(actual.status.success(), success);
        let error = String::from_utf8_lossy(&actual.stderr);
        if success {
            assert_eq!(actual.stdout, b"14\n");
        } else {
            assert!(error.contains("RecursionError: maximum recursion depth exceeded"));
            // 原PushFrame直接抛限额异常而不Attach；基线当前只报告末行，不虚构帧。
            assert_eq!(
                error.trim(),
                "RecursionError: maximum recursion depth exceeded"
            );
            assert!(
                !error.contains("in twice"),
                "PushFrame检查失败不得凭空增加callee帧：{error}"
            );
        }
    }
}

#[test]
fn unknown_arguments_and_mixed_boolean_join_are_not_specialized() {
    let input = std::env::temp_dir().join("ryupy_leaf_unknown.py");
    std::fs::write(&input,"def twice(value):\n    return value + value\nalias = twice\ndef unknown(value):\n    return alias(value)\ndef mixed(flag):\n    if flag:\n        value = 7\n    else:\n        value = True\n    return alias(value)\ndef exact(flag):\n    if flag:\n        value = 7\n    else:\n        value = 9\n    return alias(value)\nprint(unknown(7), mixed(True), mixed(False), exact(False))\n").unwrap();
    let mut session = Session {
        experimental_leaf_inline: true,
        ..Session::default()
    };
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    for (name, next) in [("unknown", "mixed"), ("mixed", "exact")] {
        let body = mir
            .split(&format!("fn {name}("))
            .nth(1)
            .unwrap()
            .split(&format!("fn {next}("))
            .next()
            .unwrap();
        assert!(
            !body.contains("WrappingAdd"),
            "未知或bool参数不该特化：{body}"
        );
    }
    let exact = mir
        .split("fn exact(")
        .nth(1)
        .unwrap()
        .split("fn main(")
        .next()
        .unwrap();
    assert!(
        exact.contains("WrappingAdd"),
        "两支精确Int64的合流必须保留证明：{exact}"
    );
    std::fs::remove_file(input).unwrap();
}

#[test]
fn default_pipeline_keeps_original_dynamic_alias_call() {
    let input = std::env::temp_dir().join("ryupy_default_leaf_inline.py");
    std::fs::write(&input, SOURCE).unwrap();
    let mut session = Session::default();
    assert!(!session.experimental_leaf_inline);
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(mir.contains("dyn.call("));
    assert!(
        !mir.contains("WrappingAdd")
            && !mir.contains("TraceCheckDepth")
            && !mir.contains("unbox_exact_i64")
    );
    std::fs::remove_file(input).unwrap();
}

#[test]
fn both_experiments_preserve_exact_facts_and_real_result() {
    let root = ryupy_driver::runtime::workspace_root().join("target/test_both_m3_9_experiments");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("source.py");
    std::fs::write(&input,"def twice(value):\n    return value + value\nalias = twice\ndef sum_twice(n):\n    total = 0\n    for index in range(n):\n        total += alias(index)\n    return total\nprint(sum_twice(10))\n").unwrap();
    let mut session = Session {
        experimental_direct_accumulator: true,
        experimental_leaf_inline: true,
        ..Session::default()
    };
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(
        mir.matches("WrappingAdd").count() >= 2,
        "两实验必须均命中：{mir}"
    );
    assert!(mir.contains("TraceCheckDepth") && mir.contains("RuntimeFn::DynBinary(const 257_i64,"));
    assert!(!mir.contains("NumericAccumulator"));
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert!(
        result.status.success(),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert_eq!(result.stdout, b"90\n");
    assert!(result.stderr.is_empty());
}
