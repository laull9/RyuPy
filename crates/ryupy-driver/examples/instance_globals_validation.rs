//! 外部验收例程序：只用公开Session/build API，错误分类不猜诊断文本。
use anyhow::{Result, ensure};
use ryupy_driver::{BuildOptions, HasErrors, ModuleEnvironmentPolicy, Session};
use std::path::PathBuf;

fn main() -> Result<()> {
    let args: Vec<String> = std::env::args().collect();
    ensure!(
        args.len() == 9,
        "input out search jit|aot resources|- selectedCSV|- true|false pilot|explicit"
    );
    let aot = match args[4].as_str() {
        "jit" => false,
        "aot" => true,
        _ => anyhow::bail!("invalid mode"),
    };
    let cap = match args[7].as_str() {
        "true" => true,
        "false" => false,
        _ => anyhow::bail!("invalid capability"),
    };
    let policy = match args[8].as_str() {
        "pilot" => ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        "explicit" => ModuleEnvironmentPolicy::ExplicitOnly,
        _ => anyhow::bail!("invalid policy"),
    };
    let selected = if args[6] == "-" {
        Vec::new()
    } else {
        let names: Vec<String> = args[6].split(',').map(str::to_owned).collect();
        ensure!(names.iter().all(|s| !s.is_empty()), "empty selection");
        names
    };
    let resource = (args[5] != "-").then(|| PathBuf::from(&args[5]));
    // 不强制AOT有资源：让真实driver检查无锚负例；JIT+资源亦保原guard。
    let out = PathBuf::from(&args[2]);
    std::fs::create_dir(&out)?;
    let mut session = Session {
        module_environment_policy: policy,
        instance_modules: selected,
        instance_direct_globals: cap,
        diagnostic_stages: true,
        ..Default::default()
    };
    let result = ryupy_driver::build_with_resources(
        &mut session,
        &BuildOptions {
            input: PathBuf::from(&args[1]),
            out_dir: out.join("product"),
            aot,
            search_paths: vec![PathBuf::from(&args[3])],
        },
        resource.as_deref(),
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
        Ok(artifacts) => {
            std::fs::write(out.join("result-kind.txt"), "success\n")?;
            std::fs::write(
                out.join("artifact-dll.txt"),
                artifacts.dll.to_string_lossy().as_bytes(),
            )?;
            if let Some(path) = artifacts.native_exe {
                std::fs::write(
                    out.join("artifact-native.txt"),
                    path.to_string_lossy().as_bytes(),
                )?;
            }
            if let Some(path) = artifacts.manifest {
                std::fs::write(
                    out.join("artifact-manifest.txt"),
                    path.to_string_lossy().as_bytes(),
                )?;
            }
            Ok(())
        }
        Err(error) => {
            let kind = if error.is::<HasErrors>() {
                "has-errors\n"
            } else {
                "other-error\n"
            };
            std::fs::write(out.join("result-kind.txt"), kind)?;
            std::fs::write(out.join("error.txt"), format!("{error:#}\n"))?;
            Err(error)
        }
    }
}
