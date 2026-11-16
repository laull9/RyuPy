//! 批量sum恢复入口的关闭态准入原型：只证明无用户回调的标量生成器，不推测消费者名字。
//! 当前仅随单元测试编译，不参与产品lowering；显式Suspend证据供后续独审后的发射使用。
use crate::mir::*;
use rustc_hash::FxHashSet;

#[derive(Debug, PartialEq)]
enum Refusal {
    StateBoundary,
    ObservableOperation,
    MissingYield,
    AmbiguousResume,
    LivenessBudget,
}

#[derive(Debug)]
struct YieldEdge {
    block: BlockId,
    resume: BlockId,
    value: Operand,
    input: Option<LocalId>,
}

#[derive(Debug)]
struct Plan {
    yields: Vec<YieldEdge>,
    residents: FxHashSet<LocalId>,
}

fn analyze(function: &Function) -> Result<Plan, Refusal> {
    if !crate::generator_state_analysis::eligible(function) {
        return Err(Refusal::StateBoundary);
    }
    let mut yields = Vec::new();
    let mut resumes = FxHashSet::default();
    for (block, body) in function.body.blocks.iter() {
        for statement in &body.stmts {
            let safe = match statement {
                Statement::Line(_) => true,
                Statement::Assign { value, .. } | Statement::Eval(value) => pure(function, value),
                _ => false,
            };
            if !safe {
                return Err(Refusal::ObservableOperation);
            }
        }
        match &body.terminator {
            Terminator::Suspend {
                value,
                resume,
                input,
                delegated: false,
            } if operand(value) => {
                if !resumes.insert(*resume) {
                    return Err(Refusal::AmbiguousResume);
                }
                yields.push(YieldEdge {
                    block,
                    resume: *resume,
                    value: value.clone(),
                    input: *input,
                });
            }
            Terminator::Goto(_) | Terminator::Return(None) => {}
            Terminator::Return(Some(value)) if operand(value) => {}
            Terminator::Branch { cond, .. } if scalar(function, cond) => {}
            _ => return Err(Refusal::ObservableOperation),
        }
    }
    if yields.is_empty() {
        return Err(Refusal::MissingYield);
    }
    let residents =
        crate::generator_state_analysis::residents(function).ok_or(Refusal::LivenessBudget)?;
    Ok(Plan { yields, residents })
}

// 全局/数组/未知对象读取均不是局部标量证据；拒绝而不是通过名字或类型提示补推断。
fn operand(value: &Operand) -> bool {
    matches!(value, Operand::Local(_) | Operand::Const(_))
}

fn scalar(function: &Function, value: &Operand) -> bool {
    match value {
        Operand::Const(Constant::I64(_) | Constant::F64(_) | Constant::Bool(_)) => true,
        Operand::Local(id) => matches!(
            function.body.locals[*id].ty,
            MirType::I64 | MirType::F64 | MirType::Bool
        ),
        _ => false,
    }
}

fn pure(function: &Function, value: &Rvalue) -> bool {
    match value {
        Rvalue::Use(value) => operand(value),
        Rvalue::Box { value, from } => {
            matches!(
                from,
                MirType::I64 | MirType::F64 | MirType::Bool | MirType::Str
            ) && operand(value)
        }
        Rvalue::Binary { lhs, rhs, .. } => scalar(function, lhs) && scalar(function, rhs),
        Rvalue::Unary { operand, .. } => scalar(function, operand),
        // 普通字段lowering会删除这些trace入口；它们不成为批量内部的用户回调。
        Rvalue::Call {
            callee: Callee::Runtime(rt),
            args,
        } => {
            matches!(
                rt,
                RuntimeFn::DynNone
                    | RuntimeFn::TracePush
                    | RuntimeFn::TracePushWithOrigin
                    | RuntimeFn::TracePopTo
            ) && args.iter().all(operand)
        }
        // 特别拒绝DynBinary/DynCompare/DynTruthy、通用迭代和Unbox；它们可能调用用户协议。
        // 尚无精确range来源证明，因此不能凭yield的表面算术形状开放原genexpr基准。
        _ => false,
    }
}

fn high(source: &str) -> (Program, Vec<FnId>) {
    let (program, plan) = high_with_sources(source);
    (program, plan.functions().to_vec())
}

pub(super) fn high_with_sources(
    source: &str,
) -> (Program, crate::generator_product::DeferredGenerators) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("sum-plan.py", source);
    let (ast, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(&ast, &Default::default(), source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let mut deferred = crate::generator_product::DeferredGenerators::default();
    let (program, _, errors) =
        crate::build::build_program_mode(&hir, &types, None, Some(&mut deferred), false);
    assert!(errors.is_empty(), "{errors:?}");
    (program, deferred)
}

#[test]
fn scalar_yields_keep_explicit_edges_and_residents() {
    let (program, generators) = high("def g():\n    sent = yield 1\n    yield sent\n");
    let function = &program.functions[generators[0]];
    let plan = analyze(function).unwrap();
    assert_eq!(plan.yields.len(), 2);
    for edge in &plan.yields {
        assert_eq!(
            function.body.blocks[edge.block].terminator,
            Terminator::Suspend {
                value: edge.value.clone(),
                resume: edge.resume,
                input: edge.input,
                delegated: false,
            }
        );
    }
    assert!(
        function
            .body
            .locals
            .iter()
            .filter(|(_, l)| l.name.is_some())
            .all(|(id, _)| plan.residents.contains(&id))
    );
}

#[test]
fn unknown_calls_and_dynamic_operators_are_not_pure() {
    for source in [
        "def g(x):\n    yield x + 1\n",
        "def g(f):\n    yield f()\n",
        "def g(x):\n    yield from x\n",
        "def g():\n    try:\n        yield 1\n    finally:\n        pass\n",
    ] {
        let (program, generators) = high(source);
        assert!(analyze(&program.functions[generators[0]]).is_err());
    }
}

#[test]
fn original_workload_is_not_claimed_without_range_and_dynamic_proofs() {
    let (program, generators) = high(include_str!("../../../tests/benchmarks/generators.py"));
    assert!(
        generators
            .iter()
            .all(|id| analyze(&program.functions[*id]).is_err())
    );
}

#[test]
fn original_three_sources_record_distinct_blocking_instructions() {
    let (program, generators) = high(include_str!("../../../tests/benchmarks/generators.py"));
    let function = |name: &str| {
        generators
            .iter()
            .map(|id| &program.functions[*id])
            .find(|f| f.name == name)
            .unwrap()
    };
    let has = |f: &Function, check: fn(&Rvalue) -> bool| {
        f.body.blocks.iter().any(|(_, b)| {
            b.stmts.iter().any(|s| match s {
                Statement::Assign { value, .. } | Statement::Eval(value) => check(value),
                _ => false,
            })
        })
    };
    let count = function("count_up");
    assert!(has(count, |v| matches!(
        v,
        Rvalue::DynBinary {
            op: DynOp::Add,
            inplace: true,
            ..
        }
    )));
    for runtime in [RuntimeFn::DynRichCompareValue, RuntimeFn::DynTruthy] {
        assert!(count.body.blocks.iter().any(|(_, b)| b.stmts.iter().any(|s|
            matches!(s, Statement::Assign { value: Rvalue::Call { callee: Callee::Runtime(rt), .. }, .. } if *rt == runtime))));
    }
    let chained = function("chained");
    assert!(chained.body.blocks.iter().any(|(_, b)| matches!(
        b.terminator,
        Terminator::Suspend {
            delegated: true,
            ..
        }
    )));
    assert!(has(chained, |v| matches!(
        v,
        Rvalue::GeneratorDelegateStep { .. }
    )));
    let expression = function("<genexpr>");
    assert!(has(expression, |v| matches!(
        v,
        Rvalue::DynBinary { op: DynOp::Mul, .. }
    )));
    for runtime in [
        RuntimeFn::IterStart,
        RuntimeFn::IterNextOk,
        RuntimeFn::IterValue,
    ] {
        assert!(expression.body.blocks.iter().any(|(_, b)| b.stmts.iter().any(|s|
            matches!(s, Statement::Assign { value: Rvalue::Call { callee: Callee::Runtime(rt), .. }, .. } if *rt == runtime))));
    }
}
