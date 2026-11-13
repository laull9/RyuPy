//! 资源发现的真实CP/JIT差分；Static旧债按已登记事件合同单列，所有临时输出由RAII清理。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
struct Tree(test_temp::Root);
impl Tree {
    fn new() -> Self {
        let root = test_temp::Root::new("resource-jit");
        for name in ["source", "cp", "jit"] {
            std::fs::create_dir(root.join(name)).unwrap();
        }
        Self(root)
    }
    fn write(&self, name: &str, bytes: impl AsRef<[u8]>) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, bytes).unwrap();
    }
    fn source(&self, name: &str, bytes: impl AsRef<[u8]>) {
        self.write(&format!("source/{name}"), bytes);
    }
    // 两臂继承等效环境，清除解释器、provider和加载器注入；脚本目录仍按正常import规则查找。
    fn command(&self, executable: impl AsRef<std::ffi::OsStr>, cwd: &Path) -> Command {
        let mut command = Command::new(executable);
        command
            .env_clear()
            .envs(std::env::vars_os().filter(|(name, _)| {
                let name = name.to_string_lossy();
                !["PYTHON", "RYUPY_", "DYLD_", "LD_"]
                    .iter()
                    .any(|prefix| name.starts_with(prefix))
                    && !matches!(
                        name.as_ref(),
                        "DOTNET_STARTUP_HOOKS" | "DOTNET_ADDITIONAL_DEPS" | "DOTNET_SHARED_STORE"
                    )
            }))
            .env("LC_ALL", "C")
            .current_dir(cwd);
        command
    }
    fn python(&self) -> Output {
        let python =
            PathBuf::from(std::env::var_os("PYTHON").expect("必须显式指定固定CPython绝对路径"));
        assert!(python.is_absolute() && python.is_file());
        let cwd = self.0.join("cp");
        let version = self.command(&python, &cwd)
            .args([
                "-B",
                "-S",
                "-c",
                "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))",
            ])
            .output()
            .unwrap();
        let expected = std::fs::read_to_string(
            Path::new(env!("CARGO_MANIFEST_DIR")).join("../../tests/pinned-python.txt"),
        )
        .unwrap();
        self.save("python-version", &version);
        assert!(version.status.success() && version.stderr.is_empty());
        assert_eq!(expected.trim(), "3.12.13");
        assert_eq!(
            version.stdout,
            format!("cpython\n{}\n", expected.trim()).as_bytes()
        );
        let output = self
            .command(python, &cwd)
            .args(["-B", "-S"])
            .arg(self.0.join("source/entry.py"))
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
    fn jit(&self, label: &str, includes: &[&str], discovered: bool) -> Output {
        let mut session = Session {
            include_modules: includes.iter().map(|name| (*name).into()).collect(),
            ..Session::default()
        };
        let options = BuildOptions {
            input: self.0.join("source/entry.py"),
            out_dir: self.0.join("build").join(label),
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
        let cwd = self.0.join("jit").join(label);
        std::fs::create_dir(&cwd).unwrap();
        let output = self
            .command(dotnet, &cwd)
            .arg(&artifact.dll)
            .output()
            .unwrap();
        self.save(label, &output);
        output
    }
}
fn equal_streams(actual: &Output, expected: &Output) {
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
fn fixture(name: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/fixtures/resource_discovery")
        .join(name)
}
#[test]
fn actual_calls_rebinding_and_dead_nonempty_candidates_preserve_python_order() {
    let tree = Tree::new();
    tree.source("pkg/__init__.py", "print('parent')\n");
    tree.source("pkg/data/__init__.py", "\n");
    tree.source("pkg/data/sample.txt", "resource-data\n");
    tree.source(
        "pkg/nonempty/__init__.py",
        "raise RuntimeError('must not import')\n",
    );
    tree.source("pkg/broken/__init__.py", "def bad(:\n");
    tree.source("pkg/encoded/__init__.py", "# coding: no-such-codec\n");
    tree.source(
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
    equal_streams(&tree.jit("jit", &[], true), &cp);
}
#[test]
fn automatically_registered_package_still_obeys_empty_parent_path() {
    let tree = Tree::new();
    tree.source("pkg/__init__.py", "");
    tree.source("pkg/data/__init__.py", "\n");
    tree.source(
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
    equal_streams(&tree.jit("jit", &[], true), &cp);
}
#[test]
fn original_static_collision_keeps_correct_default_and_records_existing_rebound_debt() {
    let tree = Tree::new();
    for (target, original) in [
        ("entry.py", "static_entry.py"),
        ("pkg/__init__.py", "pkg/__init__.py"),
        ("pkg/data/__init__.py", "pkg/data/__init__.py"),
    ] {
        tree.source(target, std::fs::read(fixture(original)).unwrap());
    }
    let cp = tree.python();
    static_events(&cp, true);
    let default = tree.jit("default", &[], true);
    static_events(&default, false);
    let included = tree.jit("included", &["pkg.data"], false);
    static_events(&included, false);
    equal_streams(&default, &included);
}

// ADR0146：Static仍用词法来源；真实CP rebound会求marker真值，旧Static路径直接正常返回。
// 仅核fixture声明的五条记录和每个事件，预期不依赖任何候选输出或旧golden。
fn static_events(output: &Output, python: bool) {
    assert_eq!(output.status.code(), Some(0));
    assert!(output.stderr.is_empty(), "{:?}", output.stderr);
    let text = std::str::from_utf8(&output.stdout).unwrap();
    assert!(text.ends_with('\n'));
    let lines: Vec<_> = text.lines().collect();
    assert_eq!(lines.len(), 5);
    let writes = ["'before-out',", "'write', 'payload'", "'write', '\\n'"];
    let mut completed = writes.to_vec();
    completed.push("'after-out',");
    static_record(lines[0], "default", "ok", &completed);
    if python {
        let mut raised = writes.to_vec();
        raised.extend(["'module-bool',", "'caught', 'module-bool-marker'"]);
        static_record(lines[1], "rebound", "runtime-error", &raised);
    } else {
        static_record(lines[1], "rebound", "ok", &completed);
    }
    assert_eq!(lines[2], "S data-cached False");
    assert_eq!(lines[3], "S done");
    assert_eq!(lines[4], "E done");
}

fn static_record(line: &str, label: &str, outcome: &str, events: &[&str]) {
    let mut fields = line.splitn(5, ' ');
    assert_eq!(fields.next(), Some("S"));
    assert_eq!(fields.next(), Some(label));
    assert_eq!(fields.next(), Some(outcome));
    assert_eq!(
        fields.next(),
        Some("True"),
        "out.__module__必须仍是原赋值对象"
    );
    let observed: Vec<_> = fields
        .next()
        .unwrap()
        .strip_prefix("[(")
        .unwrap()
        .strip_suffix(")]")
        .unwrap()
        .split("), (")
        .collect();
    assert_eq!(observed, events);
}
