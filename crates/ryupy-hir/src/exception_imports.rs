//! 原生模块的有限静态身份：异常与显式白名单基类共用稳定导入分析。
//! 只接受 driver 确认无同名源码的直接导入，不按名字猜测第三方类语义。
//! 别名必须在本模块中保持稳定；动态类型表达式留给明确诊断，不按属性拼写猜类型。

use std::cell::Cell;

use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Ranged, TextSize};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

use crate::hir::Name;

#[derive(Debug)]
pub(crate) struct ExceptionImport {
    module: String,
    member: Option<String>,
    bind_root: bool,
    available: TextSize,
    used: Cell<bool>,
}

impl ExceptionImport {
    /// 模块和函数候选共享已认证模块/member映射，但不共享别名作用域。
    pub(crate) fn new(
        module: String,
        member: Option<String>,
        bind_root: bool,
        available: TextSize,
    ) -> Self {
        Self {
            module,
            member,
            bind_root,
            available,
            used: Cell::new(false),
        }
    }

    /// 只认证叶模块别名或from成员；多段无别名导入经过可变父模块，不能证明身份。
    /// bind_root必须保留，不能把xml误当已绑定的ElementTree；父链别名/effect不在此证明域。
    fn normalized_member<'a>(&'a self, path: &[&'a str], position: TextSize) -> Option<&'a str> {
        if position < self.available || self.bind_root {
            return None;
        }
        if let Some(member) = &self.member {
            return path.is_empty().then_some(member.as_str());
        }
        let mut path = path.iter().copied();
        let member = path.next()?;
        path.next().is_none().then_some(member)
    }

    /// 与异常共用稳定导入身份分析；原生普通基类不参与CLR异常类型解析。
    pub(crate) fn resolve_native_base(
        &self,
        path: &[&str],
        position: TextSize,
    ) -> Option<crate::hir::NativeBase> {
        let kind = self.peek_native_base(path, position)?;
        self.used.set(true);
        Some(kind)
    }

    fn peek_native_base(
        &self,
        path: &[&str],
        position: TextSize,
    ) -> Option<crate::hir::NativeBase> {
        let member = self.normalized_member(path, position)?;
        match (self.module.as_str(), member) {
            ("unittest", "TestCase") => Some(crate::hir::NativeBase::UnittestTestCase),
            ("enum", "Enum") => Some(crate::hir::NativeBase::Enum),
            ("enum", "Flag") => Some(crate::hir::NativeBase::Flag),
            _ => None,
        }
    }

    /// auto只准入表达式语法，既不冻结运行时导出，也不把调用折叠为生成值。
    pub(crate) fn peek_enum_auto(&self, path: &[&str], position: TextSize) -> bool {
        self.module == "enum" && self.normalized_member(path, position) == Some("auto")
    }

    /// ABC只选择需延后发布的冷类合同，不把导出冻结成静态布局身份。
    pub(crate) fn peek_abc_base(&self, path: &[&str], position: TextSize) -> bool {
        self.module == "collections.abc"
            && self
                .normalized_member(path, position)
                .is_some_and(|name| crate::abc_bases::NAMES.contains(&name))
    }

    /// 只让准确类型导出写入取消叶模块身份证明；无关属性不扩大失效域。
    /// from导入已复制类型引用，随后写该类型的属性不是重绑引用本身。
    pub(crate) fn affected_by_attribute_write(&self, path: &[String]) -> bool {
        if self.member.is_some() {
            return false;
        }
        let path: Vec<_> = path.iter().map(String::as_str).collect();
        self.peek(&path, self.available).is_some()
            || self.peek_native_base(&path, self.available).is_some()
            || self.peek_abc_base(&path, self.available)
    }

    pub(crate) fn resolve(&self, path: &[&str], position: TextSize) -> Option<&'static str> {
        let resolved = self.peek(path, position);
        if resolved.is_some() {
            self.used.set(true);
        }
        resolved
    }

    /// 首轮异常分派判定不把mixed链的模块导出误冻结。
    pub(crate) fn peek(&self, path: &[&str], position: TextSize) -> Option<&'static str> {
        let member = self.normalized_member(path, position)?;
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
            ("xml.etree.ElementTree", "ParseError") => Some("xml.etree.ElementTree.ParseError"),
            ("io", "UnsupportedOperation") => Some("io.UnsupportedOperation"),
            _ => None,
        }
    }
}

/// 纯 Name/Attribute 链的实际绑定根与全部剩余成员；不接受调用、索引或猜测。
pub(crate) fn attribute_path(expr: &ast::Expr) -> Option<(&str, Vec<&str>)> {
    let mut current = expr;
    let mut path = Vec::new();
    while let ast::Expr::Attribute(attr) = current {
        path.push(attr.attr.as_str());
        current = &attr.value;
    }
    let ast::Expr::Name(name) = current else {
        return None;
    };
    path.reverse();
    Some((name.id.as_str(), path))
}

/// 推导式迭代目标只在自己的隐式作用域绑定；属性/下标目标不绑定其接收者名字。
pub(crate) fn comprehension_bindings(
    comp: crate::comprehension_ast::Comprehension<'_>,
) -> FxHashSet<Name> {
    fn target(expr: &ast::Expr, names: &mut FxHashSet<Name>) {
        match expr {
            ast::Expr::Name(name) => {
                names.insert(Name::from(name.id.as_str()));
            }
            ast::Expr::Tuple(tuple) => {
                for item in &tuple.elts {
                    target(item, names);
                }
            }
            ast::Expr::List(list) => {
                for item in &list.elts {
                    target(item, names);
                }
            }
            ast::Expr::Starred(starred) => target(&starred.value, names),
            _ => {}
        }
    }
    let mut names = FxHashSet::default();
    for generator in comp.generators {
        target(&generator.target, &mut names);
    }
    names
}

/// 最左iterable由调用方在外层扫描；其余表达式保留局部遮蔽，但walrus仍由访问者登记。
pub(crate) fn walk_comprehension_body<'a>(
    visitor: &mut impl Visitor<'a>,
    comp: crate::comprehension_ast::Comprehension<'a>,
) {
    for (index, generator) in comp.generators.iter().enumerate() {
        if index != 0 {
            visitor.visit_expr(&generator.iter);
        }
        visitor.visit_expr(&generator.target);
        for condition in &generator.ifs {
            visitor.visit_expr(condition);
        }
    }
    if let Some(key) = comp.key {
        visitor.visit_expr(key);
    }
    visitor.visit_expr(comp.elt);
}

pub(crate) fn collect(stmts: &[ast::Stmt], native: &[&str]) -> FxHashMap<Name, ExceptionImport> {
    let mut imports: FxHashMap<Name, ExceptionImport> = FxHashMap::default();
    let mut repeated = FxHashSet::default();
    // 仅无条件顶层导入。条件导入是否执行不能由类型化 catch 静态推断。
    for stmt in stmts {
        for request in crate::imports::collect_imports(std::slice::from_ref(stmt), "") {
            let bind = Name::from(request.bind.as_str());
            if let Some(previous) = imports.get(&bind) {
                // enum三项导出由runtime保护不可重绑；完全相同的无条件导入可合并，
                // 保首个available，不能让后面的同源导入抹掉前面已执行的身份。
                // 一般可变模块/不同来源及条件导入仍按原规则取消静态认证。
                let unconditional = matches!(stmt, ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_));
                let same_enum = unconditional
                    && native.contains(&"enum")
                    && request.module == "enum"
                    && request
                        .name
                        .as_deref()
                        .is_none_or(|member| matches!(member, "Enum" | "Flag" | "auto"))
                    && previous.module == request.module
                    && previous.member == request.name
                    && previous.bind_root == request.bind_root;
                if same_enum {
                    continue;
                }
                repeated.insert(bind.clone());
            }
            if matches!(stmt, ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_))
                && native.contains(&request.module.as_str())
            {
                imports.insert(
                    bind,
                    ExceptionImport::new(
                        request.module,
                        request.name,
                        request.bind_root,
                        stmt.range().end(),
                    ),
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
    struct Writes(
        FxHashSet<Name>,
        usize,
        FxHashMap<Name, Vec<Vec<String>>>,
        FxHashSet<Name>,
    );
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
                // 定义表达式在外层执行，不能把默认值里的walrus误当局部写。
                for decorator in &function.decorator_list {
                    self.visit_expr(&decorator.expression);
                }
                self.visit_parameters(&function.parameters);
                if let Some(annotation) = &function.returns {
                    self.visit_expr(annotation);
                }
                if let Some(parameters) = &function.type_params {
                    self.visit_type_params(parameters);
                }
                self.1 += 1;
                for statement in &function.body {
                    self.visit_stmt(statement);
                }
                self.1 -= 1;
                return;
            }
            if let ast::Stmt::ClassDef(class) = stmt {
                // 类体名字属于类命名空间；基类/装饰器仍在外层执行。
                self.0
                    .extend(crate::scope::collect_global_names(&class.body));
                for decorator in &class.decorator_list {
                    self.visit_expr(&decorator.expression);
                }
                if let Some(arguments) = &class.arguments {
                    for arg in &arguments.args {
                        self.visit_expr(arg);
                    }
                    for keyword in &arguments.keywords {
                        self.visit_expr(&keyword.value);
                    }
                }
                if let Some(parameters) = &class.type_params {
                    self.visit_type_params(parameters);
                }
                self.1 += 1;
                for statement in &class.body {
                    self.visit_stmt(statement);
                }
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
        fn visit_expr(&mut self, expr: &'a ast::Expr) {
            if let Some(comp) = crate::comprehension_ast::Comprehension::from_expr(expr) {
                if let Some(first) = comp.generators.first() {
                    self.visit_expr(&first.iter);
                }
                let outer = self.3.clone();
                self.3.extend(comprehension_bindings(comp));
                walk_comprehension_body(self, comp);
                self.3 = outer;
                return;
            }
            match expr {
                ast::Expr::Named(named) => {
                    // walrus绑定最近的非推导式作用域，而不是任何迭代局部变量。
                    if self.1 == 0 {
                        if let ast::Expr::Name(name) = named.target.as_ref() {
                            self.0.insert(Name::from(name.id.as_str()));
                        }
                    }
                    self.visit_expr(&named.value);
                    return;
                }
                ast::Expr::Attribute(attr)
                    if matches!(attr.ctx, ast::ExprContext::Store | ast::ExprContext::Del) =>
                {
                    // 即使写在函数中也可能修改共享模块；不证明跨作用域别名互斥。
                    if let Some((root, path)) = attribute_path(expr)
                        && !self.3.contains(&Name::from(root))
                    {
                        self.2
                            .entry(Name::from(root))
                            .or_default()
                            .push(path.into_iter().map(str::to_owned).collect());
                    }
                }
                ast::Expr::Name(name)
                    if self.1 == 0
                        && !self.3.contains(&Name::from(name.id.as_str()))
                        && matches!(name.ctx, ast::ExprContext::Store | ast::ExprContext::Del) =>
                {
                    self.0.insert(Name::from(name.id.as_str()));
                }
                ast::Expr::Lambda(lambda) => {
                    if let Some(parameters) = &lambda.parameters {
                        self.visit_parameters(parameters);
                    }
                    return;
                }
                _ => {}
            }
            walk_expr(self, expr);
        }
    }
    let mut writes = Writes(repeated, 0, FxHashMap::default(), FxHashSet::default());
    for stmt in stmts {
        writes.visit_stmt(stmt);
    }
    imports.retain(|name, binding| {
        !writes.0.contains(name)
            && !writes.2.get(name).is_some_and(|paths| {
                paths
                    .iter()
                    .any(|path| binding.affected_by_attribute_write(path))
            })
    });
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
