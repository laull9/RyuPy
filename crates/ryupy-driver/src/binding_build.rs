//! 固定声明独占部署：先验证再写，高层AOT保失败树、其余清理本轮根，不改旧包。
use crate::{
    Artifacts, BuildOptions, Session,
    native_binding::{BindingPlan, Platform},
    native_binding_manifest::BindingManifest,
};
use anyhow::{Context, Result, ensure};
use ryupy_codegen_cil::fixed_bindings::{self, BindingDeployment, FixedI32Binding};
use ryupy_mir::{
    BasicBlock, Body, ExportGuard, Function, GlobalDecl, MirType, ModuleDef, ModuleExport,
    Terminator,
};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
pub(crate) fn build(
    sess: &mut Session,
    opts: &BuildOptions,
    declaration: &Path,
) -> Result<Artifacts> {
    build_internal(sess, opts, declaration, None)
}
pub(crate) fn build_in_attempt(
    sess: &mut Session,
    opts: &BuildOptions,
    declaration: &Path,
    owned: &crate::aot_layout::OwnedAttempt,
) -> Result<Artifacts> {
    ensure!(opts.aot, "短绑定工作根只供高层AOT");
    build_internal(sess, opts, declaration, Some(owned))
}
fn build_internal(
    sess: &mut Session,
    opts: &BuildOptions,
    declaration: &Path,
    owned: Option<&crate::aot_layout::OwnedAttempt>,
) -> Result<Artifacts> {
    crate::aot::validate_before_build(&opts.input, opts.aot)?;
    let plan = BindingPlan::read(declaration, Platform::current()?, opts.aot)?;
    let previous = std::mem::replace(&mut sess.fixed_binding_build, true);
    let compiled = crate::compile_linked_locations(
        sess,
        &opts.input,
        &opts.search_paths,
        true,
        None,
        opts.aot,
    );
    sess.fixed_binding_build = previous;
    let (mut program, sources, _) = compiled?;
    plan.check_modules(&program)?;
    let source_modules = ryupy_codegen_cil::SourceModules::capture(&program)?;
    let binding = declare(&mut program, &plan);
    let name = crate::assembly_name(&opts.input);
    validate_name(&name)?;
    let emitted = fixed_bindings::emit_source_with_report(
        &program,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: name.clone(),
            entry_source_path: Some(opts.input.to_string_lossy().into_owned()),
        },
        &[binding],
        &BindingDeployment {
            relative_file: plan.platform.relative_file().into(),
        },
        &source_modules,
    )?;
    let context = BindingManifest::new(
        emitted.report,
        sources,
        emitted.bindings,
        program.entry.into_raw().into_u32(),
        &emitted.bytes,
        sess,
        &plan,
    )?;
    std::fs::create_dir_all(&opts.out_dir)?;
    let root = if let Some(attempt) = owned {
        attempt.create_child(&opts.out_dir, "ryupy-binding-0")?
    } else {
        owned_root(&opts.out_dir)?
    };
    let result = (|| -> Result<Artifacts> {
        let bundle = root.join("bundle");
        std::fs::create_dir(&bundle)?;
        let dll = bundle.join(format!("{name}.dll"));
        std::fs::write(&dll, &emitted.bytes)?;
        std::fs::write(
            bundle.join(format!("{name}.runtimeconfig.json")),
            ryupy_codegen_cil::runtimeconfig::render(),
        )?;
        crate::runtime::copy_runtime_to(&bundle)?;
        let runtime = bundle.join("RyuPy.Runtime.dll");
        if opts.aot {
            let p = crate::aot::publish_binding(&dll, &bundle, &context, &plan, owned)?;
            return Ok(Artifacts {
                dll,
                native_exe: Some(p.executable),
                manifest: Some(p.manifest),
            });
        }
        let target = bundle.join(plan.platform.relative_file());
        std::fs::create_dir_all(target.parent().context("shared缺父目录")?)?;
        let frozen = plan.input.freeze(&target)?;
        let mut inputs = crate::manifest::input_digests(&dll, &runtime)?;
        inputs.push(plan.input.digest.clone());
        let mut artifacts = crate::manifest_files::collect(&bundle, &dll)?;
        for a in &mut artifacts {
            a.role = if a.relative_path == format!("{name}.dll") {
                "managed_entry"
            } else if a.relative_path == "RyuPy.Runtime.dll" {
                "runtime_managed"
            } else if a.relative_path == format!("{name}.runtimeconfig.json") {
                "runtimeconfig"
            } else if a.relative_path == plan.platform.relative_file() {
                "native_shared"
            } else {
                anyhow::bail!("JIT绑定包出现意外文件")
            }
            .into();
        }
        frozen.verify()?;
        let bytes = context.render(&plan, None, &artifacts, &inputs)?;
        let manifest = crate::manifest_files::write_atomic(&bundle, &bytes)?;
        Ok(Artifacts {
            dll,
            native_exe: None,
            manifest: Some(manifest),
        })
    })();
    match result {
        Ok(v) => Ok(v),
        Err(error) if owned.is_some() => Err(error),
        Err(error) => match std::fs::remove_dir_all(&root) {
            Ok(()) => Err(error),
            Err(cleanup) => Err(error.context(format!("同时无法清理本轮绑定根：{cleanup}"))),
        },
    }
}
pub(crate) fn validate_name(name: &str) -> Result<()> {
    ensure!(
        !name.is_empty()
            && !name.ends_with([' ', '.'])
            && !name.contains(['/', '\\', ':', '<', '>', '"', '|', '?', '*'])
            && !name.chars().any(char::is_control),
        "绑定程序集名称不是可部署单组件"
    );
    let stem = name
        .split('.')
        .next()
        .unwrap_or("")
        .trim_end_matches(' ')
        .to_ascii_uppercase();
    let device = matches!(
        stem.as_str(),
        "CON" | "PRN" | "AUX" | "NUL" | "CONIN$" | "CONOUT$" | "CLOCK$"
    ) || stem
        .strip_prefix("COM")
        .or_else(|| stem.strip_prefix("LPT"))
        .is_some_and(|s| {
            matches!(
                s,
                "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "¹" | "²" | "³"
            )
        });
    ensure!(!device, "绑定程序集名是Windows设备名");
    Ok(())
}
fn owned_root(out: &Path) -> Result<PathBuf> {
    let time = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)?
        .as_nanos();
    for _ in 0..64 {
        let root = out.join(format!(
            "ryupy-binding-{}-{time}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        match std::fs::create_dir(&root) {
            Ok(()) => return root.canonicalize().map_err(Into::into),
            Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => continue,
            Err(e) => return Err(e.into()),
        }
    }
    anyhow::bail!("独占绑定根重试超额")
}
fn declare(p: &mut ryupy_mir::LinkedProgram, plan: &BindingPlan) -> FixedI32Binding {
    let export = p.globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: format!("{}_{}", plan.module, plan.export),
        ty: MirType::Dynamic,
    });
    let mut body = Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
        locals: Default::default(),
        blocks: Default::default(),
        entry: p.functions[p.modules[p.entry].init].body.entry,
        regions: vec![],
    };
    body.entry = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    let initializer = p.functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: plan.module.clone(),
        codegen_name: "__declared_binding_init".into(),
        source_file: String::new(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
        body,
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    });
    let module = p.modules.alloc(ModuleDef {
        instance_capabilities: Default::default(),
        execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
        environment_reason: "existing-contract".into(),
        location: Default::default(),
        file_backed_by_global: true,
        is_package: false,
        protected_exports: vec![],
        name: plan.module.clone(),
        package: String::new(),
        file: String::new(),
        init: initializer,
        exports: vec![ModuleExport {
            name: plan.export.clone(),
            value: export,
            guard: ExportGuard::ValueSentinel,
        }],
    });
    FixedI32Binding {
        module,
        initializer,
        export,
        module_ref: plan.library.clone(),
        entry_point: plan.symbol.clone(),
    }
}
#[cfg(test)]
#[path = "binding_build_tests.rs"]
mod tests;
