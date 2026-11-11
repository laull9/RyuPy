# ADR-0158：显式普通实例的直接 globals 冷路

状态：Rust限定检查及实际业务PE发射已过的WIP；真实 JIT/Native 与固定20保护未验收。

## 范围

`--instance-direct-globals` 仅与已有显式 `--instance-module` 的普通模块共同选择。
不改变自动选择、Legacy生成字段、包/entry或AOT类域。所有推导式继续原拒绝。
原dateutil包的`__dir__`包含推导式，不属于本切，不增加项目通过数。

HIR记录零参直接Name调用的源码位置；MIR只有真实实例context及能力时发冷helper。
先照LEGB读取callee，再用ReferenceEquals与稳定普通PyFunction标记比较。
命中返回当前调用者context的唯一PyDict；不命中按原零参callable调用。
无context调用标记仍NI，不借TLS、不扩全动态调用入口、不绑定builtin定义模块。

Default builtins新增globals键是可观察改变。身份另行保存，不从可变字典反取。
删除键后NameError，重绑后使用实际值。函数保定义时builtins引用，globals则始终
是实例同一字典；外部写删、clear与重执行不制造第二镜像，失败写入不回滚。

## 证明与边界

旧ABI不删改，仅追加冷helper；Instance函数沿既有静态owned delegate根。
层测试检查cap/context与名字遮蔽，实际产品仍须双实例、真实字典别名、旧新函数
builtins、失败发布、间接NI与原20保护。C#机制测试不能代替编译Python执行。

急切推导式StopIteration内部消费后继续回调的trace污染风险单列后续债；本切
不改迭代器、frame或trace权威。原F/T/W黄金保持，含推导式用例不记本切通过。

### MIR局部调用绑定（WIP003，限定Rust窗口已实测）

每个已准入HIR调用仅生成一份callee快照与result临时，Body窄表保存源码call/callee
局部ID、context/snapshot/result及实际求值Operand。validator逐一核真实相邻Assign/
helper Call、快照唯一写入和唯一使用、result唯一写、源码点不重复；无表新增helper
或改callee producer均拒绝。link保持LocalId并重映射表内Global operand。

DeadValues原样保留被helper使用的快照及有副作用Call；SimplifyCfg不重排这些非空
语句。未关闭默认优化，未改全部passes；默认pipeline保持性与mutation测试已在WIP003的Rust002窗口实跑。
该窗HIR 2、MIR 9、CIL 3通过，pilot CLI类型检查及相关Clippy通过；
两次业务PE发射只验证编译布局，旧Runtime仅复制未执行，不能外推新运行时或Native通过。
