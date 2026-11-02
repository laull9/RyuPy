# macOS / Windows 同宿主 NativeAOT 首切

状态：WIP首实现；Linux本地6项新平台纯测试及3项旧AOT专项通过，完整门禁
445 C#/226固定CP/200 e2e+30 ignored通过，无快照更新。工具轻测本机3通过/4平台专属跳过，
不把跳过计成功。真实macOS/Windows ILC/
迁移尚未验收，不据RID开关宣布支持。
本切不改变 runtime ABI、生成程序入口或裁剪保根规则，不重新展开兼容性长尾。

## 固定平台与交付

- 现有 Linux x86_64 glibc / `linux-x64` 保持原 ELF/Docker 验收。
- 新 macOS arm64 / `osx-arm64`，CI 显式 macos-14，并核实际宿主/SDK 架构。
- 新 Windows x64 MSVC / `win-x64`，CI 显式 windows-2022，并核实际宿主/SDK 架构。
- 未验架构、Windows GNU、跨 OS 发布明确拒绝，首切不含 universal/Rosetta。

[Microsoft 官方前提](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)：
macOS 需要 Xcode Command Line Tools；Windows 需要 Visual Studio C++ 桌面工具。
[跨编译约束](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/cross-compile)
不因底层部分跨架构能力而自动扩大本项目范围。

每平台一次普通 IL 和一次真实 `ryupy build --aot`；沿原四模式 all/argv/exit/error，
CPython3.12.13、JIT、搬迁 native 共12应用进程。保返回码0/0/7/1、全部stdout、
JIT/native stderr逐字、CP完整帧签名与最终错误。参数保空字符串、空格、Unicode；
不把发布文件存在或纯文件头视为 ILC/语义成功。trim/AOT 警告继续阻断，不加全程序集root。

生成入口仍是静态 `int Main(string[])`，保持原参数、退出码、异常打印；不重复模块初始化。
输入保留名在写入前拒绝，独占 stage 不复用旧 native；失败仅清本轮目录并保 SDK 诊断。
Windows输出 `.exe`，Mach-O64 arm64 executable / PE32+ AMD64 非DLL且无CLR header
有界验证；所有格式/路径失败不得降回JIT或返回旧文件。

## 搬迁与依赖证据

保同一原始 fixture，不为新平台删错误/参数断言。目录含空格、中文、&；复制真正必要
native产物到新目录后删除本轮源码、IL和host输出，再改cwd/精简环境直接执行native。
Windows `.dll` 可能是原生依赖，不能一刀删除；依赖需逐项格式与导入表审计。
macOS记录 `otool` 依赖/rpath，Windows记录 PE 和 `dumpbin` 依赖/imports。
拒绝对已删除构建树、托管runtime/apphost依赖的冒用；保正常系统库依赖声明。

**这不是未安装运行时的干净机器测试**：hosted runner仍安装SDK/Python，PATH隐藏
仅证明未从PATH解析它们。结果必须写 `clean_machine_without_runtime_tested=false`；
Linux runtime-deps容器的证据不能外推Mac/Windows。不卸载SDK，不关闭Gatekeeper/SIP，
不修改系统安全设置，不新增付费VM；签名、公证、安装器和单文件打包均另列后续范围。

## 超时和审计

每次应用30秒、普通IL编译600秒、ILC1800秒；脚本总截止30分钟，每次调用取
独立上限与剩余总预算的较小值。CI步骤32分钟/任务40分钟留终止和工件上传余量。
POSIX使用独占进程组。Windows先挂起子进程，再加入非继承、无breakaway、kill-on-close
的独占 Job Object，然后仅恢复该PID的唯一初始线程；失败终止并回收，不放行未归属进程。
正常超时需轻回归证明子孙一并结束。创建与Job归属之间若runner被外部强杀仍有挂起
进程窗口，不宣称原子无孤儿；这不通过修改机器策略规避。

记录实际OS/架构、SDK/Rust/CP版本、CLI/runtime/source/native哈希、完整SDK双流、
每应用状态及依赖。任一实际失败即保存失败，不换期望或静默跳过。原始工件只保CI
artifact（14天），Git只归档精简结果和限制，不提交日志或二进制。
