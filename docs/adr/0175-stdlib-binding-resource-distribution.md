# ADR0175：标准库源码、C ABI 与包资源共同分发

- 状态：已实现；macOS arm64 真实 JIT、NativeAOT 与搬迁验收通过
- 日期：2026-11-13
- 关联：ADR0160、ADR0171、ADR0173、ADR0174

## 问题

应用需要同时复用 CPython 标准库源码、调用声明式 C ABI、读取包数据。
此前固定绑定编译关闭 catalog；普通自动 AOT 与显式资源请求互斥。
这使各自可用的能力无法组成同一个可搬迁应用。

## 决定

1. driver 在成功解析绑定声明后，开启本次编译私有的 catalog 来源权限。
   编译完成或失败均还原整个来源状态；普通 fixed 标志无法签发 catalog 权限。
   原有用户源码优先级、策略门、模块同名冲突检查继续有效。
2. 自动 AOT 源码位置与显式数据声明合并为同一个 ResourcePlan。
   校验全部模块锚点与数据目标的路径冲突；显式数据 claims 始终保留。
   模块发现索引在 staging 投影到最终发射 ID；延期模块的资源消费请求明确报错。
3. 来源报告显式记录本次声明的 `c_abi_binding` provider。
   绑定与用户源码冲突、代码发射检查完成后才写报告，冲突不得创建输出目录。
4. 普通自动源码计划保持 schema4；绑定 v1/v2/v3 联合自动来源使用 schema9，
   其中保留原绑定字段并追加资源和自动来源审计。显式资源完整性单独记录，
   自动产生模块位置时不会宣称收集了未声明的数据文件。
5. 构建期扫描全部运行时 C# 文件的 `PyModules.RegisterNative[Lazy]` 字面调用，
   生成 probe 使用的注册事实表。提取器跳过注释和字符串中的伪调用，
   动态模块名和字符串拼接仍为未知。注册事实描述来源，API 覆盖以独立差分表为准。

## 架构与取舍

全部来源选择、依赖报告与文件部署留在 driver；codegen 仍只消费 MIR 和来源元数据。
没有运行用户 Python 来发现依赖，没有引入运行时 IL 发射或解释器回退。
JIT 继续从源码位置读取包数据；显式资源的独立部署由 AOT 路径提供。

完整 Traversable、动态执行、任意 CPython 扩展、跨平台干净目标环境继续分别验收。
本合同解决联合分发，不扩张 catalog 中已准入模块的语义范围。

## 验证

`tests/aot/stdlib_distribution/run.py` 构建自有 C fixture，执行：

- CPython 参考、JIT 和 NativeAOT 同时使用 colorsys、`as_file` 与 bytes C ABI；
- 核对 source-plan 中唯一 provider、闭包和 schema9 资源/源码记录；
- 缺资源构建失败，逐字节检查原 dist 仍完整；
- 搬迁至中文、空格及 `&` 路径，删除源码、JIT 和构建目录；
- 清除 Python/.NET/加载器环境变量，空工具 PATH、不同 cwd 执行；
- 删除搬迁资源后得到 FileNotFoundError。

2026-11-13 本地 macOS arm64 的 11 步全部符合预期。
空 PATH 搬迁验证与无 SDK/runtime 安装的干净目标机验证分开登记。

本轮完整 `make check` 通过：C# 3025、CPython 基线 721、Python-only 自检 724、
编译 e2e 444 通过／284 ignored；driver 定向与 pilot feature clippy 通过。
