//! `class` 语句的 MIR 构建（M2.5 / M2.6）。
//!
//! Python 的 `class` 是**运行时语句**：先求值基类，再按顺序执行类体（属性赋值、
//! 方法定义、表达式副作用），最后得到类对象。这里不把类体提到任何“编译期常量”里，
//! 而是在模块入口的 `class` 位置生成：
//!
//! ```text
//! class_global = MakeClass(name, bases, exception_factory)
//! SetAttr(class_global, "attr", <表达式>)      // 按源码顺序
//! defaults_of(method)                          // def 执行时求值默认值
//! SetAttr(class_global, "method", Decorate(.., <PyFunction>))
//! ```
//!
//! 异常类多一个工厂委托参数：codegen 为每个用户异常类生成一个 CLR 子类，
//! 运行时通过委托创建它，从而保持 CLR 与 Python 继承树同构（`except ValueError`
//! 才能直接映射成类型化 catch）。

use ryupy_hir::{ClassBase, ClassItemKind, Decorator};

use crate::build::FunctionBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl FunctionBuilder<'_> {
    /// 构建一条 `class` 语句。
    pub(crate) fn build_class_def(&mut self, cls_id: ryupy_hir::ClassId) {
        let Some(&class_global) = self.hir_class_globals.get(&cls_id) else {
            return;
        };
        let cls = &self.hir.classes[cls_id];
        let name = cls.name.as_str().to_owned();
        let base = cls.base;
        let is_exception = cls.is_exception;
        let items = cls.body.clone();
        let mir_cls = self.class_map[&cls_id];

        // 1. 基类表达式先于类体求值（与 CPython 的 `__build_class__` 顺序一致）。
        let bases = match base {
            Some(ClassBase::User(base_id)) => match self.hir_class_globals.get(&base_id) {
                Some(global) => vec![Operand::Global(*global)],
                None => Vec::new(),
            },
            Some(ClassBase::Builtin(type_name)) => {
                let type_object = self.expr_builder().runtime_call(
                    RuntimeFn::TypeObject,
                    vec![Operand::Const(Constant::Str(type_name.to_owned()))],
                    MirType::Dynamic,
                );
                vec![type_object]
            }
            None => Vec::new(),
        };

        // 2. 创建类对象。异常类的 factory 由 `__init_class_factories` 提前写入静态槽位。
        let factory = if is_exception {
            match self.class_factories.get(&mir_cls).copied() {
                Some(global) => Operand::Global(global),
                None => Operand::Const(Constant::Null),
            }
        } else {
            Operand::Const(Constant::Null)
        };
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::MakeClass),
                args: vec![
                    Operand::Const(Constant::Str(name)),
                    Operand::Array(bases),
                    factory,
                ],
            },
        });
        self.push_stmt(Statement::SetGlobal {
            dest: class_global,
            value: Operand::Local(dest),
        });
        let class_object = Operand::Local(dest);

        // 3. 按源码顺序执行类体。
        for item in items {
            match item.kind {
                ClassItemKind::Attr { name, value } => {
                    let raw = self.expr_builder().build_expr(value);
                    let ty = self.expr_builder().ty_of(value);
                    let value = self.expr_builder().coerce(raw, &ty, &MirType::Dynamic);
                    self.emit_set_attr(class_object.clone(), name.as_str(), value);
                }
                ClassItemKind::Expr(value) => {
                    let _ = self.expr_builder().build_expr(value);
                }
                ClassItemKind::Method { func, decorators } => {
                    // 默认值在 `def` 执行时求值（类体位置），之后才创建函数对象。
                    self.emit_function_defaults(func);
                    let Some(&function_global) = self.fn_globals.get(&func) else {
                        continue;
                    };
                    let mut value = Operand::Global(function_global);
                    // 修饰器从下往上应用：`@property @staticmethod def f` 先 staticmethod。
                    for decorator in decorators.iter().rev() {
                        let kind = match decorator {
                            Decorator::Property => 1i64,
                            Decorator::StaticMethod => 2,
                            Decorator::ClassMethod => 3,
                        };
                        value = self.expr_builder().runtime_call(
                            RuntimeFn::Decorate,
                            vec![Operand::Const(Constant::I64(kind)), value],
                            MirType::Dynamic,
                        );
                    }
                    let method_name = self.hir.functions[func].name.as_str().to_owned();
                    self.emit_set_attr(class_object.clone(), &method_name, value);
                }
            }
        }
    }

    /// 写类属性。属性值是动态值（普通类对象 / 方法 / 修饰器结果）。
    fn emit_set_attr(&mut self, obj: Operand, name: &str, value: Operand) {
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::SetAttr),
            args: vec![obj, Operand::Const(Constant::Str(name.to_owned())), value],
        }));
    }
}
