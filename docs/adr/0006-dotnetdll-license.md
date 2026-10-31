# ADR-0006：dotnetdll 的 GPL 许可证影响

- 状态：已接受
- 日期：2026-10-31

## 背景

`dotnetdll` 0.3.0 的许可证是 **GPL-3.0-or-later**。链接它的 `ryupy` 可执行文件在分发时整体须遵循 GPL。

## 方案

### A. 接受 GPL
本项目是学习项目，不计划闭源分发，没有实际影响。

### B. 换掉 dotnetdll
例如自己写 ECMA-335 PE writer（工作量很大，但学习价值也很高），或改为由 C# 侧用 `System.Reflection.Metadata` 生成程序集。

## 决定

选 A。所有对 dotnetdll 的使用限制在 `ryupy-codegen-cil` 内部（见 architecture.md 依赖规则 5），保留将来替换的可能。

## 后果

- **注意**：生成的 `.dll` 与 `RyuPy.Runtime` 是否受 GPL 约束，取决于它们是否包含 dotnetdll 的代码——通常不包含，但如果有一天要商用，需要认真核实。
- 仓库目前没有设置许可证，若公开发布需补充。
