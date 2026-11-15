# 真实产品 GetSlice 的独立验证门

产品候选 `3eebcceea4e9da24800a38990956360fce1b5ab3`；基线固定
`81758bb477729aee4636f2ca6d6b0815b949849a`，准备时已核远端 dev 正是此 SHA。
仅专用分支触发，不修改默认 CI；实验 workflow 不合入 dev。

## 顺序与验收

单个 ubuntu-24.04 runner、单 job、无 matrix。SDK10.0.403、Rust1.99.1、
CPython3.12.13（绝对PYTHON）。base/candidate 分离工作树、Cargo target 与内嵌运行时
Release CLI，不共享构建输出，不使用 RYUPY_RUNTIME_DIR 覆盖。

1. 两臂依次 make check；候选另跑 PyListSliceOwnershipTests 并核20个唯一通过实例、
   八个合同族及汇总计数。make check 的完整 Runtime 可能保留其已声明的专项条件跳过。
2. make build 冻结两CLI，编译器自身构建不计时。
3. pending 用例 m2_list_slice_owned_result.py 不依赖默认e2e发现：显式同次CP、两臂
   JIT与真实NativeAOT比较退出码及完整stdout/stderr。AOT另轮中文空格目录搬迁，
   隐藏原输出目录、PATH不可用。该搬迁未证明删除全部源码或其他机器可用。
4. 不改变现有 aot_hotspot_pair.py，默认全26项（含list_ops、matmul）。每项两臂
   构建后顺序CP/A/B及CP/B/A；1预热丢弃，5次新进程wall与RSS原始样本。统计和搬迁
   另轮，不与计时混用。检查最终26行及每臂样本完整；原脚本每项完成写入部分报告。

“独占”指本job没有后台构建或并发基准，不等于独占硬件；GitHub共享runner噪声、
新进程启动成本和时间顺序效应仍存在，不宣称暖态或全平台收益。最小产品变更仅移除
切片重复引用复制，不是分段背板上线，也不代签C ABI/NumPy/桥接专项。

## 执行预算与证据

job上限350分钟（低于GitHub托管6小时上限）；两臂各完整检查并冻结70、显式语义25、
26项性能150分钟各有限时，总计315分钟，留35分钟用于安装及证据上传。
这是一轮受限验证预算，不保证26×两臂AOT在150分钟内完成；超时保留已完成项，不将
部分报告算通过。没有complete.txt就不宣称完整验收；失败日志、完整双流、TRX、工具链、
基线/候选SHA、CPU/内核/磁盘和配对JSON按SHA上传。构建/生成产物不进入Git。

记录实际磁盘前后状态；各臂冻结CLI成功后清理其独占可重建Cargo target，避免两个大型Rust构建目录同时驻留；
记录中间磁盘状态。空间不足则准确记录失败，不偷偷复用另一臂输出。此环境无dotnet/cargo，当前仅静态与Python门禁轻测；发布须先独审。
