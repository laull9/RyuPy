# ADR0128：实例模块的真实词法零参 super（S1）

- 日期：2026-11-06。
- 状态：限域接受。六参PIC及旧分派委托修正后的正常门禁、真实PE、原项目复验和限定成本观测完成；边界与性能风险仍保留。
- 基线：7203d777124019d7a1e165da15c9c5750093b230。

## 目的与范围

S0已建立真实定义类cell。S1在显式实例模块的普通非生成器函数中，接通源码无任何
实参的值调用。实际callee必须是唯一canonical super才使用当前函数的词法frame；
同名用户callable正常调用。原html.parser两处super阻塞不能用TLS owner或项目特判消除。

本切不实现loader，不将窄源编译成功计作html.parser或Markdown兼容通过。

## 从名字到真实调用

1. HIR：函数读取super即隐式请求__class__，即使super被参数、local或global遮蔽。
   __class__自身的参数/local/global遮蔽仍按CP词法规则；同名own cell不是free cell。
   仅使用全局alias而无super/__class__词法读取时，不临时补造类cell。
2. MIR：InstanceZeroCall携冷来源ZeroCallFrame。定义类指向S0登记的真实Env槽；首参
   指向当前首个PosOnly/PosOrKw绑定，不算*self、keyword-only或隐藏参数。
   先求值callee，再在调用点取首参当前槽或原own-cell引用，绝不保存入口self快照。
3. CIL：只消费MIR，发射CallZeroWithFrame(int site, object callee, object env,
   long classSlot, object arg0, long argKind)。不预先CellGet/CellGetFree；没有新Rvalue
   就不建新ABI引用。每个调用仍消耗原ctx.next_site的一个站点，后续PIC编号不漂移。
4. runtime：BuiltinFunction("super")返回唯一PyNativeType。helper先ReferenceEquals；
   非canonical回原PyPic.CallValue0(site, callee)，canonical才读取首参及类cell。
   仅沿真实普通PyInstance.Class单继承链证明起始PyClass，再创建PySuper(start, receiver)。

类名重绑、函数移植和可写__module__不改变cell。没有type(self)、lookup owner、模块名
或TLS推断。MIR验证错/缺Env与index、隐藏首参、入口快照、raw Env逃逸及own-cell初始化前
消费；普通局部__class__的free cell本切明确NI，不把它误报为不存在。

PyFunction布局、Invoke和PIC布局/算法不变。实例函数UsesSuper明确false，不维护旧TLS。
type(proxy)、isinstance和super共享真实类型身份；普通方法/property/static/classmethod
沿既有绑定协议消费代理，任意descriptor仍不支持。

## 错误相位与明确边界

非canonical分支不消费缺失或空frame。canonical按CP次序检查：无位置形参、删除哨兵、
缺__class__ cell、空cell，之后才验证类及接收者。缺/空cell报RuntimeError，而不是裸
__class__读取的NameError；普通无关实例报TypeError。首参删除的完整Python源仍被实例
预检拒绝，helper的哨兵检查不表示语言层已支持del参数。

显式实参、star/kwargs、属性alias、partial/native callback、generator/genexpr alias、
class receiver、特殊native布局、多继承、代理__class__回退、任意descriptor、复杂反射、
普通外层显式__class__自由cell及None未绑定proxy均保完整pending。动态canonical边界NI，
已有语法边界继续Diagnostic，不能借alias回落TLS。CP的partial(super)可能成功，None也可
生成未绑定proxy；本切NI只表示能力未实现。复杂反射在CP可先查base同名值，不把合法遮蔽
称错误。实例obj.__class__属性协议不随本切开放。

代理__new__、__init__是已有正常基类/根构造属性，不与super类型自身的复杂反射混淆。

## 默认路径与兼容收紧

默认直写零参仍保DynBuiltin→CallBuiltin旧ABI/TLS，不宣称默认模式真实词法super已完成。
默认一等super值现在有真实类型身份；无显式frame调用改NI。旧CallBuiltin对非零位置实参
改NI，不再忽略实参。它只见展开后的数组，因此super(*[])等零runtime参数仍可能走旧TLS；
不能声称默认所有源码star形式均已拒绝。新canonical值路径的star/kwargs则不会借用TLS。

## 已保留的验证历史

- 25份新完整多文件case的固定CP基线通过；9份正例真实编译/JIT输出一致，16份边界
  保完整失败与pending。另有旧S0 pending_super完整原源通过，共10例去pending；未改断言、
  print或golden，旧目录名保留。六参修正版及后续runtime修正均经完整门禁复验。
- gate-001真实2404个C#通过、1个旧构造测试失败；518CP通过，尚未进入Rust/e2e。
  唯一定向复现确认新守卫误把代理__new__列为反射NI。最小修正只移出该名字，旧测试
  未改，并加canonical代理的root分配及继承__new__两条调用路径回归。
- 修后的五参中间候选gate-002正常make check通过：2405 C#、899 Rust、5项清单、
  518CP/518框架自检、352e2e，170ignored。此结论不能转用于后续六参ABI。
- 正式计时0负载时，静态审计发现五参helper普通分支直CallValue丢失原CallValue0 PIC。
  这是真实结构性降级。计时前改为六参并恢复原快路，增加同site两shadow/canonical交错、
  越界/禁用PIC及站点连续性检查。保留原失败和旧产物，不称修后性能已经改善。

原项目窗口001使用五参中间工具：原docopt通过；默认原Markdown仍是原375字节find_spec
NI。未改四份CP文件的窄源已compile exit0，128/141两处诊断消失，真实JIT新停在
AttributeError：importlib.util缺module_from_spec，stderr335字节。窄源无独立新CP运行，
仅复用相同入口原golden比较；尚未完成公开模块实例执行，不计项目通过。

## 最终runtime修正与验收

六参gate003为2408 C#/900 Rust/518 CP/352 e2e、170 ignored。定点CIL随后发现旧
BuiltinDispatch的super分支每次创建捕获args的Func。最新runtime将其改为直接双参
LegacyConstruct(args, kwNames)：先保原首关键词TypeError，再拒非零位置实参，最后才
进入原MakeSuper。新29项专项通过。公开旧CallBuiltin及六参CallZeroWithFrame ABI不变。

最新准确源码正常make check通过：2409 C#、900非e2e Rust、5清单自检、518 CP、
518框架自检、352编译端到端，170 ignored。11698源文件门禁前后哈希与权限一致。
CLI仍为1757a3f1…030f4db；Release runtime为f7e50d90…086da8d。真实生成PE的24项
双实例、重执行、旧函数归属及继承检查重新通过，实际加载该新runtime，不借旧002结果。

新runtime下重新编译并JIT执行三个原入口：docopt成功，16字节stdout逐字等于原golden；
默认Markdown仍在find_spec缺source metadata处NI（375字节stderr）；未改四源窄闭包
compile0、两处super诊断消失，但JIT在module_from_spec缺失处失败（335字节stderr）。
三者双流均与窗口002相同；CP只核原源/原流身份后复用，未另跑。项目通过率不扩大。

新定点CIL及5份B smoke已核验实际加载身份；原A/B生成PE与Host逐字复用，仅B换新runtime。
一次14进程/144暖样本按原固定顺序完成，无失败、重试或补采。9条不利观察全部保留，
包括default普通实例零参+6.071%、closure+6.971%，tieroff类body+10.579%、module_bare
+10.136%。这是一对进程各三样本，不给总体无回退、统计显著性或此次委托修正收益结论。
完整22组和4项绝对成本见[性能结果](../lexical-super-performance.md)。

新CIL只证明super分支移除了专用Func与WithKeywords；BuiltinDispatch入口仍分配其它
lambda所需displayclass，不能称整个分派零分配。只核19个选中runtime方法，不签全DLL、
metadata/全部ABI或JIT机器码等价。旧003的27步准备属于旧runtime，不改写为新runtime结果。

恢复原PIC只消除已识别的分派改变。实例普通零参值调用仍新增身份分支及frame固定参数；
canonical另有cell读取与类链校验，建类还承担S0真实cell成本。不能据静态结构声称零成本。

S0默认method_calls +310.450%、generator +40.126%、resource_files +31.608%等历史不利
继续未归因；metadata/token scope/PE余区未全解析的边界不变。不补采抵消旧风险，不以
无新增PyFunction字段、默认PE相同或分配相同作总体无回退证明。
