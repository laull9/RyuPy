# ADR0174：版本化原源 catalog 与可解释静态依赖报告

状态：已实现限定版本事实、静态探测、colorsys 六 API 的 CPython 差分与本机 AOT 搬迁。
日期：2026-11-13。承接 ADR0160、ADR0172；完整标准库目标未完成。

## 决策

以版本化原源事实和真实 resolver 选取结果共同决定 provider。driver 负责发现、版本、
依赖和报告；源码通过既有 syntax → HIR → ty → MIR → CIL 编译，runtime 保持 C# 协议
和系统适配。后端不接触 HIR/ty 或文件发现。sqlite、OpenSSL 等继续复用成熟外部实现，
本次没有重写底层，也没有改 C ABI 或绑定发布合同。

优先接入依赖闭合且有实用价值的 `colorsys`：颜色转换服务图像、终端配色及可视化，
原源零 import，六个公开 API 可独立逐一验收。没有按模块名添加语义特殊分支。
选择 `queue` 作为“已知来源但依赖不足”的真实反例，展示静态缺口，而非伪造兼容承诺。

## 版本事实和权限

- catalog 版本 `cpython-3.12.13-catalog-2`，事实 schema 2，Python 参考 3.12.13。
- 原四模块维持 CI42932089715 的原字节来源；上游 tag 尚未核实，单独明示。
- 新 `colorsys`、探测专用 `queue` 来自 CPython `v3.12.13/Lib/`，保留上游原文与许可。
- 五个可准入源码与仅探测来源分开：`queue` 不在 Token 表，不进入 SourceDb 编译图。
- 入口目录 → 各 `-I` 根的优先级和普通父包固定规则保持。用户源码选中后，报告中的
  catalog/version 为空，`provenance_authorized=false`，即使字节完全相同也一样。
- 原有 `ExplicitOnly` / `OrdinaryDynamicPilot` 和默认 AOT 资格门保持；catalog provenance
  只证明真实来源，正常编译阶段继续检查语法与能力，失败不静默改走解释器。

## 静态报告合同

新增 `ryupy probe <入口> [-I 根] [-o 目录] [--aot]`。复用 build 的真实模块图和来源固定点，
然后遍历 AST 的全分支、函数体、星号与相对导入。没有调用 Python、执行入口或扫描
宿主 site-packages；`--aot` 选择 AOT 的发现门，探测本身不运行 dotnet/ILC。
副作用只有读取源码、写 JSON 报告和在输出目录部署已经获准的 catalog 原源/许可。
Session 原有来源计划在探测结束后恢复，审计输出位置与来源权限载体分离。

报告 schema 1，独立文件 `<assembly>.source-plan.json`，probe 同时打印 JSON。
正常 build 在图发现后写同一形状，因此后续语义编译失败仍留有静态证据。
JIT 文件在输出目录，AOT 文件在 `<stem>.build/attempt-.../`，不进入最终 dist。

每个模块最多一个 `provider`：

| provider / 状态 | 决定证据 |
| --- | --- |
| user_source / source_selected | 实际 resolver 选中的路径与包身份 |
| cpython_source / source_selected | 同一次图中的真实 Token 与版本事实 |
| csharp_runtime / runtime_registered | 内嵌 C# 源码字面 RegisterNative 注册表 |
| null / source_detected_unselected | 根中存在用户源码，真实编译图尚未选入；仅在独立探测数据库递归解析 |
| null / catalog_unavailable | 已知原源但未获本次策略准入，或角色为 detected_only |
| null / dependency_missing | 显式根、准入图及 C# 注册表都没有 provider，或父包固定阻止补齐 |

模块记录 `source`、`version`、`catalog`、`reason`、直接 imports/dependencies、语法诊断，
并以固定点传播 `missing_dependencies` / `dependency_closure`。环内缺口会传至所有依赖者；
闭合环保持 closed。`from math import sin` 的属性不伪造为 `math.sin` 模块。
未准入 catalog 的用户依赖、现有编译图未接纳的星号导入仍继续静态解析，明确标记
source_detected_unselected，报告实际路径及后续缺口；不能改选同名 C# provider 或授予 Token。
C# 内部依赖由原有运行时发布合同负责，报告明确记录该范围，属性/API 仍逐项验收。

`__import__`、importlib 导入与 find_spec、exec/eval/compile/getattr 等保存为独立动态证据，
包含可识别别名和字面目标。字面串只作线索，不能授予额外来源权限或解除不确定性。
识别到调用时 `dynamic_status=uncertain`；没有识别到时为 `no_recognized_dynamic_import`，
始终 `dynamic_analysis_complete=false`。任意反射、间接调用、导入钩子仍有静态盲区。
`compiler_verified=false` 明示这是编译前来源计划，closed 只描述静态 provider 闭合；
成功生成和运行程序、逐 API 差分、目标平台 AOT 都有独立验收证据。

## 已验范围与后续

`colorsys` 六个公开 API 的正常颜色、无彩色、hue 扇区、绕回、HLS 高低亮度、
YIQ 截断、TypeError 和模块身份共 55 行与固定 CPython 3.12.13 输出一致。
转换浮点值比较到小数九位；没有宣称浮点原始位或所有自定义数值对象等价。
同一 fixture 经真正 NativeAOT 发布，搬迁 dist 后删除原源码及 build 目录，清除
PYTHONHOME/PYTHONPATH、PATH 指向无工具目录，在其它 cwd 执行并复核全部 55 行。

本机为 macOS arm64；Linux/Windows 实机、干净目标机、完整上游测试和性能尚待验收。
queue 仍缺 threading/heapq/_queue 等 provider，线程、锁、异常身份及 API 未接入。
开放动态执行、完整标准库、libpython 兼容宿主及自动原生库闭包仍按 ADR0172 推进。
详见 [验收记录](../validation/source-catalog-v2.md)。
