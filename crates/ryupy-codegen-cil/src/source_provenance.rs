//! 已验证代码来源表的机械冷发射：直接绑定本unit访问器/字段，不执行Python代码或查注册名。
use dotnetdll::prelude::*;
use ryupy_mir::{FnId, GlobalId, GlobalStorage, LinkedProgram, ModuleId};

/// declare之前由完整来源校验产生的局部凭据；不把可删的来源标签当作必需性依据。
pub struct SourceModules(Vec<(ModuleId, FnId, GlobalId)>);

impl SourceModules {
    pub fn capture(program: &LinkedProgram) -> Result<Self, crate::CodegenError> {
        require(program)?;
        let mut modules = Vec::new();
        for (id, module) in program.modules.iter() {
            let source = ryupy_mir::source_provenance::for_function(&program.globals, module.init)
                .ok_or_else(|| crate::CodegenError::Unsupported("源码模块缺少来源".into()))?;
            modules.push((id, module.init, source));
        }
        Ok(Self(modules))
    }

    // 调用方先validate实际绑定；原集合逐项保留，豁免仅来自本次新增的绑定ID。
    pub(crate) fn validate_extension(
        &self,
        program: &LinkedProgram,
        bindings: &[crate::fixed_bindings::FixedI32Binding],
    ) -> Result<(), crate::CodegenError> {
        let original: Vec<_> = self.0.iter().map(|(id, _, _)| *id).collect();
        let declared: std::collections::HashSet<_> = bindings.iter().map(|b| b.module).collect();
        if program.modules.len() != self.0.len() + declared.len()
            || bindings.iter().any(|b| original.contains(&b.module))
            || program
                .modules
                .iter()
                .any(|(id, _)| !original.contains(&id) && !bindings.iter().any(|b| b.module == id))
            || self.0.iter().any(|(id, init, source)| {
                !program
                    .modules
                    .iter()
                    .any(|(actual, m)| actual == *id && m.init == *init)
                    || ryupy_mir::source_provenance::for_function(&program.globals, *init)
                        != Some(*source)
            })
        {
            return Err(crate::CodegenError::Unsupported(
                "固定绑定改变或遗漏原源码模块集合".into(),
            ));
        }
        ryupy_mir::source_provenance::require_modules(program, &original).map_err(source_error)
    }
}

fn source_error(errors: Vec<String>) -> crate::CodegenError {
    crate::CodegenError::Unsupported(errors.join("\n"))
}

pub(crate) fn require(program: &LinkedProgram) -> Result<(), crate::CodegenError> {
    ryupy_mir::source_provenance::require_linked(program).map_err(source_error)
}

pub(crate) fn validate(program: &LinkedProgram) -> Result<(), crate::CodegenError> {
    ryupy_mir::validate::validate_linked_exports(program).map_err(source_error)
}

pub(crate) fn initialize(
    res: &mut Resolution<'static>,
    module: TypeIndex,
    program: &LinkedProgram,
    refs: &crate::stackify_refs::RuntimeRefs,
    accessors: &crate::modules::ModuleAccessors,
    runtime: AssemblyRefIndex,
) -> Option<MethodIndex> {
    if !program
        .globals
        .iter()
        .any(|(_, g)| matches!(g.storage, GlobalStorage::CodeSource { .. }))
    {
        return None;
    }
    let traceback = res.push_type_reference(type_ref! { RyuPy.Runtime.PyTraceback in #runtime });
    let traceback: MethodType = BaseType::class(traceback).into();
    let getter = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleGet in #runtime });
    let getter: MethodType = BaseType::class(getter).into();
    let create = res.push_method_reference(
        method_ref! { static object @traceback::CreateCodeSource(string, string, @getter) },
    );
    let create_legacy = res.push_method_reference(
        method_ref! { static object @traceback::CreateLegacyCodeSourceV1(string, string, @getter) },
    );
    let mut instructions = Vec::new();
    for (global, declaration) in program.globals.iter() {
        let GlobalStorage::CodeSource {
            entry,
            legacy_entry,
            ..
        } = &declaration.storage
        else {
            continue;
        };
        // 公共发射出口已核唯一真实module.init；无需按文件/名字或字段前缀匹配。
        let (index, (_, definition)) = program
            .modules
            .iter()
            .enumerate()
            .find(|(_, (_, m))| m.init == *entry)
            .expect("代码来源已验证真实模块入口");
        instructions.push(Instruction::load_string(definition.name.clone()));
        instructions.push(Instruction::load_string(
            program.functions[*entry].source_file.clone(),
        ));
        if legacy_entry.is_some() {
            instructions.extend([
                Instruction::LoadNull,
                Instruction::LoadMethodPointer(
                    accessors.getters[index].expect("Legacy已验证访问器").into(),
                ),
                Instruction::NewObject(UserMethod::Reference(refs.module_get_ctor)),
            ]);
        } else {
            instructions.push(Instruction::LoadNull);
        }
        instructions.push(Instruction::call(if legacy_entry.is_some() {
            create_legacy
        } else {
            create
        }));
        instructions.push(Instruction::StoreStaticField {
            volatile: false,
            param0: refs.global_fields[&global].into(),
        });
    }
    instructions.push(Instruction::Return);
    let mut body = body::Method::new(instructions);
    body.header.maximum_stack_size = 5;
    Some(res.push_method(
        module,
        Method::new(
            Accessibility::Private,
            msig! { static void () },
            "__init_code_sources",
            Some(body),
        ),
    ))
}
