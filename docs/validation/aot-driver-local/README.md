# CLI NativeAOT接线本地门禁

2026-11-02，基于32c984a后的driver首实现。`make check`实际exit0：434 C#、225固定CPython3.12.13基线、199e2e+30ignored，fmt/clippy/全部Rust及driver快照通过。
另独立先跑3项driver AOT结构/早期保留名专项与2项CLI fakeSDK失败专项，均通过；全gate亦包含这5项。原e2e JSON与完整日志已移出当前源码树，可从下方Git历史恢复。

本地禁止ILLink所需UnixIPC，因此不重复绕过尝试；本门禁不等于CLI实际NativeAOT发布。之前真实CI41400422128只验证独立静态桩。下一CI必须由同一smoke真正调用CLI --aot，验证ILC、特殊路径及无运行时容器搬迁。

## 原始记录归档

当前目录仅保留人工摘要、已有结果JSON及验证工具（如有），不再跟踪运行日志。
本文提到的原始日志/命令/状态/逐项结果可从提交 `b40e6e2a925e76831934ed5db60170b4764151b0` 的
[`docs/validation/aot-driver-local/`](https://github.com/laull9/RyuPy/tree/b40e6e2a925e76831934ed5db60170b4764151b0/docs/validation/aot-driver-local) 恢复；例如：

```bash
git show b40e6e2a925e76831934ed5db60170b4764151b0:docs/validation/aot-driver-local/e2e.jsonl
```

后续运行产物应写入忽略的 `target/`，CI链接保留用于定位运行；Actions工件当前仅保留14天，不能视为永久归档。
