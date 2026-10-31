//! 一等函数对象与参数绑定的代码生成。
//!
//! 每个用户函数生成三样东西：
//! 1. **thunk**：统一签名 `object (object[], string[], object[])`。先调用
//!    `PyBinder.Bind` 完成 Python 的参数绑定（默认值 / `*args` / `**kwargs` / 重复绑定报错），
//!    再把绑定结果拆箱成静态方法需要的类型，最后装箱返回值。
//! 2. **调用入口**：模块静态字段 `PyFunction`，持有 thunk 委托、签名与默认值数组。
//! 3. **模块初始化方法** `__init_functions`：分配默认值数组、签名与函数对象。
//!
//! 默认值数组先分配、由 `__main` 开头的 MIR 语句填充。这样 `PyFunction` 持有的是
//! 同一个数组引用，多次调用共享同一份可变默认值（与 CPython 一致）。

use std::borrow::Cow;

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{FnId, Function, GlobalId, MirType, Program};

use crate::stackify::RuntimeRefs;
use crate::types::ClassTypeMap;

/// 为 MIR 的模块级 `GlobalDecl` 分配静态字段。
///
/// 函数对象、默认值数组与签名有各自的 CLR 类型（`object` / `object[]` / `PySignature`），
/// MIR 侧统一记成 `Dynamic`；这里按用途覆盖成具体类型，避免每次读取都 `castclass`。
pub(crate) fn allocate_global_fields(
    res: &mut Resolution<'static>,
    module_class: TypeIndex,
    program: &Program,
    class_types: &ClassTypeMap,
    object_member: &MemberType,
    signature_member: &MemberType,
) -> FxHashMap<GlobalId, FieldIndex> {
    let mut roles: FxHashMap<GlobalId, MemberType> = FxHashMap::default();
    for (_, func) in program.functions.iter() {
        if let Some(global) = func.function_global {
            roles.insert(global, object_member.clone());
        }
        if let Some(global) = func.defaults_global {
            roles.insert(global, BaseType::vector(object_member.clone()).into());
        }
        if let Some(global) = func.signature_global {
            roles.insert(global, signature_member.clone());
        }
    }

    let mut fields = FxHashMap::default();
    for (id, global) in program.globals.iter() {
        let member_ty = roles
            .remove(&id)
            .or_else(|| crate::types::mir_type_to_member_type(&global.ty, class_types))
            .unwrap_or_else(|| object_member.clone());
        let field = res.push_field(
            module_class,
            Field::static_member(Accessibility::Private, global.name.clone(), member_ty),
        );
        fields.insert(id, field);
    }
    fields
}

/// 生成一个函数的 thunk。
pub(crate) fn emit_thunk(
    res: &mut Resolution<'static>,
    module_class: TypeIndex,
    func: &Function,
    static_method: MethodIndex,
    refs: &RuntimeRefs,
    class_types: &ClassTypeMap,
    index: usize,
) -> MethodIndex {
    let method_idx = res.push_method(
        module_class,
        Method::new(
            Accessibility::Private,
            msig! { static object (object[], string[], object[]) },
            Cow::Owned(format!("__thunk_{index}")),
            None,
        ),
    );

    let object_type = refs.object_type.clone();
    let object_array: MethodType = BaseType::vector(object_type.clone()).into();
    // 1. bound = PyBinder.Bind(sig, args, kwNames, kwValues, defaults)
    let mut ins = vec![
        Instruction::LoadStaticField {
            volatile: false,
            param0: FieldSource::Definition(field_of(func.signature_global, refs)),
        },
        Instruction::LoadArgument(0),
        Instruction::LoadArgument(1),
        Instruction::LoadArgument(2),
        Instruction::LoadStaticField {
            volatile: false,
            param0: FieldSource::Definition(field_of(func.defaults_global, refs)),
        },
        Instruction::call(refs.bind_args),
        Instruction::StoreLocal(0),
    ];

    // 2. 按参数顺序拆箱
    for (index, param) in func.params.iter().enumerate() {
        ins.push(Instruction::LoadLocal(0));
        ins.push(Instruction::LoadConstantInt32(index as i32));
        ins.push(Instruction::LoadElement {
            skip_range_check: false,
            skip_null_check: false,
            param0: object_type.clone(),
        });
        let ty = func.body.locals[*param].ty.clone();
        unbox_parameter(&mut ins, &ty, refs, class_types);
    }

    // 3. 调用静态方法
    ins.push(Instruction::call(static_method));

    // 4. 装箱返回值
    match &func.ret {
        MirType::Unit => {
            ins.push(Instruction::call(refs.dyn_none));
        }
        MirType::I64 | MirType::F64 | MirType::Bool => {
            if let Some(method_ty) = crate::types::mir_type_to_method_type(&func.ret, class_types) {
                ins.push(Instruction::BoxValue(method_ty));
            }
        }
        // 引用类型 / Dynamic 已经是 object，无需装箱
        _ => {}
    }
    ins.push(Instruction::Return);

    let mut body = body::Method::with_locals(vec![LocalVariable::new(object_array)], ins);
    body.header.maximum_stack_size = func.params.len() + 8;
    res[method_idx].body = Some(body);
    method_idx
}

/// 生成 `__init_functions`：分配默认值数组、签名与函数对象。
pub(crate) fn emit_init_functions(
    res: &mut Resolution<'static>,
    module_class: TypeIndex,
    program: &Program,
    refs: &RuntimeRefs,
    thunks: &FxHashMap<FnId, MethodIndex>,
) -> MethodIndex {
    let method_idx = res.push_method(
        module_class,
        Method::new(
            Accessibility::Private,
            msig! { static void () },
            "__init_functions",
            None,
        ),
    );

    let object_type = refs.object_type.clone();
    let string_type: MethodType = BaseType::String.into();
    let mut ins = Vec::new();

    for (fn_id, func) in program.functions.iter() {
        let (Some(function_global), Some(defaults_global), Some(signature_global)) = (
            func.function_global,
            func.defaults_global,
            func.signature_global,
        ) else {
            continue;
        };
        let function_field = field_of(Some(function_global), refs);
        let defaults_field = field_of(Some(defaults_global), refs);
        let signature_field = field_of(Some(signature_global), refs);

        // defaults = new object[参数个数]
        ins.push(Instruction::LoadConstantInt32(func.params.len() as i32));
        ins.push(Instruction::NewArray(object_type.clone()));
        ins.push(Instruction::StoreStaticField {
            volatile: false,
            param0: defaults_field.into(),
        });

        // signature = PySignature.Create(name, names, kinds, hasDefault)
        ins.push(Instruction::load_string(func.name.clone()));
        emit_string_array(
            &mut ins,
            func.param_meta.iter().map(|p| p.name.clone()),
            &string_type,
        );
        emit_string_array(
            &mut ins,
            func.param_meta.iter().map(|p| kind_name(p.kind).to_owned()),
            &string_type,
        );
        emit_string_array(
            &mut ins,
            func.param_meta
                .iter()
                .map(|p| if p.has_default { "1" } else { "0" }.to_owned()),
            &string_type,
        );
        ins.push(Instruction::call(refs.signature_create));
        ins.push(Instruction::StoreStaticField {
            volatile: false,
            param0: signature_field.into(),
        });

        // function = new PyFunction(name, sig, new PyCall(null, &thunk), defaults)
        ins.push(Instruction::load_string(func.name.clone()));
        ins.push(Instruction::LoadStaticField {
            volatile: false,
            param0: FieldSource::Definition(signature_field),
        });
        ins.push(Instruction::LoadNull);
        let thunk = thunks
            .get(&fn_id)
            .copied()
            .unwrap_or_else(|| panic!("函数 {:?} 缺少 thunk", func.name));
        ins.push(Instruction::LoadMethodPointer(thunk.into()));
        ins.push(Instruction::NewObject(UserMethod::Reference(
            refs.py_call_ctor,
        )));
        ins.push(Instruction::LoadStaticField {
            volatile: false,
            param0: FieldSource::Definition(defaults_field),
        });
        ins.push(Instruction::NewObject(UserMethod::Reference(
            refs.py_function_ctor,
        )));
        ins.push(Instruction::StoreStaticField {
            volatile: false,
            param0: function_field.into(),
        });
    }
    ins.push(Instruction::Return);

    let mut body = body::Method::new(ins);
    body.header.maximum_stack_size = 8;
    res[method_idx].body = Some(body);
    method_idx
}

/// 取全局槽位对应的字段下标；缺失时给出明确的内部错误。
fn field_of(global: Option<GlobalId>, refs: &RuntimeRefs) -> FieldIndex {
    let global = global.expect("函数缺少必要的静态槽位");
    refs.global_fields
        .get(&global)
        .copied()
        .unwrap_or_else(|| panic!("全局槽位 {:?} 没有对应的静态字段", global))
}

/// 把绑定结果（装箱值）转换成静态参数类型。
fn unbox_parameter(
    ins: &mut Vec<Instruction>,
    ty: &MirType,
    refs: &RuntimeRefs,
    class_types: &ClassTypeMap,
) {
    match ty {
        MirType::I64 => ins.push(Instruction::call(refs.cast_to_i64)),
        MirType::F64 => ins.push(Instruction::call(refs.cast_to_f64)),
        MirType::Bool => ins.push(Instruction::call(refs.cast_to_bool)),
        MirType::Str => ins.push(Instruction::call(refs.cast_to_str)),
        MirType::Class(id) => {
            let (type_idx, is_value_type) = class_types.get(*id);
            let method_ty: MethodType = if is_value_type {
                BaseType::valuetype(type_idx).into()
            } else {
                BaseType::class(type_idx).into()
            };
            if is_value_type {
                ins.push(Instruction::UnboxIntoValue(method_ty));
            } else {
                ins.push(Instruction::CastClass {
                    skip_type_check: false,
                    param0: method_ty,
                });
            }
        }
        // Dynamic 已经是 object；Unit 不会出现在参数里
        MirType::Dynamic | MirType::Unit => {}
    }
}

/// 参数种类 → `PySignature.Create` 使用的字符串（必须与 C# 侧解析一致）。
fn kind_name(kind: ryupy_mir::ParamKind) -> &'static str {
    match kind {
        ryupy_mir::ParamKind::PositionalOnly => "posonly",
        ryupy_mir::ParamKind::PositionalOrKeyword => "pos",
        ryupy_mir::ParamKind::VarPositional => "varpos",
        ryupy_mir::ParamKind::KeywordOnly => "kwonly",
        ryupy_mir::ParamKind::VarKeyword => "varkw",
    }
}

/// 构造一个 `string[]` 常量数组。
fn emit_string_array(
    ins: &mut Vec<Instruction>,
    values: impl Iterator<Item = String>,
    string_type: &MethodType,
) {
    let values: Vec<String> = values.collect();
    ins.push(Instruction::LoadConstantInt32(values.len() as i32));
    ins.push(Instruction::NewArray(string_type.clone()));
    for (index, value) in values.into_iter().enumerate() {
        ins.push(Instruction::Duplicate);
        ins.push(Instruction::LoadConstantInt32(index as i32));
        ins.push(Instruction::load_string(value));
        ins.push(Instruction::StoreElement {
            skip_type_check: false,
            skip_range_check: false,
            skip_null_check: false,
            param0: string_type.clone(),
        });
    }
}
