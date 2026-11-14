//! 每个测试独占的仓库外临时根：正常返回或断言展开时都删除源码与构建输出。
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);

pub struct TempScope(PathBuf);

impl TempScope {
    pub fn new(label: &str) -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let serial = NEXT.fetch_add(1, Ordering::Relaxed);
        let root = std::env::temp_dir().join(format!(
            "ryupy-{label}-{}-{stamp}-{serial}",
            std::process::id()
        ));
        std::fs::create_dir(&root).unwrap();
        Self(root)
    }

    pub fn path(&self, name: &str) -> PathBuf {
        self.0.join(name)
    }
    pub fn root(&self) -> PathBuf {
        self.0.clone()
    }
}

impl Drop for TempScope {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
