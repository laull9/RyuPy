//! 模块绑定检查的机械 ABI 引用：类型与检查策略由 MIR 的 ExportGuard 决定。
use dotnetdll::prelude::*;

pub(crate) struct BindingRefs {
    /// 动态模块名字使用未绑定哨兵；成功时仍返回原对象。
    pub load_name: MethodRefIndex,
    pub capture_module_target: MethodRefIndex,
    pub is_module: MethodRefIndex,
    pub call_captured_method: MethodRefIndex,
    pub pic_captured_method: [MethodRefIndex; 4],
    pub check_name: MethodRefIndex,
    pub check_module: MethodRefIndex,
    pub load_attribute: MethodRefIndex,
}

impl BindingRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let errors = res.push_type_reference(type_ref! { RyuPy.Runtime.PyErrors in #runtime });
        let errors: MethodType = BaseType::class(errors).into();
        let ops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let ops: MethodType = BaseType::class(ops).into();
        let modules = res.push_type_reference(type_ref! { RyuPy.Runtime.PyModules in #runtime });
        let modules: MethodType = BaseType::class(modules).into();
        let pic = res.push_type_reference(type_ref! { RyuPy.Runtime.PyPic in #runtime });
        let pic: MethodType = BaseType::class(pic).into();
        Self {
            is_module: res.push_method_reference(method_ref! { static bool @ops::IsModule(object) }),
            call_captured_method: res.push_method_reference(method_ref! { static object @ops::CallCapturedMethod(bool, object, string, object[], object[], object[]) }),
            pic_captured_method: [
                res.push_method_reference(method_ref! { static object @pic::CallCapturedMethod0(int, bool, object, string) }),
                res.push_method_reference(method_ref! { static object @pic::CallCapturedMethod1(int, bool, object, string, object) }),
                res.push_method_reference(method_ref! { static object @pic::CallCapturedMethod2(int, bool, object, string, object, object) }),
                res.push_method_reference(method_ref! { static object @pic::CallCapturedMethod3(int, bool, object, string, object, object, object) }),
            ],
            capture_module_target: res.push_method_reference(
                method_ref! { static object @ops::CaptureModuleTarget(object, string) },
            ),
            load_name: res.push_method_reference(
                method_ref! { static object @errors::LoadName(object, string) },
            ),
            check_name: res.push_method_reference(
                method_ref! { static void @ops::CheckGlobalBound(bool, string) },
            ),
            check_module: res.push_method_reference(
                method_ref! { static void @modules::CheckBound(bool, string, string) },
            ),
            load_attribute: res.push_method_reference(
                method_ref! { static object @modules::LoadAttribute(object, string, string) },
            ),
        }
    }
}
