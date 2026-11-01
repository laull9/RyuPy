//! 快照仅归一化真实入口路径；实际程序集与进程测试仍保留原始 argv[0]。
use std::path::Path;

pub fn dump(
    session: &mut ryupy_driver::Session,
    input: &Path,
    stage: ryupy_driver::Stage,
) -> anyhow::Result<String> {
    let output = ryupy_driver::dump(session, input, stage)?;
    if matches!(stage, ryupy_driver::Stage::Cil) {
        let source = input.to_string_lossy();
        let display = input
            .file_name()
            .map(|name| name.to_string_lossy())
            .unwrap_or_else(|| source.clone());
        // CIL使用Debug转义字符串；完整引号路径替换不会误改文件名片段或诊断行列。
        Ok(output.replace(&format!("{source:?}"), &format!("{display:?}")))
    } else {
        Ok(output)
    }
}
