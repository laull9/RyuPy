//! 阶段诊断的窄CLI契约：只编译小fixture，不启动dotnet或外部项目。
//! runtime是仅验证复制步骤的哨兵文件，不把这些测试计作运行时验收。
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
        let mut command = Command::new(env!("CARGO_BIN_EXE_ryupy"));
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
    assert!(!fixture.0.join("out").exists());
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
