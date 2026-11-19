//! 定义预登记前统一公开动态槽：保留装饰/运行时类路径，只扩模块函数名碰撞。
//! FuncId/ClassId是内部身份；公开名称只有源码定义成功后才写入对应模块槽。

use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::Ranged;
use ryupy_syntax::{ParsedModule, ast};

use crate::hir::{ModuleVar, ModuleVarId, Name};
use crate::scope::GlobalScope;

pub(crate) fn register(
    parsed: &ParsedModule,
    globals: &mut GlobalScope,
    vars: &mut Arena<ModuleVar>,
    slots: &mut FxHashMap<Name, ModuleVarId>,
) {
    // 同名定义若包含装饰链，全部定义位置共用公开动态槽；失败时保留此前绑定。
    let definitions = crate::module_definitions::definitions(&parsed.module.body);
    globals.ordinary_mi = crate::ordinary_mi::definitions(&parsed.module.body, globals);
    for statement in &definitions {
        if let ast::Stmt::ClassDef(class) = statement
            && globals.ordinary_mi.contains(&class.range)
        {
            let name = Name::from(class.name.as_str());
            globals.dynamic_names.insert(name.clone());
            ensure_slot(name, class.range, vars, slots);
        }
    }
    for stmt in &definitions {
        if globals.instance_globals {
            let name = match stmt {
                ast::Stmt::FunctionDef(def) => Some(Name::from(def.name.as_str())),
                ast::Stmt::ClassDef(class) => Some(Name::from(class.name.as_str())),
                _ => None,
            };
            if let Some(name) = name {
                globals.dynamic_names.insert(name.clone());
                ensure_slot(name, stmt.range(), vars, slots);
            }
        }
        match stmt {
            ast::Stmt::FunctionDef(def) if !def.decorator_list.is_empty() => {
                globals.dynamic_names.insert(Name::from(def.name.as_str()));
            }
            ast::Stmt::ClassDef(class)
                if crate::decorators::has_runtime_class_decorators(
                    class,
                    &globals.dataclass_markers,
                ) || matches!(
                    crate::native_bases::resolve(class, globals),
                    Some((
                        crate::hir::NativeBase::Enum | crate::hir::NativeBase::Flag,
                        _
                    ))
                ) =>
            {
                globals
                    .dynamic_names
                    .insert(Name::from(class.name.as_str()));
            }
            _ => {}
        }
    }
    // 单模块闭包先补齐运行时基类及全部后代的公开绑定，再统一分配槽。
    crate::runtime_bases::register(&parsed.module.body, globals, slots);
    // 原生Enum即使没有装饰器，也只在完成成员转换之后发布到动态公开槽。
    let mut class_names = FxHashSet::default();
    for stmt in &definitions {
        if let ast::Stmt::ClassDef(class) = stmt {
            let name = Name::from(class.name.as_str());
            class_names.insert(name.clone());
            if globals.dynamic_names.contains(&name) {
                ensure_slot(name, class.range(), vars, slots);
            }
        }
    }

    // 只选直接顶层def与已有模块槽相交，或多个直接同名def；局部绑定不参与。
    // 涉及class的名字完全沿旧dynamic_names准入，既不绕过旧诊断也不收紧旧成功域。
    let mut counts = FxHashMap::<Name, usize>::default();
    for stmt in &parsed.module.body {
        if let ast::Stmt::FunctionDef(def) = stmt {
            *counts.entry(Name::from(def.name.as_str())).or_default() += 1;
        }
    }
    for stmt in &parsed.module.body {
        let ast::Stmt::FunctionDef(def) = stmt else {
            continue;
        };
        let name = Name::from(def.name.as_str());
        if !class_names.contains(&name) && (slots.contains_key(&name) || counts[&name] > 1) {
            globals.dynamic_names.insert(name.clone());
            ensure_slot(name, def.range(), vars, slots);
        }
    }
    // 删除class名称同样必须公开真实槽；内部ClassId仅保留编译器身份。
    // 放在原准入登记之后，区分旧已支持的装饰类与仅因删除扩出的绑定。
    for stmt in &definitions {
        let name = match stmt {
            ast::Stmt::FunctionDef(def) => Name::from(def.name.as_str()),
            ast::Stmt::ClassDef(class) => Name::from(class.name.as_str()),
            _ => continue,
        };
        if globals.deleted_names.contains(&name) {
            if globals.dynamic_names.insert(name.clone()) {
                globals.delete_only_definitions.insert(name.clone());
            }
            ensure_slot(name, stmt.range(), vars, slots);
        }
    }
}

fn ensure_slot(
    name: Name,
    range: ryupy_base::TextRange,
    vars: &mut Arena<ModuleVar>,
    slots: &mut FxHashMap<Name, ModuleVarId>,
) {
    let binding = *slots.entry(name.clone()).or_insert_with(|| {
        vars.alloc(ModuleVar {
            name,
            range,
            annotation: None,
            is_global: true,
            may_delete: false,
        })
    });
    // 既有赋值/for槽也必须装箱，不能依第一次整数写入把未来函数对象槽推成I64。
    vars[binding].is_global = true;
}
