# macOS arm64 / Windows x64：真实 NativeAOT 同宿主交付

状态：**固定有限交付通过**。生产源提交 `7443c78c6f11143de80023963e738ea4b10d0581`，
[双平台 CI41425715952](https://github.com/laull9/RyuPy/actions/runs/41425715952) 两 job 成功；
同提交 [Linux smoke/七配置](https://github.com/laull9/RyuPy/actions/runs/41425715946) 成功。
本地完整门禁454 C#/226固定CP/200 e2e+30 ignored通过，无快照更新。

## 实际验证

- macOS14.8.9 arm64 / `osx-arm64`，Windows Server2022 x64 MSVC / `win-x64`。
  两侧实际Rust1.99.1、.NET SDK10.0.403/runtime10.0.14、CPython3.12.13。
- 公开 `ryupy build --aot` 真正调用SDK/ILC，SDK输出有 `Generating native code`，
  stderr为空；裁剪/AOT警告仍为错误，无全程序集root或反射回退。
- 每平台all/argv/exit/error × CP/JIT/搬迁native共12次运行，退出0/0/7/1。
  stdout逐字一致，JIT/native stderr逐字一致，CP完整异常帧和最终错误一致。
  不做换行归一化来掩盖差异；Windows默认标准文本流在生产边界正确翻译LF。
- 参数包含空串、空格、中文与`--flag`；输出目录包含中文、空格与`&`。
  删除本轮源码、IL、host目录后，换cwd以受限环境执行搬迁原生程序。
- Mac为thin Mach-O64 arm64 executable；Windows为AMD64 PE32+ executable、CLR目录零。
  清单逐文件bytes/SHA与实际产物一致；Windows EXE为4,584,960字节。
- Mac依赖系统dylib/Frameworks；Windows依赖系统DLL及UCRT API sets。
  没有把Python源、RyuPy.Runtime.dll、托管运行时配置放入搬迁目录，**仍依赖宿主系统库**。

[精简摘要](summary.json)保原结果字段（去临时环境绝对路径）、原result哈希、源码与
编译器/runtime/IL/native哈希、实际工具链及模式状态。两次IL编译的不同哈希分别记录，
不把它们伪称字节相同。原日志和二进制仅在CI artifact保留14天，Git不存raw logs。

## 边界

`clean_machine_without_runtime_tested=false`：两台hosted runner仍装有SDK/Python。
删除构建输入、PATH隐藏和依赖审计不是“无SDK/Python干净目标机”测试；该项仍未完成，
不能借Linux容器证据替Mac/Windows背书。未验其它OS版本/架构、Mac x64/Rosetta、
Windows GNU/arm64、交叉编译、签名公证/安装包、onefile及所有Python程序。
Windows SDK路径仅接受可回查同一目标的普通Disk/UNC表示，输入小于260 UTF-16单位；
UNC网络共享未实跑，SDK内部更深路径仍可能遇工具限制。保留明确诊断，不改注册表。

## 真实失败与修复历史

1. 41420306758：Windows工具链cmd嵌套引号失败，Mac把rustc代理resolve成rustup-init；均未到ILC。
2. 41421244429：Mac首验成功（[历史证据](../aot-macos-arm64/README.md)）；Windows固定依赖Git长路径检出失败。
3. 41422305992：Windows CP/JIT默认标准输出CRLF差异，仍未到ILC；在标准流边界修复，不改比较器。
4. 41424104080：输出及源码哈希已一致，Restore后MSBuild无法处理canonical verbatim路径的通配Import；未到ILC。
5. 当前轮：SDK五处路径边界保身份转换、Windows三项真实路径专项通过，再完成原四模式发布/搬迁。

各轮未失败项不冒整平台成功；首轮Mac Rust1.98历史保持原样，本轮显式选择1.99.1。
本次没有重新测量I64性能；先前180进程数字仍只属于其冻结测量提交。
