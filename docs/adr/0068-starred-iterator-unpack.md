# ADR-0068：通用星号解包的迭代与长度提示

状态：隔离源审后已接线；首次16项C#、2项MIR ABI测试及CIL编译检查通过。
真实源差分修后4/4通过（3应用+1结构）；两份MIR快照已独立精审迁移，首次正常完整门禁通过；本地验收、未发布。

## 范围与顺序

旧一般星号路径依赖len、下标和切片，不能正确处理map、generator和用户iterator。
新增固定 `UnpackEx(source, before, after) -> object`，返回精确tuple，依目标顺序包含前项、
一个新list和后项。仅一般星号接此入口；直接无展开tuple/list字面量、非星号及tuple_scalar
原结构保留。平坦目标之外的嵌套解包、全局list/iter/map协议不扩。

按固定CPython3.12.13语义：I0=iter(source)，先读before项，再I1=iter(I0)，向I0求
len/length_hint，耗尽I1，最后分离后缀。I1可以不是I0；前缀不足不进入二次iter或hint。
显式iter(user)在Ryu中的隐藏适配器必须还原真实Instance，不能少调用用户iter。
缺iter的普通实例可局部getitem回退，其hint读取源len减已读index，不读取源hint。

len阶段TypeError可回退hint；hint特殊查找/绑定的TypeError传播，只有其调用TypeError可忽略。
返回NotImplemented用默认估计；返回类型错误不能吞。len允许原index转换，hint只接受真实整数，
先检查ssize_t范围再检查负值；因此巨大负hint为OverflowError而巨大负len为ValueError。
已支持property分别执行getter及所得callable；任意用户descriptor明确NI，不能静默漏__get__。

所有本层值和后缀引用先冻结，再左到右求目标和写入。准备失败零目标写；setter失败保留早期写入。
目标yield恢复复用已冻结引用，不重新iter/hint/消费；清空已发布middle也不能损失后缀。
借入iterator不主动close；每个冷回调边界保原异常身份/cause/context并恢复调用者帧与ambient，
仅消费阶段Stop及getitem回退IndexError表示结束，不吞获取/hint阶段的同名错误。

## 热路、容量与明确成本

exact tuple/list的运行时快路按原Count/GetItem取值，避免访问输入数值PyList.Items导致永久物化。
str子类保既有文本迭代成功域和Unicode旧边界。未知对象真实协议不可因注解或函数名猜成exact。
新增结果tuple/槽及middle list有分配成本，通用星号必须O(n)消费和存储，不是非星号N+1有界算法。

完整执行并验证hint，但容量预留最多4096；更大的合法hint按实际元素逐步增长，不限制实际元素数。
不捕获OOM后重试，不伪造数量错误。此优化已明确接受：有效巨大hint下资源耗尽的时点可能与CP不同，
不承诺逐分配/MemoryError等价。普通Python回调、类型/范围错误及元素不得因此省略。
没有性能测量，不宣零成本或提速；不运行原生/性能矩阵。

新ABI参数/返回槽/Eval/CallWithArgs均有显式验证；refs与旧Eval集合按职责小拆分，保旧语义，
所有新增/改动源文件遵守600行约束。生成器现有全局部引用保存足够，不改状态机算法。

## 唯一参考和阶段证据

单次固定CP3.12.13参考19组，28秒TERM+2秒KILL上限；实际exit0、stderr空、无timeout，
0.031966739秒完成。时间只作预算证据，不是性能对照。
脚本SHA256：fdfb1ff5232b3c9c688a668e38358fec8c8fb938463e6933123cd3fcef5b639a。
stdout SHA256：a9d4763da0a1acefd65abf498bf62d24b4a3abb33bc857dd7ecda41493576259。
解释器SHA256：021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7。
源/解释器/双流及完成标记均由独立审核重核，没有第二次参考。

首MIR专项2/2、CIL crate check通过，首Release运行时16/16、零失败/跳过。
这些只是阶段验证，该阶段真实源3应用+1结构、相关旧回归和完整门禁仍待，不据此冒项目通过。
未重跑pyfiglet，不改历史5/20或既有matmul禁分层回归记录。

## 真实源首轮与独立旧债

首轮4项driver为3/4：固定ABI/冻结结构、目标副作用及两种yield身份、完整10项用户异常链通过；
core的CP exit0，而Ryu在短源循环报 `TypeError: object of type 'function' has no len()`。
原流前6775字节相同，失败源245行 `for values ...` 与254行后来 `def values` 同名。
既有HIR预登记函数，普通顶层函数名解析优先于模块变量，导致246行实参读到函数；这些编译路径
不在本切变更内。独立核实后仅允许该循环变量改名，三种短源及后续全部公共入口仍保留；
原完整CP成功源另存pending。不修全模块绑定、不把这个旧债算作已解决；首失败原流留证。

两行变量改名后4项driver全部通过、零ignored；完整10项用户异常链与两种yield目标身份不变。
原source逐字保`module_function_collision.py.pending`，同目录stdout为首次CP完整成功原流，
不把该pending或新增旧绑定债计作候选通过。源码snapshot清单发现2个实际旧star MIR入口，
正常运行各产生预期失败和候选；仅在独立逐项SHA/正文审后精确迁移，不blanket accept。

两份候选已按已审new SHA精确迁移：slice c55bc62f81273e959fce1e878fb41eef6409a834081587f3998904fb3550537e；
targets bd7cbb223cdf4c84c98eab5d826bfdab03c3dbfb83254c2e0f25e91817e6fd8b。
真实后续切片、非星号、main及paused剩余正文保持；随后首次正常完整make check通过。

## 最终本地门禁

首次正常 `make check` exit0：fmt、严格clippy、workspace、Release零warning/error，
818项C#零失败/跳过，固定CP基线228项，e2e 202通过＋30原ignored。
日志SHA256：c7895e2d7462dfb9027b120c3e97b66f2be154fda46bbccfd5826c7a25e227b2。
前述driver首失败、两份旧快照失败均保留，不把pending转正；未公开发布或新增项目/AOT/性能执行。
