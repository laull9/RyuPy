//! 真实provider生成器路由的结构与抗变异；普通Instance源码资格继续保留负例。
use crate::provider_namespace_tests::compile;
use crate::*;
fn source() -> &'static str {
    "def stream(holder):\n    try:\n        try:\n            yield 1\n        except holder.target as caught:\n            yield caught\n        except BaseException:\n            yield 2\n    except AttributeError:\n        yield 3\n"
}
#[test]
fn provider_handlers_have_shared_evidence_and_actual_physical_anchor() {
    assert!(compile(source(), false).is_err());
    let program = compile(source(), true).unwrap();
    let functions = program
        .functions
        .iter()
        .filter(|(_, f)| {
            matches!(
                f.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
        })
        .collect::<Vec<_>>();
    assert_eq!(functions.len(), 1);
    let body = &functions[0].1.body;
    assert_eq!(body.source_handler_contracts.len(), 2);
    assert!(
        body.source_handler_contracts
            .iter()
            .all(|contract| contract.generator.is_some())
    );
    assert_eq!(body.regions.len(), 1);
    assert_eq!(body.regions[0].source_handler_chain, Some(0));
}
#[test]
fn router_contract_rejects_missing_anchor_active_bounds_pc_frame_and_catch_clear() {
    for fault in 0..15 {
        let mut program = compile(source(), true).unwrap();
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
        let frame = function.params[0];
        match fault {
            0 => function.body.source_handler_contracts.clear(),
            1 => function.body.regions[0].source_handler_chain = None,
            2 => function.instance_capabilities.provider_namespace = false,
            3 => {
                function.body.blocks[function.body.entry].stmts[0] =
                    Statement::Eval(Rvalue::Use(Operand::Const(Constant::None)));
            }
            4 => {
                let value = function
                    .body
                    .blocks
                    .iter_mut()
                    .flat_map(|(_, b)| &mut b.stmts)
                    .find_map(|stmt| match stmt {
                        Statement::Assign {
                            value:
                                Rvalue::Binary {
                                    op: BinOp::Ge, rhs, ..
                                },
                            ..
                        } => Some(rhs),
                        _ => None,
                    })
                    .unwrap();
                *value = Operand::Const(Constant::I64(-90));
            }
            5 => {
                let value=function.body.blocks.iter_mut().flat_map(|(_,b)|&mut b.stmts).find_map(|stmt|match stmt {Statement::Eval(Rvalue::Call{callee:Callee::Runtime(RuntimeFn::ArraySet),args}) if matches!(args.as_slice(),[Operand::Local(f),Operand::Const(Constant::I64(3)),Operand::Local(_)]if *f==frame)=>Some(&mut args[2]),_=>None}).unwrap();
                *value = Operand::Const(Constant::None);
            }
            6 => {
                let value=function.body.blocks.iter_mut().flat_map(|(_,b)|&mut b.stmts).find_map(|stmt|match stmt {Statement::Eval(Rvalue::Call{callee:Callee::Runtime(RuntimeFn::ArraySet),args}) if matches!(args.as_slice(),[Operand::Local(f),Operand::Const(Constant::I64(3)),Operand::Const(Constant::None)]if *f==frame)=>Some(&mut args[1]),_=>None}).unwrap();
                *value = Operand::Const(Constant::I64(2));
            }
            7 => {
                let region = function.body.regions[0].clone();
                function.body.regions.push(region);
            }
            8 => {
                function.body.blocks[function.body.entry]
                    .stmts
                    .push(Statement::Assign {
                        dest: frame,
                        value: Rvalue::Use(Operand::Const(Constant::None)),
                    });
            }
            9 => function.body.regions[0].try_len += 1,
            10 => {
                let target=function.body.blocks.iter().find_map(|(id,b)| b.stmts.iter().any(|stmt|matches!(stmt,Statement::Assign{value:Rvalue::Call{callee:Callee::Runtime(RuntimeFn::ArrayGet),args},..}if matches!(args.as_slice(),[Operand::Local(f),Operand::Const(Constant::I64(3))]if *f==frame))).then_some(id)).unwrap();
                function.body.blocks[target].stmts.clear();
                function.body.blocks[target].terminator = Terminator::Unreachable;
            }
            11 => {
                let retry = la_arena::Idx::from_raw(la_arena::RawIdx::from_u32(
                    (function.body.regions[0].try_len + 2) as u32,
                ));
                let value = function
                    .body
                    .blocks
                    .iter_mut()
                    .find_map(|(_, b)| {
                        matches!(b.terminator,Terminator::Goto(to)if to==retry).then_some(b)
                    })
                    .unwrap();
                if let Statement::Assign {
                    value: Rvalue::Use(value),
                    ..
                } = &mut value.stmts[0]
                {
                    *value = Operand::Const(Constant::I64(999));
                } else {
                    panic!("handler PC")
                }
            }
            12 => {
                let value=function.body.blocks.iter_mut().flat_map(|(_,b)|&mut b.stmts).find_map(|stmt|match stmt {Statement::Eval(Rvalue::Call{callee:Callee::Runtime(RuntimeFn::ArraySet),args}) if matches!(args.as_slice(),[Operand::Local(f),Operand::Const(Constant::I64(0)),_]if *f==frame)=>Some(&mut args[2]),_=>None}).unwrap();
                *value = Operand::Const(Constant::None);
            }
            13 => {
                let alias = function.body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                function.body.blocks[function.body.entry]
                    .stmts
                    .push(Statement::Assign {
                        dest: alias,
                        value: Rvalue::Use(Operand::Local(frame)),
                    });
            }
            14 => {
                let value = function
                    .body
                    .blocks
                    .iter_mut()
                    .find_map(|(_, b)| {
                        b.stmts
                            .iter()
                            .any(|stmt| {
                                matches!(
                                    stmt,
                                    Statement::Assign {
                                        value: Rvalue::Binary { op: BinOp::Lt, .. },
                                        ..
                                    }
                                )
                            })
                            .then_some(b)
                    })
                    .unwrap();
                if let Terminator::Branch { then_, else_, .. } = &mut value.terminator {
                    *else_ = *then_;
                }
            }
            _ => unreachable!(),
        }
        assert!(validate::validate(&program).is_err(), "fault {fault}");
    }
}
#[test]
fn independent_source_shapes_survive_actual_handler_lowering() {
    for source in [
        include_str!("../../../tests/cases/stock_module_namespaces/inlet_handlers/inlet_stream.py"),
        include_str!("../../../tests/cases/stock_module_namespaces/quarry_handlers/quarry_flow.py"),
    ] {
        let mut program = compile(source, true).unwrap();
        passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    }
}
