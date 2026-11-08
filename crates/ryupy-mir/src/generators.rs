//! 普通生成器的独立 MIR 降级：Suspend CFG → 工厂函数 + 可恢复 CFG。
//!
//! 首版保存所有局部（包含循环迭代器、临时值与共享 cell 引用），不做存活性裁剪。
//! 帧槽 0 是恢复块编号，槽 1 是本次 send 的值，槽 2 是注入异常、槽 3 是待接异常，槽 4 起按原 LocalId 保存装箱值。生成器运行时负责
//! running/closed 状态与每次恢复的 traceback 帧；此处只表达显式状态读写和控制流。

use crate::mir::*;
use la_arena::Arena;

pub(crate) fn lower(
    program: &mut Program,
    generators: &[FnId],
    exception_globals: &rustc_hash::FxHashMap<String, GlobalId>,
) -> Vec<crate::trace_lowering::GeneratorLayout> {
    lower_with_trace(program, generators, exception_globals, false)
}

pub(crate) fn lower_with_trace(
    program: &mut Program,
    generators: &[FnId],
    exception_globals: &rustc_hash::FxHashMap<String, GlobalId>,
    product: bool,
) -> Vec<crate::trace_lowering::GeneratorLayout> {
    generators
        .iter()
        .map(|&id| lower_one(program, id, exception_globals, product))
        .collect()
}

fn empty_body() -> Body {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Return(None),
    });
    Body {
        source_handler_contracts: Vec::new(),
        locals: Arena::default(),
        blocks,
        entry,
        regions: Vec::new(),
    }
}

pub(super) fn local(locals: &mut Arena<LocalDecl>, ty: MirType) -> LocalId {
    locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}

pub(super) fn call(dest: LocalId, callee: RuntimeFn, args: Vec<Operand>) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(callee),
            args,
        },
    }
}

pub(super) fn set_slot(frame: LocalId, index: usize, value: Operand) -> Statement {
    Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::ArraySet),
        args: vec![
            Operand::Local(frame),
            Operand::Const(Constant::I64(index as i64)),
            value,
        ],
    })
}

/// 标量在跨暂停边时装箱；动态值（尤其 cell）仅保存引用，绝不能复制 cell 内容。
pub(super) fn boxed(
    locals: &mut Arena<LocalDecl>,
    stmts: &mut Vec<Statement>,
    value: Operand,
    ty: &MirType,
) -> Operand {
    if *ty == MirType::Dynamic {
        return value;
    }
    let dest = local(locals, MirType::Dynamic);
    stmts.push(Statement::Assign {
        dest,
        value: Rvalue::Box {
            value,
            from: ty.clone(),
        },
    });
    Operand::Local(dest)
}

fn lower_one(
    program: &mut Program,
    id: FnId,
    exception_globals: &rustc_hash::FxHashMap<String, GlobalId>,
    product: bool,
) -> crate::trace_lowering::GeneratorLayout {
    let function = &mut program.functions[id];
    let mut body = std::mem::replace(&mut function.body, empty_body());
    let original: Vec<_> = body
        .locals
        .iter()
        .map(|(id, decl)| (id, decl.ty.clone()))
        .collect();
    let original_entry = body.entry;
    let depths: Vec<_> = body.blocks.iter().flat_map(|(_, block)| &block.stmts)
        .filter_map(|stmt| match stmt {
            Statement::Assign { dest, value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin), ..
            }} => Some(*dest),
            _ => None,
        }).collect();
    let mut factory = empty_body();
    for (_, decl) in body.locals.iter() {
        factory.locals.alloc(LocalDecl {
            ty: decl.ty.clone(),
            name: decl.name.clone(),
            is_cell: decl.is_cell,
        });
    }
    let frame = local(&mut body.locals, MirType::Dynamic);
    let original_block_count = body.blocks.len();
    let exceptions = crate::generator_exceptions::Plan::take(&mut body);
    let exception_routes = exceptions.frame_routes();
    let mut exit_pcs = Vec::new();
    let mut resume_targets = vec![original_entry];
    for handler in exceptions.handlers() {
        if !resume_targets.contains(&handler) {
            resume_targets.push(handler);
        }
    }
    let mut inputs = Vec::new();

    for (block_id, block) in body.blocks.iter_mut() {
        // 每次 next 的帧由运行时包住，不能在暂停后把旧深度恢复到新的调用者栈。
        block.stmts.retain(|stmt| {
            !matches!(
                stmt,
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
            )
        });
        for statement in &mut block.stmts {
            if let Statement::Assign { value, .. } | Statement::Eval(value) = statement
                && let Rvalue::GeneratorDelegateStep { delegate } = value
            {
                // 委托步骤需要本实例控制槽，只有工厂/恢复拆分后才有 frame 参数。
                *value = Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::GeneratorDelegateStep),
                    args: vec![Operand::Local(frame), delegate.clone()],
                };
            }
        }
        let term = std::mem::replace(&mut block.terminator, Terminator::Unreachable);
        block.terminator = match term {
            Terminator::Suspend {
                value,
                resume,
                input,
                delegated,
            } => {
                exit_pcs.push((block_id, resume.into_raw().into_u32() as i64));
                // yield from 的恢复输入由委托步骤转发给子迭代器，不能提前消费/抛出。
                if !delegated {
                    inputs.push((resume, input));
                }
                if !resume_targets.contains(&resume) {
                    resume_targets.push(resume);
                }
                for (slot, ty) in &original {
                    let value = boxed(
                        &mut body.locals,
                        &mut block.stmts,
                        Operand::Local(*slot),
                        ty,
                    );
                    block.stmts.push(set_slot(
                        frame,
                        slot.into_raw().into_u32() as usize + 4,
                        value,
                    ));
                }
                let state = boxed(
                    &mut body.locals,
                    &mut block.stmts,
                    Operand::Const(Constant::I64(resume.into_raw().into_u32() as i64)),
                    &MirType::I64,
                );
                block.stmts.push(set_slot(frame, 0, state));
                Terminator::Return(Some(value))
            }
            Terminator::Return(value) => {
                exit_pcs.push((block_id, -1));
                let state = boxed(
                    &mut body.locals,
                    &mut block.stmts,
                    Operand::Const(Constant::I64(-1)),
                    &MirType::I64,
                );
                block.stmts.push(set_slot(frame, 0, state));
                // 完成值只由触发本次结束的 next 消费；运行时依 PC 区分它和 yield。
                Terminator::Return(Some(value.unwrap_or(Operand::Const(Constant::None))))
            }
            other => other,
        };
    }

    for (resume, input) in inputs {
        // MIR 的 Eval 用于 void 调用；GeneratorInput 返回对象，语句型 yield 也要
        // 接到丢弃临时槽位，不能把返回值遗留在 CIL 求值栈。
        let dest = input.unwrap_or_else(|| local(&mut body.locals, MirType::Dynamic));
        let receive = call(dest, RuntimeFn::GeneratorInput, vec![Operand::Local(frame)]);
        // 即便语句型 yield 不接收值，也必须在原暂停点检查 throw / close 注入。
        body.blocks[resume].stmts.insert(0, receive);
    }

    // 所有恢复入口先重新加载局部；初次调用的帧用类型合法的默认值填充未使用临时槽位。
    let mut restore = Vec::new();
    for (slot, ty) in &original {
        let dest = if *ty == MirType::Dynamic {
            *slot
        } else {
            local(&mut body.locals, MirType::Dynamic)
        };
        restore.push(call(
            dest,
            RuntimeFn::ArrayGet,
            vec![
                Operand::Local(frame),
                Operand::Const(Constant::I64(slot.into_raw().into_u32() as i64 + 4)),
            ],
        ));
        if dest != *slot {
            restore.push(Statement::Assign {
                dest: *slot,
                value: Rvalue::Unbox {
                    value: Operand::Local(dest),
                    to: ty.clone(),
                },
            });
        }
    }
    let state_object = local(&mut body.locals, MirType::Dynamic);
    if product {
        // 本地槽虽跨暂停保存，但接收深度必须绑定本次实际 Push，而非上一 activation。
        for depth in depths {
            restore.push(call(depth, RuntimeFn::TraceV1GeneratorDepth, vec![]));
        }
    }
    restore.push(call(
        state_object,
        RuntimeFn::ArrayGet,
        vec![Operand::Local(frame), Operand::Const(Constant::I64(0))],
    ));
    let state = local(&mut body.locals, MirType::I64);
    restore.push(Statement::Assign {
        dest: state,
        value: Rvalue::Unbox {
            value: Operand::Local(state_object),
            to: MirType::I64,
        },
    });
    let mut dispatch = original_entry;
    for &target in resume_targets.iter().skip(1).rev() {
        let cond = local(&mut body.locals, MirType::Bool);
        dispatch = body.blocks.alloc(BasicBlock {
            stmts: vec![Statement::Assign {
                dest: cond,
                value: Rvalue::Binary {
                    op: BinOp::Eq,
                    lhs: Operand::Local(state),
                    rhs: Operand::Const(Constant::I64(target.into_raw().into_u32() as i64)),
                },
            }],
            terminator: Terminator::Branch {
                cond: Operand::Local(cond),
                then_: target,
                else_: dispatch,
            },
        });
    }
    body.entry = body.blocks.alloc(BasicBlock {
        stmts: restore,
        terminator: Terminator::Goto(dispatch),
    });

    let (block_map, catch_reads) =
        exceptions.finish(&mut body, frame, &original, exception_globals, product);

    let physical_region = body
        .regions
        .first()
        .map(|region| (region.try_len, region.handler));
    let name = function.name.clone();
    let source_file = function.source_file.clone();
    let codegen_name = format!(
        "{}$resume{}",
        function.codegen_name,
        id.into_raw().into_u32()
    );
    let params = function.params.clone();
    let origin = function
        .dynamic_origin
        .map(|source| Operand::Local(source.owner));
    let capabilities = function.instance_capabilities;
    let module_state = function.module_state;
    let closure_environment = function.env;
    let resume = program.functions.alloc(Function {
        module_state,
        instance_capabilities: capabilities,
        module_state_source: module_state.map(|state| crate::InstanceStateSource::GeneratorFrame {
            frame,
            slot: state.into_raw().into_u32() as usize + 4,
        }),
        name,
        codegen_name,
        source_file,
        definition: None,
        params: vec![frame],
        ret: MirType::Dynamic,
        body,
        param_meta: Vec::new(),
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    });

    crate::class_cells::register_resume(
        &mut program.classes,
        id,
        resume,
        closure_environment,
        frame,
    );

    let stmts = &mut factory.blocks[factory.entry].stmts;
    let initial = boxed(
        &mut factory.locals,
        stmts,
        Operand::Const(Constant::I64(original_entry.into_raw().into_u32() as i64)),
        &MirType::I64,
    );
    let mut values = vec![
        initial,
        Operand::Const(Constant::None),
        Operand::Const(Constant::None),
        Operand::Const(Constant::None),
    ];
    for (slot, ty) in &original {
        let value = if params.contains(slot) {
            Operand::Local(*slot)
        } else {
            Operand::Const(match ty {
                MirType::I64 => Constant::I64(0),
                MirType::F64 => Constant::F64(0.0),
                MirType::Bool => Constant::Bool(false),
                MirType::Str => Constant::Str(String::new()),
                _ => Constant::Null,
            })
        };
        // null 已是动态引用，不能对未初始化的类值做非法 box。
        let value = if matches!(value, Operand::Const(Constant::Null)) {
            value
        } else {
            boxed(&mut factory.locals, stmts, value, ty)
        };
        values.push(value);
    }
    let result = local(&mut factory.locals, MirType::Dynamic);
    stmts.push(Statement::Assign {
        dest: result,
        value: Rvalue::MakeGenerator {
            resume,
            frame: values,
            origin,
        },
    });
    factory.blocks[factory.entry].terminator = Terminator::Return(Some(Operand::Local(result)));
    program.functions[id].body = factory;
    crate::trace_lowering::GeneratorLayout {
        factory: id,
        resume,
        initial_pc: original_entry.into_raw().into_u32() as i64,
        pc_targets: resume_targets
            .into_iter()
            .map(|old| {
                (
                    old.into_raw().into_u32() as i64,
                    block_map[old.into_raw().into_u32() as usize],
                )
            })
            .collect(),
        original_blocks: block_map[..original_block_count].to_vec(),
        exception_routes,
        catch_reads,
        exit_pcs: exit_pcs
            .into_iter()
            .map(|(block, pc)| (block_map[block.into_raw().into_u32() as usize], pc))
            .collect(),
        physical_region,
        saved_locals: original.into_iter().map(|(local, _)| local).collect(),
    }
}
