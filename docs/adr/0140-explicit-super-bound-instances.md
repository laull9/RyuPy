# ADR0140：普通单继承实例的显式二参super

- 日期：2026-11-07。
- 状态：实现候选，待hosted完整门禁；不得以CP基线代替产品验证。
- 基线：6636a9c98f7b6c4133beadd9c631ffa0eeb6135a。

## 为什么选这个竖切

固定项目集的progress1.6原入口在Progress.__init__第145行调用
`super(Progress,self).__init__(*args,**kwargs)`。真实层级为Bar→Progress→Infinite→object。
历史旧实现忽略显式实参并借TLS，碰巧获得相同目标；恢复这种行为会让其它起始类调用
返回错误方法。当前显式NI必须用一般对象合同解除，不能按项目名、方法名或实参数目
假装成功。原包/入口/golden保持不变；恢复与否须用新工具跑完整20项后报告。

## 支持合同

显式调用共享PyExplicitSuper.Construct：默认直写DynBuiltin和canonical一等super类型
在完整实参求值/展开后使用同一入口。只接受真实普通PyClass起始类与实际相关普通
PyInstance。按引用身份验证完整单父链，不使用类名字、当前方法owner、TLS、用户eq/hash
或可覆写__class__getter推断。可在模块/方法外调用、保存alias、传入动态起始类；
查找从真实起始类之后继续，实例字段不遮蔽super查找。

普通方法、property、staticmethod、classmethod复用既有绑定，classmethod绑定实际
receiver.Class；已保存bound method仍保旧target，新读取则观察普通祖先方法替换/删除。
PySuper布局及单链表示不变，不新增MRO缓存。直接runtime PIC仍按起始类/名字/全局类
版本缓存普通函数，每次注入本次Self；完整祖先普通属性变更使全局版本失效。

默认直写零参仍保旧TLS；Instance canonical零参仍使用真实词法classcell/当前首参数。
一等零参无frame仍NI。显式二参不借任何frame。关键词拒绝统一为CP的
`super() takes no keyword arguments`，不再沿旧默认专用unexpected-keyword文案。
非类型首参及普通无关实例是CP TypeError；真实原生类型/合法class receiver/一参或None
未绑定代理是当前能力NI，不冒称Python禁止。签名错误优先级由固定CP3.12.13核验。

## 同一次调用的属性捕获

构造正确不代表调用相位正确。旧普通super方法路径在实参后才查方法，参数中重绑/
删除祖先方法可能改变本次目标，property getter或缺属性也晚于实参副作用。

新增语义明确的RequiresEarlyMethodCapture/CaptureMethodTarget，旧IsModule/
CaptureModuleTarget作为PE兼容alias保留；旧名字早已不是Python模块类型判定。
只将PySuper加入既有有界预捕获域，不扩大所有普通实例或CLR调用。捕获只做GetAttr，
不检查callability：属性42存在时，仍先求args后报不可调用。getter抛异常或缺属性时
则不得执行args。list/dict/str/NamedTuple等旧特殊分支保留。

默认和Instance源码都通过已有MIR捕获对，在args前冻结目标。源码普通super方法调用
因此通常分配bound method并绕过原super方法PIC；不能据“未改PIC布局”声称无开销。
本切不做性能实验，不承诺无性能回退。直接runtime PIC收到的是已求值参数，仍测试其
当前Self和全局版本合同，不能拿它替代源码表达式求值顺序验收。

## 未实现边界

- 多继承/C3、metaclass、native起始类型、异常/Enum/dataclass/typing及专用存储布局。
- class receiver、一参/None未绑定代理、super子类及__get__重绑定、完整反射。
- 代理__class__回退与任意自定义descriptor；descriptor只在真实属性消费时NI。
- __class__/__bases__真实重绑。现有普通字段写入不等于CP重建类型/MRO，本切不大改
  全局对象协议，也不宣称这些旧伪字段问题已修复。普通method属性变更不属于此边界。
- 通用混合star/kwargs展开副作用顺序、参数内yield的既有prepared-super门禁。
  运行时只见展开结果，不能声称按源码星号形式一概拒绝；单个无副作用list/tuple及
  空**dict的成功不外推复杂展开。

## 验证计划及当前状态

新增同源完整default/Instance case仍pending。固定CP原流成功只证明测试有效；候选
hosted脚本显式编译/JIT两份case，并分别用PIC on/off运行，不把pending跳过当通过。
C#合同覆盖两构造入口、同名不同身份、完整特殊链、未绑定NI、getter不提前执行、
保存方法、同站点交替receiver及祖先变更。原词法零参/TLS隔离测试保留。

保留正常默认/pilot完整make check与原Instance Native两产物、Legacy/resource门。
新增同源普通class/super的Legacy Native发布、ELF检查、归档、搬迁及固定镜像离线只读
运行；镜像内拒dotnet/python工具。不放开Instance class AOT预检，自动默认仍Legacy。
完整20项目只跑一轮，原九ID逐项保护后对同轮summary另施加progress native和promotion
约束；不称两次独立矩阵。门禁promotion结果不是启用默认模块/AOT开关，也不预先将
progress列为第十保护项。不宣称全项目AOT、全trim安全或性能无损。
