//! 实际def执行点的函数构造；签名/defaults/env只求值一次，伴生体复用该canonical身份。
use crate::stackify::LowerCtx;
use crate::stackify_operands::{lower_object_array, lower_operand};
use dotnetdll::prelude::*;
use ryupy_mir::{FnId, Operand};
pub(crate) fn emit(
    func: FnId,
    instance_context: Option<&Operand>,
    env: &[Operand],
    defaults: &[Operand],
    ins: &mut Vec<Instruction>,
    ctx: &LowerCtx<'_>,
) {
    // new PyFunction(name, sig, new PyCall(null, &thunk), defaults, usesSuper, env)
    // 栈序 = 构造参数序；两个数组在构造前分别 newarr + stelem。
    let nested = &ctx.functions[func];
    let signature_field = match nested.signature_global {
        Some(global) => ctx.runtime_refs.global_fields[&global],
        None => panic!("嵌套函数 {} 缺少签名槽位", nested.name),
    };
    if let Some(context) = instance_context {
        lower_operand(
            context,
            ins,
            ctx.param_slots,
            ctx.local_slots,
            ctx.runtime_refs,
        );
    }
    ins.push(Instruction::load_string(nested.name.clone()));
    ins.push(Instruction::LoadStaticField {
        volatile: false,
        param0: FieldSource::Definition(signature_field),
    });
    if instance_context.is_some() {
        let owned = ctx
            .runtime_refs
            .owned_functions
            .as_ref()
            .expect("已验证实例函数ABI");
        ins.push(Instruction::LoadStaticField {
            volatile: false,
            param0: owned.delegates[&func].into(),
        });
    } else {
        ins.push(Instruction::LoadNull);
        let thunk = ctx
            .thunks
            .get(&func)
            .copied()
            .unwrap_or_else(|| panic!("函数 {} 缺少 thunk", nested.name));
        ins.push(Instruction::LoadMethodPointer(thunk.into()));
        ins.push(Instruction::NewObject(UserMethod::Reference(
            ctx.runtime_refs.py_call_ctor,
        )));
    }
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
    if instance_context.is_some() {
        ins.push(Instruction::call(
            ctx.runtime_refs
                .owned_functions
                .as_ref()
                .expect("已验证实例函数ABI")
                .create,
        ));
    } else {
        ins.push(Instruction::NewObject(UserMethod::Reference(
            ctx.runtime_refs.py_function_ctor_env,
        )));
    }
    if let Some(kind) = nested
        .definition
        .as_ref()
        .and_then(|definition| definition.introspection_kind)
    {
        // 保留同一个新函数在栈顶；登记消费Duplicate，不重建function/defaults/env。
        ins.push(Instruction::Duplicate);
        ins.push(Instruction::LoadConstantInt64(kind as i64));
        ins.push(Instruction::call(
            ctx.runtime_refs.function_objects.introspection_kind,
        ));
    }
    if let Some(identity) = nested.specialization_identity_global {
        // 私有特化锚点只指向本次实际创建的原对象，不读取或覆盖公开函数/defaults槽。
        ins.push(Instruction::Duplicate);
        ins.push(Instruction::StoreStaticField {
            volatile: false,
            param0: ctx.runtime_refs.global_fields[&identity].into(),
        });
    }
    if let ryupy_mir::FunctionCore::TypedWithGeneric { generic } = nested.core {
        crate::generic_callables::emit_register(ins, ctx.thunks[&generic], ctx.runtime_refs);
    }
}
