//! 应用公开入口的原生加载边随真实owned搬移映射重定位；只改能证明原来命中被搬移文件的边。
//! 不按basename搜索库，不改系统/外部边；未知rpath保持原边，JIT apphost不调用本模块。
use anyhow::{Context, Result, ensure};
use std::collections::BTreeMap;
use std::path::{Component, Path, PathBuf};
use std::process::Command;

fn lexical(path: &Path) -> Option<PathBuf> {
    let mut result = PathBuf::new();
    for part in path.components() {
        match part {
            Component::CurDir => {}
            Component::ParentDir => {
                if !result.pop() {
                    return None;
                }
            }
            other => result.push(other.as_os_str()),
        }
    }
    Some(result)
}
struct Moves {
    files: BTreeMap<PathBuf, PathBuf>,
}
impl Moves {
    fn new(root: &Path, moves: &[(String, String)]) -> Result<Self> {
        let mut files = BTreeMap::new();
        for (old, new) in moves {
            crate::manifest_files::safe_relative(old)?;
            crate::manifest_files::safe_relative(new)?;
            if old == new {
                continue;
            }
            ensure!(
                files.insert(root.join(old), root.join(new)).is_none(),
                "入口依赖搬移映射重复"
            );
        }
        Ok(Self { files })
    }
    fn target(&self, path: &Path) -> Result<Option<PathBuf>> {
        let Some(path) = lexical(path) else {
            return Ok(None);
        };
        let Some(target) = self.files.get(&path) else {
            return Ok(None);
        };
        ensure!(
            crate::manifest_files::plain_metadata(target)?.is_file(),
            "入口依赖的搬移目标不是普通文件"
        );
        Ok(Some(target.clone()))
    }
}
fn expand(root: &Path, value: &str, macho: bool) -> Option<PathBuf> {
    let prefixes = if macho {
        ["@loader_path", "@executable_path"]
    } else {
        ["$ORIGIN", "${ORIGIN}"]
    };
    for prefix in prefixes {
        if value == prefix {
            return Some(root.to_owned());
        }
        if let Some(rest) = value.strip_prefix(&format!("{prefix}/")) {
            return lexical(&root.join(rest));
        }
    }
    Path::new(value).is_absolute().then(|| PathBuf::from(value))
}
fn selected(
    root: &Path,
    name: &str,
    rpaths: &[String],
    moves: &Moves,
    macho: bool,
) -> Result<Option<PathBuf>> {
    let leaf = if macho {
        name.strip_prefix("@rpath/")
    } else {
        (!name.contains('/')).then_some(name)
    };
    if let Some(leaf) = leaf {
        for rpath in rpaths {
            // 未物化的变量/相对路径可能抢先命中，不能越过去挑一个同名打包文件。
            let Some(base) = expand(root, rpath, macho) else {
                return Ok(None);
            };
            let candidate = base.join(leaf);
            if let Some(target) = moves.target(&candidate)? {
                return Ok(Some(target));
            }
            if candidate.try_exists()? {
                return Ok(None);
            }
        }
        return Ok(None);
    }
    let Some(path) = expand(root, name, macho) else {
        return Ok(None);
    };
    moves.target(&path)
}
fn run(program: &str, args: &[&std::ffi::OsStr]) -> Result<String> {
    let result = Command::new(program)
        .args(args)
        .output()
        .with_context(|| format!("无法启动入口重定位工具{program}"))?;
    ensure!(
        result.status.success(),
        "{program}入口重定位失败：{}",
        String::from_utf8_lossy(&result.stderr)
    );
    String::from_utf8(result.stdout).map_err(Into::into)
}
/// moves为canonicalize之前真实owned库存的逐文件(old_relative,new_relative)证明；调用时文件已搬入internal。
pub(crate) fn relocate(root: &Path, entry: &Path, moves: &[(String, String)]) -> Result<()> {
    let root = root.canonicalize()?;
    let entry = root.join(entry.file_name().context("入口缺少文件名")?);
    let files = Moves::new(&root, moves)?;
    if cfg!(target_os = "macos") {
        relocate_macho(&root, &entry, &files)
    } else if cfg!(target_os = "linux") {
        relocate_elf(&root, &entry, &files)
    } else {
        Ok(())
    }
}
fn relocate_macho(root: &Path, entry: &Path, moves: &Moves) -> Result<()> {
    use crate::cpython_provider::native::{
        macho_identity, macho_relocation_signature as signature,
    };
    let info = macho_identity::read(entry)?;
    // 任意LC改写前保存真实完整授权，不能依赖改写后codesign的隐式preserve语义。
    let original_entitlements = signature::snapshot(entry)?;
    let mut changed = false;
    for dependency in &info.dependencies {
        let Some(target) = selected(root, &dependency.name, &info.rpaths, moves, true)? else {
            continue;
        };
        let actual = macho_identity::read(&target)?
            .identity
            .context("入口依赖搬移目标缺少LC_ID_DYLIB")?;
        macho_identity::require_compatible(dependency, &actual)?;
        let relative = target
            .strip_prefix(root)?
            .to_str()
            .context("入口依赖路径不是UTF8")?;
        let replacement = format!("@loader_path/{relative}");
        if dependency.name != replacement {
            run(
                "install_name_tool",
                &[
                    "-change".as_ref(),
                    dependency.name.as_ref(),
                    replacement.as_ref(),
                    entry.as_os_str(),
                ],
            )?;
            changed = true;
        }
    }
    if changed {
        // 公开入口只在实际改边时重签，沿同一通用授权恢复器保原identifier/flags/runtime。
        let version = run("/usr/bin/sw_vers", &["-productVersion".as_ref()])?;
        let force_library_entitlements = version
            .trim()
            .split('.')
            .next()
            .context("codesign宿主版本为空")?
            .parse::<u32>()?
            >= 15;
        signature::finish(
            entry,
            false,
            true,
            force_library_entitlements,
            &original_entitlements,
        )?;
    }
    Ok(())
}
fn elf_fields(text: &str) -> (Vec<String>, Vec<String>) {
    let mut needed = Vec::new();
    let mut rpaths = Vec::new();
    for line in text.lines() {
        let Some((_, tail)) = line.split_once('[') else {
            continue;
        };
        let Some((value, _)) = tail.split_once(']') else {
            continue;
        };
        if line.contains("(NEEDED)") {
            needed.push(value.to_owned());
        }
        if line.contains("(RUNPATH)") || line.contains("(RPATH)") {
            rpaths.extend(value.split(':').map(str::to_owned));
        }
    }
    (needed, rpaths)
}
fn relocate_elf(root: &Path, entry: &Path, moves: &Moves) -> Result<()> {
    let report = run("readelf", &["-dW".as_ref(), entry.as_os_str()])?;
    let (needed, rpaths) = elf_fields(&report);
    for name in needed {
        let Some(target) = selected(root, &name, &rpaths, moves, false)? else {
            continue;
        };
        let relative = target
            .strip_prefix(root)?
            .to_str()
            .context("入口ELF依赖路径不是UTF8")?;
        let replacement = format!("$ORIGIN/{relative}");
        run(
            "patchelf",
            &[
                "--replace-needed".as_ref(),
                name.as_ref(),
                replacement.as_ref(),
                entry.as_os_str(),
            ],
        )?;
    }
    Ok(())
}
#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn only_exact_moved_loader_candidates_are_selected() {
        let root =
            std::env::temp_dir().join(format!("ryupy-entry-relocate-{}", std::process::id()));
        std::fs::create_dir_all(root.join("internal/deep")).unwrap();
        std::fs::write(root.join("internal/deep/libprobe"), "test").unwrap();
        let moves = Moves::new(
            &root,
            &[("deep/libprobe".into(), "internal/deep/libprobe".into())],
        )
        .unwrap();
        let target = root.join("internal/deep/libprobe");
        assert_eq!(
            selected(
                &root,
                "@rpath/libprobe",
                &["@loader_path/deep".into()],
                &moves,
                true
            )
            .unwrap(),
            Some(target.clone())
        );
        assert_eq!(
            selected(&root, "@executable_path/deep/libprobe", &[], &moves, true).unwrap(),
            Some(target.clone())
        );
        assert_eq!(
            selected(&root, "libprobe", &["${ORIGIN}/deep".into()], &moves, false).unwrap(),
            Some(target)
        );
        assert!(
            selected(
                &root,
                "@rpath/libprobe",
                &["unknown".into(), "@loader_path/deep".into()],
                &moves,
                true
            )
            .unwrap()
            .is_none()
        );
        assert!(
            selected(
                &root,
                "@rpath/missing",
                &["@loader_path/deep".into()],
                &moves,
                true
            )
            .unwrap()
            .is_none()
        );
        std::fs::remove_dir_all(root).unwrap();
    }
    #[test]
    fn elf_needed_and_search_order_are_kept_without_basename_inventory() {
        let (needed, rpaths) = elf_fields(
            "0x1 (NEEDED) Shared library: [liba.so.1]\n0x1d (RUNPATH) Library runpath: [$ORIGIN/first:/system/second]\n",
        );
        assert_eq!(needed, ["liba.so.1"]);
        assert_eq!(rpaths, ["$ORIGIN/first", "/system/second"]);
    }
}
