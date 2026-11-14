# 当前三负载 AOT 性能基线

基于 dev `9bbc0c1f`，复用 `tests/run_benchmarks.py` / `benchlib`，不重写采样器，
不修改 provider、onefile 或 Python 绑定。输入使用仓库既有 `hello.py`、`calls.py`、
`str_concat.py`，不缩小循环或替换实现，不恢复历史生成预期。

## 固定计划

- Linux x64、GitHub `ubuntu-24.04` 共享runner，CPython 3.12.13、SDK 10.0.403。
  独立SDK安装并检查实际选择，事后核每个AOT清单实际SDK及schema1执行域。
- 干净源码提交、固定三输入及默认编译选项；报告完整提交、CPU信息、操作系统、工具路径，
  前后核源码提交与工作树未变。实验性 `RYUPY_*`、`COMPlus_*`、非工具定位的 `DOTNET_*`
  环境不继承；Python hash seed为0。
- 先JIT再AOT，每个输入均为一次预运行、五次计时，CPython先于产品；一共72次应用进程
  （3负载×2后端×2实现×6），不是72个独立同分布样本。
- 每次均为新进程，不清OS缓存。“预运行”不代表进程内JIT暖稳态。CPython使用既有runpy
  外壳，墙钟包含外壳启动成本；`main_ms`计时边界不同，不能直接宣称纯kernel加速。
- 真实stdout逐次与同次CPython一致才接受；统计缺失、非有限值、集合漂移或任一失败均
  令整轮失败，不择优丢负载、不追加重试样本。进程非零退出仍由原runner拒绝。
- JIT和AOT分别编译，不假称共享同一PE。编译成本含CLI/AOT发布，SDK/依赖缓存状态影响
  不隔离；首次恢复成本不能称纯ILC速度。先后顺序偏差及共享主机噪声保留。

## 指标与证据

`jit.json`、`aot.json`保留所有墙钟、Main、RSS样本；分配/GC/heap仅保留最后计时进程，
不是整个采样窗口分布。`run.json`记录源码、实际工具和AOT产物大小及选项；
`logs/`保留公开runner命令与双流。产物/报告只在仓库外临时目录和14天CI工件，禁止入Git。
统计表显示CP/AOT之比仅代表该提交、负载与环境，不是优化前后收益，也不是Nuitka对照。
现有hello搬迁正确性CI独立保留，本基线不重复把搬迁执行计入样本。

## 运行及范围

```sh
python -B tests/aot/test_current_baseline.py
# 实际构建/测量在已固定Python/SDK的CI；target必须是本轮独立根：
python -B tests/aot/current_baseline.py --target /tmp/ryupy-baseline-target --out /tmp/ryupy-baseline-result
```

专用CI只手动或独立基线分支触发，不在每次dev推送自动追加昂贵测量。
首次完整同提交结果待CI；脚本就绪或纯测试通过不等于AOT性能已有数据。
