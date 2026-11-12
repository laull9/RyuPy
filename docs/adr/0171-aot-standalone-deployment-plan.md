# ADR0171：真实 standalone 应用的联合部署计划

- 日期：2026-11-12
- 状态：候选；公开 AOT 前端已验，hosted 原生交付与性能待验。

## 背景与目标

当前主线转向可搬迁原生应用、与 Nuitka 同源同输入的比较和 AOT 性能。
不以 JIT 导入数量代替产品交付，也不追逐 click 的下一语言缺口。
既有默认 AOT 计划被 resources/include/metadata 参数关闭，且自动位置计划会覆盖显式资源计划；
用户不能可靠组合已有能力。首个真实应用选择锁定 pyfiglet 1.0.2 原 CLI 与其真实 console entry point，
原样执行参数解析、字体读取、渲染和输出，不改第三方源码或缩为行数/导入探针。

## 决策

1. 保留唯一 SourceModule 来源图。资源与封闭 distribution metadata 是该图的补充声明，
   不能切换默认模块执行策略；默认无feature AOT采用 ExplicitOnly、自动Instance集合为空，
   模块仍为Legacy。pilot构建保自己的独立合同；不把它冒充默认CLI。
   JIT默认不变，显式 instance_modules、fixed-binding 的旧准入边界不放宽。
2. 动态目标继续用公开 `--include-module` 显式包含；metadata 存在不等于任意动态依赖闭包完备。
   `--distribution-metadata` 只提供封闭快照，不改运行时 sys.path。
3. 自动模块虚拟锚点与显式资源 claims 合并，先核同源文件和编译字节 hash；
   全部锚点参与资源目标冲突检查。资源不覆盖源码锚点、不复制 Python 源码、不猜部署路径。
4. 冻结时将资源消费者 source ID 投影为 emitted ID。缺失、越界或延期消费者明确拒绝，
   不静默删除消费者或把源码序号当发射序号。
5. schema4 延续同字段合同。`resources_included` 等于资源记录非空；
   `declared_resources_complete` 仅表示显式声明范围已完整处理，不承诺所有可能文件/插件/字体选择。
   自动位置且无资源声明仍为 false；显式空声明可为 true，但资源包含仍为 false。
6. 原子发布、独占 staging、输入变化检查、失败保旧产物沿原流程；不生成假 manifest/ELF，
   编译拒绝不得回退 managed 或静默跳过不支持模块。

## 产品验收

复用 `tests/aot/standalone_app` 锁定源码、egg-info、553 个完整字体数据文件及逐文件 hash。
公开命令组合 `build --aot --resources`、`--distribution-metadata` 与显式 includes；
原 `__main__.py` 和 console `.load()` 入口均须完整输出 standard/slant 字形。
仅部署 standard 的早期探针不是完整字体交付；完整字体枚举也不代表 pyfiglet 所有选项兼容。
搬迁后在无 Python/.NET SDK、无源码、禁网络环境实际运行，并检查缺资源/篡改/路径冲突负域。
独立发射前 oracle、实际 PE 与 native 格式/依赖相互核验，不能从产物 manifest 反造预期。

Nuitka 对照固定同源码、输入、依赖、字体及 metadata 部署语义；standalone 对 standalone，
分别计冷启动、热吞吐、内存、交付大小及冷/暖构建成本，不预设 RyuPy 更快。
本轮不承诺任意 onefile、跨编译、全平台或全 Nuitka 兼容。

## 当前证据与限制

- 原 CLI 与 console 入口 CP 完整字形一致。最初 managed 均执行到真实 stdout.detach 缺口；
  标准流修复独立验收，不能将 driver 前端通过算应用成功。
- 两条公开 `--aot` 命令携完整字体与 metadata 实际通过 graph/link/emit/output；
  四模块均 compiled，未因组合退回显式实例 E0100。
- 本地 SDK restore 成功，但 ComputeManagedAssemblies 报 MSB4216/4027；未产 ELF。
  不能沿先前静态推测把失败归因为缺 ILCompiler。跨 runtime 重建的两次尝试分别锁 DLL hash，
  不属于同冻结 Native 成功证据；后续使用 hosted 工具链验收。
- 局部 driver 与 clippy 已通过；一次全 driver 测试因磁盘耗尽导致 rust-lld SIGBUS，
  该次不是通过。缓存清理不改变其原始失败记录，完整回归待统一执行。
- hosted 原生搬迁、完整联合回归及平台性能仍待验，不计新增项目兼容完成。

## 默认构建纠偏

首个hosted默认release已实际产ELF和553资源，但旧实现仍输出schema2/explicit-only，
专用schema4验收正确拒绝。原因是本地CLI曾显式启pilot、测试oracle又硬编码Pilot，
不能以本地成功代替默认产品。现将部署规划与执行策略解耦；oracle改用真实Session默认值。
旧具名oracle测试拆分造成exact路径零匹配，恢复原稳定wrapper并增加零/重复/异名/ignored负测。
以上是修复中的边界记录，不能把失败的hosted验收改写成全绿或已搬迁完成。
