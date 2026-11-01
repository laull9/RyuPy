//! 普通生成器的独立 MIR 降级：Suspend CFG → 工厂函数 + 可恢复 CFG。
//!
//! 首版保存所有局部（包含循环迭代器、临时值与共享 cell 引用），不做存活性裁剪。
//! 帧槽 0 是恢复块编号，槽 1 起按原 LocalId 保存装箱值。生成器运行时负责
//! running/closed 状态与每次恢复的 traceback 帧；此处只表达显式状态读写和控制流。

use crate::mir::*;
use la_arena::Arena;

pub(crate) fn lower(program: &mut Program, generators: &[FnId]) {
    for &id in generators {
        lower_one(program, id);
    }
}

fn empty_body() -> Body {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Return(None),
    });
    Body {
        locals: Arena::default(),
        blocks,
        entry,
        regions: Vec::new(),
    }
}

fn local(locals: &mut Arena<LocalDecl>, ty: MirType) -> LocalId {
    locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}

fn call(dest: LocalId, callee: RuntimeFn, args: Vec<Operand>) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(callee),
            args,
        },
    }
}

fn set_slot(frame: LocalId, index: usize, value: Operand) -> Statement {
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
fn boxed(
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

fn lower_one(program: &mut Program, id: FnId) {
    let function = &mut program.functions[id];
    let mut body = std::mem::replace(&mut function.body, empty_body());
    let original: Vec<_> = body
        .locals
        .iter()
        .map(|(id, decl)| (id, decl.ty.clone()))
        .collect();
    let original_entry = body.entry;
    let mut factory = empty_body();
    for (_, decl) in body.locals.iter() {
        factory.locals.alloc(LocalDecl {
            ty: decl.ty.clone(),
            name: decl.name.clone(),
            is_cell: decl.is_cell,
        });
    }
    let frame = local(&mut body.locals, MirType::Dynamic);
    let mut resume_targets = vec![original_entry];

    for (_, block) in body.blocks.iter_mut() {
        // 每次 next 的帧由运行时包住，不能在暂停后把旧深度恢复到新的调用者栈。
        block.stmts.retain(|stmt| {
            !matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::TracePush),
                        ..
                    },
                    ..
                } | Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    ..
                })
            )
        });
        let term = std::mem::replace(&mut block.terminator, Terminator::Unreachable);
        block.terminator = match term {
            Terminator::Suspend { value, resume } => {
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
                        slot.into_raw().into_u32() as usize + 1,
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
            Terminator::Return(_) => {
                let state = boxed(
                    &mut body.locals,
                    &mut block.stmts,
                    Operand::Const(Constant::I64(-1)),
                    &MirType::I64,
                );
                block.stmts.push(set_slot(frame, 0, state));
                Terminator::Return(Some(Operand::Const(Constant::None)))
            }
            other => other,
        };
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
                Operand::Const(Constant::I64(slot.into_raw().into_u32() as i64 + 1)),
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

    let name = function.name.clone();
    let source_file = function.source_file.clone();
    let codegen_name = format!(
        "{}$resume{}",
        function.codegen_name,
        id.into_raw().into_u32()
    );
    let params = function.params.clone();
    let resume = program.functions.alloc(Function {
        name,
        codegen_name,
        source_file,
        params: vec![frame],
        ret: MirType::Dynamic,
        body,
        param_meta: Vec::new(),
        function_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
    });

    let stmts = &mut factory.blocks[factory.entry].stmts;
    let initial = boxed(
        &mut factory.locals,
        stmts,
        Operand::Const(Constant::I64(original_entry.into_raw().into_u32() as i64)),
        &MirType::I64,
    );
    let mut values = vec![initial];
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
        },
    });
    factory.blocks[factory.entry].terminator = Terminator::Return(Some(Operand::Local(result)));
    program.functions[id].body = factory;
}
