//! 函数导入绑定与静态依赖图分开：不能把局部别名导出，也不能提前执行导入。
use crate::*;
fn lower(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("imports.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    lower_module(&parsed, &ModuleInfo::default(), source)
}
#[test]
fn local_global_and_nonlocal_import_targets_preserve_binding() {
    let (module, diagnostics) = lower(
        "def outer():\n    alias = None\n    def inner():\n        nonlocal alias\n        from math import sqrt as alias\n    inner()\n    return alias\ndef local():\n    import math as local_alias\n    return local_alias\ndef global_import():\n    global shared_alias\n    import math as shared_alias\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        !module
            .vars
            .iter()
            .any(|(_, var)| var.name.as_str() == "local_alias")
    );
    let mut local = false;
    let mut global = false;
    let mut free = false;
    for (_, function) in module.functions.iter() {
        for (_, statement) in function.body.stmts.iter() {
            match &statement.kind {
                StmtKind::Import {
                    bind: Binding::Local(_),
                    ..
                } => local = true,
                StmtKind::Import {
                    bind: Binding::ModuleVar(_),
                    ..
                } => global = true,
                StmtKind::ImportFrom { names, .. } => {
                    free |= names
                        .iter()
                        .any(|(_, bind)| matches!(bind, Binding::Free(_)))
                }
                _ => {}
            }
        }
    }
    assert!(local && global && free);
}
#[test]
fn discovery_descends_functions_but_module_binding_scan_does_not() {
    let source = "import root\ndef load():\n    from .part import child as alias\n    def nested():\n        import late\nclass C:\n    def method(self):\n        import other\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("imports.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let module = imports::collect_imports(&parsed.module.body, "pkg");
    let all = imports::collect_all_imports(&parsed.module.body, "pkg");
    assert_eq!(module.len(), 1);
    assert_eq!(
        all.iter()
            .map(|request| request.module.as_str())
            .collect::<Vec<_>>(),
        ["root", "pkg.part", "late", "other"]
    );
}
#[test]
fn function_exception_alias_and_star_remain_explicitly_rejected() {
    for source in [
        "def f():\n    from io import UnsupportedOperation as Error\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f():\n    from io import UnsupportedOperation as ValueError\n    try:\n        pass\n    except ValueError:\n        pass\n",
        "def f():\n    from math import *\n",
    ] {
        let (_, diagnostics) = lower(source);
        assert!(
            diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100")),
            "{diagnostics:?}"
        );
    }
}
