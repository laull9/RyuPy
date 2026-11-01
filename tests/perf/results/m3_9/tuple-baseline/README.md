# M3.9 tuple标量替换前基线（当前已验证默认holder）

保存产物根目录：`/tmp/ryupy-m3_9-tuple-baseline-KCoa1e`。基于e1c1ee1加已验证dirty源码，
不是仅靠旧e1二进制推测。source.patch包含捕获时crates/runtime的所有tracked修改和untracked新增
源码/测试，排.pending与性能结果目录；source-manifest.json记录基SHA及逐文件hash。CLI构建前捕获，
构建成功后保存CLI与Release runtime，之后才向生产worker释放编辑权。未用实验累加器flag。

hashes.txt固定实际CLI/产物/runtime；归档不含DLL/CLI/bin/obj。workload.py是负载快照，
MIR/CIL可见swap逐轮MakeTuple→Len→UnpackCheck→两GetItem，read逐轮MakeTuple→GetItem；
escape必须保留传saved.append的真实tuple。所有返回tuple/元素身份、空/偶数轮、独立逃逸对象断言
在固定CPython3.12.13与真实DLL短验通过。构建日志和原始输出全部保留。

## 正式基线口径

默认tier清除Jit/Tiered环境，禁tier仅设DOTNET_TieredCompilation=0。每模式3新进程，
每场景进程内3轮热身7测量，每次20001循环；先进程中位再跨进程中位。输入和saved.clear计时外，
调用/逃逸保留在计时内，语义检查在计时后。wall单列，不混函数体时间。

| 模式 | swap | read | escape |
|---|---:|---:|---:|
| 默认tier | 3.448951 ms | 1.570596 ms | 4.344413 ms |
| 禁tier | 1.429709 ms | 0.684232 ms | 1.743922 ms |

这是单边基线，不据此承诺候选收益；候选完成必须在同一窗口交错重跑B/C，不能用不同时间单边相除。
当前环境未隔离CPU/固定频率。正式测量不同时采集分配或JIT。

## 分配与JIT

harness加载真实DLL并初始化模块，再反射调用三个m0_tuple_*函数。输入、实参数组复用；
escape列表20次预热后保容量，计数前清理。每进程每方法20次预热、5批单次20001轮调用；
默认/禁tier各3新进程，所有批次结果一致：

- swap：1280128 B（64.00319984 B/轮），含末尾返回tuple的64 B。
- read：1280064 B（64 B/轮）。
- escape：1280088 B（64.00119994 B/轮），仍必须保所有真实tuple；微小固定项不能称逐轮成本。

归因候选消除tuple+元素数组应使用同harness差值，逃逸应保留约64 B/轮，不以清空容器假装消除。
分配没有计时，实际JIT另开进程记录jit-default/disabled.asm（含全部分层版本），不污染正式基线。

## 重现

从source-manifest的基提交在独立工作树应用source.patch，按固定工具链cargo build -p ryupy-cli；
用默认CLI编workload.py到artifact，然后：

```sh
python3 compare.py baseline=产物根/artifact --output 新结果目录
dotnet build harness/alloc.csproj -c Release
python3 allocation.py 产物根
```

candidate配对可向compare.py增加candidate=目录，脚本按进程轮转变体。harness项目引用
../artifact/RyuPy.Runtime.dll，须保持实际产物与运行时配套。allocation.py输出到传入根目录，
重现时使用新目录，勿覆盖本次原始记录。本阶段未提交，完整工程gate由父单独维护。
