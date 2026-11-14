//! 解释器别名不能改变PATH目录顺序，Windows仅有python.exe的venv仍应胜出。
use super::*;

#[test]
fn cpython_path_directory_precedes_alias_preference() {
    let timestamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-python-path-{}-{timestamp}",
        std::process::id()
    ));
    let first = root.join("venv");
    let second = root.join("system");
    std::fs::create_dir_all(&first).unwrap();
    std::fs::create_dir(&second).unwrap();
    let name = if cfg!(windows) {
        "python.exe"
    } else {
        "python"
    };
    let alias = if cfg!(windows) {
        "python3.exe"
    } else {
        "python3"
    };
    std::fs::write(first.join(name), "handwritten lookup fixture").unwrap();
    std::fs::write(second.join(alias), "handwritten lookup fixture").unwrap();
    assert_eq!(
        find_python_in_paths(&["python3", "python"], [first.clone(), second.clone()]),
        Some(first.join(name))
    );
    assert_eq!(
        find_python_in_paths(&["python3", "python"], [second.clone(), first]),
        Some(second.join(alias))
    );
    std::fs::remove_dir_all(root).unwrap();
}
