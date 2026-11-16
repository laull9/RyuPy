# ADR-0222：按冻结需求保留原生 inspect 的启动入口

- 日期：2026-11-16
- 状态：实施中；先登记 pending 与真实 PE/宿主保护
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
