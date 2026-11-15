# 列表分段引用背板：隔离候选

基线 `81758bb4`；对应架构计划 L。当前只提交手写原型、语义合同与筛选 host，
**未接入 PyList，未改变默认产品，未完成产品性能验收**。本机没有 dotnet/cargo；
首次隔离CI与后续未验证修改状态见文末。

## 问题与边界

原始 `list_ops.py` 包含 30 万 append、索引读写、活迭代、尾部 pop、切片及
2000 次 1000 项 extend，还含小列表前插/成员查找。不能将整体耗时归因于 extend。
原 PyList 常规背板为连续 List<object?>；Exact extend 使用 AddRange，已有批量快路。
候选以 1024 引用一段，复制只涉及新数据，段目录仍有扩容成本。段大小未经调优。
不压缩或重新创建已有对象盒，不改数值/bool背板，也不改 Count/matmul 热路。

原型只编入测试/诊断 host。tail pop 释放空段并清除引用，非尾 pop 物化回连续背板。
公开可变 List 完整构建后才发布，之后永久保持同一 List，外部长度/元素变更即时可见。
精确 self-extend 固定入口长度，分段复制支持重叠；slice 为独立槽但保留元素引用。
活迭代每次读当前长度，耗尽不复活。一般枚举失败保留已提交前缀。
新增唯一引用正负步长跨段/越界/空切片，活迭代跨物化与删除、clear后复用合同。
Python iter/length_hint/异常链仍归原协议负责；原型不是这些协议的替代实现。

## 可重现验证

```
dotnet test runtime/RyuPy.Runtime.Tests --filter FullyQualifiedName~PySegmentedListStorageTests
dotnet run -c Release --project tests/perf/list-storage/host -- 10 > /tmp/list-storage-samples.json
```

host 两轮预热、平衡 AB/BA 顺序、同一进程原始 ticks/分配字节。分别记录 append（30万项）、slice（200次500项）、extend（2000次1000项）
的原始样本，不用混合总耗时做阶段归因。源盒、源容器、GC及逐项身份校验在计时外，同进程暖态、GC在计时外、不报告臂间RSS。
它不是 CP/JIT/AOT 对比，不代表原始 list_ops 加速。不要以此替代完整 NativeAOT 验收。

## 下一阶段准入

1. 先编译并跑原型合同，再测连续/分段独立阶段；保留任何负结果。
2. 候选通过后才做编译时实验接线：PyList 的 append/extend/get/set/尾 pop/切片/迭代；
   insert/remove/复杂切片/公开 Items/桥接走一次永久物化。必须查清所有 `_items!` 假设。
3. 不引入每个普通列表的额外默认分支/字段，除非 Count、matmul 与全26项实测接受。
4. CPython3.12.13 同次语义、JIT、真实 NativeAOT、删除源/构建搬迁、NumPy 写回各自验收。
5. 冻结旧新CLI跑原始 list_ops、全26项、独占配对与分配/RSS，最后 make check；通过再决定默认。

这不是完整存储改造交付，也没有性能收益结论。

旧 v2 阶段计时是筛选器而非相同机器码：当时 slice 候选含 PySlice.Indices，连续臂
GetRange 不含 Python 切片协议。此协议不对称仅适用于下文首次 CI 的旧 v2 数据，
不适用于当前 v3；v3 的等协议与预归一化两组指标见下文。两臂存储表示、追加/复制
方法和分支仍不同，不将阶段差值直接等同于“分段复制单一机制”的因果收益。
每次阶段结束在计时外校验结果引用与顺序。

## 独立 CI（不可合入默认分支）

`work/list-storage-ci-20261115` 专用 `list-storage-experiment.yml` 只监听该分支 push，
不修改默认 CI、不执行 Rust/全仓构建。隔离安装并核验实际 SDK 10.0.403；Release
编译定向 C# 合同，再单独编译/运行 host。必须有 32 个真实通过的合同与双臂四阶段各
10 个完整样本，防止零测试成功。检查 testId/名称唯一、十一个合同族及实例数、
ResultSummary 总计/执行/通过数一致且无失败跳过；十二组门禁自检覆盖伪造重复和缺失。工件以实际 checkout SHA 和运行尝试编号命名，包含
SDK 信息、TRX、构建与运行日志和原始 JSON，失败时同样保存已有证据。产物不进 Git。

该流程只能验收实验候选，不是产品默认背板、CPython 差分、原始 list_ops、NativeAOT
或全仓 make check。共享 runner 的同进程筛选数据只能用于候选取舍，不保证应用收益。
## 分阶段首次CI与切片候选修正

CI `43679058225` 完成旧候选26项测试；暖进程host各臂中位数（ms）：
append 2.8717→2.5260，slice 0.2629→1.5738，extend 8.1498→2.8111。
这组数据只支持“旧slice诊断候选明显更慢”，不支持默认产品加速或完整AOT性能结论。

源码检查识别三项混杂：旧候选逐元素执行索引检查、段定位与Append；连续臂GetRange
批量复制；仅候选执行PySlice.Indices。固定1024引用页对于500项切片也比连续500槽
分配更多。无法由上述差值定量分离各项成本，不能声称协议是全部原因。

新候选：step=1在归一化后进入SliceRange，按源/目标段界Array.Copy；已经物化的源
用List.CopyTo。非单位步保持逐元素正确路径，不将其列为已优化。引用不重新装箱，
源和切片槽独立；目标完成后才可见，空切片没有分配数据页。保留固定尾页开销，不做
尚无实证的紧凑尾页改造（它将扩大Append/extend不变量）。新增6项边界测试，共32项。

host schema升级为v3，四阶段每臂10轮，共80个样本：append、slice、slice_normalized、
extend。slice两臂均执行同一PySlice.Indices；slice_normalized两臂均使用预归一化
(start,count)。两组不能混为同一指标；后者排除协议但仍包含各自存储表示和分配差异。
原始list_ops入口不变。新代码尚未运行CI，收益待实测；旧26项通过不替代新增代码验证。
