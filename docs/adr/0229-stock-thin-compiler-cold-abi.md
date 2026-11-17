# ADR-0229：真实薄函数编译的中性冷 ABI

- 日期：2026-11-17。
- 状态：显式 Rust 竖切和正常结束已接；两版真实 CLI 的 JIT/NativeAOT/OneFile 搬迁 24/24 通过，入口存储与性能继续收敛。
- 接续：[ADR-0226](0226-stock-thin-function-product-contract.md)、
  [ADR-0227](0227-stock-thin-function-metadata-probe.md)。
- 上游关闭合同与证据由 ADR-0228 独立登记，禁止用源码手动删函数替代正常退出。

## 1. 静态 body 与环境分离

生成程序集引用 Runtime 中性类型，不依赖 CP 指针、GIL 或 provider 私有类型：

```csharp
public delegate object? PyStockThinBody(IPyStockThinEnvironment environment, object? argument);
public interface IPyStockThinEnvironment { object? LoadGlobal(string name); }
```

provider 独立取得、验证、释放活动 lease；生成体只借用读名能力，不获得 Dispose、owned
指针或模块实例。core 直接存这个无捕获静态委托，禁止包装旧 PyFunctionOwner、Context 或
另一个捕获 env 的 lambda。既有内部 ABI 与新编译 ABI 共用静态目标/单播检查和弱 H 缓存。

`PyStockThinDefinition` 为 sealed 只读来源类，五个构造参数为四个 string 和 Int32 行号：
entryId、module、name、sourcePath、sourceLine。只存字符串/整数，不持业务环境。
这是一项有意新增的 public 冷 ABI；既有 provider 协议与函数布局不变。

## 2. 冷创建与名字读取

可选 `IPyStockThinFunctionProvider.CreateStockThinFunction(globals, definition, body)` 负责
真实 F/GC owner。`PyStockThinFunctions.Create` 只调用当前显式 provider；缺少能力明确
失败，禁止创建旧 PyFunction 替身。

`PyModuleInstances.CreateStockThinFunction(context, definition, body)` 在定义点只借用
`Context(context).State.View` 调用上述 factory；它不捕获或缓存模块 context。真实 F 持 D
及定义时 builtins，K 不持 D。`PyStockThinFunctions.LoadGlobal(object lease, string name)`
映射隐藏动态参数，实际查名语义由 provider 的活动 lease 实现。
编译 provider 只接受本 Host 的真实精确 dict，拒绝把 CLR PyDict 转成另一份环境副本。

Bootstrap.StartStockThin(config,args) 只供显式编译资格发射；安装旧宿主后启用候选，
启用失败回滚 Stop，双失败同时保留两项异常。默认 Start 不经过这个分支。

## 3. 首个编译资格

显式 `--experimental-stock-thin-functions`，默认关闭。driver 在完整图和 provider
冻结后要求只有编译入口 __main__，入口未委派、非 fixed binding。复用真实 stock entry
存储，但不放宽原自动资格。首切仅接受已验的精确 stock 3.12.13 / 3.13.3；
其它版本在编译期 Diagnostic 拒止，默认 provider 3.9–3.15 合同继续保留。

首切只接顶层同步 `def f(x)`：x 为一个位置/关键字参数，无 positional-only、kw-only、
variadic、注解、默认值、装饰器、type params 或业务 closure；body 恰一条 return Name。
参数 x 直接返回；其它准入名字必须走真实 F 的 globals/builtins 活查，不能发射旧静态
函数常量或 Runtime builtin 常量。超合同必须 Diagnostic，禁止退回旧 MakeFunction。首切对语言常量 __debug__ 明确诊断，
不能把它降成可被 globals 遮蔽的普通名字。

HIR 将实际 FuncId 与执行资格绑定，MIR 使用独立创建操作和 StockThinLease 隐藏状态源。
body 仍为真实 HIR/MIR 降级后的静态 CIL 方法；codegen 仅按 MIR 发射 typed static adapter、
无捕获委托和 factory 调用。旧 FunctionModule/Metadata 尾链只接受 PyFunction，不能用于 H。
原 context 的来源/trace helper 也不能接收 StockThinLease，应使用冻结来源或单独中性能力。

正常程序应保留 f 在 __main__ 后仍可退出，atexit 仍能调用 CIL 并读取原 D。现行内部 Host
Dispose 会拒绝活 owner；编译入口改走 ADR-0228 的专用程序权限与结束合同。
独立产品驱动显式启用候选，普通 e2e 输入保留 pending，默认路径不自动晋升。

## 4. 已验冷 ABI

- Runtime 测试验证实际字典/定义/静态委托同身份、Instance facade 不捕获 context、缺少
  能力/异常无替身、活环境读名与原异常身份。既有 provider 精确签名保护继续通过。
- provider core 共七项协议测试通过，新 static public ABI 仍拒绝捕获/多播目标。
- Host `--neutral` 两版 stock 3.12.13/3.13.3 各 JIT/NativeAOT 通过；provider 从新 public
  factory 创建 read/echo，CIL 体用中性 LoadGlobal，CP binder/真实 F 元数据及 CLR 原盒身份
  保留。每组合 2 个真实 owner 析构、4 次 native call，roots/owned/weak-core/queue 全零。
- Runtime 发布 DLL、provider/Runner 与 AOT 分析告警视为错误；AOT Main 检查动态代码不可用。
  产物仓外清理，无原始报告入库；尚未将这些手写 C# 体称为 Rust 编译业务体成绩。

后续验收采用真实 Python 输入与 Rust CLI 生成物，检查 HIR/MIR/PE 中的静态 body、委托、
lease 读名和 factory，随后两版 JIT/NativeAOT/搬迁及原二十项目与真实应用回归。


## 5. 编译与审查收口进展

显式资格已贯穿 CLI/driver/HIR/MIR/CIL；8 项初始定向测试、fmt 与全目标 clippy 通过。
新 CLI 已对原 identity.py 真实生成 DLL：输出两行 True，随后因模块保留 F 被旧关闭门
拒绝，exit=1；这个完整失败保留，正例继续 pending。正常模块结束由 ADR-0228 处理。

独立审查发现的两项 P2 已修：dump 在资格判定前按本次输入冻结 provider 并核验实际图，
CIL dump 也显示 StartStockThin；__debug__ 以明确 Diagnostic 拒止。真实 CLI 的 ast/hir/
types/mir/cil 五阶段均退出 0，额外源码模块明确拒绝；CP oracle 的 __debug__ 为 True，
候选不读取被注入的 False。完整三流/退出码在仓外保存，原输入与临时产物已清理。

canonical builtins 由新增可选 `IPyCompiledBuiltinsProvider` 冷能力提供，在入口 Execute
捕获前写入真实 CP 字典。顶层及真实 F 共享原 builtins，globals/import 规范函数守卫
只比较受 owned 保护的原对象；用户同名替换不会取得守卫权限。普通入口未启用时继续
使用既有 managed 环境。新增机制和原导入守卫共 10 项定向测试通过，产品运行待关闭接线合流。

冷 ABI 增量的 Runtime 全套固定 CPython 检查为 3422 pass、3 既有 skip；provider 43 pass。
首个 Runtime 单独命令缺少 PYTHON 导致两项 textwrap oracle 环境失败，补固定解释器后
完整重跑通过，失败日志保留。全部历史保护继续有效，最终 Rust/产品/性能门尚待生命周期合流。

2026-11-17 合流后完整 make check exit=0：3424 Runtime pass、3 skip、503 e2e pass、297 pending，
全部 Rust/fmt/clippy 通过。真实 Python 输入的参数/全局/builtins/函数身份及 atexit 四门，
两版三发布形态共 24/24 通过；删除输入和 venv 后搬迁执行，原生 owner/core/ledger/队列及
post-finalize DECREF 全零。固定二十项目用冻结 CLI 另行回归，provider 成绩继续单列。

入口强 owned 登记的 RSS 诊断暴露长循环保留临时标量的问题；原始负面数据保留在仓外。
下一切片采用入口 token 弱登记、短 callback 强 scope、Release 精确退账和终态强快照。
当前没有吞吐或内存性能晋升结论。
