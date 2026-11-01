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
use ryupy_mir::{FnId, LinkedProgram};

use crate::stackify::RuntimeRefs;
use crate::types::ClassTypeMap;

/// 每个模块的对象属性读写入口。
pub(crate) struct ModuleAccessors {
    pub(crate) getters: Vec<MethodIndex>,
    pub(crate) setters: Vec<MethodIndex>,
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
        for (name, gid) in &module.exports {
            ins.push(Instruction::LoadArgument(0));
            ins.push(Instruction::load_string(name));
            ins.push(Instruction::call(refs.module_same_name));
            let skip = ins.len();
            ins.push(Instruction::BranchFalsy(0));
            ins.push(Instruction::LoadStaticField {
                volatile: false,
                param0: FieldSource::Definition(refs.global_fields[gid]),
            });
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
        getters.push(getter);

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
        for (name, gid) in &module.exports {
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
                ryupy_mir::MirType::Unit => {}
            }
            ins.push(Instruction::StoreStaticField {
                volatile: false,
                param0: FieldSource::Definition(refs.global_fields[gid]),
            });
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
        setters.push(setter);
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
) {
    let Some(method) = res[init_functions].body.as_mut() else {
        return;
    };
    let instructions = &mut method.instructions;
    if matches!(instructions.last(), Some(Instruction::Return)) {
        instructions.pop();
    }
    for (index, (_, module)) in program.modules.iter().enumerate() {
        instructions.push(Instruction::load_string(module.name.clone()));
        instructions.push(Instruction::load_string(module.package.clone()));
        instructions.push(Instruction::load_string(module.file.clone()));
        for (method, ctor) in [
            (fn_indices[&module.init], refs.module_init_ctor),
            (accessors.getters[index], refs.module_get_ctor),
            (accessors.setters[index], refs.module_set_ctor),
        ] {
            instructions.push(Instruction::LoadNull);
            instructions.push(Instruction::LoadMethodPointer(MethodSource::User(
                UserMethod::Definition(method),
            )));
            instructions.push(Instruction::NewObject(UserMethod::Reference(ctor)));
        }
        instructions.push(Instruction::call(refs.module_register));
    }
    instructions.push(Instruction::Return);
    method.header.maximum_stack_size = 32;
}
