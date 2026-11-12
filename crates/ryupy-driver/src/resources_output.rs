//! 仅排除实际build/dist与显式资源树的交集；共同输出父目录不是排除根。
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};

pub(super) fn reject_overlap(root: &Path, directory: &str, outputs: &[PathBuf]) -> Result<()> {
    if outputs.is_empty() {
        return Ok(());
    }
    if directory != "." {
        super::paths::portable(directory)?;
    }
    // 源码宿主根允许平台规范别名，声明根及内部组件仍拒绝链接/reparse。
    ensure!(
        crate::manifest_files::plain_metadata(root)?.is_dir(),
        "资源根不是普通目录"
    );
    let mut selected = root.to_owned();
    if directory != "." {
        for component in directory.split('/') {
            selected.push(component);
            match std::fs::symlink_metadata(&selected) {
                Ok(_) => {
                    ensure!(
                        crate::manifest_files::plain_metadata(&selected)?.is_dir(),
                        "资源树不是普通目录"
                    );
                }
                Err(error) if error.kind() == std::io::ErrorKind::NotFound => {}
                Err(error) => return Err(error.into()),
            }
        }
    }
    let selected = canonical_projected(&selected)?;
    for output in outputs {
        let output = canonical_projected(output)?;
        ensure!(
            !intersects(&selected, &output)?,
            "资源树与实际AOT生成输出重叠：{} 与 {}；请用-o迁移输出或收窄directory，使资源树与.build/.dist分离",
            selected.display(),
            output.display()
        );
    }
    Ok(())
}

// dist首次构建尚不存在；从最近存在的祖先解析平台别名，再拼回合法组件。
fn canonical_projected(path: &Path) -> Result<PathBuf> {
    let mut ancestor = path;
    let mut missing = Vec::new();
    loop {
        match std::fs::symlink_metadata(ancestor) {
            Ok(_) => {
                ensure!(
                    crate::manifest_files::plain_metadata(ancestor)?.is_dir(),
                    "输出/资源边界不是普通目录"
                );
                let mut canonical = ancestor.canonicalize().context("无法解析资源/输出边界")?;
                for component in missing.iter().rev() {
                    canonical.push(component);
                }
                return Ok(canonical);
            }
            Err(error) if error.kind() == std::io::ErrorKind::NotFound => {
                missing.push(
                    ancestor
                        .file_name()
                        .context("资源/输出边界没有存在的祖先")?,
                );
                ancestor = ancestor.parent().context("资源/输出边界没有父目录")?;
            }
            Err(error) => return Err(error.into()),
        }
    }
}
fn intersects(left: &Path, right: &Path) -> Result<bool> {
    // canonicalize展开Windows长短名/卷别名；Windows再保守忽略大小写。
    #[cfg(windows)]
    {
        let components = |path: &Path| {
            path.components()
                .map(|c| {
                    c.as_os_str()
                        .to_str()
                        .map(str::to_lowercase)
                        .context("Windows资源/输出边界无法无损比较")
                })
                .collect::<Result<Vec<_>>>()
        };
        let left = components(left)?;
        let right = components(right)?;
        Ok(left.starts_with(&right) || right.starts_with(&left))
    }
    #[cfg(not(windows))]
    {
        Ok(left.starts_with(right) || right.starts_with(left))
    }
}

#[cfg(test)]
#[path = "resources_output_tests.rs"]
mod tests;
