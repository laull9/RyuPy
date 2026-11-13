# 公开离线AOT目录核验：三平台真实应用结果

独立分支 `integration/aot-distribution-verify-20261113`，验证代码 `da56a16b07605a9f788aa349ca1f5615efa25e3c`，基于已整合用户catalog/通用ABI的 `5ff6bd18`。CI [43384272262](https://github.com/laull9/RyuPy/actions/runs/43384272262) 的Linux x64、macOS arm64、Windows x64全部通过；15份原件SHA/长度和实际内容已分别核验，身份见[机器收据](distribution-verify-20261113.json)。未改dev/main，本轮验证纳入的用户dev为`979a7f3c`。

## 用户可用的结果

```sh
ryupy verify ./release/app.dist
```

命令只读核对AOT目录的文件集合、相对路径、size/SHA和唯一native角色，复用已有布局核验，不编译、不调用SDK/Python、不写回修复。成功打印稳定JSON，失败非零退出。编译globals即使放在子命令前也拒绝，不悄悄读取metadata配置。

`content_integrity_verified`仅表示文件与清单自洽。`authenticity`、`runtime_compatibility`、`schema_semantics`明确为`not_checked`；RID只回显声明。自洽同时修改文件和清单可以通过，不能当作签名或可运行保证。具体合同见[ADR0188](../adr/0188-offline-distribution-verification.md)。

## 实测

同一代码、固定Rust1.99.1、SDK10.0.403、CPython3.12.13。每平台公开构建两个锁定真实项目：pyfiglet1.0.2真实metadata入口/完整553字体，binaryornot0.4.4/chardet5.2.0四类分类输入。SDK版本从每次真实host原件核对，工具、上游archive、源树前后和managed/发布输入摘要均核。

每平台共30次verify，10次成功、20次明确拒绝，三平台均无skip。每应用原位/搬迁两次，加13个独立副本；拒绝同尺寸字节损坏、缺文件、多文件、未知schema、JIT kind、路径逃逸、重复条目、零/双native与实际符号链接。三个额外允许副本为额外空目录、只读权限、自洽重写清单与native字节；最后一种仅验包，不执行破坏过的程序。

全部调用前后文件字节/权限/目录及链接目标快照一致，验包用空PATH和失败工具哨兵且未触发。Windows符号链接确实创建并被拒绝，没有用权限不足跳过；其只读测试记录DOS只读属性，不假称目录ACL隔离。

源码、上游和build树整体移走后，独立执行搬迁Native：每平台7组完整退出码/stdout/stderr与固定CP及冻结平台黄金一致，共21组。pyfiglet渲染走UTF8 bytes，字体列表才依平台文本换行；没有把输出统一归一化掩盖差异。

最终本地Linux完整make check退出0：C#3116通过/6跳过，driver库271通过/4忽略（含15项新增验包测试），编译e2e458通过/286既有忽略；CLI新增3项参数合同测试。harness另有7项轻测及合成小包smoke，不替代上述真实项目证据。

## 边界

- 仅支持既有AOT版本号1–8的文件自洽核验，不认证完整schema含义、二进制格式、执行位、原生依赖闭包或实际目标机能力。
- 根本身链接/非目录在读取manifest前拒绝；祖先链接并非全面禁止，也不提供恶意并发替换或原子快照保证。
- GitHub artifact ZIP可能改变权限或链接表示。原宿主权限和链接以现场before/after快照为证，不能拿下载后解包权限代替。
- 空PATH/工具哨兵不是系统调用沙箱、断网或无SDK新机器证明。工具Python/dotnet身份按原版本/记录核，二进制本体未全部上传。
- 没有声称本功能或新catalog/ABI带来性能提速；旧直接入口A/B仍对应其原实验提交。

本次后续文档提交只登记这些结果，产品及验收脚本与上述已验证代码一致。原件保留期由CI设为7天，机器收据保存原件身份与摘要，不将临时下载地址写入仓库。

## 发布前发现的用户并行工作

最终核远端发现用户dev新增`a851eab1`：标准库源码、C ABI和资源联合分发及schema9。本轮未合入这些改动，也不把已有1–8验包证据外推为支持9。后续须在独立整合分支保留用户新测试、处理ADR0175编号重叠，再沿同一底座验证schema9；不另造ABI/provider实现。
