//! 显式原生组件的来源、许可与Mach-O ABI冻结；复用既有payload部署，不加载用户动态库。
use super::{Payload, Session, native, relative};
use anyhow::{Context, Result, bail, ensure};
use native::macho_identity::{self, Version};
use serde::{Deserialize, Serialize};
use std::collections::{BTreeMap, BTreeSet};
use std::path::{Component, Path, PathBuf};

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Declaration {
    schema: u32,
    components: Vec<ComponentDeclaration>,
}
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct ComponentDeclaration {
    name: String,
    version: String,
    source_url: String,
    root: PathBuf,
    libraries: Vec<LibraryDeclaration>,
    licenses: Vec<FileDeclaration>,
}
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct LibraryDeclaration {
    path: PathBuf,
    destination: String,
    install_name: String,
    compatibility_version: String,
    current_version: String,
    architecture: String,
    #[serde(default)]
    load_names: Vec<String>,
}
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct FileDeclaration {
    path: PathBuf,
    destination: String,
}
#[derive(Serialize, Deserialize)]
struct LibraryReport {
    source: PathBuf,
    destination: String,
    install_name: String,
    compatibility_version: String,
    current_version: String,
    architecture: String,
    load_names: Vec<String>,
}
#[derive(Serialize, Deserialize)]
struct Report {
    name: String,
    version: String,
    source_url: String,
    root: PathBuf,
    libraries: Vec<LibraryReport>,
    licenses: Vec<String>,
}
/// 只接受声明中已核验的精确旧加载名；实际目标仍需按映像自身身份和版本复验。
pub(crate) use native::macho_identity::Alias;

fn destination(text: &str) -> Result<String> {
    relative(text)?;
    let parts = Path::new(text)
        .components()
        .map(|part| match part {
            Component::Normal(value) => value.to_str().context("原生组件目标不是UTF-8"),
            _ => bail!("原生组件目标含非普通分量"),
        })
        .collect::<Result<Vec<_>>>()?;
    Ok(parts.join("/"))
}
fn source(root: &Path, path: &Path) -> Result<PathBuf> {
    relative(path.to_str().context("组件源相对路径不是UTF-8")?)?;
    let source = root
        .join(path)
        .canonicalize()
        .context("原生组件文件缺失或链接循环")?;
    ensure!(
        source.starts_with(root) && source.is_file(),
        "原生组件源越界或不是普通文件：{}",
        path.display()
    );
    Ok(source)
}

pub(super) fn extend(sess: &mut Session) -> Result<()> {
    let Some(path) = &sess.cpython_native_components else {
        return Ok(());
    };
    let path = path.canonicalize().context("原生组件声明不存在")?;
    let base = path.parent().context("原生组件声明缺少父目录")?;
    let declaration: Declaration =
        serde_json::from_slice(&std::fs::read(&path)?).context("原生组件声明格式错误")?;
    ensure!(declaration.schema == 1, "原生组件声明schema必须为1");
    let plan = sess
        .cpython_provider_state
        .plan
        .as_ref()
        .context("原生组件需要CPython provider")?;
    let mut destinations: BTreeSet<_> = plan
        .payloads
        .iter()
        .map(|p| destination(&p.destination))
        .collect::<Result<_>>()?;
    let mut aliases = BTreeMap::new();
    let mut names = BTreeSet::new();
    let mut payloads = Vec::new();
    let mut reports = Vec::new();
    for component in declaration.components {
        ensure!(
            !component.name.is_empty()
                && !component.version.is_empty()
                && !component.source_url.is_empty(),
            "原生组件必须登记名称、版本和来源"
        );
        ensure!(
            names.insert(component.name.clone()),
            "原生组件名称重复：{}",
            component.name
        );
        ensure!(
            !component.libraries.is_empty() && !component.licenses.is_empty(),
            "原生组件必须包含库与许可证：{}",
            component.name
        );
        let root = super::absolute(base, &component.root)?
            .canonicalize()
            .context("原生组件根不存在")?;
        ensure!(root.is_dir(), "原生组件根不是目录");
        let mut libraries = Vec::new();
        for library in component.libraries {
            ensure!(
                library.architecture == native::rid()? && library.architecture == "osx-arm64",
                "原生组件目标架构不匹配或当前格式未支持：{}",
                library.architecture
            );
            let file = source(&root, &library.path)?;
            native::check_architecture(&file, &library.architecture)?;
            let actual = macho_identity::read(&file)?
                .identity
                .context("原生组件缺少Mach-O LC_ID_DYLIB")?;
            ensure!(
                actual.name == library.install_name,
                "原生组件LC_ID身份不匹配：{}",
                file.display()
            );
            ensure!(
                actual.compatibility == Version::parse(&library.compatibility_version)?
                    && actual.current == Version::parse(&library.current_version)?,
                "原生组件声明版本与真实映像不符：{}",
                file.display()
            );
            let target = destination(&library.destination)?;
            ensure!(
                destinations.insert(target.clone()),
                "原生组件目标冲突：{target}"
            );
            let mut loads = library.load_names;
            loads.push(actual.name.clone());
            loads.sort();
            loads.dedup();
            for load in &loads {
                ensure!(
                    !load.is_empty() && !load.chars().any(char::is_control),
                    "原生组件加载名非法"
                );
                ensure!(
                    aliases.insert(load.clone(), target.clone()).is_none(),
                    "原生组件加载名冲突：{load}"
                );
            }
            payloads.push(Payload {
                source: file.clone(),
                destination: target.clone(),
            });
            libraries.push(LibraryReport {
                source: file,
                destination: target,
                install_name: actual.name,
                compatibility_version: actual.compatibility.to_string(),
                current_version: actual.current.to_string(),
                architecture: library.architecture,
                load_names: loads,
            });
        }
        let mut licenses = Vec::new();
        for license in component.licenses {
            let file = source(&root, &license.path)?;
            ensure!(
                std::fs::metadata(&file)?.len() > 0,
                "原生组件许可证为空：{}",
                file.display()
            );
            let target = destination(&license.destination)?;
            ensure!(
                destinations.insert(target.clone()),
                "原生组件许可证目标冲突：{target}"
            );
            payloads.push(Payload {
                source: file,
                destination: target.clone(),
            });
            licenses.push(target);
        }
        reports.push(Report {
            name: component.name,
            version: component.version,
            source_url: component.source_url,
            root,
            libraries,
            licenses,
        });
    }
    let plan = sess
        .cpython_provider_state
        .plan
        .as_mut()
        .context("原生组件计划丢失")?;
    plan.payloads.extend(payloads);
    if plan.dependency_report.is_null() {
        plan.dependency_report = serde_json::json!({});
    }
    plan.dependency_report["native_components"] = serde_json::to_value(reports)?;
    Ok(())
}

pub(super) fn aliases(
    report: &serde_json::Value,
    site: &Path,
    output: &Path,
) -> Result<Vec<Alias>> {
    let Some(components) = report.get("native_components") else {
        return Ok(Vec::new());
    };
    let reports: Vec<Report> = serde_json::from_value(components.clone())?;
    let mut result = Vec::new();
    for component in reports {
        for library in component.libraries {
            let target = output.join(site).join(destination(&library.destination)?);
            for load_name in library.load_names {
                result.push(Alias {
                    load_name,
                    target: target.clone(),
                    install_name: library.install_name.clone(),
                });
            }
        }
    }
    Ok(result)
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn old_sessions_do_not_require_component_declaration() {
        let mut session = Session::default();
        extend(&mut session).unwrap();
        assert!(session.cpython_provider_state.plan.is_none());
    }
    #[test]
    fn component_paths_keep_boundary_and_normalize_target_aliases() {
        let root = crate::test_temp::Root::new("native-component-paths");
        std::fs::write(root.join("LICENSE"), "handwritten fixture license").unwrap();
        let canonical = root.canonicalize().unwrap();
        assert!(source(&canonical, Path::new("LICENSE")).is_ok());
        assert!(source(&canonical, Path::new("../escape")).is_err());
        assert!(source(&canonical, Path::new("missing")).is_err());
        assert_eq!(
            destination("native/./libsample.dylib").unwrap(),
            "native/libsample.dylib"
        );
        assert!(destination("../escape").is_err());
    }
}
