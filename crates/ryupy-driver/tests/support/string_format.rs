//! 四个模板应用的共享CP-first执行器；每例另有版本probe，不额外执行第五个应用。
//! 成功和失败均保独占tmp原流与产物供独核；投影只规整冻结源码回显，所有用户帧完整保留。
use ryupy_driver::{BuildOptions, Session, Stage};
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);

pub struct Root(PathBuf);

fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-string-format-{}-{stamp}-{serial}",
        std::process::id()
    ));
    // 创建成功才持有本目录；无Drop删除，成功与失败均保原始证据，不触碰碰撞目录。
    std::fs::create_dir(&path).unwrap();
    eprintln!("string-format evidence: {}", path.display());
    Root(path)
}

fn save_output(root: &Root, label: &str, output: &Output) {
    std::fs::write(root.0.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.0.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.0.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}

fn record_command(root: &Root, label: &str, command: &Command) {
    let program = PathBuf::from(command.get_program());
    let resolved = if program.components().count() > 1 {
        std::fs::canonicalize(&program).ok()
    } else {
        std::env::var_os("PATH").and_then(|path| {
            std::env::split_paths(&path)
                .map(|directory| directory.join(&program))
                .find(|candidate| candidate.is_file())
                .and_then(|candidate| std::fs::canonicalize(candidate).ok())
        })
    };
    std::fs::write(
        root.0.join(format!("{label}.command")),
        format!("command={command:?}\nprogram={program:?}\nresolved={resolved:?}\nargs={:?}\ncwd={:?}\n",
            command.get_args().collect::<Vec<_>>(),
            command.get_current_dir().map(PathBuf::from).unwrap_or_else(|| std::env::current_dir().unwrap())),
    ).unwrap();
}

// 先验固定CP退出码，之后才编译同一源码；独占证据目录始终保留。
pub fn execute(
    source: &str,
    filename: &str,
    expected_exit: i32,
    oracle: impl FnOnce(&Output),
) -> (Root, Output, Output) {
    let root = root();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let mut version_command = Command::new(&python);
    version_command.args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"]);
    record_command(&root, "python-version", &version_command);
    let version = version_command.output().unwrap();
    save_output(&root, "python-version", &version);
    assert!(version.status.success());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    let mut cp_command = Command::new(&python);
    cp_command.arg("-B").arg(&input).current_dir(&root.0);
    record_command(&root, "cpython", &cp_command);
    let expected = cp_command.output().unwrap();
    save_output(&root, "cpython", &expected);
    assert_eq!(
        expected.status.code(),
        Some(expected_exit),
        "CP reference failed: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    // 固定CP源码合同先守门，不能让错误参考继续进入构建或放宽用户帧。
    oracle(&expected);
    if filename == "binding_identity.py" {
        check_mir(&root, &input);
    }
    let mut session = Session::default();
    let result = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    );
    std::fs::write(
        root.0.join("diagnostics.txt"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let artifact = result.unwrap();
    let mut jit_command = Command::new(ryupy_driver::dotnet::dotnet_exe());
    jit_command.arg(&artifact.dll);
    record_command(&root, "ryupy", &jit_command);
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    save_output(&root, "ryupy", &actual);
    (root, expected, actual)
}

pub fn equal(source: &str, filename: &str, marker: &[u8]) {
    let (_root, expected, actual) = execute(source, filename, 0, |output| {
        assert!(output.stderr.is_empty());
        assert!(output.stdout.ends_with(marker), "CP success marker missing");
    });
    assert!(expected.stderr.is_empty());
    assert_eq!(actual.status.code(), Some(0));
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

// 只删当前冻结源码对应帧之后的准确回显及其一行caret；未知缩进行仍参与比较。
// 不删除任何File帧，不因“是缩进”便吞掉未知异常消息或链内容。
pub fn trace_projection(output: &Output) -> Vec<String> {
    let text = String::from_utf8_lossy(&output.stderr);
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut result = Vec::new();
    let mut echo = None;
    let mut caret_allowed = false;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").expect(line);
            let basename = path.rsplit(['/', '\\']).next().unwrap();
            result.push(format!("File {basename}, line {position}"));
            echo = if basename == "traceback.py" {
                match position {
                    "5, in <module>" => Some("raise LookupError('template-cause')"),
                    "29, in <module>" => {
                        Some("raise RuntimeError('template-ambient') from saved_cause")
                    }
                    "31, in <module>" => Some("caller()"),
                    "26, in caller" => Some("return render(Raises())"),
                    "20, in render" => Some("return 'prefix {0:spec}'.format(value)"),
                    "16, in __format__" => Some("return fail()"),
                    "11, in fail" => Some("raise ValueError('template-body')"),
                    _ => None,
                }
            } else {
                None
            };
            caret_allowed = false;
            continue;
        }
        if raw.starts_with(char::is_whitespace) && echo == Some(line) {
            echo = None;
            caret_allowed = true;
            continue;
        }
        echo = None;
        if caret_allowed
            && raw.starts_with(char::is_whitespace)
            && !line.is_empty()
            && line.chars().all(|ch| matches!(ch, '^' | '~' | ' '))
        {
            caret_allowed = false;
            continue;
        }
        caret_allowed = false;
        if !line.is_empty() {
            result.push(line.to_owned());
        }
    }
    result
}

fn function<'a>(dump: &'a str, name: &str) -> &'a str {
    let start = dump.find(&format!("fn {name}(")).expect(name);
    let tail = &dump[start..];
    tail.find("\nfn ").map_or(tail, |end| &tail[..end])
}

// 仍在同一Root生命周期内，且CP真实成功后才检查MIR和构建。
fn check_mir(root: &Root, input: &std::path::Path) {
    let mut session = Session::default();
    let result = ryupy_driver::dump(&mut session, input, Stage::Mir);
    std::fs::write(
        root.0.join("mir-diagnostics.txt"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let mir = result.unwrap();
    std::fs::write(root.0.join("mir.txt"), &mir).unwrap();
    assert!(session.diagnostics.is_empty(), "{:?}", session.diagnostics);
    let numeric = function(&mir, "numeric");
    assert!(
        numeric.starts_with("fn numeric(_0: i64) -> i64"),
        "{numeric}"
    );
    assert!(numeric.contains(": i64; // total"), "{numeric}");
    assert!(numeric.contains("branch "), "{numeric}");
    for forbidden in ["dyn.Add", "DynIter", "DynTruthy", "RuntimeFn::DynBuiltin"] {
        assert!(!numeric.contains(forbidden), "{numeric}");
    }
}
