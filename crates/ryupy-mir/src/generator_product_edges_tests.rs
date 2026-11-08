//! 仅测试有限规范边模板；真实Push/Pop、七pass及完整源输入由Contract/产品测试另外覆盖。
use super::*;
use la_arena::Arena;

fn id(value: u32) -> BlockId {
    BlockId::from_raw(value.into())
}

fn body() -> Body {
    let mut blocks = Arena::default();
    for index in 0..5 {
        blocks.alloc(BasicBlock {
            stmts: if index == 0 || index >= 3 {
                vec![Statement::Line(index + 1)]
            } else {
                vec![]
            },
            terminator: match index {
                0 => Terminator::Branch {
                    cond: Operand::Local(LocalId::from_raw(0.into())),
                    then_: id(1),
                    else_: id(2),
                },
                1 => Terminator::Goto(id(3)),
                2 => Terminator::Goto(id(4)),
                _ => Terminator::Return(None),
            },
        });
    }
    Body {
        source_handler_contracts: vec![],
        locals: Arena::default(),
        blocks,
        entry: id(0),
        regions: vec![],
    }
}

// Body本身不提供Clone；测试只复制这张有限图的实际字段，不改生产类型。
fn copy(source: &Body) -> Body {
    let mut blocks = Arena::default();
    for (_, block) in source.blocks.iter() {
        blocks.alloc(block.clone());
    }
    // 此文件的模板没有局部声明或handler来源合同，只比较typed边字段。
    assert!(source.locals.is_empty());
    assert!(source.source_handler_contracts.is_empty());
    Body {
        source_handler_contracts: vec![],
        locals: Arena::default(),
        blocks,
        entry: source.entry,
        regions: source.regions.clone(),
    }
}

fn region(start: u32, len: usize, handler: u32) -> Region {
    Region {
        source_handler_chain: None,
        kind: RegionKind::CatchAll,
        try_start: id(start),
        try_len: len,
        handler: id(handler),
        exc_local: None,
    }
}

fn matches(edges: &Edges, body: &Body, at: u32) -> bool {
    check(body).unwrap();
    edges
        .matches(body, id(at), &body.blocks[id(at)].terminator)
        .unwrap()
}

#[test]
fn ordered_branch_targets_condition_and_direct_constant_fold_are_checked() {
    let source = body();
    let edges = Edges::capture(&source).unwrap();
    for mutation in 0..3 {
        let mut changed = copy(&source);
        let Terminator::Branch { cond, then_, else_ } = &mut changed.blocks[id(0)].terminator
        else {
            unreachable!()
        };
        match mutation {
            0 => std::mem::swap(then_, else_),
            1 => *cond = Operand::Local(LocalId::from_raw(1.into())),
            _ => *then_ = id(4),
        }
        assert!(!matches(&edges, &changed, 0), "mutation {mutation}");
    }
    for value in [false, true] {
        let mut source = body();
        source.blocks[id(0)].terminator = Terminator::Branch {
            cond: Operand::Const(Constant::Bool(value)),
            then_: id(3),
            else_: id(4),
        };
        let edges = Edges::capture(&source).unwrap();
        source.blocks[id(0)].terminator = Terminator::Goto(if value { id(3) } else { id(4) });
        assert!(matches(&edges, &source, 0));
    }
}

#[test]
fn suspend_raw_fields_reject_new_empty_resume_prefix_and_other_changes() {
    let mut source = body();
    source.blocks[id(0)].terminator = Terminator::Suspend {
        value: Operand::Const(Constant::I64(7)),
        resume: id(1),
        input: None,
        delegated: false,
    };
    source.blocks[id(2)].terminator = Terminator::Goto(id(1));
    let edges = Edges::capture(&source).unwrap();
    assert!(matches(&edges, &source, 0));
    for mutation in 0..4 {
        let mut changed = copy(&source);
        let Terminator::Suspend {
            value,
            resume,
            input,
            delegated,
        } = &mut changed.blocks[id(0)].terminator
        else {
            unreachable!()
        };
        match mutation {
            0 => *resume = id(2), // 新空U→原R不能冒充原恢复入口。
            1 => *value = Operand::Const(Constant::I64(8)),
            2 => *input = Some(LocalId::from_raw(0.into())),
            _ => *delegated = true,
        }
        assert!(!matches(&edges, &changed, 0), "mutation {mutation}");
    }
    // 普通Goto亦不能越过一个真实Suspend的旧resume锚。
    source.blocks[id(4)].terminator = source.blocks[id(0)].terminator.clone();
    source.blocks[id(0)].terminator = Terminator::Goto(id(1));
    let edges = Edges::capture(&source).unwrap();
    source.blocks[id(0)].terminator = Terminator::Goto(id(3));
    assert!(!matches(&edges, &source, 0));
}

#[test]
fn full_region_membership_and_handler_boundaries_are_not_erased() {
    let mut source = body();
    for line in [6, 7] {
        source.blocks.alloc(BasicBlock {
            stmts: vec![Statement::Line(line)],
            terminator: Terminator::Return(None),
        });
    }
    source.regions = vec![region(1, 4, 6), region(1, 2, 6)];
    // bb2不是边界，bb2/bb4同在外层窗，但内层成员不同，不能消去bb2→bb4。
    source.blocks[id(0)].terminator = Terminator::Goto(id(2));
    let edges = Edges::capture(&source).unwrap();
    assert!(!same_regions(&source, id(2), id(4)));
    source.blocks[id(0)].terminator = Terminator::Goto(id(4));
    assert!(!matches(&edges, &source, 0));

    let mut source = body();
    source.regions = vec![region(3, 2, 1)];
    // exclusive-end==len合法；不构造bb5，空handler bb1仍固定为真实入口。
    let edges = Edges::capture(&source).unwrap();
    assert!(!edges.anchors.contains(&id(5)));
    source.blocks[id(0)].terminator = Terminator::Branch {
        cond: Operand::Local(LocalId::from_raw(0.into())),
        then_: id(3),
        else_: id(2),
    };
    assert!(!matches(&edges, &source, 0));

    let mut source = body();
    source.regions = vec![region(3, 1, 0)];
    source.blocks[id(3)].stmts.clear();
    source.blocks[id(3)].terminator = Terminator::Goto(id(2));
    source.blocks[id(2)].terminator = Terminator::Goto(id(4));
    // 两端bb1/bb2都在窗外，但中途进入再退出[3,4)，不能仅比较链端点。
    assert!(same_regions(&source, id(1), id(2)));
    assert!(!same_regions(&source, id(1), id(3)));
    let edges = Edges::capture(&source).unwrap();
    source.blocks[id(0)].terminator = Terminator::Branch {
        cond: Operand::Local(LocalId::from_raw(0.into())),
        then_: id(2),
        else_: id(2),
    };
    assert!(!matches(&edges, &source, 0));
}

#[test]
fn original_and_final_empty_cycles_are_rejected_even_after_an_anchor() {
    for two_blocks in [false, true] {
        let mut bad = body();
        bad.blocks[id(1)].terminator = Terminator::Goto(if two_blocks { id(2) } else { id(1) });
        bad.blocks[id(2)].terminator = Terminator::Goto(id(1));
        assert!(Edges::capture(&bad).is_err());
        // handler把bb1变成固定锚，但不能掩盖其后实际空环。
        bad.regions = vec![region(3, 1, 1)];
        assert!(Edges::capture(&bad).is_err());
        let mut source = body();
        source.regions = bad.regions.clone();
        let edges = Edges::capture(&source).unwrap();
        assert!(check(&bad).is_err());
        assert!(edges.normalize(&bad, &bad.blocks[id(0)].terminator).is_err());
    }
    let mut nonempty_loop = body();
    nonempty_loop.blocks[id(3)].terminator = Terminator::Goto(id(3));
    assert!(Edges::capture(&nonempty_loop).is_ok());
}

#[test]
fn invalid_targets_regions_and_exclusive_end_are_errors_not_panics() {
    for mutation in 0..6 {
        let mut bad = body();
        match mutation {
            0 => bad.entry = id(99),
            1 => bad.blocks[id(1)].terminator = Terminator::Goto(id(99)),
            2 => bad.regions = vec![region(1, 1, 99)],
            3 => bad.regions = vec![region(99, 1, 0)],
            4 => bad.regions = vec![region(1, usize::MAX, 0)],
            _ => bad.regions = vec![region(1, 0, 0)],
        }
        assert!(check(&bad).is_err(), "mutation {mutation}");
        assert!(Edges::capture(&bad).is_err(), "mutation {mutation}");
    }
    let mut bad = body();
    bad.blocks[id(0)].terminator = Terminator::Branch {
        cond: Operand::Const(Constant::Bool(true)),
        then_: id(3),
        else_: id(99),
    };
    assert!(check(&bad).is_err(), "未选中的原始目标也必须合法");
}

#[test]
fn folding_then_bypassing_an_original_empty_branch_remains_rejected() {
    let mut source = body();
    source.blocks[id(0)].terminator = Terminator::Goto(id(1));
    source.blocks[id(1)].terminator = Terminator::Branch {
        cond: Operand::Const(Constant::Bool(true)),
        then_: id(3),
        else_: id(4),
    };
    let edges = Edges::capture(&source).unwrap();
    source.blocks[id(0)].terminator = Terminator::Goto(id(3));
    source.blocks[id(1)].terminator = Terminator::Unreachable;
    // 本轮只准消去原纯Goto桥；不把所有SimplifyCfg组合都宣称已认证。
    assert!(!matches(&edges, &source, 0));
}
