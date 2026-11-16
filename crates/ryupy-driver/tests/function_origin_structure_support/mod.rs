//! 结构门的通用证据与读取辅助：保存真实源码/MIR/CIL，失败保留目录。
//! 只辅助唯一的结构测试，不单独注册测试或引入额外执行窗口。
use ryupy_mir::{
    Callee, FnId, Function, LocalId, ModuleExport, Operand, Program, RuntimeFn, Rvalue, Statement,
};
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);
const SOURCE: &str = include_str!("../../../../tests/fixtures/function_origin/structure/source.py");
pub(super) struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
pub(super) fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-small-function-origin-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("small function origin evidence: {}", path.display());
    Root(path)
}
pub(super) fn compile(root: &Root) -> (Program, Vec<ModuleExport>) {
    let filename = "function_origin_structure.py";
    std::fs::write(root.0.join(filename), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(filename, SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    std::fs::write(
        root.0.join("syntax.diagnostics"),
        format!("{diagnostics:?}"),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        name: "__main__".into(),
        file: filename.into(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, SOURCE);
    std::fs::write(root.0.join("hir.diagnostics"), format!("{diagnostics:?}")).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    std::fs::write(
        root.0.join("types.diagnostics"),
        format!("{diagnostics:?}\n{:?}", types.modes),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    // 以收敛后的实际分类为准；有注解或range语法本身都不是Static证据。
    for name in [
        "numeric_add",
        "numeric_loop",
        "numeric_direct",
        "numeric_recursive",
        "static_output",
    ] {
        let (id, _) = unique(
            hir.functions
                .iter()
                .filter(|(_, function)| function.name.as_str() == name),
            name,
        );
        assert_eq!(types.mode(id), ryupy_ty::FuncMode::Static, "{name}");
    }
    for name in [
        "dynamic_add",
        "target",
        "argument",
        "direct_with_argument",
        "through_alias",
        "factory",
        "nested",
        "stream",
    ] {
        let (id, _) = unique(
            hir.functions
                .iter()
                .filter(|(_, function)| function.name.as_str() == name),
            name,
        );
        assert_eq!(types.mode(id), ryupy_ty::FuncMode::Dynamic, "{name}");
    }
    let (mut program, exports, diagnostics) = ryupy_mir::build_program(&hir, &types);
    std::fs::write(root.0.join("mir.diagnostics"), format!("{diagnostics:?}")).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let result =
        ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline());
    std::fs::write(root.0.join("pipeline.result"), format!("{result:?}")).unwrap();
    std::fs::write(root.0.join("source.mir"), ryupy_mir::pretty::dump(&program)).unwrap();
    result.unwrap();
    (program, exports)
}
pub(super) fn unique<T>(mut values: impl Iterator<Item = T>, label: &str) -> T {
    let value = values.next().unwrap_or_else(|| panic!("结构缺失: {label}"));
    assert!(values.next().is_none(), "结构不唯一: {label}");
    value
}
pub(super) fn function<'a>(program: &'a Program, name: &str) -> (FnId, &'a Function) {
    unique(
        program
            .functions
            .iter()
            .filter(|(_, function)| function.name == name && function.signature_global.is_some()),
        name,
    )
}
pub(super) fn rvalues(function: &Function) -> impl Iterator<Item = &Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|statement| match statement {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
}
pub(super) fn local(value: &Operand) -> LocalId {
    let Operand::Local(id) = value else {
        panic!("要求冻结局部而非重读global: {value:?}")
    };
    *id
}
pub(super) fn trace_calls(function: &Function, expected: RuntimeFn) -> Vec<&[Operand]> {
    rvalues(function)
        .filter_map(|value| match value {
            Rvalue::Call {
                callee: Callee::Runtime(actual),
                args,
            } if *actual == expected => Some(args.as_slice()),
            _ => None,
        })
        .collect()
}
pub(super) fn cil(
    root: &Root,
    program: Program,
    exports: Vec<ModuleExport>,
) -> (ryupy_mir::LinkedProgram, String) {
    let linked = ryupy_mir::link(
        vec![(
            program,
            ryupy_mir::UnitMeta {
                instance_capabilities: Default::default(),
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
                location: ryupy_mir::ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: true,
                is_package: false,
                protected_exports: vec![],
                name: "__main__".into(),
                package: String::new(),
                file: "function_origin_structure.py".into(),
                exports,
            },
        )],
        0,
    );
    let result = ryupy_codegen_cil::dump_cil(
        &linked,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "small_function_origin_structure".into(),
            entry_source_path: None,
        },
    );
    if let Err(error) = &result {
        std::fs::write(root.0.join("cil.error"), format!("{error:?}")).unwrap();
    }
    let output = result.unwrap();
    std::fs::write(root.0.join("source.cil"), &output).unwrap();
    (linked, output)
}
pub(super) fn cil_method<'a>(cil: &'a str, name: &str) -> &'a str {
    unique(
        cil.split("    .method ").skip(1).filter(|method| {
            method
                .lines()
                .next()
                .unwrap()
                .contains(&format!(" {name}("))
        }),
        name,
    )
}
pub(super) fn instructions(method: &str) -> Vec<&str> {
    method
        .lines()
        .filter_map(|line| {
            line.trim()
                .strip_prefix("IL_")
                .and_then(|line| line.split_once(": ").map(|(_, instruction)| instruction))
        })
        .collect()
}
pub(super) fn assigned_calls(
    stmts: &[Statement],
    callee: Callee,
) -> impl Iterator<Item = (usize, LocalId, &[Operand])> {
    stmts
        .iter()
        .enumerate()
        .filter_map(move |(index, statement)| match statement {
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: actual,
                        args,
                    },
            } if *actual == callee => Some((index, *dest, args.as_slice())),
            _ => None,
        })
}
