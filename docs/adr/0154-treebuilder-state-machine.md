# ADR-0154：TreeBuilder真实事件状态机的有界竖切

日期：2026-11-10。状态：实现候选，尚未产品验收。

## 背景与决定

原Markdown3.5.2的完整固定入口在extra初始化时缺TreeBuilder。仅补构造壳即使恢复原77字节，
也不证明有HTML时的真实树行为。选择通用事件状态机，另以两项真正Markdown-in-HTML内容验收；
不按库名、输入或输出特判，不引入XML DOM、内嵌解释器或源码重写。

参考CPython3.12.13默认C加速器的[状态机源码](https://github.com/python/cpython/blob/v3.12.13/Modules/_elementtree.c)
及[SyntaxError源码](https://github.com/python/cpython/blob/v3.12.13/Objects/exceptions.c)。
冻结CP原完整pending通过，原Markdown77字节和HTML102字节均exit0/stderr空；新增六组同源参考
合计1467字节（SHA256 `1a6537a8488f396487dc4e88334175f79521c2c5731ff73942049aded3e4b948`）。
这些是CP参考，不是RyuPy成功。

## 状态、工厂与边界

- 每builder独立root/current/last/tail目标、栈及pending文本；复用PyXmlElement和children引用。
  默认非空精确dict别名，空dict分离；显式canonical Element工厂浅复制。旧Element/SubElement不改。
- data在第一个start前忽略；树内只接精确str。单段保原引用，多段收集、事件flush时合并；
  外部已写text/tail按文本追加。close不flush、不验栈、不销毁，返回同根或None。
- start/end/comment/pi先flush；end忽略tag。空end为IndexError。第二根先清旧tail目标再ParseError，
  已flush不撤销。新增CP实观root.text从ab追加more为abmore，root.tail仍after。
- 默认Comment/PI只在真实native登记时捕获原函数，后建属性字典不替换它们；函数继续读定义globals。
  不插入也flush；文档外comment/pi不是root；插入时仅改tail目标。保Comment任意text、PI None/false旧能力。
- 自定义/非callable factory、用户flags真值、树内非str data/追加、任意映射或非str键明确NI。
  绑定先验证C签名，不偷执行用户协议；不把这些NI算CP等价或callback traceback修复。

## 必要异常接线

真实CLR链为PyXmlParseError→PySyntaxError→PyException。两个新类型支持零/单精确str消息、args/msg、
六个默认None定位字段、类型模块/表示/终端限定名、static/dynamic/base catch。零参str为`None`。
复杂位置、多位置参数、非str消息、字段写删及用户派生明确拒绝；不伪造code/position。
SyntaxError direct/value都读真实默认builtins，完整求值位置与关键字参数后CallValue；旧KeyError arm和其它异常ABI不动。

稳定原生导入仅沿现有HIR证明登记ParseError静态catch；局部/运行期别名仍走NativeTargets。
ParseError异身份赋值在BeforeSet提前NI、同值允许。普通setattr/属性赋值走同一setter；公开SetDynamic
仅对真实native注册且固定注册名匹配的新ParseError转回setter，不依赖可写的__name__，不误封source同名模块。
native模块删除继续原NI；未开放native __dict__。Comment/PI/Element旧重绑不受新增守卫影响。
用户新族基类先精确识别再HIR E0100；动态真实类型在RuntimeBases/MakeClassCore精确NI，不进入旧用户消息格式化器。

新自身错误仅在六个internal cold producer各自new→NativeFresh→throw；legacy独立附帧。
不复用Own/PrepareRaise、不Fresh已捕获或用户异常。S1手写六个准确方法与真实PE变异，保原38测试；
旧1700公开签名不变，另手写6条异常族增量，不重新记录baseline，也不扩大任意TreeBuilder程序的V1准入。
PyExceptionTypes的注册表移到PyExceptionFactories，原注释、公开签名及旧映射无损保留。

## 验证与停止线

原pending字节、旧XML/KeyError与原callback红项保留。候选尚未运行C#、Rust构建、JIT、Native或完整make check。
需要六组CP/JIT、原77字节与HTML102字节、独立Native及搬迁、两策略make check、S1/ABI和默认20项逐ID保护。
完整Markdown AOT只沿原配置补26个真实非入口资源锚点观察现有instance guard；不开放instance AOT。
遇到下一独立parser/emitter、完整tostring/ElementPath、广泛descriptor/traceback或owner机制依赖就保存第一错误并停止。

默认固定项目仍11/8/1；显式模块/metadata/instance配置成功不等于默认12或全库支持。
每次事件的字典key扫描、文本段收集/flush合并、children和stack增长都有成本；无新benchmark或性能结论。
栈随当前深度增长，pending随两次flush之间文本增长，close保builder可继续使用，不保证恒定内存。
