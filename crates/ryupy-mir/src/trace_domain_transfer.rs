//! 单源码模块的有限能力传播：函数实参、返回、yield、属性 getter 与错误边。
//! 所有函数（包括未调用者）均审查；默认值、装饰器和类体不作为死定义跳过。
use crate::trace_domain::{AllocationSite, Atom, Facts, Primitive, Values};
use ryupy_hir::{self as h, ExprId, FuncId};

pub(super) fn analyze(module: &h::Module) -> Facts {
    let mut state = Solver {
        module,
        facts: Facts::default(),
    };
    state.check_shapes();
    // 每轮至少增加一个有限事实。独立迭代预算只允许保守拒绝，绝不返回半收敛资格。
    for _ in 0..1024 {
        let before = state.facts.clone();
        for (function, body) in module.functions.iter() {
            state.facts.functions.entry(function).or_default();
            for (expression, _) in body.body.exprs.iter() {
                let value = state.expression(function, expression);
                state
                    .facts
                    .expressions
                    .entry((function, expression))
                    .or_default()
                    .join(&value);
            }
            state.statements(function);
        }
        if state.facts == before {
            state.facts.converged = true;
            state.check_final();
            return state.facts;
        }
    }
    state.refuse("能力不动点迭代预算耗尽");
    state.facts
}

pub(super) struct Solver<'a> {
    pub(super) module: &'a h::Module,
    pub(super) facts: Facts,
}

impl Solver<'_> {
    pub(super) fn refuse(&mut self, reason: &str) {
        self.facts.refusals.insert(reason.to_owned());
    }

    fn expression(&mut self, function: FuncId, expression: ExprId) -> Values {
        let body = &self.module.functions[function].body;
        match body.exprs[expression].kind.clone() {
            h::ExprKind::Literal(value) => Values::one(Atom::Primitive(match value {
                h::Literal::None => Primitive::None,
                h::Literal::Bool(_) => Primitive::Bool,
                h::Literal::Int(_) | h::Literal::BigInt(_) => Primitive::Integer,
                h::Literal::Float(_) => Primitive::Float,
                h::Literal::Str(_) => Primitive::String,
            })),
            h::ExprKind::Func(id) => Values::one(Atom::Function(id)),
            h::ExprKind::Class(id) => Values::one(Atom::Class(id)),
            h::ExprKind::Local(id) => {
                let mut value = self
                    .facts
                    .locals
                    .get(&(function, id))
                    .cloned()
                    .unwrap_or_default();
                if let Some(parameter) = self
                    .facts
                    .functions
                    .get(&function)
                    .and_then(|summary| summary.parameters.get(&id))
                {
                    value.join(parameter);
                }
                value
            }
            h::ExprKind::ModuleVar(id) => self.facts.globals.get(&id).cloned().unwrap_or_default(),
            h::ExprKind::Builtin(h::Builtin::Print) => Values::one(Atom::Print),
            h::ExprKind::Builtin(h::Builtin::Dynamic("next")) => Values::one(Atom::Next),
            h::ExprKind::Builtin(h::Builtin::Exception(name)) => {
                Values::one(Atom::ExceptionClass(name))
            }
            h::ExprKind::Call { callee, args } => {
                if args
                    .iter()
                    .any(|arg| !matches!(arg, h::CallArg::Positional(_)))
                {
                    self.refuse("首切不接受关键字或展开实参");
                    return Values::unknown();
                }
                let target = self.expression(function, callee);
                let arguments = args
                    .iter()
                    .map(|arg| self.expression(function, arg.expr()))
                    .collect::<Vec<_>>();
                self.call(function, expression, target, &arguments)
            }
            h::ExprKind::Attribute { base, name } => {
                let receivers = self.expression(function, base);
                let mut result = Values::default();
                if receivers.unknown {
                    return Values::unknown();
                }
                for atom in receivers.atoms {
                    let Atom::Instance(class, site) = atom else {
                        self.refuse("属性接收者不是已封闭普通实例");
                        return Values::unknown();
                    };
                    let getter = self.module.classes[class].body.iter().find_map(|item| {
                        if let h::ClassItemKind::Method { func, decorators } = &item.kind
                            && self.module.functions[*func].name == name
                            && decorators.as_slice() == [h::Decorator::Property]
                        {
                            Some(*func)
                        } else {
                            None
                        }
                    });
                    let Some(getter) = getter else {
                        self.refuse("属性没有唯一可信 property getter");
                        return Values::unknown();
                    };
                    result.join(&self.invoke(
                        function,
                        getter,
                        &[Values::one(Atom::Instance(class, site))],
                        None,
                    ));
                }
                result
            }
            h::ExprKind::Yield(value) => {
                let yielded = value.map_or_else(
                    || Values::one(Atom::Primitive(Primitive::None)),
                    |id| self.expression(function, id),
                );
                self.facts
                    .functions
                    .entry(function)
                    .or_default()
                    .yields
                    .join(&yielded);
                Values::one(Atom::Primitive(Primitive::None))
            }
            // 编译器前言元数据不是用户可调用值。原生根入口仍须独立审核。
            h::ExprKind::ModuleMetadata(_, _) => Values::one(Atom::ModuleMetadata),
            h::ExprKind::ModuleFile(_) => Values::one(Atom::Primitive(Primitive::String)),
            _ => {
                self.refuse("表达式包含尚未认证的能力或隐式协议");
                Values::unknown()
            }
        }
    }

    fn call(
        &mut self,
        caller: FuncId,
        expression: ExprId,
        target: Values,
        args: &[Values],
    ) -> Values {
        if target.unknown || args.iter().any(|value| value.unknown) {
            return Values::unknown();
        }
        let site = AllocationSite {
            function: caller,
            expression,
        };
        let mut result = Values::default();
        for atom in target.atoms {
            let value = match atom {
                Atom::Function(function) => self.invoke(caller, function, args, Some(site)),
                Atom::Class(class) if args.is_empty() => Values::one(Atom::Instance(class, site)),
                Atom::Next if args.len() == 1 => {
                    let mut yielded = Values::default();
                    for receiver in &args[0].atoms {
                        let Atom::Generator(function, _) = receiver else {
                            self.refuse("next 接收者集合不全是已认证 generator");
                            return Values::unknown();
                        };
                        let summary = self
                            .facts
                            .functions
                            .get(function)
                            .cloned()
                            .unwrap_or_default();
                        yielded.join(&summary.yields);
                        self.propagate(caller, *function);
                    }
                    self.facts
                        .functions
                        .entry(caller)
                        .or_default()
                        .errors
                        .insert("StopIteration");
                    yielded
                }
                Atom::Print if args.iter().all(primitive_only) => {
                    self.facts
                        .functions
                        .entry(caller)
                        .or_default()
                        .errors
                        .insert("OSError");
                    Values::one(Atom::Primitive(Primitive::None))
                }
                Atom::ExceptionClass(name)
                    if args.len() <= 1
                        && args.iter().all(primitive_only)
                        && matches!(name, "ValueError" | "TypeError" | "RuntimeError") =>
                {
                    Values::one(Atom::ExceptionInstance(name))
                }
                _ => {
                    self.refuse("调用目标、实参形状或隐式回调尚未认证");
                    Values::unknown()
                }
            };
            result.join(&value);
        }
        result
    }

    fn invoke(
        &mut self,
        caller: FuncId,
        target: FuncId,
        args: &[Values],
        site: Option<AllocationSite>,
    ) -> Values {
        let function = &self.module.functions[target];
        if function.params.len() != args.len() {
            self.refuse("函数实际绑定形状不满足固定位置参数");
            return Values::unknown();
        }
        let summary = self.facts.functions.entry(target).or_default();
        for (parameter, value) in function.params.iter().zip(args) {
            summary
                .parameters
                .entry(parameter.local)
                .or_default()
                .join(value);
        }
        if function.is_generator {
            return site.map_or_else(Values::unknown, |site| {
                Values::one(Atom::Generator(target, site))
            });
        }
        self.propagate(caller, target);
        self.facts
            .functions
            .get(&target)
            .map(|summary| summary.returns.clone())
            .unwrap_or_default()
    }

    fn propagate(&mut self, caller: FuncId, target: FuncId) {
        let errors = self
            .facts
            .functions
            .get(&target)
            .map(|summary| summary.errors.clone())
            .unwrap_or_default();
        let caller = self.facts.functions.entry(caller).or_default();
        caller.errors.extend(errors);
        caller.errors.insert("RecursionError");
        if !caller.calls.contains(&target) {
            caller.calls.push(target);
        }
    }

    fn statements(&mut self, function: FuncId) {
        let body = &self.module.functions[function].body;
        for (_, statement) in body.stmts.iter() {
            match &statement.kind {
                h::StmtKind::Assign { target, value } => {
                    let value = self.expression(function, *value);
                    match target {
                        h::AssignTarget::Local(local) => {
                            self.facts
                                .locals
                                .entry((function, *local))
                                .or_default()
                                .join(&value);
                        }
                        h::AssignTarget::ModuleVar(global) if function == self.module.main => {
                            self.facts.globals.entry(*global).or_default().join(&value);
                        }
                        _ => self.refuse("字段、cell 或函数内模块写入未认证"),
                    }
                }
                h::StmtKind::Return(value) => {
                    let value = value.map_or_else(
                        || Values::one(Atom::Primitive(Primitive::None)),
                        |id| self.expression(function, id),
                    );
                    self.facts
                        .functions
                        .entry(function)
                        .or_default()
                        .returns
                        .join(&value);
                }
                h::StmtKind::Yield(value) => {
                    let value = value.map_or_else(
                        || Values::one(Atom::Primitive(Primitive::None)),
                        |id| self.expression(function, id),
                    );
                    self.facts
                        .functions
                        .entry(function)
                        .or_default()
                        .yields
                        .join(&value);
                }
                h::StmtKind::Raise {
                    value: Some(value),
                    cause: None,
                } => {
                    let value = self.expression(function, *value);
                    for atom in value.atoms {
                        if let Atom::ExceptionInstance(name) | Atom::ExceptionClass(name) = atom {
                            self.facts
                                .functions
                                .entry(function)
                                .or_default()
                                .errors
                                .insert(name);
                        } else {
                            self.refuse("raise 值不是已认证内建异常");
                        }
                    }
                }
                h::StmtKind::Expr(_)
                | h::StmtKind::Pass
                | h::StmtKind::FunctionDefaults(_)
                | h::StmtKind::ClassDef(_)
                | h::StmtKind::Try { .. } => {}
                _ => self.refuse("语句具有未认证控制流、注入、导入或副作用"),
            }
        }
        // 只在语法上确定最后语句终止时保留 Bottom，避免把 getter 的 raise 当 None。
        let terminates = body.root.last().is_some_and(|id| {
            matches!(
                body.stmts[*id].kind,
                h::StmtKind::Raise { .. } | h::StmtKind::Return(_)
            )
        });
        if !terminates {
            self.facts
                .functions
                .entry(function)
                .or_default()
                .returns
                .join(&Values::one(Atom::Primitive(Primitive::None)));
        }
    }
}

fn primitive_only(value: &Values) -> bool {
    !value.unknown
        && value
            .atoms
            .iter()
            .all(|atom| matches!(atom, Atom::Primitive(_)))
}
