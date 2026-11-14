//! 一组HIR/MIR结构检查，不启动CP/SDK/JIT，不新增ABI或快照批准开关。
use super::Root;
use ryupy_hir::{ClassBase, ClassItemKind, ExprKind};
use ryupy_mir::{Callee, Constant, Operand, Program, RuntimeFn, Rvalue, Statement};

fn lower(root: &Root, label: &str, source: &str) -> (ryupy_hir::Module, Program) {
    lower_as(root, label, "__main__", source, &[])
}
fn lower_as(
    root: &Root,
    label: &str,
    module_name: &str,
    source: &str,
    dynamic: &[&str],
) -> (ryupy_hir::Module, Program) {
    std::fs::write(root.path.join(format!("{label}.py")), source).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(label, source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    std::fs::write(
        root.path.join(format!("{label}.syntax")),
        format!("{diagnostics:?}\n"),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        name: module_name.into(),
        file: label.into(),
        resolve_file_at_runtime: true,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    std::fs::write(
        root.path.join(format!("{label}.hir")),
        format!("{hir:#?}\n{diagnostics:?}\n"),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    std::fs::write(
        root.path.join(format!("{label}.types")),
        format!("{}\n{diagnostics:?}\n", types.dump()),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for name in dynamic {
        let functions: Vec<_> = hir
            .functions
            .iter()
            .filter(|(_, f)| f.name.as_str() == *name)
            .collect();
        assert_eq!(functions.len(), 1, "真实源码函数必须唯一: {name}");
        assert_eq!(
            types.mode(functions[0].0),
            ryupy_ty::FuncMode::Dynamic,
            "来源必须为真实Dynamic: {name}"
        );
    }
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    std::fs::write(
        root.path.join(format!("{label}.mir")),
        format!("{}\n{diagnostics:?}\n", ryupy_mir::pretty::dump(&program)),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    (hir, program)
}
fn statements(program: &Program) -> Vec<&Statement> {
    program.functions[program.entry]
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| b.stmts.iter())
        .collect()
}
fn call(statement: &Statement, wanted: RuntimeFn) -> Option<&[Operand]> {
    let value = match statement {
        Statement::Assign { value, .. } | Statement::Eval(value) => value,
        _ => return None,
    };
    match value {
        Rvalue::Call {
            callee: Callee::Runtime(actual),
            args,
        } if *actual == wanted => Some(args),
        _ => None,
    }
}
fn positions(statements: &[&Statement], wanted: RuntimeFn) -> Vec<usize> {
    statements
        .iter()
        .enumerate()
        .filter_map(|(i, s)| call(s, wanted).map(|_| i))
        .collect()
}
pub(super) fn check() {
    let root = Root::new("structure");
    let aliases = "from typing import NamedTuple as Root\nimport typing as t\nAlias = Root\nclass From(Root):\n    x: int\nclass Attribute(t.NamedTuple):\n    x: int\nclass Assignment(Alias):\n    x: int\n";
    let (hir, program) = lower(&root, "aliases", aliases);
    assert_eq!(hir.classes.len(), 3);
    for (_, class) in hir.classes.iter() {
        assert!(matches!(class.base, Some(ClassBase::Runtime(_))));
        assert!(class.decorated_binding.is_some(), "必须有独立公开槽");
    }
    let stmts = statements(&program);
    assert_eq!(positions(&stmts, RuntimeFn::ValidateOrdinaryBase).len(), 3);
    assert_eq!(
        positions(&stmts, RuntimeFn::PrepareRuntimeDerivedClass).len(),
        3
    );
    assert_eq!(
        positions(&stmts, RuntimeFn::InstanceClassCellGuard).len(),
        3
    );
    assert!(positions(&stmts, RuntimeFn::MakeClass).is_empty());
    assert!(positions(&stmts, RuntimeFn::PrepareOrdinaryMiClass).is_empty());
    assert_eq!(positions(&stmts, RuntimeFn::FinalizeOrdinaryClass).len(), 3);

    let atomic = "from typing import NamedTuple as Root\ndef decorate(cls):\n    return cls\nExisting = 'old'\n@decorate\nclass Existing(Root):\n    x: str = Existing\n    seen = Existing\n    def read(self, saved=Existing):\n        return saved\n";
    let (hir, program) = lower(&root, "publication", atomic);
    let (class_id, class) = hir.classes.iter().next().unwrap();
    let binding = class.decorated_binding.expect("公开槽丢失");
    assert_eq!(hir.vars[binding].name.as_str(), "Existing");
    for item in &class.body {
        if let ClassItemKind::Attr { name, value } = &item.kind {
            if name.as_str() == "x" || name.as_str() == "seen" {
                assert!(
                    matches!(hir.functions[hir.main].body.exprs[*value].kind, ExprKind::ModuleVar(id) if id == binding)
                );
            }
        }
    }
    assert!(hir.default_exprs.iter().any(|id| matches!(hir.functions[hir.main].body.exprs[*id].kind, ExprKind::ModuleVar(v) if v == binding)));
    let public_name = format!("__mv{}_Existing", binding.into_raw().into_u32());
    let private_name = format!("__class_{}", class_id.into_raw().into_u32());
    let public = program
        .globals
        .iter()
        .find(|(_, g)| g.name == public_name)
        .unwrap()
        .0;
    let private = program
        .globals
        .iter()
        .find(|(_, g)| g.name == private_name)
        .unwrap()
        .0;
    assert_ne!(public, private);
    let stmts = statements(&program);
    let make = positions(&stmts, RuntimeFn::PrepareRuntimeDerivedClass);
    let guard = positions(&stmts, RuntimeFn::InstanceClassCellGuard);
    assert_eq!(guard.len(), 1);
    assert!(positions(&stmts, RuntimeFn::MakeClass).is_empty());
    assert!(positions(&stmts, RuntimeFn::PrepareOrdinaryMiClass).is_empty());
    let finish = positions(&stmts, RuntimeFn::FinalizeOrdinaryClass);
    let declare = positions(&stmts, RuntimeFn::DeclareDataclass);
    assert_eq!(make.len(), 1);
    assert_eq!(finish.len(), 1);
    assert_eq!(declare.len(), 1);
    let writes: Vec<_> = stmts
        .iter()
        .enumerate()
        .filter_map(|(i, s)| match s {
            Statement::SetGlobal { dest, value }
                if *dest == public && !matches!(value, Operand::Const(Constant::Null)) =>
            {
                Some(i)
            }
            _ => None,
        })
        .collect();
    assert_eq!(writes.len(), 2, "只允许旧值写入与完成后的公开写入");
    assert!(
        writes[0] < make[0]
            && make[0] < finish[0]
            && finish[0] < declare[0]
            && declare[0] < writes[1]
    );
    let decorator_calls: Vec<_> = positions(&stmts, RuntimeFn::CallValue)
        .into_iter()
        .filter(|i| *i > declare[0] && *i < writes[1])
        .collect();
    assert_eq!(decorator_calls.len(), 1, "装饰发生在finish之后publish之前");
    let private_writes: Vec<_> = stmts
        .iter()
        .enumerate()
        .filter_map(|(index, statement)| {
            matches!(statement, Statement::SetGlobal { dest, .. } if *dest == private)
                .then_some(index)
        })
        .collect();
    assert_eq!(private_writes.len(), 1);
    assert!(
        finish[0] < guard[0]
            && guard[0] < private_writes[0]
            && private_writes[0] < declare[0]
            && declare[0] < decorator_calls[0]
            && decorator_calls[0] < writes[1]
    );
    let validation = positions(&stmts, RuntimeFn::ValidateOrdinaryBase);
    assert_eq!(validation.len(), 1);
    assert!(validation[0] < make[0]);

    let ordinary = "from typing import NamedTuple as Root\nclass Plain:\n    pass\nRoot = Plain\nclass Ordinary(Root):\n    if True:\n        marker = 1\n    def __init__(self, value):\n        self.value = value\nif False:\n    from typing import NamedTuple as Never\nclass Conditional(Never):\n    pass\n";
    let (hir, _) = lower(&root, "ordinary_shadow", ordinary);
    for (_, class) in hir
        .classes
        .iter()
        .filter(|(_, c)| c.name.as_str() != "Plain")
    {
        assert!(matches!(class.base, Some(ClassBase::Runtime(_))));
    }
    assert!(
        hir.functions
            .iter()
            .any(|(_, f)| f.name.as_str() == "__init__")
    );

    let docs = "from typing import NamedTuple as Root\nclass Documented(Root):\n    'true-doc'\n    x: int\n    'not-doc'\n    if True:\n        'branch-not-doc'\nclass Undocumented(Root):\n    x: int\n    if True:\n        'branch-not-doc'\n";
    let (_, program) = lower(&root, "docstrings", docs);
    let stmts = statements(&program);
    let doc_writes: Vec<_> = stmts.iter().filter_map(|s| call(s, RuntimeFn::SetAttr)).filter(|args| {
        matches!(args.get(1), Some(Operand::Const(Constant::Str(name))) if name == "__doc__")
    }).collect();
    assert_eq!(doc_writes.len(), 1, "仅真正类首句docstring写__doc__");
    provider_origin(&root);
}

fn provider_origin(root: &Root) {
    // 读取同一实际provider源码，不用无注解或函数名字猜Dynamic来源。
    const PROVIDER: &str =
        include_str!("../../../../tests/fixtures/typing_namedtuple/modules/provider.py");
    let (_, mut program) = lower_as(root, "provider", "provider", PROVIDER, &["make"]);
    let result =
        ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline());
    std::fs::write(root.path.join("provider.pipeline"), format!("{result:?}\n")).unwrap();
    std::fs::write(
        root.path.join("provider.optimized.mir"),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    result.unwrap();
    let functions: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, f)| f.name == "make" && f.signature_global.is_some())
        .collect();
    assert_eq!(functions.len(), 1);
    let function = functions[0].1;
    let owner = function.dynamic_origin.expect("Dynamic函数来源丢失").owner;
    assert_eq!(function.params.last(), Some(&owner));
    assert_eq!(function.body.locals[owner].ty, ryupy_mir::MirType::Dynamic);
    let stmts: Vec<_> = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| b.stmts.iter())
        .collect();
    assert!(
        positions(&stmts, RuntimeFn::TracePush).is_empty(),
        "不得静态来源回退"
    );
    let pushes: Vec<_> = stmts
        .iter()
        .filter_map(|s| call(s, RuntimeFn::TracePushWithOrigin))
        .collect();
    assert_eq!(pushes.len(), 1);
    assert_eq!(pushes[0].len(), 3);
    assert!(matches!(pushes[0].get(2), Some(Operand::Local(id)) if *id == owner));
}
