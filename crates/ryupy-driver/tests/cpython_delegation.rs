//! 路线 A 显式本地整模块与入口 CPython 委派的端到端契约测试（禁止静默回退）。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);

struct TempDir(PathBuf);
impl Drop for TempDir {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

fn temp_dir(label: &str) -> TempDir {
    let dir = std::env::temp_dir().join(format!(
        "ryupy-delegation-{label}-{}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir_all(&dir).unwrap();
    TempDir(dir)
}

#[test]
fn async_entry_and_local_module_do_not_silently_fallback() {
    let dir = temp_dir("no-fallback");
    let entry = dir.0.join("app.py");
    std::fs::write(&entry, "async def run():\n    await something()\n").unwrap();
    let mut sess = Session::default();
    let err = ryupy_driver::build(
        &mut sess,
        &BuildOptions {
            input: entry,
            out_dir: dir.0.join("out"),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .unwrap_err();
    assert!(err.is::<ryupy_driver::HasErrors>());
    assert!(sess.render_diagnostics(false).contains("E0100"));

    // 入口引用含 async 语法的本地模块但未声明 --cpython-module 时，同样必须报 E0100。
    let dir2 = temp_dir("no-fallback-local");
    let entry2 = dir2.0.join("app.py");
    std::fs::write(&entry2, "import async_mod\nprint(1)\n").unwrap();
    std::fs::write(
        dir2.0.join("async_mod.py"),
        "async def run():\n    await something()\n",
    )
    .unwrap();
    let mut sess2 = Session::default();
    let err2 = ryupy_driver::build(
        &mut sess2,
        &BuildOptions {
            input: entry2,
            out_dir: dir2.0.join("out"),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .unwrap_err();
    assert!(err2.is::<ryupy_driver::HasErrors>());
    assert!(sess2.render_diagnostics(false).contains("E0100"));
}

#[test]
fn delegation_options_require_cpython_provider() {
    let dir = temp_dir("require-provider");
    let entry = dir.0.join("app.py");
    std::fs::write(&entry, "print(1)\n").unwrap();
    let mut sess = Session {
        cpython_entry: true,
        ..Session::default()
    };
    let err = ryupy_driver::build(
        &mut sess,
        &BuildOptions {
            input: entry.clone(),
            out_dir: dir.0.join("out1"),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .unwrap_err();
    assert!(err.to_string().contains("CPython provider"), "{err}");

    let mut sess = Session {
        cpython_modules: vec!["helper".into()],
        ..Session::default()
    };
    let err = ryupy_driver::build(
        &mut sess,
        &BuildOptions {
            input: entry.clone(),
            out_dir: dir.0.join("out2"),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .unwrap_err();
    assert!(err.to_string().contains("CPython provider"), "{err}");

    let mut sess = Session {
        cpython_dynamic_modules: vec!["aiosqlite".into()],
        ..Session::default()
    };
    let err = ryupy_driver::build(
        &mut sess,
        &BuildOptions {
            input: entry,
            out_dir: dir.0.join("out3"),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .unwrap_err();
    assert!(err.to_string().contains("CPython provider"), "{err}");
}
