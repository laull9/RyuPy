//! 在发布器真正拥有的候选目录注入审计写盘失败，保留编译、交付及实际入口运行合同。
use super::*;

#[test]
fn failed_stage_audit_write_keeps_the_compiled_application_runnable() {
    let root = crate::test_temp::Root::new("stage-audit-write");
    let input = root.join("main.py");
    std::fs::write(&input, "print(1)\n").unwrap();
    let mut binaries = Vec::new();
    for enabled in [false, true] {
        let opts = BuildOptions {
            input: input.clone(),
            out_dir: root.join(if enabled { "traced" } else { "plain" }),
            aot: false,
            search_paths: vec![],
        };
        let mut session = Session {
            diagnostic_stages: enabled,
            ..Default::default()
        };
        let artifacts = crate::cpython_jit::build(&opts, |candidate| {
            // 只在当前事务排他拥有的候选中构造同名目录；不接管用户非空输出目录。
            std::fs::create_dir(candidate.out_dir.join("module-environment.json"))?;
            build_with_resources_inner(&mut session, candidate, None, None)
        })
        .unwrap();
        assert!(session.diagnostics.is_empty());
        assert!(
            opts.out_dir
                .join("internal/module-environment.json")
                .is_dir()
        );
        binaries.push(std::fs::read(&artifacts.dll).unwrap());
        let actual = crate::dotnet::run_executable_with(
            artifacts.native_exe.as_ref().unwrap(),
            &crate::dotnet::RunOptions::default(),
        )
        .unwrap();
        assert!(
            actual.status.success(),
            "{}",
            String::from_utf8_lossy(&actual.stderr)
        );
        assert_eq!(actual.stdout, b"1\n");
        assert!(actual.stderr.is_empty());
    }
    assert_eq!(binaries[0], binaries[1]);
}
