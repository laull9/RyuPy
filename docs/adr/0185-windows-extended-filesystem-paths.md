# ADR0185：Windows extended 文件系统路径的有限准入

日期：2026-11-13。状态：修复草案，尚未完成本轮修复后的运行时、真实 Windows AOT 与完整回归验收。

## 首轮整合证据

整合提交 `bd982f278c81205f07f7bc230ad866586ca558bd` 的 CI [43367296487](https://github.com/laull9/RyuPy/actions/runs/43367296487) 保留四组原版验收：`direct_entry`、`binding_multi`、`binding_resources`、`resources_as_file`。Linux/macOS 原件已通过复核；macOS 的 schema 5 单导出加资源、schema 6 多导出加资源、原位/搬迁原流及真实 ILC 直接入口输入已检查。这些是首轮整合的有限宿主证据，不证明此处的新修复或无 SDK 目标环境通过。

Windows 首轮失败暴露两类问题：

1. `binding_resources` 从 `ProcessPath` 获取的真实部署路径含 `\\?\` 前缀；它进入 `PyPath` 时被共享词法层的设备路径门禁拒绝。Native 已构建不等于部署执行成功。
2. `resources_as_file` 原测试直接比较路径文本；Windows 普通绝对路径与带前缀路径的表示不同，不能仅凭文本不等就判断资源身份不同，也不能为让测试变绿无差别剥除前缀。

保留首轮失败，不把 Linux/macOS 绿灯覆盖到 Windows。

## 决定与有限支持范围

在 ADR0173 的共享 Windows drive/root/tail 词法层修复，不在 `importlib.resources`、特定包或单个 IO 调用处旁路。

- 仅准入 extended 盘符绝对路径（ASCII 盘符）与完整 extended UNC（非空 server/share）这两种类型。共享词法接受 CPython 对照中的宽容分隔符拼写，包括 `\\?\C:\...`、`//?/C:/...`、混合分隔符、`//?/UNC/server/share/...` 与小写 `unc`；是否准入由解析后的路径类型决定，不扩展到任意设备路径。
- 保留 extended 前缀身份与文件名分量的尾点、尾空格；不将 extended 路径整体转换为普通路径。Path 按 PureWindowsPath 的词法行为规范化斜线、`.` 与重复分隔符，保留 `..`；有限 `ntpath` 风格的 dirname/join 保留相应原始分隔符拼写。这不是原始 Win32 字符串逐字保留或实际文件系统等价性承诺。
- `\\.\` 设备命名空间、pipe、Volume GUID、GLOBALROOT、盘符相对 extended 路径、残缺 extended UNC 及其他未列出的设备形式仍明确拒绝。不通过通用去前缀或 fallback 将设备解释成普通文件。
- 保留既有普通盘符、根相对与完整 UNC 行为，以及非 ASCII 盘符/UNC 大小写等价判断的既有限制。不宣称完整 pathlib/ntpath、任意长路径或所有 Windows 命名空间兼容。
- 不修改资源声明资格、资源树选择、输出相交预检、manifest、nofollow、安全路径穿越合同或 AOT 入口选择。

验收器应按它要证明的文件身份检查路径，不将大小写、符号链接或 extended 前缀的不同文本一律视为相同。修正测试必须保留资源内容、源/托管输入移走后的搬迁执行及缺资源负例，不能只放宽 stdout 比较。

## 待验收

- 共享词法层的正常盘符/UNC 回归、extended 盘符与完整 UNC 的分量/父路径/join/绝对性、前缀与尾点空格保留，宽容分隔符和小写 UNC 的 CP 对照，以及其他设备形式的拒绝矩阵。
- 固定 CPython 3.12.13 的有限词法对照与 Windows 实际文件查询；纯算法跨宿主测试不能代替 Windows 文件系统行为。
- Windows 四组公开 AOT 验收重新执行，尤其 schema 5/6 真实交付清单、资源内容、原位/搬迁执行、失败不破坏旧产物及缺资源诊断。保留 Windows 薄入口；不据此扩大直接入口范围。
- Linux/macOS 回归与完整门禁；新一轮结论必须绑定修复后的提交和原件。首轮整合成功项不是修复后通过项，当前不宣称性能收益。
