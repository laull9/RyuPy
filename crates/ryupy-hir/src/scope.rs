//! 作用域与名称解析。
//!
//! Python 的作用域规则与「块作用域」语言不同，是本阶段最容易踩坑的地方：
//!
//! 1. **函数级作用域**：`if`/`for`/`while` 不引入新作用域。
//! 2. **赋值即局部**：只要函数体内任何位置对 `x` 赋值（含 `for x in`、增量赋值），
//!    `x` 在**整个函数**内都是局部变量——即使读取发生在赋值之前（运行时 `UnboundLocalError`）。
//!    因此必须先**预扫描**整个函数体收集被赋值的名字，再进行解析。
//! 3. 未被赋值的名字按 LEGB 查找：Local → Enclosing（Phase 3 闭包）→ Global → Builtin。
//! 4. `global` / `nonlocal` 声明会改变第 2 条（Phase 1 暂不支持，报错即可）。

use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Ranged, TextRange};
use ryupy_syntax::ast;

use crate::hir::{Builtin, ClassId, FuncId, LocalId, Name};

/// 名字解析的结果。
#[allow(dead_code)]
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Resolution {
    Local(LocalId),
    Func(FuncId),
    Class(ClassId),
    Builtin(Builtin),
}

/// 模块级名字表：第一遍收集所有顶层 `def` / `class`，因此函数之间可以前向引用。
#[derive(Debug, Default)]
pub struct GlobalScope {
    /// 真实名字从当前模块实例命名空间读取，内建也不可静态直达。
    pub instance_globals: bool,
    /// 同模块MI族定义只获得延后公开入口；实际基值仍必须逐值验证。
    pub(crate) ordinary_mi: FxHashSet<TextRange>,
    /// TypedDict仅登记候选语法，真实根/父身份仍在执行定义时验证。
    pub(crate) typed_dict_roots: FxHashSet<Name>,
    pub(crate) typed_dict_modules: FxHashSet<Name>,
    pub(crate) typed_dict_classes: FxHashSet<Name>,
    /// future annotations：注解仅保存规范字符串，不参与名称求值与布局假设。
    pub postponed_annotations: bool,
    /// driver认证的原生模块集合；只传身份，不把函数别名提升为模块导出。
    pub(crate) native_exception_modules: FxHashSet<String>,
    /// driver 已确认的原生模块异常导入别名。
    pub(crate) exception_imports: FxHashMap<Name, crate::exception_imports::ExceptionImport>,
    pub names: FxHashMap<Name, Resolution>,
    /// 装饰后的公开绑定读取模块变量，names只保留内部定义身份/静态基类信息。
    pub dynamic_names: FxHashSet<Name>,
    /// 当前模块及函数global删除集合，在任何定义体降级之前建立。
    pub deleted_names: FxHashSet<Name>,
    /// 仅因删除才公开的类名不顺便放开旧重复类/混合定义禁域。
    pub delete_only_definitions: FxHashSet<Name>,
    /// 定义位置已验证的 dataclasses 导入装饰器。
    pub dataclass_markers: FxHashSet<TextRange>,
}

/// 单个函数的局部名字表。
#[derive(Debug, Default)]
pub struct FunctionScope {
    /// 当前函数直属稳定导入；每函数独立，不传播free/cell身份或保护模块导出。
    pub(crate) exception_imports:
        FxHashMap<Name, crate::function_exception_imports::LocalExceptionImport>,
    pub locals: FxHashMap<Name, LocalId>,
    /// 该函数里 `global` 声明的名字：它们不是局部变量，赋值/读取都落到模块命名空间。
    pub globals: FxHashSet<Name>,
    /// 该函数里 `nonlocal` 声明的名字（M3.0）：指向外层函数的 cell。
    pub nonlocals: FxHashSet<Name>,
    /// 外层 cell 名字 → `ClosureInfo::free` 下标（M3.0）。读取与写入都按此下标访问 env。
    pub free: FxHashMap<Name, usize>,
    /// 本函数内被捕获的局部名（cell）（M3.0）。嵌套 def 创建函数对象时，
    /// 靠它判断 free 名字来自当前局部 cell 还是外层 env。
    pub cells: FxHashSet<Name>,
}

impl FunctionScope {
    /// 名字在该函数里是否指向模块级变量。
    pub fn is_global(&self, name: &Name) -> bool {
        self.globals.contains(name)
    }

    /// 名字是否指向外层函数的 cell（M3.0）。
    pub fn free_index(&self, name: &Name) -> Option<usize> {
        self.free.get(name).copied()
    }
}

/// 收集函数体内的 `global` 声明（不进入嵌套 def / class）。
pub fn collect_global_names(body: &[ast::Stmt]) -> Vec<Name> {
    let mut out = Vec::new();
    fn walk(stmts: &[ast::Stmt], out: &mut Vec<Name>) {
        for stmt in stmts {
            match stmt {
                ast::Stmt::Global(g) => {
                    out.extend(g.names.iter().map(|n| Name::from(n.as_str())));
                }
                ast::Stmt::If(s) => {
                    walk(&s.body, out);
                    for clause in &s.elif_else_clauses {
                        walk(&clause.body, out);
                    }
                }
                ast::Stmt::For(s) => {
                    walk(&s.body, out);
                    walk(&s.orelse, out);
                }
                ast::Stmt::While(s) => {
                    walk(&s.body, out);
                    walk(&s.orelse, out);
                }
                ast::Stmt::Try(s) => {
                    walk(&s.body, out);
                    for handler in &s.handlers {
                        let ast::ExceptHandler::ExceptHandler(h) = handler;
                        walk(&h.body, out);
                    }
                    walk(&s.orelse, out);
                    walk(&s.finalbody, out);
                }
                ast::Stmt::With(s) => walk(&s.body, out),
                _ => {}
            }
        }
    }
    walk(body, &mut out);
    out
}

/// 预扫描函数体，返回所有被赋值的名字及其首次出现位置（规则 2）。
pub fn collect_assigned_names(body: &[ast::Stmt]) -> Vec<(Name, TextRange)> {
    let mut names = Vec::new();
    let mut seen = FxHashSet::default();

    fn collect_expr(
        expr: &ast::Expr,
        names: &mut Vec<(Name, TextRange)>,
        seen: &mut FxHashSet<Name>,
    ) {
        match expr {
            ast::Expr::Name(name) => {
                let n = Name::from(name.id.as_str());
                if seen.insert(n.clone()) {
                    names.push((n, name.range()));
                }
            }
            ast::Expr::Tuple(tuple) => {
                for elt in &tuple.elts {
                    collect_expr(elt, names, seen);
                }
            }
            ast::Expr::List(list) => {
                for elt in &list.elts {
                    collect_expr(elt, names, seen);
                }
            }
            ast::Expr::Starred(starred) => {
                collect_expr(&starred.value, names, seen);
            }
            _ => {}
        }
    }

    fn walk_stmts(
        stmts: &[ast::Stmt],
        names: &mut Vec<(Name, TextRange)>,
        seen: &mut FxHashSet<Name>,
    ) {
        for stmt in stmts {
            match stmt {
                ast::Stmt::Assign(s) => {
                    for target in &s.targets {
                        collect_expr(target, names, seen);
                    }
                }
                ast::Stmt::Delete(s) => {
                    // 仅裸名字构成局部绑定；属性与下标的base仍是普通读取。
                    for target in &s.targets {
                        if let ast::Expr::Name(_) = target {
                            collect_expr(target, names, seen);
                        }
                    }
                }
                ast::Stmt::AnnAssign(s) => {
                    collect_expr(&s.target, names, seen);
                }
                ast::Stmt::AugAssign(s) => {
                    collect_expr(&s.target, names, seen);
                }
                ast::Stmt::For(s) => {
                    collect_expr(&s.target, names, seen);
                    walk_stmts(&s.body, names, seen);
                    walk_stmts(&s.orelse, names, seen);
                }
                ast::Stmt::While(s) => {
                    walk_stmts(&s.body, names, seen);
                    walk_stmts(&s.orelse, names, seen);
                }
                ast::Stmt::If(s) => {
                    walk_stmts(&s.body, names, seen);
                    for clause in &s.elif_else_clauses {
                        walk_stmts(&clause.body, names, seen);
                    }
                }
                ast::Stmt::Match(s) => {
                    for case in &s.cases {
                        walk_stmts(&case.body, names, seen);
                    }
                }
                ast::Stmt::With(s) => {
                    for item in &s.items {
                        if let Some(target) = &item.optional_vars {
                            collect_expr(target, names, seen);
                        }
                    }
                    walk_stmts(&s.body, names, seen);
                }
                ast::Stmt::Try(s) => {
                    walk_stmts(&s.body, names, seen);
                    for handler in &s.handlers {
                        let ast::ExceptHandler::ExceptHandler(except) = handler;
                        // `except E as x` 里的 x 是函数级局部变量
                        if let Some(name) = except.name.as_ref() {
                            let n = Name::from(name.id.as_str());
                            if seen.insert(n.clone()) {
                                names.push((n, name.range));
                            }
                        }
                        walk_stmts(&except.body, names, seen);
                    }
                    walk_stmts(&s.orelse, names, seen);
                    walk_stmts(&s.finalbody, names, seen);
                }
                // 嵌套 def / class 不递归进去（它们有独立作用域）
                _ => {}
            }
        }
    }

    walk_stmts(body, &mut names, &mut seen);
    names
}

/// 函数词法绑定包含 import 别名；独立于模块赋值扫描，避免稳定导入被误判为重绑定。
pub(crate) fn collect_function_bindings(stmts: &[ast::Stmt]) -> Vec<(Name, TextRange)> {
    use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};
    struct Imports(Vec<(Name, TextRange)>);
    impl<'a> Visitor<'a> for Imports {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            let aliases = match stmt {
                ast::Stmt::FunctionDef(_) | ast::Stmt::ClassDef(_) => return,
                ast::Stmt::Import(import) => Some((&import.names, true)),
                ast::Stmt::ImportFrom(import) => Some((&import.names, false)),
                _ => None,
            };
            if let Some((aliases, root)) = aliases {
                for alias in aliases {
                    if alias.name.as_str() == "*" {
                        continue;
                    }
                    let bound = alias.asname.as_ref().map_or_else(
                        || {
                            if root {
                                alias.name.as_str().split('.').next().unwrap_or_default()
                            } else {
                                alias.name.as_str()
                            }
                        },
                        |name| name.as_str(),
                    );
                    let name = Name::from(bound);
                    if !self.0.iter().any(|(existing, _)| existing == &name) {
                        self.0.push((name, alias.range));
                    }
                }
            } else {
                walk_stmt(self, stmt);
            }
        }
    }
    let mut imports = Imports(collect_assigned_names(stmts));
    for stmt in stmts {
        imports.visit_stmt(stmt);
    }
    imports.0
}

pub fn resolve_builtin(name: &str) -> Option<Builtin> {
    // Python 3 的历史名称是 OSError 同一对象；统一规范名让构造、catch 和基类共用身份。
    if matches!(name, "IOError" | "EnvironmentError") {
        return Some(Builtin::Exception("OSError"));
    }
    // 内建异常类：名字表与运行时 `PyExceptionTypes` 保持一致（两边都登记才算可用）。
    const EXCEPTIONS: &[&str] = &[
        "BaseException",
        "Exception",
        "Warning",
        "UserWarning",
        "DeprecationWarning",
        "PendingDeprecationWarning",
        "RuntimeWarning",
        "SyntaxWarning",
        "FutureWarning",
        "ImportWarning",
        "UnicodeWarning",
        "BytesWarning",
        "ResourceWarning",
        "SystemExit",
        "KeyboardInterrupt",
        "GeneratorExit",
        "ArithmeticError",
        "BufferError",
        "MemoryError",
        "ZeroDivisionError",
        "OverflowError",
        "LookupError",
        "IndexError",
        "KeyError",
        "SyntaxError",
        "ValueError",
        "UnicodeError",
        "UnicodeDecodeError",
        "UnicodeEncodeError",
        "TypeError",
        "NameError",
        "UnboundLocalError",
        "AttributeError",
        "RuntimeError",
        "NotImplementedError",
        "RecursionError",
        "AssertionError",
        "ImportError",
        "ModuleNotFoundError",
        "OSError",
        "FileNotFoundError",
        "FileExistsError",
        "PermissionError",
        "TimeoutError",
        "IsADirectoryError",
        "NotADirectoryError",
        "StopIteration",
        "EOFError",
    ];

    if let Some(exception) = EXCEPTIONS.iter().find(|e| **e == name) {
        return Some(Builtin::Exception(exception));
    }

    Some(match name {
        "print" => Builtin::Print,
        "open" => Builtin::Dynamic("open"),
        "input" => Builtin::Dynamic("input"),
        "object" => Builtin::Dynamic("object"),
        "pow" => Builtin::Dynamic("pow"),
        "int" => Builtin::Int,
        "float" => Builtin::Float,
        "abs" => Builtin::Abs,
        "str" => Builtin::Str,
        "type" => Builtin::Type,
        "len" => Builtin::Dynamic("len"),
        "ord" => Builtin::Dynamic("ord"),
        "chr" => Builtin::Dynamic("chr"),
        "list" => Builtin::Dynamic("list"),
        "tuple" => Builtin::Dynamic("tuple"),
        "dict" => Builtin::Dynamic("dict"),
        "set" => Builtin::Dynamic("set"),
        "frozenset" => Builtin::Dynamic("frozenset"),
        "bytes" => Builtin::Dynamic("bytes"),
        "bytearray" => Builtin::Dynamic("bytearray"),
        "slice" => Builtin::Dynamic("slice"),
        "memoryview" => Builtin::Dynamic("memoryview"),
        "sorted" => Builtin::Dynamic("sorted"),
        "hash" => Builtin::Dynamic("hash"),
        "repr" => Builtin::Dynamic("repr"),
        "format" => Builtin::Dynamic("format"),
        "dir" => Builtin::Dynamic("dir"),
        "Ellipsis" => Builtin::Dynamic("Ellipsis"),
        "NotImplemented" => Builtin::Dynamic("NotImplemented"),
        "bool" => Builtin::Dynamic("bool"),
        "iter" => Builtin::Dynamic("iter"),
        "next" => Builtin::Dynamic("next"),
        "range" => Builtin::Dynamic("range"),
        "enumerate" => Builtin::Dynamic("enumerate"),
        "zip" => Builtin::Dynamic("zip"),
        "map" => Builtin::Dynamic("map"),
        "exit" => Builtin::Dynamic("exit"),
        "quit" => Builtin::Dynamic("quit"),
        "reversed" => Builtin::Dynamic("reversed"),
        "sum" => Builtin::Dynamic("sum"),
        "min" => Builtin::Dynamic("min"),
        "max" => Builtin::Dynamic("max"),
        "any" => Builtin::Dynamic("any"),
        "all" => Builtin::Dynamic("all"),
        "round" => Builtin::Dynamic("round"),
        "isinstance" => Builtin::Dynamic("isinstance"),
        "issubclass" => Builtin::Dynamic("issubclass"),
        "property" => Builtin::Dynamic("property"),
        "staticmethod" => Builtin::Dynamic("staticmethod"),
        "classmethod" => Builtin::Dynamic("classmethod"),
        "super" => Builtin::Dynamic("super"),
        "id" => Builtin::Dynamic("id"),
        "getattr" => Builtin::Dynamic("getattr"),
        "setattr" => Builtin::Dynamic("setattr"),
        "delattr" => Builtin::Dynamic("delattr"),
        "locals" => Builtin::Dynamic("locals"),
        "hasattr" => Builtin::Dynamic("hasattr"),
        "callable" => Builtin::Dynamic("callable"),
        _ => return None,
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use ryupy_base::SourceDb;

    #[test]
    fn collects_assigned_variables_across_blocks() {
        let mut db = SourceDb::default();
        let src = r#"
y = 1
if True:
    z = 2
for i in range(10):
    w = 3
def nested():
    inner = 4
"#;
        let file = db.add("t.py", src);
        let (parsed, _) = ryupy_syntax::parse(file, src);
        let names = collect_assigned_names(&parsed.module.body);
        let name_strs: Vec<&str> = names.iter().map(|(n, _)| n.as_str()).collect();
        assert_eq!(name_strs, vec!["y", "z", "i", "w"]);
    }
}
