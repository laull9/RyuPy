//! 自动选择三态反例：复用真实前端/实际索引与原validator，不以产品输出构造oracle。
//! 这些是IR合同测试；真实无flag CLI/JIT/Native及CP整流另由独立产品门执行。
use crate::generator_product::Failure;
use crate::generator_product_initialization::Initialization;
use crate::generator_product_tests::{B, Observer, frontend};
use crate::trace_lowering::LoweringOrigins;
use crate::*;

use crate::generator_product_defaults_tests::fixture as defaults_fixture;
use crate::generator_product_initialization_tests::fixture as initialization_fixture;

struct High {
    program: Program,
    exports: Vec<ModuleExport>,
    origins: LoweringOrigins,
    plan: generator_product::DeferredGenerators,
    facts: trace_domain::Facts,
    initialization: Initialization,
}
impl High {
    fn new(source: &str) -> Self {
        let (hir, types) = frontend(source);
        let facts = trace_domain::analyze(&hir);
        let mut origins = LoweringOrigins::default();
        let mut plan = generator_product::DeferredGenerators::default();
        let (mut program, exports, errors) =
            build::build_program_mode(&hir, &types, Some(&mut origins), Some(&mut plan));
        assert!(errors.is_empty());
        let initialization = Initialization::expected(&hir, &origins, &program, &exports).unwrap();
        passes::run_pipeline_high(
            &mut program,
            &passes::default_pipeline(),
            &mut Observer,
            plan.functions(),
        )
        .unwrap();
        Self {
            program,
            exports,
            origins,
            plan,
            facts,
            initialization,
        }
    }
    fn facts_ready(&self) {
        assert!(
            self.facts.converged && self.facts.refusals.is_empty(),
            "{:?}",
            self.facts.refusals
        );
    }
    fn baseline(source: &str) -> Self {
        let high = Self::new(source);
        high.facts_ready();
        high.check().expect("未变异真实基底必须先通过全部高层合同");
        high
    }
    fn check(&self) -> Result<(), Failure> {
        validate::validate_high(&self.program, self.plan.functions()).map_err(|e| e.join("\n"))?;
        generator_product_trace::Contract::capture(&self.program, self.plan.functions())?;
        generator_product_high::inspect(
            &self.program,
            &self.facts,
            &self.origins,
            self.plan.functions(),
            &self.exports,
            &self.initialization,
        )
    }
    fn unknown(&mut self, last_function: bool, after: bool) {
        let function = if last_function {
            self.program.functions.iter().last().unwrap().0
        } else {
            self.program.functions.iter().next().unwrap().0
        };
        let body = &mut self.program.functions[function].body;
        // 不插到前言或正常返回Pop之后；同块前后交换不改变原栈合同。
        let block = &mut body.blocks[body.entry];
        let at = if after {
            block.stmts.len()
        } else {
            block
                .stmts
                .iter()
                .rposition(|s| matches!(s, Statement::SetGlobal { .. }))
                .map_or(block.stmts.len(), |at| at + 1)
        };
        let at = if matches!(
            block.stmts.last(),
            Some(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePopTo),
                ..
            }))
        ) {
            at.min(block.stmts.len() - 1)
        } else {
            at
        };
        block.stmts.insert(
            at,
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::Len),
                args: vec![Operand::Const(Constant::None)],
            }),
        );
    }
}

fn domain(result: Result<(), Failure>) {
    assert!(matches!(result, Err(Failure::NotInDomain(_))), "{result:?}");
}
fn invalid_reason(result: Result<(), Failure>, expected: &str) {
    match result {
        Err(Failure::InvalidCompilerState(reason)) => {
            assert!(reason.contains(expected), "期待{expected}，实际{reason}")
        }
        other => panic!("期待目标硬错{expected}，实际{other:?}"),
    }
}
fn token_invalid(result: Result<(), Failure>) {
    match result {
        Err(Failure::InvalidCompilerState(reason)) => assert!(
            reason.starts_with("实际helper TraceV1") && reason.ends_with("第0实参协议来源错误"),
            "{reason}"
        ),
        other => panic!("期待真实token来源硬错，实际{other:?}"),
    }
}
// 只重排测试定义区；原B及其产品golden/行号不变，所有seed先于失败执行尾部。
fn with_definitions(extra: &str, first: bool, seed: &str) -> String {
    let (definitions, tail) = B.split_once("\nit = ").expect("原B定义区/执行尾部分界");
    let definitions = if first {
        format!("{extra}\n{definitions}")
    } else {
        format!("{definitions}\n{extra}")
    };
    format!("{definitions}\n{seed}\nit = {tail}")
}

#[test]
fn unused_bottom_identity_is_uncovered_not_corrupt_and_preparation_consumes_once() {
    let source = format!("def identity(value):\n    return value\n{B}");
    let high = High::new(&source);
    high.facts_ready();
    domain(high.check());
    let (hir, types) = frontend(&source);
    assert!(matches!(
        generator_product::prepare_high(&hir, &types, &passes::default_pipeline(), &mut Observer),
        Err(Failure::NotInDomain(_))
    ));
    let (hir, types) = frontend(B);
    let prepared =
        generator_product::prepare_high(&hir, &types, &passes::default_pipeline(), &mut Observer)
            .unwrap();
    let (program, _, _) = generator_product::finish(prepared).unwrap();
    let generators = hir.functions.iter().filter(|(_, f)| f.is_generator).count();
    assert_eq!(program.functions.len(), hir.functions.len() + generators);
    // PreparedHigh没有Clone/公开构造，finish按值消费，不能重建后再次lower。
}

#[test]
fn bottom_helper_never_masks_independent_missing_initialization() {
    let mut high = High::new(&format!("def identity(value):\n    return value\n{B}"));
    high.facts_ready();
    domain(high.check());
    let slot = high
        .exports
        .iter()
        .find(|e| e.name == "__name__")
        .unwrap()
        .value;
    let body = &mut high.program.functions[high.program.entry].body;
    let block = &mut body.blocks[body.entry];
    let at = block
        .stmts
        .iter()
        .rposition(|s| matches!(s, Statement::SetGlobal { dest, .. } if *dest == slot))
        .unwrap();
    block.stmts.remove(at);
    match high.check() {
        Err(Failure::InvalidCompilerState(reason)) => {
            assert_eq!(reason, "代码来源缺少真实Legacy入口初值发布");
        }
        other => panic!("期待缺少真实Legacy初值发布硬错，实际{other:?}"),
    }
}

#[test]
fn unknown_helper_does_not_mask_token_owner_or_private_slot_in_either_order() {
    for last_function in [false, true] {
        for after in [false, true] {
            let mut control = High::baseline(B);
            control.unknown(last_function, after);
            domain(control.check());
            for mutation in 0..3 {
                let mut high = High::baseline(B);
                high.unknown(last_function, after);
                match mutation {
                    0 => {
                        let body = &mut high.program.functions[high.plan.functions()[0]].body;
                        let target = body
                            .blocks
                            .iter_mut()
                            .flat_map(|(_, b)| &mut b.stmts)
                            .find_map(|s| match s {
                                Statement::Eval(Rvalue::Call {
                                    callee:
                                        Callee::Runtime(
                                            RuntimeFn::TraceV1Match
                                            | RuntimeFn::TraceV1Abort
                                            | RuntimeFn::TraceV1AllMiss,
                                        ),
                                    args,
                                }) => Some(args),
                                _ => None,
                            })
                            .unwrap();
                        target[0] = Operand::Const(Constant::Null); // 同Dynamic类型，但非Park/None。
                        validate::validate_high(&high.program, high.plan.functions()).unwrap();
                    }
                    1 => {
                        let body = &mut high.program.functions[high.plan.functions()[0]].body;
                        let target = body
                            .blocks
                            .iter_mut()
                            .flat_map(|(_, b)| &mut b.stmts)
                            .find_map(|s| match s {
                                Statement::Assign {
                                    value:
                                        Rvalue::Call {
                                            callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                                            args,
                                        },
                                    ..
                                } => Some(args),
                                _ => None,
                            })
                            .unwrap();
                        target[2] = Operand::Const(Constant::None); // 同Dynamic类型，真实owner身份错误。
                    }
                    _ => {
                        let slot = high
                            .program
                            .functions
                            .iter()
                            .find_map(|(_, f)| f.defaults_global)
                            .unwrap();
                        high.exports.push(ModuleExport {
                            name: "leaked_private".into(),
                            value: slot,
                            guard: ExportGuard::None,
                        });
                    }
                }
                match mutation {
                    0 => token_invalid(high.check()),
                    1 => {
                        // 新载体与原owner验证器均须硬拒绝；逐字核两条诊断，不接受任意错误。
                        let expected = format!(
                            "函数 `{name}` 的新版压帧载体错误\n函数 `{name}` 压帧必须传实际owner参数",
                            name = high.program.functions[high.plan.functions()[0]].name
                        );
                        match high.check() {
                            Err(Failure::InvalidCompilerState(reason)) => {
                                assert_eq!(reason, expected);
                            }
                            other => panic!("期待owner硬错{expected}，实际{other:?}"),
                        }
                    }
                    _ => invalid_reason(high.check(), "私有槽角色冲突、公开导出或无效GlobalId"),
                }
            }
        }
    }
}

#[test]
fn missing_origin_mapping_is_typed_error_not_index_panic() {
    let mut high = High::baseline(B);
    let id = *high.origins.functions.keys().next().unwrap();
    high.origins.functions.remove(&id);
    invalid_reason(
        generator_product_values::Bounds::from_facts(&high.facts, &high.origins).map(|_| ()),
        "映射",
    );
}

#[test]
fn defaults_exact_127_128_129_span_and_real_missing_duplicate_publications() {
    use generator_product_defaults::Defaults;
    for span in [127, 128, 129] {
        for mutation in 0..3 {
            let mut f = defaults_fixture::Fixture::new();
            while f.publish - f.producer < span {
                let alias = f.local();
                let input = f.result;
                let at = f.publish;
                f.stmts().insert(
                    at,
                    Statement::Assign {
                        dest: alias,
                        value: Rvalue::Use(Operand::Local(input)),
                    },
                );
                f.publish += 1;
                let at = f.publish;
                let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
                    panic!("publish")
                };
                *value = Operand::Local(alias);
                f.result = alias;
            }
            let at = f.publish;
            match mutation {
                1 => {
                    f.stmts().remove(at);
                }
                2 => {
                    let statement = f.stmts()[at].clone();
                    f.stmts().insert(at + 1, statement);
                }
                _ => {}
            }
            let result = Defaults::inspect(&f.program, &f.exports).map(|_| ());
            if mutation != 0 {
                invalid_reason(
                    result,
                    if mutation == 1 {
                        "实际索引缺少发布"
                    } else {
                        "私有槽重复或错误发布"
                    },
                );
            } else if span > 128 {
                domain(result);
            } else {
                result.unwrap();
            }
        }
    }
}

#[test]
fn unsupported_signature_still_checks_owner_and_required_parameter_mapping() {
    let mut f = defaults_fixture::Fixture::new();
    f.program.functions[f.target].param_meta[0].has_default = true;
    let at = f.producer;
    let value = match &f.stmts()[at] {
        Statement::Assign { value, .. } => value.clone(),
        _ => panic!("def"),
    };
    domain(generator_product_defaults::make_function(&f.program, &value).map(|_| ()));
    f.program.functions[f.target].params.pop();
    invalid_reason(
        generator_product_defaults::make_function(&f.program, &value).map(|_| ()),
        "构造不是精确Python参数数",
    );
}

#[test]
fn initialization_127_128_129_steps_never_hide_missing_or_duplicate_roles() {
    for locals in [127, 128, 129] {
        for mutation in 0..3 {
            let mut f = initialization_fixture::Fixture::new(true);
            let mut at = f.publish("__name__");
            let Statement::SetGlobal {
                value: mut input, ..
            } = f.stmts()[at].clone()
            else {
                panic!("name")
            };
            // 原Box算一个局部；原循环128步同时含终端常量检查，127局部为可证明边界。
            for _ in 1..locals {
                let dest = f.local();
                f.stmts().insert(
                    at,
                    Statement::Assign {
                        dest,
                        value: Rvalue::Use(input),
                    },
                );
                input = Operand::Local(dest);
                at += 1;
            }
            let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
                panic!("publish")
            };
            *value = input;
            let at = f.publish("__spec__");
            match mutation {
                1 => {
                    f.stmts().remove(at);
                }
                2 => {
                    let statement = f.stmts()[at].clone();
                    f.stmts().insert(at, statement);
                }
                _ => {}
            }
            let result = f.initialization.inspect(&f.program, &f.exports);
            if mutation != 0 {
                invalid_reason(result, "角色不是唯一Null reset随后唯一发布");
            } else if locals >= 128 {
                domain(result);
            } else {
                result.unwrap();
            }
        }
    }
}

#[test]
fn unknown_cold_step_does_not_mask_an_opaque_defaults_escape() {
    for escape in [false, true] {
        let mut f = defaults_fixture::Fixture::new();
        let at = f.getter;
        f.stmts().insert(
            at,
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::Len),
                args: vec![Operand::Const(Constant::None)],
            }),
        );
        if escape {
            let result = f.result;
            f.stmts()
                .push(Statement::Eval(Rvalue::Use(Operand::Local(result))));
        }
        let checked =
            generator_product_defaults::Defaults::inspect(&f.program, &f.exports).map(|_| ());
        if escape {
            invalid_reason(checked, "不透明结果存在其它消费者或覆盖");
        } else {
            domain(checked);
        }
    }
}

#[test]
fn incomplete_facts_are_not_partially_credited_to_high() {
    let mut high = High::baseline(B);
    high.facts.converged = false;
    domain(high.check());
    // 原32项吸收测试和1024轮生产上限保留；本层不把未收敛前缀送入PreparedHigh。
    let (hir, types) = frontend(include_str!(
        "../../../tests/validation/generator_default/sources/budget_set.py"
    ));
    let facts = trace_domain::analyze(&hir);
    assert!(!facts.refusals.is_empty());
    assert!(matches!(
        generator_product::prepare_high(&hir, &types, &passes::default_pipeline(), &mut Observer),
        Err(Failure::NotInDomain(_))
    ));
}

#[path = "generator_product_selection_limits_tests.rs"]
mod limits;

#[path = "generator_product_selection_roots_tests.rs"]
mod roots;

#[path = "generator_product_selection_arguments_tests.rs"]
mod arguments;
