# ADR0123：显式模块实例基础与默认静态路径隔离

- 日期：2026-11-06。
- 状态：B1本地功能、正常完整门禁、真实PE/CIL及唯一有界性能测量完成；性能回退风险未关闭，未推送。
- 开发基线：458c968ff968a793285b0fa2eaf4f7ded5ffb8b6。

## 动机与切分

ADR0122已交付有限find_spec。原Markdown仍在native/legacy发现域失败；即便解决发现，
module_from_spec和exec_module也不能直接复用当前静态全局槽。复制模块属性只改变外观，
函数、类、闭包和生成器仍可能读取原模块；临时交换静态槽无法承受重入、异常与暂停。
因此先建立真实实例存储和显式执行环境，来源发现与完整html.parser另切处理。

B1只接受显式选中且没有新增Python函数/类的模块顶层。函数定义、lambda、类、推导式、
生成器及尚未接好的环境操作在编译前给Diagnostic，不生成看似能执行的半成品。
B2再补所有函数/方法/闭包/生成器归属与完整名字语义；B2通过后才开放Python loader执行。

## 选择

1. `--instance-module`只选择实际依赖图中的源码模块，不等同于`--include-module`。
   选择是构建配置，不根据find_spec的字符串、项目名或测试名猜测；未选模块保持原静态路径。
2. 一个不可变代码描述对应任意多个实例；实例只有一个真实PyDict作为公共全局存储。
   外部模块属性、真实`__dict__`和生成代码的名称读写共享它，不维护权威镜像。
3. 选中模块的真实ModuleMain接显式object环境参数；其中包括实例状态和本次执行捕获的
   builtins环境。名称读取先globals后builtins，写入只改globals；不依赖线程当前模块或缓存名称。
4. 元数据仅在创建实例时初始化。重复执行不清空字典、不重置元数据、不回滚先前写入。
   普通import失败则移除本次目标缓存，之后重试创建新实例；旧对象不会被新执行覆写。
5. 内部CLR宿主可创建和执行真实登记代码，供编译产物验收；不据此导出Python
   module_from_spec/create_module/exec_module，不称可写sys.modules或Markdown已通过。
6. B1暂为JIT-only，所有instance与AOT组合在driver前置诊断，不让缺来源能力拖到
   运行时注册失败。详细公开范围见[模块实例合同](../module-instances.md)。

## 分层与性能

前端明确记录能力，在类型收敛前使选中模块走动态名字语义。MIR显式标记执行种类、
state local和InstancePublic存储，后端不能按名字前缀猜测。选中公共全局不发静态字段，
不生成静态getter/setter；原普通模块分支继续保留。代码后端只读取MIR，不依赖HIR/ty。

函数对象布局和owner ABI本切不扩充，因为B1不允许新增函数。模块实例与状态采用弱键
冷关联，代码描述不强留所有实例；普通函数不查询该关联。默认builtin对象复用真实稳定
身份，默认字典为共享存储，按每次执行捕获环境引用而不是全局可变的ambient选择器。
这不等于零成本：注册表新能力、导入分支、实例字典、执行context和弱键关联都需测量。

真实PE的RuntimeError调用曾抛TypeError，暴露既有通用PyType异常构造未接入。
修复通过真实类型对象走已有异常factory，保CLR catch、raw args身份和延迟格式化，
不能把builtin名字硬编码回编译器。专有异常分支仍优先；未实现的Unicode构造、
多参OSError及特殊关键字协议明确NI。代价包括所有异常对象新增bool（尺寸依赖padding）、
旧消息读取新增分支、共享CallValue/IsCallable新增判定；不因主要接口ABI保留而忽略。

## CP事实及限制

新增CP3.12.13证据保留六个进程：原core两轮在13项前置检查后因改名失败；独立lifecycle
两轮各18项通过，boundary两轮各9项通过。原失败不改期望，也未将原core整体标为通过。
SourceFileLoader.exec_module实际按module.__name__检查loader名称，改名后先抛ImportError；
此协议不能偷换成B1内部代码执行限制。独立lifecycle保持名称匹配，已证明重复执行保键和
元数据对象、直接失败保写入/缓存、普通失败import重试后新旧模块/dict/容器独立。

完整源码和原流在私有验收材料module-instance-validation/cp-foundation-001。
这些仅为CP事实。B1另以实际编译同类源码并由宿主调用生成的ModuleMain(state)验收，
不能用C#手写正文测试代替，也不能删完整边界程序后半段换通过。

原CP执行环境源直接读写__builtins__，真实B1编译已在该处E0100拒绝，未执行宿主。
该完整源和原错误保留，不能记为上下文执行通过。实际可执行范围的外部字典变更由
独立源使用宿主注入事件触发，后续名字读取和结果写入仍由真实生成正文执行；两者
分开报告，不用前者CP结果冒充后者原源码支持。

## 实际验收与保留失败

- 正常完整make check最终通过：2337 C#、817 Rust、5项清单解析自测、453 CP基线、
  453 cases selftest、320编译e2e；137 ignored。11426份源文件前后逐份核对一致。
- 最终CLI d52be252…配Release运行时8c36fea5…，真实Python PE生命周期28项、
  外部字典事件/捕获builtins/同实例及跨实例重入/ImportGuard 18项全部通过；
  双实例、旧失败对象、普通失败重试与直接执行失败缓存/部分写入均覆盖。
- 两份完整多文件正例转正：ordinary_retry、dynamic_names。原完整注解版本仍E0100
  pending；直接环境名字原源亦真实E0100，不以宿主事件版本冒充该源支持。
- 先前动态异常调用真实红灯已通过同一原PE换runtime及最终CLI重编验证闭环。
  门禁暴露的integration测试字段遗漏、Python清单解析遗漏、未选程序诊断相位回归
  均修复后全量再验；低空间保护终止一次，原流未删除，未用定向通过替代完整门禁。
- 独立CIL逐方法/字段/引用/EH审计：默认新旧25600字节PE仅两字节COFF时间戳不同；
  46方法、100字段完全相同。runtime全部97个方法增删改归因完毕，无未解释变化。
  既有公开ABI保留，内部注册/异常布局和共享分支成本仍真实存在。
- 唯一12进程/204暖样本完成，全部不利保留。共同首次Main稳定新增344B；default
  plain_def_create两对+15.91%/+7.61%、resource+21.41%/+5.53%，禁tier
  plain_def_create −9.25%/+55.25%不能择优；未证明统计显著或无回退。
  新实例完整操作绝对成本与全部冷/暖结果见[完整报告](../module-instances-performance.md)。

## 原项目干净提交复验

干净功能提交0eeaf8b2a2972fe94a9571491fdc4fd4f7d1a7d4的原docopt0.6.2固定入口
CP/compile/JIT均exit0，16字节stdout逐字等原golden，JIT stderr空；原860字节
CP SyntaxWarning保留。原Markdown3.5.2 CP/compile exit0，CP stdout 77字节等golden；
JIT exit1、stdout空，375字节原stderr定位htmlparser.py:39的find_spec('html.parser')，
仍是NotImplementedError: find_spec(): legacy or native module registrations without source metadata are not implemented。
后续module_from_spec/exec_module未到达，仍不计Markdown通过。原63输入、62矩阵输入、
两份3187文件源树、20根次序、入口/golden/工具及干净HEAD/tree前后均未改变；不重跑旧20项目矩阵。

公开loader、完整函数归属、自定义模块类型、完整builtins反射、动态finder/path、可写模块
缓存与html.parser不在本切已验范围；不得由基础机制或定向测试推断其可用。
