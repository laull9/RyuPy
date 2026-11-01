//! 数值字面量池与用户异常类的 CLR 工厂初始化。
//!
//! 循环里的 `1`、`0.5` 等动态数值字面量在模块初始化时装箱一次存进静态字段
//! （`__init_literals`），循环体直接读字段；用户异常类的工厂委托同样在
//! 模块初始化时构造（`__init_class_factories`）。

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{LinkedProgram, Operand, Rvalue, Statement};

use crate::stackify_refs::{LiteralInit, RuntimeRefs, boxed_literal_init, boxed_literal_key};

/// 收集所有 `Box` 常量里可以池化的数值字面量。
pub(crate) fn collect_literal_inits(program: &LinkedProgram) -> Vec<(String, LiteralInit)> {
    let mut out: FxHashMap<String, LiteralInit> = FxHashMap::default();
    for (_, func) in program.functions.iter() {
        for (_, block) in func.body.blocks.iter() {
            for stmt in &block.stmts {
                let Statement::Assign {
                    value: Rvalue::Box { value, from },
                    ..
                } = stmt
                else {
                    continue;
                };
                let Operand::Const(constant) = value else {
                    continue;
                };
                let Some(key) = boxed_literal_key(constant, from) else {
                    continue;
                };
                if let Some(init) = boxed_literal_init(constant, from) {
                    out.entry(key).or_insert(init);
                }
            }
        }
    }
    let mut items: Vec<_> = out.into_iter().collect();
    items.sort_by(|a, b| a.0.cmp(&b.0));
    items
}

/// 为字面量池分配静态字段，并生成 `__init_literals` 初始化方法。
pub(crate) fn emit_literal_init(
    res: &mut Resolution<'static>,
    module_class: TypeIndex,
    inits: &[(String, LiteralInit)],
    object_member: &MemberType,
    refs: &RuntimeRefs,
) -> (FxHashMap<String, FieldIndex>, MethodIndex) {
    let mut fields = FxHashMap::default();
    let method = res.push_method(
        module_class,
        Method::new(
            Accessibility::Private,
            msig! { static void () },
            "__init_literals",
            None,
        ),
    );
    let mut ins = Vec::new();
    for (index, (key, init)) in inits.iter().enumerate() {
        let field = res.push_field(
            module_class,
            Field::static_member(
                Accessibility::Private,
                format!("__lit_{index}"),
                object_member.clone(),
            ),
        );
        match init {
            LiteralInit::I64(value) => {
                ins.push(Instruction::LoadConstantInt64(*value));
                ins.push(Instruction::call(refs.box_i64));
            }
            LiteralInit::F64(value) => {
                ins.push(Instruction::LoadConstantFloat64(*value));
                ins.push(Instruction::BoxValue(BaseType::Float64.into()));
            }
            LiteralInit::Bool(value) => {
                ins.push(Instruction::LoadConstantInt32(if *value { 1 } else { 0 }));
                ins.push(Instruction::call(refs.box_bool));
            }
            LiteralInit::BigInt(text) => {
                ins.push(Instruction::load_string(text.clone()));
                ins.push(Instruction::call(refs.dyn_from_decimal));
            }
        }
        ins.push(Instruction::StoreStaticField {
            volatile: false,
            param0: FieldSource::Definition(field),
        });
        fields.insert(key.clone(), field);
    }
    ins.push(Instruction::Return);
    let mut body = body::Method::new(ins);
    body.header.maximum_stack_size = 8;
    res[method].body = Some(body);
    (fields, method)
}

/// 生成 `__init_class_factories`：给用户异常类的静态槽位写入 CLR 工厂委托。
pub(crate) fn emit_init_class_factories(
    res: &mut Resolution<'static>,
    module_class: TypeIndex,
    program: &LinkedProgram,
    refs: &RuntimeRefs,
    factories: &FxHashMap<ryupy_mir::ClassDefId, MethodIndex>,
) -> MethodIndex {
    let method_idx = res.push_method(
        module_class,
        Method::new(
            Accessibility::Private,
            msig! { static void () },
            "__init_class_factories",
            None,
        ),
    );
    let mut ins = Vec::new();
    for (cls_id, factory) in factories {
        let cls = &program.classes[*cls_id];
        let Some(global) = cls.factory_global else {
            continue;
        };
        let Some(&field) = refs.global_fields.get(&global) else {
            continue;
        };
        ins.push(Instruction::LoadNull);
        ins.push(Instruction::LoadMethodPointer(MethodSource::User(
            UserMethod::Definition(*factory),
        )));
        ins.push(Instruction::NewObject(UserMethod::Reference(
            refs.exception_factory_ctor,
        )));
        ins.push(Instruction::StoreStaticField {
            volatile: false,
            param0: FieldSource::Definition(field),
        });
    }
    ins.push(Instruction::Return);
    let mut body = body::Method::new(ins);
    body.header.maximum_stack_size = 8;
    res[method_idx].body = Some(body);
    method_idx
}
