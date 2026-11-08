# ADR0142：显式生成器 trace 产品 V1（未验收候选）

日期：2026-11-08。状态：源码候选；尚无 Rust 编译、C#、正常 driver JIT、NativeAOT 或同提交完整门结果。不得据本文改写项目矩阵或 accepted。

## 决定

默认产品保持旧域。只有全局 `--trace-product-v1` 才尝试单源码 Legacy 整产物准入；任何未知能力以 E0310 和“trace V1 整产物拒绝”结束编译，不产生局部旧回退。旧 S1/S2 测试适配不是生产开关。

高层 MIR 的 Suspend/Region 保留到全部已选 pass 后。高层与最后低层分别使用对应验证阶段；严格最终 validator 不放宽。生成器身份取 HIR is_generator→真实 FnId，不扫描残留 yield。复用既有 lowering 后，以实际输入/输出的类型化结构、相邻定义、槽位、PC、EH、source/行号核验；不再从帧槽反推整个高层状态，也不建立第二个跨 Retry 固定点。

link 前后以实际 FnId/GlobalId 双射核对完整已准入 IR，发射入口必须核对封存结果。标签、Debug 文本、旧 checker 结论均不授信。codegen 仍只依赖 MIR；只有 codegen 依赖 dotnetdll，只有 syntax 依赖 ruff。

## 有限能力

- 正向目标为原 A/all-miss、原 B/frames 和另外冻结的一处 getter 重复执行输入。原 B 文件/expected 不改。
- 单源码、固定位置参数、已知普通生成器的模块级 next、标量输出、无字段/无继承普通类的固定 property。property 走真实 PyPic、InvokeBound/thunk/getter；生成器走真实 PyGenerator。
- 显式异常只接受 ValueError/TypeError/RuntimeError；实际值、调用与效果都检查，不能只检查构造文本。源 bare、send/throw/close/yield-from、SystemExit/PEP479、字段/构造器、Instance、多模块、未知回调/外逃一律拒绝。
- 现有异常构造ABI不区分无参和显式None消息。产品专用HIR合同据现有收敛值事实拒绝单参可能None的构造（含参数/返回/callee别名），不改旧ABI；零参及直接raise已准入类型保留。next/getter的RecursionError守卫效果保留并使用已迁入的具体NativeFresh producer，不因此开放源码RecursionError类型。
- 原 C/D 保留原输入，必须在 V1 初始化和源码执行前拒绝。没有将其改写成更容易的负例。
- 固定 stdout/stderr 与空 atexit 在真实 Enter(1) 入口核验；准入拒绝重绑、导入和外宿主。具体 CLR 流错误每次映射为新的 Python 异常，故不宣称支持任意自定义 writer 的同对象多 flight 协议。

## 单一权威与真实 ABI

真实 Main 在 PySystem.Initialize 之后、用户函数/类初始化之前直接调用 PyTraceProduct.Enter(Int64 1)。旧产物不引用该类型。未知版本、S2 runtime 或不合格执行根拒绝，不回落 Attach。

异常 owner 只保存一份 H/Q/Park/receipt。Frames 只能从 H 派生。Explicit 与审核具体 producer 的 NativeFresh 成功后才发布新 binding；无历史不等于出生许可。Park 提交本帧及内段，token 不保存可回滚旧 H。Match/Abort 只消费旧 continuation；AllMiss 接当前最新 H 与原外段，不伪装成源码 raise。

Push 分配真实 activation；普通下一次 next 新建 activation，内部 Retry 不新建。Forward 首次空段产生 receipt；只有精确相同事件或明确的 AllMiss continuation 可透明重放。Handle/Forward/escape/root 在真实弹栈前接收。FinishExit 的 Reset 只清物理栈，域和 epoch 保持到报告/刷新结束；每个被吞掉的已映射流错误都结束 flight。

## 验收与旧实验

正常 driver 产生的 A/B/热输入 JIT 与 NativeAOT，每产物 PIC1/0 对固定 CP 原双流和 exit；C/D 普通与 AOT 编译拒绝。既有 PE 只读 decoder 核真实 Main ABI、实际恢复委托和唯一热 getter 站点，不以 dump 或测试 Host 替代产品。

新增直接 C# PyPic 合同覆盖 cold/warm 两条实际机制路径。真实热 fixture 覆盖同站点重复执行；产物没有 hit 计数，不能声称观察到了命中数量。没有增加生产计数/退出 JSON。

完整 default/pilot make check 和固定20逐 ID 仍必需，原十项通过项不能以总数替代。原 fullcheck 只补既有一轮测试的 TRX 与源码/工具/两个实际 runtime role 的前后只读身份，不增加测试进程或补造旧证据。

只隔离以下四个未验收、已知失败的旧实验，原断言保留且可 `make trace-frame-proof-experiment` 独立执行。主门明确报告 3+1 未执行，不能计通过，其余安全负例不豁免：

1. trace_frame_tests::original_frames_has_pc_partitioned_retry_frame_obligations
2. trace_frame_tests::renamed_generator_and_factory_alias_keep_frame_proof
3. trace_frame_tests::repeated_actual_match_block_converges_without_epoch_growth
4. trace_frame_dataflow::tests::actual_mir_diamond_reaches_fixed_point

这是有限产品候选，不宣称通用 Python trace 完成、性能收益或项目兼容率提高。若需要通用 heap、第二固定点或全 runtime 迁移，停止本切并重新决策。

## S1 阶段合同迁移（测试源码候选，未验收）

产品接线与历史“未接线”门分阶段处理，详见 [S1 的 V1 合同](../trace-state-s1.md#显式-trace-v1-的阶段合同迁移源码候选尚未运行)。
保留固定旧 1659 条 ABI，只接受该类及 11 个完整方法签名；保留原17内核限制，对三承载体、
具体桥接成员/目标/操作补精确合同，不给予整个 runtime 或类型通配权限。原失败报告不改。

新增有限生命周期/判定器/只读 PE 变异测试，不改变生产行为，不作为真实 driver 的替代品。
结构检查明确限制相对顺序和可见引用，不宣称完整控制流/别名证明；默认 A/B 的 PE-only 控制
也不被升级为实际运行结果。源码/静态检查完成不等于新 S1、完整门或产品已通过。

### 无默认参数 def 的内部默认槽合同

普通顶层 def 即使没有 Python 默认参数，也创建与 Python 参数元数据等长的全 CLR Null
数组，并发布到该函数的私有 defaults 槽；hidden owner 不计入数组，Python None 不等于 Null。
产品证明只准模块同块的真实 MakeFunction 局部，经未覆盖的 Use/合法 Box 与同实例
FunctionModule 冷写，到 FunctionDefaults getter，再以不透明能力发布到对应唯一私有槽。
未知操作、跨块/覆盖、公开导出、重复/错误发布或其它消费者均整产物拒绝。

函数参数/返回上界中的 Function 标签不构成 getter 权限。property 构造不被强制追加发布链；
实际默认参数、任意元数据、一般数组读取/修改仍不开放。运行时、codegen 与默认产品路径不变。
每次检查先建立一次实际赋值/全局写入/槽角色索引，局部追溯及发布跨度上限为128；中间别名
复核最坏为该固定上限的平方，不逐getter扫描整个程序，也不引入跨块或Retry固定点。
集中变异测试直接进入此合同并断言具体错误，另保留原A/B/HOT、C/D和完整产品验收门；
新增测试名单或源码检查不代表真实编译、JIT或NativeAOT已通过。
