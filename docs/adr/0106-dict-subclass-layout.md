# ADR0106：独立载体的有限真实 dict 派生

- 日期：2026-11-05
- 状态：有限默认布局已通过语义、性能及完整门禁；固定项目窗口另行登记
- 前置：ADR0104 的真实 list 布局、ADR0105 的 dict 更新基础

## 动机

固定 docopt 0.6.2 原入口在 79a04f7 上只余 `Dict(dict)` 的 E0100，未启动 JIT。其构造器消费 generator pairs，因此先完成 ADR0105，再开放真实布局。本切定义一般字典派生能力，不检查项目名或类名；导入或编译成功不代替项目原入口运行。

## 表示与生命周期

新增 `PyDictInstance : PyInstance`，仅新 wrapper 持有 readonly `PyDict Payload`；保留 sealed PyDict 及普通 PyInstance 字段。公开类型、属性、方法 self、repr 循环 owner 都是 wrapper。re-init 更新同一 payload，不清空旧内容，不断开旧视图或别名。

canonical dict 经已有 runtime-base 与 Validate→Make→Finalize→decorators→publish 路径准入。有限单继承支持其后代、普通业务属性/方法、property、classmethod/staticmethod、custom init/repr/str/missing。custom init 收原实参；根 new 只分配空对象，根 init 才绑定至多一位置源及 kwargs。晚写/删 unsupported 槽先验证，失败不污染原类或版本。与 list/str/namedtuple/异常/Enum/native/dataclass 的混合布局对称拒绝。

核心 len/iter/item/contains/hash/比较/运算/new 槽覆盖、类布局重绑、自定义descriptor与metaclass 等仍明确 NotImplementedError。类/实例 `__dict__` 暂不开放。正常业务方法覆盖不被根操作错误重派发。

## 根协议与有限键域

新根描述符提供构造、len/bool/iter/reversed、getitem/set/del/in、get/pop/setdefault/popitem/clear/copy/update、keys/values/items/fromkeys、eq/ne 及 union。显式 `dict.method(d,...)` 绕业务覆盖；唯一缺键例外是根 getitem 查实际类的 `__missing__`，不查实例同名属性，不把用户 KeyError 当缺键。

fromkeys 绑定实际类，先执行 cls()（保 custom init），再逐键写入。copy 和二元 union 产生精确 dict；原位 union 保 self。dict.__str__ 与 object.__str__ 共享根身份，仍调用公开 repr。

新布局只接受精确 None/string/bool/int/long/BigInteger/double 键。合法 tuple/bytes/用户键明确 NI，不执行其 hash/eq；unhashable 键仍 TypeError。空 pop 沿 CP 先返回 default/KeyError而不哈希。所有根查询局部统一 null→PyNone。精确 dict 的旧业务路径保持原支持域；新增显式根操作以及 exact target 接收 wrapper 来源时做有限域校验，不把旧 raw 自定义键复制/compact/重入宣称为已修复。

默认 iter 核心槽不可覆盖，所以 dict(wrapper)/update(wrapper) 按原存储复制，忽略业务 keys/getitem 名称覆盖。其它来源复用 ADR0105 的流式 pairs/mapping 顺序与失败前缀。scalar 不变量让 wrapper 单次查键保持 O(1)；不以每次扫全表代替设计。

## 视图、迭代与比较

新根视图沿用 PyDictView 载体，增加两个布尔标志区分新协议及可信 scalar 不变量；旧 public 构造路径不变。实际对象分配大小须由同 host 测量，不根据布局猜零成本。

新独立游标读活值，覆盖不失效；大小变化持续 RuntimeError；正常耗尽后永久结束。同尺寸结构替换及比较/成员回调中的结构重入明确 NI，不把旧 Version 规则冒充完整 CP 迭代语义。popitem 用无新字段的尾槽裁剪，总尾扫描线性，不用每次从固定末尾重扫墓碑。

新dict根比较另设线程局部256层守卫，避免两个独立自环载体穿过CLR栈；所有失败路径归还深度。错误为可捕获的RecursionError，文案经CP核对，但此保守阈值不是CP默认递归阈值，也不宣称遵循sys.recursionlimit。

字典内容比较保同身份值短路、用户相等的 Truthy 及反射顺序。表达式 bool ABI 不能返回任意对象时明确 NI。视图 values 按身份，keys/items 同类有限内容比较；跨kind、与set比较、集合算术、mapping元数据仍独立 pending。外部 eq/ne 必须按实际同名槽分派，显式 ne=NI 不能重放 eq。

## 消费者不是统一剥 payload

- 常用动态类型/ABC、len/bool/index/contains/hash、repr、iter/unpack/list.extend/namedtuple/zip_longest 和 call ** 接真实布局。
- JSON 空字典先返回，不调用 items；非空调用公开 items 覆盖，循环保留 wrapper owner。
- Counter 空 update 沿根复制，非空 update/subtract 按公开 items；defaultdict update 按根复制。新来源的自定义目标键域保持明确 NI。
- mappingproxy 的 get/keys/items/copy 等保公开方法委托，下标仍能触发 missing；percent mapping 按下标读取。
- copy/deepcopy、函数字典元数据存储等未闭合算法明确 NI，不悄悄降型丢属性。TypedDict fields 等既有精确存储边界不扩大。dataclass 可变默认拒绝该布局，factory 可返回独立实例。

## 验证与原失败纪律

CP 3.12.13 先冻结合同；原六应用 baseline 均不能通过 dict 基类。候选首次六应用 4过2败：显式两参 super 原本被内建入口忽略参数，视图与set比较为本切暂缓域。保原源/CP golden/JIT失败，分别拆为 pending；主应用改由业务方法返回 `super().get` 检验保存根绑定，主输出 golden 未改。字面量 `{**d}` 也是既有 HIR 独立缺口，原失败保留并单列 pending，不能算原源已过。

独立审查另发现两个真实问题：新视图 foreign ne 分派、共享 bool 与非数字碰撞比较。两项均保原失败源后修复，候选003原源严格对齐；全C#2004通过，六主应用转正前后各6/6通过。不能用主应用通过遮掩独立失败。最终 gate、真实性能及原 docopt 窗口结果在完成后补入本节。

### 真实成本记录

同公共CIL与host，仅替换runtime；32个旧路径对照、28个新wrapper-only案例，各7样本。默认配置四ABBA进程共1288样本全部完成。32项B/op均未变，精确dict视图仍32B/op；wrapper视图公开调用为64B/op（不是零成本替身）。wrapper查键8/256/4096约37–41ns、0B/op，结合单键源码路径未见每次线性扫描；不保证最坏碰撞O(1)。

默认配置普通构造旧run中位106/258ns、新304/296ns，聚合约+65%，但原七样本明显有阶段变化，后段旧85–109ns、新82–97ns。没有JIT事件记录，不能把首A约2.87倍当稳定回退，也不能丢早期样本宣称改善。另有exact8 values约+15%、getitem约+4–5%等不利样本。

额外固定两侧 `DOTNET_TieredCompilation=0`、保持同host/CIL的四ABBA单独归档，不能替代默认或等同最终PGO：普通构造旧130/125ns、新130/143ns，约+7.1%，104B/op不变；exact256 iter-first约+26.2%（默认未同向）、exact list append/pop约+7.3%、ordinary属性约+6.2%。全部32项分配仍未变。保全部run/原样本，不宣称整体加速或零成本。

两轮共2576样本，记录每样本真实迭代/时间/分配/分代GC；未逐级保存中间校准探针，没有CPU affinity，宿主为共享CPU。测量窗口停止其它构建/测试/项目，但不能排除宿主调度。静态结构保持exact dict分支与存储字段，并不等于任意JIT配置无CPU成本。

### 正常完整门禁

正常 `make check` exit0：2004 C#、228 CP、202 e2e，30个原ignored逐名未变；新增六主driver和旧dict_update六主均6/6，HIR100项含新四发布结构测试。runtime最终SHA为72590fd2d4e829698cb129f864145ebf8d7b12a814e82b9cb6d2a36b15331ad3。源码/原失败/CP与JIT/两配置性能均保留，原docopt只在干净提交后单独窗口记录。

### 原 docopt 固定窗口

功能提交c0786d19f1aa581d1b9c47d6e7c4b19e9674d5c6、干净tree70177b1216c33fb9bf3218bc97208c0eb7b07023上，只运行一次未改docopt0.6.2原入口。官方28文件/归档、入口、golden与工具均核hash不变；CP exit0，16字节stdout对齐，原四条SyntaxWarning共732字节及hash与旧窗口一致。compile exit0，原Dict(dict) E0100消失；首次JIT exit1，stdout空，首错docopt.py:466 printable_usage的 `AttributeError: module 're' has no attribute 'split'`（调用链入口10→docopt558→466）。总0.263秒，无超时/重试。

这只证明编译阻断解除，docopt仍不计通过。下一候选为通用re.split，不能按该项目特判。未另跑完整20项目矩阵、AOT或跨平台，不改历史5/20及不同候选的既有单入口记录。result.json SHA256：31c72bd90589931ae3fc17d88c995c304374926f7ee519fe1efdb36f13e033e3。
