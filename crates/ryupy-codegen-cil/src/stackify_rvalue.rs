//! MIR rvalue → CIL 指令序列的发射。
//!
//! 一条 rvalue 对应「压栈操作数 → 运算指令」，结果由调用方 `stloc`。
//! 动态调用在这里附加 PIC 调用点编号；`MakeFunction` 在此展开成
//! `new PyFunction(...)` 的完整构造序列。

use dotnetdll::prelude::*;
use ryupy_mir::{
    BinOp, Callee, CastKind, Constant as MirConstant, Operand, RuntimeFn, Rvalue, UnaryOp,
};

use crate::stackify::LowerCtx;
use crate::stackify_operands::{lower_object_array, lower_operand};
use crate::stackify_refs::{boxed_literal_key, emit_box, runtime_method};

/// 一条 rvalue 最多会把多少个值同时留在求值栈上。
///
/// 只关心「同一层」的压栈数：调用/构造的实参全部先求值再压栈，因此取实参个数；
/// 嵌套调用的额外深度已经包含在各自的实参计数里，这里不重复累加。
pub(crate) fn rvalue_arg_count(value: &Rvalue) -> usize {
    match value {
        Rvalue::Call { args, .. } | Rvalue::NewObject { args, .. } => args.len(),
        // MakeFunction 的数组逐元素发射，加上构造函数的其余实参
        Rvalue::MakeFunction { env, defaults, .. } => env.len() + defaults.len() + 8,
        Rvalue::MakeGenerator { frame, .. } => frame.len() + 5,
        _ => 0,
    }
}

fn is_f64_operand(op: &Operand, locals: &la_arena::Arena<ryupy_mir::LocalDecl>) -> bool {
    match op {
        Operand::Const(MirConstant::F64(_)) => true,
        Operand::Local(loc) => locals[*loc].ty == ryupy_mir::MirType::F64,
        _ => false,
    }
}

pub(crate) fn lower_rvalue(rval: &Rvalue, ins: &mut Vec<Instruction>, ctx: &LowerCtx<'_>) {
    match rval {
        Rvalue::Use(op) => {
            lower_operand(op, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs)
        }
        Rvalue::Binary { op, lhs, rhs } => {
            lower_operand(lhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            lower_operand(rhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            let is_float = is_f64_operand(lhs, ctx.locals) || is_f64_operand(rhs, ctx.locals);
            match op {
                BinOp::Add => {
                    if is_float {
                        ins.push(Instruction::Add);
                    } else {
                        ins.push(Instruction::AddOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Sub => {
                    if is_float {
                        ins.push(Instruction::Subtract);
                    } else {
                        ins.push(Instruction::SubtractOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Mul => {
                    if is_float {
                        ins.push(Instruction::Multiply);
                    } else {
                        ins.push(Instruction::MultiplyOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Div => ins.push(Instruction::Divide(NumberSign::Signed)),
                BinOp::BitAnd => ins.push(Instruction::And),
                BinOp::BitOr => ins.push(Instruction::Or),
                BinOp::BitXor => ins.push(Instruction::Xor),
                BinOp::Shl => ins.push(Instruction::ShiftLeft),
                BinOp::Shr => ins.push(Instruction::ShiftRight(NumberSign::Signed)),
                BinOp::Eq => ins.push(Instruction::CompareEqual),
                BinOp::Ne => {
                    ins.push(Instruction::CompareEqual);
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                BinOp::Lt => ins.push(Instruction::CompareLess(NumberSign::Signed)),
                BinOp::Gt => ins.push(Instruction::CompareGreater(NumberSign::Signed)),
                BinOp::Le => {
                    // CIL 无 cle，以 !(a > b) 实现
                    ins.push(Instruction::CompareGreater(NumberSign::Signed));
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                BinOp::Ge => {
                    // CIL 无 cge，以 !(a < b) 实现
                    ins.push(Instruction::CompareLess(NumberSign::Signed));
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
            }
        }
        Rvalue::Unary { op, operand } => {
            lower_operand(
                operand,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            match op {
                UnaryOp::Neg => ins.push(Instruction::Negate),
                UnaryOp::Not => {
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                UnaryOp::BitNot => ins.push(Instruction::Not),
            }
        }
        Rvalue::Cast { kind, operand } => {
            lower_operand(
                operand,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            match kind {
                CastKind::I64ToF64 => ins.push(Instruction::ConvertFloat64),
                CastKind::F64ToI64 => ins.push(Instruction::call(ctx.runtime_refs.f64_to_i64)),
                CastKind::BoolToI64 => ins.push(Instruction::Convert(ConversionType::Int64)),
            }
        }
        Rvalue::Call { callee, args } => {
            // 动态调用把调用点编号作为第一个实参，运行时据此做有界内联缓存。
            if matches!(
                callee,
                Callee::Runtime(
                    RuntimeFn::CallValue
                        | RuntimeFn::CallMethod
                        | RuntimeFn::DynGetAttr
                        | RuntimeFn::SetAttr
                )
            ) {
                let site = ctx.next_site();
                ins.push(Instruction::LoadConstantInt32(site));
            }
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            let system = &ctx.runtime_refs.system;
            match callee {
                Callee::Runtime(rt_fn) => match rt_fn {
                    RuntimeFn::PrintTarget => {
                        ins.push(Instruction::call(ctx.runtime_refs.system.print_target))
                    }
                    RuntimeFn::WriteI64 => ins.push(Instruction::call(system.write_i64)),
                    RuntimeFn::WriteF64 => ins.push(Instruction::call(system.write_f64)),
                    RuntimeFn::WriteBool => ins.push(Instruction::call(system.write_bool)),
                    RuntimeFn::WriteStr => ins.push(Instruction::call(system.write_str)),
                    RuntimeFn::WriteNewline => ins.push(Instruction::call(system.write_newline)),
                    RuntimeFn::WriteSep => ins.push(Instruction::call(system.write_sep)),
                    RuntimeFn::StrConcat => {
                        ins.push(Instruction::call(ctx.runtime_refs.str_concat))
                    }
                    RuntimeFn::FloorDivI64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.floor_div_i64))
                    }
                    RuntimeFn::ModI64 => ins.push(Instruction::call(ctx.runtime_refs.mod_i64)),
                    RuntimeFn::PowI64 => ins.push(Instruction::call(ctx.runtime_refs.pow_i64)),
                    RuntimeFn::TrueDivF64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.true_div_f64))
                    }
                    RuntimeFn::FloorDivF64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.floor_div_f64))
                    }
                    RuntimeFn::ModF64 => ins.push(Instruction::call(ctx.runtime_refs.mod_f64)),
                    RuntimeFn::PowF64 => ins.push(Instruction::call(ctx.runtime_refs.pow_f64)),
                    RuntimeFn::F64ToI64 => ins.push(Instruction::call(ctx.runtime_refs.f64_to_i64)),
                    RuntimeFn::AbsI64 => ins.push(Instruction::call(ctx.runtime_refs.abs_i64)),
                    RuntimeFn::AbsF64 => ins.push(Instruction::call(ctx.runtime_refs.abs_f64)),
                    RuntimeFn::ShlI64 => ins.push(Instruction::call(ctx.runtime_refs.shl_i64)),
                    RuntimeFn::ShrI64 => ins.push(Instruction::call(ctx.runtime_refs.shr_i64)),
                    RuntimeFn::GeneratorDelegateStart => {
                        ins.push(Instruction::call(ctx.runtime_refs.delegation.start))
                    }
                    RuntimeFn::GeneratorDelegateStep => {
                        ins.push(Instruction::call(ctx.runtime_refs.delegation.step))
                    }
                    RuntimeFn::GeneratorDelegateCurrent => {
                        ins.push(Instruction::call(ctx.runtime_refs.delegation.current))
                    }
                    RuntimeFn::GeneratorDelegateResult => {
                        ins.push(Instruction::call(ctx.runtime_refs.delegation.result))
                    }
                    RuntimeFn::GeneratorInput => {
                        ins.push(Instruction::call(ctx.runtime_refs.generator_input))
                    }
                    RuntimeFn::GeneratorRetry => {
                        ins.push(Instruction::call(ctx.runtime_refs.generator_retry))
                    }
                    RuntimeFn::ArrayGet => ins.push(Instruction::call(ctx.runtime_refs.array_get)),
                    RuntimeFn::ArraySet => ins.push(Instruction::call(ctx.runtime_refs.array_set)),
                    RuntimeFn::CellNew => ins.push(Instruction::call(ctx.runtime_refs.cell_new)),
                    RuntimeFn::CellGet => ins.push(Instruction::call(ctx.runtime_refs.cell_get)),
                    RuntimeFn::CellSet => ins.push(Instruction::call(ctx.runtime_refs.cell_set)),
                    RuntimeFn::CellGetFree => {
                        ins.push(Instruction::call(ctx.runtime_refs.cell_get_free))
                    }
                    RuntimeFn::CallValue => {
                        ins.push(Instruction::call(ctx.runtime_refs.call_value))
                    }
                    RuntimeFn::BuiltinFunction => {
                        ins.push(Instruction::call(ctx.runtime_refs.function_objects.builtin))
                    }
                    RuntimeFn::ClassNamespaceGet => ins.push(Instruction::call(
                        ctx.runtime_refs.function_objects.class_namespace_get,
                    )),
                    RuntimeFn::DeclareDataclass => ins.push(Instruction::call(
                        ctx.runtime_refs.function_objects.declare_dataclass,
                    )),
                    RuntimeFn::ApplyDataclass => ins.push(Instruction::call(
                        ctx.runtime_refs.function_objects.apply_dataclass,
                    )),
                    RuntimeFn::FunctionMetadata => ins.push(Instruction::call(
                        ctx.runtime_refs.function_objects.metadata,
                    )),
                    RuntimeFn::TypeObject => {
                        ins.push(Instruction::call(ctx.runtime_refs.type_object))
                    }
                    RuntimeFn::ArgError => ins.push(Instruction::call(ctx.runtime_refs.arg_error)),
                    RuntimeFn::StarArg => ins.push(Instruction::call(ctx.runtime_refs.star_arg)),
                    RuntimeFn::FormatValue => {
                        ins.push(Instruction::call(ctx.runtime_refs.format_value))
                    }
                    RuntimeFn::WriteObject => ins.push(Instruction::call(system.write_object)),
                    RuntimeFn::DynBinary => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_binary))
                    }
                    RuntimeFn::DynCompare => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_compare))
                    }
                    RuntimeFn::DynUnary => ins.push(Instruction::call(ctx.runtime_refs.dyn_unary)),
                    RuntimeFn::DynTruthy => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_truthy))
                    }
                    RuntimeFn::DynNone => ins.push(Instruction::call(ctx.runtime_refs.dyn_none)),
                    RuntimeFn::DynFromDecimal => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_from_decimal))
                    }
                    RuntimeFn::DynToInt => ins.push(Instruction::call(ctx.runtime_refs.dyn_to_int)),
                    RuntimeFn::DynToFloat => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_to_float))
                    }
                    RuntimeFn::DynAbs => ins.push(Instruction::call(ctx.runtime_refs.dyn_abs)),
                    RuntimeFn::MakeException => {
                        ins.push(Instruction::call(ctx.runtime_refs.make_exception))
                    }
                    RuntimeFn::WithCause => {
                        ins.push(Instruction::call(ctx.runtime_refs.with_cause))
                    }
                    RuntimeFn::RangeStep => {
                        ins.push(Instruction::call(ctx.runtime_refs.range_step))
                    }
                    RuntimeFn::HandlerEnter => {
                        ins.push(Instruction::call(ctx.runtime_refs.handler_enter))
                    }
                    RuntimeFn::HandlerLeave => {
                        ins.push(Instruction::call(ctx.runtime_refs.handler_leave))
                    }
                    RuntimeFn::Reraise => ins.push(Instruction::call(ctx.runtime_refs.reraise)),
                    RuntimeFn::RaisePrepare => {
                        ins.push(Instruction::call(ctx.runtime_refs.raise_prepare))
                    }
                    RuntimeFn::MakeUnbound => {
                        ins.push(Instruction::call(ctx.runtime_refs.make_unbound))
                    }
                    RuntimeFn::TracePush => {
                        ins.push(Instruction::call(ctx.runtime_refs.trace_push))
                    }
                    RuntimeFn::TraceLine => {
                        ins.push(Instruction::call(ctx.runtime_refs.trace_line))
                    }
                    RuntimeFn::TracePopTo => {
                        ins.push(Instruction::call(ctx.runtime_refs.trace_pop_to))
                    }
                    RuntimeFn::DynStr => ins.push(Instruction::call(ctx.runtime_refs.dyn_str)),
                    RuntimeFn::DynType => ins.push(Instruction::call(ctx.runtime_refs.dyn_type)),
                    RuntimeFn::DynGetAttr => {
                        ins.push(Instruction::call(ctx.runtime_refs.pic_get_attr))
                    }
                    RuntimeFn::SetAttr => {
                        ins.push(Instruction::call(ctx.runtime_refs.pic_set_attr))
                    }
                    RuntimeFn::LoadChecked => {
                        ins.push(Instruction::call(ctx.runtime_refs.load_checked))
                    }
                    RuntimeFn::LoadName => ins.push(Instruction::call(ctx.runtime_refs.load_name)),
                    RuntimeFn::ImportModule => {
                        ins.push(Instruction::call(ctx.runtime_refs.import_module))
                    }
                    RuntimeFn::ImportAttr => {
                        ins.push(Instruction::call(ctx.runtime_refs.import_attr))
                    }
                    RuntimeFn::CallMethod => {
                        ins.push(Instruction::call(ctx.runtime_refs.call_method))
                    }
                    // 容器相关的调用与 CallWithArgs 走同一张表
                    RuntimeFn::MakeList
                    | RuntimeFn::MakeTuple
                    | RuntimeFn::MakeSet
                    | RuntimeFn::MakeDict
                    | RuntimeFn::MakeSlice
                    | RuntimeFn::MakeBytes
                    | RuntimeFn::GetItem
                    | RuntimeFn::SetItem
                    | RuntimeFn::DelItem
                    | RuntimeFn::Contains
                    | RuntimeFn::Len
                    | RuntimeFn::IsSame
                    | RuntimeFn::ReprValue
                    | RuntimeFn::DynBuiltin
                    | RuntimeFn::PrepareMethod
                    | RuntimeFn::SetAdd
                    | RuntimeFn::SetExtend
                    | RuntimeFn::ListAppend
                    | RuntimeFn::ListExtend
                    | RuntimeFn::UnpackCheck
                    | RuntimeFn::IterStart
                    | RuntimeFn::IterNextOk
                    | RuntimeFn::IterValue
                    | RuntimeFn::MakeClass
                    | RuntimeFn::DelAttr
                    | RuntimeFn::Decorate => {
                        ins.push(Instruction::call(runtime_method(*rt_fn, ctx.runtime_refs)))
                    }
                    RuntimeFn::TraceAttach => {
                        // M2.2 用 PrepareRaise 合并了「挂栈帧 + 挂上下文」，这里不再单独发射
                        ins.push(Instruction::NoOperation);
                    }
                    RuntimeFn::CastToI64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_i64))
                    }
                    RuntimeFn::CastToF64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_f64))
                    }
                    RuntimeFn::CastToBool => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_bool))
                    }
                    RuntimeFn::CastToStr => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_str))
                    }
                },
                Callee::Fn(fn_id) => {
                    let method_idx = ctx.fn_map[fn_id];
                    ins.push(Instruction::call(method_idx));
                }
            }
        }
        Rvalue::NewObject { class, args } => {
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            let ctor = ctx.class_info[class].ctor;
            ins.push(Instruction::NewObject(ctor.into()));
        }
        Rvalue::Box { value, from } => {
            // 数值字面量只装箱一次：模块初始化时存进静态字段，循环里直接读。
            if let Operand::Const(constant) = value
                && let Some(field) = boxed_literal_key(constant, from)
                    .and_then(|key| ctx.runtime_refs.literal_fields.get(&key).copied())
            {
                ins.push(Instruction::LoadStaticField {
                    volatile: false,
                    param0: FieldSource::Definition(field),
                });
                return;
            }
            lower_operand(
                value,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            emit_box(ins, from, ctx.runtime_refs, ctx.class_types);
        }
        Rvalue::Unbox { value, to } => {
            lower_operand(
                value,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            match to {
                ryupy_mir::MirType::Class(_) => {
                    // 引用类型直接 castclass（dataclass 实例）。
                    if let Some(mt) = crate::types::mir_type_to_method_type(to, ctx.class_types) {
                        ins.push(Instruction::CastClass {
                            skip_type_check: false,
                            param0: mt,
                        });
                    }
                }
                _ => {
                    let cast = match to {
                        ryupy_mir::MirType::I64 => ctx.runtime_refs.cast_to_i64,
                        ryupy_mir::MirType::F64 => ctx.runtime_refs.cast_to_f64,
                        ryupy_mir::MirType::Bool => ctx.runtime_refs.cast_to_bool,
                        ryupy_mir::MirType::Str => ctx.runtime_refs.cast_to_str,
                        _ => ctx.runtime_refs.cast_to_i64,
                    };
                    ins.push(Instruction::call(cast));
                }
            }
        }
        Rvalue::DynBinary {
            op,
            lhs,
            rhs,
            inplace,
        } => {
            let site = ctx.next_site();
            ins.push(Instruction::LoadConstantInt32(site));
            // 增量赋值把标记位并进操作码，运行时据此选择原地路径（ABI 见 PyOps.InPlaceFlag）
            let code = if *inplace {
                op.code() | ryupy_mir::DynOp::INPLACE_FLAG
            } else {
                op.code()
            };
            ins.push(Instruction::LoadConstantInt32(code));
            lower_operand(lhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            lower_operand(rhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            ins.push(Instruction::call(ctx.runtime_refs.pic_binary));
        }
        Rvalue::DynCompare { op, lhs, rhs } => {
            ins.push(Instruction::LoadConstantInt32(op.code()));
            lower_operand(lhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            lower_operand(rhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            ins.push(Instruction::call(ctx.runtime_refs.dyn_compare));
        }
        Rvalue::DynUnary { op, operand } => {
            ins.push(Instruction::LoadConstantInt32(op.code()));
            lower_operand(
                operand,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            ins.push(Instruction::call(ctx.runtime_refs.dyn_unary));
        }
        Rvalue::DynTruthy { value } => {
            lower_operand(
                value,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            ins.push(Instruction::call(ctx.runtime_refs.dyn_truthy));
        }
        Rvalue::DynCallValue { callee, args } => {
            let site = ctx.next_site();
            ins.push(Instruction::LoadConstantInt32(site));
            lower_operand(
                callee,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            ins.push(Instruction::call(
                ctx.runtime_refs.pic_call_value[args.len()],
            ));
        }
        Rvalue::DynCallMethod { obj, name, args } => {
            let site = ctx.next_site();
            ins.push(Instruction::LoadConstantInt32(site));
            lower_operand(obj, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            ins.push(Instruction::load_string(name.clone()));
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            ins.push(Instruction::call(
                ctx.runtime_refs.pic_call_method[args.len()],
            ));
        }
        Rvalue::GeneratorDelegateStep { .. } => {
            // MIR验证器应先拒绝残留高层节点，后端不猜测Python委托语义。
            ins.push(Instruction::LoadNull);
            ins.push(Instruction::Throw);
        }
        Rvalue::MakeGenerator { resume, frame } => {
            let function = &ctx.functions[*resume];
            ins.push(Instruction::LoadNull);
            ins.push(Instruction::LoadMethodPointer(ctx.fn_map[resume].into()));
            ins.push(Instruction::NewObject(UserMethod::Reference(
                ctx.runtime_refs.generator_resume_ctor,
            )));
            lower_object_array(frame, ins, ctx);
            ins.push(Instruction::load_string(function.source_file.clone()));
            ins.push(Instruction::load_string(function.name.clone()));
            ins.push(Instruction::NewObject(UserMethod::Reference(
                ctx.runtime_refs.generator_ctor,
            )));
        }
        Rvalue::MakeFunction {
            func,
            env,
            defaults,
        } => {
            // new PyFunction(name, sig, new PyCall(null, &thunk), defaults, usesSuper, env)
            // 栈序 = 构造参数序；两个数组在构造前分别 newarr + stelem。
            let nested = &ctx.functions[*func];
            let signature_field = match nested.signature_global {
                Some(global) => ctx.runtime_refs.global_fields[&global],
                None => panic!("嵌套函数 {} 缺少签名槽位", nested.name),
            };
            ins.push(Instruction::load_string(nested.name.clone()));
            ins.push(Instruction::LoadStaticField {
                volatile: false,
                param0: FieldSource::Definition(signature_field),
            });
            ins.push(Instruction::LoadNull);
            let thunk = ctx
                .thunks
                .get(func)
                .copied()
                .unwrap_or_else(|| panic!("函数 {} 缺少 thunk", nested.name));
            ins.push(Instruction::LoadMethodPointer(thunk.into()));
            ins.push(Instruction::NewObject(UserMethod::Reference(
                ctx.runtime_refs.py_call_ctor,
            )));
            lower_object_array(defaults, ins, ctx);
            ins.push(Instruction::LoadConstantInt32(
                if crate::callables::function_uses_super(nested) {
                    1
                } else {
                    0
                },
            ));
            if env.is_empty() {
                ins.push(Instruction::LoadNull);
            } else {
                lower_object_array(env, ins, ctx);
            }
            ins.push(Instruction::NewObject(UserMethod::Reference(
                ctx.runtime_refs.py_function_ctor_env,
            )));
        }
        Rvalue::CallWithArgs {
            callee,
            prefix,
            args,
        } => {
            let is_method_call = matches!(callee, RuntimeFn::CallMethod);
            if is_method_call {
                let site = ctx.next_site();
                ins.push(Instruction::LoadConstantInt32(site));
            }
            for op in prefix {
                lower_operand(op, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            lower_object_array(args, ins, ctx);
            let method = runtime_method(*callee, ctx.runtime_refs);
            ins.push(Instruction::call(method));
        }
        Rvalue::CatchException => {
            // 异常对象由 CLR 在进入 handler 时压栈，这里不发射任何取值指令，
            // 随后的 `stloc` 直接把它存进局部变量。
        }
        Rvalue::GetField { obj, field } => {
            lower_operand(obj, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            let class_id = match obj {
                Operand::Local(loc) => match ctx.locals[*loc].ty {
                    ryupy_mir::MirType::Class(id) => id,
                    _ => panic!("GetField obj 不是 Class 类型"),
                },
                _ => panic!("GetField obj 必须是局部变量操作数"),
            };
            let field_idx = ctx.class_info[&class_id].fields[*field as usize];
            ins.push(Instruction::LoadField {
                unaligned: None,
                volatile: false,
                param0: field_idx.into(),
            });
        }
    }
}
