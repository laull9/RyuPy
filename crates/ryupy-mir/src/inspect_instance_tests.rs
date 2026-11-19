//! inspect Instance AOT后置门的真实MIR变异测试：注解不能绕过ABI/拆箱/源context约束。
use crate::*;

fn program() -> Program {
    let source = "def convert(value: str) -> int:\n    return int(value)\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("inspect.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let info = ryupy_hir::ModuleInfo {
        instance_globals: true,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty());
    program
}
fn callable(program: &Program) -> FnId {
    program
        .functions
        .iter()
        .find(|(_, f)| f.name == "convert")
        .unwrap()
        .0
}
#[test]
fn inspect_profile_keeps_annotations_but_requires_real_dynamic_abi() {
    let mut program = program();
    inspect_instance_contract::validate(&program).unwrap();
    let id = callable(&program);
    let function = &mut program.functions[id];
    let arg = function.params[0];
    function.body.locals[arg].ty = MirType::I64;
    function.param_meta[0].ty = MirType::I64;
    assert!(
        inspect_instance_contract::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("Dynamic公开ABI"))
    );
}
#[test]
fn inspect_profile_rejects_hidden_unbox_and_typed_direct_call() {
    let mut program = program();
    let id = callable(&program);
    let function = &mut program.functions[id];
    let temp = function.body.locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    });
    function.body.blocks[function.body.entry]
        .stmts
        .push(Statement::Assign {
            dest: temp,
            value: Rvalue::Unbox {
                value: Operand::Const(Constant::None),
                to: MirType::I64,
            },
        });
    assert!(
        inspect_instance_contract::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("不得拆箱Python值"))
    );
    function_ret_and_direct_call(&mut program, id, temp);
    assert!(
        inspect_instance_contract::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("不得直调typed核心"))
    );
}
fn function_ret_and_direct_call(program: &mut Program, id: FnId, temp: LocalId) {
    let function = &mut program.functions[id];
    function.ret = MirType::I64;
    let args = function
        .params
        .iter()
        .copied()
        .map(Operand::Local)
        .collect();
    function.body.blocks[function.body.entry]
        .stmts
        .push(Statement::Assign {
            dest: temp,
            value: Rvalue::Call {
                callee: Callee::Fn(id),
                args,
            },
        });
}
#[test]
fn inspect_profile_rejects_returning_private_source_context() {
    let mut program = program();
    let id = callable(&program);
    let function = &mut program.functions[id];
    let context = function.module_state.unwrap();
    function.body.blocks[function.body.entry].terminator =
        Terminator::Return(Some(Operand::Local(context)));
    assert!(
        inspect_instance_contract::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("私有源context不得逃逸"))
    );
}
