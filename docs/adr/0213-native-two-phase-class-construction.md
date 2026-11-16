# ADR-0213：两阶段原生外部类构造

- 状态：已实施，macOS arm64 定向验收通过
- 日期：2026-11-16
- 关联：ADR-0208、ADR-0211

## 问题

外部基类的类构造原先通过 `_rp_calculate_meta` 和 `_rp_make_class` 执行 Python 胶水。
类体先运行，再调用 `types.prepare_class`，使自定义元类的 `__prepare__`、prepared mapping
查找和失败时序偏离 CPython。元类的 `__new__` 或基类的 `__init_subclass__` 重入 CIL
方法时，真实类型尚未登记，接收者也可能形成第二个不相同的托管身份。

## 决策

在 CIL 类体前经静态 C shim 选择真实元类并调用 `__prepare__`，在类体成功后调用同一元类。
不执行 Python 构造胶水，也不通过解析源码或调用 Python `compile/exec` 补偿。
元类和外部函数本身仍归其原 provider 执行域。

`class_construction.h` 使用选中发行的公开头文件：比较真实 `PyTypeObject` 的元类子类型关系，
持有 name、bases、metaclass 和 prepared namespace 的 owned 引用，以私有 capsule 管理生命周期。
`__prepare__` 缺失只认 AttributeError；值为 None 仍调用并产生 TypeError；namespace 必须为 mapping。
本切不新增显式 metaclass 关键字或任意 `__mro_entries__` 语言域。

类体裸名先执行一次 mapping `__getitem__`，仅 KeyError 触发 globals/builtins 回退。
中性桥的 Contains 冻结该次读取，Get 消费相同结果，因此不会额外触发 `__contains__` 或二次查询。
写入和删除使用真实映射协议；若返回值确为先前导出的同一对象，保留原 CIL 函数/描述符身份。
`__metaclass__` 保持普通属性。

provider 词法类采用新的三参数 `ForeignClassPrepare(name,qualname,bases)` ABI。
prepare 后由 CIL 执行类体前言：先 namespace 查询 `__name__`，缺失才读实际定义模块的 globals/builtins，
然后写 `__module__`、`__qualname__`。旧四参数词法 ABI 和原生程序的 CFG 保留。
旧模块类入口也先开启真实 prepare，再读取元数据；实例模块入口使用已经捕获的真实 context。
函数 `__module__` 继续从 def 所属 globals 获取，不随类 namespace 对 `__name__` 的遮蔽变化。
完成后的类 `__name__`、`__qualname__`、`__module__` 由外部真实类型提供，允许 mapping 与元类改写。

## 身份与失败清理

构造期间真实类型调用已写入的 CIL 方法时，通过真实 type 字典中的同一导出描述符及直接 bases
逐引用一致性识别接收类型。登记临时 pointer→PyClass 映射，避免按名字猜测或注入用户 namespace 标记。
成功返回类型必须与已登记临时类型一致；元类返回非类型、已登记其它类型身份或更换临时身份时明确报错。
本切继续保留对精确 Python FunctionType、CodeType、frame 的有限合同，不扩展反射域。

CIL provider 类体保护区在异常路径调用幂等 `ClassDefinitionAbort` 后原样抛出同一异常。
该清理不是 Python except，不改变当前处理异常、上下文或 traceback 状态；CellNew 与成功终结后的
唯一 CellSet/发布证明保持。纯原生程序不增加该 EH 区域。

冷事务使用弱键表及宿主登记，普通 PyClass 不新增实例字段。成功、失败和宿主关闭释放所有 capsule、
prepared namespace 和已导出成员引用。失败撤销临时类型关联；真实 traceback 合法持有的 frame/namespace
由其原生命周期管理，释放测试显式清除 traceback 后再验证构造事务没有残留引用。

## 验证

- CPython 3.9.25 / 3.12.13 / 3.13.3 各以真实头文件构建 shim，并跑同次 oracle 与真实 C# 类桥。
- 默认方法包装与显式原生描述符候选分别验收；本切不改变候选的默认状态。
- 重点覆盖 prepare/body 顺序、单次 namespace 查询、None prepare、元类冲突、普通 `__metaclass__`、
  构造回调身份、prepare/body/meta-init 失败、临时身份撤销、namespace 释放及真实元数据改写。
- 产品输入先 pending，再用固定 CPython 同次对照 JIT、NativeAOT，删除输入和 build 目录后中文空格路径搬迁。
- 运行帧审计要求两个已删除 helper 为零；C0 启动完整性状态继续按原受限合同报告。
- 普通 MI 的 NativeAOT 保护门保留，相关 MI 输入继续 JIT 专项。

本次 macOS arm64 实测：3.9.25 / 3.12.13 / 3.13.3 各以真实 shim 跑默认与原生方法候选
30 项类桥，全 180 项通过；五个手写类构造输入均在固定 3.12.13 同次 oracle 下通过 JIT 与 NativeAOT，
并在删除输入、build 目录和隐藏私有基础发行后从中文空格路径搬迁执行。
其中 `native_class_prepare_metadata` 同时核验 prepared 名称遮蔽、prepare 修改真实模块名和 namespace
优先于外层 free-cell 的 LOAD_CLASSDEREF 查找。类体显式 global/nonlocal 保持现有 Diagnostic 禁域，
方法体的 globals/cell 规则保留。

真实 NativeAOT 审计探针也已搬迁通过：宿主关闭、三阶段有真实帧、零丢帧、零覆盖失败；
`_rp_make_class` / `_rp_calculate_meta` 与已移除的 object/super helper 帧数均为零，
保留的 `_rp_wrap_method` 有真实调用帧，严格门按 complete=false / startup_complete=false 拒绝。
其它平台以及未开放的 Python 类构造协议留给对应验收。


## 构造重入与专用 set_name 代理

真实构造重入按全部活动事务匹配：原生 API 核对冻结直接 bases 和真实 MRO 首个字典成员，
成员须为同一活 callable，或精确内建 staticmethod/classmethod/property 中的同一 callable。
继承回调还核真实声明类型。callable 使用 owned 弱引用，target 不强持 wrapper；
这既避免跨堆环，也避免 wrapper 回收后裸地址被复用而误认。多个事务同时匹配、已存在其它
opaque 身份、元类更换临时类型均明确拒绝；不会替换已经返回给用户的对象视图。
匹配发生在容器同步和实参转换之前，property getter/setter/deleter 均有相同身份合同。

只有真实 RyuPy 实例类型链拥有 `__set_name__` 才签发 `ryupy.ManagedDescriptor`，复用原代理布局，
静态特殊槽通过同一 Rootid 回调 CIL；owner 在转换前按真实字典中的同一 descriptor 关联。
普通代理以及仅有实例同名字段的对象不暴露该槽。专用类型没有 `__get__`、`__set__`、`__delete__`，
同时定义这些尚未桥接协议时明确拒绝导出。类型删除 set_name 后按实时类型查询无动作；
已经导出并由 CPython 保活的普通代理若后来新增类型级 set_name，再导出时精确诊断能力变化，
保持原 native 代理身份。已完成真实 foreign 构造的类跳过原 set_name 未实现守卫，原生类守卫保留。

候选 NativeMethod 不满足 CPython 的 PyFunction 精确检查，type_new 无法替它自动完成隐式 new 绑定。
按导出键 `__new__` 选择独立 `NativeStaticMethod` 类型，共用完全相同实例布局，仅 descr_get 为静态；
后续改名字不会改变绑定。prepared mapping 不为归一化再次写入，普通方法也不增加标志字段。
旧 `rp_native_method` ABI 保留，新 `rp_native_static_method` 仍属于显式候选，默认 wrapper 不变。

新增真实重入输入同时覆盖自动和元类显式 set_name、隐式 new、实例 new 的静态调用及名字修改、
property 写/删、继承 classmethod。默认与候选均完成 JIT、NativeAOT 和删除输入/build 后搬迁。
C# 负例保留同名同 bases 的 decoy、提前形成的 opaque 身份、实例同名假 hook、迟增能力及未桥接
数据描述符；三版本各两模式 30 项全过。首次候选实例 new 的“双实参”失败样本保留于仓库外验证日志。
