//! 入口系统环境 ABI：只传递源文件名与 Main 实参，不在后端实现 Python 系统语义。

use dotnetdll::prelude::*;

pub(crate) struct SystemRefs {
    pub initialize: MethodRefIndex,
    pub exit_flush: MethodRefIndex,
    pub print_target: MethodRefIndex,
    pub write_i64: MethodRefIndex,
    pub write_f64: MethodRefIndex,
    pub write_bool: MethodRefIndex,
    pub write_str: MethodRefIndex,
    pub write_newline: MethodRefIndex,
    pub write_sep: MethodRefIndex,
    pub write_object: MethodRefIndex,
}

impl SystemRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let system = res.push_type_reference(type_ref! { RyuPy.Runtime.PySystem in #runtime });
        let system: MethodType = BaseType::class(system).into();
        let builtins = res.push_type_reference(type_ref! { RyuPy.Runtime.Builtins in #runtime });
        let builtins: MethodType = BaseType::class(builtins).into();
        Self {
            exit_flush: res
                .push_method_reference(method_ref! { static int @builtins::FlushForExit(int) }),
            write_i64: res.push_method_reference(
                method_ref! { static void @builtins::WriteI64(object, long) },
            ),
            write_f64: res.push_method_reference(
                method_ref! { static void @builtins::WriteF64(object, double) },
            ),
            write_bool: res.push_method_reference(
                method_ref! { static void @builtins::WriteBool(object, bool) },
            ),
            write_str: res.push_method_reference(
                method_ref! { static void @builtins::WriteStr(object, string) },
            ),
            write_newline: res
                .push_method_reference(method_ref! { static void @builtins::WriteNewline(object) }),
            write_sep: res
                .push_method_reference(method_ref! { static void @builtins::WriteSep(object) }),
            write_object: res.push_method_reference(
                method_ref! { static void @builtins::WriteObject(object, object) },
            ),
            print_target: res
                .push_method_reference(method_ref! { static object @builtins::PrintTarget() }),
            initialize: res.push_method_reference(
                method_ref! { static void @system::Initialize(string, string[]) },
            ),
        }
    }
}
