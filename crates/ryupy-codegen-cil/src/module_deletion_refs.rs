//! 模块裸名删除的惰性固定ABI；只读取MIR调用，不判断Python名称、绑定或内建策略。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, LinkedProgram, RuntimeFn, Rvalue, Statement};

pub(crate) struct DeletionRefs {
    check: Option<MethodRefIndex>,
    check_value: Option<MethodRefIndex>,
    load_name: Option<MethodRefIndex>,
    delete_name: Option<MethodRefIndex>,
}

impl DeletionRefs {
    /// 没有新调用时不增加任何引用；各入口也只按实际使用建立，保持无关路径不变。
    pub(crate) fn for_program(
        res: &mut Resolution<'static>,
        program: &LinkedProgram,
        runtime: AssemblyRefIndex,
    ) -> Option<Self> {
        let mut used = [false; 4];
        for (_, function) in program.functions.iter() {
            for (_, block) in function.body.blocks.iter() {
                for stmt in &block.stmts {
                    let (Statement::Assign { value, .. } | Statement::Eval(value)) = stmt else {
                        continue;
                    };
                    let Rvalue::Call {
                        callee: Callee::Runtime(callee),
                        ..
                    } = value
                    else {
                        continue;
                    };
                    if let Some(slot) = slot(*callee) {
                        used[slot] = true;
                    }
                }
            }
        }
        if !used.iter().any(|used| *used) {
            return None;
        }
        let mut refs = Self {
            check: None,
            check_value: None,
            load_name: None,
            delete_name: None,
        };
        if used[..3].iter().any(|used| *used) {
            let pyops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
            let pyops: MethodType = BaseType::class(pyops).into();
            refs.check = used[0].then(|| {
                res.push_method_reference(
                    method_ref! { static void @pyops::CheckModuleDelete(bool, string) },
                )
            });
            refs.check_value = used[1].then(|| {
                res.push_method_reference(
                    method_ref! { static void @pyops::CheckModuleDeleteValue(object, string) },
                )
            });
            refs.load_name = used[2].then(|| {
                res.push_method_reference(
                    method_ref! { static object @pyops::LoadDeletedModuleName(object, string) },
                )
            });
        }
        if used[3] {
            let instances =
                res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInstances in #runtime });
            let instances: MethodType = BaseType::class(instances).into();
            refs.delete_name = Some(res.push_method_reference(
                method_ref! { static void @instances::DeleteName(object, string) },
            ));
        }
        Some(refs)
    }
}

fn slot(callee: RuntimeFn) -> Option<usize> {
    match callee {
        RuntimeFn::CheckModuleDelete => Some(0),
        RuntimeFn::CheckModuleDeleteValue => Some(1),
        RuntimeFn::LoadDeletedModuleName => Some(2),
        RuntimeFn::InstanceDeleteName => Some(3),
        _ => None,
    }
}

/// 调用已经由MIR验证为固定ABI；后端只取对应引用，绝不转换为数组调用。
pub(crate) fn required(
    refs: &crate::stackify_refs::RuntimeRefs,
    callee: RuntimeFn,
) -> MethodRefIndex {
    let refs = refs.deletions.as_ref().expect("已验证模块删除ABI");
    match callee {
        RuntimeFn::CheckModuleDelete => refs.check,
        RuntimeFn::CheckModuleDeleteValue => refs.check_value,
        RuntimeFn::LoadDeletedModuleName => refs.load_name,
        RuntimeFn::InstanceDeleteName => refs.delete_name,
        _ => unreachable!("非模块删除ABI"),
    }
    .expect("已收集模块删除调用引用")
}
