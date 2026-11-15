# records：保留已物化 key 的融合边界

状态：基于 `98eb800b` 的独立实验切片，默认 MIR 与默认调用路径不变。
新增运行时合同和源码→MIR结构测试尚未编译；Python手写输入仅作CP源程序自检。
此文不宣称产品接线、性能收益或完整兼容通过。

## 为什么不继续直接消 tuple

原始 `tests/benchmarks/records.py` 的 `key` 是具名局部。第一次插入后，
`key is next(iter(counts))` 应为真；命中旧键后，新构造的 `key` 与表内旧键通常不同。
循环退出、locals、异常帧或回调均可能观察这个区别。仅在循环末尾补建一个tuple，
不能恢复“最后一轮是插入还是命中”的身份关系。

本切片保留源码的真实tuple构造，只融合已经证明封闭的get、加一、写回。
因此**不再假设消除临时tuple的分配收益**。其价值仅是减少重复查找及协议开销，
是否值得接线还须以真实产品算术控制臂和NativeAOT数据判断。

## 真实构建器中的操作边界

已阅读 `call_builder.rs::build_method_call` 与 `build_assign.rs::build_subscript_assign`：

1. 方法调用先求接收者并保存到匿名局部，再产生 `IsModule` 与 `CaptureModuleTarget`；
   实参随后求值，最终是带captured标志的 `DynCallMethod`。
2. 下标赋值先完整生成RHS，再生成目标对象及key并调用 `SetItem`。不能从同名counts/key
   推断其在get时和写回时一定是同一对象；未知回调、cell/global读取须单独拒绝或证明。
3. `get`拼写、`DynBinary(Add)`和`MakeTuple`仅说明语法形状，不证明canonical dict方法、
   精确字符串、精确long结果、局部稳定性或反射观察不存在。

新增 `records_increment_contract_tests` 从真实records与两个手写观察程序经过
parse/HIR/type/build/validate，要求上述具名tuple/get/add/set形状仍在。测试不是matcher，
不修改函数、不伪造准入证书；实际执行结果仍待Rust CI。

## 已物化键的运行时合同

`StringPairAccess.TryIncrementMaterializedKey(key)` 仅接受精确二字符串PyTuple、
原默认比较器与已经核验的全表键域。它仍使用原索引、原键/值槽及结构版本：

- 命中精确long且不为MaxValue：只更新该槽的结果，原实体键、顺序及结构版本不变。
- 缺键：通过原 `Set` 插入**调用方的原tuple**，不是复制的新tuple。
- 非精确tuple/字符串、异构键域、自定义comparer、非long或MaxValue：返回false，
  不执行用户hash/eq/add，不写入。调用方在原位置用**同一原key**执行原get/add/set一次。
- 不缓存跨用户代码的槽位，不执行get后才发现回退。所有准入检查先于可观察操作。
- 方法自身不证明canonical get、接收者/实参稳定性、异常行号与边界；不能直接作为
  任意字典 `d[k] += 1` 或任意get默认值的替代实现。

缺键与命中的整数结果均通过 `PyInt.Box`，与产品 `PyOps.Binary` / `PyInt.AddI64`
的小整数缓存保持一致。原discarded实验入口保留历史host直接装箱，不悄悄修改已测版本。
该旧入口不是可直接接入产品的完整语义入口。

## 新发现的计量边界

此前host A/B/F三臂使用CLR直接装箱long加一。它们在同一实验内部可比较，但真实产品
`PyInt.AddI64`返回 `PyInt.Box`，在[-5,256]复用缓存。特别是4096键、每键只有少量访问时，
产品整数结果分配与原host假设不同。因此旧日志的2,880,000字节结果盒成本不能套到产品，
也不能把旧融合比率用作本新入口的收益估计。后续需使用真实PyOps/PyInt控制臂，且保持
相同初始key身份与引用、算术范围、查找次数以及异常/回退语义。

## 合同测试与接线准入

新增11项C#合同包括：miss/hit/删除重插原key身份；缓存上下边界；MaxValue拒绝后
BigInteger提升；bool拒绝后原算术；用户add的可观察副作用仅在fallback执行一次；
抛出异常不写值或重放；派生tuple/用户成员/自定义comparer拒绝；600步普通vs候选的
逐步键/值实体与顺序/版本差分，含真实哈希碰撞。普通参考臂调用真实 `PyOps.Binary`。
补充原用户key的hash/eq仅在fallback的get/set各执行一次、add回调删除重插后的新键实体、
活动迭代器覆盖/结构变化，以及超过256槽后外部修改、同Count删插与清空恢复的版本预算。

手写pending Python输入覆盖上述用户可见行为以及dict派生get/set协议，不保存golden。
它不代表产品候选已启用；未来接线必须用固定CPython同次输出做实际JIT/AOT差分。

下一步只能在验证通过后构造显式、一次性、受控的MIR区域证书：证明原接收者/原key、
canonical方法、常量默认零和加一、参数求值顺序及无内部观察；将guard安排在尚未执行
原操作的点，失败保留原块与行号。保留key物化的版本先验收，再独立考虑死key消除。
原records、普通负例、26项保护、异常行号和删除输入搬迁仍须完整验证。
