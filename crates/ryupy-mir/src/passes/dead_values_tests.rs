//! 死值装箱消除的正反结构回归：循环后读取、闭包、帧、异常和副作用均不能误删。
use super::{DeadValues, MirPass};
use crate::mir::*;
use la_arena::Arena;

fn sample() -> (Function, [LocalId; 4]) {
    let mut locals = Arena::default();
    let counter = locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    });
    let boxed = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let target = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("i".into()),
        is_cell: false,
    });
    let unused = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![
            Statement::Line(1),
            Statement::Assign {
                dest: counter,
                value: Rvalue::Use(Operand::Const(Constant::I64(999))),
            },
            Statement::Assign {
                dest: boxed,
                value: Rvalue::Box {
                    value: Operand::Local(counter),
                    from: MirType::I64,
                },
            },
            Statement::Assign {
                dest: target,
                value: Rvalue::Use(Operand::Local(boxed)),
            },
        ],
        terminator: Terminator::Return(None),
    });
    (
        Function {
            module_state: None,
            instance_capabilities: Default::default(),
            module_state_source: None,
            name: "dead".into(),
            codegen_name: "dead".into(),
            source_file: "dead.py".into(),
            definition: None,
            params: vec![],
            ret: MirType::Unit,
            body: Body {
                source_handler_contracts: Vec::new(),
                locals,
                blocks,
                entry,
                regions: vec![],
            },
            param_meta: vec![],
            function_global: None,
            specialization_identity_global: None,
            defaults_global: None,
            signature_global: None,
            env: None,
            dynamic_origin: None,
        },
        [counter, boxed, target, unused],
    )
}

#[test]
fn unread_loop_target_removes_box_chain_but_keeps_declarations_and_lines() {
    let (mut function, _) = sample();
    function.body.blocks[function.body.entry].terminator = Terminator::Goto(function.body.entry);
    DeadValues.run(&mut function);
    assert_eq!(function.body.locals.len(), 4);
    assert!(matches!(
        function.body.blocks[function.body.entry].stmts.as_slice(),
        [Statement::Line(1)]
    ));
}

#[test]
fn returning_loop_target_preserves_all_definitions() {
    let (mut function, [_, _, target, _]) = sample();
    function.body.blocks[function.body.entry].terminator =
        Terminator::Return(Some(Operand::Local(target)));
    DeadValues.run(&mut function);
    assert_eq!(function.body.blocks[function.body.entry].stmts.len(), 4);
}

#[test]
fn closure_cell_write_and_nested_array_reads_preserve_boxed_value() {
    let (mut function, [_, _, target, _]) = sample();
    function.body.blocks[function.body.entry]
        .stmts
        .push(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::CellSet),
            args: vec![
                Operand::Const(Constant::Null),
                Operand::Array(vec![Operand::Array(vec![Operand::Local(target)])]),
            ],
        }));
    DeadValues.run(&mut function);
    assert_eq!(function.body.blocks[function.body.entry].stmts.len(), 5);
}

#[test]
fn discarded_calls_unbox_arithmetic_and_field_access_still_execute() {
    let (mut function, [counter, _, target, unused]) = sample();
    let block = &mut function.body.blocks[function.body.entry];
    block.stmts.extend([
        Statement::Assign {
            dest: unused,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynNone),
                args: vec![],
            },
        },
        Statement::Assign {
            dest: counter,
            value: Rvalue::Unbox {
                value: Operand::Local(target),
                to: MirType::I64,
            },
        },
        Statement::Assign {
            dest: counter,
            value: Rvalue::Binary {
                op: BinOp::Add,
                lhs: Operand::Local(counter),
                rhs: Operand::Const(Constant::I64(1)),
            },
        },
        Statement::Assign {
            dest: unused,
            value: Rvalue::GetField {
                obj: Operand::Local(target),
                field: 0,
            },
        },
    ]);
    DeadValues.run(&mut function);
    assert_eq!(function.body.blocks[function.body.entry].stmts.len(), 8);
}

#[test]
fn generator_frames_and_exception_metadata_are_conservative_roots() {
    let (mut function, [_, boxed, _, _]) = sample();
    function.body.regions.push(Region {
        source_handler_chain: None,
        kind: RegionKind::CatchAll,
        try_start: function.body.entry,
        try_len: 1,
        handler: function.body.entry,
        exc_local: Some(boxed),
    });
    DeadValues.run(&mut function);
    assert_eq!(function.body.blocks[function.body.entry].stmts.len(), 3);
    let (mut function, [_, _, target, unused]) = sample();
    function.body.blocks[function.body.entry]
        .stmts
        .push(Statement::Assign {
            dest: unused,
            value: Rvalue::MakeGenerator {
                resume: FnId::from_raw(la_arena::RawIdx::from(0)),
                frame: vec![Operand::Local(target)],
                origin: None,
            },
        });
    DeadValues.run(&mut function);
    assert_eq!(function.body.blocks[function.body.entry].stmts.len(), 5);
}
