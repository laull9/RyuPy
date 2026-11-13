# dev 的最小 NativeAOT 验证链

`.github/workflows/aot-dev-validation.yml` 对 dev 推送、目标为 dev 的 PR 及手动运行生效。
这是独立的新门，不扩大历史性能工作流的触发范围，也不恢复被删除的生成预期文件。

## 验收范围

- 先运行 runner、AOT 布局、清单和公开 benchmark 入口的纯 Python 回归。
- Linux x64 使用仓库固定 CPython、固定 .NET SDK，实际执行一次公开
  `make bench-aot BENCH=hello` 管线。CPython 在同次运行生成 stdout 预期。
- 既有 `bench_smoke.py` 检查真实原生文件、清单、编译输入哈希与唯一新输出树；
  校验 CPython/原生各一次热身、五次运行及结果，再保留本次产物证据。
- Rust 构建、benchmark 产物及报告放 runner 临时目录，证据作为 14 天 CI 工件，
  不提交 IR、JSON 报告、生成 golden 或二进制。

该门只证明 hello 的编译/发布/执行及公开测量管线，不证明性能提升、全部兼容、
provider 可用性、搬迁或无 SDK 干净机运行。共享 runner 的五次 hello 时间不是性能基线。
完整 `make check`、provider 多版本矩阵和真实应用性能配对分别验收。
本地环境若不允许 SDK 所需 Unix socket，只运行以下轻量门；真实 ILC 结果以同提交 CI 为准。

```sh
python -B tests/aot/test_runner.py
python -B tests/aot/test_aot_layout.py
python -B tests/test_bench_aot.py
python -B tests/aot/test_bench_smoke.py
python -B -m unittest discover -s tests/aot -p 'test_manifest_*.py'
```

冷 release 构建使用 30 分钟保守预算，管线维持 32 分钟；作业总预算为 80 分钟，
为依赖安装与工件收尾留出余量。这些上限不是已验证耗时；首次 CI 后依据实际日志复核，
不能将环境安装或冷编译超时报告为应用语义失败。

## 后续性能迭代

先让该门在当前提交通过，再用 `tests/run_benchmarks.py --aot` 选择有证据的实际负载。
旧 `perf.py`、`pair.py` 和 pyfiglet 独立门仍有已清理预期文件消费者，必须迁移到
同次 CPython 差分后才能重新启用，不能以历史 CI 通过代替本轮验证。
每轮冻结源码、解析出的 Python/ABI、依赖、provider 配置及样本计划，分别报告构建、
启动、暖吞吐、RSS/分配与包大小。一次只晋升一个通过语义门且无超预算回退的优化。
