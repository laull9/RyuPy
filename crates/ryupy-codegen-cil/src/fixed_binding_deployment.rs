//! 固定绑定的冷注册和真实PInvoke报告；只观察已生成方法，不新增反射根。
use crate::fixed_bindings::{BindingDeployment, BindingEmission, FixedI32Binding};
use dotnetdll::prelude::*;

pub(crate) fn registration(
    res: &mut Resolution<'static>,
    owner: TypeIndex,
    system: AssemblyRefIndex,
    runtime: AssemblyRefIndex,
    binding: &FixedI32Binding,
    deployment: &BindingDeployment,
) -> Vec<Instruction> {
    let ty = res.push_type_reference(type_ref! { System.Type in #system });
    let ty: MethodType = BaseType::class(ty).into();
    let handle = res.push_type_reference(type_ref! { System.RuntimeTypeHandle in #system });
    let handle: MethodType = BaseType::valuetype(handle).into();
    let assembly = res.push_type_reference(type_ref! { System.Reflection.Assembly in #system });
    let assembly: MethodType = BaseType::class(assembly).into();
    let library =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyNativeBindingLibrary in #runtime });
    let library: MethodType = BaseType::class(library).into();
    let get_type =
        res.push_method_reference(method_ref! { static @ty @ty::GetTypeFromHandle(@handle) });
    let get_assembly = res.push_method_reference(method_ref! { @assembly @ty::get_Assembly() });
    let register = res.push_method_reference(
        method_ref! { static void @library::Register(@assembly, string, string) },
    );
    vec![
        Instruction::LoadTokenType(BaseType::class(owner).into()),
        Instruction::call(get_type),
        Instruction::CallVirtual {
            skip_null_check: false,
            param0: get_assembly.into(),
        },
        Instruction::load_string(binding.module_ref.clone()),
        Instruction::load_string(deployment.relative_file.clone()),
        Instruction::call(register),
    ]
}

fn descriptor(res: &Resolution<'_>, index: MethodIndex) -> crate::MethodDescriptor {
    // 索引来自本次发射，不从__fixed等命名约定推断owner或角色。
    for (owner, ty) in res.enumerate_type_definitions() {
        for (candidate, method) in res.enumerate_methods(owner) {
            if candidate == index {
                return crate::MethodDescriptor {
                    owner: ty
                        .namespace
                        .as_ref()
                        .map_or_else(|| ty.name.to_string(), |ns| format!("{ns}.{}", ty.name)),
                    name: method.name.to_string(),
                    signature: method.signature.show(res),
                };
            }
        }
    }
    unreachable!("emitted binding method belongs to this resolution")
}

pub(crate) fn describe(
    res: &Resolution<'_>,
    methods: &[(&FixedI32Binding, MethodIndex, MethodIndex)],
) -> Vec<BindingEmission> {
    methods
        .iter()
        .enumerate()
        .map(|(number, (binding, wrapper, native))| {
            let method = &res[*native];
            let pinvoke = method
                .pinvoke
                .as_ref()
                .expect("explicitly emitted PInvoke method");
            BindingEmission {
                binding_id: format!("b{number}"),
                module_id: binding.module.into_raw().into_u32(),
                initializer_id: binding.initializer.into_raw().into_u32(),
                export_id: binding.export.into_raw().into_u32(),
                body: descriptor(res, *wrapper),
                native: descriptor(res, *native),
                library: res[pinvoke.import_scope].name.to_string(),
                symbol: pinvoke.import_name.to_string(),
                calling_convention: format!("{:?}", pinvoke.calling_convention),
                character_set: format!("{:?}", pinvoke.character_set),
                no_mangle: pinvoke.no_mangle,
                supports_last_error: pinvoke.supports_last_error,
                preserve_sig: method.preserve_sig,
                is_static: !method.signature.instance,
                has_body: method.body.is_some(),
            }
        })
        .collect()
}
