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

## 首轮工具前置失败

CI41420306758的Windows job在Visual C++环境选择阶段失败，尚未进入Job轻测、
编译或ILC：PowerShell向cmd转发嵌套引号，使带空格的VsDevCmd路径被错误解析。
修正仅改该步骤为原生cmd调用，并用固定Python以UTF-8导出同一白名单环境；不修改
产品或跳过工具链校验。macOS该轮独立结果另记，不把此Windows失败称平台已验。

同轮macOS的生命周期轻测、runtime与CLI构建成功，但新工具元数据检查在应用编译前
失败：将`rustc`代理symlink按真实路径解析成`rustup-init`，改变了argv0分派，取得的是
安装器版本而非Rust host。窄修保留已找到工具的调用名，不取消实际Rust host/SDK架构
校验。两平台首轮均未执行ILC，不记作NativeAOT通过；既有Linux七配置同轮成功。

上述工具修后，本机永久轻测4通过/4平台专属跳过，真实本地rustc代理返回预期Rust host；
Windows shell及两平台迁移仍须新的真实CI，不以Linux轻测替代。

第二轮CI41421244429：macOS实际ILC与搬迁四模式成功（精简证据另归档）；Windows
已过VS选择、真实Job轻测和runtime构建，但Cargo检出固定ruff依赖时遇264字符路径
而失败，未进入产品CLI/ILC。仅在一次性Windows runner的用户Git配置启用并读回
`core.longpaths=true`；不修改系统注册表、安全策略、锁定依赖或凭据。依据
[Cargo官方问题记录](https://github.com/rust-lang/cargo/issues/13141)，不以缩删依赖
源码或静默跳过文件绕过。修后Windows结果仍待实际CI。

第三轮CI41422305992：macOS再次通过；Windows已构建CLI并运行CP/JIT，首次产品
差异是默认标准文本输出：CP104B含11个CRLF，JIT93B仅LF，内容/顺序完全相同。
不归一化比较器；修复限定PyStandardStream的Windows每LF翻译，并加原码点计数、
CRLF/孤CR、一次write、原Format/替换流边界专测；两平台在ILC前执行这组及旧CR输入回归。
该失败仍未到Windows ILC。fixture仅按路径固定LF checkout保证源码hash，不改期望。
显式RUSTUP_TOOLCHAIN=1.99.1锁后续真实工具选择，不改写此前Mac实际1.98证据。

标准输出窄修本机20项专项通过，完整make check退出0：454 C#/226固定CP/200 e2e
+30 ignored，fmt/clippy/Rust门禁通过，无快照更新。Windows策略纯测试不替代实际
Windows默认流验证，下一CI仍按原CP/JIT/native逐字比较与真实ILC验收。

第四轮CI41424104080（331c8c7）：macOS再次真实发布/搬迁成功，Linux七配置另行通过；
Windows20项标准流回归通过，CP/JIT四模式严格字节比较通过，两平台fixture源码SHA一致。
首次进入Windows SDK发布，Restore成功后MSBuild通配Import报MSB4019：Rust生成的
`\\?\D:\...`绝对路径中的问号被按MSBuild字面值转义，无法找到`Host.csproj.*.props`。
本轮仍未执行ILC，不把Restore成功当AOT通过。下一窄修仅处理SDK外部路径表示，
内部canonical归属/清理、特殊字符目录、逐字比较与真实搬迁验收不变。

SDK路径窄修在五处外部边界把已存在canonical Disk/UNC转换为普通绝对表示，并再次
canonical回查必须相同；内部owned目录、清理及返回产物仍使用原路径。拒device/volume、
危险组件与达到260 UTF-16单位的输入路径，不盲删前缀或更改长路径系统策略。
这个输入上限不保证SDK内部更深obj路径无其他工具限制。Windows三项实际身份/词法测试
在同一job执行，Unix一项保原路径；UNC未在网络共享实跑，不称完整UNC交付已验。

路径修本机10项driver专项通过，完整make check退出0：454 C#/226固定CP/200 e2e
+30 ignored，无快照更新。新增三项Windows路径测试及真实SDK发布仍待同平台CI；
Unix保留路径测试与完整本机门禁不能替代Windows路径语义验证。
