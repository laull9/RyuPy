//! 独审追加的帧槽与异常边负测；正常生成路径由父模块的正例共同保护。
use super::compile_instance;
use crate::*;

#[test]
fn instance_review_owner_frame_writes_are_not_arbitrary() {
    for (before, use_state, dynamic_index) in [
        (true, false, false),
        (false, false, false),
        (true, true, false),
        (false, false, true),
    ] {
        let (mut program, _) = compile_instance("def g():\n    yield outside\n");
        let function = program
            .functions
            .iter_mut()
            .find_map(|(_, f)| {
                matches!(
                    f.module_state_source,
                    Some(InstanceStateSource::GeneratorFrame { .. })
                )
                .then_some(f)
            })
            .unwrap();
        let Some(InstanceStateSource::GeneratorFrame { frame, slot }) =
            function.module_state_source
        else {
            unreachable!()
        };
        let state = function.module_state.unwrap();
        let write = Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArraySet),
            args: vec![
                Operand::Local(frame),
                if dynamic_index {
                    Operand::Local(state)
                } else {
                    Operand::Const(Constant::I64(slot as i64))
                },
                if use_state {
                    Operand::Local(state)
                } else {
                    Operand::Const(Constant::Null)
                },
            ],
        });
        let block = &mut function.body.blocks[function.body.entry];
        if before {
            block.stmts.insert(0, write);
        } else {
            block.stmts.push(write);
        }
        assert!(
            validate::validate(&program).is_err(),
            "帧owner槽伪写未拒绝: before={before}, state={use_state}, dynamic={dynamic_index}"
        );
    }
}

#[test]
fn instance_review_captured_context_must_dominate_exception_paths() {
    let (mut program, _) =
        compile_instance("try:\n    class C:\n        pass\nfinally:\n    cleanup()\n");
    let function = &mut program.functions[program.entry];
    let context = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .find_map(|s| match s {
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext),
                        ..
                    },
            } => Some(*dest),
            _ => None,
        })
        .unwrap();
    let handler = function.body.regions[0].handler;
    let dest = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    function.body.blocks[handler].stmts.push(Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceDefinitionModule),
            args: vec![Operand::Local(context)],
        },
    });
    assert!(
        validate::validate(&program).is_err(),
        "捕获语句前抛出的异常可以到handler，不能信任其context"
    );
}

#[test]
fn instance_review_resume_cannot_masquerade_as_generator_factory() {
    let (mut program, _) = compile_instance("def g():\n    yield outside\n");
    let resume = program
        .functions
        .iter()
        .find_map(|(id, f)| {
            matches!(
                f.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
            .then_some(id)
        })
        .unwrap();
    let function = &mut program.functions[resume];
    let Some(InstanceStateSource::GeneratorFrame { slot, .. }) = function.module_state_source
    else {
        unreachable!()
    };
    let mut values = vec![Operand::Const(Constant::None); slot + 1];
    values[slot] = Operand::Local(function.module_state.unwrap());
    let dest = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    function.body.blocks[function.body.entry].stmts.insert(
        0,
        Statement::Assign {
            dest,
            value: Rvalue::MakeGenerator {
                resume,
                frame: values,
                origin: None,
            },
        },
    );
    assert!(
        validate::validate(&program).is_err(),
        "owned generator必须由显式FunctionParameter工厂创建"
    );
}
