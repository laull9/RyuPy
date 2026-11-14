//! 候选冷注册引用仅在MIR明确携带候选时生成；后端不读文件/AST或重新判定来源。
use dotnetdll::prelude::*;
pub(crate) struct References {
    pub source: MethodRefIndex,
    pub instance: MethodRefIndex,
    pub configure: MethodRefIndex,
}
impl References {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let modules = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModules in #runtime });
        let modules: MethodType = BaseType::class(modules).into();
        let init = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInit in #runtime });
        let init: MethodType = BaseType::class(init).into();
        let get = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleGet in #runtime });
        let get: MethodType = BaseType::class(get).into();
        let set = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleSet in #runtime });
        let set: MethodType = BaseType::class(set).into();
        let execute =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleExecute in #runtime });
        let execute: MethodType = BaseType::class(execute).into();
        Self {
            source: res.push_method_reference(method_ref! { static void @modules::RegisterCandidateSource(string, string, string, long, string, bool, bool, @init, @get, @set) }),
            instance: res.push_method_reference(method_ref! { static void @modules::RegisterCandidateInstance(string, string, string, long, string, bool, @execute) }),
            configure: res.push_method_reference(method_ref! { static void @modules::ConfigureOptionalSources(string) }),
        }
    }
}
