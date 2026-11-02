//! 真构建模块位置：两处__file__、可写对象身份、循环import及源目录资源，不用dump替代实际类型验收。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};

struct OwnedRoot(PathBuf);
impl Drop for OwnedRoot {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn root() -> OwnedRoot {
    let unique = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-module-locations-{}-{unique}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    OwnedRoot(path)
}
fn python() -> std::ffi::OsString {
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python).arg("--version").output().unwrap();
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert!(version.status.success());
    assert!(String::from_utf8_lossy(&version.stdout).contains(pinned.trim()));
    python
}
fn reference(python: &std::ffi::OsStr, input: &Path, cwd: &Path) -> Output {
    let output = Command::new(python)
        .arg(input)
        .current_dir(cwd)
        .output()
        .unwrap();
    assert!(
        output.status.success(),
        "{}",
        String::from_utf8_lossy(&output.stderr)
    );
    assert!(output.stderr.is_empty());
    output
}
fn compare(expected: &Output, actual: &Output) {
    assert_eq!(
        actual.status.code(),
        expected.status.code(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

// common.py自身绝不写__file__：否则旧类型推断已因None赋值降级，会掩盖公开setter问题。
const FILES: &[(&str, &str)] = &[
    (
        "cycle_a.py",
        r#"import cycle_b
seen_by_b = cycle_b.seen_a
b_saw_absolute = cycle_b.saw_absolute
"#,
    ),
    (
        "cycle_b.py",
        r#"import os.path
import cycle_a
seen_a = cycle_a.__file__
saw_absolute = os.path.isabs(seen_a)
"#,
    ),
    (
        "entry.py",
        r#"import os.path
import left.common as left
import right.common as right
import cycle_a

class Token:
    pass

token = Token()
print('initial', os.path.isabs(__file__), os.path.isabs(left.__file__), os.path.isabs(right.__file__))
print('same-basename', os.path.basename(left.__file__) == os.path.basename(right.__file__), left.__file__ != right.__file__)
print('source', left.read_file() == left.__file__, right.read_file() == right.__file__)
for module in (left, right):
    original = module.__file__
    module.__file__ = None
    print('none', module.__file__ is None, module.read_file() is None)
    module.__file__ = token
    print('object', module.__file__ is token, module.read_file() is token)
    module.__file__ = original
    print('restored', module.read_file() == original)
print('cycle', cycle_a.seen_by_b == cycle_a.__file__, cycle_a.b_saw_absolute)
print('data', left.load_text(), right.load_text())
"#,
    ),
    ("left/__init__.py", r#""#),
    (
        "left/common.py",
        r#"import os.path

def read_file():
    return __file__

def load_text():
    with open(os.path.join(os.path.dirname(__file__), 'data.txt'), 'r', encoding='utf-8') as stream:
        return stream.read().strip()
"#,
    ),
    ("right/__init__.py", r#""#),
    (
        "right/common.py",
        r#"import os.path

def read_file():
    return __file__

def load_text():
    with open(os.path.join(os.path.dirname(__file__), 'data.txt'), 'r', encoding='utf-8') as stream:
        return stream.read().strip()
"#,
    ),
];

#[test]
fn actual_build_file_identity_cycles_and_unicode_source_work_outside_source_cwd() {
    let root = root();
    let source = root.0.join("源码 & host");
    for (relative, contents) in FILES {
        let path = source.join(relative);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, contents).unwrap();
    }
    for name in ["left", "right"] {
        std::fs::write(source.join(name).join("data.txt"), format!("{name}-v1\n")).unwrap();
    }
    let input = source.join("entry.py");
    let cwd_a = root.0.join("outside-a");
    let cwd_b = root.0.join("outside-b");
    std::fs::create_dir(&cwd_a).unwrap();
    std::fs::create_dir(&cwd_b).unwrap();
    let python = python();
    let expected = reference(&python, &input, &cwd_a);
    assert_eq!(String::from_utf8_lossy(&expected.stdout), EXPECTED);
    let artifacts = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input: input.clone(),
            out_dir: root.0.join("artifacts"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    for cwd in [&cwd_a, &cwd_b] {
        let actual = ryupy_driver::dotnet::run_dll_with(
            &artifacts.dll,
            &ryupy_driver::dotnet::RunOptions {
                cwd: Some(cwd.clone()),
                ..Default::default()
            },
        )
        .unwrap();
        compare(&expected, &actual);
    }
    // JIT位置指向原资源而非打包快照：编译后改data应在下一次启动可见。
    std::fs::write(source.join("left/data.txt"), "left-v2\n").unwrap();
    let changed = reference(&python, &input, &cwd_b);
    assert_ne!(changed.stdout, expected.stdout);
    let actual = ryupy_driver::dotnet::run_dll_with(
        &artifacts.dll,
        &ryupy_driver::dotnet::RunOptions {
            cwd: Some(cwd_b),
            ..Default::default()
        },
    )
    .unwrap();
    compare(&changed, &actual);
}
const EXPECTED: &str = r#"initial True True True
same-basename True True
source True True
none True True
object True True
restored True
none True True
object True True
restored True
cycle True True
data left-v1 right-v1
"#;
