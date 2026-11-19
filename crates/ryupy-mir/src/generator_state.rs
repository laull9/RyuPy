//! 字段驻留生成器：原生PC与类型化字段保存长期局部，单次恢复临时不再逐yield全量搬运。
//! 每个字段读写显式进入MIR，后端复用普通GetField/SetField；复杂协议仍由旧降级承担。
use crate::generators::{call, local};
use crate::local_access::{
    rewrite_statement, rewrite_terminator, statement_reads, terminator_reads,
};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(crate) fn lower(program: &mut Program, id: FnId) -> bool {
    if !crate::generator_state_analysis::eligible(&program.functions[id]) {
        return false;
    }
    // 暂停边的send目标由分析按定义处理；载体选择前只读检查原CFG。
    let f = &mut program.functions[id];
    let mut inputs = Vec::new();
    for (_, b) in f.body.blocks.iter() {
        if let Terminator::Suspend { resume, input, .. } = b.terminator {
            inputs.push((resume, input));
        }
    }
    // 有限MIR契约要求每个恢复块只属于一个yield；其他形态沿旧路径。
    if inputs
        .iter()
        .map(|(b, _)| *b)
        .collect::<FxHashSet<_>>()
        .len()
        != inputs.len()
    {
        return false;
    }
    let Some(residents) = crate::generator_state_analysis::residents(f) else {
        return false;
    };
    // 命名绑定和参数保留；匿名临时只在跨暂停实际活跃时进入字段。
    let mut body = std::mem::replace(&mut f.body, crate::generators::empty_body());
    let entry = body.entry;
    let fields: Vec<_> = body
        .locals
        .iter()
        .filter(|(id, _)| residents.contains(id))
        .map(|(id, decl)| (id, decl.ty.clone()))
        .collect();
    let slots: FxHashMap<_, _> = fields
        .iter()
        .enumerate()
        .map(|(n, (id, _))| (*id, n))
        .collect();
    let frame_class = program.classes.alloc(ClassDef {
        class_cell: None,
        name: format!("__generator_state_{}", id.into_raw().into_u32()),
        codegen_name: format!("{}$state{}", f.codegen_name, id.into_raw().into_u32()),
        fields: fields
            .iter()
            .map(|(id, ty)| (format!("slot{}", id.into_raw().into_u32()), ty.clone()))
            .collect(),
        is_value_type: false,
        is_generator_state: true,
        is_dataclass: false,
        base: None,
        builtin_base: None,
        is_exception: false,
        factory_global: None,
    });
    let frame = local(&mut body.locals, MirType::Dynamic);
    let payload = local(&mut body.locals, MirType::Class(frame_class));
    let mut targets = vec![entry];
    for (resume, input) in inputs {
        let dest = input.unwrap_or_else(|| local(&mut body.locals, MirType::Dynamic));
        body.blocks[resume].stmts.insert(
            0,
            call(dest, RuntimeFn::GeneratorInput, vec![Operand::Local(frame)]),
        );
        targets.push(resume);
    }
    for (_, block) in body.blocks.iter_mut() {
        let old = std::mem::take(&mut block.stmts);
        for mut s in old {
            if matches!(
                &s,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(
                            RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin
                        ),
                        ..
                    },
                    ..
                } | Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    ..
                })
            ) {
                continue;
            }
            let map = load_reads(
                &mut body.locals,
                &mut block.stmts,
                statement_reads(&s),
                &slots,
                payload,
            );
            rewrite_statement(&mut s, &map);
            let dest = if let Statement::Assign { dest, .. } = &s {
                Some(*dest)
            } else {
                None
            };
            block.stmts.push(s);
            if let Some(dest) = dest
                && let Some(&field) = slots.get(&dest)
            {
                block.stmts.push(Statement::SetField {
                    obj: Operand::Local(payload),
                    field: field as u32,
                    value: Operand::Local(dest),
                });
            }
        }
        let map = load_reads(
            &mut body.locals,
            &mut block.stmts,
            terminator_reads(&block.terminator),
            &slots,
            payload,
        );
        rewrite_terminator(&mut block.terminator, &map);
        let term = std::mem::replace(&mut block.terminator, Terminator::Unreachable);
        block.terminator = match term {
            Terminator::Suspend { value, resume, .. } => {
                block
                    .stmts
                    .push(set_pc(frame, i64::from(resume.into_raw().into_u32())));
                Terminator::Return(Some(value))
            }
            Terminator::Return(value) => {
                block.stmts.push(set_pc(frame, -1));
                Terminator::Return(Some(value.unwrap_or(Operand::Const(Constant::None))))
            }
            other => other,
        };
    }
    let state = local(&mut body.locals, MirType::I64);
    let raw_payload = local(&mut body.locals, MirType::Dynamic);
    let mut dispatch = entry;
    for &target in targets.iter().skip(1).rev() {
        let condition = local(&mut body.locals, MirType::Bool);
        dispatch = body.blocks.alloc(BasicBlock {
            stmts: vec![Statement::Assign {
                dest: condition,
                value: Rvalue::Binary {
                    op: BinOp::Eq,
                    lhs: Operand::Local(state),
                    rhs: Operand::Const(Constant::I64(i64::from(target.into_raw().into_u32()))),
                },
            }],
            terminator: Terminator::Branch {
                cond: Operand::Local(condition),
                then_: target,
                else_: dispatch,
            },
        });
    }
    body.entry = body.blocks.alloc(BasicBlock {
        stmts: vec![
            call(
                raw_payload,
                RuntimeFn::GeneratorStateData,
                vec![Operand::Local(frame)],
            ),
            Statement::Assign {
                dest: payload,
                value: Rvalue::Unbox {
                    value: Operand::Local(raw_payload),
                    to: MirType::Class(frame_class),
                },
            },
            call(
                state,
                RuntimeFn::GeneratorStateGetPC,
                vec![Operand::Local(frame)],
            ),
        ],
        terminator: Terminator::Goto(dispatch),
    });
    let mut factory = crate::generators::empty_body();
    // 工厂仍使用原参数LocalId，普通调用与默认值/函数owner绑定无需改变。
    for (_, decl) in body.locals.iter() {
        factory.locals.alloc(LocalDecl {
            ty: decl.ty.clone(),
            name: decl.name.clone(),
            is_cell: false,
        });
    }
    let parameters = f.params.clone();
    let origin = f.dynamic_origin.map(|o| Operand::Local(o.owner));
    let resume_function = Function {
        core: Default::default(),
        is_comprehension: false,
        module_state: None,
        module_state_source: None,
        instance_capabilities: f.instance_capabilities,
        definition: None,
        name: f.name.clone(),
        codegen_name: format!(
            "{}$state_resume{}",
            f.codegen_name,
            id.into_raw().into_u32()
        ),
        source_file: f.source_file.clone(),
        params: vec![frame],
        ret: MirType::Dynamic,
        body,
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    };
    let resume = program.functions.alloc(resume_function);
    let data = local(&mut factory.locals, MirType::Class(frame_class));
    let values = fields
        .iter()
        .map(|(id, ty)| {
            if parameters.contains(id) {
                Operand::Local(*id)
            } else {
                Operand::Const(match ty {
                    MirType::I64 => Constant::I64(0),
                    MirType::F64 => Constant::F64(0.0),
                    MirType::Bool => Constant::Bool(false),
                    MirType::Str => Constant::Str(String::new()),
                    _ => Constant::Null,
                })
            }
        })
        .collect();
    let result = local(&mut factory.locals, MirType::Dynamic);
    factory.blocks[factory.entry].stmts = vec![
        Statement::Assign {
            dest: data,
            value: Rvalue::NewObject {
                class: frame_class,
                args: values,
            },
        },
        Statement::Assign {
            dest: result,
            value: Rvalue::MakeStateGenerator {
                resume,
                sum_resume: None,
                payload: Operand::Local(data),
                initial_pc: i64::from(entry.into_raw().into_u32()),
                origin,
            },
        },
    ];
    factory.blocks[factory.entry].terminator = Terminator::Return(Some(Operand::Local(result)));
    program.functions[id].body = factory;
    true
}

fn load_reads(
    locals: &mut la_arena::Arena<LocalDecl>,
    stmts: &mut Vec<Statement>,
    reads: FxHashSet<LocalId>,
    slots: &FxHashMap<LocalId, usize>,
    payload: LocalId,
) -> FxHashMap<LocalId, LocalId> {
    let mut map = FxHashMap::default();
    // 固定LocalId顺序，输出与哈希表迭代种子无关。
    let mut reads: Vec<_> = reads
        .into_iter()
        .filter(|id| slots.contains_key(id))
        .collect();
    reads.sort_by_key(|id| id.into_raw().into_u32());
    for id in reads {
        let ty = locals[id].ty.clone();
        let temp = local(locals, ty);
        stmts.push(Statement::Assign {
            dest: temp,
            value: Rvalue::GetField {
                obj: Operand::Local(payload),
                field: slots[&id] as u32,
            },
        });
        map.insert(id, temp);
    }
    map
}
fn set_pc(frame: LocalId, pc: i64) -> Statement {
    Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::GeneratorStateSetPC),
        args: vec![Operand::Local(frame), Operand::Const(Constant::I64(pc))],
    })
}

/// 默认普通模块先构建高层生成器；字段载体未准入者交回完整旧降级。
pub(crate) fn build_program(
    hir: &ryupy_hir::Module,
    types: &ryupy_ty::ModuleTypes,
) -> (
    Program,
    Vec<crate::ModuleExport>,
    Vec<ryupy_base::Diagnostic>,
) {
    build_program_with_generic(hir, types, false)
}

pub(crate) fn build_program_with_generic(
    hir: &ryupy_hir::Module,
    types: &ryupy_ty::ModuleTypes,
    generic: bool,
) -> (
    Program,
    Vec<crate::ModuleExport>,
    Vec<ryupy_base::Diagnostic>,
) {
    let mut deferred = crate::generator_product::DeferredGenerators::default();
    let (mut program, exports, diagnostics) =
        crate::build::build_program_cores(hir, types, None, Some(&mut deferred), false, generic);
    #[cfg(feature = "generator-sum-experiment")]
    let prepared = crate::generator_sum_emit::prepare(&program, &deferred);
    let fallback: Vec<_> = deferred
        .functions()
        .iter()
        .copied()
        .filter(|id| !crate::generator_state::lower(&mut program, *id))
        .collect();
    crate::generators::lower(&mut program, &fallback, &deferred.exceptions);
    #[cfg(feature = "generator-sum-experiment")]
    crate::generator_sum_emit::finish(&mut program, prepared);
    crate::coroutines::wrap_factories(&mut program, &deferred.coroutines);
    (program, exports, diagnostics)
}
