# ADR-0005：Native AOT 打包方式

- 状态：方案 A 有限首实现待 CLI 实测（Linux x64 glibc；不代表整 AOT 里程碑完成）
- 日期：2026-10-31

## 背景

ILCompiler（`ilc`）通常通过 `dotnet publish /p:PublishAot=true` 驱动，而 publish 的输入是 **项目**，入口点来自项目自身编译出的程序集。
RyuPy 产出的是一个现成的 `.dll`，不是项目。

## 方案

### A. 生成桩项目
在输出目录下独占创建 `ryupy-aot-<pid>-<nanos>-<counter>/host/`，示意如下（实际引用使用经过 XML/MSBuild 转义的绝对 HintPath）：

```xml
<!-- RyuPyHost.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    <Reference Include="UserProgram"><HintPath>/absolute/output/user.dll</HintPath></Reference>
    <Reference Include="RyuPy.Runtime"><HintPath>/absolute/output/RyuPy.Runtime.dll</HintPath></Reference>
  </ItemGroup>
</Project>
```
```csharp
// Program.cs
internal static class Program
{
    private static int Main(string[] args) => RyuPy.Generated.Module.Main(args);
}
```
然后 `dotnet publish -c Release -r <rid>`。实现最简单，完全使用官方支持的路径。
缺点：每次 AOT 都要走一遍 MSBuild（秒级），且需要完整 SDK。

### B. 直接调用 `ilc`
从 NuGet 缓存中找到 `runtime.<rid>.Microsoft.DotNet.ILCompiler` 包，手工拼 `ilc` 参数和链接器命令。
更快、更可控，但参数多、平台差异大、官方不保证稳定。

## 倾向

采用 A 的有限首实现。现有 CI 已验证独立薄宿主的真实 ILC 与运行时无 dotnet 的容器行为；本次接入 driver/CLI 仍待专项实测，不能把前者当作 CLI 已通过。只支持本机 Linux x64 glibc，固定 `linux-x64`，其它平台与交叉发布明确拒绝。

## 后果

- 生成的 `.dll` 中所有代码必须是 AOT 安全的（不使用反射发射）——这也是 roadmap 的核心约束之一。
- `RyuPy.Runtime.csproj` 已开启 `IsAotCompatible`，AOT 分析器警告视为错误。
- 分析器通过不能代替实际发布。原生与桥接分别验收裁剪后的模块注册、异常与入口；桥接产物继续声明 libpython / 标准库 / 扩展依赖。

## 首实现部署与失败合同

- 真实生成入口是 `public static int Main(string[])`；薄宿主原样传 args 并返回退出码，不重复初始化或异常报告。
- 宿主 identity 与用户程序集及 RyuPy.Runtime 不同；显式唯一 Compile 项，禁祖先 Directory.Build.props/targets，裁剪/AOT 警告视错误，不使用全程序集 roots 或反射 fallback。
- 每次发布使用独占工作根，成功返回其 `publish/` 内可执行文件的绝对路径；完整部署只搬该 publish 目录。成功根保留 host/obj/日志供审计，后续构建不覆盖旧原生文件。
- 成功 SDK stdout/stderr 按原始 bytes 保存到根目录 `publish.stdout.log` / `publish.stderr.log`。日志写入或产物校验失败也算失败；不能只有 SDK exit0 就返回成功。
- 失败诊断包含 SDK 退出状态以及两流的完整 **UTF-8 lossy 文本**，不是原始 bytes 无损归档；CLI/CI 负责保存该诊断。失败清理仅本次独占创建的根，清理失败附加报告，不返回旧 native 路径。
- `RyuPy.Runtime` 为所有构建的程序集保留名（忽略 ASCII 大小写），必须在任何产物写入前拒绝，防止用户 DLL 与部署运行时相互覆盖；其它旧文件不得因该拒绝改变。
- 不改 JIT 语义、浮点规则或 globalization；不宣称跨平台、桥接 AOT 或完整性能优化已实现。
