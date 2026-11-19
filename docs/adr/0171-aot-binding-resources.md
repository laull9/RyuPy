# ADR-0171：固定 C ABI 与显式资源的组合 AOT 发布

状态：已实现并整合；宿主执行证据见[专项验收](../../tests/aot/binding_resources/README.md)。

## 决定

允许 `ryupy build entry.py --aot --resources resources.json --native-binding binding.json`。
版本 1 单导出和版本 2 单库多导出均支持；JIT 的资源请求继续明确报错。
资源选择、固定签名与库数量沿用各自既有合同，没有新增绑定类型语言或目录扫描。

组合发布复用既有 `ResourcePlan`，完整路径为：

1. `build_configured` 将资源请求交给同一次绑定编译。
2. `compile_linked_locations` 按真实源码图建立资源 consumer 与模块虚拟位置，返回唯一计划。
3. driver 在追加原生生成模块前捕获 `SourceModules`；发射使用这些已定位源码与实际 PInvoke 记录。
4. `publish_binding` 将该计划传给既有 AOT publisher，在启动 SDK/ILC 前冻结资源与静态 archive。
5. SDK 成功后从冻结资源复制到 publish，标记 `declared_resource`，扩展同次绑定清单。
6. 高层布局准备完整候选、核验并安装 dist；失败按既有 owned attempt 留下诊断与 SDK 双流。

所有 IO、暂存及部署编排仍在 driver。MIR 只携已有模块位置，codegen 仍消费显式绑定与源码来源，
运行时 API、`RuntimeFn`、`importlib.resources` 的实现保持原样。

## 两种来源与两种命名空间

`resources` / `module_locations` 的 `module_id` 只能来自本次真实源码集合。
追加的原生模块保留 `declared_native_binding` 来源，逐函数 `binding_id`、真实 wrapper/PInvoke
与导出槽记录完整保留。声明原生生成模块为资源 consumer 会在资源计划读取阶段失败。

资源仍位于 `_internal/resources/v1/<logical_source 的父目录>/<显式文件>`，并使用既有 ASCII
portable 路径、来源身份、祖先冲突及预算规则。数据路径相对实际源码文件目录。
原生共享库的 JIT 布局仍为 `_internal/native/v1/b0/library.*`；AOT 只静态链接 archive，
最终 dist 不复制该 archive 或 shared。数据名和原生输入名互不推导，不能借资源记录给库来源背书。

已冻结资源在 ILC 后只从 staging 复制；源数据之后改变不改变成功 dist。
已有资源身份/快照与产物清单校验继续生效，本次工具没有新增普通文件 hash 核验流程。

## 清单版本与兼容性

组合清单新增两个版本，完整保留对应绑定基础清单的来源、输入与实际发射报告：

| 发布请求 | 绑定声明版本 | 产物清单版本 |
| --- | --- | --- |
| 无资源绑定 | 1 | 3，保持原合同 |
| 无资源绑定 | 2 | 4，保持原合同 |
| 显式资源加绑定 | 1 | 5 |
| 显式资源加绑定 | 2 | 6 |

schema 5/6 在基础绑定清单上增加 `module_locations`、`resources` 与
`scope.declared_resources_complete`。`resources_included` 按实际数据是否非空计算；空 files
可以只签发模块位置，完成标志仍为 true。动态依赖闭包、原生保根证明及清单自身摘要标志保留原值。
普通无绑定发布继续使用其原 schema 1/2/4 合同。

选择新版本是为了让旧严格消费者明确拒绝尚未理解的组合产物，避免把多导出来源降格成单导出，
或把绑定清单误写为普通资源 schema 2。布局层接受 schema 5/6，并对其执行既有资源命名策略。

## 失败与发布事务

缺数据在 SDK 启动前失败；资源模块不在真实源码图、错误声明、路径冲突继续明确失败。
静态缺符号由真实链接器拒绝，完整 SDK 输出与 `build-error.log` 保存在本轮 attempt，
失败 attempt 没有成功产物清单。所有这些失败都保留原成功 dist 的字节和执行行为。

成功提交复用既有 dist 候选/备份/回滚事务；跨平台安装仍为两次 rename，存在其既有崩溃窗口。
本次没有改造目录交换原语、锁或所有权 ledger，也没有新增自动清理失败现场。

## 验收范围与后续

专项使用真实包数据、两个实际 C 符号、CPython ctypes 参考和 NativeAOT 原生入口。
v1/v2 各发布一次成功程序，v2 另用缺第二符号 archive 做真实失败发布。成功 dist 整树搬迁，
原源码/库及 build 中间路径全部改名后，在带同名诱饵数据的其他 cwd 逐字比较原双流。
缺资源副本应抛带 Python 帧的 FileNotFoundError。

整合验收已使用 `as_file(files(...))`，覆盖 Path 身份、正常/异常退出保留资源和原异常传播。
多库、通用 ABI、传递原生依赖收集、wheel/zip、单文件自解包与干净目标机验证继续独立推进。
