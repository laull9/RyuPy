//! 实例模块的机械ABI：上下文由Execute显式传入，不查询ambient模块或猜测名称。
use dotnetdll::prelude::*;

pub(crate) struct InstanceRefs {
    pub class_guard: MethodRefIndex,
    pub create_class: MethodRefIndex,
    pub capture_definition: MethodRefIndex,
    pub definition_module: MethodRefIndex,
    pub load_name: MethodRefIndex,
    pub direct_globals: MethodRefIndex,
    pub store_name: MethodRefIndex,
    pub execution_source: MethodRefIndex,
    pub import_guard: MethodRefIndex,
    pub relative_import: MethodRefIndex,
    pub relative_attribute: MethodRefIndex,
    pub execute_ctor: MethodRefIndex,
    pub register: MethodRefIndex,
}

impl InstanceRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let instances =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInstances in #runtime });
        let instances: MethodType = BaseType::class(instances).into();
        let modules = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModules in #runtime });
        let modules: MethodType = BaseType::class(modules).into();
        let execute =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleExecute in #runtime });
        let execute: MethodType = BaseType::class(execute).into();
        Self {
            class_guard: res.push_method_reference(method_ref! { static void @instances::ClassGuard(object) }),
            create_class: res.push_method_reference(method_ref! { static object @instances::CreateClass(object, string, object[]) }),
            capture_definition: res.push_method_reference(method_ref! { static object @instances::CaptureDefinitionContext(object) }),
            definition_module: res.push_method_reference(method_ref! { static object @instances::DefinitionModule(object) }),
            direct_globals: res.push_method_reference(method_ref! { static object @instances::CallDirectGlobals(object, object) }),
            load_name: res.push_method_reference(method_ref! { static object @instances::LoadName(object, string) }),
            store_name: res.push_method_reference(method_ref! { static void @instances::StoreName(object, string, object) }),
            execution_source: res.push_method_reference(method_ref! { static object @instances::ExecutionSource(object) }),
            relative_import: res.push_method_reference(method_ref! { static object @instances::RelativeImport(object, string, long, object[]) }),
            relative_attribute: res.push_method_reference(method_ref! { static object @instances::RelativeImportAttr(object, object, string) }),
            import_guard: res.push_method_reference(method_ref! { static void @instances::ImportGuard(object) }),
            execute_ctor: res.push_method_reference(method_ref! { void @execute::.ctor(object, nint) }),
            register: res.push_method_reference(method_ref! { static void @modules::RegisterInstanceSourceLocated(string, string, string, long, string, bool, @execute) }),
        }
    }
}

/// 登记代码描述只构建委托，不执行源码正文，不分配实例全局字段。
pub(crate) fn append_registration(
    instructions: &mut Vec<Instruction>,
    module: &ryupy_mir::ModuleDef,
    execute: MethodIndex,
    refs: &InstanceRefs,
    candidate_register: Option<MethodRefIndex>,
) {
    let (kind, location) = match &module.location {
        ryupy_mir::ModuleLocation::LegacyDiagnostic => (0, ""),
        ryupy_mir::ModuleLocation::SourceAbsolute(value) => (1, value.as_str()),
        ryupy_mir::ModuleLocation::ExecutableRelative(value) => (2, value.as_str()),
    };
    instructions.extend([
        Instruction::load_string(&module.name),
        Instruction::load_string(&module.package),
        Instruction::load_string(&module.file),
        Instruction::LoadConstantInt64(kind),
        Instruction::load_string(location),
        Instruction::LoadConstantInt32(i32::from(module.is_package)),
        Instruction::LoadNull,
        Instruction::LoadMethodPointer(MethodSource::User(UserMethod::Definition(execute))),
        Instruction::NewObject(UserMethod::Reference(refs.execute_ctor)),
        Instruction::call(candidate_register.unwrap_or(refs.register)),
    ]);
}

/// 仅实例模块建立新引用，未选择程序的既有metadata token顺序保持不变。
pub(crate) fn for_program(
    res: &mut Resolution<'static>,
    program: &ryupy_mir::LinkedProgram,
    runtime: AssemblyRefIndex,
) -> Option<InstanceRefs> {
    program
        .modules
        .iter()
        .any(|(_, module)| module.execution == ryupy_mir::ModuleExecutionKind::InstanceGlobals)
        .then(|| InstanceRefs::build(res, runtime))
}

pub(crate) fn required(refs: &crate::stackify_refs::RuntimeRefs) -> &InstanceRefs {
    refs.instances
        .as_ref()
        .expect("validated instance module ABI")
}
