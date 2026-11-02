//! Windows词法和实际身份分开验证；Unix测试不冒充Windows路径行为。
use super::*;

#[cfg(not(windows))]
#[test]
fn non_windows_paths_are_unchanged() {
    let path = Path::new("/tmp/\\\\?\\literal & 中文");
    assert_eq!(sdk_path(path).unwrap(), path);
}

#[cfg(windows)]
#[test]
fn known_disk_unc_namespaces_preserve_components() {
    for (source, expected) in [
        (
            r"\\?\D:\根 & output\x$(v);%.dll",
            r"D:\根 & output\x$(v);%.dll",
        ),
        (
            r"\\?\UNC\server\share\根 & x.dll",
            r"\\server\share\根 & x.dll",
        ),
        (r"D:\plain\x.dll", r"D:\plain\x.dll"),
    ] {
        assert_eq!(
            ordinary_windows_path(Path::new(source)).unwrap(),
            Path::new(expected)
        );
    }
    for source in [
        r"\\.\pipe\x",
        r"\\?\GLOBALROOT\Device\x",
        r"\\?\Volume{abc}\x",
        r"C:relative",
        r"\relative",
        "relative",
        r"\\?\C:\x.\y",
        r"\\?\C:\x \y",
        r"\\?\C:\NUL.txt",
        r"\\?\C:\a\..\b",
    ] {
        assert!(
            ordinary_windows_path(Path::new(source)).is_err(),
            "{source}"
        );
    }
}

#[cfg(windows)]
#[test]
fn length_is_utf16_units_not_utf8_or_scalar_count() {
    // C:\占3单位；每个emoji占2单位。
    let accepted = format!(r"\\?\C:\{}", "😀".repeat(128));
    assert!(ordinary_windows_path(Path::new(&accepted)).is_ok());
    assert!(ordinary_windows_path(Path::new(&(accepted + "a"))).is_err());
}

#[cfg(windows)]
#[test]
fn actual_files_and_sdk_directories_roundtrip_to_same_canonical_identity() {
    let (root, _) = super::super::owned_root(&std::env::temp_dir(), "sdk_path_test").unwrap();
    let result = (|| -> Result<()> {
        let directory = root.join("中文 & output with spaces");
        std::fs::create_dir(&directory)?;
        for child in ["host", "publish"] {
            std::fs::create_dir(directory.join(child))?;
        }
        for child in ["input$(v);%.dll", "RyuPy.Runtime.dll", "host/Host.csproj"] {
            let file = directory.join(child);
            std::fs::write(&file, b"original-bytes")?;
            let converted = sdk_path(&file)?;
            assert!(converted.is_absolute());
            assert!(
                matches!(converted.components().next(), Some(std::path::Component::Prefix(prefix))
                if matches!(prefix.kind(), std::path::Prefix::Disk(_) | std::path::Prefix::UNC(_, _)))
            );
            assert_eq!(converted.canonicalize()?, file.canonicalize()?);
            assert_eq!(std::fs::read(converted)?, b"original-bytes");
        }
        for child in ["host", "publish"] {
            let original = directory.join(child);
            assert_eq!(
                sdk_path(&original)?.canonicalize()?,
                original.canonicalize()?
            );
        }
        assert!(sdk_path(&directory.join("missing.dll")).is_err());
        Ok(())
    })();
    std::fs::remove_dir_all(root).unwrap();
    result.unwrap();
}
