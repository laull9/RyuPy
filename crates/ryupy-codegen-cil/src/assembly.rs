//! 组装并序列化程序集。
//!
//! 生成的程序集结构：
//!
//! ```text
//! .assembly <name>
//! .assembly extern System.Runtime
//! .assembly extern System.Console
//! .assembly extern RyuPy.Runtime
//! .class public abstract sealed RyuPy.Generated.Module      // 所有顶层函数都是它的静态方法
//!     .method public static void Main(string[] args) { .entrypoint ... }
//!     .method public static int64 fib(int64 n) { ... }
//! .class public RyuPy.Generated.<DataclassName>               // 每个 @dataclass 一个
//! ```

use std::borrow::Cow;

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{FnId, LinkedProgram};

use crate::EmitOptions;
use crate::entry::emit_entry_wrapper;
use crate::literals::{collect_literal_inits, emit_init_class_factories, emit_literal_init};
use crate::refs::{RefExtras, build_runtime_refs};
use crate::stackify::{ClassCodegenInfo, ClassInfoMap, lower_function};
use crate::types::ClassTypeMap;
pub(crate) fn build_resolution(
    program: &LinkedProgram,
    opts: &EmitOptions,
) -> (Resolution<'static>, TypeIndex) {
    let (res, module, _, _) = build_resolution_core(program, opts, false, &[], None);
    (res, module)
}
pub(crate) fn build_resolution_with_report(
    program: &LinkedProgram,
    opts: &EmitOptions,
) -> (Resolution<'static>, TypeIndex, crate::EmissionReport) {
    let (res, module, report, _) = build_resolution_core(program, opts, true, &[], None);
    (res, module, report.expect("requested emission report"))
}
/// 调用者必须先验证sidecar；普通发射仍传空集合且不建立固定绑定引用。
pub(crate) fn build_resolution_with_bindings(
    program: &LinkedProgram,
    opts: &EmitOptions,
    bindings: &[crate::fixed_bindings::FixedI32Binding],
) -> (Resolution<'static>, TypeIndex) {
    let (res, module, _, _) = build_resolution_core(program, opts, false, bindings, None);
    (res, module)
}
pub(crate) fn build_binding_report(
    program: &LinkedProgram,
    opts: &EmitOptions,
    bindings: &[crate::fixed_bindings::FixedI32Binding],
    deployment: &crate::fixed_bindings::BindingDeployment,
) -> (
    Resolution<'static>,
    crate::EmissionReport,
    Vec<crate::fixed_bindings::BindingEmission>,
) {
    let (res, _, report, records) =
        build_resolution_core(program, opts, true, bindings, Some(deployment));
    (res, report.expect("requested binding report"), records)
}

fn build_resolution_core(
    program: &LinkedProgram,
    opts: &EmitOptions,
    report_requested: bool,
    bindings: &[crate::fixed_bindings::FixedI32Binding],
    deployment: Option<&crate::fixed_bindings::BindingDeployment>,
) -> (
    Resolution<'static>,
    TypeIndex,
    Option<crate::EmissionReport>,
    Vec<crate::fixed_bindings::BindingEmission>,
) {
    let dll_name = format!("{}.dll", opts.assembly_name);
    let mut res = Resolution::new(Module::new(dll_name));
    res.assembly = Some(Assembly::new(opts.assembly_name.clone()));

    let (mut runtime_refs, extras) = build_runtime_refs(&mut res);
    crate::program_refs::initialize(&mut res, program, extras.ryupy_runtime, &mut runtime_refs);
    let RefExtras {
        object_ref,
        object_ctor_ref,
        py_signature_ref,
        py_exc_ref,
        py_exc_type,
        exception_type_refs,
        sys_runtime,
        ryupy_runtime,
        set_file,
        traceback_type,
        report_unhandled,
        report_clr,
        clr_exc_type,
    } = extras;

    let binding_refs = (!bindings.is_empty()).then(|| {
        crate::fixed_binding_refs::BindingRefs::build(&mut res, sys_runtime, ryupy_runtime)
    });

    let module_class = res.push_type_definition(TypeDefinition::new(
        Some("RyuPy.Generated".into()),
        "Module",
    ));
    res[module_class].set_extends(object_ref);
    res[module_class].flags.abstract_type = true;
    res[module_class].flags.sealed = true;
    res[module_class].flags.accessibility = TypeAccessibility::Public;

    // 1. 为 dataclass 与用户异常类分配 CLR 类型。
    //
    // 普通 class 不生成 CLR 类型：它由运行时 `PyClass` / `PyInstance` 承载。
    // 用户异常类必须生成 CLR 子类，CIL 的类型化 catch 才能按 Python 异常继承树匹配。
    let mut class_types = ClassTypeMap::new(
        object_ref,
        runtime_refs.vectors.as_ref().map(|refs| refs.ty),
    );
    let mut class_defs: Vec<(ryupy_mir::ClassDefId, TypeIndex)> = Vec::new();
    for (cls_id, cls) in program.classes.iter() {
        if !cls.is_dataclass {
            continue;
        }
        let type_idx = res.push_type_definition(TypeDefinition::new(
            Some("RyuPy.Generated".into()),
            cls.codegen_name.clone(),
        ));
        class_types
            .classes
            .insert(cls_id, (type_idx, cls.is_value_type));
        class_defs.push((cls_id, type_idx));
    }

    // 模块静态字段：函数对象 / 默认值数组 / 签名 / 模块变量。
    // 必须在 dataclass 类型分配之后：模块变量可能持有 dataclass 实例（静态 Class 槽位）。
    let object_member: MemberType = BaseType::class(object_ref).into();
    let signature_member: MemberType = BaseType::class(py_signature_ref).into();
    let global_fields = crate::callables::allocate_global_fields(
        &mut res,
        module_class,
        program,
        &class_types,
        &object_member,
        &signature_member,
    );
    runtime_refs.global_fields = global_fields;

    // 动态数值字面量池：循环里的 `1`、`0.5` 等只装箱一次。
    let literal_inits = collect_literal_inits(program);
    let (literal_fields, init_literals) = emit_literal_init(
        &mut res,
        module_class,
        &literal_inits,
        &object_member,
        &runtime_refs,
    );
    runtime_refs.literal_fields = literal_fields;

    // 2. dataclass：extends、字段与 .ctor。
    //
    // ADR-0009 之后 frozen dataclass 也是引用类型：值拷贝会破坏对象身份。
    let mut class_info: ClassInfoMap = FxHashMap::default();
    for &(cls_id, type_idx) in &class_defs {
        let cls = &program.classes[cls_id];
        res[type_idx].set_extends(object_ref);
        res[type_idx].flags.sealed = true;
        res[type_idx].flags.accessibility = TypeAccessibility::Public;

        let mut field_indices = Vec::new();
        for (field_name, field_ty) in &cls.fields {
            let member_ty = crate::types::mir_type_to_member_type(field_ty, &class_types).unwrap();
            let field_idx = res.push_field(
                type_idx,
                Field::instance(Accessibility::Public, field_name.clone(), member_ty),
            );
            field_indices.push(field_idx);
        }

        let ctor_params: Vec<Parameter<MethodType>> = cls
            .fields
            .iter()
            .map(|(_, field_ty)| {
                crate::types::mir_type_to_parameter(field_ty, &class_types).unwrap()
            })
            .collect();
        let ctor_sig = MethodSignature::instance(ReturnType::VOID, ctor_params);
        let ctor_method_idx = res.push_method(
            type_idx,
            Method::new(Accessibility::Public, ctor_sig, ".ctor", None),
        );
        res[ctor_method_idx].special_name = true;
        res[ctor_method_idx].runtime_special_name = true;

        let mut ctor_instructions = vec![
            Instruction::LoadArgument(0),
            Instruction::call(object_ctor_ref),
        ];
        for (i, &f_idx) in field_indices.iter().enumerate() {
            ctor_instructions.push(Instruction::LoadArgument(0));
            ctor_instructions.push(Instruction::LoadArgument((i + 1) as u16));
            ctor_instructions.push(Instruction::StoreField {
                unaligned: None,
                volatile: false,
                param0: f_idx.into(),
            });
        }
        ctor_instructions.push(Instruction::Return);
        let mut ctor_body = body::Method::new(ctor_instructions);
        ctor_body.header.maximum_stack_size = 8;
        res[ctor_method_idx].body = Some(ctor_body);

        class_info.insert(
            cls_id,
            ClassCodegenInfo {
                ctor: ctor_method_idx,
                fields: field_indices,
            },
        );
    }

    // 3. 用户异常类：先分配类型，再配置继承与构造函数。
    let mut exception_type_indices: FxHashMap<ryupy_mir::ClassDefId, TypeIndex> =
        FxHashMap::default();
    for (cls_id, cls) in program.classes.iter() {
        if !cls.is_exception || cls.is_dataclass {
            continue;
        }
        let type_idx = res.push_type_definition(TypeDefinition::new(
            Some("RyuPy.Generated".into()),
            format!("_{}", cls.codegen_name),
        ));
        exception_type_indices.insert(cls_id, type_idx);
        // 用户异常采用链接后的内部限定名，不能覆盖内建或其它模块的同名类型。
        runtime_refs
            .exception_types
            .insert(cls.codegen_name.clone(), BaseType::class(type_idx).into());
    }

    let object_array: MethodType = BaseType::vector(BaseType::class(object_ref).into()).into();
    let string_type: MethodType = BaseType::String.into();
    let pyclass_mt = runtime_refs.py_class_type.clone();
    let py_exc_mt: MethodType = BaseType::class(py_exc_ref).into();
    let mut exception_ctors: FxHashMap<ryupy_mir::ClassDefId, MethodIndex> = FxHashMap::default();

    for (cls_id, cls) in program.classes.iter() {
        if !cls.is_exception || cls.is_dataclass {
            continue;
        }
        let type_idx = exception_type_indices[&cls_id];
        res[type_idx].flags.accessibility = TypeAccessibility::Public;
        res[type_idx].flags.sealed = false;

        let ctor_sig = MethodSignature::instance(
            ReturnType::VOID,
            vec![
                Parameter::value(pyclass_mt.clone()),
                Parameter::value(object_array.clone()),
                Parameter::value(string_type.clone()),
            ],
        );
        let ctor_idx = res.push_method(
            type_idx,
            Method::new(Accessibility::Public, ctor_sig, ".ctor", None),
        );
        res[ctor_idx].special_name = true;
        res[ctor_idx].runtime_special_name = true;
        exception_ctors.insert(cls_id, ctor_idx);

        let mut ins = vec![Instruction::LoadArgument(0)];
        if let Some(base_id) = cls.base
            && let Some(&base_type) = exception_type_indices.get(&base_id)
            && let Some(&base_ctor) = exception_ctors.get(&base_id)
        {
            // 用户异常链：直接转发给生成的基类构造函数。
            res[type_idx].set_extends(base_type);
            // 实例方法的参数下标从 1 开始：1 = PyClass，2 = args，3 = typeName。
            ins.extend([
                Instruction::LoadArgument(1),
                Instruction::LoadArgument(2),
                Instruction::LoadArgument(3),
                Instruction::call(UserMethod::Definition(base_ctor)),
            ]);
        } else if let Some(builtin) = &cls.builtin_base {
            let base_type_ref = exception_type_refs[builtin];
            res[type_idx].set_extends(base_type_ref);
            let base_mt = runtime_refs.exception_types[builtin].clone();
            let base_ctor = res.push_method_reference(
                method_ref! { void @base_mt::.ctor(string, string, object[]) },
            );
            ins.extend([
                // 实例方法：1 = PyClass，2 = args，3 = typeName。
                Instruction::LoadArgument(3),
                // 消息是否延迟由运行时基类ABI决定，后端不解释Python警告类别。
                Instruction::load_string(builtin.clone()),
                Instruction::LoadArgument(2),
                Instruction::call(runtime_refs.user_exception_message_for_base),
                Instruction::LoadArgument(2),
                Instruction::call(base_ctor),
                // 把 Python 类对象挂到异常上（属性访问与 isinstance 需要）。
                Instruction::LoadArgument(0),
                Instruction::LoadArgument(1),
                Instruction::call(runtime_refs.bind_python_class),
            ]);
        } else {
            // 理论上不可达：异常类一定有用户基类或内建异常基类。
            res[type_idx].set_extends(py_exc_ref);
        }
        ins.push(Instruction::Return);
        let mut ctor_body = body::Method::new(ins);
        ctor_body.header.maximum_stack_size = 8;
        res[ctor_idx].body = Some(ctor_body);
    }

    // 4. 为每个用户异常类生成工厂方法，并生成 `__init_class_factories`。
    let mut exception_factories: FxHashMap<ryupy_mir::ClassDefId, MethodIndex> =
        FxHashMap::default();
    for (cls_id, cls) in program.classes.iter() {
        if !cls.is_exception || cls.is_dataclass {
            continue;
        }
        let Some(&ctor_idx) = exception_ctors.get(&cls_id) else {
            continue;
        };
        let factory_sig = MethodSignature::new(
            false,
            ReturnType::value(py_exc_mt.clone()),
            vec![
                Parameter::value(pyclass_mt.clone()),
                Parameter::value(object_array.clone()),
            ],
        );
        let factory_idx = res.push_method(
            module_class,
            Method::new(
                Accessibility::Private,
                factory_sig,
                Cow::Owned(format!("__exc_factory_{}", cls_id.into_raw().into_u32())),
                None,
            ),
        );
        let ins = vec![
            Instruction::LoadArgument(0),
            Instruction::LoadArgument(1),
            Instruction::load_string(cls.name.clone()),
            Instruction::NewObject(UserMethod::Definition(ctor_idx)),
            Instruction::Return,
        ];
        let mut factory_body = body::Method::new(ins);
        factory_body.header.maximum_stack_size = 8;
        res[factory_idx].body = Some(factory_body);
        exception_factories.insert(cls_id, factory_idx);
    }

    let init_class_factories = emit_init_class_factories(
        &mut res,
        module_class,
        program,
        &runtime_refs,
        &exception_factories,
    );

    // 第一遍：为所有函数分配 MethodIndex。
    //
    // 方法名用 `codegen_name`（link 阶段已按模块加前缀，保证唯一）；Python 可见的
    // 函数名仍保留在 `Function::name`，供 PyFunction 与签名使用。
    let mut fn_indices: FxHashMap<FnId, MethodIndex> = FxHashMap::default();
    for (fn_id, func) in program.functions.iter() {
        let params: Vec<_> = func
            .params
            .iter()
            .map(|&p| {
                crate::types::mir_type_to_parameter(&func.body.locals[p].ty, &class_types).unwrap()
            })
            .collect();
        let ret = crate::types::mir_type_to_return_type(&func.ret, &class_types);
        let sig = MethodSignature::new(false, ret, params);
        let method_idx = res.push_method(
            module_class,
            Method::new(
                Accessibility::Public,
                sig,
                Cow::Owned(func.codegen_name.clone()),
                None,
            ),
        );
        fn_indices.insert(fn_id, method_idx);
    }

    // 入口包装器：`Main` 带 try/catch，负责把未捕获异常渲染成 traceback。
    let wrapper = res.push_method(
        module_class,
        Method::new(
            Accessibility::Public,
            msig! { static int (string[]) },
            "Main",
            None,
        ),
    );
    res.set_entry_point(wrapper);

    // 每个用户函数生成统一签名的 thunk，供 PyFunction 的委托调用。
    // 顶层函数与嵌套函数（M3.0）都有签名槽位；模块初始化函数跳过
    //（它们由导入机制通过委托调用）。
    let mut thunks: FxHashMap<FnId, MethodIndex> = FxHashMap::default();
    let mut thunk_index = 0usize;
    for (fn_id, func) in program.functions.iter() {
        if func.signature_global.is_none() {
            continue;
        }
        let thunk = crate::callables::emit_thunk(
            &mut res,
            module_class,
            func,
            fn_indices[&fn_id],
            &runtime_refs,
            &class_types,
            thunk_index,
        );
        thunks.insert(fn_id, thunk);
        thunk_index += 1;
    }

    // 模块初始化只预建签名；Python函数身份在真实定义执行点构造。
    let init_functions = crate::owned_function_refs::initialize(
        &mut res,
        module_class,
        program,
        &mut runtime_refs,
        &thunks,
        init_literals,
    );
    // 模块对象的属性读写入口 + 注册表。注册追加到 `__init_functions` 末尾，
    // 此时函数对象与类工厂都已就绪。
    let accessors = crate::modules::emit_module_accessors(
        &mut res,
        module_class,
        program,
        &runtime_refs,
        &class_types,
    );
    crate::modules::append_module_registration(
        &mut res,
        program,
        &runtime_refs,
        &fn_indices,
        init_functions,
        &accessors,
        binding_refs.as_ref().map(|refs| (bindings, refs)),
    );

    // 所有原生注册已由入口System.Initialize完成；检查置于任何生成初始化写之前。
    if let Some(refs) = &binding_refs {
        let mut prefix = Vec::new();
        for binding in bindings {
            prefix.push(Instruction::load_string(
                program.modules[binding.module].name.clone(),
            ));
            prefix.push(Instruction::call(refs.ensure));
        }
        if let Some(deployment) = deployment {
            prefix.extend(crate::fixed_binding_deployment::registration(
                &mut res,
                module_class,
                sys_runtime,
                ryupy_runtime,
                &bindings[0],
                deployment,
            ));
        }
        if let Some(body) = res[init_functions].body.as_mut() {
            body.instructions.splice(0..0, prefix);
        }
    }

    // 第二遍：逐函数编译并填充 body。动态运算 / 调用的 PIC 编号在整程序内递增分配。
    let site_counter = std::cell::Cell::new(0u32);
    let mut binding_methods = Vec::new();
    for (fn_id, func) in program.functions.iter() {
        let method_idx = fn_indices[&fn_id];
        if let Some(binding) = bindings.iter().find(|binding| binding.initializer == fn_id) {
            // 已验证记录选择专用body，不按生成名称覆盖普通函数。
            let (body, wrapper, native) = crate::fixed_binding_emit::initializer(
                &mut res,
                module_class,
                program,
                binding,
                binding_refs
                    .as_ref()
                    .expect("validated nonempty binding references"),
                &runtime_refs,
            );
            res[method_idx].body = Some(body);
            if report_requested {
                binding_methods.push((binding, wrapper, native));
            }
            continue;
        }
        let compiled = lower_function(
            func,
            &program.functions,
            &thunks,
            &runtime_refs,
            &fn_indices,
            &class_types,
            &class_info,
            &site_counter,
        );
        let mut method_body = body::Method::with_locals(compiled.locals, compiled.instructions);
        method_body.header.maximum_stack_size = compiled.max_stack_size;
        if !compiled.exceptions.is_empty() {
            method_body.data_sections =
                vec![body::DataSection::ExceptionHandlers(compiled.exceptions)];
        }
        res[method_idx].body = Some(method_body);
    }

    // 运行入口模块：由 `PyModules.RunMain()` 通过导入机制执行 `__main__`，
    // 这样 `import` 与入口模块走同一条初始化路径（循环导入也成立）。
    let run_main = res.push_method(
        module_class,
        Method::new(
            Accessibility::Private,
            msig! { static void () },
            "__run_main",
            None,
        ),
    );
    let mut run_main_body = body::Method::new(vec![
        Instruction::call(runtime_refs.module_run_main),
        Instruction::Return,
    ]);
    run_main_body.header.maximum_stack_size = 8;
    res[run_main].body = Some(run_main_body);

    // 包装器最后生成：需要 `__run_main` 的方法下标
    {
        let py_system_exit_ref = res.push_type_reference(ExternalTypeReference::new(
            Some("RyuPy.Runtime".into()),
            "PySystemExit",
            ResolutionScope::Assembly(ryupy_runtime),
        ));
        let py_system_exit_type: MethodType = BaseType::class(py_system_exit_ref).into();
        let system_exit_code = res.push_method_reference(
            method_ref! { static int @traceback_type::SystemExitCode(@py_system_exit_type) },
        );
        emit_entry_wrapper(
            &mut res,
            wrapper,
            run_main,
            report_unhandled,
            report_clr,
            py_exc_type,
            clr_exc_type,
            py_system_exit_type,
            set_file,
            runtime_refs.system.initialize,
            system_exit_code,
            runtime_refs.system.exit_flush,
            init_functions,
            init_class_factories,
            &program.modules[program.entry].file,
            opts.entry_source_path
                .as_deref()
                .unwrap_or(&program.modules[program.entry].file),
        );
    }

    let report = report_requested.then(|| {
        crate::emission_report::collect(
            &res,
            program,
            wrapper,
            &fn_indices,
            &thunks,
            &exception_factories,
            &accessors,
            &binding_methods,
        )
    });
    let records = if report_requested {
        crate::fixed_binding_deployment::describe(&res, &binding_methods)
    } else {
        Vec::new()
    };
    (res, module_class, report, records)
}
