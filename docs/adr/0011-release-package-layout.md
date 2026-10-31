# ADR-0011：发行包布局与运行时定位

- 状态：已接受
- 日期：2026-10-31

## 背景

`ryupy` 编译产物需要同目录的 `RyuPy.Runtime.dll`。此前只能通过 `RYUPY_RUNTIME_DIR`
或编译期 `CARGO_MANIFEST_DIR`（开发机上的仓库路径）定位运行时，打包后的二进制换台机器就找不到。
同时仓库没有任何「把 CLI 和运行时打成一个包」的命令，`make build` 不存在。

## 方案

### A. 把运行时嵌进 CLI 二进制

单文件分发，体验最好；但需要改动驱动层的产物读取方式（内嵌资源 / 落盘解压），
并牵扯 Native AOT 与 AOT 分析器约束（见 ADR-0005），现在做会提前锁定方案。

### B. 目录式发行包：`bin/ryupy` + `runtime/RyuPy.Runtime.dll`

CLI 依次查找：

1. 环境变量 `RYUPY_RUNTIME_DIR`（保留给测试与自定义部署）；
2. 可执行文件旁边的 `runtime/`（`<prefix>/runtime` 与 `<prefix>/bin/../runtime`）；
3. 开发期路径 `<workspace>/runtime/RyuPy.Runtime/bin/Release/net10.0/`。

成本最低，不改驱动层产物模型；代价是包内多个文件、需要按平台打包。

## 决定

采用 B。`make build` 构建 Release 版 CLI 与 C# 运行时，输出
`releases/ryupy-<版本>-<os>-<arch>/{bin,runtime}`，再打成同名 `.tar.gz`，并在打包后做一次
「不设 `RYUPY_RUNTIME_DIR`、编译并运行示例」的自检。

包名只反映宿主平台：当前不做交叉编译，也不承诺任意平台运行。`releases/` 属构建产物，不进版本库。

## 后果

- 发行包仍要求目标机安装对应 .NET Runtime（当前 net10.0），不是免依赖单文件；
- 运行时查找顺序把发行包放在开发路径之前，避免旧仓库路径干扰打包产物；
- 若之后实现内嵌运行时或 Native AOT，按 ADR-0005 新写一篇取代本篇，而不是改正文。
