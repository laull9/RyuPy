# 生成器 Runtime 协议隔离验证

基于 `52fffb0035372114608adab3385871684359532c`，专用分支
`work/generator-runtime-ci-20261115`。workflow 只监听该分支 push，不改 dev/main
和默认 CI，也不触发 Rust/CPython/AOT 全仓任务。该实验 workflow 不应合入默认分支。

隔离安装并确认实际 SDK 为 10.0.403：先 Release 编译并执行
`PyGeneratorSumTests` 的 14 方法/17 实例，再运行无 filter 的完整 Runtime suite。
完整套件即使定向步骤失败也会尝试执行，保留更多故障证据。定向测试必须全部通过且零跳过；完整套件仅允许下述两个源码身份各跳过一次，
TRX 的非空唯一 testId、TestDefinitions 类/方法映射、定向名称唯一、合同族实例数、
总计/执行/通过/未执行数量均检查。其余失败、跳过和重复白名单记录一律失败。
完整套件另外必须至少有一个非定向测试实际通过，防止仅定向合同加白名单跳过冒充全套。
完整套件实际数量由 TRX 记录，不把合成门禁自检的 20 条记录冒充真实测试数量。

工件以本次 checkout SHA 与 attempt 命名，包含 SDK 信息、定向/完整 TRX、日志和
门禁输出；失败同样上传现有证据。原始结果不进 Git。20 组 Python 门禁正负例在 CI
先执行，可本地运行 `python3 -B tests/perf/generator-runtime/test_check_evidence.py`。

这里只验证 Runtime 协议候选与现有 Runtime 回归。**未验证编译器接线、CP 差分、
产品 JIT/NativeAOT、打包搬迁或性能收益**。实际 SDK 和 NuGet 依赖解析须看 CI 工件；
静态 workflow 校验或门禁自检不能替代 C# 编译运行。

## 完整套件条件跳过白名单

- `RyuPy.Runtime.Tests.PyTextDedentInitializationTests.FreshProcessContract`：
  源码 `DedentFreshFact` 在未提供 `RYUPY_TEXTWRAP_INIT_SCENARIO` 时跳过。
- `RyuPy.Runtime.Tests.PyTraceStateArtifactTests.ProducedAssemblyPreservesEveryBaselinePublicSignature`：
  源码 `TraceBaselineFact` 在未提供 `RYUPY_TRACE_BASELINE_DLL` 时跳过。

这里的属性名不是测试方法名，门禁使用上述完整类/方法身份并与 TRX 定义表核对。
此 CI **不证明 dedent 首次进程初始化合同，也不证明历史构建 DLL 的公开 ABI 兼容**。
实际通过数与跳过数分开报告；合成门禁正例中 22 条记录不是实际 Runtime 套件数量。

## trace 迁移修复后的实跑门禁

固定安装 CPython 3.12.13，确认实际版本与绝对解释器路径，以 `PYTHON` 环境变量
提供给 TextWrap 即时 oracle。此修复仅补齐 Runtime 单测依赖，不代表完整 CP 兼容验证。
全套 TRX 必须包含 `GeneratorSumBridgeRejectsWrongSignatureAndExtraAuthority` 一项通过，
以及 `ActualPeRejectsGeneratorSumAuthorityAndFinallyOrderChanges` 两个参数化实例通过。
仅执行普通非定向测试不能替代上述真实 PE 变异验证；仍不提供历史 DLL，旧 ABI 专项
依前述精确白名单跳过，不允许把它报为通过。

## VSTest 17.14.1 汇总字段语义

真实 run `43680868168` / commit `407c770164c5b20efc40639b7dd8a8d7c525e543`
的 full TRX 有3204条：3202 Passed、2个上述精确白名单 NotExecuted；汇总
`total=3204, executed=3202, passed=3202, notExecuted=0`。旧门禁误把最后一个字段
当跳过数而失败，不能把该失败描述为产品测试失败。

固定版本的 [TrxLogger.cs](https://github.com/microsoft/vstest/blob/v17.14.1/src/Microsoft.TestPlatform.Extensions.TrxLogger/TrxLogger.cs)
传入 total、passed+failed、passed、failed；
[TestRunSummary.cs](https://github.com/microsoft/vstest/blob/v17.14.1/src/Microsoft.TestPlatform.Extensions.TrxLogger/ObjectModel/TestRunSummary.cs)
将 notExecuted 等其余字段设为0。因此门禁对本固定 logger 精确要求 notExecuted=0，
**跳过许可仍只来自逐项身份白名单**，total必须等于全部逐项记录、executed/passed必须
等于逐项通过数。任意第三项跳过、重复跳过、focused跳过、总数不守恒仍拒绝。
真实工件重放通过不等于新版 workflow 已重跑；历史 DLL 与首次初始化专项仍未执行。
