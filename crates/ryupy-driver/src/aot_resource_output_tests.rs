//! 真实build入口在首次/重复构建均必须在SDK、发射、资源复制之前失败。
use super::*;

#[test]
fn overlapping_request_rejects_before_emit_and_preserves_dist() {
    for previous in [false, true] {
        let mut c = Case::new();
        c.opts.input = c.root.join("app.py");
        // 解析可通过但MIR不支持的语法：如继续到编译，会产生不同错误。
        std::fs::write(&c.opts.input, "print([(x := n) for n in [1]])\n").unwrap();
        c.opts.out_dir = c.root.clone();
        let old = if previous {
            Some(build(&c.opts, |opts, _| fake(opts, b"old-native", 1)).unwrap())
        } else {
            None
        };
        let old_dist = if previous {
            Some(files::snapshot(&c.root.join("app.dist"), false).unwrap())
        } else {
            None
        };
        let request = c.root.join("resources.json");
        std::fs::write(&request, br#"{"schema_version":1,"modules":[{"module":"__main__","trees":[{"directory":".","suffixes":[".json"]}]}]}"#).unwrap();
        let mut session = crate::Session::default();
        let error = crate::build_with_resources(&mut session, &c.opts, Some(&request))
            .err()
            .unwrap();
        let error = format!("{error:#}");
        assert!(error.contains("生成输出重叠"), "{error}");
        assert!(error.contains("__main__") && error.contains("directory"));
        assert!(!error.contains("SDK") && session.diagnostics.is_empty());
        let build_root = c.root.join("app.build");
        let attempts: Vec<_> = std::fs::read_dir(&build_root)
            .unwrap()
            .map(|e| e.unwrap().path())
            .filter(|p| p.join("build-error.log").is_file())
            .collect();
        assert_eq!(attempts.len(), 1);
        let mut entries: Vec<_> = std::fs::read_dir(&attempts[0])
            .unwrap()
            .map(|e| e.unwrap().file_name())
            .collect();
        // 用户catalog在语义/发射之前保存来源审计；仅该报告与失败日志可存在。
        entries.sort();
        assert_eq!(entries, ["app.source-plan.json", "build-error.log"]);
        let report: serde_json::Value = serde_json::from_slice(
            &std::fs::read(attempts[0].join("app.source-plan.json")).unwrap(),
        )
        .unwrap();
        assert_eq!(report["compiler_verified"], false);
        if let Some(expected) = old_dist {
            assert_eq!(
                files::snapshot(&c.root.join("app.dist"), false).unwrap(),
                expected
            );
            assert_eq!(
                std::fs::read(old.unwrap().native_exe.unwrap()).unwrap(),
                b"old-native"
            );
        } else {
            assert!(!c.root.join("app.dist").exists());
        }
    }
}

#[cfg(unix)]
#[test]
fn fake_sdk_never_started_in_isolated_process() {
    use std::os::unix::fs::PermissionsExt;
    let c = Case::new();
    let marker = c.root.join("sdk-started");
    let sdk = c.root.join("fake-dotnet");
    std::fs::write(
        &sdk,
        "#!/bin/sh\nprintf started > \"$SDK_MARKER\"\nexit 91\n",
    )
    .unwrap();
    std::fs::set_permissions(&sdk, std::fs::Permissions::from_mode(0o755)).unwrap();
    let output = std::process::Command::new(std::env::current_exe().unwrap())
        .args(["--exact", "aot_layout::tests::resource_output_tests::overlapping_request_rejects_before_emit_and_preserves_dist", "--nocapture"])
        .env("DOTNET", sdk).env("SDK_MARKER", &marker).output().unwrap();
    assert!(
        output.status.success(),
        "{}\n{}",
        String::from_utf8_lossy(&output.stdout),
        String::from_utf8_lossy(&output.stderr)
    );
    let stdout = String::from_utf8(output.stdout).unwrap();
    assert_eq!(
        stdout
            .lines()
            .filter(|line| *line == "running 1 test")
            .count(),
        1,
        "{stdout}"
    );
    assert_eq!(
        stdout
            .lines()
            .filter(|line| line.starts_with("test result: ok. 1 passed; 0 failed; 0 ignored;"))
            .count(),
        1,
        "{stdout}"
    );
    assert!(stdout.contains("test aot_layout::tests::resource_output_tests::overlapping_request_rejects_before_emit_and_preserves_dist ... ok"), "{stdout}");
    assert!(!marker.exists());
}

#[cfg(unix)]
#[test]
fn trusted_output_alias_resolves_before_overlap_check() {
    let mut c = Case::new();
    c.opts.input = c.root.join("app.py");
    std::fs::write(&c.opts.input, "print(1)\n").unwrap();
    let alias = c.root.join("output-alias");
    std::os::unix::fs::symlink(&c.root, &alias).unwrap();
    c.opts.out_dir = alias;
    let request = c.root.join("resources.json");
    std::fs::write(&request, br#"{"schema_version":1,"modules":[{"module":"__main__","trees":[{"directory":".","suffixes":[".json"]}]}]}"#).unwrap();
    let error =
        crate::build_with_resources(&mut crate::Session::default(), &c.opts, Some(&request))
            .err()
            .unwrap();
    assert!(format!("{error:#}").contains("生成输出重叠"), "{error:#}");
    assert!(!c.root.join("app.dist").exists());
}

#[test]
fn project_output_root_with_sibling_assets_remains_usable() {
    let mut c = Case::new();
    c.opts.input = c.root.join("app.py");
    std::fs::write(&c.opts.input, "print(1)\n").unwrap();
    c.opts.out_dir = c.root.clone();
    std::fs::create_dir(c.root.join("assets")).unwrap();
    std::fs::write(c.root.join("assets/data.json"), "{}").unwrap();
    let source =
        crate::modules::load_single(&mut crate::Session::default(), &c.opts.input).unwrap();
    let request = c.root.join("resources.json");
    std::fs::write(&request, br#"{"schema_version":1,"modules":[{"module":"__main__","trees":[{"directory":"assets","suffixes":[".json"]}]}]}"#).unwrap();
    build(&c.opts, |opts, owned| {
        let plan = crate::resources::ResourcePlan::read_context(
            crate::resources::ResourceRequestContext {
                path: &request,
                output_roots: owned.output_roots(),
            },
            std::slice::from_ref(&source),
        )?;
        let staged = plan.stage(&opts.out_dir)?;
        assert_eq!(staged.records.len(), 1);
        fake(opts, b"sibling-ok", 1)
    })
    .unwrap();
    assert!(c.root.join("app.dist").is_dir());
}
