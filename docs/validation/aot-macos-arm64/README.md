# macOS arm64：真实 CLI NativeAOT 与搬迁首验

状态：**此有限macOS交付已验**。源提交`22941abf99c2aad550da0282e74dd5b0f004bf5e`，
[Mac job](https://github.com/laull9/RyuPy/actions/runs/41421244429/job/125197027121)成功；
同workflow的Windows job失败，不能把整轮或Windows说成通过。

## 实际证据

- macOS14.8.9 arm64，.NET SDK10.0.403/runtime10.0.14、Xcode15.4、CPython3.12.13。
  **实际Rust为1.98.0**：repo `channel=stable`覆盖action设的默认1.99.1，不能按workflow
  旁注声称构建版本。后续显式固定工具链另验，不追改本轮实际版本。
- 真实公开`ryupy build --aot`调用SDK，原日志含`Generating native code`，stderr为空。
  无裁剪警告豁免或全程序集root。原两fixture源码SHA与冻结版本相同。
- all/argv/exit/error，CP/JIT/搬迁native共12应用，退出0/0/7/1；stdout逐字相同，
  JIT/native stderr逐字相同，CP完整异常帧签名及最终SampleError相同。
- Mach-O64 arm64 MH_EXECUTE，4,438,808字节，SHA与清单实算一致。系统依赖有
  libSystem、Objective-C、Swift、ICU及系统Frameworks，**不是零系统依赖**。
- 复制原生程序到中文/空格/&目录，删除本轮源码、IL与host目录后换cwd直接执行；
  搬迁目录只含native，不含Python源、RyuPy.Runtime.dll或.NET托管配置。

[精简摘要](summary.json)保源码/CLI/runtime/IL/native哈希、实际工具链与每模式状态。
两次编译IL的哈希不同且分别记录；runtime同字节，没有把非确定PE硬说成字节同一。
原raw日志/二进制在该CI artifact保留14天，可能到期；Git不提交原始日志或二进制。

## 范围与失败历史

这是装有SDK的hosted runner上的真实NativeAOT+构建树删除+搬迁+受限环境+依赖审计，
**不是在没有.NET/Python的干净机器上运行**。摘要明确
`clean_machine_without_runtime_tested=false`；不能借Linux容器证据替Mac背书。
未验mac x64、跨OS/架构、其它OS版本、签名公证、Gatekeeper发行、onefile或所有程序。

首轮41420306758未到应用ILC：Mac工具元数据检查把rustc代理symlink resolve到
rustup-init而改变argv0；Windows的PowerShell→cmd引号亦失败。两处窄修并保存失败记录，
没有删语义断言。第二轮本Mac job通过，Windows则在固定ruff依赖长路径检出失败，
后续Windows独立验证继续，不计本记录的成功范围。
