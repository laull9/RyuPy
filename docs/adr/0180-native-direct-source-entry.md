# ADR0180：普通源码 AOT 直接进入 SDK 编译主入口

状态：整合候选，2026-11-13。原独立候选已有限验收；与最新 dev 的多导出 C ABI、资源组合和上下文协议合并后的验证待完成，不把旧候选结果当作合并树通过。

## 动机与边界

真实 Linux consumer SDK 计时（CI 43353629191）显示 Csc 首次/重复分别约 1.51/1.46 秒，ILC 仍是主要成本。现有薄宿主仅转发 `RyuPy.Generated.Module.Main(args)`，初始化、异常、退出码和退出刷新均已在生成的 Main 内。删除重复 C# 编译比未经完整身份验证的构建缓存更容易保证正确性。

源码 AOT 必须发射带真实入口 token 的 executable PE。仅有 Main token 而仍带 DLL 标志会令 ILC 报 `No entrypoint module`，失败 CI 43355312850 保留。修正发射后 CI 43358802500 两个独立 SDK 根编译/运行成功；CI 43360854985 的后续探针已验证安全 SDK 程序集名不同于输入内部程序集名、中文及 `$();% &` 输入路径。它们是内部探针，不是完成产品迁移的证明。

## 设计

- 普通源码的本轮 ManifestContext 且 Linux/macOS：维持安全独占 ASCII 宿主名称，通过公开 `BeforeTargets="CoreCompile"` 把本次已校验 managed 字节复制到 `@(IntermediateAssembly)`；使用 `SkipCompilerExecution`，不再生成 Program.cs 或把输入程序集作为 Reference。
- 保留 Runtime 精确 Reference、SDK 标准 restore/引用解析/ILC/native 链、所有裁剪警告失败规则、SDK及优化偏好收据、资源 staging、输入前后摘要和产物事务。不得手写替代 SDK 内部 native 目标链。
- Windows 保留原薄宿主以保留 SDK 默认 Win32 manifest；静态绑定发射 library PE、无 context 入口也保留薄宿主。不能因 Unix 成功扩展到任意 managed 输入。
- 不改默认优化偏好，不新增缓存，不清除构建历史，不改变 Python 语义或固定项目兼容率。

## 验收

局部 XML 合同不能代替真实 ILC 使用证明。验收需要核本次唯一 ILC 主输入与本轮 managed 摘要，参数/非零退出/字符串 SystemExit/异常栈/退出回调刷新，原位与搬迁运行；另保留 construction、静态绑定和三平台 pyfiglet 字体/metadata 真实部署、Linux consumer 三偏好收据及分离符号恢复。

原独立候选的有限跨平台结果见下节；Mac/Windows 宿主隔离仍不是无 SDK 干净机证明。原型计时受构建顺序与包缓存影响，不把减去 Csc 或一次原型 wall time 报为稳定产品性能提升。

首轮产品 `639c2643` 的 construction Linux/Windows 已产出真实 Native，但旧验收器将全部 compiler options 误当布尔值而拒绝当前默认的 `instance_modules=[]` 与 `module_environment_policy="explicit-only"`。保留 CI 43362463721 失败原件，修为七个默认字段逐类型/值精确检查，不能忽略未知字段。macos-14 的两项任务未执行步骤即取消，后续改在已用于项目门的 macos-15 验证；取消不计通过。


## 原独立候选的已验证结果与整合边界

产品变更在 `639c2643`，后续 `063469ef` 只修验收合同和平台 runner；下列各门的产品代码一致：

- CI [43363656557](https://github.com/laull9/RyuPy/actions/runs/43363656557)：Linux consumer、default/size/speed 六次构建、288 份宿主原流和 24 份禁网容器原流、优化收据与分离符号恢复通过。
- CI [43363656533](https://github.com/laull9/RyuPy/actions/runs/43363656533)：Linux 真实项目资源/metadata 与搬迁部署通过。
- CI [43363658401](https://github.com/laull9/RyuPy/actions/runs/43363658401)：macOS/Windows 真实项目与入口协议通过。三平台入口共 36 份原位/搬迁运行原流已独审；Linux/macOS 的实际 ILC 唯一主输入与本轮 managed 字节一致，Windows 保留薄宿主。
- CI [43363656513](https://github.com/laull9/RyuPy/actions/runs/43363656513)：原单导出 C ABI 绑定三平台回归通过；这不是用户新增多导出/资源组合的验收。
- 最后 CI [43365134081](https://github.com/laull9/RyuPy/actions/runs/43365134081)，测试修复提交 `baf6f205`：construction 三平台全部通过。前序保留 SDK cwd 和路径别名的失败记录；改为 owned cwd 固定 SDK，并按 canonical 目录身份校验测试路径，不放宽版本或行为合同。

这里不声明稳定构建提速、Nuitka 总体性能领先或完整兼容。整合以最新用户 dev 为基线，须重验其 `binding_multi`、`binding_resources`、`resources_as_file` 和上下文协议，以及既有 direct/thin 两路径；在合并树结果完成前保持候选状态。

## dev 整合验收（2026-11-13）

独立分支 `integration/dev-aot-native-20261113` 以用户 dev `cd678640` 为第一父、原候选 `baf6f205` 为第二父真实合并，保留用户六提交和原测试。修复提交 `2fab5424` 的产品树经完整 `make check`（C# 3100 通过/6 跳过，编译 e2e 458 通过/286 既有忽略）及 CI [43368686854](https://github.com/laull9/RyuPy/actions/runs/43368686854) 三平台四组通过。直接入口共36次原位/搬迁原流，C ABI v2 三导出/同符号别名，schema5/6资源组合、真实失败保旧包、as_file CP/JIT/搬迁Native均已独审。

首轮 CI43367296487 的 Windows 两失败原件保留：共享词法不支持真实 extended ProcessPath，以及验收器用路径文本替代目录身份；修复与边界见[ADR0185](0185-windows-extended-filesystem-paths.md)。本次没有修改dev/main，没有增加直接入口平台范围；只证明已安装SDK宿主，不是无SDK干净机或Nuitka兼容/性能结论。
