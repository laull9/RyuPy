//! 在既有Mach-O闭包验证之后规划最少改写；未知加载上下文一律交回原严格重定位。
//! 只读取路径事实，不修改映像、不执行映像，也不按第三方名称选择规则。
use super::macho_identity::Image;
use std::collections::BTreeMap;
use std::path::{Component, Path, PathBuf};

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) enum Decision {
    Preserve,
    Strict(&'static str),
}

/// images必须来自同一轮冻结的原始身份表；target由旧解析器完成闭包/ABI/版本核验。
pub(crate) struct Plan<'a> {
    root: &'a Path,
    images: &'a BTreeMap<PathBuf, Image>,
    minimal: bool,
}

impl<'a> Plan<'a> {
    pub(crate) fn new(root: &'a Path, images: &'a BTreeMap<PathBuf, Image>) -> Self {
        Self {
            root,
            images,
            minimal: cfg!(feature = "experimental-minimal-macho-relocation"),
        }
    }

    /// 纯计划测试显式选择候选；生产new始终遵守编译feature，测试构建也不例外。
    #[cfg(test)]
    fn candidate(root: &'a Path, images: &'a BTreeMap<PathBuf, Image>) -> Self {
        Self {
            root,
            images,
            minimal: true,
        }
    }

    pub(crate) fn identity(&self, image: &Path) -> Decision {
        if !self.minimal {
            return Decision::Strict("默认严格重定位；最小计划仅编译期实验");
        }
        let Some(info) = self.images.get(image) else {
            return Decision::Strict("映像未登记");
        };
        let Some(id) = &info.identity else {
            return Decision::Strict("映像无可核验的LC_ID");
        };
        let suffix = id
            .name
            .strip_prefix("@rpath/")
            .or_else(|| id.name.strip_prefix("@loader_path/"));
        let Some(suffix) = suffix else {
            return Decision::Strict("LC_ID不是可搬迁相对身份");
        };
        if suffix.is_empty()
            || Path::new(suffix)
                .components()
                .any(|part| !matches!(part, Component::Normal(_)))
        {
            return Decision::Strict("LC_ID含未知路径分量");
        }
        if self
            .images
            .values()
            .filter(|candidate| {
                candidate
                    .identity
                    .as_ref()
                    .is_some_and(|other| other.name == id.name)
            })
            .count()
            != 1
        {
            return Decision::Strict("LC_ID存在多版本或重复拥有者");
        }
        Decision::Preserve
    }

    pub(crate) fn dependency(&self, image: &Path, name: &str, target: &Path) -> Decision {
        if !self.images.contains_key(image)
            || !self.images.contains_key(target)
            || !image.starts_with(self.root)
            || !target.starts_with(self.root)
        {
            return Decision::Strict("依赖未在部署闭包内核验");
        }
        if self.identity(target) != Decision::Preserve {
            return Decision::Strict("依赖目标身份需要严格重定位");
        }
        if name.starts_with("@loader_path/") {
            return if loader_path(image, name, self.root).as_deref() == Some(target) {
                Decision::Preserve
            } else {
                Decision::Strict("loader相对边与核验目标不一致")
            };
        }
        let Some(leaf) = name.strip_prefix("@rpath/") else {
            return Decision::Strict("加载边需要显式loader相对路径");
        };
        // leaf也必须留在runpath根内；不能借@rpath/../绕开局部目录证明。
        if Path::new(leaf)
            .components()
            .any(|part| !matches!(part, Component::Normal(_)))
        {
            return Decision::Strict("runpath后缀含未知路径分量");
        }
        let info = &self.images[image];
        let mut local_match = None;
        for rpath in &info.rpaths {
            let Some(root) = loader_path(image, rpath, self.root) else {
                return Decision::Strict("当前映像runpath缺少内部相对证明");
            };
            if let Ok(candidate) = root.join(leaf).canonicalize() {
                local_match = Some(candidate);
                break;
            }
        }
        if local_match.as_deref() != Some(target) {
            return Decision::Strict("当前映像runpath第一命中与核验目标不一致");
        }
        // 不猜动态dlopen入口：全拥有集都是潜在祖先，保守检查所有runpath。
        // 任一外部/未知根或相同leaf的另一个实际映像都要求原严格改写。
        for (ancestor, info) in self.images {
            for rpath in &info.rpaths {
                let Some(root) = loader_path(ancestor, rpath, self.root) else {
                    return Decision::Strict("潜在祖先runpath缺少内部相对证明");
                };
                if let Ok(candidate) = root.join(leaf).canonicalize()
                    && candidate != target
                {
                    return Decision::Strict("潜在祖先runpath存在其它命中");
                }
            }
        }
        Decision::Preserve
    }

    /// 调用者在切片提取和全部计划应用之后检查真实签名；仅提取原slice可保留有效签名。
    pub(crate) fn preserve_signature(
        &self,
        image: &Path,
        changed: bool,
        signature_valid: bool,
    ) -> bool {
        !changed && signature_valid && self.identity(image) == Decision::Preserve
    }
}

fn loader_path(image: &Path, value: &str, root: &Path) -> Option<PathBuf> {
    let suffix = value.strip_prefix("@loader_path")?;
    if !suffix.is_empty() && !suffix.starts_with('/') {
        return None;
    }
    let path = image
        .parent()?
        .join(suffix.trim_start_matches('/'))
        .canonicalize()
        .ok()?;
    path.starts_with(root).then_some(path)
}

#[cfg(test)]
#[path = "cpython_macho_relocation_plan_tests.rs"]
mod tests;
