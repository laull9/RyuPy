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

阶段计时仍是筛选器而非相同机器码：slice候选含PySlice.Indices，连续臂GetRange不含
Python切片协议；两臂追加/复制方法和分支也不同。保留这些差异，不将阶段差值直接
等同于“分段复制单一机制”的因果收益。每次阶段结束在计时外校验结果引用与顺序。

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

## v3原始样本复核与独立小切片候选

复核run `43679972374`，精确head `17a6c836be10f018c80e89b00bb31d0360223577`，
artifact `14147569950` ZIP SHA256
`777280d304583148611760bbc427e4ac270c3a8f3a3a40bd59e42fe33f3d7e85`。
下载字节与GitHub digest一致。32合同通过。以下只比较本轮v3，不拼接v2数据。

500项、200次切片的分配每轮固定：连续811200字节、分段1668800字节，约2.06倍。
归一化slice分段10轮ms为1.2622/0.2198/1.0480/0.6263/1.1215/0.1914/1.1720/
0.6605/1.0831/0.6561；同协议slice也有明显奇偶差异，偶轮1.06–1.24ms、奇轮
0.20–0.70ms。append分段偶轮3.24–3.68ms、奇轮2.02–2.20ms同样分离。
因此中位数不稳定，不能把时序效应归因于段容量；固定尾页浪费是已知分配事实，
但是否为时间退化主因尚未证实。温态小样本、GC与顺序效应均须保留。

新增独立SliceRangeCompact候选：长度<=1024直接构造精确容量连续List，跨源段用
Span.CopyTo；长切片走原分段实现。旧SliceRange不改，作为同轮控制。短slice自创建
即永久连续兼容，后续append/extend/外部Items/清空按原兼容表示，元素盒保持身份。
这是一种混合表示候选，不宣称只有容量变化（同时减少了段目录/分派），所以测量后
也不能将所有差值单因果归为“省容量”。新增8项合同，总40项。

新增独立slice-study：0/1/64/500/1024/1025/4096长度，源偏移0/1000，连续/原分段/
短切片混合三臂，六种排列轮换，六轮预热+12轮记录，各样本2000次；504个原始样本。
每次使用同一归一化切片语义，计时外验证引用次序。保留v3原host80样本，以检测未改
控制的变化，不跨run端点比较收益。新测试与host未执行C#，仅证据门18项Python自检通过。

```
dotnet run -c Release --project tests/perf/list-storage/slice-study/SliceStudy.csproj -- 12 > /tmp/list-slice-study.json
python3 -B tests/perf/list-storage/check_slice_study.py /tmp/list-slice-study.json
```

独立CI仍仅实验分支触发，40合同、v3原host80样本、尺寸504样本均须满足；不设速度
通过线以免抹去负结果。任何产品接线仍须原始list_ops、26项全量、matmul及既定语义/
搬迁/桥接/make check门禁；此实验不改变它们，更不能代替它们。
