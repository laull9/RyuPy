# ADR0115：typing.Tuple 独立原始键缓存

- 日期：2026-11-05
- 状态：有限域专项、独审、有界性能与正常完整门禁完成；原项目干净提交复测完成

## 来源与范围

ClassVar 未验收候选暴露的真实缓存相位回归仍按 F1a普通根、F1b Tuple、F1c Callable
顺序拆分；本切只实现 _TupleType 自己的独立 raw-key 128 LRU，不恢复 C1，也不把
原 Markdown 的 ClassVar 导入首错宣称解除。基线是已验收 b7b88b0 的固定实际运行时。

仅改 PyTyping.Subscribe 分流与新增 Tuple partial。复用 PyTypingRawCache 的原始
receiver/parameter、真实 hash/eq、真 LRU 和失败不插入逻辑；不再进入 normalized
Intern。F0/F1a checker、cache、alias 五个保护文件逐字不变。静态方法组只初始化一次。
Tuple 新池与普通/SpecialForm/Generic及剩余旧池隔离，不声称旧Callable/Literal/native
彼此已经按 CP 完整分池。多一池增加最多128个条目的持有上限，不承诺即时析构。

## 冷正文和身份

Tuple[T] 与 Tuple[(T,)]、None 与 NoneType 保原键区别；产物可相等但不同身份。
保存 receiver 不受公开 Tuple 导出改绑影响。真正暖命中绕冷 helper，但每次 hash 与
非同身份相等键的 eq 仍消费自己依赖；不能把缓存命中说成不读任何全局。

Tuple 没有 _check_generic 或固定 arity。长度至少2且最后为真实 Ellipsis 时，先按序
对全部前缀 type-check，再读取 sentinel；Tuple[int,str,...] 合法，不保留旧两项限制。
否则按普通Tuple逐项检查。空 Tuple[()] 不执行 _type_check，构造空 args 不读
_TypingEllipsis，但仍真正消费 _GenericAlias 与 _collect_parameters。非空构造再读
sentinel，不能把 guard 全部提前到入口掩盖参数首错。两支使用不同 CP 确切错误消息。

安全不可变参数树复用已有惰性稳定 metadata；不会给每个冷 alias 增加额外 holder。
普通类参数不任意 getattr，采集器当前不能正确遍历的嵌套 Generic 专用载体或 alias
内部 tuple 明确 NI。单独/中间 Ellipsis 虽 CP 合法，仍经未扩展的 checker 明确 NI，
完整源保 pending；不是“非法 Ellipsis”。用户 hash/eq/helper 回调、真正 unhashable
回退、动态采参、ForwardRef、tuple派生和ClassVar冷行为均不由本切冒称支持。

## 真值与独审

20份最终源固定 CP3.12.13 双跑40/40成立；8支持候选严格 CP/compile/JIT 全过，
18新 C# 与178旧 typing 回归通过。旧基线仅空参数主源严格通过，其余七主失败，
十二完整边界也保失败（tuple派生编译E0100），原源/断言/golden不为求绿而删改。
12份完整 CP 合法/有确切副作用边界保持 pending，NI的C#边界断言不是CP支持证明。

独审复用4原源加2独立源，CP双跑、每源唯一compile/JIT均严格通过。独立覆盖等价
而非同身份嵌套alias raw键占一格LRU、触碰与跨池重建，以及1–8长度共72个前缀首错
位置，确保所有前缀都在sentinel/collector前处理。未把静态catch结构或安全错误成功
误写成未知回调TypeError fallback已动态验证。源码/工具/实际JIT DLL身份全部核准。

## 性能和仍存风险

复用 F1a 18个旧公开整函数CIL/次数，加Tuple普通/尾Ellipsis各热与160键冷轮转四路。
同CIL/host双runtime，固定8192热身/固定次数；默认与禁tier各唯一ABBA，8进程880
样本44.36秒，前置共同语义、候选raw/LRU/池隔离/空cold及所有checksum通过，无重跑。
仅每侧两个独立进程，固定热身不保证tier稳定，不作显著性或全面无回归结论。

普通Tuple热默认/禁tier -56.67%/-43.04%，688→536B；尾Ellipsis热-20.32%/-5.50%，
1320→1192B。普通Tuple冷-21.87%/+26.27%，默认960B不变、禁tier960→928B；尾
Ellipsis冷-48.92%/-51.27%，2784→1600B。这是真实整函数，不是私有cache纯成本。

默认type(Union) +13.35%，禁tier普通构造+6.40%、TypeVar hash+15.98%、Union冷
+15.14%、旧List冷+13.00%等所有不利样本完整保留，不能用Tuple收益掩盖。F0冷
Optional增加848B及F1a的冷/旧路径风险仍开放。详细22路径、配置离散、分配及源身份
随恢复证据保存；后续优化必须复用原证据，不以完整功能门禁代替性能风险关闭。

## 验收与后续

只转正8支持源，12完整源保持pending；正常make check成功后才功能提交，再以干净
提交原docopt/Markdown各唯一复测。Callable真实外层参数变换与inner缓存是下一切，
完成后再接回未验收C1。六件仍要求标准tar.xz小于32MiB及独立实际恢复，保全部历史
失败、性能负值和C1 WIP，不能将之前超限归档称为成功备份。

正常 make check 唯一运行 exit0，约120秒、未触低空间界：2148C#/335CP/252e2e通过，
87ignored。旧244通过/75ignored全部319项逐名状态保留，仅新增8支持/12pending；
非e2e Rust775通过、另1旧ignored。335 CP-only只排除phase4四项，不计Ryu支持率。
10938源内容/size/mode两次与门禁冻结一致，生产与测试源/golden无中途改动。

干净2ee56fe原项目各唯一复测：docopt0.6.2 CP/编译/JIT均通过，四条860B原CP警告
保留，stdout16B同golden；Markdown3.5.2 CP/编译通过、JIT exit1仍为57B的ClassVar
ImportError。63输入、20锁包两份各3187源文件、原入口/golden及实际运行时经独审
核准，没有重试，不拼接旧矩阵计数或增加项目分子。后续先Callable，再恢复C1。
