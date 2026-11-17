//! 必要重定位计划的独立目录布局、身份冲突和祖先runpath回退测试。
use super::super::macho_identity::{Dylib, Version};
use super::*;

struct Fixture(PathBuf);
impl Fixture {
    fn new() -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let root = std::env::temp_dir().join(format!(
            "ryupy-relocation-plan-{}-{stamp}",
            std::process::id()
        ));
        std::fs::create_dir(&root).unwrap();
        Self(root.canonicalize().unwrap())
    }
    fn file(&self, relative: &str) -> PathBuf {
        let path = self.0.join(relative);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(&path, b"fixture").unwrap();
        path
    }
}
impl Drop for Fixture {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn image(id: &str, rpaths: &[&str]) -> Image {
    Image {
        identity: Some(Dylib {
            name: id.into(),
            compatibility: Version([1, 0, 0]),
            current: Version([1, 0, 0]),
        }),
        dependencies: vec![],
        rpaths: rpaths.iter().map(|value| (*value).into()).collect(),
    }
}

#[test]
fn independent_layouts_preserve_only_equivalent_internal_paths() {
    for (folder, library) in [
        ("copper/assets", "libpetal.dylib"),
        ("quartz/nested/foreign", "libstone.4.dylib"),
    ] {
        let f = Fixture::new();
        let owner = f.file(&format!("{folder}/consumer.dylib"));
        let target = f.file(&format!("{folder}/libraries/{library}"));
        let images = BTreeMap::from([
            (
                owner.clone(),
                image("@rpath/consumer.dylib", &["@loader_path/libraries"]),
            ),
            (target.clone(), image(&format!("@rpath/{library}"), &[])),
        ]);
        let plan = Plan::new(&f.0, &images);
        assert_eq!(
            plan.dependency(&owner, &format!("@rpath/{library}"), &target),
            Decision::Preserve
        );
        assert_eq!(
            plan.dependency(
                &owner,
                &format!("@loader_path/libraries/{library}"),
                &target
            ),
            Decision::Preserve
        );
        assert!(plan.preserve_signature(&owner, false, true));
        for (changed, valid) in [(true, true), (false, false)] {
            assert!(!plan.preserve_signature(&owner, changed, valid));
        }
    }
}

#[test]
fn duplicate_identity_versions_use_strict_path_even_with_local_hit() {
    let f = Fixture::new();
    let first = f.file("first/libsame.dylib");
    let second = f.file("second/libsame.dylib");
    let mut alternate = image("@rpath/libsame.dylib", &[]);
    alternate.identity.as_mut().unwrap().current = Version([2, 0, 0]);
    let images = BTreeMap::from([
        (
            first.clone(),
            image("@rpath/libsame.dylib", &["@loader_path"]),
        ),
        (second, alternate),
    ]);
    let plan = Plan::new(&f.0, &images);
    assert!(matches!(plan.identity(&first), Decision::Strict(_)));
    assert!(matches!(
        plan.dependency(&first, "@loader_path/libsame.dylib", &first),
        Decision::Strict(_)
    ));
}

#[test]
fn ancestor_shadow_and_unknown_or_escaping_roots_force_strict_relocation() {
    let f = Fixture::new();
    let owner = f.file("own/consumer.dylib");
    let target = f.file("own/libsample.dylib");
    let ancestor = f.file("other/host.dylib");
    f.file("other/libsample.dylib");
    for rpath in [
        "@loader_path",
        "@executable_path",
        "/tmp",
        "@loader_path/../../",
        "relative/unknown",
    ] {
        let images = BTreeMap::from([
            (
                owner.clone(),
                image("@rpath/consumer.dylib", &["@loader_path"]),
            ),
            (target.clone(), image("@rpath/libsample.dylib", &[])),
            (ancestor.clone(), image("@rpath/host.dylib", &[rpath])),
        ]);
        assert!(
            matches!(
                Plan::new(&f.0, &images).dependency(&owner, "@rpath/libsample.dylib", &target),
                Decision::Strict(_)
            ),
            "{rpath}"
        );
    }
}

#[test]
fn missing_first_hit_external_id_and_no_id_cannot_preserve_signature() {
    let f = Fixture::new();
    let owner = f.file("consumer.dylib");
    let target = f.file("nested/libsample.dylib");
    let mut images = BTreeMap::from([
        (
            owner.clone(),
            image("/build/consumer.dylib", &["@loader_path"]),
        ),
        (target.clone(), image("@rpath/libsample.dylib", &[])),
    ]);
    let plan = Plan::new(&f.0, &images);
    assert!(matches!(
        plan.dependency(&owner, "@rpath/libsample.dylib", &target),
        Decision::Strict(_)
    ));
    assert!(!plan.preserve_signature(&owner, false, true));
    images.get_mut(&owner).unwrap().identity = None;
    assert!(!Plan::new(&f.0, &images).preserve_signature(&owner, false, true));
}

#[cfg(unix)]
#[test]
fn symlink_escape_and_relative_parent_identity_never_preserve() {
    let f = Fixture::new();
    let outside = Fixture::new();
    let owner = f.file("consumer.dylib");
    let target = outside.file("libsample.dylib");
    std::os::unix::fs::symlink(&outside.0, f.0.join("linked")).unwrap();
    let images = BTreeMap::from([
        (
            owner.clone(),
            image("@rpath/../consumer.dylib", &["@loader_path/linked"]),
        ),
        (target.clone(), image("@rpath/libsample.dylib", &[])),
    ]);
    let plan = Plan::new(&f.0, &images);
    assert!(matches!(plan.identity(&owner), Decision::Strict(_)));
    assert!(matches!(
        plan.dependency(&owner, "@rpath/libsample.dylib", &target),
        Decision::Strict(_)
    ));
    assert!(loader_path(&owner, "@loader_path/linked", &f.0).is_none());
}
