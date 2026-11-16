# ADR-0222：按冻结需求保留原生 inspect 的启动入口

- 日期：2026-11-16
- 状态：实现与定向产品验证完成；正式独占性能由主代理统一验收
- 接续：ADR-0219、ADR-0218

## 依据与决定

第三轮正式产品和外置唯一行控制组证明：完整 MIR/CIL、65个热符号的 ARM64 指令及
重定位目标相同；无条件 inspect 冷登记却令 NativeAOT 多保留704个相关符号和约47KB代码。
12轮六排列控制中，移除该登记的 generators 中位数比正式v2低4.00%，calls低1.02%。
该证据定位到冷登记引入的可达图与布局，尚未细分缓存、对齐或接口元数据成本。
产品不使用 nop、基准专属对齐或运行期反射加载。

仅新增静态 `PySystem.InitializeCompiledBase(string, string[])`。它复用原sys/argv初始化，
登记除inspect以外的原生模块。旧 `PySystem.Initialize` 与 `PyNativeModules.Register`
继续全登记；先基础启动再调用旧API仍须补齐inspect，重复全登记不重置已导入模块。
inspect的登记状态由独立冷holder持有，仅全登记路径引用；基础入口不可间接引用其工厂。
共享prefix/suffix冷函数令首次完整登记保持functools→inspect→operator原有顺序；
登记中重入仍沿旧短路，基础完成后升级仅追加inspect一次，不重置其它模块或inspect状态。

MIR `LinkedProgram` 携带有限的 `NativeModuleRequirements { inspect: bool }`。
原始MIR/link默认true，driver只有明确完成保守扫描后才关闭。codegen仅选择同参数的
两种初始化MethodRef，入口EH、参数和异常边界保持。不给HIR增加运行时环境读取或授权字段。

## 冻结与保守规则

driver扫描闭图中的全部已选源码，包括传递导入、显式include及compiled optional候选。
以下任何语法出现即保留inspect，不做别名、遮蔽、常量求值或任意值流分析：

- 任意层级import/from inspect、importlib或builtins。
- Name/Attribute引用 `__import__`、`__builtins__`、`__globals__`。
- Name引用globals、locals、vars、getattr、eval、exec。
- Attribute读取 `__dict__` 或modules，覆盖sys别名及计算键取得导入器。
- 任意CPython provider，覆盖显式inspect profile及未分析的外部消费者。

本地include及其依赖进入同一图；现有显式include必须找到源码的诊断合同保持。
dump沿用同一扫描；其单模块图存在导入时保守保留，避免漏掉未搜索的传递需求。
原生Runtime实现若间接依赖inspect，须额外列入已知保留规则；该核查不扩大模块catalog。
误留只影响裁剪；未知动态路径不能因优化丢失原功能。

## 验收

先以pending手写输入锁住静态from/别名、传递源码、显式include和动态导入器别名。
同次固定CPython3.12.13核退出与完整双流；原生inspect五组及真实CP消费者保留。
实际PE检查基础/完整初始化选择、相同参数/EH和旧MIR默认完整合同。
独立进程宿主测试验证基础→旧Initialize/Register升级、模块/已改属性身份不被重置。
真实NativeAOT删除输入和整个构建目录后中文空格搬迁，并检查无inspect需求时相关符号被裁去。
所有产物仓库外；独占性能窗口另行协调，保留原26项与全部控制样本。

## 本次实际验证

- driver全lib：299通过、5既有忽略；默认MIR436/候选MIR437通过、各4既有忽略；
  codegen72通过、3既有忽略，含序列化PE的初始化目标/参数/EH逐指令保护。
- 新专用产品门4/4通过：静态from/传递、保存动态导入器、显式include、旧宿主6种
  独立新进程路径及原native inspect五组。裸`__import__`环境名仍保原HIR诊断；
  成功别名用例从真实函数builtins字典取导入器，不扩原有准入。
- 三组NativeAOT先查当次真实ILC对象：普通源码没有PyInspect符号，静态/动态需求均保留。
  然后删除输入及整个build目录，中文空格路径搬迁运行，同次CPython3.12.13双流/退出一致。
- Runtime相关12/12，包括原7个PE/provider ABI/inspect；实际Runtime启动ABI增量另1/1，
  旧Initialize及新CompiledBase精确同签名且无额外重载。原C3协程21/21含AOT7继续通过。
- fresh CLI/新compat shim的bridge、attrs、Pydantic与FastAPI（Python3.13.3）四组真实
  消费者均同次CP预期、JIT/AOT删除input/build后搬迁通过，host关闭统计及inspect生命周期
  保持。首次SDK空双流exit1未复现；保留目录复建成功，终轮显式禁MSBuild server复用。
  atexit与owner两组桥生命周期也分别通过JIT/AOT删除输入/build后的真实搬迁。
- 定向clippy（含native-coroutines/all-targets，-D warnings）、fmt与diff检查通过。

这些是功能与裁图证据；性能数值沿用前面的外置归因实验，正式修复后26项另行独占采集。
