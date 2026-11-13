//! 三组固定无子进程测试共用的临时目录、直接子进程期限及CPython身份探针。
// 不负责业务流归一化、异常合同、编译期限或任意后代进程监管。

pub fn bounded(
    root: &std::path::Path,
    label: &str,
    command: &mut std::process::Command,
    timeout: std::time::Duration,
) -> std::io::Result<std::process::Output> {
    let stdout = root.join(format!("{label}.stdout"));
    let stderr = root.join(format!("{label}.stderr"));
    let mut child = command
        .stdin(std::process::Stdio::null())
        .stdout(std::fs::File::create(&stdout)?)
        .stderr(std::fs::File::create(&stderr)?)
        .spawn()?;
    let start = std::time::Instant::now();
    loop {
        match child.try_wait() {
            Ok(Some(status)) => {
                return Ok(std::process::Output {
                    status,
                    stdout: std::fs::read(stdout)?,
                    stderr: std::fs::read(stderr)?,
                });
            }
            Ok(None) if start.elapsed() < timeout => {
                std::thread::sleep(std::time::Duration::from_millis(10))
            }
            result => {
                let _ = child.kill();
                let _ = child.wait();
                return Err(result.err().unwrap_or_else(|| {
                    std::io::Error::new(
                        std::io::ErrorKind::TimedOut,
                        format!("{label}超过期限{timeout:?}"),
                    )
                }));
            }
        }
    }
}

/// 独占仓库外目录；正常及panic退出均清理。
pub struct TempRoot(std::path::PathBuf);
impl TempRoot {
    pub fn new(prefix: &str) -> Self {
        let base = std::fs::canonicalize(std::env::temp_dir()).unwrap();
        let repository = std::fs::canonicalize(ryupy_driver::runtime::workspace_root()).unwrap();
        assert!(!base.starts_with(repository), "测试产物必须位于仓库外");
        static NEXT: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
        let serial = NEXT.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let path = base.join(format!(
            "ryupy-{prefix}-{}-{stamp}-{serial}",
            std::process::id()
        ));
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
}
impl TempRoot {
    pub fn path(&self) -> &std::path::Path {
        &self.0
    }
}
impl Drop for TempRoot {
    fn drop(&mut self) {
        if let Err(error) = std::fs::remove_dir_all(&self.0) {
            eprintln!("清理测试临时目录失败 {}: {error}", self.0.display());
        }
    }
}

/// 只对版本元数据按行比较，业务输出仍由调用方逐字节验收。
pub fn pinned_python(root: &std::path::Path) -> std::path::PathBuf {
    let python =
        std::path::PathBuf::from(std::env::var_os("PYTHON").expect("须指定固定CPython绝对路径"));
    assert!(python.is_absolute());
    let pin = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(pin.trim(), "3.12.13");
    let version = bounded(root, "version", std::process::Command::new(&python).args([
        "-I", "-B", "-S", "-c", "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))"
    ]), std::time::Duration::from_secs(10)).unwrap();
    assert!(version.status.success() && version.stderr.is_empty());
    assert!(
        std::str::from_utf8(&version.stdout)
            .unwrap()
            .lines()
            .eq(["cpython", "3.12.13"])
    );
    python
}
