//! 缺省补齐后的冷通用入口；不改变原完整位置thunk，也不创建第二份PyFunction。
use crate::stackify_refs::RuntimeRefs;
use dotnetdll::prelude::*;

/// Binder已经成功，数量少于Python形参才可能由默认值补齐；错误不会先执行函数体。
pub(crate) fn emit_missing_dispatch(
    ins: &mut Vec<Instruction>,
    generic: MethodIndex,
    total: usize,
    offset: u16,
    refs: &RuntimeRefs,
) {
    ins.extend([
        Instruction::LoadArgument(1 + offset),
        Instruction::LoadLength,
        Instruction::Convert(ConversionType::Int32),
        Instruction::LoadArgument(2 + offset),
        Instruction::LoadLength,
        Instruction::Convert(ConversionType::Int32),
        Instruction::Add,
        Instruction::LoadConstantInt32(total as i32),
    ]);
    let complete = ins.len();
    ins.push(Instruction::BranchGreaterOrEqual(NumberSign::Signed, 0));
    emit_bound_call(ins, generic, total, offset, refs);
    ins[complete] = Instruction::BranchGreaterOrEqual(NumberSign::Signed, ins.len());
}

fn emit_bound_call(
    ins: &mut Vec<Instruction>,
    generic: MethodIndex,
    total: usize,
    offset: u16,
    refs: &RuntimeRefs,
) {
    for index in 0..total {
        ins.extend([
            Instruction::LoadLocal(0),
            Instruction::LoadConstantInt32(index as i32),
            Instruction::LoadElement {
                skip_range_check: false,
                skip_null_check: false,
                param0: refs.object_type.clone(),
            },
        ]);
    }
    ins.extend([
        Instruction::LoadArgument(offset),
        Instruction::call(generic),
        Instruction::Return,
    ]);
}

/// generic体向兄弟/递归函数传值时用此入口；一律保留对象表示，绑定共用canonical签名和defaults。
pub(crate) fn emit_thunk(
    res: &mut Resolution<'static>,
    module: TypeIndex,
    func: &ryupy_mir::Function,
    generic: MethodIndex,
    refs: &RuntimeRefs,
    index: usize,
) -> MethodIndex {
    let object = refs.object_type.clone();
    let array: MethodType = BaseType::vector(object.clone()).into();
    let strings: MethodType = BaseType::vector(BaseType::String.into()).into();
    let signature = MethodSignature::new(
        false,
        ReturnType::value(object.clone()),
        vec![
            Parameter::value(refs.py_function_type.clone()),
            Parameter::value(array.clone()),
            Parameter::value(strings),
            Parameter::value(array.clone()),
        ],
    );
    let mut ins = vec![
        Instruction::LoadStaticField {
            volatile: false,
            param0: refs.global_fields[&func.signature_global.expect("canonical signature")].into(),
        },
        Instruction::LoadArgument(1),
        Instruction::LoadArgument(2),
        Instruction::LoadArgument(3),
        Instruction::LoadArgument(0),
        Instruction::call(refs.py_function_get_defaults),
        Instruction::call(refs.bind_args),
        Instruction::StoreLocal(0),
    ];
    emit_bound_call(&mut ins, generic, func.param_meta.len(), 0, refs);
    let mut body = body::Method::with_locals(vec![LocalVariable::new(array)], ins);
    body.header.maximum_stack_size = func.param_meta.len() + 8;
    res.push_method(
        module,
        Method::new(
            Accessibility::Private,
            signature,
            format!("__generic_thunk_{index}"),
            Some(body),
        ),
    )
}

/// 注册键为实际刚创建的函数，静态委托无捕获；栈上仍保留同一函数供正常发布。
pub(crate) fn emit_register(ins: &mut Vec<Instruction>, generic: MethodIndex, refs: &RuntimeRefs) {
    ins.extend([
        Instruction::Duplicate,
        Instruction::LoadNull,
        Instruction::LoadMethodPointer(generic.into()),
        Instruction::NewObject(UserMethod::Reference(refs.py_call_ctor)),
        Instruction::call(refs.function_objects.generic_register),
    ]);
}

/// 全部方法ID已预分配后才建立入口，伴生体只登记冷thunk、不发布独立元数据。
pub(crate) fn emit_all_thunks(
    res: &mut Resolution<'static>,
    module_class: TypeIndex,
    program: &ryupy_mir::LinkedProgram,
    fn_indices: &rustc_hash::FxHashMap<ryupy_mir::FnId, MethodIndex>,
    runtime_refs: &RuntimeRefs,
    class_types: &crate::types::ClassTypeMap,
) -> rustc_hash::FxHashMap<ryupy_mir::FnId, MethodIndex> {
    let mut thunks: rustc_hash::FxHashMap<ryupy_mir::FnId, MethodIndex> =
        rustc_hash::FxHashMap::default();
    let mut thunk_index = 0usize;
    for (fn_id, func) in program.functions.iter() {
        if func.module_state_source == Some(ryupy_mir::InstanceStateSource::StockThinLease) {
            let adapter = crate::stock_thin::adapter(
                res,
                module_class,
                fn_indices[&fn_id],
                runtime_refs.stock_thin.as_ref().expect("薄函数ABI"),
                thunk_index,
            );
            thunks.insert(fn_id, adapter);
            thunk_index += 1;
            continue;
        }
        if func.signature_global.is_none() {
            continue;
        }
        let thunk = crate::callables::emit_thunk(
            res,
            module_class,
            func,
            (
                fn_indices[&fn_id],
                match func.core {
                    ryupy_mir::FunctionCore::TypedWithGeneric { generic } => {
                        Some(fn_indices[&generic])
                    }
                    _ => None,
                },
            ),
            runtime_refs,
            class_types,
            thunk_index,
        );
        thunks.insert(fn_id, thunk);
        if let ryupy_mir::FunctionCore::TypedWithGeneric { generic } = func.core {
            thunks.insert(
                generic,
                emit_thunk(
                    res,
                    module_class,
                    func,
                    fn_indices[&generic],
                    runtime_refs,
                    thunk_index,
                ),
            );
        }
        thunk_index += 1;
    }

    thunks
}
