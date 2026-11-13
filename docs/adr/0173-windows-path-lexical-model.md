# ADR0173：Windows Path 的有界共享词法模型

日期：2026-11-12。状态：实现候选，尚未完成产品验收。

## 问题与决定

真实 Windows AOT 项目在统一隔离环境下，CPython 对照成功，而 Native 经 `importlib.resources.files` 构造 `PyPath` 时触发旧 Windows 词法门禁。不能给 pyfiglet、exists 或资源包旁路，也不能把编译成功视作打包成功。

在运行时共享 Windows drive/root/tail 词法模型，保持文件系统查询与编译器层无关。覆盖普通相对路径、盘符相对/绝对路径、当前盘根相对及完整 UNC；Path 清理重复分隔符与点分量，但保留 `..`。同一模型供 Path 的分量、父路径、组合、名称与绝对性使用；现有 os.path 的有限入口按其各自规则消费，不把 Path 清理混成 normpath。

新增共享词法辅助方法为运行时内部实现，不扩展编译器可调用的公开 ABI。

宿主 Path 必须具有正确 WindowsPath 身份，异平台具体路径类明确拒绝。CPython 3.12 的 `os.path.isabs` 与 `Path.is_absolute` 不完全相同，分别验证。路径 equality/hash 必须一致；不能将 CLR 忽略大小写直接当作 Python Unicode lower。

## 边界

- 设备命名空间继续明确拒绝，不用混合斜杠绕过。已有非 ASCII 盘符比较限制不悄然扩域。
- 未验证的盘符工作目录解析不通过简单拼接或 `GetFullPath` 冒充正确；绝对化能力单列。
- 不改资源资格、anchor、注册、manifest、metadata nofollow 或路径穿越安全合同。
- 不提供跨宿主 PureWindowsPath 产品类；纯算法测试可跨宿主运行，但不能替代真实 Windows 文件执行。
- 此竖切不代表完整 pathlib/ntpath、所有第三方包、干净 Windows 目标机或性能达标。

## 验证要求

固定 CPython 3.12.13 的 ntpath/PureWindowsPath 矩阵，覆盖盘符、UNC、双分隔符、点/父分量、组合、身份与拒绝项；C# 定向测试及完整 make check。随后实际 Windows 同宿主文件查询及公开 AOT 两入口，源隐藏、原位与搬迁、完整553字体、standard/slant/fonts 原流对照和 nofollow 3/3 全部通过才可更新应用状态。Mac/POSIX 保持回归，未测部分仍列未测。
