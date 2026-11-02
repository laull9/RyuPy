//! 固定绑定所需CLR引用：精确参数、委托和三个受控装载异常，不使用反射分派。
use dotnetdll::prelude::*;

pub(crate) struct BindingRefs {
    pub checked: MethodRefIndex,
    pub create: MethodRefIndex,
    pub delegate_ctor: MethodRefIndex,
    pub ensure: MethodRefIndex,
    pub register: MethodRefIndex,
    pub failure: MethodRefIndex,
    pub loader_errors: Vec<MethodType>,
}
impl BindingRefs {
    pub(crate) fn build(
        res: &mut Resolution<'static>,
        system: AssemblyRefIndex,
        runtime: AssemblyRefIndex,
    ) -> Self {
        let owner =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyNativeBindings in #runtime });
        let owner: MethodType = BaseType::class(owner).into();
        let modules = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModules in #runtime });
        let modules: MethodType = BaseType::class(modules).into();
        let function = res.push_type_reference(type_ref! { RyuPy.Runtime.PyFunction in #runtime });
        let function: MethodType = BaseType::class(function).into();
        let exception = res.push_type_reference(type_ref! { System.Exception in #system });
        let exception: MethodType = BaseType::class(exception).into();
        let py_exception =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyBaseException in #runtime });
        let py_exception: MethodType = BaseType::class(py_exception).into();
        let func = res.push_type_reference(type_ref! { System.Func<2> in #system });
        let func: MethodType = BaseType::class(TypeSource::generic(
            func,
            vec![ctype! { object[] }, ctype! { object }],
        ))
        .into();
        let init = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInit in #runtime });
        let init: MethodType = BaseType::class(init).into();
        let get = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleGet in #runtime });
        let get: MethodType = BaseType::class(get).into();
        let set = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleSet in #runtime });
        let set: MethodType = BaseType::class(set).into();
        // .NET10 ref pack将DllNotFoundException定义在InteropServices；另两者在Runtime。
        // 实现程序集虽都转发CoreLib，发射仍必须引用各自的正式facade。
        let interop = res.push_assembly_reference(crate::assembly_refs::interop_services());
        let loader_errors = [
            ("DllNotFoundException", interop),
            ("EntryPointNotFoundException", system),
            ("BadImageFormatException", system),
        ]
        .into_iter()
        .map(|(name, scope)| {
            let ty = res.push_type_reference(ExternalTypeReference::new(
                Some("System".into()),
                name,
                ResolutionScope::Assembly(scope),
            ));
            BaseType::class(ty).into()
        })
        .collect();
        Self {
            checked: res.push_method_reference(method_ref! { static int @owner::CheckedI32(object, string, string) }),
            create: res.push_method_reference(method_ref! { static @function @owner::CreateI32Binary(string, @func) }),
            delegate_ctor: res.push_method_reference(method_ref! { void @func::.ctor(object, nint) }),
            ensure: res.push_method_reference(method_ref! { static void @modules::EnsureDeclaredBindingAvailable(string) }),
            register: res.push_method_reference(method_ref! { static void @modules::RegisterDeclaredBinding(string, @init, @get, @set) }),
            failure: res.push_method_reference(method_ref! { static @py_exception @owner::LoadFailure(@exception, string, string) }),
            loader_errors,
        }
    }
}
