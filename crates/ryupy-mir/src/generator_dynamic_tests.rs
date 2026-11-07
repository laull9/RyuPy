//! Legacy动态handler的真实降级合同：按实际LocalId检查帧映射，不以指令数量代替身份。
use crate::*;
use std::collections::BTreeSet;

fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("generator_dynamic.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(validate::validate(&program).is_ok());
    program
}

fn assignments(function: &Function) -> Vec<(LocalId, &Rvalue)> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|statement| match statement {
            Statement::Assign { dest, value } => Some((*dest, value)),
            _ => None,
        })
        .collect()
}

fn runtime_values(function: &Function, wanted: RuntimeFn) -> Vec<(LocalId, &[Operand])> {
    assignments(function)
        .into_iter()
        .filter_map(|(dest, value)| match value {
            Rvalue::Call {
                callee: Callee::Runtime(actual),
                args,
            } if *actual == wanted => Some((dest, args.as_slice())),
            _ => None,
        })
        .collect()
}

fn local(value: &Operand) -> LocalId {
    match value {
        Operand::Local(id) => *id,
        _ => panic!("合同要求真实局部操作数"),
    }
}

fn generated(program: &Program) -> (&Function, usize) {
    let generated: Vec<_> = program
        .functions
        .iter()
        .flat_map(|(_, function)| assignments(function))
        .filter_map(|(_, value)| match value {
            Rvalue::MakeGenerator { resume, frame, .. } => Some((*resume, frame.len() - 4)),
            _ => None,
        })
        .collect();
    assert_eq!(generated.len(), 1);
    let (id, originals) = generated[0];
    (&program.functions[id], originals)
}

// Box/Unbox只追同一块内真实定义；绝不把任意同类型局部当作被保存值。
fn assert_frame_roundtrip(function: &Function, id: LocalId) {
    let frame = function.params[0];
    let index = i64::from(id.into_raw().into_u32()) + 4;
    let mut saved = BTreeSet::new();
    let mut restored = BTreeSet::new();
    for (block_id, block) in function.body.blocks.iter() {
        let mut boxes = Vec::new();
        let mut loads = Vec::new();
        for statement in &block.stmts {
            match statement {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Box {
                            value: Operand::Local(source),
                            ..
                        },
                } if *source == id => boxes.push(*dest),
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ArraySet),
                    args,
                }) if matches!(args.as_slice(), [Operand::Local(owner), Operand::Const(Constant::I64(slot)), _] if *owner == frame && *slot == index) =>
                {
                    assert!(
                        matches!(&args[2], Operand::Local(value) if *value == id || boxes.contains(value))
                    );
                    assert!(saved.insert(block_id), "同槽重复保存");
                }
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ArrayGet),
                            args,
                        },
                } if matches!(args.as_slice(), [Operand::Local(owner), Operand::Const(Constant::I64(slot))] if *owner == frame && *slot == index) =>
                {
                    if *dest == id {
                        assert!(restored.insert(block_id));
                    }
                    loads.push(*dest);
                }
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Unbox {
                            value: Operand::Local(source),
                            ..
                        },
                } if *dest == id && loads.contains(source) => {
                    assert!(restored.insert(block_id));
                }
                _ => {}
            }
        }
    }
    // 固定本源两个yield加唯一Retry均必须保存，而不是跨块OR得到“至少一次”。
    let spill_blocks: BTreeSet<_> = function.body.blocks.iter().filter_map(|(block_id, block)| {
        block.stmts.iter().any(|statement| matches!(statement,
            Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
            if matches!(args.as_slice(), [Operand::Local(owner), Operand::Const(Constant::I64(4)), _] if *owner == frame)
        )).then_some(block_id)
    }).collect();
    assert_eq!(spill_blocks.len(), 3, "固定源两yield和Retry的三个spill块");
    assert_eq!(
        saved, spill_blocks,
        "局部{id:?}每条暂停/Retry边都保存槽{index}"
    );
    assert_eq!(restored, BTreeSet::from([function.body.entry]));
}

#[test]
fn real_dynamic_temporaries_and_second_selection_survive_suspend() {
    let source = "def gen(first, second):\n    try:\n        raise ValueError('x')\n    except first as a:\n        yield a\n    except second as b:\n        try:\n            return 17\n        finally:\n            yield b\n";
    let program = compile(source);
    let (resume, original_count) = generated(&program);
    assert!(resume.body.source_handler_contracts.is_empty());
    assert!(resume.module_state.is_none());
    assert_eq!(resume.body.regions.len(), 1);
    assert!(matches!(resume.body.regions[0].kind, RegionKind::CatchAll));
    let enters = runtime_values(resume, RuntimeFn::HandlerEnter);
    let (ambient, args) = enters
        .iter()
        .find(|(_, args)| matches!(args, [Operand::Local(_)]))
        .unwrap();
    let original = local(&args[0]);
    assert_frame_roundtrip(resume, *ambient);
    assert_frame_roundtrip(resume, original);
    let values = assignments(resume);
    let assigned_integer = |number| -> BTreeSet<_> {
        values
            .iter()
            .filter_map(|(dest, value)| {
                matches!(value, Rvalue::Use(Operand::Const(Constant::I64(n))) if *n == number)
                    .then_some(*dest)
            })
            .filter(|id| (id.into_raw().into_u32() as usize) < original_count)
            .collect()
    };
    let initial = assigned_integer(-1);
    let selected: Vec<_> = initial
        .intersection(&assigned_integer(1))
        .copied()
        .collect();
    assert_eq!(selected.len(), 1, "selected必须有真实-1和第二handler索引1");
    let selected = selected[0];
    assert_eq!(resume.body.locals[selected].ty, MirType::I64);
    assert_frame_roundtrip(resume, selected);
    assert!(values.iter().any(|(_, value)| matches!(value,
        Rvalue::Binary { op: BinOp::Eq, lhs: Operand::Local(id), rhs: Operand::Const(Constant::I64(1)) }
        if *id == selected)));
    let flows = assigned_integer(2);
    assert!(!flows.is_empty(), "pending return必须有实际FLOW_RETURN局部");
    for flow in flows {
        assert_frame_roundtrip(resume, flow);
    }
    // escaped与其它清理临时也逐身份覆盖，不能只给三个已命名角色做特例。
    for (id, _) in resume.body.locals.iter().take(original_count) {
        assert_frame_roundtrip(resume, id);
    }
    assert_eq!(runtime_values(resume, RuntimeFn::ExceptionMatch).len(), 2);
    assert!(
        resume
            .body
            .blocks
            .iter()
            .all(|(_, block)| { !matches!(block.terminator, Terminator::Suspend { .. }) })
    );
}

#[test]
fn current_control_structures_do_not_enter_dynamic_generator_dispatch() {
    for (source, has_eh) in [
        ("def gen():\n    yield 1\n", false),
        (
            "def gen():\n    try:\n        yield 1\n    except ValueError:\n        yield 2\n",
            true,
        ),
    ] {
        let program = compile(source);
        let (resume, _) = generated(&program);
        assert!(runtime_values(resume, RuntimeFn::ExceptionMatch).is_empty());
        assert_eq!(!resume.body.regions.is_empty(), has_eh);
        assert_eq!(
            !runtime_values(resume, RuntimeFn::GeneratorRetry).is_empty(),
            has_eh
        );
    }
    let program = compile(
        "def ordinary(kind):\n    try:\n        raise ValueError('x')\n    except kind:\n        return 1\n",
    );
    assert!(program.functions.iter().all(|(_, function)| {
        assignments(function)
            .iter()
            .all(|(_, value)| !matches!(value, Rvalue::MakeGenerator { .. }))
    }));
    assert!(
        program.functions.iter().any(|(_, function)| {
            !runtime_values(function, RuntimeFn::ExceptionMatch).is_empty()
        })
    );
}

// 只读取既有逻辑EH的常量区间树，不解释Python或一般MIR。
fn logical_destination(function: &Function, active: i64) -> Option<i64> {
    let active_local = match function.body.blocks[function.body.entry].stmts.first() {
        Some(Statement::Assign {
            dest,
            value: Rvalue::Use(Operand::Const(Constant::I64(-1))),
        }) => *dest,
        _ => panic!("恢复入口active哨兵"),
    };
    let pc_local = retry_pc(function);
    let mut current = function.body.regions[0].handler;
    for _ in 0..function.body.blocks.len() {
        let block = &function.body.blocks[current];
        let mut branch = None;
        for statement in &block.stmts {
            match statement {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Binary {
                            op,
                            lhs: Operand::Local(lhs),
                            rhs: Operand::Const(Constant::I64(bound)),
                        },
                } => {
                    assert_eq!(*lhs, active_local, "区间比较必须读实际active局部");
                    branch = Some((
                        *dest,
                        match op {
                            BinOp::Lt => active < *bound,
                            BinOp::Ge => active >= *bound,
                            _ => panic!("逻辑EH只允许既有区间比较"),
                        },
                    ));
                }
                Statement::Assign {
                    dest,
                    value: Rvalue::Use(Operand::Const(Constant::I64(pc))),
                } => {
                    assert_eq!(*dest, pc_local, "选择必须写真实Retry PC局部");
                    return Some(*pc);
                }
                _ => {}
            }
        }
        current = match block.terminator {
            Terminator::Goto(target) => target,
            Terminator::Branch {
                ref cond,
                then_,
                else_,
            } => {
                let (id, value) = branch.expect("区间条件");
                assert_eq!(local(cond), id, "分支必须消费该区间比较");
                if value { then_ } else { else_ }
            }
            Terminator::Raise { .. } => return None,
            _ => panic!("目标必须经过真实逻辑handler选择"),
        };
    }
    panic!("逻辑EH区间树不可成环");
}

fn retry_pc(function: &Function) -> LocalId {
    assert_eq!(runtime_values(function, RuntimeFn::GeneratorRetry).len(), 1);
    let frame = function.params[0];
    for (_, block) in function.body.blocks.iter() {
        let has_retry = block.stmts.iter().any(|statement| {
            matches!(
                statement,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GeneratorRetry),
                        ..
                    },
                    ..
                }
            )
        });
        if !has_retry {
            continue;
        }
        let boxed = block.stmts.iter().find_map(|statement| match statement {
            Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
                if matches!(args.as_slice(), [Operand::Local(owner), Operand::Const(Constant::I64(0)), _] if *owner == frame)
                => Some(local(&args[2])),
            _ => None,
        }).expect("Retry实际PC槽写入");
        return block
            .stmts
            .iter()
            .find_map(|statement| match statement {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Box {
                            value: Operand::Local(source),
                            ..
                        },
                } if *dest == boxed => Some(*source),
                _ => None,
            })
            .expect("Retry实际PC装箱源");
    }
    panic!("必须有唯一Retry保存路径");
}

fn active_label(block: &BasicBlock) -> Option<i64> {
    match block.stmts.first() {
        Some(Statement::Assign {
            value: Rvalue::Use(Operand::Const(Constant::I64(label))),
            ..
        }) => Some(*label),
        _ => None,
    }
}

fn reachable_has_match(function: &Function, entry: BlockId) -> bool {
    let mut queue = vec![entry];
    let mut seen = BTreeSet::new();
    while let Some(id) = queue.pop() {
        if !seen.insert(id) {
            continue;
        }
        let block = &function.body.blocks[id];
        if block.stmts.iter().any(|statement| {
            matches!(
                statement,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ExceptionMatch),
                        ..
                    },
                    ..
                }
            )
        }) {
            return true;
        }
        match block.terminator {
            Terminator::Goto(target) => queue.push(target),
            Terminator::Branch { then_, else_, .. } => queue.extend([then_, else_]),
            _ => {}
        }
    }
    false
}

#[test]
fn getter_failure_routes_to_cleanup_not_the_sibling_chain() {
    let source = "def gen(target):\n    try:\n        try:\n            yield 1\n            raise ValueError('original')\n        except target.error as caught:\n            yield caught\n        except TypeError:\n            yield 'wrong sibling'\n    finally:\n        print('outside finally')\n";
    let program = compile(source);
    let (resume, _) = generated(&program);
    let getter = resume
        .body
        .blocks
        .iter()
        .find(|(_, block)| {
            block.stmts.iter().any(|statement| {
                matches!(
                    statement,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::DynGetAttr),
                            ..
                        },
                        ..
                    }
                )
            })
        })
        .expect("真实属性目标");
    let destination = logical_destination(resume, active_label(getter.1).unwrap()).unwrap();
    let (guard, _) = resume
        .body
        .blocks
        .iter()
        .find(|(_, block)| active_label(block) == Some(destination))
        .expect("逻辑PC必须映回原handler块");
    assert!(
        !reachable_has_match(resume, guard),
        "目标失败不能重进同级匹配"
    );
    let try_raise = resume.body.blocks.iter().find(|(_, block)| {
        matches!(block.terminator, Terminator::Raise { .. })
            && active_label(block).is_some_and(|label| label >= 0)
            && reachable_handler_has_match(resume, block)
    });
    assert!(try_raise.is_some(), "原try异常必须仍进入真实动态目标链");
}

fn reachable_handler_has_match(function: &Function, block: &BasicBlock) -> bool {
    let Some(pc) = logical_destination(function, active_label(block).unwrap()) else {
        return false;
    };
    let (target, _) = function
        .body
        .blocks
        .iter()
        .find(|(_, block)| active_label(block) == Some(pc))
        .expect("逻辑PC必须映回原handler块");
    reachable_has_match(function, target)
}
