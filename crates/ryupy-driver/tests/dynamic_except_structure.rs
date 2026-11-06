//! 单一结构门：直接检查真实MIR的保护窗口、支配/退出路径与绑定/token身份。
//! 不将普通CFG证明冒充异常运行证明；属性错误/context/控制流由四应用及C#补证。
use ryupy_mir::{
    BinOp, BlockId, Callee, Constant, Function, LocalId, MirType, Operand, Program, Region,
    RegionKind, RuntimeFn, Rvalue, Statement, Terminator,
};
use std::collections::HashSet;

fn index(id: BlockId) -> usize {
    u32::from(id.into_raw()) as usize
}
fn inside(region: &Region, block: BlockId) -> bool {
    (index(region.try_start)..index(region.try_start) + region.try_len).contains(&index(block))
}
fn value(stmt: &Statement) -> Option<&Rvalue> {
    match stmt {
        Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
        _ => None,
    }
}
fn calls(
    function: &Function,
    wanted: RuntimeFn,
) -> Vec<(BlockId, usize, Option<LocalId>, &[Operand])> {
    let mut found = Vec::new();
    for (id, block) in function.body.blocks.iter() {
        for (offset, stmt) in block.stmts.iter().enumerate() {
            if let Some(Rvalue::Call {
                callee: Callee::Runtime(callee),
                args,
            }) = value(stmt)
            {
                if *callee == wanted {
                    let dest = match stmt {
                        Statement::Assign { dest, .. } => Some(*dest),
                        _ => None,
                    };
                    found.push((id, offset, dest, args.as_slice()));
                }
            }
        }
    }
    found
}
fn successors(function: &Function, id: BlockId) -> Vec<BlockId> {
    match function.body.blocks[id].terminator {
        Terminator::Goto(next) => vec![next],
        Terminator::Branch { then_, else_, .. } => vec![then_, else_],
        _ => vec![],
    }
}
fn reachable(function: &Function, start: BlockId, skip: Option<BlockId>) -> HashSet<BlockId> {
    let mut seen = HashSet::new();
    let mut todo = vec![start];
    while let Some(id) = todo.pop() {
        if Some(id) == skip || !seen.insert(id) {
            continue;
        }
        todo.extend(successors(function, id));
    }
    seen
}
fn one_function<'a>(program: &'a Program, name: &str) -> &'a Function {
    let mut found = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .filter(|f| f.name == name);
    let function = found.next().unwrap();
    assert!(found.next().is_none());
    function
}
fn local(operand: &Operand) -> LocalId {
    match operand {
        Operand::Local(id) => *id,
        _ => panic!("须保真实local身份"),
    }
}
fn checked_local(function: &Function, name: &str) {
    let ids: Vec<_> = function
        .body
        .locals
        .iter()
        .filter_map(|(id, decl)| (decl.name.as_deref() == Some(name)).then_some(id))
        .collect();
    assert_eq!(ids.len(), 1, "须找到唯一真实局部槽{name}");
    assert!(
        calls(function, RuntimeFn::LoadChecked)
            .iter()
            .any(|call| matches!(call.3.first(), Some(Operand::Local(id)) if *id == ids[0])),
        "{name}的潜在Unbound读取必须检查实际绑定槽"
    );
}
fn check_dynamic(function: &Function) {
    let catches: Vec<_> = function
        .body
        .regions
        .iter()
        .filter(|r| matches!(r.kind, RegionKind::CatchAll))
        .collect();
    let guards: Vec<_> = function
        .body
        .regions
        .iter()
        .filter(|r| matches!(r.kind, RegionKind::FinallyGuard))
        .collect();
    assert_eq!(catches.len(), 1);
    assert_eq!(guards.len(), 1);
    let (catch, guard) = (catches[0], guards[0]);
    assert_eq!(
        function.body.regions.len(),
        2,
        "同级typed catch不可绕过动态头"
    );
    let original = catch.exc_local.unwrap();
    let escaped = guard.exc_local.unwrap();
    let enters = calls(function, RuntimeFn::HandlerEnter);
    let leaves = calls(function, RuntimeFn::HandlerLeave);
    let matches = calls(function, RuntimeFn::ExceptionMatch);
    let reads = calls(function, RuntimeFn::DynGetAttr);
    assert_eq!(enters.len(), 1);
    assert_eq!(leaves.len(), 1);
    assert_eq!(matches.len(), 2);
    assert_eq!(reads.len(), 1);
    let enter = enters[0];
    let leave = leaves[0];
    assert_eq!(local(&enter.3[0]), original);
    assert_eq!(local(&leave.3[0]), enter.2.unwrap());
    assert!(!inside(guard, enter.0) && !inside(guard, leave.0));
    assert!(!inside(catch, enter.0) && !inside(catch, guard.try_start));
    for call in matches.iter().chain(reads.iter()) {
        assert!(inside(guard, call.0));
        assert!(!inside(catch, call.0));
        assert!(reachable(function, catch.handler, None).contains(&call.0));
        assert!(
            !reachable(function, catch.handler, Some(enter.0)).contains(&call.0),
            "Enter必须支配目标求值/匹配"
        );
    }
    for call in &matches {
        assert_eq!(local(&call.3[0]), original);
    }
    let first = matches[0];
    let second = matches[1];
    for (call, header) in [(first, 14), (second, 16)] {
        let before = &function.body.blocks[call.0].stmts[..call.1];
        assert_eq!(
            before.iter().rev().find_map(|stmt| match stmt {
                Statement::Line(line) => Some(*line),
                _ => None,
            }),
            Some(header),
            "matcher现场必须是本except头"
        );
    }
    assert!(
        function.body.blocks[reads[0].0].stmts[..reads[0].1]
            .iter()
            .any(|stmt| matches!(stmt, Statement::Line(14))),
        "属性读取前就要写except头现场"
    );
    let (first_yes, first_no) = match function.body.blocks[first.0].terminator {
        Terminator::Branch { then_, else_, .. } => (then_, else_),
        _ => panic!("matcher须branch"),
    };
    assert!(reachable(function, first_no, None).contains(&second.0));
    assert!(
        !reachable(function, first_yes, None).contains(&second.0),
        "命中不可继续读后handler"
    );
    assert!(
        reads[0].0 == first.0 && reads[0].1 < first.1,
        "本例属性read须先于matcher"
    );
    // guard处理入口在窗口外，不得把新错误送回同级分派。
    assert!(!inside(guard, guard.handler));
    let guard_path = reachable(function, guard.handler, Some(leave.0));
    assert!(!guard_path.contains(&guard.try_start));
    let prepares = calls(function, RuntimeFn::RaisePrepare);
    let guard_prepare: Vec<_> = prepares
        .iter()
        .filter(|call| guard_path.contains(&call.0))
        .collect();
    assert_eq!(guard_prepare.len(), 1);
    assert_eq!(local(&guard_prepare[0].3[0]), escaped);
    let restores = calls(function, RuntimeFn::TracePopTo);
    assert!(
        restores
            .iter()
            .any(|call| call.0 == guard_prepare[0].0 && call.1 > guard_prepare[0].1),
        "新错Prepare先于恢复帧"
    );
    assert!(
        restores
            .iter()
            .any(|call| call.0 == enter.0 && call.1 < enter.1),
        "入口恢复帧先于Enter"
    );
    let entry_prepare: Vec<_> = prepares.iter().filter(|call| call.0 == enter.0).collect();
    assert_eq!(entry_prepare.len(), 1);
    assert_eq!(local(&entry_prepare[0].3[0]), original);
    assert_eq!(entry_prepare[0].2, Some(original));
    assert!(
        restores
            .iter()
            .any(|call| call.0 == enter.0 && entry_prepare[0].1 < call.1 && call.1 < enter.1),
        "原对象Prepare→恢复帧→Enter顺序"
    );
    // 此有限无循环fixture中，每个dispatch/guard的普通出口都必须经唯一Leave；
    // EH边由窗口覆盖证明，不能把无异常边的普通dominance当成EH证明。
    for start in [guard.try_start, guard.handler] {
        let path = reachable(function, start, Some(leave.0));
        assert!(reachable(function, start, None).contains(&leave.0));
        for id in path {
            assert!(
                !matches!(
                    function.body.blocks[id].terminator,
                    Terminator::Return(_) | Terminator::Raise { .. }
                ),
                "清理前不得直接返回/抛出"
            );
        }
    }
    let mut no_match = Vec::new();
    for (id, block) in function.body.blocks.iter() {
        if block.stmts.iter().any(|stmt| {
            matches!(stmt,
            Statement::Assign { dest, value: Rvalue::Use(Operand::Local(source)) }
            if *dest == escaped && *source == original)
        }) {
            no_match.push(id);
        }
    }
    assert_eq!(no_match.len(), 1);
    let no_match = no_match[0];
    let after_no_match = reachable(function, no_match, None);
    assert!(
        prepares
            .iter()
            .all(|call| !after_no_match.contains(&call.0)),
        "未匹配不得额外Prepare"
    );
    let reraises: Vec<_> = after_no_match
        .iter()
        .filter(|id| {
            matches!(function.body.blocks[**id].terminator,
        Terminator::Raise { value: Some(Operand::Local(id)), cause: None } if id == escaped)
        })
        .collect();
    assert_eq!(reraises.len(), 1);
    // as名字清理存在且位于保护域外、Leave之前；索引数据流进一步明确锁住。
    let unbounds = calls(function, RuntimeFn::MakeUnbound);
    assert_eq!(unbounds.len(), 1);
    assert!(!inside(guard, unbounds[0].0));
    assert!(reachable(function, unbounds[0].0, None).contains(&leave.0));
    let selected: Vec<_> = function.body.blocks[enter.0]
        .stmts
        .iter()
        .filter_map(|stmt| match stmt {
            Statement::Assign {
                dest,
                value: Rvalue::Use(Operand::Const(Constant::I64(-1))),
            } => Some(*dest),
            _ => None,
        })
        .collect();
    assert_eq!(selected.len(), 1);
    let selected = selected[0];
    let mut writes = Vec::new();
    for (id, block) in function.body.blocks.iter() {
        for (position, stmt) in block.stmts.iter().enumerate() {
            if let Statement::Assign { dest, value } = stmt {
                if *dest == selected {
                    writes.push((id, position, value));
                }
            }
        }
    }
    assert_eq!(writes.len(), 2, "仅初始化和实际as命中可写selected");
    let assignment = writes
        .iter()
        .find(|(_, _, value)| matches!(value, Rvalue::Use(Operand::Const(Constant::I64(0)))))
        .unwrap();
    assert!(reachable(function, first_yes, None).contains(&assignment.0));
    assert!(!reachable(function, first_no, None).contains(&assignment.0));
    let binding = function.body.blocks[assignment.0].stmts[..assignment.1]
        .iter()
        .find_map(|stmt| match stmt {
            Statement::Assign {
                dest,
                value: Rvalue::Use(Operand::Local(id)),
            } if *id == original => Some(*dest),
            _ => None,
        })
        .expect("先绑定原对象再登记selected");
    assert!(
        function.body.blocks[unbounds[0].0]
            .stmts
            .iter()
            .any(|stmt| matches!(stmt,
        Statement::Assign { dest, value: Rvalue::Use(Operand::Local(id)) }
        if *dest == binding && *id == unbounds[0].2.unwrap())),
        "删除同一个as槽"
    );
    let mut selectors = Vec::new();
    for (id, block) in function.body.blocks.iter() {
        for stmt in &block.stmts {
            if let Statement::Assign {
                dest,
                value:
                    Rvalue::Binary {
                        op: BinOp::Eq,
                        lhs: Operand::Local(lhs),
                        rhs: Operand::Const(Constant::I64(0)),
                    },
            } = stmt
            {
                if *lhs == selected {
                    selectors.push((id, *dest));
                }
            }
        }
    }
    assert_eq!(selectors.len(), 1);
    match function.body.blocks[selectors[0].0].terminator {
        Terminator::Branch {
            cond: Operand::Local(cond),
            then_,
            else_,
        } => {
            assert_eq!(cond, selectors[0].1);
            assert!(reachable(function, then_, Some(leave.0)).contains(&unbounds[0].0));
            assert!(!reachable(function, else_, Some(leave.0)).contains(&unbounds[0].0));
        }
        _ => panic!("as删除必须受selected索引控制"),
    }
}

#[test]
fn bounded_dynamic_except_regions_and_identity_flow() {
    const SOURCE: &str = include_str!("../../../tests/fixtures/dynamic_except_structure/source.py");
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-dynamic-except-structure-{}-{stamp}",
        std::process::id()
    ));
    std::fs::create_dir(&root).unwrap();
    eprintln!("动态except结构证据: {}", root.display());
    std::fs::write(root.join("source.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("dynamic_except_structure.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        name: "__main__".into(),
        file: "dynamic_except_structure.py".into(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    std::fs::write(root.join("mir.diagnostics"), format!("{diagnostics:?}")).unwrap();
    std::fs::write(root.join("source.mir"), ryupy_mir::pretty::dump(&program)).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    check_dynamic(one_function(&program, "mixed"));
    checked_local(one_function(&program, "late_target"), "missing");
    checked_local(one_function(&program, "cleanup_read"), "caught");
    checked_local(one_function(&program, "body_target"), "target");
    checked_local(one_function(&program, "handler_body"), "body_local");
    let numeric = one_function(&program, "numeric");
    assert_eq!(numeric.ret, MirType::I64);
    assert_eq!(numeric.params.len(), 2);
    assert!(
        numeric
            .params
            .iter()
            .all(|id| numeric.body.locals[*id].ty == MirType::I64)
    );
    assert!(numeric.body.regions.is_empty());
    for name in ["numeric", "typed"] {
        let function = one_function(&program, name);
        assert!(calls(function, RuntimeFn::ExceptionMatch).is_empty());
        assert!(calls(function, RuntimeFn::DynGetAttr).is_empty());
    }
    let typed = one_function(&program, "typed");
    assert_eq!(typed.ret, MirType::I64);
    assert_eq!(
        typed.body.regions.len(),
        2,
        "原typed catch加其handler清理guard"
    );
    assert_eq!(typed.body.regions.iter().filter(|r| matches!(&r.kind, RegionKind::Catch { type_names } if type_names == &["ZeroDivisionError"])).count(), 1);
    assert_eq!(
        typed
            .body
            .regions
            .iter()
            .filter(|r| matches!(r.kind, RegionKind::FinallyGuard))
            .count(),
        1
    );
    assert!(
        !typed
            .body
            .regions
            .iter()
            .any(|r| matches!(r.kind, RegionKind::CatchAll))
    );
    for (name, source, supported) in [
        (
            "ni_generator",
            include_str!("../../../tests/fixtures/dynamic_except_structure/ni_generator.py"),
            false,
        ),
        (
            "ni_call",
            include_str!("../../../tests/fixtures/dynamic_except_structure/ni_call.py"),
            false,
        ),
        (
            "nested_static_generator",
            include_str!(
                "../../../tests/fixtures/dynamic_except_structure/nested_static_generator.py"
            ),
            true,
        ),
    ] {
        let input = root.join(format!("{name}.py"));
        std::fs::write(&input, source).unwrap();
        let mut session = ryupy_driver::Session::default();
        let result = ryupy_driver::dump(&mut session, &input, ryupy_driver::Stage::Mir);
        std::fs::write(
            root.join(format!("{name}.diagnostics")),
            format!("{:?}", session.diagnostics),
        )
        .unwrap();
        if supported {
            let mir = result.unwrap();
            assert!(!session.diagnostics.has_errors());
            std::fs::write(root.join(format!("{name}.mir")), mir).unwrap();
        } else {
            assert!(result.is_err() && session.diagnostics.has_errors());
            assert!(session.diagnostics.iter().any(|d| d.code == Some("E0100")));
        }
    }
}
