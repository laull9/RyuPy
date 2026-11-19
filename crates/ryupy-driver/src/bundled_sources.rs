//! 计算编译源码的随包位置；显式数据根保留原目录关系，未声明时保持按模块名部署。
use crate::modules::{SourceKind, SourceModule};
use anyhow::{Context, Result};
use std::path::Path;

/// `-I` 只参与名称解析；布局使用实际源码相对声明根，不以模块名倒推目录层数。
pub(crate) fn bundled_relative_path(root: Option<&Path>, source: &SourceModule) -> Result<String> {
    if let Some(root) = root {
        let actual = source.resolved_source_path.canonicalize()?;
        if let Ok(relative) = actual.strip_prefix(root) {
            // 保留现有 __main__ 锚点命名；同一份文件仍可按普通模块名加载，二者互不覆盖。
            let relative = if source.kind == SourceKind::Entry {
                relative.with_file_name("__main__.py")
            } else {
                relative.to_path_buf()
            };
            let relative = relative
                .iter()
                .map(|part| part.to_str().context("编译源码随包路径必须是 UTF-8"))
                .collect::<Result<Vec<_>>>()?
                .join("/");
            crate::resources::validate_layout_artifact_path(&format!(
                "python/ryupy_compiled/{relative}"
            ))
            .with_context(|| format!("编译源码随包路径不便携：{relative}"))?;
            return Ok(format!("ryupy_compiled/{relative}"));
        }
    }
    let module_file = if source.name == "__main__" {
        "__main__.py".to_owned()
    } else {
        format!(
            "{}{}",
            source.name.replace('.', "/"),
            if source.kind == SourceKind::Package {
                "/__init__.py"
            } else {
                ".py"
            }
        )
    };
    Ok(format!("ryupy_compiled/{module_file}"))
}

#[cfg(test)]
mod tests {
    use super::*;
    use ryupy_base::FileId;
    use ryupy_syntax::ParsedModule;
    use std::path::PathBuf;

    fn dummy_source(name: &str, kind: SourceKind, path: PathBuf) -> SourceModule {
        let (parsed, _) = ryupy_syntax::parse(FileId::from_raw(0.into()), "");
        SourceModule {
            candidate: false,
            hard: true,
            catalog: None,
            parse_diagnostics: Vec::new(),
            kind,
            resource_discovery: None,
            resolved_source_path: path,
            name: name.to_owned(),
            package: String::new(),
            file: String::new(),
            logical_source: String::new(),
            parsed: ParsedModule {
                file: FileId::from_raw(0.into()),
                module: parsed.module,
            },
            text: String::new(),
        }
    }

    #[test]
    fn preserves_declared_tree_without_changing_legacy_anchors() {
        let root = crate::test_temp::Root::new("bundled-sources");
        std::fs::create_dir_all(root.join("src/gui")).unwrap();
        std::fs::write(root.join("translator.py"), "").unwrap();
        std::fs::write(root.join("src/version.py"), "").unwrap();
        std::fs::write(root.join("src/main.py"), "").unwrap();
        std::fs::write(root.join("src/gui/__init__.py"), "").unwrap();

        let entry = dummy_source("__main__", SourceKind::Entry, root.join("src/main.py"));
        assert_eq!(
            bundled_relative_path(None, &entry).unwrap(),
            "ryupy_compiled/__main__.py"
        );
        assert_eq!(
            bundled_relative_path(Some(&root), &entry).unwrap(),
            "ryupy_compiled/src/__main__.py"
        );
        // 同一物理位置不会因 -I 导致的两种模块拼写而被重复加 src 前缀。
        for name in ["version", "src.version"] {
            let source = dummy_source(name, SourceKind::ModuleFile, root.join("src/version.py"));
            assert_eq!(
                bundled_relative_path(Some(&root), &source).unwrap(),
                "ryupy_compiled/src/version.py"
            );
        }
        assert_eq!(
            bundled_relative_path(
                Some(&root),
                &dummy_source(
                    "translator",
                    SourceKind::ModuleFile,
                    root.join("translator.py")
                )
            )
            .unwrap(),
            "ryupy_compiled/translator.py"
        );
        assert_eq!(
            bundled_relative_path(
                Some(&root),
                &dummy_source("gui", SourceKind::Package, root.join("src/gui/__init__.py"))
            )
            .unwrap(),
            "ryupy_compiled/src/gui/__init__.py"
        );
    }
}
