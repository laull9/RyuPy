//! 同一次Resolution的实际委托引用报告；只观察已发射IL，不新增根或推断运行时可达性。
use dotnetdll::prelude::*;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_mir::{ClassDefId, FnId, LinkedProgram, Rvalue, Statement};

#[derive(Clone, Debug, PartialEq, Eq, PartialOrd, Ord)]
pub struct MethodDescriptor {
    pub owner: String,
    pub name: String,
    pub signature: String,
}
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct ModuleEmission {
    pub module_id: u32,
    pub module_name: String,
    pub environment_reason: String,
    pub init: MethodDescriptor,
    pub getter: Option<MethodDescriptor>,
    pub setter: Option<MethodDescriptor>,
    pub exports: Vec<String>,
}
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct DelegateReference {
    pub source: MethodDescriptor,
    pub instruction: u32,
    pub target: Option<MethodDescriptor>,
    pub target_display: String,
    pub role: String,
    pub module_id: Option<u32>,
    pub function_id: Option<u32>,
    pub class_id: Option<u32>,
}
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct EmissionReport {
    pub schema_version: u32,
    pub entry: MethodDescriptor,
    pub modules: Vec<ModuleEmission>,
    pub delegate_references: Vec<DelegateReference>,
}
#[derive(Debug)]
pub struct EmissionArtifact {
    pub bytes: Vec<u8>,
    pub report: EmissionReport,
}

#[derive(Clone)]
struct Role {
    name: &'static str,
    module: Option<u32>,
    function: Option<u32>,
    class: Option<u32>,
}

// 显式生成映射只有assembly拥有；报告不从__thunk等名字反推语义。
#[allow(clippy::too_many_arguments)]
pub(crate) fn collect(
    res: &Resolution<'_>,
    program: &LinkedProgram,
    entry: MethodIndex,
    functions: &FxHashMap<FnId, MethodIndex>,
    thunks: &FxHashMap<FnId, MethodIndex>,
    factories: &FxHashMap<ClassDefId, MethodIndex>,
    accessors: &crate::modules::ModuleAccessors,
    bindings: &[(
        &crate::fixed_bindings::FixedI32Binding,
        MethodIndex,
        MethodIndex,
    )],
) -> EmissionReport {
    let identities: FxHashMap<_, _> = res
        .enumerate_type_definitions()
        .flat_map(|(owner, ty)| {
            res.enumerate_methods(owner).map(move |(index, method)| {
                (
                    index,
                    MethodDescriptor {
                        owner: match &ty.namespace {
                            Some(namespace) => format!("{namespace}.{}", ty.name),
                            None => ty.name.to_string(),
                        },
                        name: method.name.to_string(),
                        signature: method.signature.show(res),
                    },
                )
            })
        })
        .collect();
    let mut roles = FxHashMap::default();
    let mut modules = Vec::new();
    for (position, (id, module)) in program.modules.iter().enumerate() {
        let number = id.into_raw().into_u32();
        let init = functions[&module.init];
        let getter = accessors.getters[position];
        let setter = accessors.setters[position];
        for (method, role) in [
            (Some(init), "module_init"),
            (getter, "module_getter"),
            (setter, "module_setter"),
        ] {
            let Some(method) = method else {
                continue;
            };
            roles.insert(
                method,
                Role {
                    name: role,
                    module: Some(number),
                    function: None,
                    class: None,
                },
            );
        }
        let mut exports: Vec<_> = module
            .exports
            .iter()
            .map(|export| export.name.clone())
            .collect();
        exports.sort();
        modules.push(ModuleEmission {
            module_id: number,
            module_name: module.name.clone(),
            environment_reason: module.environment_reason.clone(),
            init: identities[&init].clone(),
            getter: getter.map(|index| identities[&index].clone()),
            setter: setter.map(|index| identities[&index].clone()),
            exports,
        });
    }
    for (&function, &method) in thunks {
        roles.insert(
            method,
            Role {
                name: "callable_thunk",
                module: None,
                function: Some(function.into_raw().into_u32()),
                class: None,
            },
        );
    }
    // 只真实MakeGenerator声明的resume目标被标记，不把所有普通函数猜作生成器。
    let resumes: FxHashSet<_> = program
        .functions
        .iter()
        .flat_map(|(_, function)| {
            function
                .body
                .blocks
                .iter()
                .flat_map(|(_, block)| &block.stmts)
                .filter_map(|statement| match statement {
                    Statement::Assign {
                        value: Rvalue::MakeGenerator { resume, .. },
                        ..
                    }
                    | Statement::Eval(Rvalue::MakeGenerator { resume, .. }) => Some(*resume),
                    _ => None,
                })
        })
        .collect();
    for resume in resumes {
        roles.insert(
            functions[&resume],
            Role {
                name: "generator_resume",
                module: None,
                function: Some(resume.into_raw().into_u32()),
                class: None,
            },
        );
    }
    for (&class, &method) in factories {
        roles.insert(
            method,
            Role {
                name: "exception_factory",
                module: None,
                function: None,
                class: Some(class.into_raw().into_u32()),
            },
        );
    }
    // 仅按本次发射保存的真实方法索引标注，不根据生成名称推断角色。
    for (binding, wrapper, _) in bindings {
        roles.insert(
            *wrapper,
            Role {
                name: "declared_native_body",
                module: Some(binding.module.into_raw().into_u32()),
                function: None,
                class: None,
            },
        );
    }
    let mut delegate_references = Vec::new();
    for (owner, _) in res.enumerate_type_definitions() {
        for (source, method) in res.enumerate_methods(owner) {
            let Some(body) = &method.body else {
                continue;
            };
            for (instruction, value) in body.instructions.iter().enumerate() {
                let Instruction::LoadMethodPointer(target) = value else {
                    continue;
                };
                let definition = match target {
                    MethodSource::User(UserMethod::Definition(method)) => Some(*method),
                    _ => None,
                };
                let role = definition.and_then(|method| roles.get(&method));
                delegate_references.push(DelegateReference {
                    source: identities[&source].clone(),
                    instruction: instruction as u32,
                    target: definition.map(|method| identities[&method].clone()),
                    target_display: target.show(res),
                    role: role.map_or("unclassified", |role| role.name).to_owned(),
                    module_id: role.and_then(|role| role.module),
                    function_id: role.and_then(|role| role.function),
                    class_id: role.and_then(|role| role.class),
                });
            }
        }
    }
    delegate_references.sort_by(|a, b| (&a.source, a.instruction).cmp(&(&b.source, b.instruction)));
    EmissionReport {
        schema_version: 1,
        entry: identities[&entry].clone(),
        modules,
        delegate_references,
    }
}
