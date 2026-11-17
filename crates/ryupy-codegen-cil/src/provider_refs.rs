//! 组合显式外部 provider 宿主：只根据 MIR 配置建立静态 ABI 引用，不解析分发目录或决定模块归属。
use dotnetdll::prelude::*;
pub(crate) struct ProviderRefs {
    pub start: MethodRefIndex,
    pub finish: MethodRefIndex,
}
impl ProviderRefs {
    pub(crate) fn for_program(
        res: &mut Resolution<'static>,
        program: &ryupy_mir::LinkedProgram,
    ) -> Option<Self> {
        program.external_provider.as_ref().map(|_| {
            let mut refs = Self::build(res);
            if program.functions.iter().any(|(_, f)| {
                f.module_state_source == Some(ryupy_mir::InstanceStateSource::StockThinLease)
            }) {
                let parent = res[refs.start].parent.clone();
                refs.start = res.push_method_reference(ExternalMethodReference::new(
                    parent,
                    "StartStockThin",
                    msig! { static void (string, string[]) },
                ));
            }
            refs
        })
    }
    pub(crate) fn build(res: &mut Resolution<'static>) -> Self {
        let mut assembly = ExternalAssemblyReference::new("RyuPy.CPython");
        assembly.version = Version {
            major: 1,
            minor: 0,
            build: 0,
            revision: 0,
        };
        let assembly = res.push_assembly_reference(assembly);
        let bootstrap = res.push_type_reference(type_ref! { RyuPy.CPython.Bootstrap in #assembly });
        let bootstrap: MethodType = BaseType::class(bootstrap).into();
        Self {
            start: res.push_method_reference(
                method_ref! { static void @bootstrap::Start(string, string[]) },
            ),
            finish: res.push_method_reference(method_ref! { static int @bootstrap::Finish(int) }),
        }
    }
}
