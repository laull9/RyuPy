//! Windows真实句柄合同；链接创建失败会使测试失败，不静默跳过安全门。
use super::*;
use std::fs::{self, OpenOptions};

struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        Self(crate::test_temp::directory("ryupy-win-metadata"))
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.0);
    }
}

#[test]
fn distribution_metadata_windows_plain_and_bounded() {
    // 测试包装也必须真正使用并发安全的独占allocator；持有所有目录直到比较结束。
    let workers: Vec<_> = (0..16)
        .map(|_| std::thread::spawn(|| (0..4).map(|_| Tree::new()).collect::<Vec<_>>()))
        .collect();
    let trees: Vec<_> = workers
        .into_iter()
        .flat_map(|worker| worker.join().unwrap())
        .collect();
    let unique: std::collections::HashSet<_> = trees.iter().map(|tree| &tree.0).collect();
    assert_eq!(trees.len(), 64);
    assert_eq!(unique.len(), trees.len());
    let tree = Tree::new();
    let path = tree.0.join("METADATA");
    fs::write(&path, b"data").unwrap();
    assert_eq!(read(&path, 4).unwrap(), b"data");
    assert!(read(&path, 3).is_err());
    assert!(read(&tree.0, 4096).is_err());
    fs::write(&path, b"").unwrap();
    assert_eq!(read(&path, 0).unwrap(), b"");
}

#[test]
fn distribution_metadata_windows_handle_identity_and_write_delete_denial() {
    let tree = Tree::new();
    let path = tree.0.join("METADATA");
    let other = tree.0.join("same-size");
    fs::write(&path, b"same").unwrap();
    fs::write(&other, b"same").unwrap();
    let (file, _) = snapshot(&path, 4).unwrap();
    assert!(verify_identity(&path, &file).is_ok());
    assert!(verify_identity(&other, &file).is_err());
    // 原文件持活句柄，不能写、删或改名后用同长度文件替换。
    assert!(OpenOptions::new().write(true).open(&path).is_err());
    assert!(fs::remove_file(&path).is_err());
    assert!(fs::rename(&path, tree.0.join("retained")).is_err());
    drop(file);
    fs::rename(&path, tree.0.join("retained")).unwrap();
    fs::rename(&other, &path).unwrap();
    assert_eq!(read(&path, 4).unwrap(), b"same");
    // 并存可写句柄也不能在新读取期间偷偷修改。
    let writer = OpenOptions::new().write(true).open(&path).unwrap();
    assert!(read(&path, 4).is_err());
    drop(writer);
    assert_eq!(read(&path, 4).unwrap(), b"same");
}

#[test]
fn distribution_metadata_windows_final_symlink_and_junction_rejected() {
    let tree = Tree::new();
    let actual = tree.0.join("actual");
    fs::create_dir(&actual).unwrap();
    let path = actual.join("METADATA");
    fs::write(&path, b"same").unwrap();
    let link = tree.0.join("file-link");
    std::os::windows::fs::symlink_file(&path, &link)
        .expect("Windows安全测试需要可创建symlink的runner权限");
    // 直接测open，不让先行plain_path遮住末端nofollow缺陷。
    assert!(open_no_follow(&link).is_err());
    assert!(read(&link, 4).is_err());
    let junction = tree.0.join("junction");
    let output = std::process::Command::new("cmd.exe")
        .args(["/D", "/C", "mklink", "/J"])
        .arg(&junction)
        .arg(&actual)
        .output()
        .unwrap();
    assert!(output.status.success(), "创建junction失败：{output:?}");
    assert!(plain_path(&junction).is_err());
    assert!(read(&junction.join("METADATA"), 4).is_err());
    // 删除reparse本身，再清理真实目录；不让测试清理沿junction递归。
    fs::remove_dir(&junction).unwrap();
    fs::remove_file(&link).unwrap();
}
