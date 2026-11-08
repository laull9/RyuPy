//! 资格事实的保守边界：拒绝隐藏定义副作用、动态布局、未知值和恢复环。
//! 拒绝只记录原因，不改变现有编译结果；最终 MIR 证书尚需独立签发。
use crate::trace_domain::{Atom, Values};
use crate::trace_domain_transfer::Solver;
use ryupy_hir::{self as h, FuncId};
use rustc_hash::FxHashSet;

impl Solver<'_> {
    pub(super) fn check_shapes(&mut self) {
        if self.module.info.instance_globals {
            self.refuse("首切仅证明单源码 Legacy 绑定，不能忽略实例内建覆盖");
        }
        for (_, variable) in self.module.vars.iter() {
            if variable.may_delete {
                self.refuse("模块绑定可能被函数写入或删除");
            }
        }
        for (id, function) in self.module.functions.iter() {
            if !function.closure.free.is_empty() || !function.closure.cells.is_empty()
                || function.closure.has_nested || function.closure.class_cell.is_some()
            {
                self.refuse("闭包、嵌套函数与 cell 逃逸未认证");
            }
            if !function.annotation_values.is_empty()
                || function.params.iter().any(|parameter| parameter.default.is_some()
                    || !matches!(parameter.kind, h::ParamKind::PositionalOnly | h::ParamKind::PositionalOrKeyword))
            {
                self.refuse("定义默认值、运行时注解或可变绑定签名未认证");
            }
            for (_, expression) in function.body.exprs.iter() {
                // 首切不推理模块初始化的跨调用时序。直接参数/返回别名仍能传播。
                if id != self.module.main && matches!(expression.kind, h::ExprKind::ModuleVar(_)) {
                    self.refuse("函数读取模块可变槽的初始化时序未认证");
                }
            }
            for (_, handler) in function.body.handlers.iter() {
                if handler.name.is_some() {
                    self.refuse("except-as 生命周期尚未绑定最终 IR");
                }
            }
            for (_, statement) in function.body.stmts.iter() {
                if let h::StmtKind::Try { body, handlers, orelse, finalbody } = &statement.kind {
                    if !orelse.is_empty() || !finalbody.is_empty() {
                        self.refuse("首切不接受 else/finally 的额外清理边");
                    }
                    if body.iter().chain(handlers.iter().flat_map(|id| &function.body.handlers[*id].body))
                        .any(|id| matches!(function.body.stmts[*id].kind, h::StmtKind::Try { .. }))
                    {
                        self.refuse("嵌套 dispatch 需要 token 栈，首切整产物保旧");
                    }
                }
            }
        }
        for (_, class) in self.module.classes.iter() {
            if class.base.is_some() || class.is_exception || class.is_dataclass
                || class.runtime_dataclass || class.enum_members.is_some()
                || class.has_class_cell || class.has_annotations
                || !class.decorators.is_empty() || class.decorated_binding.is_some()
            {
                self.refuse("类不是无基类扩展、无装饰副作用的闭合普通类");
            }
            let mut names = FxHashSet::default();
            for item in &class.body {
                match &item.kind {
                    h::ClassItemKind::Method { func, decorators } => {
                        let function = &self.module.functions[*func];
                        if !names.insert(function.name.clone()) {
                            self.refuse("类命名空间重定义使 descriptor 身份不稳定");
                        }
                        if function.name.as_str().starts_with("__") {
                            self.refuse("用户特殊方法可能改变默认对象协议");
                        }
                        if decorators.as_slice() != [h::Decorator::Property]
                            || function.is_generator || function.params.len() != 1
                        {
                            self.refuse("类成员尚未证明为固定 self 形状的可信 property");
                        }
                    }
                    _ => self.refuse("类体额外执行、属性写入或动态布局未认证"),
                }
            }
        }
        self.check_module_order();
    }

    /// 顶层初始化采用明确顺序证据。定义后的普通 alias 不按名称区分。
    /// 分析不把全模块绑定 union 误当成使用前已初始化的证明。
    fn check_module_order(&mut self) {
        let body = &self.module.functions[self.module.main].body;
        let mut globals = FxHashSet::default();
        let mut definitions = FxHashSet::default();
        let mut classes = FxHashSet::default();
        for statement in &body.root {
            match &body.stmts[*statement].kind {
                h::StmtKind::FunctionDefaults(function) => { definitions.insert(*function); }
                h::StmtKind::ClassDef(class) => {
                    classes.insert(*class);
                    for item in &self.module.classes[*class].body {
                        if let h::ClassItemKind::Method { func, .. } = item.kind {
                            definitions.insert(func);
                        }
                    }
                }
                h::StmtKind::Assign { target, value } => {
                    self.check_ready(*value, &globals, &definitions, &classes);
                    if let h::AssignTarget::ModuleVar(global) = target {
                        if !globals.insert(*global) {
                            self.refuse("模块槽重复写入尚无执行点证明");
                        }
                    }
                }
                h::StmtKind::Expr(value) => self.check_ready(*value, &globals, &definitions, &classes),
                _ => self.refuse("模块根包含尚未认证的初始化控制流"),
            }
        }
    }

    fn check_ready(&mut self, id: h::ExprId, globals: &FxHashSet<h::ModuleVarId>,
        definitions: &FxHashSet<FuncId>, classes: &FxHashSet<h::ClassId>)
    {
        let body = &self.module.functions[self.module.main].body;
        match &body.exprs[id].kind {
            h::ExprKind::ModuleVar(value) if !globals.contains(value) => self.refuse("模块槽使用先于初始化"),
            h::ExprKind::Func(value) if !definitions.contains(value) => self.refuse("函数对象使用先于定义执行"),
            h::ExprKind::Class(value) if !classes.contains(value) => self.refuse("类对象使用先于类体执行"),
            h::ExprKind::Call { callee, args } => {
                if self.module.top_functions.iter().any(|id| !definitions.contains(id))
                    || self.module.top_classes.iter().any(|id| !classes.contains(id))
                {
                    self.refuse("调用先于闭世界定义初始化完成");
                }
                self.check_ready(*callee, globals, definitions, classes);
                for arg in args { self.check_ready(arg.expr(), globals, definitions, classes); }
            }
            h::ExprKind::Attribute { base, .. } => self.check_ready(*base, globals, definitions, classes),
            _ => {}
        }
    }

    pub(super) fn check_final(&mut self) {
        let unknown = self.facts.expressions.values().chain(self.facts.locals.values())
            .chain(self.facts.globals.values()).any(Values::is_unknown)
            || self.facts.functions.values().any(|summary| summary.returns.is_unknown()
                || summary.yields.is_unknown() || summary.parameters.values().any(Values::is_unknown));
        if unknown { self.refuse("能力集合含 Unknown 或超过32项预算"); }
        for (function, definition) in self.module.functions.iter() {
            let summary = self.facts.functions.get(&function).cloned().unwrap_or_default();
            if definition.is_generator && summary.errors.iter().any(|error|
                matches!(*error, "StopIteration" | "GeneratorExit"))
            {
                self.refuse("生成器体存在 PEP479 或 GeneratorExit 边");
            }
            if definition.is_generator && summary.returns.atoms().iter().any(|value|
                !matches!(value, Atom::Primitive(_)))
            {
                self.refuse("generator return 值可能在 StopIteration 报告中触发用户转换");
            }
            for (_, expression) in definition.body.exprs.iter() {
                let input = match expression.kind {
                    h::ExprKind::Call { callee, .. } => Some(callee),
                    h::ExprKind::Attribute { base, .. } => Some(base),
                    _ => None,
                };
                if let Some(input) = input
                    && self.facts.expressions.get(&(function, input)).is_none_or(|value| value.atoms.is_empty())
                {
                    self.refuse("调用或属性输入为 Bottom，未用函数也不能借空集合过关");
                }
            }
            for (_, handler) in definition.body.handlers.iter() {
                if let Some(expression) = handler.type_expr {
                    let target = self.facts.expressions.get(&(function, expression)).cloned().unwrap_or_default();
                    if target.unknown || target.atoms.iter().any(|atom| !matches!(atom, Atom::ExceptionClass(_))) {
                        self.refuse("动态匹配目标不是内建异常类或已证明无正常返回的 getter");
                    }
                }
            }
            if self.reaches(function, function, &mut FxHashSet::default()) {
                self.refuse("恢复调用图存在环，无法排除 generator 重入");
            }
        }
    }

    fn reaches(&self, start: FuncId, target: FuncId, visited: &mut FxHashSet<FuncId>) -> bool {
        if !visited.insert(start) { return false; }
        self.facts.functions.get(&start).is_some_and(|summary| summary.calls.iter().any(|callee|
            *callee == target || self.reaches(*callee, target, visited)))
    }
}
