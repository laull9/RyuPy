# 首次真实NativeAOT与无托管运行时搬迁验收

- 源码：`8662402c40ece18d7511eff2e980c57d49acbd20`，基于恢复后的M3.10。
- 实际CI：https://github.com/laull9/RyuPy/actions/runs/41400422128
- 终态：2026-11-02 06:33:40 UTC，success，Linux x64任务2分22秒。
- SDK10.0.403、Rust1.99.1、CPython3.12.13；本地IPC限制未绕过，使用正常标准runner。

`result.json`取自该run工件；原始`logs/`已移出当前源码树，见下方历史归档。publish确实完成`Generating native code`，
不是仅编译C#桩；publish stderr为空，trim/AOT警告作为error，没有全程序集root或JIT回退。
同一输入的all/argv/exit/error四模式分别经过CPython、JIT、搬迁native及无dotnet/Python的
runtime-deps容器验证；正常/退出stderr为空，用户异常JIT/native stderr逐字相同，CP比较
只按测试中公开的路径/源码展示行规范化，保留全部帧和最后异常消息。SystemExit退出7。

发布后删去本轮源码/IL/宿主/publish目录；容器仅挂载relocated目录，禁网/只读，明确检查
dotnet/Python命令不存在。实际ELF仍依赖libm、libc及Linux加载器，不是完全静态二进制。
工件NativeHost SHA256：`d89cdc1d5582b7843829627a234bc9dad89865312f517fbb661dfa6cb885ee2c`。
二进制曾随Actions工件上传；工件保留期仅14天，不保证长期可下载。本仓库当前保留摘要、结果及哈希，原始命令、输出和状态可从Git历史恢复。

范围：这证明真实生成CIL的模块注册/跨模块调用、闭包、kwargs、生成器恢复清理、用户异常
及argv/退出码可经静态桩裁剪发布运行。不是全部语言/库、ISA组合或性能验收；
`ryupy build --aot`的生产driver仍待独立实现。本次没有恢复丢失的M3.11代码。
CI另有旧action Node20被runner提升Node24的弃用提示，非应用trim/AOT诊断。

## 原始记录归档

当前目录仅保留人工摘要、已有结果JSON及验证工具（如有），不再跟踪运行日志。
本文提到的原始日志/命令/状态/逐项结果可从提交 `32c984ad549a27700d7284923154e6111855f494` 的
[`docs/validation/aot-smoke-ci/`](https://github.com/laull9/RyuPy/tree/32c984ad549a27700d7284923154e6111855f494/docs/validation/aot-smoke-ci) 恢复；例如：

```bash
git show 32c984ad549a27700d7284923154e6111855f494:docs/validation/aot-smoke-ci/logs/compile.command.json
```

后续运行产物应写入忽略的 `target/`，CI链接保留用于定位运行；Actions工件当前仅保留14天，不能视为永久归档。
