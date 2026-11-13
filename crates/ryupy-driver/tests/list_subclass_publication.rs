//! 真实list派生沿普通类发布事务：先验证/创建/完成，再装饰，最后写公开槽。
//! 只检查HIR/MIR，不启动SDK/JIT；保留独占输入与结构原流，不批准任何快照。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_hir::{ClassBase, ClassItemKind, ExprKind};
use ryupy_mir::{Callee, Constant, Operand, RuntimeFn, Rvalue, Statement};

const SOURCE: &str = "def decorate(cls):\n    return cls\nExisting = 'old'\n@decorate\nclass Existing(list):\n    marker = Existing\n    def read(self, saved=Existing):\n        return saved\n";

#[test]
fn list_layout_finishes_before_decoration_and_publication() {
    let root = test_temp::Root::new("list-subclass-publication");
    std::fs::write(root.join("source.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("source.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        name: "__main__".into(),
        file: "source.py".into(),
        resolve_file_at_runtime: true,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, SOURCE);
    std::fs::write(root.join("hir.txt"), format!("{hir:#?}\n{diagnostics:?}\n")).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (class_id, class) = hir.classes.iter().next().unwrap();
    assert!(matches!(class.base, Some(ClassBase::Runtime(_))));
    let binding = class.decorated_binding.expect("公开槽必须独立于隐藏类身份");
    assert_eq!(hir.vars[binding].name.as_str(), "Existing");
    for item in &class.body {
        if let ClassItemKind::Attr { name, value } = &item.kind {
            if name.as_str() == "marker" {
                assert!(matches!(hir.functions[hir.main].body.exprs[*value].kind,
                    ExprKind::ModuleVar(id) if id == binding));
            }
        }
    }
    assert!(hir.default_exprs.iter().any(|id| matches!(
        hir.functions[hir.main].body.exprs[*id].kind,
        ExprKind::ModuleVar(value) if value == binding
    )));
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    std::fs::write(
        root.join("mir.txt"),
        format!("{}\n{diagnostics:?}\n", ryupy_mir::pretty::dump(&program)),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
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
    let statements: Vec<_> = program.functions[program.entry]
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| block.stmts.iter())
        .collect();
    let positions = |wanted: RuntimeFn| -> Vec<usize> {
        statements
            .iter()
            .enumerate()
            .filter_map(|(index, statement)| {
                let found = match statement {
                    Statement::Assign {
                        value:
                            Rvalue::Call {
                                callee: Callee::Runtime(function),
                                ..
                            },
                        ..
                    }
                    | Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(function),
                        ..
                    }) => *function == wanted,
                    _ => false,
                };
                found.then_some(index)
            })
            .collect()
    };
    let validate = positions(RuntimeFn::ValidateOrdinaryBase);
    let make = positions(RuntimeFn::PrepareRuntimeDerivedClass);
    assert!(positions(RuntimeFn::MakeClass).is_empty());
    assert!(positions(RuntimeFn::PrepareOrdinaryMiClass).is_empty());
    let guard = positions(RuntimeFn::InstanceClassCellGuard);
    let finish = positions(RuntimeFn::FinalizeOrdinaryClass);
    assert_eq!(
        (validate.len(), make.len(), finish.len(), guard.len()),
        (1, 1, 1, 1)
    );
    let writes: Vec<_> = statements
        .iter()
        .enumerate()
        .filter_map(|(index, statement)| match statement {
            Statement::SetGlobal { dest, value }
                if *dest == public && !matches!(value, Operand::Const(Constant::Null)) =>
            {
                Some(index)
            }
            _ => None,
        })
        .collect();
    assert_eq!(writes.len(), 2, "只允许旧值和最终完成值写公开槽");
    assert!(
        writes[0] < validate[0]
            && validate[0] < make[0]
            && make[0] < finish[0]
            && finish[0] < writes[1]
    );
    let decorators: Vec<_> = positions(RuntimeFn::CallValue)
        .into_iter()
        .filter(|index| *index > finish[0] && *index < writes[1])
        .collect();
    assert_eq!(decorators.len(), 1, "唯一装饰调用在完成后、发布前");
    let private_writes: Vec<_> = statements
        .iter()
        .enumerate()
        .filter_map(|(index, statement)| {
            matches!(statement, Statement::SetGlobal { dest, .. } if *dest == private)
                .then_some(index)
        })
        .collect();
    assert_eq!(private_writes.len(), 1, "内部原类槽也不得提前公开半成品");
    assert!(
        finish[0] < guard[0]
            && guard[0] < private_writes[0]
            && private_writes[0] < decorators[0]
            && decorators[0] < writes[1]
    );
}
