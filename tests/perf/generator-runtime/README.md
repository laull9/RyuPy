# 生成器 Runtime 协议隔离验证

基于 `ec67823f9ef62769be46cd8abde3b9cfe011409e`，专用分支
`work/generator-runtime-ci-20261115`。workflow 只监听该分支 push，不改 dev/main
和默认 CI，也不触发 Rust/CPython/AOT 全仓任务。该实验 workflow 不应合入默认分支。

隔离安装并确认实际 SDK 为 10.0.403：先 Release 编译并执行
`PyGeneratorSumTests` 的 13 方法/16 实例，再运行无 filter 的完整 Runtime suite。
完整套件即使定向步骤失败也会尝试执行，保留更多故障证据。全部测试必须通过且无跳过，
TRX 的非空唯一 testId、定向名称唯一、合同族实例数、总计/执行/通过数量均检查。
完整套件实际数量由 TRX 记录，不把合成门禁自检的 17 条记录冒充真实测试数量。

工件以本次 checkout SHA 与 attempt 命名，包含 SDK 信息、定向/完整 TRX、日志和
门禁输出；失败同样上传现有证据。原始结果不进 Git。8 组 Python 门禁正负例在 CI
先执行，可本地运行 `python3 -B tests/perf/generator-runtime/test_check_evidence.py`。

这里只验证 Runtime 协议候选与现有 Runtime 回归。**未验证编译器接线、CP 差分、
产品 JIT/NativeAOT、打包搬迁或性能收益**。实际 SDK 和 NuGet 依赖解析须看 CI 工件；
静态 workflow 校验或门禁自检不能替代 C# 编译运行。
