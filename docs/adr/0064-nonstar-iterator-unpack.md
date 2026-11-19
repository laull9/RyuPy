# ADR-0064：非星号解包的真实迭代协议

状态：有限实现与正常完整门禁本地通过；未发布；后续固定项目入口结果单列，不代表通用星号或嵌套解包支持。

## 背景与范围

固定 pyfiglet 入口在 d231 候选进入 loadFont 后，实际抛出 map 没有 len 的 TypeError，
随后由库包装为 FontError。旧 MIR 平坦解包先调用 Len，再按索引取值，不能消费真实 map。
原错误没有内部解包位置，故源中的 map 解包仅是与已确认编译路径相符的静态线索，不声称
已经单独定位唯一表达式。修复应实现通用协议，而不是给 map 增加假的长度或按项目名特判。

本切仅改非星号平坦解包。直接且不含 Starred 元素的 tuple/list 字面量保留原长度检查和
下标形态，供既有 tuple_scalar 消除临时对象。其它 RHS 求值一次，通过新的
UnpackSequence(object, long) 冻结本层结果，然后才按原次序计算和写入各目标。
星号保持原受控序列路径；嵌套目标仍沿既有明确诊断，不宣称本切实现。

## 运行时协议与状态

exact PyTuple 校验数量后可复用；exact PyList 逐项读取并冻结，不访问会永久物化数值背板的
Items。其它输入取得一次迭代器，收集 N 项后只探测第 N+1 项；不足或多出才报数量错误。
不读 len/length_hint，不耗尽多余输入，不关闭或处置借入游标。探测时的非 StopIteration
错误原样传播，获取迭代器时的 StopIteration 也不是耗尽。

普通类真正缺少类级 __iter__、但有 __getitem__ 时，在本入口局部使用从零递增的整数索引。
这保留旧 Len/GetItem 路径已有的成功输入，同时不要求 __len__。__iter__ 存在但为 None、
抛错或返回非法迭代器绝不回退；仅索引回退的 IndexError/StopIteration 是结束，普通 next
的 IndexError 仍传播。不修改全局 PyIter、map 或其它消费者的协议。

冷回调边界保存调用者帧深度和 ambient handler。传播异常先准备已有现场再恢复状态；
成功、false 以及被解释为结束的异常出口都恢复状态，避免数量错误附上被吞 StopIteration。
用户迭代器调用真实 __next__，不让适配器的永久 finished 标记改变可再次使用的游标。

## 成本与边界

通用路径按目标数量分配结果存储和元组，不能从无限或不可信输入长度分配；精确元组可免复制。
新增序列回退需要小游标对象。字面量保留原优化形态不等于所有动态解包零成本，也不代表
已测性能改善。成功采集后才开始目标写入；采集失败没有本层目标写入，后续 setter 失败则
保留已经完成的先前目标写入，不做回滚或重放。

旧非可迭代输入错误文案、通用星号迭代、嵌套解包仍有独立边界。本切不引入通用异常处理
重构，不承诺全部第三方或 native 对象序列协议都已覆盖。

## 参考与验证记录

固定 CPython 3.12.13 的第一份 12 组参考仅执行一次，exit0、空 stderr，0.090789 秒；
脚本 SHA256 为 fe81d3c00520e7f3c26fd6988420b280c8ba6fb35aedfd8fb8047619276c12f7，
stdout SHA256 为 01792c02cd80d57437087ab66c1400eccdce1f1382f33d94d6c2d97bae87355e。
它锁迭代次数、N+1、游标续用、目标冻结/写序和 ambient context。

为避免旧序列输入回归，另一次五组补充参考独立保存，exit0、空 stderr，0.060207 秒；
脚本 SHA256 为 56734e27c37a2b9fceb7ff8f64e3b84071b12e1dff1682ffe8f1bb83f021df2a，
stdout SHA256 为 7e7a389c8238c3f8f30ac9ec208036abd986fa04834d051ec0336dbe3159278d。
两次均有 30 秒上限，不属于性能测量。

编译器五个接点首次 cargo check 通过，原 MIR tuple_scalar 八项通过。
当时新运行时、真实源差分、正常完整门禁仍待完成，不能以设计参考替代候选执行证据。

普通实例 `Disabled.__iter__ = None` 的 CPython 诊断为 `TypeError: 'Disabled' object is not iterable`。
现 PyIter 旧路径可能给出 NoneType 不可调用文案；本切仅锁精确 TypeError 类别与 getitem 零调用，
不修改全局 PyIter，此完整消息一致性仍 pending。真正 next 异常的完整用户帧与链比较不削弱。
运行时首次 12/12 专项通过，零失败或跳过；当时真实源差分及完整门禁仍待。

首次真实 driver 四项全部通过：一项 MIR 结构、三项固定 CP/JIT 原流差分，零 ignored。
覆盖 map、zip、enumerate、range、生成器、普通类与索引回退，赋值/循环/推导式/with 目标，
N+1 数量检查、游标续用、冻结后写入、暂停目标以及完整 next 异常链。Release 运行时构建
零警告/错误。原 tuple_scalar driver 一项通过，正常完整门禁随后执行；这不是新项目入口验收。

首正常完整门禁的 fmt/strict clippy、776 项 C# 与 228 项 CP 基线通过，随后停在 m3_0
旧 MIR 快照。该失败保留，不把未跑完的 workspace/e2e 计为成功。一次只运行快照函数的
发现清单共 25 个 target、70 个函数，67 通过、3 个 MIR 旧期待失败；不接受尚未出现的后级
CIL，不采用 blanket accept。实际候选按原/新 SHA 单独归档和独立审查后才迁移。

三份 MIR 及后续实际出现的两份 CIL 候选均逐项独立审查后按精确 SHA 迁移。
MIR 保长度临时改冻结元组以及暂停生成器对应槽的真实对象保存；星号段不变。
CIL 合计三处 Len/UnpackCheck 序列改 helper，206 个 branch/leave 与 16 个 EH 区间
映射后其余正文逐字相同，不允许控制流进入替换段内部。首失败与发现轮日志保留。
随后第二次正常 make check 已完成，结果如下。

## 最终本地验收

第二次正常 make check 真实 exit0：fmt、strict clippy、workspace、776 项 C#（零失败/跳过）、
固定 CPython 基线 228 项、e2e 202 通过＋30 原 ignored 均通过。日志 SHA256：
3de744528cda399c63f1eb9eee099d257316d9434cd66e55e65dfb8b5b57c986。
新运行时12项、driver四项及原tuple_scalar的8项MIR/1项driver均已通过；参考与候选执行
计数分别记录，原失败没有删除。该门禁阶段没有改 AOT/性能工作流或运行新项目入口，仍不是全项目
通过或性能收益结论。下一次固定项目验证必须对应最终候选身份另行记录。

## 后续固定 pyfiglet 入口

2026-11-03，未发布候选 751afefbdc75f782b86dc958cd95b93a24682a24（tree
1827dbbe98f5c42ab72c4d4246102b6b4072c40a）在 Linux JIT 完成一次原固定入口验证。
官方 pyfiglet 1.0.2 sdist SHA256 为
758788018ab8faaddc0984e1ea05ff330d3c64be663c513cc1f105f6a3066dab；572 源文件与归档
逐成员字节及 exactset 一致，原 entry/golden 不变，显式包含真实 pyfiglet.fonts 包。

一次匹配候选的 CLI 准备后，CP、编译、JIT 各一次均 exit0，无 timeout。固定窗口总180秒，
分阶段30/90/30秒，首失败停，无重试；实际1.046347秒，仅记录执行预算，不是性能测量。
CP/JIT stdout 同为11字节 `6  _     _\n`，stderr 均空，原字节匹配固定 golden。编译 stderr
只有产物路径，六条原流分别留证。结果记录 SHA256：
9231e24b37e30eb9b9dfaa5488fe3934a0d67e8aaec79065a64557cc9f46b74f。

独立核验已重核源/入口/CLI/runtime/解释器/三个编译产物及全部原流哈希。这仅表示这个
未发布候选的固定 Linux JIT 入口成功，不是 main、完整 pyfiglet API/CLI、全部字体、压缩字体、
AOT或跨平台通过。未重跑20项目，历史5/20不能直接更新为当前6/20。
