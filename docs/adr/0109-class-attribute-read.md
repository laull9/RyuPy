# ADR0109：保用户覆盖的默认类身份读取

- 日期：2026-11-05
- 状态：有限读取、独立CP/JIT、正常完整门禁及原docopt入口通过；性能风险未排除
- 前置：ADR0108 的干净功能提交在原 docopt 入口报 Argument 缺 __class__

## 范围和真实身份

这是一项通用属性读取，不识别 docopt、Argument 或类名。普通实例的默认 __class__
返回既有 instance.Class 引用，不新建类型，不用 CLR 宿主类型或短名猜测身份。
同名的两次类定义必须保持不同对象；同类不同实例共享真实类身份。
普通类对象的 __class__ 是实际元类，先于它自身命名空间的同名项；复用 TypeOf，
保留 Enum 等现有专用元数据入口。另补 PyType 载体的 type 固定点，不能由此声称
所有原生类型和实例已经支持完整反射。

## 查找次序

先保留 typing IO、str/list/dict 和 Enum 的既有布局入口。在普通实例读取中，仅当
名字为 __class__ 且用户类链没有任何同名项时，应用 object 根数据 descriptor 的
有限语义，在实例字段和 __getattr__ 之前返回真实类。存在同名项时沿用正常查找，
不能把 None 当作不存在，不能跳过 property 或提前调用方法。
最近类项决定 descriptor 优先级：子类普通值可以遮蔽父类 property；没有用户遮蔽
时根类身份压过实例同名字段。有用户普通值或非数据 descriptor 时实例字段仍可优先。

类修改和新增槽位已有 GlobalVersion 失效机制。PIC 的冷填充复用现有类值条目，
ClassValue 保存真实类，Slot=-1 表示不可被同名字段遮蔽；不增加 PIC 热命中分支。
若只补慢路末尾回退，同名字段会先被返回并缓存，这是不可接受的错误。
这只是结构设计，不能代替性能测量，也不能宣布零成本。

## 明确边界

不实现 __class__ 赋值/删除或动态换类；测试通过运行时直接布置同名字段，只证明读取
优先级，不冒充 Python 换类。Python 的实例 __dict__ 布置路径单列 pending。
自定义 __getattribute__、一般 descriptor、property 抛 AttributeError 后 __getattr__
回退、显式 root descriptor、super 反射、自定义元类和多重继承保持各自边界。
真实 str/list/dict 派生上的用户 __class__ 覆盖仍受既有守卫限制，不趁本轮撤掉。
普通实例、PyClass 和 PyType 以外载体的完整反射不得由本切通过推定。

## 验证安排

独立 CP3.12.13 合同包含12份源、130条观察，发现后冻结，再逐字重跑12/12通过。
它只说明 CP 行为，不计作 RyuPy 支持。专项应覆盖身份、覆盖/继承、getter副作用与
错误传播、PIC重复读取及类版本失效；先保基线失败，候选单测和实际CP/JIT通过后转正。
性能用相同生成 CIL/host 对照已有路径，新默认能力仅报告绝对成本；保全部不利样本。
ADR0108 的旧性能风险未关闭。正常完整门禁后，先本地提交，再以原 docopt 源/入口/
golden 复测；不把新增 API 通过当作项目通过。恢复集低于32 MiB并独立实际恢复。

## 首轮证据和拆分边界

基线 bac46ec 的三个主源 CP 通过；lookup/errors 编译后 JIT 在类对象元类断言失败，
identity 原完整源先遇到同模块重复类名 E0102，未启动 JIT。原源和 golden 完整保留为
duplicate_name_pending，不用把不同名字的类当成同名定义；运行时 C# 直接检验同名身份。
候选首轮 identity/lookup 严格 CP/compile/JIT 通过，errors 在 getter ValueError 8次和
AttributeError 2次均正确后，遇到旧 property() 零参数构造不支持。该完整源另存
property_constructor_pending；主源仅改 CP 等价的 property(None)，保留全部读取断言。
不能把未到达的无 getter 读取当成已测失败，也不顺带扩大属性构造 API。
候选三处生产文件不增加 RuntimeFn、CIL编译路径或新的 PIC 热命中分支。新增11项
C#专项通过，含暖缓存后闭包状态变更而类版本不变时的原异常/次数，以及raw null项。

候选主三源严格 CP/compile/JIT 全过后转正，原三份边界保持 pending。独立探针另核
混合同调用点、暖getter后异常恢复、类与实例覆盖区别和字段/普通类版本变化：PIC开/关
各唯一JIT均exit0、空stderr，stdout81字节与冻结CP逐字一致；源/golden/工具前后hash不变。

## 有界性能观察

固定host/CIL的默认ABBA保留300原样本，语义/hash均通过；新默认PIC直接约37.6ns/0B，
128次循环约2860ns/0B、继承约39.1ns/0B，getattr约371ns/72B，类读取约236–244ns/24B。
这些是完整Python函数的成本，新能力没有旧版正确行为基线，不宣称提速，也不据24B推定新建类型。
旧type两邻接对约+7.8%/+14.1%，覆盖property约+50.6%/+5.9%、覆盖method约+50.3%/+3.9%，
field_loop约+55.4%/+0.15%。进程/tiering漂移明显，既不能可靠归因也不能宣称无回退；
保全部不利值并按有界诊断要求停止，本切不关闭ADR0108旧风险。完整方法与原样本另存。

## 正常完整门禁与原项目验收

2026-11-05 08:33:32Z 至08:35:36Z，首轮正常 make check exit0：2056项C#、246项
CP基线、211项e2e通过；39 ignored逐名核对为原36不变加本切3个明确边界。
候选Release运行时前后SHA256均为
12816ee60de14bafcf1cfc83829f0cc43c3b50b9c55344882be1d4485c6ed875。

干净功能提交 e0a9ac1014755150b1476c48d5ca558dee7a7b24，tree
063e379daa72c0bdaf3c644d5fab6f186317c734 上，原 docopt0.6.2 固定入口唯一
CP/compile/JIT均exit0。CP及JIT stdout16字节，逐字等于原golden，SHA256
04b5ae134f7bd7fc7d01cd651c2cef60c887fc130dcc5518e90bf63eb414f6bb；JIT stderr空。
CP仍有四条上游SyntaxWarning（804字节含本轮冻结路径），编译stderr90字节为产物通知，
不能误写全部阶段stderr空。28份官方源、原入口/golden、冻结复制、工具和HEAD/tree/clean
前后核验一致；没有重试或改项目源码。约1.05秒只用于预算记录，不作为性能结果。

这证明本候选Linux JIT的原固定入口通过，不代表docopt完整库、AOT、其他平台或重跑
其它19项目。e066238默认9/20、字体选项10/20完整矩阵仍是历史同提交结果，不直接相加。
