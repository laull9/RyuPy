//! 真实发布来源与前置事实的回归：可选角色不授值，整链删改由先capture的原合同保护。
//! 只作IR单测，不运行Python或产品；与预算测试共享真实两条冷发布链的身份核验。
use super::*;

pub(super) struct ColdChain {
    pub block: BlockId,
    pub producer: usize,
    pub getter: usize,
    pub publish: usize,
    pub result: LocalId,
    pub target: FnId,
    pub slot: GlobalId,
}
pub(super) fn cold_chains(high: &High) -> Vec<ColdChain> {
    generator_product_defaults::Defaults::inspect(&high.program, &high.exports).unwrap();
    let module = &high.program.functions[high.program.entry];
    let mut chains = vec![];
    for (block_id, block) in module.body.blocks.iter() {
        for (getter, statement) in block.stmts.iter().enumerate() {
            let Statement::Assign {
                dest: result,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::FunctionDefaults),
                        args,
                    },
            } = statement
            else {
                continue;
            };
            let [Operand::Local(receiver)] = args.as_slice() else {
                panic!("真实receiver");
            };
            let producers: Vec<_> = block
                .stmts
                .iter()
                .enumerate()
                .filter_map(|(at, s)| match s {
                    Statement::Assign {
                        dest,
                        value: Rvalue::MakeFunction { func, .. },
                    } if dest == receiver => Some((at, *func)),
                    _ => None,
                })
                .collect();
            let [(producer, target)] = producers.as_slice() else {
                panic!("唯一真实MakeFunction");
            };
            let slot = high.program.functions[*target].defaults_global.unwrap();
            let writes: Vec<_> = high
                .program
                .functions
                .iter()
                .flat_map(|(function, f)| {
                    f.body.blocks.iter().flat_map(move |(id, b)| {
                        b.stmts
                            .iter()
                            .enumerate()
                            .filter_map(move |(at, s)| match s {
                                Statement::SetGlobal { dest, value } if *dest == slot => {
                                    Some((function, id, at, value))
                                }
                                _ => None,
                            })
                    })
                })
                .collect();
            let [(function, published_block, publish, value)] = writes.as_slice() else {
                panic!("唯一真实私有发布");
            };
            assert_eq!(
                (*function, *published_block),
                (high.program.entry, block_id)
            );
            assert_eq!(**value, Operand::Local(*result));
            assert!(*producer < getter && getter < *publish);
            chains.push(ColdChain {
                block: block_id,
                producer: *producer,
                getter,
                publish: *publish,
                result: *result,
                target: *target,
                slot,
            });
        }
    }
    for (index, chain) in chains.iter().enumerate() {
        for other in &chains[..index] {
            assert_ne!(chain.target, other.target);
            assert_ne!(chain.slot, other.slot);
            assert_ne!((chain.block, chain.result), (other.block, other.result));
        }
    }
    chains
}

#[test]
fn optional_property_slot_is_not_a_publication_or_read_capability() {
    for mutation in 0..2 {
        let mut high = High::baseline(B);
        let slot = high
            .program
            .functions
            .iter()
            .find(|(_, f)| f.name == "error")
            .unwrap()
            .1
            .defaults_global
            .unwrap();
        assert!(
            !high
                .program
                .functions
                .iter()
                .flat_map(|(_, f)| f.body.blocks.iter())
                .any(|(_, b)| {
                    b.stmts
                        .iter()
                        .any(|s| matches!(s, Statement::SetGlobal { dest, .. } if *dest == slot))
                })
        );
        if mutation == 0 {
            high.exports.push(ModuleExport {
                name: "leaked_optional".into(),
                value: slot,
                guard: ExportGuard::None,
            });
            invalid_reason(high.check(), "私有槽角色冲突、公开导出或无效GlobalId");
        } else {
            let body = &mut high.program.functions[high.program.entry].body;
            let block = &mut body.blocks[body.entry];
            let at = block.stmts.len() - 1;
            block
                .stmts
                .insert(at, Statement::Eval(Rvalue::Use(Operand::Global(slot))));
            invalid_reason(
                high.check(),
                "FunctionDefaults合同：私有槽不能作为高层值读取",
            );
        }
    }
}

#[test]
fn captured_def_effects_cannot_be_removed_by_high_passes() {
    for mutation in 0..4 {
        let mut high = High::baseline(B);
        let chains = cold_chains(&high);
        assert_eq!(chains.len(), 1, "B只有一条普通def静态发布链");
        let chain = &chains[0];
        // 必须先capture原程序；变异后High::check重新capture不能代替历史守恒证据。
        let contract =
            generator_product_trace::Contract::capture(&high.program, high.plan.functions())
                .unwrap();
        contract
            .verify(&high.program, high.plan.functions())
            .unwrap();
        let mut deleted = match mutation {
            0 => vec![chain.producer],
            1 => vec![chain.getter],
            2 => vec![chain.publish],
            _ => vec![chain.getter, chain.publish],
        };
        deleted.sort_unstable();
        for at in deleted.into_iter().rev() {
            high.program.functions[high.program.entry].body.blocks[chain.block]
                .stmts
                .remove(at);
        }
        let error = contract
            .verify(&high.program, high.plan.functions())
            .unwrap_err();
        assert_eq!(error, "pass改变了仍可读的局部定义或实际effect顺序");
    }
}

#[test]
fn calls_before_later_definitions_remain_out_of_domain() {
    for source in [
        format!("{B}\ndef helper():\n    return next\n"),
        format!(
            "def passthrough(value):\n    return value\npassthrough(0)\npassthrough(None)\n{B}"
        ),
    ] {
        let (hir, types) = frontend(&source);
        let facts = trace_domain::analyze(&hir);
        assert!(facts.converged);
        assert!(facts.refusals.contains("调用先于闭世界定义初始化完成"));
        match generator_product::prepare_high(
            &hir,
            &types,
            &passes::default_pipeline(),
            &mut Observer,
        ) {
            Err(Failure::NotInDomain(reason)) => {
                assert!(reason.contains("调用先于闭世界定义初始化完成"))
            }
            _ => panic!("原顺序guard必须在构建前拒绝"),
        }
    }
}

#[test]
fn known_receiver_requires_publication_beyond_output_budget() {
    for unsupported_signature in [false, true] {
        for mutation in 0..4 {
            let mut f = defaults_fixture::Fixture::new();
            let mut input = f.result;
            for _ in 0..129 {
                let dest = f.local();
                let at = f.publish;
                f.stmts().insert(
                    at,
                    Statement::Assign {
                        dest,
                        value: Rvalue::Use(Operand::Local(input)),
                    },
                );
                f.publish += 1;
                input = dest;
            }
            let at = f.publish;
            let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
                panic!("publish");
            };
            *value = Operand::Local(input);
            if unsupported_signature {
                f.program.functions[f.target].param_meta[0].has_default = true;
            }
            domain(
                generator_product_defaults::Defaults::inspect(&f.program, &f.exports).map(|_| ()),
            );
            match mutation {
                0 => continue,
                1 => {
                    f.stmts().remove(at);
                }
                2 => {
                    let statement = f.stmts()[at].clone();
                    f.stmts().insert(at + 1, statement);
                }
                _ => {
                    let wrong = f.program.globals.alloc(GlobalDecl {
                        storage: GlobalStorage::LegacyStatic,
                        name: "wrong_target".into(),
                        ty: MirType::Dynamic,
                    });
                    let Statement::SetGlobal { dest, .. } = &mut f.stmts()[at] else {
                        panic!("publish");
                    };
                    *dest = wrong;
                }
            }
            invalid_reason(
                generator_product_defaults::Defaults::inspect(&f.program, &f.exports).map(|_| ()),
                if mutation == 2 {
                    "私有槽重复或错误发布"
                } else {
                    "实际索引缺少发布"
                },
            );
        }
    }
}
