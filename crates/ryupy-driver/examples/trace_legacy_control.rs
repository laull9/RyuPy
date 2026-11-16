//! 有界验收用的明确Legacy API对照。没有Auto/协议开关，不是发行CLI，也不称历史旧CLI。
//! 普通CLI一侧必须另行真实无flag执行；此程序只使用公开裸Session构建相同源。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;

fn main() -> anyhow::Result<()> {
    let arguments: Vec<_> = std::env::args_os().skip(1).collect();
    anyhow::ensure!(arguments.len() == 2, "需要输入源码与独立输出目录");
    let mut session = Session {
        diagnostic_stages: true,
        ..Default::default()
    };
    let result = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: PathBuf::from(&arguments[0]),
            out_dir: PathBuf::from(&arguments[1]),
            aot: false,
            search_paths: vec![],
        },
    );
    if !session.diagnostics.is_empty() {
        eprint!("{}", session.render_diagnostics(false));
    }
    result?;
    Ok(())
}
