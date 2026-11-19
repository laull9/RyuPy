//! 一个结构/来源门：真实driver查module/package/searchroot，不执行CP或JIT。
//! 普通模块导入与动态callee按当前ImportAttr/DynGetAttr/DynCallValue名称断言。
use ryupy_mir::{Callee, Constant, Operand, RuntimeFn, Rvalue, Statement};
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        eprintln!("mappingproxy来源证据保留: {}", self.0.display());
    }
}
fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-mappingproxy-structure-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("mappingproxy结构证据目录已创建: {}", path.display());
    Root(path)
}
const SHADOW: &str = include_str!("../../../tests/fixtures/mapping_proxy/shadow/types.py");
const SOURCE: &str = "\
import types
from types import MappingProxyType as saved

def through_parameter(factory, mapping):
    return factory(mapping)

def capture_attribute(mapping):
    factory = types.MappingProxyType
    return factory(mapping)

def through_import(mapping):
    return saved(mapping)

def rebind(replacement):
    types.MappingProxyType = replacement
";

fn values(function: &ryupy_mir::Function) -> impl Iterator<Item = &Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| block.stmts.iter())
        .filter_map(|statement| match statement {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
}
#[test]
fn mapping_proxy_source_resolution_and_dynamic_value_paths() {
    let root = root();
    for mode in ["module", "package", "searchroot"] {
        let directory = root.0.join(mode);
        std::fs::create_dir(&directory).unwrap();
        let entry = directory.join("main.py");
        std::fs::write(&entry, SOURCE).unwrap();
        let (path, search_paths) = match mode {
            "module" => (directory.join("types.py"), vec![]),
            "package" => {
                let package = directory.join("types");
                std::fs::create_dir(&package).unwrap();
                (package.join("__init__.py"), vec![])
            }
            "searchroot" => {
                let search = directory.join("extra");
                std::fs::create_dir(&search).unwrap();
                (search.join("types.py"), vec![search])
            }
            _ => unreachable!(),
        };
        std::fs::write(&path, SHADOW).unwrap();
        let mut session = ryupy_driver::Session::default();
        let result = ryupy_driver::modules::load_program(&mut session, &entry, &search_paths);
        std::fs::write(
            directory.join("load.diagnostics"),
            format!("{:?}\n", session.diagnostics),
        )
        .unwrap();
        let (sources, entry_index) = result.unwrap();
        assert!(!session.diagnostics.has_errors());
        let metadata: Vec<_> = sources
            .iter()
            .map(|source| {
                format!(
                    "{} {:?} {:?}\n{}",
                    source.name, source.kind, source.resolved_source_path, source.text
                )
            })
            .collect();
        std::fs::write(directory.join("sources.txt"), metadata.join("\n")).unwrap();
        let found: Vec<_> = sources
            .iter()
            .filter(|source| source.name == "types")
            .collect();
        assert_eq!(found.len(), 1, "必须真正找到源码，而非原生注册表");
        assert_eq!(found[0].resolved_source_path, path);
        assert_eq!(found[0].text, SHADOW);
        assert_eq!(
            found[0].kind,
            if mode == "package" {
                ryupy_driver::modules::SourceKind::Package
            } else {
                ryupy_driver::modules::SourceKind::ModuleFile
            }
        );
        let source = &sources[entry_index];
        let info = ryupy_hir::ModuleInfo {
            instance_globals: false,
            name: source.name.clone(),
            package: source.package.clone(),
            file: source.file.clone(),
            ..Default::default()
        };
        let (hir, diagnostics) = ryupy_hir::lower_module(&source.parsed, &info, &source.text);
        std::fs::write(
            directory.join("hir.diagnostics"),
            format!("{diagnostics:?}\n"),
        )
        .unwrap();
        std::fs::write(directory.join("source.hir"), ryupy_hir::pretty::dump(&hir)).unwrap();
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (types, diagnostics) = ryupy_ty::check_module(&hir);
        std::fs::write(
            directory.join("types.diagnostics"),
            format!("{diagnostics:?}\n"),
        )
        .unwrap();
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (mut program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
        std::fs::write(
            directory.join("mir.diagnostics"),
            format!("{diagnostics:?}\n"),
        )
        .unwrap();
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let result =
            ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline());
        std::fs::write(directory.join("pipeline.result"), format!("{result:?}\n")).unwrap();
        std::fs::write(
            directory.join("source.mir"),
            ryupy_mir::pretty::dump(&program),
        )
        .unwrap();
        result.unwrap();
        assert!(program.functions.iter().any(|(_, function)| values(function).any(|value| matches!(value,
            Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ImportAttr), args }
            if matches!(args.as_slice(), [Operand::Const(Constant::Str(module)), Operand::Const(Constant::Str(attr))] if module == "types" && attr == "MappingProxyType")))));
        for name in ["through_parameter", "capture_attribute", "through_import"] {
            let functions: Vec<_> = program
                .functions
                .iter()
                .filter(|(_, function)| function.name == name)
                .collect();
            assert_eq!(functions.len(), 1, "{name}");
            let function = functions[0].1;
            assert_eq!(
                values(function)
                    .filter(|value| matches!(value, Rvalue::DynCallValue { .. }))
                    .count(),
                1,
                "{name}"
            );
            assert!(
                !values(function).any(|value| matches!(value, Rvalue::NewObject { .. })),
                "不能按名称静态恢复构造器"
            );
            if name == "capture_attribute" {
                assert_eq!(values(function).filter(|value| matches!(value,
                    Rvalue::Call { callee: Callee::Runtime(RuntimeFn::DynGetAttr), args }
                    if matches!(&args[1], Operand::Const(Constant::Str(attr)) if attr == "MappingProxyType"))).count(), 1);
            }
        }
    }
}
