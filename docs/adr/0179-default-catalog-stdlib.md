# ADR0179：默认发行开启认证标准库源码与应用联合集成

- 状态：已实现，默认 CLI 的 macOS arm64 JIT/NativeAOT 搬迁通过
- 日期：2026-11-13
- 关联：ADR0174、ADR0175、ADR0176、ADR0177、ADR0178、ADR0180

## 问题

版本化标准库 catalog 已有认证源码，但默认 CLI 仍需 pilot feature 才能使用。
数据库、网络、压缩与编码也需要在同一发布包中验证，独立模块用例不足以覆盖组合部署。

## 决定

1. 无 feature 的默认模块策略改为 `CatalogOnly`。只对认证 catalog 的静态导入开启
   已有实例环境与默认 AOT 来源计划；同名同字节用户文件不继承来源权限。
2. 用户源搜索顺序、父包身份、显式 InstanceModules 及 `ExplicitOnly` 保留。
   动态提示收集、metadata 候选与普通用户自动实例环境继续由 pilot 单独控制。
3. 来源选择与部署在 driver；纯 Python 原源走正常编译链，C# runtime 模块注册事实
   在构建期生成。codegen 继续只依赖 MIR，不按模块名绕过语言语义。
4. 网络、编码、压缩复用 .NET BCL；SQLite 使用成熟 Microsoft.Data.Sqlite/SQLitePCLRaw。
   版本合同、托管依赖、当前 RID 原生库与许可证随运行模式部署。
5. 以真实服务应用验证组合：HTTP 获取 Base64 包裹的 gzip JSON，读取包内 schema.sql，
   参数化写入 SQLite，再验证异常事务回滚。输入含中文、引号与 SQL 形状文本。

## 验收

- driver 默认策略与来源隔离专项四项；默认 Session 的 colorsys 六 API CP 差分。
- 无 feature CLI 的认证源码+C ABI+资源联合分发 11 步通过。
- 服务应用 CPython/JIT/原位 NativeAOT/搬迁 NativeAOT 输出完全一致；清单依赖和许可证齐全。
- 搬迁至中文、空格与 & 目录，删除源码、JIT 和构建目录，清理 Python/.NET/加载器变量，
  空工具 PATH、新 cwd 运行。缺失随包 SQLite 原生库时明确抛 ImportError。
- 逐 API 状态见 stdlib.md 和各专项文档，联合证据见 validation/stdlib-integrated/。

## 后续

扩大纯 Python 原源 catalog、公开 API 与固定应用覆盖。Linux/Windows、无 SDK 目标机、
代理/cookie/auth、高级 SSL、增量压缩及 SQLite 扩展 API 保持独立任务。
全部运行时暂携带 SQLite 依赖；按实际 provider 闭包裁剪依赖另行设计并验收，避免隐式缺库。
