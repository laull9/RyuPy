# ADR-0164：动态单基选择延后发布合同，不猜跨模块类身份

- 状态：实施与验证中；完整回归及同提交 Native 后验待收集
- 日期：2026-11-12

## 问题与选择

ADR0163 的原 sortedcontainers 入口已经完整通过，但扩展使用者的
`class Descendant(SortedList)` 仍触发旧 MakeClass 的 MI 父类守卫。
它不是 C3 算法失败，而是跨模块导入名没有获得类体成功后再公开的编译合同。
直接删除守卫会把提前发布的旧入口暴露给多重继承，破坏类体失败、旧名及类 cell
语义；按 sortedcontainers 或导出名字特判则把值身份与准备能力混为一谈。

本切在已有 Legacy Runtime/Expr 单基域引入独立 `deferred_runtime_base` 标志。
沿现有模块槽登记与实际表达式求值，不追踪跨模块 ClassId，不增加来源认证或
全程序固定点。Name、Attribute、Subscript 和已有装饰后类路径保持现有语法域；
不因此开放任意调用式基类、动态元类、类关键字或 Instance 新域。
已知静态类、特殊类协议和 dataclass 路径不被改造成一般动态类。

## 分层与运行期权威

HIR 标志只选择发布合同。MIR 保留一次基表达式求值和既有 ValidateOrdinaryBase，
先冻结真实基值，再调用两参数 `PrepareRuntimeDerivedClass(name, bases)`。
只有类体、普通终结与类 cell 守卫成功后才更新内部类槽；公开名字仍在
装饰器完成后发布。装饰器看到旧绑定，装饰器失败也不能泄漏半成品。
CIL 仅绑定 RuntimeFn 的精确 ABI，不依赖 HIR 或解释 Python 类型。

运行时新入口要求恰好一个基类，通过原 MakeClassCore 解析一次：

- 实际基值是 MI PyClass 时，复用现有 C3 准备和布局守卫；
- 其它值继续原单基 str/list/dict、普通类、typing 等专用验证与布局；
- 原 MakeClass 不能藉此获得 MI 权限，原拒绝保持；
- 动态异常缺工厂、原生不可继承布局和未支持的元类仍明确拒绝。

这不是放松所有基类验证，也不是给所有用户类强行改布局。

## Native 与动态码能力

不能把所有新入口在 AOT 编译前拒绝，否则会收紧原来能运行的普通动态单基。
wrapper 只在实际 MI 分支要求 RuntimeFeature.IsDynamicCodeSupported；
Native AOT 或其它禁动态码配置中的实际 MI 明确 NotImplementedError，
普通分支仍沿旧单基路径。该能力约束不限于 Native，不提供绕过开关。

已知静态 MI 的原编译前 E0100 不变；未知动态基值没有被静态证明，因此不把
它的运行时拒绝描述成编译前拒绝。内部 mode helper 仅用于合同测试，不能
代替真实 Native 产物原地及搬移执行的证据。

## 验证与保留边界

多文件合同覆盖导入/转导出/别名/属性重绑、属性接收者与下标一次求值、
继承 super 链、成功/失败发布、装饰器旧绑定、以及旧单基布局。
显式 __class__、创建 hooks 与任意非类基值触发的动态元类分别保留完整
pending 反例，不挪走失败后宣称这些域已支持。

原未删减 sortedcontainers 扩展探针已在默认/PIC0 完整匹配 CPython3.12.13，
但这仍不是全项目、全 Native 或所有 Python 类语义验收。
新增冷入口及发布时序没有性能数据，不以路径简短宣称零成本。
