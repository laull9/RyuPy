# 列表分段引用背板：隔离候选

基线 `81758bb4`；对应架构计划 L。当前只提交手写原型、语义合同与筛选 host，
**未接入 PyList，未改变默认产品，未完成编译/运行/性能验收**。环境没有 dotnet/cargo。

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

阶段计时仍是筛选器而非相同机器码：slice候选含PySlice.Indices，连续臂GetRange不含
Python切片协议；两臂追加/复制方法和分支也不同。保留这些差异，不将阶段差值直接
等同于“分段复制单一机制”的因果收益。每次阶段结束在计时外校验结果引用与顺序。

## 独立 CI（不可合入默认分支）

`work/list-storage-ci-20261115` 专用 `list-storage-experiment.yml` 只监听该分支 push，
不修改默认 CI、不执行 Rust/全仓构建。隔离安装并核验实际 SDK 10.0.403；Release
编译定向 C# 合同，再单独编译/运行 host。必须有至少 26 个真实通过的合同与双臂三阶段各
10 个完整样本，防止零测试成功。工件以实际 checkout SHA 和运行尝试编号命名，包含
SDK 信息、TRX、构建与运行日志和原始 JSON，失败时同样保存已有证据。产物不进 Git。

该流程只能验收实验候选，不是产品默认背板、CPython 差分、原始 list_ops、NativeAOT
或全仓 make check。共享 runner 的同进程筛选数据只能用于候选取舍，不保证应用收益。
