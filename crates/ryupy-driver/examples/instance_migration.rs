//! 独立迁移诊断入口：调用既有Session显式模块选择，不改变产品CLI或自动策略。
use ryupy_driver::{BuildOptions, ModuleEnvironmentPolicy, Session};
use std::path::PathBuf;
fn main() -> anyhow::Result<()> {
    let args: Vec<String> = std::env::args().collect();
    anyhow::ensure!(
        args.len() >= 4,
        "用法：instance_migration 输入 输出 搜索根 [实例模块…]"
    );
    let out = PathBuf::from(&args[2]);
    std::fs::create_dir_all(&out)?;
    let mut session = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        instance_modules: args[4..].to_vec(),
        diagnostic_stages: true,
        ..Default::default()
    };
    let result = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: PathBuf::from(&args[1]),
            out_dir: out.join("product"),
            search_paths: vec![PathBuf::from(&args[3])],
            aot: false,
        },
    );
    std::fs::write(
        out.join("module-environment.json"),
        session.module_environment_report_json()?,
    )?;
    std::fs::write(
        out.join("diagnostics.txt"),
        session.render_diagnostics(false),
    )?;
    match result {
        Ok(artifact) => {
            println!("{}", artifact.dll.display());
            Ok(())
        }
        Err(error) => {
            eprintln!("{}", session.render_diagnostics(false));
            Err(error)
        }
    }
}
