# ADR-0031：项目工具库的原生协议与受控目录

- 状态：已接受；M3.7 用户批准的有界范围最终聚合验收通过（兼容门按 ADR-0032 独立保留）
- 日期：2026-11-01
- 关联：M3.7、ADR-0030、固定 CPython 3.12.13 / 20 项目集

## 背景

固定项目的真实调用暴露 map、future 导入、warnings、dir 等入口阻断；原 M3.7
还要求 argparse/logging/unittest 与 enum/copy/datetime/random/string。模块导入
成功不是 API 验收，也不是项目通过。保留原始里程碑用例和项目分母，按完整入口
重新运行，不用空模块、硬编码目标库名或 CPython 子进程桥接冒充原生实现。

## 决定

1. 能复用的调用、异常、迭代和参数绑定协议继续复用；原生类型显式登记，而不是
   逐对象反射。需要真实用户继承的 unittest.TestCase 与 Enum 单独保持 PyClass
   身份，编译器用已验证的稳定原生导入标记，不把它们伪装成内建异常。
2. map 保留惰性、可恢复推进、直接 next 的 StopIteration.value 和重入安全。
   普通调用复用 k 项数组，重入才分配另一数组；参数槽 finally 清理。
3. future 指令仅允许已实现语义，元数据与启用能力分离。Warning CLR 类树和 Python
   类树一致，原参数与延迟消息保留；警告不是 traceback 报告，使用独立过滤/输出。
4. dir 用户对象默认目录只枚举当前真正存在的用户属性和已赋槽，不伪造尚不可读的
   object/type 默认 dunder。这个**低影响目录偏差**明确接受为当前默认子集：常见
   公共成员枚举可工作，而反射完整 object/type 目录仍独立 pending。自定义 __dir__
   完整消费返回 iterable 并排序，不能过滤/改写用户返回项。零参局部帧目录与未登记
   内建对象直接拒绝。目录不逐属性 GetAttr，不触发被枚举 property；名称按 Python
   码点排序而非 CLR UTF-16 单元排序。
5. copy 的首批深复制用引用身份 memo 保 list/dict/tuple 环与共享节点。用户重建协议、
   外部 memo 尚未开放，明确拒绝；浅复制具有用户 hash 的字典也拒绝，避免在尚无
   cached-hash 复制表示时新增可观察回调。string 常量不依赖宿主 locale；未实现
   Unicode 标题化不能用 ToUpper 单字符近似。

## 性能与边界

原生接口缓存签名/稳定类型；不因接口数量增加每个用户实例的反射成本。logging、
测试执行、argparse 与警告属于低频控制路径，但仍要保求值顺序和错误副作用。
map 正常每轮复用参数数组；dir 每次收集排序 O(n log n)，copy 额外 O(n) memo 与
新容器。没有基准的库实现不作加速承诺，M3.6 数据类热点优化仍保留。

warnings 的当前位置从既有帧数组冷读取，不增加每次 TracePush 的分配。当前编译器
源标识可能只有 basename，同一标识对应多个模块时明确拒绝，不猜 __main__，
也不宣称完整包级 warning 位置语义。复杂过滤正则/启动配置/替换钩子等逐 API
边界见后续库表。各边界不删除兼容性欠账或缩小固定项目分母。

## 验证与后续

当前为分片联调状态。最终将记录原 M3.7 验收、扩展差分、运行时专测、完整
`make runtime` / `make check`、固定项目结果与独立只读审阅；在此之前不得将
ADR 状态改为已完成或创建 M.3.7 标签。

逐 API 说明：`../stdlib-project-tools.md`、`../stdlib-logging.md`、
`../stdlib-argparse.md`。完整 dir、Unicode capwords 等独立 pending 保留。

## CI 基线安装修复

M.3.6 的 Ubuntu 远端全部通过；macOS 在测试前的 setup-python 安装阶段失败，
其 3.12.13 发行清单没有 Darwin 构建。M3.7 保留 Linux 原安装流程，macOS 使用
固定官方 setup-uv 提交 `68559e99cc807c1eb7e9238fd43a15bba460eec2`（v10.3.3），
固定 uv 0.13.4 显式安装 CPython 3.12.13，版本断言后将绝对解释器路径写入 PYTHON。
没有改为 3.12.14，也没有跳过 macOS 测试。离线 uv metadata 中存在
3.12.13-macos-aarch64 / PBS 20260807 下载项，但这不等于 macOS 实际安装已经通过；
下一整里程碑推送后必须依据远端 CI 确认。


## 大字典实际标量守卫

本节修订 ADR-0023 的大字典拒绝范围；ADR-0023 原接受记录保持不变。

固定 colorama 入口被未执行 Windows 分支的 18+ 属性整数键字典阻塞；按语法一律 E0100
会拒绝本已没有用户 hash/eq 回调的常用配置表。现在保留原 <=17 项路径、字典 `**` 拒绝与
所有字面量键免守卫路径；更大动态字典每对先求 key/value，再调用 void
`PyOps.DictSetScalarKey(dict,key,value)`。守卫只检查精确 CLR 标量表示：int/long/BigInteger、
bool/string/double/null/PyNone，不调用 `hash`、`eq`、`str` 或 `repr` 来试探。

所有已插入键也经过相同守卫，且构建字典未公开；因此提前执行这些标量的内部哈希不会
增加用户回调或改变表达式求值顺序，不需要构造 CPython 的临时分块字典或搬运缓存 hash。
新增成本是每对一次类型分派，literal 全安全路径无此成本；不据此宣称字典整体加速。
拒绝键的 value 已按源码求值，之后在任何用户 hash/eq 之前抛 NotImplementedError，
后续 pair 不再求值。这是明确未支持边界，不声称其副作用时机与 CPython 用户键建表相同。

原首/尾动态键 E0100 快照迁为 MIR 守卫快照，不删除原源例的暂停/边界覆盖。
`m3_7_large_scalar_dict.py` 覆盖 34 项、跨块重复、属性整数、bool/int/float 等值键、
后值异常与暂停后模块重绑；`large_dict_guard.rs` 验证拒绝时没有用户 hash/eq/repr 回调。
原 `m3_2_dict_chunk_hash.py` 与新增自定义键差分仍 pending；tuple、bytes 与自定义键
即便某个实例实际可哈希也不在本次接受范围，完整 cached-hash 合并仍是独立欠账。

## 链式赋值与对象身份

colorama 的标准流包装使用链式赋值。模块与函数中的链式目标依源码顺序处理，
RHS 只求值一次并存入隐藏 Runtime 对象槽；每个属性/下标目标到达时才求其对象和索引，
前目标失败会阻止后续目标。复用现有名字（含 global/nonlocal）、属性、下标和平坦解包路径；
类体链式赋值与嵌套解包仍明确拒绝。

当前类型分析会让含该隐藏 Runtime 槽的函数走动态路径，并有恒真分支封装及生成器局部
保存成本。这是为保同一 RHS 对象身份而选择的首版正确实现，不能宣称保持原静态性能；
后续特化须证明身份不可观察，不能靠重复装箱伪造共享值。

## 注解、比较拒绝值与当前项目证据

`from __future__ import annotations` 的规范化字符串与实时模块注解表采用编译期
AST 去糖；准确支持形态和未支持项见 [future-annotations.md](../future-annotations.md)。
它不调用注解表达式，也不靠截取源码保存不规范的空白；复杂调用表达式仍明确拒绝。

`NotImplemented` 是公开单例，不是 `NotImplementedError`。富比较收到它必须继续
右侧反射方法或最终类型错误，不能先转 bool 而误判 True。显式 bool 转换对齐固定
CPython 3.12：先经当前 warnings 策略发送 DeprecationWarning，再返回 True；error 策略
照常抛警告实例。其类型无参构造返回同一对象，不接受把单例当 callable 使用。

2026-11-01 当前局部集成：354 项 C# 单元通过；future/链式赋值/比较回退实际差分通过。
固定项目入口为 **1/20 原生通过（wcwidth）**，另有九个有来源和 MIT 许可的 wcwidth
核心上游场景移植通过（见 tests/upstream/README.md）。这不是整个 M3.7 完成，也不是
80% 目标达成；该轮时点的其它首阻断包括内建容器继承、ctypes.Structure、io.UnsupportedOperation。
其中 io 异常阻断现已解除，固定项目仍待复测，不能据此提前提高项目通过数。
完整聚合门禁及剩余原生项目阻断继续推进，不以模块导入、语法接受或小样例替代固定分母。

## 平坦解包中的可求值目标

texttable 的实际阻断是 `(self.a, self.b) = values`，不是递归嵌套解包。HIR 新的
叶目标表示保留名字绑定、属性或下标，赋值、for、with、链式赋值复用同一路径。
MIR 先校验并冻结本层全部拆出的值，再逐叶从左到右求对象/索引并写回：拆值失败不写，
后叶失败保留已完成的前叶赋值。一个星号叶也使用相同目标规则。

递归嵌套目标仍明确 E0100；既有 len/getitem 解包路径并不等于任意迭代器解包协议，
不能据此次目标扩展声称已修完整 iterable 解包。每个值增加保序临时量，生成器可能
增加跨暂停保存槽；没有新增运行时 ABI，也不作性能加速承诺。

## io.UnsupportedOperation 双父异常适配

本增量修订 ADR-0028 当时“UnsupportedOperation 层级未支持、能力错误为普通 OSError”的边界；
历史 accepted ADR 正文不回写。准确 API、稳定导入限制和验收见
[io-unsupported-operation.md](../io-unsupported-operation.md)。

CLR 类型继承 PyOSError，Python 层次表另登记 ValueError 父边；普通内建 ValueError handler
在源位置增加 UnsupportedOperation typed catch/trampoline，保持 first-match 与元组去重，
生成器复用逻辑异常匹配。跨模块调用也可能抛该异常，因此每个此类 handler 都有额外
EH 元数据和生成体积成本，不能仅在导入 io 的模块展开；保留既有 20/21 项 fat EH 边界门禁。
这不是一般多重继承或动态 except 支持；多参数 errno/strerror 构造仍明确拒绝。

### 源码模块之间的普通运行时基类

`from package import Base as Alias; class Child(Alias)` 与 `class Child(module.Base)`
在定义位置读取实际绑定，并由普通类守卫检查完整单继承祖先链，不传播跨模块
`ClassId`，也不以导入名猜测类型。别名/模块属性改绑为另一个普通用户类时继承新值；
`from` 旧绑定继续保留原类对象。类装饰器表达式、基类表达式、类体、逆序装饰应用按
既有定义顺序执行；仅全部成功后发布最终公开绑定，失败保留旧绑定及已发生的副作用。
本地继续派生运行时基类的类，也保留动态公开槽和真实基类检查。

首切仅开放已解析模块槽名字或属性基类表达式，普通类保持 `PyClass/PyInstance`
动态表示；直接工厂调用基类、多重继承和类关键字/自定义元类仍未实现。
运行时守卫不调用用户 `hash/eq/repr`，按引用身份检查祖先；非普通值、内建类型、
异常类、Enum、dataclass，以及需要专用入口的原生类均在类体前明确
`NotImplementedError`。已验证的原生 TestCase/Enum 和静态异常类入口保持原能力。
这不是模拟所有 CPython 元类失败时序：例如整数基对象可让 CPython 先执行类体、
最终构造再失败，该路径独立 pending，不冒称原生兼容。

成本是每个类定义一次 O(继承深度) 守卫与引用身份集合分配、以及运行时公开绑定；
没有给每次实例构造/字段读写增加守卫，不宣称比静态定义路径更快。
验收：`m3_7_runtime_bases.py` 与源码 fixtures、`runtime_bases` driver、
`PyRuntimeBasesTests`；特殊基类和非类元类时序各有独立 pending 正例。

## 函数内静态导入与局部注解目标

函数 import 使用已有模块缓存与成功后绑定协议，HIR 保留 Local/Free/ModuleVar 身份，
不增加运行时解释器。AOT 图收集递归发现函数中的静态依赖，但实际初始化仍在执行到
import 时发生；未调用函数的依赖源码也须编译，因此会增加图规模及遇到不支持语法的
机会。含 import 的函数沿当前保守类型策略 Dynamic，新导入局部保留 LoadChecked，
不把成功路径的赋值传播到异常出口。未含导入的静态函数不因此全部动态化；
后续精确类型/确定赋值优化按 M3.10 的直接 typed CIL 目标推进。
详见 [function-imports.md](../function-imports.md)，不支持动态 __import__ 或函数异常别名 catch。

函数内属性/下标注解赋值复用已有目标求值和写入，不执行注解、不为忽略表达式创建
运行时对象。yield 的符号判定与未执行注解不同，仍按固定基线保留生成器身份；
海象等影响绑定而尚未实现的路径明确诊断。未生成无运行时用途的注解闭包元数据，
其 co_freevars 长尾差异明确记录于 [annotation-targets.md](../annotation-targets.md)。

## 大整数字面量的有界正规化

超出小整数表示的 token 在 HIR 转为十进制常量，再复用既有 BigInt 池，不在每次
函数调用重新解析。4300 个有效十进制位是本实现的统一资源边界，包含非十进制输入；
不是宣称与 CPython 非十进制容量策略相同。默认值不受类型注解强制，类型或 i64
范围未证明安全时仅降级该函数，保正确对象/身份；可能损失原来错误静态路径的速度，
不冒称优化。精确范围与验证见 [integer-literals.md](../integer-literals.md)。

## Unicode 属性的数据版本

unicodedata 的 east_asian_width/combining 从官方固定 UCD15.0.0 生成完整码点
区间表，保留原数据与许可/SHA，生成器逐码点对固定解释器验收，不复制 CPython
代码，也不把宿主.NET的不同Unicode版本当成兼容实现。运行时二分范围表返回
缓存字符串/小整数；315/388范围约8.4KB原始整数表，不宣称所有Unicode API已支持。
详情与UTF16既有边界见 [实现记录](../notes/2026-11-01-m3.7-unicodedata.md)。

## textwrap 的独立有限换行实现

wrap/fill 使用固定Unicode码点与词块区间的独立状态机，支持声明的常用空白、
缩进、连字符和长词选项，不复制 CPython 代码或正则。分类复用已固定版本的表，
不依赖.NET宿主Unicode；自定义参数协议和max_lines等未实现功能显式拒绝。
每次调用仍有规范化、码点/边界表和结果分配，只有词块区间避免部分字符串复制，
不宣称速度优于CPython。固定矩阵与穷举摘要锁行为，见
[实现记录](../notes/2026-11-01-m3.7-textwrap-plan.md)。

## 有导出所有权的连续 buffer 视图

memoryview 共享 bytes/bytearray exporter 与偏移，每个切片/cast有独立租约而不是
快照；bytearray改变长度前验证导出锁，同长修改仍即时可见。整数/切片参数能执行
用户代码，读写按各自错误优先级重查活性，切片转换持临时租约并finally释放，
不会用旧存储越界。显式release确定；finalizer使用线程安全计数，仅承担D001允许的
非确定GC收尾，不承诺CPython立即refcount释放。B/c连续布局以外明确拒绝。
复制与迭代仍有输出数组/对象分配，不宣传完整零拷贝或比CPython更快；
详见 [memoryview](../stdlib-memoryview.md)。

内建绑定__len__复用方法对象表示，精确九类容器共享参数校验和实时Len读取，不
改变用户/native协议优先级；method-wrapper仅开放已验证元数据，未绑定descriptor
保持未实现。详见 [长度方法](../notes/2026-11-01-m3.7-builtin-length.md)。


## TypedDict 的有限冷创建与 Ellipsis 单例

TypedDict 通过 HIR 候选和运行时真实基值共同选择有限专用入口，保留条件导入与
TYPE_CHECKING 的可改绑语义，不为通过项目而常量折叠。私有 PyClass 命名空间壳
只用于源序注解求值，完成后复制元数据为独立 PyTypedDictType，再应用装饰器并发布。
这增加类定义的冷分配，不增加普通类/PIC 的热访问检查；total-only 之外的包装器、
多父、前向引用等明确拒绝，完整 pending 不移除。详见 [范围](../typing-typed-dict.md)。
Ellipsis 字面量直接取得单例；同名模块槽仅此名字保 Dynamic，未绑定时才允许单例回退，
已有用户值与外部模块属性语义不变。该窄降级有对象槽成本，不把所有静态模块槽装箱，
不宣称完整内建回退。固定差分与共享路由测试通过，第五轮聚合门禁亦通过；原生固定项目仍4/20，
不以元数据能力数代替项目覆盖。


## 普通类体条件的惰性名称回退

HIR 保存条件与每个方法的独立源码范围，以可能/必定绑定集合表达合流；MIR生成
普通CFG，只在原始类字典缺值时构建模块或内建读取。可选名字命中需Contains+Get
两次字典查询，这是冷类定义成本，不向普通实例/PIC路径增加解释层。
含条件普通类省旧静态dataclass字段后备表，实际annotations字典决定后置应用字段，
未执行分支不被union，删除字典后为空；无需新隐藏拒绝flag。特殊类条件保持E0100。
详见 [类体条件](../class-body-conditions.md)。Str属性结果Dynamic的窄修见
[字符串属性](../string-attributes.md)，承认所属函数仍粗粒度降级，不宣传静态性能保留。

## 有界 Flag 与 mapping 百分号格式

根据用户批准的 ADR-0032，本轮最后两个竖切仅解除已实际遇到的 Flag 与 mapping `%`
阻断，不自动追逐下一传递依赖链。mapping格式单遍处理平衡键，入口资格纯类型槽
快照、每次取键重新执行实际协议；不预先格式化、缓存键值或合并副作用。
宽度、精度及未声明转换仍明确拒绝，详见 [字符串格式](../string-percent.md)。

Flag复用Enum的真实类/成员身份与Finalize ABI，不伪装成int，不把Python规则塞给CIL。
组合首次按单bit定义序扫描、生成名称与成员并缓存，后续同值返回同对象；缓存按实际
遇到的组合增长，不预枚举所有位集合。成员迭代仍需扫描/临时list，BigInteger位运算
保留宽整数；不在普通属性/PIC/算术热路径添加Flag解释。序列物化在既有容器
和迭代器快分支之后增加类枚举分支，复用Enum迭代入口；不宣称测量前加速。只支持已声明的
严格掩码和非负成员子集，auto/IntFlag/边界配置等独立未实现。


## 最终聚合证据

2026-11-01 的 `/tmp/ryupy-m3_7-milestone-final.log` 命令链整体exit0：422 C#、
214固定Python基线、189编译差分（29 ignored）、fmt/clippy/Rust/无更新快照通过；
全20项目5 native/14 unsupported/1 failed，三个指定上游集合9/16/9通过。
progress原生通过；chardet因bytes正则仍失败，按ADR0032不扩大本轮。
没有以25%替代80%声明，M3.11兼容门仍未完成。
