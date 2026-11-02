# AOT前置：真实AssemblyRef身份（2026-11-02）

基线为远端M3.10 a788d86，非丢失的M3.11。SDK10.0.403 / Runtime10.0.14、Linux x64。
本记录尚不是NativeAOT发布成功；driver的publish_aot仍是显式未实现。

## 实证与必要修复

原生成DLL中的System.Runtime引用为Version0/tokenNone，JIT可运行，但真实C#薄宿主
编译报CS0012（before.log）。InspectIdentity.cs是仅供开发验证的工具，不进入应用；
对SDK reference pack及RyuPy.Runtime读取实际AssemblyName得到identities.log：

- System.Runtime：10.0.0.0 / b03f5f7f11d50a3a。
- System.Runtime.Intrinsics：10.0.0.0 / cc7b13ffcd2ddd51。
- RyuPy.Runtime：1.0.0.0 / unsigned；项目显式固定该独立ABI版本。

集中helper只修三条编译期引用，不改vendor默认、不添加运行期反射、不改变方法体；
Intrinsics仍仅向量程序建立。真实emit→parse PE测试检查版本/token/full-key/culture，
不能用未打印这些字段的旧CIL快照冒验证。

## 当前结果与环境阻断

修后薄宿主C#编译已通过（after-host-compile.log）。随后ILLink的TaskHostFactory
报MSB4216/MSB4027：当前执行沙箱连socket(AF_UNIX)创建都返回EPERM。
正式require_escalated请求同一publish命令，环境启动器在进入命令前报bwrap只读路径错误；
没有访问或修改受限制路径。没有通过关裁剪、抑制警告或全程序集root绕过验证。
因此当前不能声称ILC/native产物完成，后续需受支持的构建环境后继续实际发布。

薄宿主为 `static int Main(string[] args) => RyuPy.Generated.Module.Main(args)`，
保返回码与参数。探针print(1+2)来自真实RyuPy CIL。现有函数/模块/闭包/生成器/异常工厂
已有静态ldftn引用，更多裁剪正确性必须经真正publish与行为验收，不靠源码推定。

官方实现依据：[.NET10.0.14 Native Unix targets](https://raw.githubusercontent.com/dotnet/runtime/v10.0.14/src/coreclr/nativeaot/BuildIntegration/Microsoft.NETCore.Native.Unix.targets)
允许非Apple环境使用gcc备选，故本轮没有因为缺clang就擅装新链或关闭AOT。

## 本次身份修的完整回归

`full-check.log`实际exit0：434 C# /225固定3.12.13基线 /199编译差分、30 ignored；
fmt/clippy/全部Rust单元与driver快照也通过。11个codegen测试含新PE身份测试；
SLP packed/scalar/ISA-off与2000项位模式payload专项共3个driver测试原断言通过。
真实生成DLL输出3。首SLP专项因调用时未选固定Python而使用环境3.12.14，版本断言失败；
`first-regression.log`在下方Git历史保留，修复仅设置PYTHON到已安装3.12.13，没有改源码/断言。

上述是程序集身份修的语义/JIT门禁，不是AOT发布或性能提升验收。

## 原始记录归档

当前目录仅保留人工摘要、已有结果JSON及验证工具（如有），不再跟踪运行日志。
本文提到的原始日志/命令/状态/逐项结果可从提交 `16cf490dc6678763d695c9d54708b70e7dabeff5` 的
[`docs/validation/aot-reference-identity/`](https://github.com/laull9/RyuPy/tree/16cf490dc6678763d695c9d54708b70e7dabeff5/docs/validation/aot-reference-identity) 恢复；例如：

```bash
git show 16cf490dc6678763d695c9d54708b70e7dabeff5:docs/validation/aot-reference-identity/after-host-compile.log
```

后续运行产物应写入忽略的 `target/`，CI链接保留用于定位运行；Actions工件当前仅保留14天，不能视为永久归档。
