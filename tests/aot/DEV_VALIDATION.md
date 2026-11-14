# dev 的最小 NativeAOT 验证链

`.github/workflows/aot-dev-validation.yml` 对 dev 推送、目标为 dev 的 PR 及手动运行生效。
这是独立的新门，不扩大历史性能工作流的触发范围，也不恢复被删除的生成预期文件。

## 验收范围

- 先运行 runner、AOT 布局、清单和公开 benchmark 入口的纯 Python 回归。
- Linux x64 使用仓库固定 CPython、固定 .NET SDK，实际执行一次公开
  `make bench-aot BENCH=hello` 管线。CPython 在同次运行生成 stdout 预期。
- 既有 `bench_smoke.py` 检查真实原生文件、清单、编译输入登记与唯一新输出树；
  校验 CPython/原生各一次热身、五次运行及结果，再保留本次产物证据。
- 随后独立执行一对搬迁正确性比较，不计入上述性能样本：仅复制 dist 到中文、空格、
  `&` 路径，使用空 cwd、空工具 PATH 与最小环境，临时隐藏本次原 build/dist。
  同次 CPython 参考来自私有 hello 源码副本，native 运行前隐藏副本，严格比较退出码及双流。
  成功、输出不符、超时均经 finally 恢复本轮自有路径，不移动仓库源码或用户文件。
- Rust 构建、benchmark 产物及报告放 runner 临时目录，证据作为 14 天 CI 工件，
  不提交 IR、JSON 报告、生成 golden 或二进制。

该门只证明 hello 的编译/发布/执行及公开测量管线，不证明性能提升、全部兼容、
provider 可用性或无 SDK 干净机运行。搬迁门仅证明原 build/dist 路径不再需要；
仓库源码与宿主系统库仍存在，不能宣称源码路径不可访问或完全脱离开发机。共享 runner 的五次 hello 时间不是性能基线。
完整 `make check`、provider 多版本矩阵和真实应用性能配对分别验收。
本地环境若不允许 SDK 所需 Unix socket，只运行以下轻量门；真实 ILC 结果以同提交 CI 为准。

```sh
python -B tests/aot/test_runner.py
python -B tests/aot/test_aot_layout.py
python -B tests/test_bench_aot.py
python -B tests/aot/test_bench_smoke.py
python -B tests/aot/test_bench_relocation.py
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

## 普通发布空摘要合同

当前生成端 `manifest_files.rs` 的 `hash_bytes` 和 `digest_file` 明确把普通文件摘要
登记为空串，文件登记只核大小、打开前后身份；`docs/workflow.md` 同样不要求新增普通文件 hash。
本门仅同步普通 schema1：源码摘要允许为空，build inputs/artifacts 允许空摘要但仍严格
核文件集合、普通文件类型、大小、打开前后身份、当前输出树及原生文件头。
空摘要不保证源码/输入/原生产物内容一致，不能检测事先完成的等长内容篡改。
非空摘要必须是合法小写 SHA256 且核对实际内容，不能以空摘要支持为由忽略错误摘要。

共享核验函数默认仍要求摘要，binding/provider 及资源 schema2 合同在本切不放宽，
更广协议迁移另行验收。测试归档的复制前后摘要仅用于证据自校验，不恢复生产端摘要计算，
也不写回发布清单。此修复需由新提交 CI 再验真实编译和运行，旧工件 schema 通过不能替代重跑。

搬迁证据单列 `relocation.json` 和 `logs/relocation-*`；额外 CP/native 各一次，单次上限30秒，
仍服从原整组1800秒预算。失败不降级为仅原位成功，也不把搬迁运行混入五次时间统计。

恢复阶段逐项尝试所有安全路径，原路径被重建或恢复IO失败时保留冲突hidden，并在
`path_restoration`记录每项原路径、隐藏路径、恢复状态及汇总错误。只有原对象身份真正
恢复才记成功，不能用路径重新存在替代；冲突不阻断其他无冲突路径恢复。

## SDK 确定性

此前该工作流请求安装10.0.403，但共享runner预装的更高SDK仍可能成为默认选择；
成功记录实际使用11.0.100。这些旧结果仍是其记录版本上的有效正确性证据，不能改写为10.0.403。
当前只在本CI作业内用独立 `DOTNET_INSTALL_DIR` 安装既定SDK，不给产品仓库新增全局
`global.json`，不改net10.0目标或用户provider。安装后检查PATH命中的可执行路径和版本，
随后 `--expected-sdk` 再核本次真实MSBuild写出的版本，拒绝安装设置与实际发布版本漂移。
只有此精确提交的新CI成功，才能登记固定SDK上的AOT/搬迁通过；这仍不证明性能收益。
这只固定SDK选择：setup-dotnet v4仍会安装当时的LTS runtime，action标签及系统依赖也未全部
冻结，不能称整个工具链可复现。driver的 `DOTNET` 覆盖入口在本作业内也明确指向隔离可执行文件。
