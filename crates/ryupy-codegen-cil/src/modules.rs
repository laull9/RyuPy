//! 模块对象的代码生成（M2.7）。
//!
//! 每个模块生成三样东西：
//! 1. `__mod_get_<i>(string) -> object`：按名字读取模块变量（按槽位类型装箱）；
//! 2. `__mod_set_<i>(PyModule, string, object)`：按名字写入（拆箱后写静态字段，
//!    未声明的名字交给运行时动态字典）；
//! 3. 在 `__init_functions` 末尾追加 `PyModules.Register(...)`，把初始化入口与
//!    上面两个委托登记到运行时。
//!
//! 属性分派用「顺序比较 + 分支」而不是哈希表：模块变量在编译期已知且数量很少，
//! 生成代码简单，也避免运行时分配。

use std::borrow::Cow;

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{ExportGuard, FnId, LinkedProgram};

use crate::stackify::RuntimeRefs;
use crate::types::ClassTypeMap;

/// 每个模块的对象属性读写入口。
pub(crate) struct ModuleAccessors {
    pub(crate) getters: Vec<Option<MethodIndex>>,
    pub(crate) setters: Vec<Option<MethodIndex>>,
}

/// 生成 `__mod_get_<i>(string) -> object` 与 `__mod_set_<i>(PyModule, string, object)`。
///
/// 模块变量是静态槽位：读取时按槽位类型装箱，写入时拆箱并交给 `PyCast`，
/// 因此外部代码看到的仍是 Python 值，内部代码保持静态类型。
pub(crate) fn emit_module_accessors(
    res: &mut Resolution<'static>,
    module_class: TypeIndex,
    program: &LinkedProgram,
    refs: &RuntimeRefs,
    class_types: &ClassTypeMap,
) -> ModuleAccessors {
    let object_type = refs.object_type.clone();
    let string_type: MethodType = BaseType::String.into();
    let py_module_type = refs.py_module_type.clone();
    let mut getters = Vec::new();
    let mut setters = Vec::new();

    for (index, (_, module)) in program.modules.iter().enumerate() {
        if module.execution == ryupy_mir::ModuleExecutionKind::InstanceGlobals {
            getters.push(None);
            setters.push(None);
            continue;
        }
        let getter = res.push_method(
            module_class,
            Method::new(
                Accessibility::Private,
                MethodSignature::new(
                    false,
                    ReturnType::value(object_type.clone()),
                    vec![Parameter::value(string_type.clone())],
                ),
                Cow::Owned(format!("__mod_get_{index}")),
                None,
            ),
        );
        let mut ins = Vec::new();
        for export in &module.exports {
            let (name, gid) = (&export.name, &export.value);
            ins.push(Instruction::LoadArgument(0));
            ins.push(Instruction::load_string(name));
            ins.push(Instruction::call(refs.module_same_name));
            let skip = ins.len();
            ins.push(Instruction::BranchFalsy(0));
            if let ExportGuard::BoundFlag(bound) = &export.guard {
                // 检查由 MIR 导出协议明确指定；后端不根据字段类型或名字猜 Python 状态。
                ins.push(Instruction::LoadStaticField {
                    volatile: false,
                    param0: FieldSource::Definition(refs.global_fields[bound]),
                });
                ins.push(Instruction::load_string(module.name.clone()));
                ins.push(Instruction::load_string(name));
                ins.push(Instruction::call(refs.bindings.check_module));
            }
            ins.push(Instruction::LoadStaticField {
                volatile: false,
                param0: FieldSource::Definition(refs.global_fields[gid]),
            });
            if matches!(export.guard, ExportGuard::ValueSentinel) {
                ins.push(Instruction::load_string(module.name.clone()));
                ins.push(Instruction::load_string(name));
                ins.push(Instruction::call(refs.bindings.load_attribute));
            }
            let ty = program.globals[*gid].ty.clone();
            crate::stackify::emit_box(&mut ins, &ty, refs, class_types);
            ins.push(Instruction::Return);
            let next = ins.len();
            ins[skip] = Instruction::BranchFalsy(next);
        }
        ins.push(Instruction::load_string(module.name.clone()));
        ins.push(Instruction::LoadArgument(0));
        ins.push(Instruction::call(refs.module_missing_attr));
        ins.push(Instruction::LoadNull);
        ins.push(Instruction::Return);
        let mut body = body::Method::new(ins);
        body.header.maximum_stack_size = 8;
        res[getter].body = Some(body);
        getters.push(Some(getter));

        let setter = res.push_method(
            module_class,
            Method::new(
                Accessibility::Private,
                MethodSignature::new(
                    false,
                    ReturnType::VOID,
                    vec![
                        Parameter::value(py_module_type.clone()),
                        Parameter::value(string_type.clone()),
                        Parameter::value(object_type.clone()),
                    ],
                ),
                Cow::Owned(format!("__mod_set_{index}")),
                None,
            ),
        );
        let mut ins = Vec::new();
        for export in &module.exports {
            let (name, gid) = (&export.name, &export.value);
            ins.push(Instruction::LoadArgument(1));
            ins.push(Instruction::load_string(name));
            ins.push(Instruction::call(refs.module_same_name));
            let skip = ins.len();
            ins.push(Instruction::BranchFalsy(0));
            if module.protected_exports.contains(name) {
                // 初始化 import 直接写静态槽，不经过本 setter；这里只拒绝外部重绑定，
                // 避免已经静态化的 typed catch 与实际 Python 属性身份悄悄分离。
                ins.push(Instruction::load_string("NotImplementedError"));
                ins.push(Instruction::load_string(format!(
                    "rebinding static exception import '{}.{}' is not supported",
                    module.name, name
                )));
                ins.push(Instruction::call(refs.make_exception));
                ins.push(Instruction::call(refs.raise_prepare));
                ins.push(Instruction::Throw);
                ins[skip] = Instruction::BranchFalsy(ins.len());
                continue;
            }
            ins.push(Instruction::LoadArgument(2));
            let ty = program.globals[*gid].ty.clone();
            match &ty {
                ryupy_mir::MirType::Dynamic => {}
                ryupy_mir::MirType::I64 => ins.push(Instruction::call(refs.cast_to_i64)),
                ryupy_mir::MirType::F64 => ins.push(Instruction::call(refs.cast_to_f64)),
                ryupy_mir::MirType::Bool => ins.push(Instruction::call(refs.cast_to_bool)),
                ryupy_mir::MirType::Str => ins.push(Instruction::call(refs.cast_to_str)),
                ryupy_mir::MirType::Class(_) => {
                    if let Some(mt) = crate::types::mir_type_to_method_type(&ty, class_types) {
                        ins.push(Instruction::CastClass {
                            skip_type_check: false,
                            param0: mt,
                        });
                    }
                }
                ryupy_mir::MirType::Unit | ryupy_mir::MirType::V128F64 => {}
            }
            ins.push(Instruction::StoreStaticField {
                volatile: false,
                param0: FieldSource::Definition(refs.global_fields[gid]),
            });
            if let ExportGuard::BoundFlag(bound) = &export.guard {
                // 值转换和存储均成功后才发布绑定；失败不能改变旧值/旧状态。
                ins.push(Instruction::LoadConstantInt32(1));
                ins.push(Instruction::StoreStaticField {
                    volatile: false,
                    param0: FieldSource::Definition(refs.global_fields[bound]),
                });
            }
            ins.push(Instruction::Return);
            let next = ins.len();
            ins[skip] = Instruction::BranchFalsy(next);
        }
        // 未声明的属性：模块允许动态增删属性，交给运行时字典。
        ins.push(Instruction::LoadArgument(0));
        ins.push(Instruction::LoadArgument(1));
        ins.push(Instruction::LoadArgument(2));
        ins.push(Instruction::call(refs.module_set_dynamic));
        ins.push(Instruction::Return);
        let mut body = body::Method::new(ins);
        body.header.maximum_stack_size = 8;
        res[setter].body = Some(body);
        setters.push(Some(setter));
    }

    ModuleAccessors { getters, setters }
}

/// 在 `__init_functions` 末尾追加 `PyModules.Register(...)` 调用。
pub(crate) fn append_module_registration(
    res: &mut Resolution<'static>,
    program: &LinkedProgram,
    refs: &RuntimeRefs,
    fn_indices: &FxHashMap<FnId, MethodIndex>,
    init_functions: MethodIndex,
    accessors: &ModuleAccessors,
    binding_context: Option<(
        &[crate::fixed_bindings::FixedI32Binding],
        &crate::fixed_binding_refs::BindingRefs,
    )>,
) {
    let Some(method) = res[init_functions].body.as_mut() else {
        return;
    };
    let instructions = &mut method.instructions;
    if matches!(instructions.last(), Some(Instruction::Return)) {
        instructions.pop();
    }
    for (index, (module_id, module)) in program.modules.iter().enumerate() {
        if module.execution == ryupy_mir::ModuleExecutionKind::InstanceGlobals {
            crate::instance_refs::append_registration(
                instructions,
                module,
                fn_indices[&module.init],
                refs.instances.as_ref().expect("instance module references"),
            );
            continue;
        }
        let declared = binding_context.is_some_and(|(bindings, _)| {
            bindings.iter().any(|binding| binding.module == module_id)
        });
        instructions.push(Instruction::load_string(module.name.clone()));
        if !declared {
            instructions.push(Instruction::load_string(module.package.clone()));
            instructions.push(Instruction::load_string(module.file.clone()));
        }
        // 实际构建显式启用定位及公开槽；旧dump保原Register指令。
        let located = !declared
            && (module.file_backed_by_global
                || !matches!(
                    &module.location,
                    ryupy_mir::ModuleLocation::LegacyDiagnostic
                ));
        if located {
            let (kind, value) = match &module.location {
                ryupy_mir::ModuleLocation::LegacyDiagnostic => (0, ""),
                ryupy_mir::ModuleLocation::SourceAbsolute(value) => (1, value.as_str()),
                ryupy_mir::ModuleLocation::ExecutableRelative(value) => (2, value.as_str()),
            };
            instructions.push(Instruction::LoadConstantInt64(kind));
            instructions.push(Instruction::load_string(value));
            instructions.push(Instruction::LoadConstantInt32(i32::from(
                module.file_backed_by_global,
            )));
        }
        // 所有真实定位模块显式传递resolver身份，非包也不得由路径反推。
        if located {
            instructions.push(Instruction::LoadConstantInt32(i32::from(module.is_package)));
        }
        for (method, ctor) in [
            (fn_indices[&module.init], refs.module_init_ctor),
            (
                accessors.getters[index].expect("legacy getter"),
                refs.module_get_ctor,
            ),
            (
                accessors.setters[index].expect("legacy setter"),
                refs.module_set_ctor,
            ),
        ] {
            instructions.push(Instruction::LoadNull);
            instructions.push(Instruction::LoadMethodPointer(MethodSource::User(
                UserMethod::Definition(method),
            )));
            instructions.push(Instruction::NewObject(UserMethod::Reference(ctor)));
        }
        instructions.push(Instruction::call(if declared {
            binding_context
                .expect("validated binding references")
                .1
                .register
        } else if located {
            refs.module_register_source_located
        } else {
            refs.module_register
        }));
    }
    instructions.push(Instruction::Return);
    method.header.maximum_stack_size = 32;
}
