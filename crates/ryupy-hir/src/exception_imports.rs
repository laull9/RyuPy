//! 原生模块的有限静态身份：异常与显式白名单基类共用稳定导入分析。
//! 只接受 driver 确认无同名源码的直接导入，不按名字猜测第三方类语义。
//! 别名必须在本模块中保持稳定；动态类型表达式留给明确诊断，不按属性拼写猜类型。

use std::cell::Cell;

use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Ranged, TextSize};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};

use crate::hir::Name;

#[derive(Debug)]
pub(crate) struct ExceptionImport {
    module: String,
    member: Option<String>,
    available: TextSize,
    used: Cell<bool>,
}

impl ExceptionImport {
    /// 模块和函数候选共享已认证模块/member映射，但不共享别名作用域。
    pub(crate) fn new(module: String, member: Option<String>, available: TextSize) -> Self {
        Self {
            module,
            member,
            available,
            used: Cell::new(false),
        }
    }

    /// 与异常共用稳定导入身份分析；原生普通基类不参与CLR异常类型解析。
    pub(crate) fn resolve_native_base(
        &self,
        member: Option<&str>,
        position: TextSize,
    ) -> Option<crate::hir::NativeBase> {
        let member = match (&self.member, member) {
            (Some(imported), None) => imported.as_str(),
            (None, Some(member)) => member,
            _ => return None,
        };
        let kind = match (self.module.as_str(), member) {
            ("unittest", "TestCase") => crate::hir::NativeBase::UnittestTestCase,
            ("enum", "Enum") => crate::hir::NativeBase::Enum,
            ("enum", "Flag") => crate::hir::NativeBase::Flag,
            _ => return None,
        };
        if position < self.available {
            return None;
        }
        self.used.set(true);
        Some(kind)
    }

    pub(crate) fn resolve(&self, member: Option<&str>, position: TextSize) -> Option<&'static str> {
        let resolved = self.peek(member, position);
        if resolved.is_some() {
            self.used.set(true);
        }
        resolved
    }

    /// 首轮异常分派判定不把mixed链的模块导出误冻结。
    pub(crate) fn peek(&self, member: Option<&str>, position: TextSize) -> Option<&'static str> {
        if position < self.available {
            return None;
        }
        let member = match (&self.member, member) {
            (Some(imported), None) => imported.as_str(),
            (None, Some(member)) => member,
            _ => return None,
        };
        match (self.module.as_str(), member) {
            ("json", "JSONDecodeError") => Some("json.JSONDecodeError"),
            ("zipfile", "BadZipFile") => Some("zipfile.BadZipFile"),
            ("getopt", "GetoptError" | "error") => Some("getopt.GetoptError"),
            ("optparse", "OptParseError") => Some("optparse.OptParseError"),
            ("optparse", "OptionError") => Some("optparse.OptionError"),
            ("optparse", "OptionConflictError") => Some("optparse.OptionConflictError"),
            ("optparse", "OptionValueError") => Some("optparse.OptionValueError"),
            ("optparse", "BadOptionError") => Some("optparse.BadOptionError"),
            ("optparse", "AmbiguousOptionError") => Some("optparse.AmbiguousOptionError"),
            ("dataclasses", "FrozenInstanceError") => Some("dataclasses.FrozenInstanceError"),
            ("re", "error") => Some("re.error"),
            ("io", "UnsupportedOperation") => Some("io.UnsupportedOperation"),
            _ => None,
        }
    }
}

pub(crate) fn collect(stmts: &[ast::Stmt], native: &[&str]) -> FxHashMap<Name, ExceptionImport> {
    let mut imports = FxHashMap::default();
    let mut repeated = FxHashSet::default();
    // 仅无条件顶层导入。条件导入是否执行不能由类型化 catch 静态推断。
    for stmt in stmts {
        for request in crate::imports::collect_imports(std::slice::from_ref(stmt), "") {
            let bind = Name::from(request.bind.as_str());
            if imports.contains_key(&bind) {
                repeated.insert(bind.clone());
            }
            if matches!(stmt, ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_))
                && native.contains(&request.module.as_str())
            {
                imports.insert(
                    bind,
                    ExceptionImport {
                        module: request.module,
                        member: request.name,
                        available: stmt.range().end(),
                        used: Cell::new(false),
                    },
                );
            } else {
                repeated.insert(bind);
            }
        }
    }
    for (name, _) in crate::scope::collect_assigned_names(stmts) {
        repeated.insert(name);
    }
    for definition in crate::module_definitions::definitions(stmts) {
        match definition {
            ast::Stmt::FunctionDef(def) => {
                repeated.insert(Name::from(def.name.as_str()));
            }
            ast::Stmt::ClassDef(def) => {
                repeated.insert(Name::from(def.name.as_str()));
            }
            _ => {}
        }
    }
    // 删除也会让静态身份失效。函数局部同名赋值不影响模块导入；global 写入则影响。
    struct Writes(FxHashSet<Name>, usize);
    impl<'a> Visitor<'a> for Writes {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            if let ast::Stmt::FunctionDef(function) = stmt {
                let globals = crate::scope::collect_global_names(&function.body);
                let writes = crate::scope::collect_assigned_names(&function.body);
                for (name, _) in writes {
                    if globals.contains(&name) {
                        self.0.insert(name);
                    }
                }
                // 删除 global 名也必须取消，保守覆盖函数里声明的全部 global。
                self.0.extend(globals);
                self.1 += 1;
                walk_stmt(self, stmt);
                self.1 -= 1;
                return;
            }
            if let ast::Stmt::Delete(delete) = stmt {
                if self.1 == 0 {
                    for target in &delete.targets {
                        if let ast::Expr::Name(name) = target {
                            self.0.insert(Name::from(name.id.as_str()));
                        }
                    }
                }
            }
            walk_stmt(self, stmt);
        }
    }
    let mut writes = Writes(repeated, 0);
    for stmt in stmts {
        writes.visit_stmt(stmt);
    }
    imports.retain(|name, _| !writes.0.contains(name));
    imports
}

/// 仅实际参与静态handler或原生基类的导入别名需要保护；普通导入仍可被外部改写。
pub(crate) fn protected_names(imports: &FxHashMap<Name, ExceptionImport>) -> Vec<String> {
    let mut names: Vec<_> = imports
        .iter()
        .filter(|(_, binding)| binding.used.get())
        .map(|(name, _)| name.as_str().to_owned())
        .collect();
    names.sort();
    names
}
