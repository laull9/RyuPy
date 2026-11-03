# ADR-0050：普通单继承类的有限 new/init 协议

- 日期：2026-11-03
- 状态：本地候选；冷编译接点、真实driver专项与完整正常门禁通过，远端尚待。

## 目标与边界

继ADR0049的真实object实例之后，本切只补普通PyClass单继承分配协议，不建立通用type、
metaclass、C3或内建布局分配器。保持canonical object/PyInstance身份；原Enum、异常工厂、
运行时dataclass与原生类路径不能被普通new PyInstance覆盖。

## 构造顺序

用户参数沿原路径完整求值/展开，requestedClass固定引用。实际new槽先调用并传真实cls；
只有结果是requestedClass或其子类的真实实例才继续，并在new返回后从result.Class重新
查当前init。foreign/None原样返回且不读init；返回缓存实例也每次初始化。new内重绑init
本次立即可见，init必须返回None，否则带原caller/context抛TypeError，不注入假函数帧。

普通构造仍复用CallValue→PyClass.Invoke。只新增一个冷
`FinalizeOrdinaryClass(object)->void` RuntimeFn：类体完成并恢复namespace后、类decorator
之前，验证最终own namespace，再把own PyFunction __new__包为staticmethod。继承值、
已有wrapper和其它形态不盲包；类创建后的raw函数重绑保持原绑定语义。没有新local或CFG。
根new/init及super终点仅补本切构造槽，真实定义owner/cls/self上下文在嵌套/异常后恢复。

## 专用布局与有限冷拒绝

actual special无自定义new继续旧成功路径；自定义/继承new在分配或dataclass快计划之前
明确拒绝。类decorator晚于finalizer，dataclass激活会Touch原版本；祖先late new由现有
GlobalVersion失效重查，不做反向子类网络。未覆盖new的原专用快路保留。

不模拟CP tp_new完整历史。finalized普通类own __new__删除、late根槽重设或根wrapper
在Attributes写/Remove/Touch之前NotImplementedError，原槽和版本不变；缺own删除仍原
AttributeError。类体阶段只按最终namespace检查。合法但不支持的CP槽历史完整保留在
`tests/fixtures/object_construction/rare_slot_history.py.pending`，不能把拒绝说成CP一致。
非callable仍TypeError；未支持的一般descriptor/布局不假调用。不改变全局bool富比较旧债。

## 成本合同

无用户new路径最多一次可失效cached槽查找/版本guard，不新建wrapper或参数数组，不先造
一个随后丢弃的实例。真实用户new才允许一次显式cls实参数组/既有绑定等价数组；原init
路径与dataclass fastplan保留。版本失效时允许重查实际祖先链，不作每次无界HashSet或反射。
本轮无性能采样，不声明零指令成本或已关闭既有matmul/模块调用成本负项。

## 实测与首失败记录

固定CPython3.12.13已核foreign/缓存/子类返回、actual init/late重绑、定义期与late函数
绑定差异、classmethod显式cls、super、参数顺序与caller/context。
冷MIR ABI/效应与DCE两项、真实PE引用一项通过；真实B四driver通过，旧A三通过加一个
完整富比较ignored保留。A旧派生分配拒绝用例按新能力迁为CP正例，锁真实类型/不调用init、
普通构造仍init、派生可写和其它内建布局拒绝，并非删成无断言。

首C#编译的三个错误是测试API接点（MakeSuper所属类、internal标记），改真实公开入口与
仅测试反射后保生产API不扩。次轮21/23通过：一旧PIC断言仍期待已实现分配抛错，另一
测试在已有“dataclass继承不支持”前置就失败。后者改真实无基DC late-new、普通类缓存后
DC激活以及真实TestCase自有两层链的祖先late-new，未修改共享TestCase/未伪造DC继承。
修后23/23（新12+旧object11）实际通过。首批7个冷call快照经两轮独立机械审查后按SHA精确迁移，正常snapshot目标25/25通过。

首轮完整门禁已过579 C#与228 CP，随后独立class_if旧MIR快照缺冷finalizer而停止。
对25个独立snapshot入口一次正常driver全量发现（无FORCE/UPDATE）：193通过、7个
快照失败、2个既有ignored；未将发现轮当验收。新增候选继续逐项审receiver/类体完成
时点、原业务指令及CIL分支/leave/switch/EH映射，按归档SHA迁移，不广泛接受快照。
共16份快照/23处冷finalizer已逐项独立审查并精确迁移；未改原方法、局部和EH结构。
修后正常`make check`终态exit0：579 C#、228 CP基线、202 e2e通过加30 ignored，
fmt/clippy及workspace测试通过，未使用INSTA FORCE/UPDATE；原失败日志保留。

本轮不增加项目通过分子、不新跑性能或原生B专项；远端复验范围后续单列。
