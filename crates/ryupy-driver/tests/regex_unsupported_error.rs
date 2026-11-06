//! Ryu专用Unsupported诊断，锁真实用户函数调用行和完整context链；不是CP同错误差分。
//! 只构建一个新应用并运行一次；完整原始证据保留，运行预算由外层验证命令统一约束。
use ryupy_driver::{BuildOptions, Session};

#[test]
fn unsupported_regex_keeps_exact_user_call_lines_and_original_context() {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-regex-unsupported-{}-{stamp}",
        std::process::id()
    ));
    std::fs::create_dir(&root).unwrap();
    eprintln!("正则冷错误证据: {}", root.display());
    let input = root.join("unsupported_context.py");
    std::fs::write(
        &input,
        include_str!("../../../tests/fixtures/regex_errors/unsupported_context.py"),
    )
    .unwrap();
    let mut session = Session::default();
    let options = BuildOptions {
        input: input.clone(),
        out_dir: root.join("out"),
        aot: false,
        search_paths: vec![],
    };
    std::fs::write(root.join("build.options"), format!("{options:?}\n")).unwrap();
    let built = ryupy_driver::build(&mut session, &options);
    std::fs::write(
        root.join("build.diagnostics"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let artifact = match built {
        Ok(artifact) => {
            std::fs::write(root.join("build.status"), "success\n").unwrap();
            artifact
        }
        Err(error) => {
            std::fs::write(root.join("build.status"), format!("failed: {error:#}\n")).unwrap();
            panic!("正则诊断应用编译失败，不启动JIT: {error:#}");
        }
    };
    let command = format!(
        "{:?} {:?}\n",
        ryupy_driver::dotnet::dotnet_exe(),
        artifact.dll
    );
    std::fs::write(root.join("jit.command"), command).unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll);
    let output = match result {
        Ok(output) => output,
        Err(error) => {
            std::fs::write(root.join("jit.launch-error"), format!("{error:#}\n")).unwrap();
            panic!("正则诊断JIT启动失败: {error:#}");
        }
    };
    std::fs::write(root.join("jit.stdout"), &output.stdout).unwrap();
    std::fs::write(root.join("jit.stderr"), &output.stderr).unwrap();
    std::fs::write(
        root.join("jit.exit"),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
    // 当前driver的hir.info.file明确使用basename；锁真实输出，不变更生产路径插桩。
    let file = "unsupported_context.py";
    let expected = format!(
        "Traceback (most recent call last):\n  File \"{file}\", line 14, in <module>\nValueError: outer context\n\nDuring handling of the above exception, another exception occurred:\nTraceback (most recent call last):\n  File \"{file}\", line 16, in <module>\n  File \"{file}\", line 10, in outer\n  File \"{file}\", line 6, in leaf\nNotImplementedError: re: unsupported lookbehind, atomic, conditional, comment or scoped flags\n"
    );
    std::fs::write(root.join("expected.stderr"), &expected).unwrap();
    assert_eq!(output.status.code(), Some(1));
    assert!(output.stdout.is_empty());
    assert_eq!(String::from_utf8(output.stderr).unwrap(), expected);
}
