//! 实例零参调用的来源与改写负测；不允许快照/错Env/隐藏参数冒充当前Python绑定。
use crate::*;
const SIMPLE: &str = "class C:\n    def f(self):\n        return super()\n";
const CELL: &str = "class C:\n    def f(self, other):\n        def replace():\n            nonlocal self\n            self = other\n        replace()\n        return super()\n";

fn compile(source: &str, instance: bool) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("super.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            instance_globals: instance,
            ..Default::default()
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    program
}
fn method(program: &mut Program) -> &mut Function {
    program
        .functions
        .iter_mut()
        .find(|(_, function)| function.name == "f")
        .unwrap()
        .1
}
fn zero(function: &mut Function) -> (&mut Operand, &mut ZeroCallFrame) {
    function
        .body
        .blocks
        .iter_mut()
        .flat_map(|(_, block)| &mut block.stmts)
        .find_map(|statement| {
            if let Statement::Assign {
                value: Rvalue::InstanceZeroCall { callee, frame },
                ..
            } = statement
            {
                Some((callee, frame))
            } else {
                None
            }
        })
        .unwrap()
}
fn rejected(program: &Program) {
    let errors = validate::validate(program).expect_err("损坏的显式零参frame必须拒绝");
    assert!(
        errors
            .iter()
            .any(|error| error.contains("零参frame") || error.contains("Env")),
        "{errors:?}"
    );
}

#[test]
fn lexical_super_pipeline_preserves_real_bindings_and_default_isolation() {
    for source in [
        SIMPLE,
        CELL,
        "alias = super\ndef f(self):\n    return alias()\n",
        "class C:\n    def f(self, super):\n        return super()\n",
    ] {
        let mut program = compile(source, true);
        passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
        validate::validate(&program).unwrap();
        assert!(pretty::dump(&program).contains("instance.zero_call"));
    }
    let program = compile(SIMPLE, false);
    assert!(!pretty::dump(&program).contains("instance.zero_call"));
    assert!(pretty::dump(&program).contains("DynBuiltin"));
}

#[test]
fn lexical_super_rejects_missing_wrong_slot_and_wrong_environment() {
    for mode in 0..3 {
        let mut program = compile(SIMPLE, true);
        let function = method(&mut program);
        let wrong = function.module_state.unwrap();
        let (_, frame) = zero(function);
        match mode {
            0 => frame.class = SuperClassSource::Missing,
            1 => {
                if let SuperClassSource::Lexical { index, .. } = &mut frame.class {
                    *index += 1;
                }
            }
            _ => {
                if let SuperClassSource::Lexical { environment, .. } = &mut frame.class {
                    *environment = wrong;
                }
            }
        }
        rejected(&program);
    }
}

#[test]
fn lexical_super_rejects_arg_snapshot_and_hidden_parameter() {
    for hidden in [false, true] {
        let mut program = compile(SIMPLE, true);
        let function = method(&mut program);
        let source = function.params[0];
        let fake = if hidden {
            function.module_state.unwrap()
        } else {
            let fake = function.body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            function.body.blocks[function.body.entry].stmts.insert(
                0,
                Statement::Assign {
                    dest: fake,
                    value: Rvalue::Use(Operand::Local(source)),
                },
            );
            fake
        };
        zero(function).1.first_parameter = Some(fake);
        rejected(&program);
    }
}

#[test]
fn lexical_super_rejects_raw_environment_as_callable() {
    let mut program = compile(SIMPLE, true);
    let function = method(&mut program);
    let env = function.env.unwrap();
    *zero(function).0 = Operand::Local(env);
    rejected(&program);
}

#[test]
fn lexical_super_rejects_early_cell_call_and_replaced_parameter_cell() {
    for early in [false, true] {
        let mut program = compile(CELL, true);
        let function = method(&mut program);
        let entry = function.body.entry;
        let parameter = function.params[0];
        if early {
            let statements = &mut function.body.blocks[entry].stmts;
            let index = statements
                .iter()
                .position(|statement| {
                    matches!(
                        statement,
                        Statement::Assign {
                            value: Rvalue::InstanceZeroCall { .. },
                            ..
                        }
                    )
                })
                .unwrap();
            let call = statements.remove(index);
            statements.insert(0, call);
        } else {
            function.body.blocks[entry].stmts.push(Statement::Assign {
                dest: parameter,
                value: Rvalue::Use(Operand::Const(Constant::Null)),
            });
        }
        rejected(&program);
    }
}

#[test]
fn lexical_super_own_class_cell_is_not_free_and_outer_free_stays_unsupported() {
    let mut program = compile(
        "class C:\n    def f(self):\n        __class__ = C\n        def inner():\n            return __class__\n        return super()\n",
        true,
    );
    assert_eq!(
        zero(method(&mut program)).1.class,
        SuperClassSource::Missing
    );
    let mut program = compile(
        "def outer():\n    __class__ = 42\n    def f(self):\n        return super()\n    return f\n",
        true,
    );
    assert_eq!(
        zero(method(&mut program)).1.class,
        SuperClassSource::UnsupportedFree
    );
}

#[test]
fn lexical_super_generator_alias_has_no_supported_frame() {
    let program = compile(
        "class C:\n    def f(self):\n        alias = super\n        yield alias()\n",
        true,
    );
    assert!(!pretty::dump(&program).contains("instance.zero_call"));
}
