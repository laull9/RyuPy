# CLI NativeAOT接线本地门禁

2026-11-02，基于32c984a后的driver首实现。`make check`实际exit0：434 C#、225固定CPython3.12.13基线、199e2e+30ignored，fmt/clippy/全部Rust及driver快照通过。
另独立先跑3项driver AOT结构/早期保留名专项与2项CLI fakeSDK失败专项，均通过；全gate亦包含这5项。原e2e JSON与完整日志保留。

本地禁止ILLink所需UnixIPC，因此不重复绕过尝试；本门禁不等于CLI实际NativeAOT发布。之前真实CI41400422128只验证独立静态桩。下一CI必须由同一smoke真正调用CLI --aot，验证ILC、特殊路径及无运行时容器搬迁。
