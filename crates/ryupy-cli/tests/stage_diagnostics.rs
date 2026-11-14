//! 阶段诊断的窄CLI契约：只编译小fixture，不启动dotnet或外部项目。
//! runtime使用复制步骤哨兵；运行时语义另由真实差分验收。
use std::fs;
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);
struct Fixture(PathBuf);
impl Fixture {
    fn new(source: &str) -> Self {
        let root = std::env::temp_dir().join(format!(
            "ryupy-stage-diagnostics-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        fs::create_dir(&root).unwrap();
        fs::write(root.join("main.py"), source).unwrap();
        fs::write(root.join("helper.py"), "value = 3\n").unwrap();
        fs::write(
            root.join("RyuPy.Runtime.dll"),
            b"compile-only-copy-sentinel",
        )
        .unwrap();
        Self(root)
    }
    fn build(&self, enabled: bool) -> Output {
        self.build_mode(enabled, false)
    }
    fn build_mode(&self, enabled: bool, required: bool) -> Output {
        let mut command = Command::new(env!("CARGO_BIN_EXE_ryupy"));
        if required {
            command.arg("--trace-product-v1");
        }
        command
            .arg("build")
            .arg(self.0.join("main.py"))
            .arg("--out-dir")
            .arg(self.0.join("out"))
            .env("RYUPY_RUNTIME_DIR", &self.0);
        if enabled {
            command.arg("--diagnostic-stages");
        }
        command.output().unwrap()
    }

    // 图加载完成后，即使语义编译失败也保留来源报告；禁止出现 DLL 等编译产物。
    fn assert_source_report_only(&self) {
        let files: Vec<_> = fs::read_dir(self.0.join("out"))
            .unwrap()
            .map(|entry| entry.unwrap().file_name())
            .collect();
        assert_eq!(files, ["main.source-plan.json"]);
        let report = fs::read_to_string(self.0.join("out/main.source-plan.json")).unwrap();
        assert!(report.contains("\"compiler_verified\": false"));
        assert!(report.contains("\"__main__\""));
    }
}
impl Drop for Fixture {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.0);
    }
}
fn markers(output: &Output) -> Vec<(String, String, String)> {
    let text = String::from_utf8_lossy(&output.stderr);
    let mut previous = 0;
    text.lines()
        .filter(|line| line.starts_with("[ryupy-stage] "))
        .filter_map(|line| {
            let fields: Vec<_> = line.split_whitespace().collect();
            assert!(matches!(fields.len(), 5 | 7), "{line}");
            let elapsed: u128 = fields
                .last()
                .unwrap()
                .strip_prefix("elapsed_us=")
                .unwrap()
                .parse()
                .unwrap();
            assert!(elapsed >= previous, "时间必须单调");
            previous = elapsed;
            if fields.len() == 7 {
                assert!(matches!(
                    fields[2],
                    "stage=pass-run" | "stage=pass-validate"
                ));
                None
            } else {
                Some((
                    fields[1].to_owned(),
                    fields[2].to_owned(),
                    fields[3].to_owned(),
                ))
            }
        })
        .collect()
}
fn without_markers(output: &Output) -> String {
    String::from_utf8_lossy(&output.stderr)
        .split_inclusive('\n')
        .filter(|line| !line.starts_with("[ryupy-stage] "))
        .collect()
}
fn pair(stage: &str, module: &str) -> Vec<(String, String, String)> {
    ["begin", "end"]
        .map(|event| {
            (
                format!("event={event}"),
                format!("stage={stage}"),
                format!("module={module:?}"),
            )
        })
        .to_vec()
}

// 单点结果没有begin/end区间；elapsed_us仍只是共享起点的时间戳。
fn outcome(stage: &str, module: &str) -> (String, String, String) {
    (
        "event=result".into(),
        format!("stage={stage}"),
        format!("module={module:?}"),
    )
}

#[test]
fn stage_diagnostics_default_unchanged_and_enabled_ordered() {
    let fixture = Fixture::new("import helper\nprint(helper.value)\n");
    let plain = fixture.build(false);
    let traced = fixture.build(true);
    assert!(
        plain.status.success(),
        "{}",
        String::from_utf8_lossy(&plain.stderr)
    );
    assert!(
        traced.status.success(),
        "{}",
        String::from_utf8_lossy(&traced.stderr)
    );
    assert!(markers(&plain).is_empty());
    assert_eq!(plain.stdout, traced.stdout);
    assert_eq!(
        String::from_utf8_lossy(&plain.stderr),
        without_markers(&traced)
    );
    let mut expected = pair("graph-load", "-");
    for module in ["__main__", "helper"] {
        for stage in [
            "hir",
            "type-check",
            "mir-build",
            "pipeline",
            "final-validate",
        ] {
            expected.extend(pair(stage, module));
        }
        expected.push(outcome("trace-legacy", module));
    }
    for stage in ["link", "export-check", "emit", "output-write"] {
        expected.extend(pair(stage, "-"));
    }
    assert_eq!(markers(&traced), expected);
    assert_pass_markers(&traced);
    assert_eq!(
        fs::read(fixture.0.join("out/RyuPy.Runtime.dll")).unwrap(),
        b"compile-only-copy-sentinel"
    );
}

#[test]
fn stage_diagnostics_error_preserves_original_diagnostics() {
    let fixture = Fixture::new("def broken(:\n");
    let plain = fixture.build(false);
    let traced = fixture.build(true);
    assert!(!plain.status.success());
    assert_eq!(plain.status.code(), traced.status.code());
    assert_eq!(plain.stdout, traced.stdout);
    assert_eq!(
        String::from_utf8_lossy(&plain.stderr),
        without_markers(&traced)
    );
    // parser的诊断原本就在HIR后的边界终止；graph-load end仅表示加载返回。
    let mut expected = pair("graph-load", "-");
    expected.push((
        "event=begin".into(),
        "stage=hir".into(),
        "module=\"__main__\"".into(),
    ));
    assert_eq!(markers(&traced), expected);
    fixture.assert_source_report_only();
}

#[test]
fn stage_diagnostics_missing_input_has_no_false_end() {
    let fixture = Fixture::new("pass\n");
    fs::remove_file(fixture.0.join("main.py")).unwrap();
    let traced = fixture.build(true);
    assert!(!traced.status.success());
    assert_eq!(
        markers(&traced),
        vec![(
            "event=begin".into(),
            "stage=graph-load".into(),
            "module=\"-\"".into()
        )]
    );
}

#[test]
fn stage_diagnostics_rejects_dump_and_native_binding() {
    for args in [
        vec!["dump", "x.py", "--diagnostic-stages"],
        vec![
            "build",
            "x.py",
            "--diagnostic-stages",
            "--native-binding",
            "x.json",
        ],
    ] {
        assert_eq!(
            Command::new(env!("CARGO_BIN_EXE_ryupy"))
                .args(args)
                .output()
                .unwrap()
                .status
                .code(),
            Some(2)
        );
    }
}

// 针对固定小fixture比较完整嵌套序列，不将任意用户模块名用空格切词。
fn assert_pass_markers(output: &Output) {
    let text = String::from_utf8_lossy(&output.stderr);
    let names = [
        "simplify_cfg",
        "dead_values",
        "numeric_accumulator",
        "string_accumulator",
        "tuple_scalar",
        "dead_values",
        "interval_narrow",
        "float_chain",
    ];
    let actual: Vec<_> = text
        .lines()
        .filter(|line| line.contains("stage=pass-") || line.contains("stage=pipeline "))
        .map(|line| line.rsplit_once(" elapsed_us=").unwrap().0.to_owned())
        .collect();
    let mut expected = Vec::new();
    for module in ["__main__", "helper"] {
        expected.push(format!(
            "[ryupy-stage] event=begin stage=pipeline module={module:?}"
        ));
        for (index, name) in names.iter().enumerate() {
            for stage in ["pass-run", "pass-validate"] {
                for event in ["begin", "end"] {
                    expected.push(format!(
                        "[ryupy-stage] event={event} stage={stage} module={module:?} pass_index={} pass={name:?}",
                        index + 1,
                    ));
                }
            }
        }
        expected.push(format!(
            "[ryupy-stage] event=end stage=pipeline module={module:?}"
        ));
    }
    assert_eq!(actual, expected);
}

#[test]
fn stage_diagnostics_audit_write_failure_does_not_change_build() {
    let fixture = Fixture::new("print(1)\n");
    // 已有同名目录使审计write确定失败，不依赖宿主chmod或管理员权限。
    fs::create_dir_all(fixture.0.join("out/module-environment.json")).unwrap();
    let plain = fixture.build(false);
    let traced = fixture.build(true);
    assert!(plain.status.success());
    assert!(
        traced.status.success(),
        "{}",
        String::from_utf8_lossy(&traced.stderr)
    );
    assert_eq!(plain.stdout, traced.stdout);
    assert_eq!(
        String::from_utf8_lossy(&plain.stderr),
        without_markers(&traced)
    );
    assert!(fixture.0.join("out/main.dll").is_file());
    assert!(fixture.0.join("out/module-environment.json").is_dir());
    assert_eq!(
        fs::read(fixture.0.join("out/RyuPy.Runtime.dll")).unwrap(),
        b"compile-only-copy-sentinel"
    );
}

// 同一小参数组覆盖实际CLI四条选路，不启动dotnet，不从结果日志推断产物证书。
#[test]
fn stage_diagnostics_trace_results_are_single_points_for_each_path() {
    let original = include_str!("../../../tests/cases/generator_dynamic/frames/entry.py");
    // identity位于定义区，确保拒绝源于未覆盖输入，而不是调用先于后续定义。
    let outside = format!("def identity(value):\n    return value\n{original}");
    for (source, selected, fallback, required) in [
        ("print(1)\n", false, false, false),
        (original, true, false, false),
        (outside.as_str(), false, true, false),
        (outside.as_str(), false, false, true),
    ] {
        let fixture = Fixture::new(source);
        assert!(!fixture.0.join("out").exists());
        let plain = fixture.build_mode(false, required);
        if required {
            fixture.assert_source_report_only();
        }
        let traced = fixture.build_mode(true, required);
        assert_eq!(plain.status.code(), traced.status.code());
        assert_eq!(
            plain.status.success(),
            !required,
            "{}",
            String::from_utf8_lossy(&plain.stderr)
        );
        assert!(markers(&plain).is_empty());
        assert_eq!(plain.stdout, traced.stdout);
        assert_eq!(
            String::from_utf8_lossy(&plain.stderr),
            without_markers(&traced)
        );
        let mut expected = pair("graph-load", "-");
        for stage in ["hir", "type-check"] {
            expected.extend(pair(stage, "__main__"));
        }
        if selected || fallback || required {
            expected.extend(pair("trace-v1-prepare", "__main__"));
        }
        if selected {
            expected.extend(pair("trace-v1-lower", "__main__"));
        } else if !required {
            if fallback {
                expected.push(outcome("trace-v1-not-in-domain", "__main__"));
            }
            for stage in ["mir-build", "pipeline", "final-validate"] {
                expected.extend(pair(stage, "__main__"));
            }
            expected.push(outcome("trace-legacy", "__main__"));
        }
        if required {
            assert_eq!(
                String::from_utf8_lossy(&traced.stderr)
                    .matches("[E0310]")
                    .count(),
                1
            );
            fixture.assert_source_report_only();
        } else {
            for stage in ["link", "export-check", "emit", "output-write"] {
                expected.extend(pair(stage, "-"));
            }
            assert!(fixture.0.join("out/main.dll").is_file());
            assert_eq!(
                fs::read(fixture.0.join("out/RyuPy.Runtime.dll")).unwrap(),
                b"compile-only-copy-sentinel"
            );
        }
        // 包含精确event、模块、相对位置和全序列；拒绝时绝不能有lower/结果/emit/write。
        assert_eq!(markers(&traced), expected);
    }
}
