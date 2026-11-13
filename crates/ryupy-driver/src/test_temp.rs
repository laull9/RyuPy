//! 仅测试使用的独占临时目录；不依赖墙钟精度，不复用或清理其他测试的路径。
use std::io;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);

pub(crate) fn directory(prefix: &str) -> PathBuf {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    allocate(&std::env::temp_dir(), || {
        format!(
            "{prefix}-{}-{stamp}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        )
    })
    .expect("分配独占测试目录")
}

fn allocate(parent: &Path, mut name: impl FnMut() -> String) -> io::Result<PathBuf> {
    for _ in 0..128 {
        let path = parent.join(name());
        match std::fs::create_dir(&path) {
            Ok(()) => return Ok(path),
            Err(error) if error.kind() == io::ErrorKind::AlreadyExists => continue,
            Err(error) => return Err(error),
        }
    }
    Err(io::Error::new(
        io::ErrorKind::AlreadyExists,
        "测试目录连续碰撞超限",
    ))
}

#[test]
fn occupied_candidate_is_not_reused_or_modified() {
    let parent = directory("ryupy-temp-occupied");
    let occupied = parent.join("0");
    std::fs::create_dir(&occupied).unwrap();
    std::fs::write(occupied.join("sentinel"), "original").unwrap();
    let mut next = 0;
    let path = allocate(&parent, || {
        let value = next;
        next += 1;
        value.to_string()
    })
    .unwrap();
    assert_eq!(path, parent.join("1"));
    assert_eq!(
        std::fs::read_to_string(occupied.join("sentinel")).unwrap(),
        "original"
    );
    std::fs::remove_dir_all(parent).unwrap();
}

#[test]
fn fixed_clock_parallel_allocations_are_distinct() {
    let parent = directory("ryupy-temp-concurrent");
    let threads: Vec<_> = (0..16)
        .map(|_| {
            let parent = parent.clone();
            std::thread::spawn(move || {
                (0..16)
                    .map(|_| {
                        allocate(&parent, || {
                            // 所有线程的时钟部分相同；原子序号及独占创建仍保证隔离。
                            format!("fixed-clock-{}", NEXT.fetch_add(1, Ordering::Relaxed))
                        })
                        .unwrap()
                    })
                    .collect::<Vec<_>>()
            })
        })
        .collect();
    let paths: Vec<_> = threads
        .into_iter()
        .flat_map(|thread| thread.join().unwrap())
        .collect();
    let unique: std::collections::HashSet<_> = paths.iter().collect();
    assert_eq!(paths.len(), 256);
    assert_eq!(unique.len(), paths.len());
    std::fs::remove_dir_all(parent).unwrap();
}

#[test]
fn non_collision_errors_are_not_retried() {
    let parent = directory("ryupy-temp-errors");
    let file = parent.join("file");
    std::fs::write(&file, "not a directory").unwrap();
    let mut calls = 0;
    assert!(
        allocate(&file, || {
            calls += 1;
            "child".into()
        })
        .is_err()
    );
    assert_eq!(calls, 1);
    std::fs::remove_dir_all(parent).unwrap();
}
