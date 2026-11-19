# 原版 libpython 必要胶水探针

配套 [ADR-0223](../../docs/adr/0223-stock-libpython-bounded-glue.md) 与
[非侵入式路线评估](../../docs/libpython-noninvasive-evaluation.md)。

本目录验证原版 CPython 正常初始化后，保留必要 importlib/codec 胶水，并使用产品
`runtime/RyuPy.CPython/native/import_hooks.h` 的原生 finder/loader。
`dispatch` 的两个模块由纯 C stub 创建，用于隔离导入服务层；真实 CIL 模块、完整应用闭包、
第三方扩展产品与搬迁由对应产品测试验收。

## 运行

当前只开放已实测的 macOS arm64、常规 GIL CPython 3.12/3.13 共享库。
不支持的平台、版本和缺失共享库都会明确失败。无需构建完整 CPython 或 .NET Runtime。

在仓库根目录运行功能与输出验收：

```sh
python3 tests/libpython_embedding/run_probe.py \
  --python "$HOME/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12" \
  --repo "$PWD" --iterations 10 --repeats 2

python3 tests/libpython_embedding/run_probe.py \
  --python "$HOME/.local/share/uv/python/cpython-3.13.3-macos-aarch64-none/bin/python3.13" \
  --repo "$PWD" --iterations 10 --repeats 2
```

默认参数为 `--iterations 10000 --repeats 4`。正偶数轮次按 native/a2、a2/native
交替，避免固定先后顺序。`--python` 与 `--repo` 支持其它安装位置，源码不含本机绝对路径。

runner 分别查询目标解释器配置、调用 clang、执行子进程，各有超时；所有子进程的工作目录、
构建产物和 dSYM 位于仓库外临时目录，正常及异常退出自动清理。stdout/stderr 输出到终端，
失败子进程保留已捕获的输出。仓库只保留本目录源码与说明，不保存原始日志或计时报告。

## 观测合同

- `native` 直接 include 产品原生 hook，保留原版 importlib 的锁、ModuleSpec 和模块事务。
- `a2` 用静态 C callable 替换真实 `_frozen_importlib` 的三个私有服务入口，仅覆盖探针固定包。
  A2 的锁回调为空操作，没有完整回滚、并发循环和任意 fromlist 合同，只作为研究对照。
- 两个 profile 进程在正常初始化后安装 PEP523 观察器，以
  `PyUnstable_InterpreterFrame_GetCode` 记录每个进入观察器的函数及来源，随后委托原版求值器。
  PEP523 与该 Unstable API 仅用于已验证 minor 的探针观测，不进入被测产品。
- profile 改变解释器分派，单独进程运行，其时间不参与计时中位数。计时进程不安装观察器。
- 128 项归属表满、字符串超过容量、code/属性查找失败均有独立计数并令进程失败；
  `OBSERVATION` 必须三个错误计数均为零，明细合计须与总数一致，codec 正向操作必须观测到帧。
  查询、编译和运行的 stderr 均须为空；关闭时出现告警即失败，即使退出码为零。
- PEP523 在初始化之后安装，因此本探针不统计启动函数帧，也不构成全生命周期执行拒绝门。
  早期仓外 LLDB 研究统计的是 `_PyEval_EvalFrameDefault` **入口次数**；CPython 可在一次入口内
  执行多个内联函数帧，两种口径分开记录。`exec` audit 事件也不能代替逐帧观测。

## 本轮功能验收

2026-11-16，macOS arm64，原版 python-build-standalone：

- `3.12.13 (main, Jun 23 2026, 15:44:24) [Clang 22.1.3 ]`
- `3.13.3 (main, May 30 2025, 05:45:55) [Clang 20.1.4 ]`

上面两条命令均通过：包/fromlist 与相对导入返回真实模块值 `42`，UTF-8 decoder 返回汉字，
标准输出空写与 flush 成功，Finalize 返回零，观测完整性错误均零，临时目录清理完成。
这些功能验收使用极小计时次数，执行时存在其它仓库验证负载，时间不参与研究结论。

推荐 native 路线的逐帧分布：

| 阶段 | 3.12.13 | 3.13.3 | 来源 |
| --- | ---: | ---: | --- |
| 安装原生 finder/loader | 0 | 0 | C API 与缓存模块 |
| 首次导入固定包及 child，含 fromlist/相对调用 | 56 | 50 | frozen importlib 的锁、ModuleSpec、模块事务、fromlist |
| 缓存普通模块直接导入一次 | 0 | 0 | C 缓存命中 |
| 缓存包 fromlist 一次 | 1 | 1 | frozen importlib `_handle_fromlist` |
| UTF-8 增量 decoder 构造及 decode | 3 | 3 | frozen codecs 两个 `__init__` 与 `decode` |
| stdout 空 write/flush、关闭 | 0 | 0 | 本探针触发的 C 流路径 |

因此，必要胶水的动态成本集中于冷导入事务、包 fromlist 和 codec 对象初始化。
业务/标准库/框架函数的 CIL 执行归属仍须通过真实产品闭包测试建立。

## 前置研究小样

正式整理本目录前，仓外初稿做过 6 轮平衡次序、每轮 100 万次缓存导入；下表为中位数。
当时采样先于主代理宣布启动完整门，未独立监控全机负载。初稿与正式版的 API 检查有差异，
这些结果仅用于选择后续研究方向，不作为当前源码的性能基线或产品性能门。

| 版本与操作 | native | A2 |
| --- | ---: | ---: |
| 3.12.13 缓存普通模块，100 万次 | 48.924 ms | 48.166 ms |
| 3.12.13 缓存包 fromlist，100 万次 | 103.746 ms | 106.497 ms |
| 3.13.3 缓存普通模块，100 万次 | 46.218 ms | 45.126 ms |
| 3.13.3 缓存包 fromlist，100 万次 | 105.018 ms | 109.607 ms |

初稿的两模块冷导入约为 native 17.5/18.5 µs、A2 6 µs；两条路线均先正常 bootstrap。
A2 省下的冷导入工作同时包含其尚未实现的事务职责，不能按完整等价实现计算收益。
推荐先采用 stock 正常初始化、原版必要胶水和已有原生 finder/loader，下一竖切验证固定
真实 CIL 模块图与原生扩展消费者；若产品剖析发现胶水进入热点，再逐项优化。
