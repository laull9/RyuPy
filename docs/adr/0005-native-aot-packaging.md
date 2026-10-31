# ADR-0005：Native AOT 打包方式

- 状态：提议（M1.6 实现时定稿）
- 日期：2026-10-31

## 背景

ILCompiler（`ilc`）通常通过 `dotnet publish /p:PublishAot=true` 驱动，而 publish 的输入是 **项目**，入口点来自项目自身编译出的程序集。
RyuPy 产出的是一个现成的 `.dll`，不是项目。

## 方案

### A. 生成桩项目
在 `target/ryupy/<name>/aot/` 下生成：

```xml
<!-- RyuPyHost.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="../<name>.dll" />
    <Reference Include="../RyuPy.Runtime.dll" />
  </ItemGroup>
</Project>
```
```csharp
// Program.cs
RyuPy.Generated.Module.Main(args);
```
然后 `dotnet publish -c Release -r <rid>`。实现最简单，完全使用官方支持的路径。
缺点：每次 AOT 都要走一遍 MSBuild（秒级），且需要完整 SDK。

### B. 直接调用 `ilc`
从 NuGet 缓存中找到 `runtime.<rid>.Microsoft.DotNet.ILCompiler` 包，手工拼 `ilc` 参数和链接器命令。
更快、更可控，但参数多、平台差异大、官方不保证稳定。

## 倾向

M1.6 先做 A，跑通后测量耗时；只有当 AOT 构建时间成为痛点时再研究 B（这本身是个不错的学习主题）。

## 后果

- 生成的 `.dll` 中所有代码必须是 AOT 安全的（不使用反射发射）——这也是 roadmap 的核心约束之一。
- `RyuPy.Runtime.csproj` 已开启 `IsAotCompatible`，AOT 分析器警告视为错误。
