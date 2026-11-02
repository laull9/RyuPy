# 首次真实NativeAOT与无托管运行时搬迁验收

- 源码：`8662402c40ece18d7511eff2e980c57d49acbd20`，基于恢复后的M3.10。
- 实际CI：https://github.com/laull9/RyuPy/actions/runs/41400422128
- 终态：2026-11-02 06:33:40 UTC，success，Linux x64任务2分22秒。
- SDK10.0.403、Rust1.99.1、CPython3.12.13；本地IPC限制未绕过，使用正常标准runner。

`result.json`及`logs/`原样取自该run工件。publish确实完成`Generating native code`，
不是仅编译C#桩；publish stderr为空，trim/AOT警告作为error，没有全程序集root或JIT回退。
同一输入的all/argv/exit/error四模式分别经过CPython、JIT、搬迁native及无dotnet/Python的
runtime-deps容器验证；正常/退出stderr为空，用户异常JIT/native stderr逐字相同，CP比较
只按测试中公开的路径/源码展示行规范化，保留全部帧和最后异常消息。SystemExit退出7。

发布后删去本轮源码/IL/宿主/publish目录；容器仅挂载relocated目录，禁网/只读，明确检查
dotnet/Python命令不存在。实际ELF仍依赖libm、libc及Linux加载器，不是完全静态二进制。
工件NativeHost SHA256：`d89cdc1d5582b7843829627a234bc9dad89865312f517fbb661dfa6cb885ee2c`。
二进制由Actions工件保留；本仓库只保存小型命令、输出、状态和结果证据。

范围：这证明真实生成CIL的模块注册/跨模块调用、闭包、kwargs、生成器恢复清理、用户异常
及argv/退出码可经静态桩裁剪发布运行。不是全部语言/库、ISA组合或性能验收；
`ryupy build --aot`的生产driver仍待独立实现。本次没有恢复丢失的M3.11代码。
CI另有旧action Node20被runner提升Node24的弃用提示，非应用trim/AOT诊断。
