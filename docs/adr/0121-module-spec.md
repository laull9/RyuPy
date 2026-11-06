# ADR0121：真实有限 ModuleSpec 与普通源模块同槽元数据

- 日期：2026-11-05
- 状态：已实现有限范围；B5完整门禁、真实PE/CIL复核与唯一性能矩阵完成，保留性能风险和未支持边界；原docopt入口通过，Markdown仍缺importlib.util，不计Markdown项目通过
- 已验 B4 阶段身份（不是最终 B5 编译器）：CLI SHA256 5c4d808db49dc786af97cdef227a8986196fe5455891e8153bc8aa630bb5e6a1；
  B4 Release runtime 与 frozen-source-004 清单身份见下，B5 runtime 字节不变。
- 最终候选身份：B5 CLI SHA256 f6956679307d0f88483916e5b442d94505d55c3d480e1450e4e973875764ba92；
  frozen-source-005清单SHA256 f56e50346ea2c795bdabe08d02e2cc9c652d581913a62228be34b9ada2f1fbef。
  原项目复核的干净功能提交为889ba9a7f0fcb0036303e45e6c18e7466b1273be，
  tree为6a98990bbc5a99f197955779de9ae4590492cd49；对应冻结源与工具身份如上。

## 来源与选择

ADR0120 的干净原 Markdown 3.5.2 入口仍因缺少 importlib.util 失败。本切先建立该链路
需要的真实规格对象和普通已编译模块元数据，不以空 importlib.util、同名字典或假
SourceFileLoader 绕过导入，也不把 spec 可读算作模块实例创建或执行已完成。

选择复用现有 PyClass/PyInstance、property、对象协议及生成模块槽。独立的完整模块
实例全局会改变当前静态全局布局，不混入本切；逐次读旁表则会使裸名字与外部属性
分离，或把冷初始化成本转到普通函数热路径。本切只在每次真实导入尝试的入口前言
填入四个新动态槽，成功 import 的缓存命中不重复执行前言。实现合同见
[ModuleSpec](../module-spec.md)，标准库入口见[元数据 API](../stdlib-metadata.md)。

## 公开对象与实际消费阶段

importlib.machinery.ModuleSpec 导出稳定真实 PyClass，定义模块为 _frozen_importlib；
实例使用普通 PyInstance。name、loader 为必需位置或关键字参数，origin、loader_state、
is_package 仅关键字。先完成绑定，再按原顺序写字段并消费 is_package 真值；不提前
字符串化其它参数。包构造独立 locations 列表，非包为 None；保存传入对象的原身份。

parent 根据 submodule_search_locations 是否 None 判断包，而不是其真值；普通模块
才消费 name.rpartition。has_location 在真值转换成功后写 _set_fileattr，失败不覆盖
旧值。cached 为惰性路径元数据：显式缓存保引用，None 允许重新计算，已经生成的
缓存不随 origin 重绑自动变化。支持固定 CP3.12、optimize=0、无 pycache_prefix 的
源路径；.pyc 原值保引用，未知后缀返回 None，不检查文件也不创建 pyc。自定义 origin
协议在实际消费时明确拒绝；缺 endswith 的对象在该读取点保留 AttributeError。
本轮实际差分限 Linux，不能由宿主路径分支推断跨平台验收。

repr 顺序读取当前属性，条件和格式化各自消费 getter；locations 与动态类名走真实
空 format 协议，不以 str 替代用户 __format__。eq 忽略 loader_state，先在 try 外
读取 self.locations，再在 try 内依次比较 name、loader、origin、locations、cached、
has_location；短路与 AttributeError 返回 NotImplemented 的边界保留。__hash__ 为
None，实例不可哈希，不能用普通类的默认身份相等和可哈希冒充规格对象。

通用比较 ABI 当前返回 bool，不能保存自定义 __eq__ 的非 bool 原结果；直接字段
出现自定义比较或未知原生比较对象时，仅在真正到达该字段时明确 NI，不提前扫描
后续字段。sealed object() 载体无可变比较协议，保身份比较。普通数据、容器及无
自定义 eq 的对象沿既有比较路径；这不修复全部容器元素比较或描述符/派生长尾。

## 同槽元数据、初始化与资源

包身份使用 resolver 显式 isPackage，经 MIR 和登记入口传递，不从 __init__.py 或
文件名猜测。普通已定位模块每次导入尝试先创建 spec、来源描述 loader、缓存路径与
包 locations，再发布部分初始化缓存并执行入口。入口按原规则重置全局绑定位，在
用户正文前把 __spec__/__loader__/__cached__/__path__ 初值写入真实动态全局槽。

裸名字、函数内全局与外部模块属性读写共享生成槽；__name__/__package__ 的外部读取
也不再遮蔽真实槽的重绑定。__file__ 与 spec.origin、__cached__ 与 spec.cached、
__loader__ 与 spec.loader 初始共享引用；包 __path__ 与 spec.locations 初始为同一
列表。之后重绑互不自动同步，列表原地修改仍共享。非包 __path__ 沿未绑定哨兵报错。
重复成功 import 复用模块和 spec；失败清缓存，重试新建 spec，已成功依赖不移除。

loader 的真实类型为 _ryupy_importlib.CompiledModuleLoader，name/path 描述实际已编译
来源，不提供 create_module、exec_module、get_source。源文件改动不会触发重编译。
resources.files 仍依显式资源登记位置，检查本次 spec/loader 身份和 spec 来源字段，
不由可写 __file__ 猜路径；getter 异常保留。该守卫不声称检查 loader 自身 name/path
任意字段修改，也不是任意 loader 协议或动态资源发现。

HIR 表达元数据初值，类型层固定四槽为 Dynamic；MIR 降为 (str,str)->Dynamic 的专用
运行时调用并验证参数/返回；后端只消费 MIR。普通函数、普通全局的既有静态 ABI
保留，不逐次查元数据旁表。已有源码结构测试覆盖四次调用只在入口及示例 add 保持
Static，真实合成 PE 测试覆盖引用签名与显式包参数；这些不是全程序集热路径审计。

### 旧 PE 兼容：登记位置不等于声明新槽能力

B3 的旧 PE 回归已实际复现：原十参 RegisterResourceLocated 登记了真实来源和包标记，
但旧 PE 没有新四槽；原样 roots.dll 配 B3 runtime 的 resources.files 抛出
AttributeError: module 'left' has no attribute '__spec__'。原错和原流完整保留。

B4 保留旧公开静态 void RegisterResourceLocated 的十参 ABI 和缺属性/资源消费合同，
另增公开静态 void RegisterSourceLocated 显式声明新四槽能力。两入口十参依次为
string name、string package、string diagnosticFile、long locationKind、string
locationValue、bool fileBackedByGlobal、bool isPackage、PyModuleInit init、
PyModuleGet get、PyModuleSet set。内部旧入口 metadataEnabled=false，新入口=true；
Resolve 成功后才替换登记，不从位置或 fileBackedByGlobal 猜新槽能力。
codegen 的真实定位模块走新入口，包标记仍来自 resolver；非资源旧登记和旧 dump
保持各自合同，不改变普通函数/global ABI。新 PE 依赖新 runtime，不声称双向兼容。

B4 定向 20 项 C#、8 项 codegen 已通过，仅作定向证据。旧 A/B3/B4 三部署使用同一
roots.dll，SHA256 为 18dcecf90087f6d8fe5efcf940a31e149527f7d87c05500e480d49d539f744ee；
本轮只新增 B4 JIT，exit0、无超时、stdout 逐字等于原 A、stderr 空，没有重编译旧 PE
或重跑 A/B3。证据在独立 legacy-resource-pe-001/jit-B4.*；旧 summary 和 B3 原错不覆盖。

独立窄审 review/04-b4-registration-review.zh.md 未发现新增阻断，核对八文件与
frozen-source-004 一致，源清单 SHA256 为
8972dd495773ec18a55288eecb7e51e051547493163d49ff0a14f4e8e8028b43；
candidate-release-004 的 B4 Release runtime SHA256 为
c2fb6b3f3b423631ae13eedb246bbc3125b00fba05b15051dccf619b772ca7f8。
独审与旧 PE 窄验不替代完整门禁、原项目或性能验收；B3 不作为最终兼容身份。

## 已建立的阶段证据与原失败

基线 HEAD 为 312c34274fe41e8769e4f17a36ace9f71ca8f96b。B3 Debug runtime SHA256 为
9c26cbe2c3210ebbb5391667870125c61b5afa296db01bac11febab4539b062f，仅标记这一阶段。
52 项定向 C# 全过，不是 52 项新增测试，也不替代正常完整门禁。

五份完整公开源 spec_core、module_metadata、spec_object_protocol、
spec_cached_descriptors、spec_repr_format 的 B3 实际 JIT exit0、stderr 空，stdout
逐字等于各自原 CP3.12.13 golden。此轮复用既有同字节 PE，仅更换 B3 runtime，
没有重新运行 CP 或重新编译 PE；不能描述为最终新版编译器与 runtime 的全新验收。
转正只替换五源首行 pending 注释，仍占一行；其余源、位置 AST 与 golden 不变。

独审发现并保留两项红灯：cached 把应重复读取的 origin/_cached 缓存在局部，首次
错误返回 first.py 对应路径；repr 用 str 代替空 format，错误输出 wrong。B3 修复
原 getter 消费次数及两处空 format，52 项定向及两份完整公开源通过，独审离线复核
原流、来源和同 PE 身份。object() 比较误拒绝的早期失败也保留，不扩大其身份豁免。

两份完整边界不转正：spec_boundaries 实际 JIT exit1，首错为 machinery 缺少
SourceFileLoader；metadata_boundaries 保留 del __spec__/del __loader__ 的 HIR E0100
编译失败，后续运行断言未执行。不能把其中已通过的前半段算成整份源通过。

## B4 完整门禁与 B5 最终窗口

在模块验证 env.sh 环境下正常运行 make check，check-002 exit0，耗时
196.120127859 秒：2281 C#、790 Rust workspace、433 固定 CP 基线、433 cases
selftest、310 compiled e2e 全过，127 ignored 保留。生产/测试输入与
frozen-source-004 一致，清单 SHA256 8972dd495773ec18a55288eecb7e51e051547493163d49ff0a14f4e8e8028b43。
证据为 check-002-summary.json、check-002.before/after.json、check-002.log 及原门禁流；
没有因磁盘阈值中断。门禁运行时 HEAD 仍是 312c34274fe41e8769e4f17a36ace9f71ca8f96b，
这是未提交候选的源码冻结验收，不冒充该旧 HEAD 提交自带新功能。

独立只读复核 review/06-b4-gate-evidence.zh.md 确认计数、10923个生产/测试等输入SHA256
及全部437项逐名状态：仅两延期边界由第一次FAILED转为ignored，310旧ok与125旧ignored
保持；五份主源在本次310通过集合中。两边界CP-only通过不计作编译执行通过。

第一次正常门禁真实失败，不隐藏：2276 C#、433 固定 CP 通过，编译 e2e 为
310 通过、2 失败、125 ignored。原因是两份明确延期的多文件边界仅在入口首行写
pending，而 case.txt 未写 pending=true，框架按默认 false 实际执行。metadata_boundaries
重新编译并在全局 del 处失败；spec_boundaries 重新编译/JIT 并在 SourceFileLoader
处失败。这次执行不能被先前“复用 PE”的记录替代。修正只向两个 case.txt 加入
pending=true，不改完整源、golden 或失败断言；原失败及前后哈希保留在
gate-001-correction.json。check-002 已在修正后完整通过，最终独审复核另记。

阶段通过数不替代此次完整退出；若之后修改生产或测试，旧冻结门禁
只属于旧候选。文档更新与最终生产/测试冻结身份须分开核验，不能把 B3 定向结果
当成新登记能力修复后的完整验收。

B5 在此之后修改类型层，故不能把 B4 的 check-002 直接当作 B5 完整门禁。
frozen-source-005 上在同env.sh环境正常make check，check-003 exit0，耗时
256.1457604839816秒：2281 C#、791 Rust workspace、433固定CP基线、433 cases
selftest、310编译e2e通过，127 ignored。独立review/07-b5-gate-evidence.zh.md
逐名核实437项状态与B4完全相同、五主源通过且两边界ignored；10924生产/测试等
文件与snapshot005一致。559个runtime输入逐文件同B4，正常Release产物也同c2fb6b3f；
旧PE/runtime证据在原范围沿用，没有冒充复跑。完整原流和summary均保留。

## 性能窗口与仍开放的历史成本

唯一正式矩阵已完成，完整绝对值、两配置全部方向与冷Main/启动表见
[ModuleSpec性能报告](../module-spec-performance.md)；下面保留实施与审计阶段的顺序。

P1/P2 准备与语义门已实际通过：4次 CP oracle、1次按A接口构建host，3份真实PE编译、
6份全程序集审计和4次smoke完成，含同旧PE+B4的资源路径；证据在performance/p1-complete.json
与p2-complete.json及各原流。两阶段formal_processes均为0。review/05-performance-b4-cil.zh.md 对43条原始差异
逐项归因后拒绝 B4 进入 P3：内部 ModuleMetadata 初值触发整个 main Dynamic，继而
把 perf_pkg/perf_plain 的4个普通业务全局由 Int64/String 变为 Object，并移除4个
bound Boolean字段；typed getter/setter转换与正文也随之改变。这不是无害token变化，
不因七Hot函数自身签名/CIL稳定或smoke通过而忽略。B4原PE、审计、43条差异和红灯均保留。

B5 仅在 ty/module_metadata.rs 窄豁免编译器合成初值触发的降级，保留用户实际动态
元数据读取导致的降级；29项types与2项真实PE定向测试通过，static-globals-red-001
保留修复前红灯。review/06-b5-static-globals-review.zh.md确认豁免仅限模块main的
内部ModuleMetadata节点，用户ModuleVar/动态locals仍降级；静态审查不替代实际PE布局核验。C# runtime 未变，旧同PE+B4验证与既有runtime测试只在原范围成立。
performance-b5 限定重新生成2份B5 PE、2次审计与2次smoke，CP/A/host/runtime均不重跑；
B5两份新PE、两次审计与两次smoke已实际通过；旧CP/A/host/runtime未重跑。
review/08-performance-b5-cil.zh.md对43项差异逐条归因确认CIL隔离成立，关闭B4普通global
Object化阻断：原88字段全部类型/属性保持，仅新增12个Object metadata槽；6个get/set
保全部旧指令与typed转换、bound flag，3个前言保原指令，仅加四槽清零/初值和四object
局部；Hot差异经精确字段编号映射后完整结构相同，不删指令。Main真实初始化仍在冷边界。
新common PE SHA256 9e6ce5c4b4db39792daac67d24dc1ddde260b8fbf52a8b0c69c5d45ffcfa5983；
API PE SHA256 3d475c1c21e48d88b8d9fc3a1b9d6f2f53ef465ccab3daad96dccc0da9ae9b53。
新增getter分支、资源guard与冷分配是有意成本，不因ABI/CIL隔离就视为零成本。
正式default/禁tier矩阵16个顺序新dotnet进程、336暖样本、16次首次Main全部成功，
两次独立镜像配对，无校准、重试、补样或择优删样。独立证据审查
review/10-b5-performance-evidence.zh.md从336原样本重算中位数与全部配对，
结果与聚合精确一致，并核实原流终态、工具/输入身份和全部checksum。A为旧PE+旧runtime，Bold为同旧PE+
新runtime，Bnew为新PE+新runtime；全部原流保留在performance-b5/formal，聚合SHA256为
a0762a05a7b624e5c1f030d3fb163436e7aa6465eb301a617efac643f9572972。

Bnew真实resources.files由2760降至1296 B/item（-1464B），default两配对时间
-55.40%/-52.79%，禁tier -82.53%/-81.96%；另外六项公共暖路径分配不变。
但Bold仅换runtime的default资源时间+28.89%/+25.01%，禁tier为-6.45%/-4.27%，
该不利尚未归因，不能用Bnew改善抵消。Bnew module_external禁tier +12.62%/+1.31%；
static_calls、module_bare、closure_create等其余全部不利配对留在性能完整报告，不取最好。

共同源首次Main分配两配置均为Bold相对A +7288B、Bnew相对A +17648B，Bnew比Bold
再增10360B，是完整组合路径而非单对象归因。Bnew禁tier首次Main +9.752/+27.017ms、
完整启动 +1.849/+23.912ms；default方向混合，全部原值保留。完整启动含dotnet、装载、
host准备、真实Main和管道传输，不减IPC；Main分配只是当前线程，不冒充整进程总分配。

新API仅Bnew绝对成本、无A兼容基线；构造模块/包592/792 B，cold cached组合1224B、
hot cached 0B、parent192B、repr920B；有限eq默认272B/禁tier224B，不抹配置差异。
样本只有单机Linux .NET JIT、两次独立重复，不声称统计显著、全路径提速或AOT结论。
比较须区分同旧 PE 换 runtime
的兼容成本与新版 PE 四槽前言的实际冷成本，不能用前者代替后者。冷初始化只执行
一次不等于零成本；稳定普通函数 ABI 或某路径分配不增也不意味着时间没有回退。

ADR0120 普通首次 Main 的 default/禁tier +3312/+3056B 保持开放，禁tier 静态调用
+4.21%、keyword +11.15%、bound 默认参数 +4.12%、普通 def 创建 +4.73%、闭包创建
+7.30%、闭包调用 +3.03% 六项不利不删除。ADR0119 函数定义来源 13 项时间不利与冷
分配、ADR0118 repeat/chain 及 ADR0117 四字段 TypedDict +512B 和时间回退，以及
TODO 中其它既有风险均未关闭；新 API 的绝对成本或收益不能抵消这些历史记录。

## 未完成边界与原项目窗口

- 旧失败 PyModule 的 getter 仍连共享静态全局槽，重试改变旧对象可见状态；不对四个
  元数据字段特判冻结快照冒充独立实例全局。完整旧别名隔离另切。
- 全局裸名字 del 保留 HIR E0100，外部模块属性删除仍 NI；删除后的 builtins 回退未接。
- SourceFileLoader、native/旧无来源登记及 __main__ 混合元数据不计兼容；不捏造
  built-in/frozen 规格。__main__ spec/loader/cached 为 None，package 空串对 CP None
  的既有偏差仍保留。缺失 spec/loader 的已登记拒绝不能被计为 CP 合法域支持。
- 自定义 eq 非 bool 结果、未知 origin 协议、完整派生/反射、可写 sys.modules、
  find_spec/module_from_spec/exec_module 与完整 html.parser 均未完成。

2026-11-05 22:44:34 UTC，在干净功能提交
889ba9a7f0fcb0036303e45e6c18e7466b1273be（tree
6a98990bbc5a99f197955779de9ae4590492cd49）上，只运行原 docopt 0.6.2 与
Markdown 3.5.2 两个固定入口；各CP/compile/JIT一次，无重试、无全矩阵/AOT。
CPython 3.12.13、B5 CLI f6956679、runtime c2fb6b3f，完整工具SHA见本ADR与性能报告。

- docopt：CP/compile/JIT均exit0，CP/JIT stdout均16字节并逐字等于原golden，
  JIT stderr空。CP四条上游SyntaxWarning原样保留，共860字节；没有修上游源抹警告。
- Markdown：CP exit0，stdout77字节逐字等于原golden且stderr空；compile exit0；
  JIT exit1、stdout空、stderr54字节，首错仍为
  ModuleNotFoundError: No module named 'importlib.util'。不计项目通过，未追加重试。

独立窗口projects-final-001保全部命令、环境、原stdout/stderr、阶段退出与产物SHA。
前后audit完全相同：63输入、官方原包树与冻结原包树各3187文件、原入口/golden、
20个CP搜索根顺序、各项目唯一编译搜索根、CLI/runtime、HEAD/tree及干净工作树不变。
源树聚合SHA256为93bbcd6bf508ab37acce038fb7b491e50c16472706d2f69dbc3d1cbbfaeb7955。
阶段限时仍为CP30/compile90/JIT30秒，每项目总预算180秒，实际两入口均在预算内。

这是本功能提交的新鲜两入口结果，不沿用ADR0120在cf2e28e的历史结果充数。
ModuleSpec基础不会凭空提供util的查找/创建/执行链；Markdown首错未变如实保留。
不改历史同提交20项完整矩阵或分母，不外推完整importlib、其它平台、AOT或全库性能。
