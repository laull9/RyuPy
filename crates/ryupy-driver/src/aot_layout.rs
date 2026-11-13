//! 高层AOT固定布局：锁住同名目标，在owned build中构建，校验完整候选后替换dist。
//! 两次目录rename不是跨平台原子交换；回滚/元数据故障保留现场并明确失败。
use crate::{Artifacts, BuildOptions, manifest_files};
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::io::Write;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};
#[path = "aot_layout_files.rs"]
mod files;
#[path = "aot_layout_symbols.rs"]
mod symbols;
static NEXT: AtomicU64 = AtomicU64::new(0);
#[derive(Serialize, Deserialize)]
#[serde(deny_unknown_fields)]
struct Owner {
    version: u32,
    source: String,
    stem: String,
    build: Vec<files::Node>,
    dist: Option<Vec<files::Node>>,
}
/// 仅外层排他创建attempt后可构造；调用者不能靠目录名字伪造资格。
pub(crate) struct OwnedAttempt {
    root: PathBuf,
    output_roots: [PathBuf; 2],
}
impl OwnedAttempt {
    pub(crate) fn output_roots(&self) -> &[PathBuf] {
        &self.output_roots
    }
    pub(crate) fn create_child(&self, parent: &Path, name: &str) -> Result<PathBuf> {
        ensure!(
            matches!(name, "ryupy-binding-0" | "ryupy-aot-0"),
            "非法固定工作根"
        );
        let relative = parent
            .strip_prefix(&self.root)
            .context("工作父目录不属于本轮attempt")?;
        ensure!(
            relative
                .components()
                .all(|c| matches!(c, std::path::Component::Normal(_))),
            "工作路径含相对逃逸组件"
        );
        let mut checked = self.root.clone();
        ensure!(
            manifest_files::plain_metadata(&checked)?.is_dir(),
            "attempt不是普通目录"
        );
        for component in relative.components() {
            checked.push(component);
            ensure!(
                manifest_files::plain_metadata(&checked)?.is_dir(),
                "工作祖先不是普通目录"
            );
        }
        let path = parent.join(name);
        // 只有成功create_dir后才拥有清理权；已有目录/链接不能接管。
        std::fs::create_dir(&path).context("本轮短工作根已存在或无法独占创建，拒绝复用")?;
        Ok(path)
    }
}
struct Lock(PathBuf);
impl Drop for Lock {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir(&self.0);
    }
}

/// aot=true的out_dir是两棵目录的共同ROOT；闭包仅见本轮独占attempt。
pub(crate) fn build(
    opts: &BuildOptions,
    compile: impl FnOnce(&BuildOptions, &OwnedAttempt) -> Result<Artifacts>,
) -> Result<Artifacts> {
    ensure!(opts.aot, "布局事务只用于AOT");
    crate::aot::validate_before_build(&opts.input, true)?;
    let stem = crate::assembly_name(&opts.input);
    crate::binding_build::validate_name(&stem)?;
    let source = opts.input.canonicalize().context("AOT源文件不存在")?;
    // ROOT是用户选择的可信边界，先解析平台临时目录别名；边界内不允许链接/reparse。
    std::fs::create_dir_all(&opts.out_dir)?;
    let root = opts.out_dir.canonicalize()?;
    let build = root.join(format!("{stem}.build"));
    let dist = root.join(format!("{stem}.dist"));
    let lock = root.join(format!("{stem}.build.lock"));
    std::fs::create_dir(&lock).context("同名AOT已有构建锁（或崩溃遗留）；拒绝并发/自动清理")?;
    let _lock = Lock(lock);
    let source = source.to_str().context("源路径必须为UTF8")?.to_owned();
    let mut owner = if files::exists(&build)? {
        ensure!(
            manifest_files::plain_metadata(&build)?.is_dir(),
            "已有build不是普通目录"
        );
        let owner: Owner = serde_json::from_slice(&files::read_json(&build.join(files::OWNER))?)?;
        ensure!(
            owner.version == 1 && owner.source == source && owner.stem == stem,
            "已有build没有匹配的工具所有权"
        );
        ensure!(
            owner.build.len() <= 100_000 && owner.dist.as_ref().is_none_or(|d| d.len() <= 100_000),
            "owner条目过多"
        );
        ensure!(
            files::snapshot(&build, true)? == owner.build,
            "已有build被修改/新增文件，拒绝覆盖"
        );
        owner
    } else {
        ensure!(!files::exists(&dist)?, "dist存在但无owned build，拒绝接管");
        std::fs::create_dir(&build)?;
        let owner = Owner {
            version: 1,
            source,
            stem: stem.clone(),
            build: vec![],
            dist: None,
        };
        save_owner(&build, &owner)?;
        owner
    };
    verify_dist(&dist, &owner.dist)?;
    let attempt = unique_attempt(&build)?;
    let mut inner = opts.clone();
    inner.out_dir = attempt.clone();
    let owned = OwnedAttempt {
        root: attempt.clone(),
        output_roots: [build.clone(), dist.clone()],
    };
    let result = compile(&inner, &owned)
        .and_then(|artifacts| install(&attempt, &dist, &stem, &owner.dist, artifacts));
    match result {
        Ok(artifacts) => {
            owner.dist = Some(files::snapshot(&dist, false)?);
            owner.build = files::snapshot(&build, true)?;
            save_owner(&build, &owner)
                .context("dist已安装但owner提交失败；本次不返回成功，请保留现场人工核验")?;
            Ok(artifacts)
        }
        Err(error) => {
            // 低层保留原失败清理合同；外层保存完整错误与SDK双流诊断。
            let mut diagnostic = std::fs::OpenOptions::new()
                .create_new(true)
                .write(true)
                .open(attempt.join("build-error.log"))?;
            diagnostic.write_all(format!("{error:#}\n").as_bytes())?;
            diagnostic.sync_all()?;
            // 正常失败保留本轮诊断；仅旧dist仍完全相同才登记attempt，不掩盖回滚失败。
            if let Err(recovery) = verify_dist(&dist, &owner.dist) {
                return Err(error.context(format!("旧dist恢复未确认，保留build现场：{recovery:#}")));
            }
            owner.build =
                files::snapshot(&build, true).context("失败现场无法安全归档，保留目录")?;
            save_owner(&build, &owner).context("失败现场owner无法提交，禁止自动重试接管")?;
            Err(error)
        }
    }
}
fn verify_dist(dist: &Path, expected: &Option<Vec<files::Node>>) -> Result<()> {
    match expected {
        Some(nodes) => ensure!(
            files::snapshot(dist, false)? == *nodes,
            "已有dist内容/权限被修改，拒绝替换"
        ),
        None => ensure!(!files::exists(dist)?, "发现未拥有dist，拒绝替换"),
    }
    Ok(())
}
fn unique_attempt(build: &Path) -> Result<PathBuf> {
    let time = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)?
        .as_nanos();
    for _ in 0..64 {
        let path = build.join(format!(
            "attempt-{}-{time}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        match std::fs::create_dir(&path) {
            Ok(()) => return Ok(path),
            Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => (),
            Err(e) => return Err(e.into()),
        }
    }
    anyhow::bail!("本轮attempt无法独占创建")
}
fn save_owner(build: &Path, owner: &Owner) -> Result<()> {
    let mut bytes = serde_json::to_vec_pretty(owner)?;
    bytes.push(b'\n');
    ensure!(
        bytes.len() as u64 <= files::JSON_LIMIT,
        "owner超过32MiB，保留现场并使用新输出ROOT"
    );
    let temp = build.join(".ryupy-build-owner.tmp");
    let mut file = std::fs::OpenOptions::new()
        .create_new(true)
        .write(true)
        .open(&temp)?;
    file.write_all(&bytes)?;
    file.sync_all()?;
    drop(file);
    std::fs::rename(temp, build.join(files::OWNER))?;
    Ok(())
}
fn install(
    attempt: &Path,
    dist: &Path,
    stem: &str,
    old: &Option<Vec<files::Node>>,
    mut artifacts: Artifacts,
) -> Result<Artifacts> {
    let native = artifacts
        .native_exe
        .as_ref()
        .context("本轮没有native产物")?;
    let manifest = artifacts.manifest.as_ref().context("本轮没有manifest")?;
    let source = native.parent().context("native没有父目录")?;
    ensure!(
        source.starts_with(attempt) && artifacts.dll.starts_with(attempt),
        "本轮产物不在owned attempt"
    );
    ensure!(
        manifest == &source.join(manifest_files::MANIFEST_NAME),
        "manifest不属于本轮publish"
    );
    // 逐级检查owned边界内路径，不让产物中的链接绕到外部。
    for ancestor in source.ancestors() {
        ensure!(
            manifest_files::plain_metadata(ancestor)?.is_dir(),
            "publish祖先不是普通目录"
        );
        if ancestor == attempt {
            break;
        }
    }
    let native_name = native
        .file_name()
        .and_then(|s| s.to_str())
        .context("native名不是UTF8")?;
    let name = if cfg!(windows) {
        format!("{stem}.exe")
    } else {
        stem.to_owned()
    };
    let candidate = attempt.join("dist-candidate");
    files::verify_manifest(source)?;
    files::copy_tree(source, &candidate)?;
    artifacts.debug_symbols = symbols::separate(attempt, source, &candidate, native_name)?;
    files::relocate(&candidate, native_name, &name)?;
    let ready = files::snapshot(&candidate, false)?;
    verify_dist(dist, old)?;
    let backup = attempt.join("previous-dist");
    swap(&candidate, dist, &backup, old.is_some())?;
    let verified = (|| -> Result<()> {
        ensure!(
            files::snapshot(dist, false)? == ready,
            "rename后dist内容/权限变化"
        );
        files::verify_manifest(dist)?;
        Ok(())
    })();
    if let Err(error) = verified {
        std::fs::rename(dist, &candidate).context("安装后核验失败且无法保留候选，保留backup")?;
        if old.is_some() {
            std::fs::rename(&backup, dist).context("安装后核验失败且旧dist恢复失败，保留backup")?;
        }
        return Err(error);
    }
    artifacts.native_exe = Some(dist.join(name));
    artifacts.manifest = Some(dist.join(manifest_files::MANIFEST_NAME));
    Ok(artifacts)
}
// 小私有提交原语允许纯测试用不存在的candidate触发真实rename错误，无生产故障开关。
fn swap(candidate: &Path, dist: &Path, backup: &Path, had_old: bool) -> Result<()> {
    if had_old {
        std::fs::rename(dist, backup).context("无法暂存旧dist；旧产物未改")?;
    }
    if let Err(error) = std::fs::rename(candidate, dist) {
        if had_old {
            std::fs::rename(backup, dist).context("候选安装失败且旧dist恢复失败，保留backup")?;
        }
        return Err(error).context("无法安装本次dist");
    }
    Ok(())
}
#[cfg(test)]
#[path = "aot_layout_tests.rs"]
mod tests;

#[cfg(all(test, target_os = "linux"))]
#[path = "aot_layout_symbols_tests.rs"]
mod symbols_tests;
