//! 固定绑定的机械CIL：转换在受保护调用之前，窄loader catch之后仅BoxInt物化。
use dotnetdll::prelude::*;
use ryupy_mir::LinkedProgram;

use crate::fixed_binding_refs::BindingRefs;
use crate::fixed_bindings::FixedI32Binding;
use crate::stackify::RuntimeRefs;

/// 先构造全部一等函数，再发布所有槽；创建失败时不会留下部分可见导出。
pub(crate) fn initializer<'a>(
    res: &mut Resolution<'static>,
    class: TypeIndex,
    program: &LinkedProgram,
    bindings: &'a [FixedI32Binding],
    refs: &BindingRefs,
    runtime: &RuntimeRefs,
) -> (
    body::Method,
    Vec<(&'a FixedI32Binding, MethodIndex, MethodIndex)>,
) {
    let mut methods = Vec::new();
    let mut instructions = Vec::new();
    for (index, binding) in bindings.iter().enumerate() {
        let (creation, wrapper, native) = function(res, class, program, binding, refs, runtime);
        methods.push((binding, wrapper, native));
        instructions.extend(creation);
        if bindings.len() == 1 {
            instructions.push(Instruction::StoreStaticField {
                volatile: false,
                param0: runtime.global_fields[&binding.export].into(),
            });
        } else {
            instructions.push(Instruction::StoreLocal(index as u16));
        }
    }
    if bindings.len() > 1 {
        for (index, binding) in bindings.iter().enumerate() {
            instructions.extend([
                Instruction::LoadLocal(index as u16),
                Instruction::StoreStaticField {
                    volatile: false,
                    param0: runtime.global_fields[&binding.export].into(),
                },
            ]);
        }
    }
    instructions.push(Instruction::Return);
    let mut init = if bindings.len() == 1 {
        body::Method::new(instructions)
    } else {
        body::Method::with_locals(
            vec![LocalVariable::new(ctype! { object }); bindings.len()],
            instructions,
        )
    };
    init.header.maximum_stack_size = 3;
    (init, methods)
}

fn function(
    res: &mut Resolution<'static>,
    class: TypeIndex,
    program: &LinkedProgram,
    binding: &FixedI32Binding,
    refs: &BindingRefs,
    runtime: &RuntimeRefs,
) -> (Vec<Instruction>, MethodIndex, MethodIndex) {
    let module = &program.modules[binding.module];
    let export_name = &module
        .exports
        .iter()
        .find(|e| e.value == binding.export)
        .expect("validated export global")
        .name;
    // 同型方法须使用唯一名称，否则ILC/元数据消费者无法区分签名相同的多个导出。
    let method_name = |kind: &str| {
        if module.exports.len() == 1 {
            format!("__fixed_i32_{kind}")
        } else {
            format!(
                "__fixed_i32_{kind}_{}",
                binding.export.into_raw().into_u32()
            )
        }
    };
    let scope = res.push_module_reference(ExternalModuleReference::new(binding.module_ref.clone()));
    let mut import = PInvoke::new(scope, binding.entry_point.clone());
    import.no_mangle = true;
    import.calling_convention = UnmanagedCallingConvention::Cdecl;
    import.character_set = CharacterSet::NotSpecified;
    import.supports_last_error = false;
    let mut method = Method::new(
        Accessibility::Private,
        msig! { static long (int, int) },
        method_name("native"),
        None,
    );
    method.pinvoke = Some(import);
    method.preserve_sig = true;
    let native = res.push_method(class, method);

    let mut ins = Vec::new();
    for (index, parameter) in ["left", "right"].into_iter().enumerate() {
        ins.extend([
            Instruction::LoadArgument(0),
            Instruction::LoadConstantInt32(index as i32),
            Instruction::LoadElement {
                skip_range_check: false,
                skip_null_check: false,
                param0: ctype! { object },
            },
            Instruction::load_string(export_name),
            Instruction::load_string(parameter),
            Instruction::call(refs.checked),
            Instruction::StoreLocal(index as u16),
        ]);
    }
    let start = ins.len();
    ins.extend([
        Instruction::LoadLocal(0),
        Instruction::LoadLocal(1),
        Instruction::call(native),
        Instruction::StoreLocal(2),
        Instruction::Leave(0),
    ]);
    let try_len = ins.len() - start;
    let mut clauses = Vec::new();
    for error in &refs.loader_errors {
        let handler = ins.len();
        // catch栈上的异常先于两个逻辑名；不公开CLR消息中的绝对宿主路径。
        ins.extend([
            Instruction::load_string(binding.module_ref.clone()),
            Instruction::load_string(binding.entry_point.clone()),
            Instruction::call(refs.failure),
            Instruction::Throw,
        ]);
        clauses.push(body::Exception {
            kind: body::ExceptionKind::TypedException(error.clone()),
            try_offset: start,
            try_length: try_len,
            handler_offset: handler,
            handler_length: ins.len() - handler,
        });
    }
    ins[start + try_len - 1] = Instruction::Leave(ins.len());
    ins.extend([
        Instruction::LoadLocal(2),
        Instruction::call(runtime.box_i64),
        Instruction::Return,
    ]);
    let mut wrapper_body = body::Method::with_locals(
        vec![
            LocalVariable::new(ctype! { int }),
            LocalVariable::new(ctype! { int }),
            LocalVariable::new(ctype! { long }),
        ],
        ins,
    );
    wrapper_body.header.maximum_stack_size = 3;
    wrapper_body.data_sections = vec![body::DataSection::ExceptionHandlers(clauses)];
    let wrapper = res.push_method(
        class,
        Method::new(
            Accessibility::Private,
            msig! { static object (object[]) },
            method_name("body"),
            Some(wrapper_body),
        ),
    );
    // 仅构造函数对象；公开槽写入由整个模块初始化器统一安排。
    let creation = vec![
        Instruction::load_string(export_name),
        Instruction::LoadNull,
        Instruction::LoadMethodPointer(MethodSource::User(UserMethod::Definition(wrapper))),
        Instruction::NewObject(UserMethod::Reference(refs.delegate_ctor)),
        Instruction::call(refs.create),
    ];
    (creation, wrapper, native)
}
