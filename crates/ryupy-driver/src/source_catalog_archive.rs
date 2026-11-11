//! 只读编译器自带的唯一固定USTAR子集；没有用户archive入口，不提供通用解包器。
//! 全包digest先验；再验证固定白名单、头校验、边界/padding与每成员manifest身份。
use anyhow::{Context, Result, ensure};
use std::collections::BTreeMap;
const ARCHIVE: &[u8] = include_bytes!("../catalog/cpython-sources.tar");
const DIGEST: &str = "7a6f3d5a28f5bcc7843e81ecb2433867408d61441c53041e1ecf299b1bb9b34b";
const NAMES: &[&str] = &[
    "_markupbase.py",
    "html/__init__.py",
    "html/entities.py",
    "html/parser.py",
    "LICENSE.txt",
    "manifest.json",
];

pub(super) fn member(name: &str) -> Result<&'static [u8]> {
    ensure!(
        crate::manifest_files::hash_bytes(ARCHIVE) == DIGEST,
        "catalog发行资源包完整性损坏"
    );
    let members = parse(ARCHIVE)?;
    members.get(name).copied().context("catalog缺少固定成员")
}
fn number(field: &[u8]) -> Result<usize> {
    let text = std::str::from_utf8(field)?.trim_end_matches(['\0', ' ']);
    ensure!(
        !text.is_empty() && text.bytes().all(|byte| (b'0'..=b'7').contains(&byte)),
        "catalog数字头不是固定octal"
    );
    Ok(usize::from_str_radix(text, 8)?)
}
fn parse(bytes: &[u8]) -> Result<BTreeMap<String, &[u8]>> {
    ensure!(
        bytes.len() <= 256 * 1024 && bytes.len() % 10240 == 0,
        "catalog包尺寸或尾块不合法"
    );
    let mut offset = 0usize;
    let mut members = BTreeMap::new();
    for expected in NAMES {
        let header = bytes.get(offset..offset + 512).context("catalog头截断")?;
        let length = header[..100]
            .iter()
            .position(|byte| *byte == 0)
            .context("catalog文件名无终止符")?;
        let name = std::str::from_utf8(&header[..length])?;
        ensure!(
            name == *expected && header[length..100].iter().all(|byte| *byte == 0),
            "catalog固定成员次序/白名单不符"
        );
        ensure!(
            header[156] == b'0' && &header[257..265] == b"ustar\x0000",
            "catalog只支持固定普通USTAR文件"
        );
        ensure!(
            header[157..257].iter().all(|byte| *byte == 0)
                && header[265..512].iter().all(|byte| *byte == 0),
            "catalog拒绝链接/扩展头/目录前缀"
        );
        ensure!(
            number(&header[100..108])? == 0o644
                && number(&header[108..116])? == 0
                && number(&header[116..124])? == 0
                && number(&header[136..148])? == 0,
            "catalog固定metadata不符"
        );
        let checksum: usize = header
            .iter()
            .enumerate()
            .map(|(index, byte)| {
                if (148..156).contains(&index) {
                    32
                } else {
                    usize::from(*byte)
                }
            })
            .sum();
        ensure!(
            number(&header[148..156])? == checksum,
            "catalog头checksum不符"
        );
        let size = number(&header[124..136])?;
        ensure!(size <= 128 * 1024, "catalog成员尺寸超限");
        let start = offset.checked_add(512).context("catalog偏移溢出")?;
        let end = start.checked_add(size).context("catalog长度溢出")?;
        let content = bytes.get(start..end).context("catalog成员截断")?;
        offset = end
            .checked_add((512 - size % 512) % 512)
            .context("catalog填充溢出")?;
        ensure!(
            bytes
                .get(end..offset)
                .context("catalog填充截断")?
                .iter()
                .all(|byte| *byte == 0),
            "catalog非零padding"
        );
        ensure!(
            members.insert(name.to_owned(), content).is_none(),
            "catalog重复成员"
        );
    }
    let tail = bytes.get(offset..).context("catalog尾块截断")?;
    ensure!(
        tail.len() >= 1024 && tail.iter().all(|byte| *byte == 0),
        "catalog未知成员或无终止块"
    );
    let manifest: serde_json::Value = serde_json::from_slice(members["manifest.json"])?;
    ensure!(
        manifest["schema_version"] == 1,
        "catalog manifest schema不符"
    );
    let records = manifest["members"]
        .as_object()
        .context("catalog manifest无成员")?;
    ensure!(
        records.len() == NAMES.len() - 1,
        "catalog manifest成员集合不符"
    );
    for name in &NAMES[..NAMES.len() - 1] {
        let content = members[*name];
        let record = records.get(*name).context("catalog manifest缺固定成员")?;
        ensure!(
            record["bytes"].as_u64() == Some(content.len() as u64)
                && record["sha256"].as_str()
                    == Some(crate::manifest_files::hash_bytes(content).as_str()),
            "catalog成员身份不符：{name}"
        );
    }
    Ok(members)
}
#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn default_source_archive_fixed_members_and_corruption() {
        assert_eq!(parse(ARCHIVE).unwrap().len(), 6);
        assert_eq!(
            member("html/entities.py")
                .unwrap()
                .split(|b| *b == b'\n')
                .count()
                - 1,
            2513
        );
        for index in [0, 124, 148, 156, 257, 512, ARCHIVE.len() - 1] {
            let mut broken = ARCHIVE.to_vec();
            broken[index] ^= 1;
            assert!(parse(&broken).is_err(), "mutated byte {index}");
        }
        assert!(parse(&ARCHIVE[..ARCHIVE.len() - 1]).is_err());
        let mut duplicate = ARCHIVE.to_vec();
        duplicate.extend_from_slice(ARCHIVE);
        assert!(parse(&duplicate).is_err());
    }
}
