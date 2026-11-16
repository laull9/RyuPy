# ADR-0211：原生方法描述符显式候选与 super 胶水移除

- 日期：2026-11-16
- 状态：原生 super 已实现；方法描述符保留显式候选，平台及分发验收分列
- 接续：ADR-0208、ADR-0209

## 背景

外部类方法原先由 `_rp_wrap_method` 建立 Python 函数，再由 `_rp_method`
逐次转发至托管 `ForeignMethodTarget`。该函数提供普通 Python 描述符绑定，但每次
调用都会增加 CPython 字节码执行。`_rp_super_create` 和 `_rp_super_getattr`
也仅包装真实 `super` 类型和 `getattr`，无需 Python 求值来完成这些工作。

真实 `PyFunction` 的代码、闭包与 CPython 布局属于消费者可观察合同。将其替换成
原生可调用对象后，`types.FunctionType` 判断和精确 `CodeType` 消费者会遇到能力差异。
本切片以独立候选承接这种变化，既有兼容默认继续保留。

## 决定

1. `super` 直接缓存真实 CPython `super` 类型，按既有合同构造单参未绑定或双参绑定
   代理，属性读取调用静态对象 ABI。删除两个 Python 转发 helper。
   编译方法的词法类、首参数与零参 `super()` 上下文仍由 `ForeignMethodTarget` 和
   `PyMethodContext` 提供。真实 PySuper_Type、MRO、描述符和错误由选中 CPython 处理。
   精确内建 super 实例与类型对象通过静态 C 类型检查进入同一个中性 `PySuper` /
   `PyLexicalSuper.Type` 身份合同；外部返回、保存、`super.__get__` 重新绑定和再次导出
   都保留该合同。普通外部对象保持原有分类。外部 S(super) 及编译类继承 S 的实例
   保留自身真实类型；仅 canonical super 目标的 isinstance/issubclass 交由已有 C API
   检查祖先关系，其他未开放 NativeType 继续执行原有诊断。
2. 新增 `RYUPY_CPYTHON_NATIVE_METHODS=1` 显式方法候选；`0` 或未设置沿用旧包装器。
   其他开关值明确报错。候选内部模块不安装 `_rp_wrap_method` 或 `_rp_method` 定义。
3. 静态 C heap type `ryupy.NativeMethod` 以 `tp_call` 调用现有托管代理，
   `tp_descr_get` 使用 `PyMethod_New` 提供绑定。对象遍历 target、元数据、普通字典
   和 heap type；支持弱引用与自环回收。类型缓存持有一份引用，Finalize 前释放，
   活实例自行持有类型，托管根保持至 Finalize 完成。
4. 方法的 `__name__`、`__qualname__`、`__module__`、`__doc__`、`__annotations__`、
   `__defaults__`、`__kwdefaults__` 与 `__dict__` 独立保存，沿旧转发函数合同开放修改。
   已取得的 bound method 冷读当前名字与限定名。原编译函数负责真实参数绑定，
   修改转发对象元数据不替换底层函数。普通、classmethod、staticmethod、property
   都使用同一个原生可调用体，不为组合描述符添加 Python helper。
5. `__code__`、`__globals__`、`__closure__` 的读写删给出明确 `NotImplementedError`，
   不构造 CPython CodeType。精确 FunctionType/CodeType 判断、对应 inspect 路径、
   函数帧/闭包布局、函数专属序列化以及依赖 PyFunction 精确 C 布局的原生消费者
   均在本候选域之外。它们需要后续独立合同，当前不允许默认晋升。
6. 方法候选使用 CPython 3.9 起可用的对象、GC、描述符和弱引用 API；3.9 的 newref
   宏沿现有版本 guard 适配。产品既有 3.9–3.15 版本合同维持独立回归，审计私有
   3.12.13 帧 ABI 的适用范围保持在审计模式内。

## 验收

`tests/cpython/native_methods.py` 同次选择每个发行生成 oracle，构建真实 shim 和
手写 C 小宿主，对照普通/类/静态/属性绑定、异常保持、动态名字与限定名、元数据、
弱引用和自环 GC。macOS arm64 上 CPython 3.9.25、3.12.13、3.13.3 已通过。
所有中间输出位于仓库外并自动清理。

真实 C# 类桥的默认包装器与原生候选各通过十四项测试，覆盖 UserDict、deque、
多层/混合继承、描述符、custom new、显式及零参/classmethod super、元数据与生命周期。
候选审计中 `_rp_wrap_method`、`_rp_method`、`_rp_super_create`、`_rp_super_getattr`
均为零帧；审计整体仍保留 startup_complete=false / complete=false 的既有覆盖边界。

审查回归覆盖：实际接收类具有外部层级时，纯 Mixin 起点也立即
交给完整 CPython C3；外部基类在前/后、兄弟 Mixin 优先于共享祖先、`__self__` 遮蔽、
Mixin 关系及镜像元数据身份分别检查。已导出 Mixin 的字段/方法写删先同步外部镜像，
成功后发布本地值；普通类不切换归属。镜像结构改绑继续明确拒绝。
默认与候选各十四项、Runtime super/桥五十七项及 3.9/3.12/3.13 静态 super API 门已通过。
单继承后代继承实际外部层级后，显式/词法 super 对实例及类接收者均查询完整真实关系。
CPython 再派生后返回的 opaque 类型/实例通过有限桥协议查询已有 Mixin 镜像；未知普通
类不建立镜像，无关系返回 False，非法 issubclass 首参继续报 TypeError。

JIT、NativeAOT、删除输入搬迁与跨平台产品验证由独立产品门记录。普通多继承的既有
NativeAOT 保护门保持原状；完整混合继承用例保留 JIT 回归，单继承产品独立验收。

## 普通 super 内存保护

普通 `PySuper` 保留 `target` 与 `Self` 两个引用。`target` 通常直接保存 PyClass；
外部代理与读取委托集中在显式 `ForeignTarget` 冷状态，首次懒导出后缓存真实代理，
原本的 Owner/Self 与 owned 代理释放合同保持一致。普通构造维持单次对象分配，
热路径直接使用类型标记，冷状态集中管理外部能力。

保留 attrs 负面样本：总分配从 207697352 B 增至 212497472 B；其中 300000 个普通
super 各增加两个引用字段，贡献 4800000 B。修复后的 Release 测试独立检查恰好两个
引用字段，并把 4096 个对象写入预分配数组防止分配消除，实测预算为 131072 B，
即 64 位运行时每对象恢复 32 B。最终端到端时间与总分配由最终二十六项配对另行验收。

## 后续

先通过当前候选的平台与真实消费者门，再分别决定扩大元数据合同和默认晋升。
`_rp_calculate_meta` / `_rp_make_class` 的元类、prepare、MRO entries 与 namespace
行为作为下个独立竖切；本切片保留现有类构建路径。
