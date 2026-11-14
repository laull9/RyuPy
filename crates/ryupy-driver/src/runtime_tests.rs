//! 隔离验证运行时依赖复制、宿主原生库缺失错误与固定角色；不加载 provider、不校验普通文件摘要。
use super::*;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);

struct Temp(PathBuf);
impl Temp {
    fn new() -> Self {
        let path = std::env::temp_dir().join(format!(
            "ryupy-runtime-test-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
}
impl Drop for Temp {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

#[test]
fn runtime_missing_managed_and_native_dependencies_fail_explicitly() {
    let temp = Temp::new();
    let source = temp.0.join("runtime");
    let output = temp.0.join("app");
    std::fs::create_dir(&source).unwrap();
    std::fs::create_dir(&output).unwrap();
    std::fs::write(source.join(DEPENDENCIES), "<Project/>").unwrap();
    std::fs::write(source.join(LICENSE), "许可证测试夹具").unwrap();
    let error = copy_dependencies(&source, &output).unwrap_err().to_string();
    assert!(error.contains("Microsoft.Data.Sqlite.dll"));
    assert_eq!(std::fs::read_dir(&output).unwrap().count(), 0);
    for name in MANAGED_DEPENDENCIES {
        std::fs::write(source.join(name), name).unwrap();
    }
    let error = copy_dependencies(&source, &output).unwrap_err().to_string();
    assert!(error.contains("原生依赖"));
    assert_eq!(std::fs::read_dir(&output).unwrap().count(), 0);
    let library = if cfg!(target_os = "windows") {
        "e_sqlite3.dll"
    } else if cfg!(target_os = "macos") {
        "libe_sqlite3.dylib"
    } else {
        "libe_sqlite3.so"
    };
    let architecture = match std::env::consts::ARCH {
        "aarch64" => "arm64",
        "x86_64" => "x64",
        other => other,
    };
    let platform = if cfg!(target_os = "macos") {
        "osx"
    } else if cfg!(target_os = "windows") {
        "win"
    } else if cfg!(target_env = "musl") {
        "linux-musl"
    } else {
        "linux"
    };
    let native = source.join(format!("runtimes/{platform}-{architecture}/native"));
    std::fs::create_dir_all(&native).unwrap();
    std::fs::write(native.join(library), "native fixture").unwrap();
    copy_dependencies(&source, &output).unwrap();
    for name in MANAGED_DEPENDENCIES {
        assert_eq!(std::fs::read_to_string(output.join(name)).unwrap(), *name);
    }
    assert_eq!(
        std::fs::read_to_string(output.join(library)).unwrap(),
        "native fixture"
    );
    assert!(output.join(DEPENDENCIES).is_file());
    std::fs::remove_file(source.join(DEPENDENCIES)).unwrap();
    let error = copy_dependencies(&source, &output).unwrap_err().to_string();
    assert!(error.contains("版本合同"));
}

#[test]
fn runtime_roles_only_accept_the_pinned_provider_files() {
    for name in ["libe_sqlite3.so", "libe_sqlite3.dylib", "e_sqlite3.dll"] {
        assert_eq!(dependency_role(name), Some("runtime_native_dependency"));
    }
    assert_eq!(
        dependency_role("Microsoft.Data.Sqlite.dll"),
        Some("runtime_managed_dependency")
    );
    for name in [
        "custom.dll",
        "nested/libe_sqlite3.so",
        "libsqlite3.so",
        "e_sqlite3.dll.pdb",
    ] {
        assert_eq!(dependency_role(name), None);
    }
}
