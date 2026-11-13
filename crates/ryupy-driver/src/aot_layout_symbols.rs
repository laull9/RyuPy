//! Linux 交付符号分离：只排除与本次 ELF 身份匹配的顶层 host.dbg 副本。
//! SDK publish 与原清单完整保留在 owned attempt，未知旁件、资源及 native 字节不变。
//! CRC 证明 debuglink 关联，非空 build-id 再核身份；不把扩展名当成符号证明。
use anyhow::Result;
use std::path::{Path, PathBuf};

pub(super) fn separate(
    attempt: &Path,
    source: &Path,
    candidate: &Path,
    native_name: &str,
) -> Result<Option<PathBuf>> {
    #[cfg(target_os = "linux")]
    {
        linux::separate(attempt, source, candidate, native_name).map(Some)
    }
    #[cfg(not(target_os = "linux"))]
    {
        let _ = (attempt, source, candidate, native_name);
        Ok(None)
    }
}

#[cfg(target_os = "linux")]
mod linux {
    use super::*;
    use crate::manifest_files;
    use anyhow::{Context, ensure};
    use object::{Object, ObjectSection};
    use serde_json::json;
    use std::fs::{File, Metadata, OpenOptions};
    use std::io::{Read, Write};
    use std::os::unix::fs::MetadataExt;

    const LIMIT: u64 = 512 * 1024 * 1024;

    struct Identity {
        crc: u32,
        build_id: String,
    }

    /// 读同一活 handle，前后同时核路径身份、长度、mtime，再独立哈希核读取内容。
    /// 超限由上层归为保守保留；这里的 IO、链接、并发修改一律失败。
    fn read_stable(path: &Path, logical: &str) -> Result<Vec<u8>> {
        let before = manifest_files::plain_metadata(path)?;
        ensure!(
            before.is_file() && before.len() <= LIMIT,
            "符号身份读取超限或不是文件"
        );
        let mut file = File::open(path)?;
        let opened = file.metadata()?;
        ensure!(same(&before, &opened)?, "符号文件在打开时变化");
        let mut bytes = Vec::new();
        Read::by_ref(&mut file)
            .take(LIMIT + 1)
            .read_to_end(&mut bytes)?;
        let after = manifest_files::plain_metadata(path)?;
        ensure!(
            bytes.len() as u64 == before.len()
                && same(&before, &after)?
                && same(&opened, &file.metadata()?)?,
            "符号文件在读取时变化"
        );
        let digest = manifest_files::digest_file(path, logical, "symbol_identity_input")?;
        ensure!(
            digest.sha256 == manifest_files::hash_bytes(&bytes)
                && same(&before, &manifest_files::plain_metadata(path)?)?,
            "符号文件在复核哈希时变化"
        );
        Ok(bytes)
    }

    fn same(a: &Metadata, b: &Metadata) -> Result<bool> {
        Ok(a.is_file()
            && b.is_file()
            && a.dev() == b.dev()
            && a.ino() == b.ino()
            && a.len() == b.len()
            && a.modified()? == b.modified()?
            && a.ctime() == b.ctime()
            && a.ctime_nsec() == b.ctime_nsec())
    }

    fn hex(bytes: &[u8]) -> String {
        bytes.iter().map(|b| format!("{b:02x}")).collect()
    }

    /// 解析不认识、字段缺失或身份不一致仅表示不能安全分离，不拒绝原有完整发布。
    fn identify(
        native: &[u8],
        debug: &[u8],
        expected: &str,
    ) -> std::result::Result<Identity, &'static str> {
        let image = object::File::parse(native).map_err(|_| "native_parse_unrecognized")?;
        let symbols = object::File::parse(debug).map_err(|_| "symbols_parse_unrecognized")?;
        for object in [&image, &symbols] {
            if object.format() != object::BinaryFormat::Elf
                || !object.is_64()
                || object.architecture() != object::Architecture::X86_64
                || !object.is_little_endian()
            {
                return Err("not_linux_elf64_x86_64");
            }
        }
        let (name, crc) = image
            .gnu_debuglink()
            .map_err(|_| "debuglink_unrecognized")?
            .ok_or("debuglink_absent")?;
        if name != expected.as_bytes() {
            return Err("debuglink_basename_mismatch");
        }
        if crc32fast::hash(debug) != crc {
            return Err("debuglink_crc_mismatch");
        }
        let a = image
            .build_id()
            .map_err(|_| "native_build_id_unrecognized")?
            .filter(|id| !id.is_empty())
            .ok_or("native_build_id_absent")?;
        let b = symbols
            .build_id()
            .map_err(|_| "symbols_build_id_unrecognized")?
            .filter(|id| !id.is_empty())
            .ok_or("symbols_build_id_absent")?;
        if a != b {
            return Err("build_id_mismatch");
        }
        // 本竖切只识别真正有内容的标准 DWARF debug_info；压缩/其它格式保留即可。
        let section = symbols
            .section_by_name(".debug_info")
            .ok_or("debug_info_absent")?;
        if section.size() == 0
            || section
                .data()
                .map_err(|_| "debug_info_unrecognized")?
                .is_empty()
        {
            return Err("debug_info_empty");
        }
        Ok(Identity {
            crc,
            build_id: hex(a),
        })
    }

    fn relative(attempt: &Path, path: &Path) -> Result<String> {
        let path = path
            .strip_prefix(attempt)
            .context("符号保留路径不属于本轮attempt")?;
        let name = path.to_str().context("符号保留路径不是UTF8")?;
        manifest_files::safe_relative(name)?;
        Ok(name.to_owned())
    }

    fn check_ancestors(attempt: &Path, path: &Path) -> Result<()> {
        relative(attempt, path)?;
        for ancestor in path.ancestors() {
            ensure!(
                manifest_files::plain_metadata(ancestor)?.is_dir(),
                "符号树祖先不是普通目录"
            );
            if ancestor == attempt {
                return Ok(());
            }
        }
        anyhow::bail!("符号树不属于本轮attempt")
    }

    pub(super) fn separate(
        attempt: &Path,
        source: &Path,
        candidate: &Path,
        native_name: &str,
    ) -> Result<PathBuf> {
        use super::super::files;
        manifest_files::safe_relative(native_name)?;
        ensure!(!native_name.contains('/'), "native必须是顶层文件名");
        ensure!(source != candidate, "符号分离不能修改原publish");
        check_ancestors(attempt, source)?;
        check_ancestors(attempt, candidate)?;
        let source_before = files::snapshot(source, false)?;
        let source_manifest = files::verify_manifest(source)?;
        let mut manifest = files::verify_manifest(candidate)?;
        ensure!(source_manifest == manifest, "符号分离前清单不一致");
        ensure!(
            files::snapshot(candidate, false)? == source_before,
            "符号分离前副本不一致"
        );
        let native_entry = manifest["artifacts"]
            .as_array()
            .context("清单缺artifacts")?
            .iter()
            .find(|e| e["role"] == "native_executable")
            .context("清单缺native")?;
        ensure!(
            native_entry["relative_path"] == native_name,
            "native名称与清单不一致"
        );
        let native_sha = native_entry["sha256"].clone();
        let native = source.join(native_name);
        let debug_name = format!("{native_name}.dbg");
        let debug = source.join(&debug_name);
        let mut record = json!({
            "schema_version": 1, "platform": "linux-x64", "status": "retained",
            "reason": "symbols_absent", "native_relative_path": relative(attempt, &native)?,
            "symbols_relative_path": relative(attempt, &debug)?,
            "native_sha256": native_sha, "symbols_sha256": null,
            "gnu_debuglink_crc32": null, "build_id": null,
            "scope": "仅Linux ELF64 x86-64本次host符号；SDK publish完整保留，不代表调试器已验证"
        });
        let native_meta = manifest_files::plain_metadata(&native)?;
        ensure!(native_meta.is_file(), "native不是普通文件");
        // 仅NotFound表示缺失，权限/链接/其它异常不可静默降为未识别。
        let debug_meta = match std::fs::symlink_metadata(&debug) {
            Ok(_) => Some(manifest_files::plain_metadata(&debug)?),
            Err(e) if e.kind() == std::io::ErrorKind::NotFound => None,
            Err(e) => return Err(e.into()),
        };
        if let Some(meta) = debug_meta {
            if !meta.is_file() {
                record["reason"] = "symbols_not_file".into();
            } else if native_meta.len() > LIMIT || meta.len() > LIMIT {
                record["reason"] = "identity_read_limit_exceeded".into();
            } else {
                let native_bytes = read_stable(&native, native_name)?;
                let debug_bytes = read_stable(&debug, &debug_name)?;
                record["native_sha256"] = manifest_files::hash_bytes(&native_bytes).into();
                record["symbols_sha256"] = manifest_files::hash_bytes(&debug_bytes).into();
                match identify(&native_bytes, &debug_bytes, &debug_name) {
                    Err(reason) => record["reason"] = reason.into(),
                    Ok(identity) => {
                        // 删副本前再次核完整源及副本；只允许删精确已验证的未分类旁件。
                        ensure!(
                            files::snapshot(source, false)? == source_before,
                            "符号核验期间原publish变化"
                        );
                        ensure!(
                            files::snapshot(candidate, false)? == source_before,
                            "符号核验期间副本变化"
                        );
                        let entries = manifest["artifacts"]
                            .as_array_mut()
                            .context("清单缺artifacts")?;
                        let index = entries
                            .iter()
                            .position(|e| e["relative_path"] == debug_name)
                            .context("清单缺已验证符号")?;
                        ensure!(
                            entries[index]["role"] == "unclassified_sidecar",
                            "不能分离资源或其它已分类产物"
                        );
                        ensure!(
                            entries[index]["sha256"] == record["symbols_sha256"],
                            "符号摘要不等于清单"
                        );
                        entries.remove(index);
                        std::fs::remove_file(candidate.join(&debug_name))?;
                        let temp = candidate.join(".ryupy-symbol-manifest.tmp");
                        let mut output = OpenOptions::new()
                            .create_new(true)
                            .write(true)
                            .open(&temp)?;
                        serde_json::to_writer_pretty(&mut output, &manifest)?;
                        output.write_all(b"\n")?;
                        output.sync_all()?;
                        drop(output);
                        std::fs::rename(temp, candidate.join(manifest_files::MANIFEST_NAME))?;
                        files::verify_manifest(candidate)?;
                        record["status"] = "separated".into();
                        record["reason"] = "verified_debuglink_crc_and_build_id".into();
                        record["gnu_debuglink_crc32"] = identity.crc.into();
                        record["build_id"] = identity.build_id.into();
                    }
                }
            }
        }
        ensure!(
            files::snapshot(source, false)? == source_before,
            "符号分离改变原publish"
        );
        files::verify_manifest(candidate)?;
        if record["status"] == "retained" {
            ensure!(
                files::snapshot(candidate, false)? == source_before,
                "保留符号期间副本变化"
            );
        }
        let path = attempt.join("debug-symbols.json");
        let mut output = OpenOptions::new()
            .create_new(true)
            .write(true)
            .open(&path)?;
        serde_json::to_writer_pretty(&mut output, &record)?;
        output.write_all(b"\n")?;
        output.sync_all()?;
        Ok(path)
    }
}
