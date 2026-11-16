//! 参数保形回归：先核真实前端基底，再单独调用原Resolver检查空集合/未知和后继硬义务。
//! 局部producer仅为已有helper合同夹具，不替代全程序验证；原B真实prepare/finish测试仍保留。
use super::*;
use crate::generator_product_defaults::Defaults;
use crate::generator_product_failure::Limitations;
use crate::generator_product_values::{Bound, Bounds, Resolver, Set, Value, complete_arguments};

fn runtime(runtime: RuntimeFn, args: Vec<Operand>) -> Rvalue {
    Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args,
    }
}
fn assign(high: &mut High, value: Rvalue, ty: MirType) -> Operand {
    let body = &mut high.program.functions[high.program.entry].body;
    let dest = body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    });
    let block = &mut body.blocks[body.entry];
    block
        .stmts
        .insert(block.stmts.len() - 1, Statement::Assign { dest, value });
    Operand::Local(dest)
}
fn empty_input(high: &mut High, unknown: bool) -> Operand {
    let value = if unknown {
        runtime(RuntimeFn::GetItem, vec![Operand::Const(Constant::None); 2])
    } else {
        // LoadName的正常返回排除Null，因此此夹具为完整Bottom，不是未知helper。
        runtime(
            RuntimeFn::LoadName,
            vec![
                Operand::Const(Constant::Null),
                Operand::Const(Constant::Str("missing".into())),
            ],
        )
    };
    assign(high, value, MirType::Dynamic)
}
fn inspect(high: &High, value: &Rvalue) -> Result<Set, Failure> {
    let bounds = Bounds::from_facts(&high.facts, &high.origins).unwrap();
    let defaults = Defaults::inspect(&high.program, &high.exports).unwrap();
    let resolver = Resolver {
        program: &high.program,
        bounds: &bounds,
        generators: high.plan.functions(),
        defaults: &defaults,
        properties: Default::default(),
    };
    resolver.inspect_value(high.program.entry, None, value)
}

#[test]
fn completed_arguments_keep_every_slot_and_missing_is_an_internal_error() {
    for empty in 0..3 {
        let mut expected = vec![
            vec![Value::Integer(0)],
            vec![Value::Integer(1)],
            vec![Value::Integer(2)],
        ];
        expected[empty].clear();
        let actual = complete_arguments(
            Limitations::default(),
            expected.iter().cloned().map(Some).collect(),
        )
        .unwrap();
        assert_eq!(actual.len(), 3);
        assert_eq!(actual, expected);
        for missing in [false, true] {
            let mut values = expected.iter().cloned().map(Some).collect::<Vec<_>>();
            if missing {
                values[empty] = None;
            }
            let mut limitations = Limitations::default();
            limitations.note("尚未闭合的局部定义");
            domain(complete_arguments(limitations, values).map(|_| ()));
        }
        let mut values = expected.into_iter().map(Some).collect::<Vec<_>>();
        values[empty] = None;
        invalid_reason(
            complete_arguments(Limitations::default(), values).map(|_| ()),
            "调用实参在能力检查完成后仍缺位",
        );
    }
}

#[test]
fn runtime_empty_and_unknown_targets_preserve_existing_bottom_contract() {
    let mut high = High::baseline(B);
    let bounds = Bounds::from_facts(&high.facts, &high.origins).unwrap();
    let getter = high
        .program
        .functions
        .iter()
        .find(|(_, f)| f.name == "error")
        .unwrap()
        .0;
    assert!(
        bounds.returns[&getter].value().unwrap().is_empty(),
        "原B必抛getter必须保持完整Bottom"
    );
    let error = assign(
        &mut high,
        runtime(
            RuntimeFn::MakeException,
            vec![
                Operand::Const(Constant::Str("ValueError".into())),
                Operand::Const(Constant::None),
            ],
        ),
        MirType::Dynamic,
    );
    for unknown in [false, true] {
        let target = empty_input(&mut high, unknown);
        let result = inspect(
            &high,
            &runtime(RuntimeFn::ExceptionMatch, vec![error.clone(), target]),
        );
        if unknown {
            domain(result.map(|_| ()));
        } else {
            assert_eq!(
                result.unwrap(),
                vec![Value::Primitive(trace_domain::Primitive::Bool)]
            );
        }
    }
    // 上方baseline走真实B高层；原21项中的prepare/finish继续覆盖实际lower一次。
}

#[test]
fn empty_runtime_argument_never_masks_a_later_known_protocol_error() {
    for unknown in [false, true] {
        let mut high = High::baseline(B);
        let error = empty_input(&mut high, unknown);
        let depth = assign(
            &mut high,
            runtime(
                RuntimeFn::TracePush,
                vec![
                    Operand::Const(Constant::Str("file".into())),
                    Operand::Const(Constant::Str("function".into())),
                ],
            ),
            MirType::I64,
        );
        let control = inspect(
            &high,
            &runtime(RuntimeFn::TraceV1Park, vec![error.clone(), depth]),
        );
        if unknown {
            domain(control.map(|_| ()));
        } else {
            // 空集合无负反证，但完整Bottom仍不满足Park的非空Error正向合同。
            assert!(
                matches!(&control, Err(Failure::InvalidCompilerState(reason))
                    if reason == "实际helper TraceV1Park 的协议来源合同损坏"),
                "{control:?}"
            );
        }
        invalid_reason(
            inspect(
                &high,
                &runtime(
                    RuntimeFn::TraceV1Park,
                    vec![error, Operand::Const(Constant::I64(0))],
                ),
            )
            .map(|_| ()),
            "实际helper TraceV1Park 第1实参协议来源错误",
        );
    }
}

#[test]
fn function_calls_keep_empty_inputs_unproved_and_check_later_arguments() {
    let source = with_definitions(
        "def passthrough(first, second):\n    return second\n",
        true,
        "passthrough(0, 0)\npassthrough(None, None)",
    );
    for kind in 0..3 {
        for mode in 0..4 {
            for bad in [false, true] {
                let mut high = High::baseline(&source);
                let target = high
                    .program
                    .functions
                    .iter()
                    .find(|(_, f)| f.name == "passthrough")
                    .unwrap()
                    .0;
                let definition = &high.program.functions[target];
                assert_eq!(definition.params.len(), 3);
                assert_eq!(definition.param_meta.len(), 2);
                assert!(
                    definition
                        .param_meta
                        .iter()
                        .all(|p| p.ty == MirType::Dynamic)
                );
                let first = definition.params[0];
                let receiver = high.program.functions[high.program.entry]
                    .body
                    .blocks
                    .iter()
                    .flat_map(|(_, b)| &b.stmts)
                    .find_map(|s| match s {
                        Statement::Assign {
                            dest,
                            value: Rvalue::MakeFunction { func, .. },
                        } if *func == target => Some(Operand::Local(*dest)),
                        _ => None,
                    })
                    .unwrap();
                let input = if mode < 2 {
                    empty_input(&mut high, mode == 1)
                } else {
                    Operand::Const(Constant::None)
                };
                let second = if bad {
                    Constant::Str("bad".into())
                } else {
                    Constant::None
                };
                let mut args = vec![input, Operand::Const(second)];
                let call = match kind {
                    0 => {
                        args.push(receiver);
                        Rvalue::Call {
                            callee: Callee::Fn(target),
                            args,
                        }
                    }
                    1 => Rvalue::DynCallValue {
                        callee: receiver,
                        args,
                    },
                    _ => runtime(
                        RuntimeFn::CallValue,
                        vec![
                            receiver,
                            Operand::Array(args),
                            Operand::Array(vec![]),
                            Operand::Array(vec![]),
                        ],
                    ),
                };
                let mut bounds = Bounds::from_facts(&high.facts, &high.origins).unwrap();
                for parameter in high.program.functions[target].params.iter().take(2) {
                    let expected = bounds.parameters[&(target, *parameter)].value().unwrap();
                    assert_eq!(expected.len(), 2);
                    assert!(expected.contains(&Value::None));
                    assert!(expected.contains(&Value::Primitive(trace_domain::Primitive::Integer)));
                }
                if mode == 2 {
                    bounds
                        .parameters
                        .insert((target, first), Bound::Known(vec![]));
                }
                let defaults = Defaults::inspect(&high.program, &high.exports).unwrap();
                let resolver = Resolver {
                    program: &high.program,
                    bounds: &bounds,
                    generators: high.plan.functions(),
                    defaults: &defaults,
                    properties: Default::default(),
                };
                let checked = resolver.inspect_value(high.program.entry, None, &call);
                if bad {
                    invalid_reason(checked.map(|_| ()), "真实参数/env/owner不满足函数定义合同");
                } else if mode == 3 {
                    assert_eq!(
                        checked.unwrap(),
                        bounds.returns[&target].value().unwrap().clone()
                    );
                } else {
                    domain(checked.map(|_| ()));
                }
            }
        }
    }
    // 动态callee为完整Bottom或未知时仍拒绝；不借保形扩展调用成功域。
    for unknown in [false, true] {
        let mut high = High::baseline(B);
        let callee = empty_input(&mut high, unknown);
        domain(
            inspect(
                &high,
                &Rvalue::DynCallValue {
                    callee: callee.clone(),
                    args: vec![],
                },
            )
            .map(|_| ()),
        );
        domain(
            inspect(
                &high,
                &runtime(
                    RuntimeFn::CallValue,
                    vec![
                        callee,
                        Operand::Array(vec![]),
                        Operand::Array(vec![]),
                        Operand::Array(vec![]),
                    ],
                ),
            )
            .map(|_| ()),
        );
    }
}
