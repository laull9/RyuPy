# ADR0111：真实 Generic 根及有限 TypeVar 别名

- 日期：2026-11-05
- 状态：有限支持域已验收；参数化继承仍为下一切

## 动机与切分

原 Markdown3.5.2 在 TypeVar 基础提交后仍于 Registry(Generic[_T]) 基类表达式
报 E0100，JIT 未启动。先完成可单独验收的真实根及订阅协议，再接参数化基类和类体
终结；不将 Generic 擦为 object，不把导入成功或局部 API 当作 Markdown 通过。
旧 m3_7_typing_generic 保持 pending，不用减少断言扩大支持声明。

本切只改运行时公开对象协议，不改 HIR/MIR/CIL 或 ABI。下一切负责基类表达式只求值
一次、原始 __orig_bases__、参数推导和类体失败次序；用户类再订阅、替换、IO 祖先迁移
及完整 descriptor/元类另验。

## 根身份与实例布局

Generic 是 canonical PyClass，真实 type 固定点、object 单父链；使用独立根标记，
不占用 IsNativeRuntimeClass，也不将将来的普通后代送入根特殊构造。根 bases/mro 是
两个稳定缓存 tuple，mro() 返回新 list。不存在的 __slots__/__parameters__ 不伪造。

Generic()、Generic[T]() 和 object.__new__(Generic) 返回真实根实例，无可写实例字典，
不能把失败字段写入悄悄分配 slot。参数错误、字段错误和公共 TypeName 保原生限定名
typing.Generic；这个特殊名字不影响普通同名用户类。根协议改写可先存储，未知 new/
init/getattribute/hash 等在对应实际消费时明确 NI，不提前阻断无关动作。

根 module 可写，普通字符串影响显示，builtins 或非字符串按 CP 原生限定名回退。
alias repr 则另行消费当前根 module；具有未知比较/格式化回调的值在显示时 NI，不能
以 __str__ 冒充 CP 的比较和 __format__ 次序。根使用字符串子类 module 的显示暂 NI。
name/qualname 修改、完整 mappingproxy 和特殊 descriptor 的显式绑定暂不开放。

## 有限别名与缓存

只接受现有真实 TypeVar，身份去重，保参数顺序；空列表、重复变量和已知非法参数给
CP 错误，未知 hash/eq 域明确 NI。别名使用与既有 List[T] 一致的 _GenericAlias
Python 类型身份，但专用缓存包含根语义及单值/显式 tuple 输入形状，不复用旧按名字
键控的 alias cache。128 项 LRU 保留等值但身份不同的调用形状及同名变量的独立身份。

__args__ 与 __parameters__ 内容相同但为两个不同的稳定 tuple；get_args 返回 args
原对象，输入 tuple 不偷用为 alias args。origin 是真实 canonical 根，等值/hash 依据
变量身份与参数顺序，不承诺具体 CP hash 数值。alias 调用后根空布局阻止 orig_class
写入；未知 setattr/descriptor 不被吞成一般 AttributeError。

__mro_entries__ 当前只验收 exact tuple 内唯一等值 Generic alias，保非同一对象的
等值索引规则；其它合法组合明确 NI。直接显式 class_getitem、参数变量种类扩展、alias
核心 metadata 改写/普通属性写入与再订阅均不扩展；不能把尚未实现的 CP 合法域称非法。

## 可写导出与旧消费者

canonical 根独立于可写 typing 导出。保存旧根订阅的 cache hit 仍返回旧 alias；miss
和必不命中的非法输入才消费当前 Generic 导出。删除/改绑导出的未知情况在真实消费点
NI，不在每次导入、alias 元数据读取或已命中 cache 上抢先失败。

NamedTuple 的功能式非空注解与类体终结、TypeVar bound、TypedDict 每个非空字段和
旧 TypeArgument 按各自 CP 相位迁移已有导出守卫。空功能式、空 TypedDict 及 TypeVar
无 bound 不额外消费；更早 binder/default 错误仍获胜。真实 Generic 根和根 alias 不因
成为真实 class 就被当合法 bound/注解放行。get_origin 真 alias 先返回，普通根与特殊
form 后续才消费导出；Any/Optional 不能误走提前分支。

IO/BinaryIO/TextIO 当前祖先尚未迁移，对 Generic 关系检查明确 NI，tuple 先匹配其它
合法类型仍短路；不把部分真实根实现扩展为全部 typing 家族的继承兼容。

## 原始失败与验收安排

五组主合同先用固定 CP3.12.13 双跑冻结，旧运行时五组均缺 Generic；首候选四过一败。
唯一 root 失败已独立证明为既有前方 import 和后文同名 class 绑定顺序缺陷：去掉后文
同名定义时根身份正确，加回后 JIT 前方读取为 None。完整原源、oracle 和失败保留为
unsupported_import_class_shadow pending。主合同改用 ImportedGeneric 仍保留后文普通
class Generic 与根不同身份的断言，不声称修复绑定顺序。

第二候选五组原源 CP/compile/JIT 严格通过，12 项 C# 专项通过。独审支持域原源另接
第六主应用，复用已冻 CP oracle；复杂边界探针的 CP 真实值与 Ryu 的 NI 分列，不冒
逐字差分通过。九项新 pending 保留
alias metadata、导出删除、真实继承、IO 关系、复杂 mro_entries、参数变量种类、根
hook、根 metadata 及同名导入绑定边界。审查、最终门禁、性能与项目复测结论另列，
不以专项替代全量门禁，不累计不同提交的项目通过率。

## 性能与恢复约束

普通对象热路径仅新增根判断，但冷调用链及新 alias 缓存可能增加分配和指令；不得只凭
源码分析宣称无回退。以同一生成 CIL/host 比较八条旧公共路径，六条新能力只报绝对
成本。默认与固定 tiering 诊断各一轮 ABBA，保全部不利原样本，旧性能风险不因新功能
通过而关闭。新 miss 路径用预先构造的129变量轮换128缓存，独立验证真实淘汰。

恢复包保旧源、原始失败、性能和恢复证据，严格低于32 MiB；按不可变对象去重及单层
可重建差分压缩，不删证据换容量。全逻辑文件和历史源快照须实际恢复核对后才替换上次
已验证备份。磁盘清理限已授权、逐文件可重建且无占用的重复物化副本。

## 有界性能结果

两配置各唯一 ABBA 共440/440样本，全部语义checksum和工具/CIL/host身份核验通过，
未删不利样本或追加重跑。八旧路径分配均不变；默认普通构造候选/基线1.062，约+6.2%
时间风险保留。其它汇总比值虽低于1，但基线两进程存在明显漂移，例如字段112→22ns，
不能宣称广泛提速或可靠归因。禁tier构造0.982只作诊断，不消除默认风险。

六条新路径只报完整函数成本，默认两候选进程中位数分别为：根构造196/160ns及72B，
alias构造518/334ns及104B，cache hit565/562ns及304B，真实miss轮换1789/1386ns及
600B，parameters读取47/42ns及0B，mro_entries747/720ns及640B。包装、参数数组及
缓存key都有成本，不能把参数读取0B外推全部Generic。旧TypeVar、属性、partition
性能风险继续保留。

## 正常完整门禁

2026-11-05 11:04:40Z至11:06:39Z，唯一正常 make check exit0：2084项C#、
273项CP基线、222项e2e通过，55 ignored逐名核对为原46不变加九项边界。六主转正
前逐一CP/compile/JIT通过，转正后正常门禁再次全部通过。门禁前后运行时SHA256为
364e80269b7f94874adc63e2db66a67c43c7a43ec77373d5c322a3d130a72434。
门禁冻结的10728份文件逐个重新读取核对内容/size/执行位一致；之后只补本文档结果。

## 干净功能提交原项目复测

功能提交 d78ed61b31183af5addbc50a9cc8da00de36161d（tree
11d9a90f06fb2c689dfe5649f9ab0687a0827973）上，原docopt0.6.2唯一
CP/compile/JIT窗口均exit0，CP/JIT stdout均等于原16字节golden，JIT stderr空；
CP四条上游非法转义SyntaxWarning完整保留。原Markdown3.5.2的CP exit0且77字节
stdout等于原golden，compile exit1，首诊断仍util.py:272:16的Generic[_T]基类
E0100，JIT未运行。没有修改入口、桥接或将根支持当继承成功，也未重跑完整20项矩阵。

复用原20根CP顺序，编译仅相应原包根。3187份原始包源与3187份已冻结源逐个重读
校验，前后零增删改；原归档、入口、golden、CLI/runtime以及HEAD/tree/clean均不变。
下一切继续真实参数化基类，不由本轮局部完成停止兼容性工作。
