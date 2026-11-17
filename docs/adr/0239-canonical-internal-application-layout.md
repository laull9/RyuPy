# ADR-0239：应用入口与统一 internal 依赖目录

- 日期：2026-11-17。
- 状态：已接受；macOS arm64 定向产品搬迁通过，完整联合门另行登记。
- 接续：[ADR-0232](0232-generic-native-interface-distribution.md)。

## 决定

所有公开 CLI 的非 OneFile 应用分发，根目录只保留实际入口文件与精确名称
`internal/`。JIT 入口使用官方 .NET SDK `CreateAppHost` 任务，指向
`internal/<入口>.dll`；程序集、runtimeconfig、deps、Runtime、provider、CPython、
原生依赖与配置都放在内部。NativeAOT 保留根入口，其余内容采用相同布局。
版本化资源、原生绑定与接口分别位于 `internal/{resources,native,interfaces}/v1`。

JIT 构建现在需要 .NET SDK，以生成官方 apphost；这改变了旧 JIT 仅需 Runtime 的
构建前提。用户已接受安装 SDK 的方案。缺 SDK 时先给出中文诊断、官方 SDK 下载页及
官方安装脚本命令，再停止构建；不继续尝试 msbuild 后输出难以操作的英文错误。
JIT 应用运行只需对应 .NET Runtime，部署机器无需 SDK。NativeAOT 的构建 SDK/原生
工具链及独立运行合同保持原有要求。纯 PE 发射 API 仍可只产生 DLL。

公开 `Artifacts.native_exe` 指向实际入口，`Artifacts.dll` 对 JIT 指向内部程序集。
CLI `run` 启动实际入口并传播退出码。CLI 只打印一条公开“已生成”入口记录，独立
“CIL 中间产物：”记录保留构建诊断所需的原始 managed 输入位置。

## 加载协议

`internal/.ryupy-layout.json` 是明确的布局证明，精确五字段：

```json
{"schema_version":1,"kind":"ryupy-application-layout","payload_mode":"canonical-v1","entry":"app","entry_kind":"native_aot"}
```

`entry_kind` 还允许 `apphost`。marker 为不超过 4096 字节的普通文件，拒绝重复键、
未知字段、符号链接和无效入口名。Runtime 只检查 AppContext 基目录及其直接
`internal/` 子目录两个候选；无 marker 时保留原平铺布局。损坏 marker 报错，不能
静默选择另一种路径。新 marker 下 `DependencyRoot` 为内部目录，旧 CIL
`_internal/...` 逻辑 ABI 映射到该目录相对子树，已有显式路径 API 保留合同。

Bootstrap、模块位置与原生绑定从同一依赖根解析。SQLite resolver 在 PySystem
初始化最先幂等安装，仅新布局启用，尊重宿主已经注册的程序集 resolver。
NativeAOT 根入口的加载边只依据本次真实文件搬移映射与有序 RPATH 解析结果重写；
未知、系统及外部加载边保留原值，不按文件 basename 搜索替代库。

OneFile 私有解包载荷保持已有平铺布局，使用显式构建状态选择该合同；公开产物仍为
一个可执行文件。运行时兼容旧平铺产品不依赖目录名字猜测或扫描整树。

## 发布与审计

候选目录先完成 apphost、全部依赖、marker 和清单，再沿既有 rename/rollback
事务安装。允许预先创建的空输出目录；拒绝未归属的非空目录。保留路径与 payload
冲突在搬移前报错，marker 使用 create-new，不能覆盖用户资源。重复构建可替换此前
完整的工具产物。Unix 入口名等于 `internal`（忽略 ASCII 大小写）时自动采用
`<原名>.bin`，Windows 的 `.exe` 入口天然独立；Python 模块名不受限制。

失败候选中已有同次 producer 的来源计划或审计报告时，保留到独占 sibling
`<输出目录>.build-<pid>-<序号>/attempt/`，错误中打印精确位置。该目录不属于公开应用。
整理诊断失败仍保留原候选并报告原位置；语义错误码、诊断内容与阶段顺序保持不变。

清单位于 `internal/ryupy-artifact.json`，所有发布路径相对公开产品根，marker 的
精确角色为 `application_layout`，JIT apphost 为 `application_launcher`。源码名、
声明、符号和 build_inputs 保留原值，旧 schema 的缺省字段保持缺省。
provider 报告以配置所在目录为路径锚点。普通文件和产物不新增内容哈希。

## 已验证范围

macOS arm64、固定 CPython 3.12.13，即时三流差分。修正 v2 的两套独立 C ABI 声明
各经过 JIT/NativeAOT，四个产品均删除源码、声明、原生库输入和构建树后搬迁执行，
导出的 `.pyi` 可解析，机器 ABI 与实际清单路径一致。`INTERNAL.py` 在两种模式下
通过相对输出、预建空目录、中文空格路径、自动 `.bin` 命名、删除输入和任意 cwd
空 PATH 启动。结构门覆盖严格 marker、保留名冲突、schema1 字段及根库存。

仓外证据：`~/.cache/ryupy/validation/internal-layout-cabi-v2/binding-stubs-p08vd2jq/`
及 `~/.cache/ryupy/validation/布局边界 7bcqkhxj/`。首次布局冻结还经过纯 CIL、实际
Cocoa、源码/native 共享归属、两种扩展接口及两套原生组件产品；联合门统一记录最终
冻结版本。其他平台加载器与 SDK 的实际交付由对应平台门验收。
