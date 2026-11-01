//! 类与装饰器的HIR数据结构，独立于表达式/语句主体以保持职责与文件规模。

use crate::hir::{ClassId, ExprId, FuncId, ModuleVarId, Name, TypeRef};
use ryupy_base::TextRange;

/// 一个类定义。
///
/// 两类类共享同一结构：
/// - `@dataclass`：默认由运行时应用装饰器；`fields` 静态布局保留给可证明特化；
/// - 普通 class（M2.5）：运行时由 `PyClass` / `PyInstance` 承载，类体按源码顺序
///   求值属性并登记方法。
#[derive(Debug)]
pub struct Class {
    pub name: Name,
    /// 单继承的基类；`None` 表示 `object`。
    pub base: Option<ClassBase>,
    pub fields: Vec<Field>,
    /// 类自身简单标注名，按首次声明顺序去重；供动态dataclass转换读取。
    /// 不参与静态字段布局，也不把普通类标注强行解析为TypeRef。
    pub declared_fields: Vec<Name>,
    /// 类体按源码顺序执行：属性、方法、表达式语句。
    pub body: Vec<ClassItem>,
    /// `@dataclass(frozen=True)`：字段写入在编译期拒绝。
    pub frozen: bool,
    /// `@dataclass` 修饰的类。
    pub is_dataclass: bool,
    /// 完整运行时数据类语义；静态字段布局仅保留供将来可证明特化。
    pub runtime_dataclass: bool,
    /// 是否为 `BaseException` 家族（内建或用户定义）——决定能否出现在 `except` 中。
    pub is_exception: bool,
    pub range: TextRange,
    /// 类装饰器按源码顺序登记，dataclass位置保留，避免混合装饰链重复转换。
    pub decorators: Vec<Decorator>,
    /// 任意装饰器的最终公开绑定；原类对象保持内部ClassId，不提前覆盖此槽。
    pub decorated_binding: Option<ModuleVarId>,
}

/// 单继承的基类表示。用户类指向已解析的 `ClassId`，内建基类保留异常类型名。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum ClassBase {
    User(ClassId),
    Builtin(&'static str),
    /// 已知普通类经装饰后，从最终模块绑定读取运行时基类。
    Expr(ExprId),
}

#[derive(Debug)]
pub struct Field {
    pub name: Name,
    pub annotation: TypeRef,
    pub range: TextRange,
}

/// 类体里的一项，保留源码顺序（Python 的类体是顺序执行的）。
#[derive(Clone, Debug)]
pub struct ClassItem {
    pub kind: ClassItemKind,
    pub range: TextRange,
}

#[derive(Clone, Debug)]
pub enum ClassItemKind {
    /// 字段标注在可选初值之后求值，写入当前类的__annotations__。
    Annotation { name: Name, value: ExprId },
    /// `name = value` / `name: T = value`：写入类命名空间。
    Attr { name: Name, value: ExprId },
    /// 方法定义；`@property` / `@staticmethod` / `@classmethod` 在运行时应用。
    Method {
        func: FuncId,
        decorators: Vec<Decorator>,
    },
    /// 表达式语句（通常是 docstring 或类体里的副作用）。
    Expr(ExprId),
}

/// `def` 上方支持的类内修饰器。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Decorator {
    Property,
    StaticMethod,
    ClassMethod,
    /// 定义位置求值的一般装饰器表达式。
    Expr(ExprId),
    /// dataclass既有专用转换在装饰链中的位置。
    Dataclass,
}
