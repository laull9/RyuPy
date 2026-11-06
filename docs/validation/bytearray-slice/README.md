# bytearray 切片赋值验证记录

基线 HEAD `d27e40e0fbf5d634917021008fc7a8ffe18e0232`，固定 CPython 3.12.13、
.NET SDK10.0.403、Rust1.99.1，Linux x86_64。支持域与算法见
[ADR0098](../../adr/0098-bytearray-slice-assignment.md)。

- `reference.py` / `results.txt`：CP 语义探针原源与原输出，包括本轮未支持的通用 iterable，
  不能将全部参考样例当作 RyuPy 通过项。
- `review_probes.py` / `review_results.txt`：list 活变异与 22,932 个机械翻译算法组合。
  只证明参考算法，不代替生产 C# 测试。
- 生产专项：`runtime/RyuPy.Runtime.Tests/PyByteArraySliceTests.cs`，首次 24/24。
- 公开差分：`crates/ryupy-driver/tests/bytearray_slice.rs`，先核固定 CP 原流/退出/全部用户帧，
  再编译/JIT。首轮 2/3，基础源因不支持的 slice 构造器编译失败，原 source 另保 pending。
- 原 regex fixture SHA256仍为 `56458872296c59d3646aac9358040fed5c358014ac341ee954d3a3eb9563e9d8`，
  本轮不修改该历史失败源；新的 driver 直接 include 原字节并已通过。

基础窄改仅单独重跑1/1通过，合计三unique应用通过；首2/3历史保留。
首次正常make check exit0，1771项C#、228项固定CP基线、202项e2e及原30 ignored，
fmt/严格clippy/workspace均通过。日志SHA与生产源码SHA见 `summary.json`；完整原流及产物
哈希随六件恢复包保存，不将DLL放入源码包。没有重跑完整项目矩阵、chardet、
NativeAOT或吞吐基准，不增加历史5/20分子。

后续ADR0099公开slice类型切面已直接执行本目录对应的原始构造器失败源并通过；历史首轮与pending文件保持原样，见../slice-constructor/README.md。
