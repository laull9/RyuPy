//! test-only拥有式准备：同次构建的Program/来源/计划不可拆开传入，验证真实普通字段lowering。
//! 不发射批量sum，不给调用方可变Program，link或任何重写必须重新走准备入口。
use crate::generator_product::DeferredGenerators;
use crate::generator_sum_fields::Snapshot;
use crate::generator_sum_range_plan::{RangePlan, analyze_source};
use crate::mir::*;

struct PreparedSum {
    program: Program,
    deferred: DeferredGenerators,
    plans: Vec<(FnId, RangePlan)>,
}

struct VerifiedSumLowering {
    program: Program,
    verified: usize,
}

impl PreparedSum {
    // 唯一入口从同一源构建全部材料，没有from_parts、外来ID或外部deferred参数。
    fn build(source: &str) -> Self {
        let (program, deferred) = super::generator_sum_plan::high_with_sources(source);
        let plans = deferred.comprehension_inputs.iter().filter_map(|(function, input)| {
            analyze_source(&program, &deferred, *function, *input).map(|plan| (*function, plan))
        }).collect();
        Self { program, deferred, plans }
    }

    fn finish(self) -> Result<VerifiedSumLowering, &'static str> {
        let (program, snapshots) = self.lower_owned()?;
        for snapshot in &snapshots { snapshot.verify(&program)?; }
        crate::validate::validate(&program).map_err(|_| "降级后MIR验证失败")?;
        Ok(VerifiedSumLowering { program, verified: snapshots.len() })
    }

    // 私有实现只由finish调用；同模块负例可变异降级结果，不开放给候选消费者。
    fn lower_owned(self) -> Result<(Program, Vec<Snapshot>), &'static str> {
        let Self { mut program, deferred, plans } = self;
        let snapshots: Vec<_> = plans.into_iter().map(|(function, plan)| {
            Snapshot::capture(&program, function, plan.residents)
        }).collect();
        let mut fallback = Vec::new();
        for &id in deferred.functions() {
            if !crate::generator_state::lower(&mut program, id) { fallback.push(id); }
        }
        if snapshots.iter().any(|source| fallback.contains(&source.function)) {
            return Err("已准入源没有生成字段状态机");
        }
        crate::generators::lower(&mut program, &fallback, &deferred.exceptions);
        Ok((program, snapshots))
    }
}

fn original() -> PreparedSum {
    PreparedSum::build(include_str!("../../../tests/benchmarks/generators.py"))
}

fn resume(program: &Program, function: FnId) -> FnId {
    program.functions[function].body.blocks.iter().flat_map(|(_, b)| &b.stmts).find_map(|s| {
        if let Statement::Assign { value: Rvalue::MakeStateGenerator { resume, .. }, .. } = s { Some(*resume) } else { None }
    }).unwrap()
}

#[test]
fn owned_prepare_checks_actual_lowering_and_keeps_other_generators() {
    let verified = original().finish().unwrap();
    assert_eq!(verified.verified, 1);
    crate::validate::validate(&verified.program).unwrap();
    // 每次独立持有完整构建；即使数值ID相同，也没有拼接两份Program/来源的入口。
    let second = original().finish().unwrap();
    assert_eq!(second.verified, 1);
}

#[test]
fn mapping_does_not_depend_on_field_names_or_layout_order() {
    let (mut program, snapshots) = original().lower_owned().unwrap();
    let source = &snapshots[0];
    let id = resume(&program, source.function);
    // 名字彻底替换，关系仍由真实构造参数、读写操作证明。
    for (_, class) in program.classes.iter_mut() {
        if class.is_generator_state {
            for (index, (name, _)) in class.fields.iter_mut().enumerate() { *name = format!("renamed{index}"); }
        }
    }
    source.verify(&program).unwrap();
    // 完整一致地倒置字段、构造实参和所有读写号，也必须仍可证明。
    let class_id = program.functions[source.function].body.blocks.iter().flat_map(|(_, b)| &b.stmts).find_map(|s| {
        if let Statement::Assign { value: Rvalue::NewObject { class, .. }, .. } = s { Some(*class) } else { None }
    }).unwrap();
    let count = program.classes[class_id].fields.len() as u32;
    program.classes[class_id].fields.reverse();
    for (_, block) in program.functions[source.function].body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign { value: Rvalue::NewObject { args, .. }, .. } = statement { args.reverse(); }
        }
    }
    for (_, block) in program.functions[id].body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            match statement {
                Statement::Assign { value: Rvalue::GetField { field, .. }, .. } | Statement::SetField { field, .. } => *field = count - 1 - *field,
                _ => {}
            }
        }
    }
    source.verify(&program).unwrap();
}

#[test]
fn real_field_reads_writes_and_pc_mutations_are_rejected() {
    for mutation in ["missing-write", "late-write", "missing-read", "wrong-write", "wrong-read", "wrong-pc", "wrong-dispatch"] {
        let (mut program, snapshots) = original().lower_owned().unwrap();
        let source = &snapshots[0];
        let id = resume(&program, source.function);
        source.verify(&program).unwrap();
        let f = &mut program.functions[id];
        let mut changed = false;
        let entry = f.body.entry;
        let mut moved = None;
        for (_, block) in f.body.blocks.iter_mut() {
            if mutation == "missing-read" {
                if let Some(at) = block.stmts.iter().position(|s| matches!(s, Statement::Assign { value: Rvalue::GetField { .. }, .. })) {
                    block.stmts.remove(at); changed = true; break;
                }
            }
            if mutation == "missing-write" || mutation == "late-write" {
                if let Some(at) = block.stmts.iter().position(|s| matches!(s, Statement::SetField { .. })) {
                    moved = Some(block.stmts.remove(at)); changed = true; break;
                }
            }
            for statement in &mut block.stmts {
                match statement {
                    Statement::SetField { field, .. } if mutation == "wrong-write" => {
                        *field = u32::MAX; changed = true;
                    }
                    Statement::Assign { value: Rvalue::GetField { field, .. }, .. } if mutation == "wrong-read" => {
                        *field = u32::MAX; changed = true;
                    }
                    Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::GeneratorStateSetPC), args }) if mutation == "wrong-pc" => {
                        args[1] = Operand::Const(Constant::I64(999999)); changed = true;
                    }
                    Statement::Assign { value: Rvalue::Binary { op: BinOp::Eq, rhs: Operand::Const(Constant::I64(pc)), .. }, .. } if mutation == "wrong-dispatch" => {
                        *pc = 999999; changed = true;
                    }
                    _ => {}
                }
                if changed { break; }
            }
            if changed { break; }
        }
        if mutation == "late-write" { f.body.blocks[entry].stmts.push(moved.unwrap()); }
        assert!(changed);
        assert!(source.verify(&program).is_err(), "{mutation}");
    }
}


#[test]
fn factory_input_and_function_origin_are_checked_from_actual_operands() {
    for mutation in ["parameter", "origin"] {
        let (mut program, snapshots) = original().lower_owned().unwrap();
        let source = &snapshots[0];
        source.verify(&program).unwrap();
        let f = &mut program.functions[source.function];
        let mut changed = false;
        for (_, block) in f.body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                match statement {
                    Statement::Assign { value: Rvalue::NewObject { args, .. }, .. } if mutation == "parameter" => {
                        let value = args.iter_mut().find(|value| matches!(value, Operand::Local(_))).unwrap();
                        *value = Operand::Const(Constant::Null); changed = true;
                    }
                    Statement::Assign { value: Rvalue::MakeStateGenerator { origin, .. }, .. } if mutation == "origin" => {
                        *origin = Some(Operand::Const(Constant::Null)); changed = true;
                    }
                    _ => {}
                }
            }
        }
        assert!(changed);
        assert!(source.verify(&program).is_err());
    }
}

#[test]
fn inserted_dispatch_goto_must_not_hide_a_side_effect() {
    for with_effect in [false, true] {
        let (mut program, snapshots) = original().lower_owned().unwrap();
        let source = &snapshots[0];
        source.verify(&program).unwrap();
        let id = resume(&program, source.function);
        let f = &mut program.functions[id];
        let previous = f.body.blocks[f.body.entry].terminator.clone();
        let inserted = f.body.blocks.alloc(BasicBlock {
            stmts: if with_effect {
                vec![Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::DynBuiltin),
                    args: vec![Operand::Const(Constant::Str("print".to_owned())),
                        Operand::Array(vec![Operand::Const(Constant::Str("unexpected".to_owned()))]),
                        Operand::Array(vec![]), Operand::Array(vec![])] })]
            } else { vec![] },
            terminator: previous,
        });
        f.body.blocks[f.body.entry].terminator = Terminator::Goto(inserted);
        if with_effect { assert!(source.verify(&program).is_err()); }
        else { source.verify(&program).unwrap(); }
    }
}

#[test]
fn merging_two_field_read_targets_and_rewriting_the_operation_is_rejected() {
    let (mut program, deferred) = super::generator_sum_plan::high_with_sources(
        "def g(a, b):\n    yield a + b\n"
    );
    let function = deferred.functions()[0];
    let residents = crate::generator_state_analysis::residents(&program.functions[function]).unwrap();
    let source = Snapshot::capture(&program, function, residents);
    assert!(crate::generator_state::lower(&mut program, function));
    source.verify(&program).unwrap();
    let id = resume(&program, function);
    let f = &mut program.functions[id];
    let mut merged = false;
    for (_, block) in f.body.blocks.iter_mut() {
        let Some(operation) = block.stmts.iter().position(|statement| matches!(statement,
            Statement::Assign { value: Rvalue::DynBinary { .. }, .. })) else { continue; };
        assert!(operation >= 2);
        let first = match &block.stmts[operation - 2] {
            Statement::Assign { dest, value: Rvalue::GetField { .. } } => *dest,
            _ => panic!("双参数动态运算前必须读取第一个实际字段"),
        };
        let second = match &mut block.stmts[operation - 1] {
            Statement::Assign { dest, value: Rvalue::GetField { .. } } => {
                let previous = *dest; *dest = first; previous
            }
            _ => panic!("双参数动态运算前必须读取第二个实际字段"),
        };
        // 旧检查器的两个源local→同一个dest映射会同步期待这个错误运算，因而漏检。
        let map = rustc_hash::FxHashMap::from_iter([(second, first)]);
        crate::local_access::rewrite_statement(&mut block.stmts[operation], &map);
        merged = true;
        break;
    }
    assert!(merged);
    assert!(source.verify(&program).is_err());
}

#[test]
fn extra_uncovered_dispatch_block_is_not_silently_ignored() {
    let (mut program, snapshots) = original().lower_owned().unwrap();
    let source = &snapshots[0];
    source.verify(&program).unwrap();
    let id = resume(&program, source.function);
    program.functions[id].body.blocks.alloc(BasicBlock {
        stmts: vec![], terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    assert!(source.verify(&program).is_err());
}
