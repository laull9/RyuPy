//! 从真实源码构建MIR，记录records融合仍必须处理的方法捕获、具名key和动态加法边界。
//! 本模块不实现宽松matcher，不把方法名get或单个MakeTuple当成产品准入证书。
use crate::mir::*;

fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("records-contract.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (program, _, errors) = crate::build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    crate::validate::validate(&program).expect("源码MIR必须合法");
    program
}

fn function<'a>(program: &'a Program, name: &str) -> &'a Function {
    program
        .functions
        .iter()
        .find(|(_, f)| f.name == name)
        .unwrap()
        .1
}

fn values(function: &Function) -> Vec<&Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|statement| match statement {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
        .collect()
}

fn has_increment_spelling(f: &Function) {
    let values = values(f);
    assert!(values.iter().any(|value| matches!(value,
        Rvalue::CallWithArgs { callee: RuntimeFn::MakeTuple, args, .. } if args.len() == 2)));
    // 捕获标志存在不等于精确dict证明；模块方法和用户对象也走同一种显式MIR。
    assert!(values.iter().any(|value| matches!(value,
        Rvalue::DynCallMethod { name, captured: Some(_), args, .. } if name == "get" && args.len() == 2)));
    assert!(values.iter().any(|value| matches!(
        value,
        Rvalue::DynBinary {
            op: DynOp::Add,
            inplace: false,
            ..
        }
    )));
    assert!(values.iter().any(|value| matches!(value,
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::SetItem), args } if args.len() == 3)));
    assert!(
        f.body
            .locals
            .iter()
            .any(|(_, local)| local.name.as_deref() == Some("key"))
    );
}

#[test]
fn original_records_retains_named_tuple_dynamic_get_add_and_write() {
    let program = compile(include_str!("../../../tests/benchmarks/records.py"));
    has_increment_spelling(function(&program, "summarize"));
}

#[test]
fn observable_key_and_generic_receiver_still_have_the_same_candidate_spelling() {
    let program = compile(
        "def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    return key\n",
    );
    let f = function(&program, "step");
    has_increment_spelling(f);
    assert!(
        f.body
            .blocks
            .iter()
            .any(|(_, block)| matches!(block.terminator, Terminator::Return(Some(_))))
    );
    // 函数参数counts可能是用户dict派生；语法相同不能消掉get/__setitem__协议。
    // 物理params可能另含module_state/owner；Python签名不能按ABI槽数推断。
    let names: Vec<_> = f.param_meta.iter().map(|p| p.name.as_str()).collect();
    assert_eq!(names, ["counts", "user", "action"]);
}

#[test]
fn local_observation_does_not_authorize_discarding_the_key() {
    let program = compile(
        "def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    visible = locals()\n    return visible['key'] is key\n",
    );
    has_increment_spelling(function(&program, "step"));
}

#[test]
fn records_wire_diagnostic_preserves_actual_before_and_after_mir() {
    let fixtures = [
        (
            "records",
            include_str!("../../../tests/benchmarks/records.py"),
            "summarize",
        ),
        (
            "visible-key",
            "def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    return key\n",
            "step",
        ),
        (
            "callback-addend",
            "def step(counts, key, change):\n    counts[key] = counts.get(key, 0) + change()\n    return counts\n",
            "step",
        ),
        (
            "observed-locals",
            "def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    return locals()\n",
            "step",
        ),
    ];
    for (label, source, name) in fixtures {
        let mut program = compile(source);
        println!("records-wire begin {label} before");
        println!("{:#?}", function(&program, name));
        println!("records-wire end {label} before");
        crate::passes::run_pipeline(&mut program, &crate::passes::default_pipeline())
            .expect("既有默认流水线仍须验证合法");
        println!("records-wire begin {label} after");
        println!("{:#?}", function(&program, name));
        println!("records-wire end {label} after");
    }
}

#[test]
fn records_wire_plan_matches_only_closed_capture_window() {
    let mut program = compile(include_str!("../../../tests/benchmarks/records.py"));
    crate::passes::run_pipeline(&mut program, &crate::passes::default_pipeline()).unwrap();
    let f = function(&program, "summarize");
    let plans = crate::records_increment_plan::plans(f);
    assert_eq!(plans.len(), 1);
    let plan = &plans[0];
    assert_eq!(f.body.locals[plan.receiver].name.as_deref(), Some("counts"));
    assert_eq!(f.body.locals[plan.key].name.as_deref(), Some("key"));
    println!("records-wire plan-only {plan:?}; no rewrite");
    for (source, expected) in [
        ("def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    return key\n", 1),
        ("def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    return locals()\n", 0),
        ("def step(counts, key, change):\n    counts[key] = counts.get(key, 0) + change()\n", 0),
        ("def step(counts, key):\n    counts[key] = counts.get(key, 1) + 1\n", 0),
        ("def step(counts, key):\n    counts[key] = counts.get(key, 0) + 2\n", 0),
        ("def step(counts, key):\n    try:\n        counts[key] = counts.get(key, 0) + 1\n    except Exception:\n        pass\n", 0),
    ] {
        let program = compile(source);
        assert_eq!(crate::records_increment_plan::plans(function(&program, "step")).len(), expected, "{source}");
    }
}

#[test]
fn records_wire_plan_rejects_overwrite_escape_observer_and_wrong_target() {
    for mode in 0..6 {
        let mut program = compile("def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    return key\n");
        let (_, f) = program.functions.iter_mut().find(|(_, f)| f.name == "step").unwrap();
        let plans = crate::records_increment_plan::plans(f);
        assert_eq!(plans.len(), 1);
        let p = &plans[0];
        let Statement::Assign { dest: target, .. } = f.body.blocks[p.block].stmts[p.start - 2] else { panic!("capture assignment"); };
        let Statement::Assign { dest: updated, .. } = f.body.blocks[p.block].stmts[p.start + 2] else { panic!("add assignment"); };
        match mode {
            0 => f.body.blocks[p.block].stmts.insert(p.start, Statement::Assign { dest: target, value: Rvalue::Use(Operand::Local(p.receiver)) }),
            1 => f.body.blocks[p.block].stmts.push(Statement::Eval(Rvalue::Use(Operand::Local(updated)))),
            2 => f.body.blocks[p.block].stmts.insert(p.start + 1, Statement::Line(99)),
            3 => {
                let Statement::Eval(Rvalue::Call { args, .. }) = &mut f.body.blocks[p.block].stmts[p.start + 3] else { panic!("set call"); };
                args[0] = Operand::Local(p.key);
            }
            4 => f.body.locals[p.key].is_cell = true,
            5 => f.body.blocks[p.block].terminator = Terminator::Return(Some(Operand::Local(target))),
            _ => unreachable!(),
        }
        assert!(crate::records_increment_plan::plans(f).is_empty(), "mode {mode}");
    }
}
