# ADR-0098：bytearray 切片赋值的有限原位实现

日期：2026-11-05。状态：24 项运行时专项、三 unique 公开差分及正常完整门禁通过；本地未发布。

## 背景与范围

ADR0096 的模板应用首轮在合法的 `replacement[:] = b'changed'` 失败。该轮只迁移了
测试写法，原始 213 行 CP 成功程序逐字保存在 pending 目录，没有修复生产缺口。
TODO 的 M2.3 旧行曾笼统写成 bytearray 支持切片赋值，实际 SetItem 只分派单元素。
本切修正该常用容器行为，不继续追逐 memoryview 正则模板缓存等长尾。

仅支持精确 bytes、bytearray、已有一维连续 B/c memoryview，以及精确 list/tuple/range
整数序列作为右值。通用 iterable、原生子类布局、其它 buffer 布局独立待做；域外源明确
NotImplementedError，不偷偷使用会接受 str 的共享 ToBytes。精确 str/已支持原始数值源仍
TypeError；None/自定义数字等域外对象的非法参数错误分类不冒称完整对齐。
slice 分量接受 None、整数/bool、大整数和普通类型级 `__index__`；弃用警告型返回、
property/自定义描述符仍明确拒绝。此范围不改变单元素赋值、删除或其它容器的 PySlice。

## CPython 合同

固定 CPython3.12.13 的独立小参考确认以下规则，不能用“只转换一次更快”抹掉副作用：

1. 每轮按 step、start、stop 转换，step=0 立即失败。全部回调结束后才按当时长度归一化。
2. 不同 bytearray 右值只有一轮。bytes/self/view/整数序列先执行一轮，再转换右值，
   最后执行第二轮。self 与 view 在两轮之间快照；第二轮可修改原目标或释放原 view。
3. 非空扩展右值必须和选中数等长；空右值删除选中项。错误长度先 ValueError，
   之后才检查 export。连续 start>stop 退化为归一化 start 处插入。
4. 同长写保所有别名且允许活动导出；只有真实长度变化被 BufferError 阻止。
   转换失败不提交赋值自身的部分写入，但用户回调已发生的写入不能回滚。
5. 大索引饱和，最小负步长夹为 -MAX；计数采用 `1+(distance-1)/step`，避免宽步长加法溢出。

精确 list 在转换时逐项活读并立即执行元素整数协议，非法元素后不继续消费。
通用迭代构造及数字协议拒绝矩阵留在未实现域，不由少量序列样例冒充完整支持。

## 算法与成本

新路径沿已有 SetItem ABI 进入独立 partial 文件，没有新增 RuntimeFn 或编译器分层依赖。
bytes 复用不可变数组，不同 bytearray 在所有回调结束后短借源 span；不为这两个源再复制。
self/view/整数序列需要 O(m) 右值快照。源 span 的使用期间没有 Python 回调，不缓存 span，
不宣称公开 List 支持任意 CLR 线程并发写入。

连续写先验证实际 resize 和最终容量，增长在修改内容前 EnsureCapacity；随后用支持重叠的
span CopyTo 一次搬移尾段并填入右值，缩短后更新 Count。不会先删除再分配，也不会重建整个
目标。扩展空赋值按物理顺序线性压紧，避免逐次 RemoveAt 的 O(n×k) 退化；等长扩展写只触及
选中位置。成功长度变化只记录一次 ResizeEpoch，失败和同长写不更新。

新增 slice 分支及错误准备有成本；同长普通写没有额外内部导出。CLR Array.MaxLength 之外
明确 NI，实际分配失败不捕获重试。上述是复杂度/分配设计，不是吞吐或跨平台性能验收。

## 验证计划

先独立审源，再验证普通/扩展正负切片、巨量索引、一轮/两轮协议、self/view 别名、
回调重入、错误身份/上下文、活动 view/regex 导出及 ResizeEpoch。最多三项 CP-first/JIT
应用，其中第三项直接执行原始未迁写法的 regex 模板 fixture，不修改历史失败证据。
最后执行正常 make check。项目矩阵、已通过 chardet 入口、NativeAOT 与性能组不重跑，
不新增项目通过率或发布声明。实际执行结果另附。

## 初次验证

只读审源未发现支持域内阻断；机械翻译归一化/扩展删除算法的 22,932 项 CP 组合检查通过，
但它不替代真实 C# 验收。首次实际运行 24 项 C# 专项全部通过，零失败/跳过，包括
view 第二轮释放、regex 迭代器导出、完整错误身份/上下文和 ResizeEpoch。
已有 bytes/不同 bytearray 右值、重用字面 slice 的同长写，在预热后每 1,000 次调用测得
零新增托管分配；仅是当前宿主专项的分配证据，不是吞吐倍率或所有路径零成本。

公开应用首轮 2/3：回调/完整六用户帧异常链及原始未改写 regex fixture 通过。
基础应用 CP0，但编译 E0100 拒绝未实现的 `slice(...)` 构造器，未进入 JIT。
这是新测试额外用了其它未纳入 API，不是切片赋值运行失败；首源码、原流与诊断保留。
后续仅把该循环中的两个构造器改为显式切片语法，原首源另存 pending；不扩大生产实现
或删除观察点。复验与完整门禁结果随后记录。

基础应用窄迁后单独 CP-first/JIT 1/1 通过，三个 unique 应用均已通过，不改写首轮2/3历史。
原 regex 源 SHA256 `56458872296c59d3646aac9358040fed5c358014ac341ee954d3a3eb9563e9d8`
与旧提交完全一致。本切新基础首源 SHA256
`59ec8e4a65e2b8caabe8b11910a09d6b13f524cf95a69c7c8cfdfc85bb132a19` 保留为 slice 构造器 pending。

首次正常 make check 随后 exit0：fmt、严格 clippy、workspace、1771项C#零失败跳过、
固定CP基线228、e2e202及原30 ignored全部通过。完整日志SHA256
`68cd91f1179ac8801fc24657455df939fd1ca1eefaaee12c94d760f1100858ca`。门禁中的三项新公开差分也通过。
原代码/测试/输出与产物哈希随恢复包归档；本地候选未推送，未重跑固定项目或AOT/吞吐组。
