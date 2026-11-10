//! 真源码差分候选：实际callee/未执行候选/父path与Static旧债分开验，不把冷单测当产品。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let path = std::env::temp_dir().join(format!(
            "ryupy-resource-jit-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
    fn write(&self, name: &str, bytes: impl AsRef<[u8]>) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, bytes).unwrap();
    }
    fn python(&self) -> Output {
        let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
        let version = Command::new(&python)
            .args([
                "-B",
                "-S",
                "-c",
                "import sys; print('.'.join(map(str, sys.version_info[:3])))",
            ])
            .output()
            .unwrap();
        let expected = std::fs::read_to_string(
            Path::new(env!("CARGO_MANIFEST_DIR")).join("../../tests/pinned-python.txt"),
        )
        .unwrap();
        assert!(version.status.success() && version.stderr.is_empty());
        assert_eq!(
            String::from_utf8(version.stdout).unwrap().trim(),
            expected.trim()
        );
        let output = Command::new(python)
            .args(["-B", "-S"])
            .arg(self.0.join("entry.py"))
            .current_dir(&self.0)
            .output()
            .unwrap();
        self.save("cp", &output);
        assert_eq!(output.status.code(), Some(0));
        assert!(output.stderr.is_empty(), "{:?}", output.stderr);
        output
    }
    fn save(&self, label: &str, output: &Output) {
        self.write(&format!("{label}.stdout"), &output.stdout);
        self.write(&format!("{label}.stderr"), &output.stderr);
        self.write(
            &format!("{label}.exit"),
            format!("{:?}", output.status.code()),
        );
    }
    fn jit(&self, label: &str, includes: &[&str], expected: &[u8], discovered: bool) {
        let mut session = Session {
            include_modules: includes.iter().map(|name| (*name).into()).collect(),
            ..Session::default()
        };
        let options = BuildOptions {
            input: self.0.join("entry.py"),
            out_dir: self.0.join(label),
            aot: false,
            search_paths: vec![],
        };
        let artifact = ryupy_driver::build(&mut session, &options)
            .unwrap_or_else(|error| panic!("{error}\n{}", session.render_diagnostics(false)));
        let report: serde_json::Value =
            serde_json::from_slice(&session.module_environment_report_json().unwrap()).unwrap();
        self.write(
            &format!("{label}.graph.json"),
            serde_json::to_vec_pretty(&report).unwrap(),
        );
        let modules = report["modules"].as_array().unwrap();
        assert_eq!(
            modules
                .iter()
                .filter(|row| row["module"] == "pkg.data")
                .count(),
            1
        );
        let child = modules
            .iter()
            .find(|row| row["module"] == "pkg.data")
            .unwrap();
        assert_eq!(child.get("resource_discovery").is_some(), discovered);
        let dotnet = std::env::var_os("DOTNET").unwrap_or_else(|| "dotnet".into());
        let output = Command::new(dotnet)
            .arg(&artifact.dll)
            .current_dir(&self.0)
            .output()
            .unwrap();
        self.save(label, &output);
        assert_eq!(
            (
                output.status.code(),
                output.stdout.as_slice(),
                output.stderr.as_slice()
            ),
            (Some(0), expected, &[][..])
        );
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn fixture(name: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/fixtures/resource_discovery")
        .join(name)
}
#[test]
fn actual_calls_rebinding_and_dead_nonempty_candidates_preserve_python_order() {
    let tree = Tree::new();
    tree.write("pkg/__init__.py", "print('parent')\n");
    tree.write("pkg/data/__init__.py", "\n");
    tree.write("pkg/data/sample.txt", "resource-data\n");
    tree.write(
        "pkg/nonempty/__init__.py",
        "raise RuntimeError('must not import')\n",
    );
    tree.write("pkg/broken/__init__.py", "def bad(:\n");
    tree.write("pkg/encoded/__init__.py", "# coding: no-such-codec\n");
    tree.write(
        "entry.py",
        r#"import pkg
import sys
import importlib.resources as r
from importlib.resources import files as selected
from importlib import resources as alias

def unused():
    selected('pkg.missing')
    selected('pkg.nonempty')
    selected('pkg.broken')
    selected('pkg.encoded')

print('before', 'pkg.data' in sys.modules)
selected = lambda name: name
print('rebound', selected('pkg.data'))
real = r.files
r.files = lambda name: name
print('attribute', r.files('pkg.data'))
r.files = real
print(alias.files(anchor='pkg.data').joinpath('sample.txt').read_text(), end='')
print('after', 'pkg.data' in sys.modules)
"#,
    );
    let cp = tree.python();
    tree.jit("jit", &[], &cp.stdout, true);
}
#[test]
fn automatically_registered_package_still_obeys_empty_parent_path() {
    let tree = Tree::new();
    tree.write("pkg/__init__.py", "");
    tree.write("pkg/data/__init__.py", "\n");
    tree.write(
        "entry.py",
        r#"import pkg
import sys
from importlib.resources import files
pkg.__path__ = []
try:
    files('pkg.data')
except ModuleNotFoundError as error:
    print(type(error).__name__, str(error))
print('cached', 'pkg.data' in sys.modules)
"#,
    );
    let cp = tree.python();
    tree.jit("jit", &[], &cp.stdout, true);
}
#[test]
fn original_static_collision_keeps_correct_default_and_records_existing_rebound_debt() {
    let tree = Tree::new();
    for (target, original) in [
        ("entry.py", "static_entry.py"),
        ("pkg/__init__.py", "pkg/__init__.py"),
        ("pkg/data/__init__.py", "pkg/data/__init__.py"),
    ] {
        tree.write(target, std::fs::read(fixture(original)).unwrap());
    }
    let cp = tree.python();
    assert_eq!(
        cp.stdout,
        std::fs::read(fixture("static.cp.stdout")).unwrap()
    );
    let legacy = std::fs::read(fixture("static.legacy.stdout")).unwrap();
    assert_ne!(cp.stdout, legacy, "不可把既有Static rebound差异冒充CP一致");
    tree.jit("default", &[], &legacy, true);
    tree.jit("included", &["pkg.data"], &legacy, false);
}
