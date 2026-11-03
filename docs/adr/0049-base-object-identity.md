# ADR-0049：真实 object 基础实例与身份

- 日期：2026-11-03
- 状态：本地候选，隔离源码审通过；真实专项及正常完整门禁通过，远端三平台仍待，不是完整构造协议。

## 背景与有限交付

Quitter 的缺close测试曾遇到 object() 未实现，不能永远用其它实例绕过这个普通sentinel用法。
本切复用现有 `TypeObject` → `PyType.Get("object")` 唯一身份；该一等值路径本来正确，
不是新造一个名为object的用户类。新增无字典裸实例，支持 `MISSING = object()`、直接/别名
构造、身份/哈希、repr形状、有限根元数据和属性拒写。对象地址只遵循现有D002表示限制。

根关系只在已验证的类型候选分支补齐，不前移tuple检查、不改变短路/空tuple行为，
TypedDict候选仍拒运行时检查。普通类逻辑根 `__bases__ == (object,)`，子类保真实BaseClass；
别名基类只精确认可canonical object，不开放其它内建布局，不虚构普通类完整MRO。

局部object值原类型推导为None，现仅该值改为Dynamic以允许真实alias调用。包含它的函数
可能保守降动态；结构测试锁无关数值函数仍Static Int，不宣称成本为零或新增性能收益。
没有新HIR名、MIR RuntimeFn或CIL ABI。

## 不扩展普通派生构造协议

现有普通PyClass调用尚不分派用户 `__new__`，本切不将其静默包装成完整支持。
`object.__new__(PyClass)` 与派生带额外参数的显式根初始化明确NotImplementedError；
普通类现有构造入口保持不变。完整new/init四槽组合、返回foreign对象、init返回值、
slots/metaclass/多继承/内建布局分配属于后续独立合同，不能因裸object成功勾选完成。

## 富比较既有债与保留期望

现有比较ABI返回bool，`!=` 使用相等结果取反，不能保存任意用户富比较返回对象或独立
`__ne__` 运算协议。原完整CP正例逐字保留在
`tests/fixtures/baseobject/sentinel_rich_comparison_pending.py`，对应真实差分明确ignored，
不改其期望、不声称通过。首切核心另测反向bool `__eq__` 的真实调用顺序与对象身份；
直接 `object.__eq__` / `__ne__` 仍须保NotImplemented单例和已有回调异常，不新增静默强转。

## 本轮验证及首失败

固定CPython3.12.13已实际校准sentinel、根关系、tuple顺序及派生构造边界探针。
本地ty结构1/1、首轮Release C#9/9通过。首真实driver为1通过、2失败、1已知ignored：
点语法 `object.__eq__` / `__new__` 经CallMethod时，旧分派未包含canonical object类型，
手工GetAttr专项通过不能替代此真实入口。窄补仅在PyType且身份等于canonical object时
进入原GetAttr/CallValue，保其它PyType旧边界；补实际PIC/CallMethod与回调异常专项。
修后Release C#11/11、真实driver3通过+1既有富比较ignored，原CP期望未削；正常完整门禁随后通过。

首完整门禁已到567 C#与228 CP通过，随后旧 `runtime_bases` 案例仍期待
Enum别名重绑object后“不支持基类”。这项已实现能力使旧拒绝期待失效；仅该案例按固定CP13
迁为普通类成功正断言，保别名重绑、成员为真实int非Enum、根身份与实例属性，不能以空stdout
替代检验。另两元类/快照案例原样保留，专项3/3通过；正常完整门禁重新运行exit0：567 C#、228固定CP基线、202 e2e+30 ignored；首失败不删。

既有三平台workflow只扩原两3分钟专项step，保Quitter原filter/driver，并追加baseobject；
不新增job、ILC发布或性能样本，远端尚待本批推送与实际结果。

本轮不增加固定项目通过分子，不做新性能矩阵，不宣称原生AOT或三平台验收已完成。
