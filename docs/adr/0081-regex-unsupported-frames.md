# ADR-0081：正则未支持语法的冷错误现场

状态：本地专项和正常完整门禁通过，独立终审通过，独立提交并制作恢复备份；未发布。

## 原因与最小范围

候选 `25fd4fe` 的锁定 tabulate 入口返回无帧的正则未支持语法错误，见 ADR-0080。
共同错误消息同时覆盖多种语法，不能据静态条件组线索认定本次唯一模式或执行位置。

`PyRegexParser.Unsupported` 的 11 个调用点共用一个新建异常工厂。
本切仅在该工厂新建 `PyNotImplementedError` 后调用 `PyErrors.PrepareRaise`，原样返回同一错误。
调用条件、消息、类型、顺序均不变，不在成功 Compile 路径增加 guard/catch，不围用户 flags、替换回调正文重包。
已有 bytes 构造的局部 Prepare 保留。另纠正 CreateBytes 的阶段 A 旧注释，不改其逻辑。

Attach 只保留当时已有的帧及行；已有非空快照不会被后续 Prepare 覆盖，已有非空 context 同样保留。
首次无帧或无 context 后再准备仍可能补入现场，不将有限幂等夸大为永不变化。
冷失败增加现场快照成本，成功路径不新增该调用；没有性能测量结论。

## 验收与明确边界

22 项 C# 专项首次全部通过，零失败、零跳过。覆盖工厂各拒绝路径、译文容量检查、text/bytes、原消息、caller/context、重复 Prepare 和成功路径状态；用户 flags 与替换回调的原异常标记不被此切改变。

一个真实 Ryu 源码用例首次通过：模块 ValueError context 后调用 outer→leaf，独立语句触发合法但尚未支持的 scoped flags。
严格比较完整 stderr、退出码及空 stdout，保模块/outer/leaf 调用行 14/16/10/6，没有删帧或宽松投影。
此用例不是 CPython 同错误差分：CPython 支持该语法，本切只改善 Ryu 的明确拒绝现场。

当前 class-body 项内联到外层方法，缺少逐项 TraceLine 和独立类体帧；本切不补造这些信息。
任意嵌套表达式、多行表达式和回调的精确 CP 行归属不因此闭合。
Parser.ErrorAt、组深度 RecursionError、模式长度、flags 及其它错误工厂不在本切，不宣称全部 re 异常都有完整帧。

首次正常 make check 已通过：fmt、严格 clippy、workspace、Release 零 warning/error、1081 项 C# 零失败/跳过、228 项固定 CPython 基线、202 项 e2e 与原30 ignored。
日志 SHA256 `93581fc7fe8ba5b15a3a617ac379a07f527994020efc053c6e0d1d9cc53c3f00`，status 0；没有快照迁移。
Ryu 诊断应用在专项与门禁各一次，共两次；不是两个独立用例，也未另跑 CP 参考。
上述门禁阶段未重跑 tabulate 固定入口、AOT 或性能组，项目分子不变。

## 后续原入口定位（2026-11-04）

候选 `97208aeadefb42c27fd5f14798f2053bfa94e068`（tree `eb52d5a090f05234cf7497f303f0c78d21d62a77`）完成验收和恢复备份后，执行一次原锁定 tabulate 入口的编译/JIT诊断。
CLI 单次准备 exit 0，约0.095秒；官方包32源文件及入口未改。CP复用 `25fd4fe` 已核原流及结果 `c45ab2a868379483d05e8725a956b5591fdfa50966c240abeaba0c69a1dfdeaf`，本次没有新 CP 执行。
编译/JIT 各一次，退出码0/1，总2.244017秒、无timeout，保持90/30秒阶段与总180秒上限、首失败停止。

新原 stderr 给出真实调用链：`tabulate_sample.py:2` → `__init__.py:767`，随后保持同一 Unsupported 消息。
对应原源码767–769的 `_float_with_thousands_separators = re.compile(...)` 定义，模式含 `(?(1)...)` 条件组。
这是本次实际定义调用定位，不是根据旧无帧消息猜测；仍未执行成功完整表格入口，不能外推后续消费者或全库兼容。

结果 SHA256 为 `a4a6cabd9a58d7bf0fca64768c038e612d07886d09db7f153486d5f5841d6a00`。
独立核验包含原包/32源exactset、入口/golden、复用CP引用、四份新原流、工具及三个产物哈希。
没有追加运行、AOT、性能或远端发布，不修改历史项目分子。
