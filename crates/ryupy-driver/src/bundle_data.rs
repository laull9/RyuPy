//! 显式随包应用数据清单（JIT / NativeAOT / OneFile 共用）：
//! 从声明根冻结非 Python 文件的字节，发布时只消费快照；此根也提供编译源码的相对布局。
use super::delegation::DelegatedEntry;
use crate::Session;
use crate::manifest_files::{plain_metadata, safe_relative, unchanged};
use anyhow::{Context, Result, ensure};
use serde::Deserialize;
use std::fs::{File, OpenOptions};
use std::io::{Read, Write};
use std::path::{Path, PathBuf};
use std::sync::Arc;

const MAX_MANIFEST_BYTES: u64 = 1024 * 1024;
const MAX_FILES: usize = 1024;

#[derive(Clone, Debug, Default)]
pub(crate) struct BundleData {
    root: Option<PathBuf>,
    files: Vec<FrozenFile>,
}

#[derive(Clone, Debug)]
struct FrozenFile {
    destination: String,
    bytes: Arc<[u8]>,
}

impl BundleData {
    pub(crate) fn layout_root(&self) -> Option<&Path> {
        self.root.as_deref()
    }

    pub(super) fn destinations(&self) -> impl Iterator<Item = &str> {
        self.files.iter().map(|file| file.destination.as_str())
    }

    pub(super) fn validate_targets(&self, compiled: &[DelegatedEntry]) -> Result<()> {
        if self.root.is_some() {
            crate::resources::validate_unique_layout_artifact_paths(
                compiled
                    .iter()
                    .map(|source| source.destination.as_str())
                    .chain(self.destinations()),
            )
            .context("随包数据/编译源码目标冲突或不便携")?;
        }
        Ok(())
    }

    pub(super) fn stage(&self, python: &Path) -> Result<()> {
        for data in &self.files {
            let target = python.join(&data.destination);
            std::fs::create_dir_all(target.parent().context("随包数据缺少目标目录")?)?;
            // 发布树为本次独占目录；仍要求目标不存在，不能覆盖其他载荷。
            OpenOptions::new()
                .write(true)
                .create_new(true)
                .open(target)
                .with_context(|| format!("无法独占创建随包数据目标：{}", data.destination))?
                .write_all(&data.bytes)?;
        }
        Ok(())
    }
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Manifest {
    schema_version: u32,
    #[serde(default)]
    root: Option<String>,
    files: Vec<FileEntry>,
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct FileEntry {
    source: String,
    #[serde(default)]
    destination: Option<String>,
}

/// 读取并校验 `--bundle-data` 清单，将已冻结的文件映射写入当前 provider 委派计划。
pub(super) fn extend(sess: &mut Session) -> Result<()> {
    let Some(manifest_path) = &sess.bundle_data else {
        return Ok(());
    };
    let bytes = read_bounded(manifest_path, MAX_MANIFEST_BYTES).context("无法冻结随包数据清单")?;
    let manifest: Manifest = serde_json::from_slice(&bytes).context("随包数据清单不是有效 JSON")?;
    ensure!(
        manifest.schema_version == 1 && manifest.files.len() <= MAX_FILES,
        "随包数据清单版本或文件数量超限"
    );

    let base_dir = manifest_path
        .canonicalize()?
        .parent()
        .context("随包数据清单缺少父目录")?
        .to_path_buf();
    let root_dir = resolve_root(&base_dir, manifest.root.as_deref())?;

    let mut total_bytes = 0_u64;
    let mut entries = Vec::with_capacity(manifest.files.len());
    for item in &manifest.files {
        let dest = item.destination.as_deref().unwrap_or(&item.source);
        crate::resources::validate_layout_artifact_path(dest)?;
        ensure!(
            !python_path(dest) && !python_path(&item.source),
            "随包数据不能声明 Python 源码或字节码：{} -> {dest}",
            item.source
        );
        let source = resolve_source(&root_dir, &item.source)?;
        let bytes = read_bounded(&source, crate::resources::FILE_LIMIT)
            .with_context(|| format!("无法冻结随包数据文件：{}", item.source))?;
        total_bytes = total_bytes
            .checked_add(bytes.len() as u64)
            .context("随包数据总大小溢出")?;
        ensure!(
            total_bytes <= crate::resources::TOTAL_LIMIT,
            "随包数据总大小超过 64MiB"
        );
        entries.push(FrozenFile {
            destination: format!("ryupy_compiled/{dest}"),
            bytes: bytes.into(),
        });
    }

    let plan = sess
        .cpython_provider_state
        .plan
        .as_mut()
        .context("--bundle-data 需要启用 CPython provider")?;
    let data = BundleData {
        root: Some(root_dir),
        files: entries,
    };
    data.validate_targets(&[])?;
    plan.delegation.bundle_data = data;
    Ok(())
}

fn python_path(path: &str) -> bool {
    path.split('/').any(|part| {
        let lower = part.to_ascii_lowercase();
        lower == "__pycache__"
            || [".py", ".pyc", ".pyo"]
                .iter()
                .any(|ext| lower.ends_with(ext))
    })
}

fn read_bounded(path: &Path, limit: u64) -> Result<Vec<u8>> {
    let before = plain_metadata(path)?;
    ensure!(
        before.is_file() && before.len() <= limit,
        "不是普通文件或超过 {limit} 字节上限"
    );
    let file = File::open(path)?;
    let opened = file.metadata()?;
    ensure!(
        opened.is_file() && unchanged(&before, &opened)?,
        "文件在打开时变化"
    );
    let mut bytes = Vec::new();
    (&file).take(limit + 1).read_to_end(&mut bytes)?;
    let after = plain_metadata(path)?;
    ensure!(
        bytes.len() as u64 == before.len()
            && unchanged(&opened, &after)?
            && unchanged(&opened, &file.metadata()?)?,
        "文件在冻结期间变化"
    );
    Ok(bytes)
}

fn resolve_root(base_dir: &Path, configured: Option<&str>) -> Result<PathBuf> {
    let raw = match configured {
        None | Some(".") => base_dir.to_path_buf(),
        Some(text) => {
            let path = Path::new(text);
            if path.is_absolute() {
                path.to_path_buf()
            } else {
                base_dir.join(path)
            }
        }
    };
    let meta = plain_metadata(&raw).context("随包数据根目录不存在或为符号链接")?;
    ensure!(meta.is_dir(), "随包数据根目录不是普通目录");
    Ok(raw.canonicalize()?)
}

fn resolve_source(root: &Path, rel: &str) -> Result<PathBuf> {
    safe_relative(rel)?;
    let mut current = root.to_path_buf();
    for part in rel.split('/') {
        current.push(part);
        plain_metadata(&current)
            .with_context(|| format!("随包数据路径缺失或包含符号链接：{rel}"))?;
    }
    let meta = plain_metadata(&current)?;
    ensure!(meta.is_file(), "随包数据 source 不是普通文件：{rel}");
    let canonical = current.canonicalize()?;
    ensure!(
        canonical.starts_with(root),
        "随包数据 source 越出声明根目录：{rel}"
    );
    Ok(canonical)
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::cpython_provider::{ContainerTransfer, Plan, Source, version::PythonVersion};

    fn test_session(root: &Path, manifest: &Path) -> Session {
        let mut sess = Session {
            bundle_data: Some(manifest.to_path_buf()),
            ..Session::default()
        };
        sess.cpython_provider_state.plan = Some(Plan {
            version: PythonVersion::new(3, 12, 13),
            root: root.to_path_buf(),
            source: serde_json::from_value::<Source>(serde_json::json!({
                "project": "fixture",
                "build_id": "frozen",
                "source_url": "handwritten-fixture",
                "licenses": []
            }))
            .unwrap(),
            modules: Vec::new(),
            stdlib: Default::default(),
            declared: Default::default(),
            packages: Default::default(),
            container_transfer: ContainerTransfer::Proxy,
            payloads: Vec::new(),
            delegation: Default::default(),
            bridge: root.to_path_buf(),
            shim: root.join("shim"),
            _shim_build: None,
            dependency_report: serde_json::Value::Null,
        });
        sess
    }

    #[test]
    fn loads_valid_entries_and_rejects_python_sources_and_traversal() {
        let root = crate::test_temp::Root::new("bundle-data");
        std::fs::create_dir_all(root.join("locales")).unwrap();
        std::fs::write(root.join("pyproject.toml"), "[project]\nversion=\"1.0\"\n").unwrap();
        std::fs::write(root.join("locales/translations.json"), "{}").unwrap();
        std::fs::write(root.join("secret.py"), "x = 1\n").unwrap();

        let manifest = root.join("bundle-data.json");
        std::fs::write(
            &manifest,
            serde_json::json!({
                "schema_version": 1,
                "files": [
                    {"source": "pyproject.toml", "destination": "pyproject.toml"},
                    {"source": "locales/translations.json"}
                ]
            })
            .to_string(),
        )
        .unwrap();
        let mut sess = test_session(&root, &manifest);
        extend(&mut sess).unwrap();
        let staged = &sess
            .cpython_provider_state
            .plan
            .as_ref()
            .unwrap()
            .delegation
            .bundle_data
            .files;
        assert_eq!(staged.len(), 2);
        assert_eq!(staged[0].destination, "ryupy_compiled/pyproject.toml");
        assert_eq!(
            staged[1].destination,
            "ryupy_compiled/locales/translations.json"
        );

        // 发布只消费冻结快照，不再从可能已移动的输入树取资源。
        std::fs::write(root.join("locales/translations.json"), "changed").unwrap();
        let published = root.join("published");
        let plan = &sess
            .cpython_provider_state
            .plan
            .as_ref()
            .unwrap()
            .delegation;
        super::super::delegation::stage(plan, &published).unwrap();
        assert_eq!(
            std::fs::read(published.join("ryupy_compiled/locales/translations.json")).unwrap(),
            b"{}"
        );
        assert!(super::super::delegation::stage(plan, &published).is_err());

        std::fs::write(
            &manifest,
            serde_json::json!({
                "schema_version": 1,
                "files": [{"source": "secret.py", "destination": "secret.txt"}]
            })
            .to_string(),
        )
        .unwrap();
        let mut bad_py = test_session(&root, &manifest);
        assert!(extend(&mut bad_py).is_err());

        std::fs::write(
            &manifest,
            serde_json::json!({
                "schema_version": 1,
                "files": [{"source": "pyproject.toml", "destination": "../escape.toml"}]
            })
            .to_string(),
        )
        .unwrap();
        let mut bad_dest = test_session(&root, &manifest);
        assert!(extend(&mut bad_dest).is_err());
    }

    #[test]
    fn rejects_file_directory_and_case_aliases_before_publication() {
        let root = crate::test_temp::Root::new("bundle-data-collision");
        let manifest = root.join("data.json");
        std::fs::write(root.join("input.txt"), "data").unwrap();
        for (first, second) in [
            ("docs", "docs/help.txt"),
            ("docs/help.txt", "Docs/other.txt"),
            ("docs/help.txt", "docs/help.txt"),
        ] {
            std::fs::write(
                &manifest,
                serde_json::json!({
                    "schema_version": 1,
                    "files": [
                        {"source": "input.txt", "destination": first},
                        {"source": "input.txt", "destination": second}
                    ],
                })
                .to_string(),
            )
            .unwrap();
            let mut sess = test_session(&root, &manifest);
            assert!(extend(&mut sess).is_err(), "{first} 与 {second}");
        }
        std::fs::write(
            &manifest,
            r#"{"schema_version":1,"files":[{"source":"input.txt","destination":"src"}]}"#,
        )
        .unwrap();
        let mut sess = test_session(&root, &manifest);
        extend(&mut sess).unwrap();
        let plan = &mut sess
            .cpython_provider_state
            .plan
            .as_mut()
            .unwrap()
            .delegation;
        plan.compiled_sources.push(DelegatedEntry {
            source: root.join("nonexistent.py"),
            destination: "ryupy_compiled/src/main.py".into(),
        });
        let published = root.join("published");
        assert!(super::super::delegation::stage(plan, &published).is_err());
        assert!(!published.exists());
    }

    #[cfg(unix)]
    #[test]
    fn rejects_links_even_when_target_is_inside_the_declared_root() {
        let root = crate::test_temp::Root::new("bundle-data-link");
        std::fs::create_dir(root.join("real")).unwrap();
        std::fs::write(root.join("real/input.txt"), "data").unwrap();
        std::os::unix::fs::symlink(root.join("real"), root.join("alias")).unwrap();
        assert!(resolve_source(&root, "alias/input.txt").is_err());
        std::os::unix::fs::symlink(root.join("real/input.txt"), root.join("link.txt")).unwrap();
        assert!(resolve_source(&root, "link.txt").is_err());
    }
}
