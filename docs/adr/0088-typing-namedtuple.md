# ADR 0088：typing.NamedTuple 的有限真实布局与注解

日期：2026-11-04。状态：阶段 A、B0/B1/B2、C-functional、C-members 与 Runtime 元数据内部能力已验；公开转发/注册与编译器接线及五应用已完成，第二次正常完整门禁通过；尚未发布。

## 背景与范围

此前固定 tomli 2.0.1 Linux JIT 入口在候选 dd736 的真实首失败为无法从 typing 导入 NamedTuple，尚未进入 TOML 解析。不将源码中的 Output 类静态线索视为已执行定位，不把导入前置或单入口等同整个项目支持。

计划复用 collections.namedtuple 的真实 tuple 载体、字段布局和构造协议，不把 typing.NamedTuple 简单设为别名。类体最终有序注解、默认值、方法、属性和发布时序须真实闭合；不新增数值 ABI，也不为占位导入提前开放 API。

## 参考与来源修正

固定 CPython 3.12.13 首参考前 10 组完成，第 11 组的来源预期断言失败；随后仅补充第 11–14 组并通过，前 10 组未重跑。两轮原流、退出码及哈希保留，不写成首次全部通过。

CP 3.12 的 typing._caller 优先经 sys._getframemodulename 读取函数的 Module；修改函数 __module__ 会影响功能式 NamedTuple 来源。类定义用当前 globals 的 __name__ 初始化 namespace['__module__']；显式类体覆盖保留。原假设混用了备用 _getframe 路径，已按实际原流修正。现有函数来源前置可复用，不需为此扩大编译器。

参考另确认 fields 两遍消费、最终默认值顺序、新 annotations 字典与 __new__ 共享、真实 ForwardRef、类体普通方法/property、可变 NamedTuple 根与 collections 工厂查找，以及用户注解比较副作用。超出首切输入域不伪造这些协议。

## 阶段 A：内部未求值 ForwardRef

新增独立值与有限语法验证器，首次创建登记一个真实原生类型，不逐实例覆盖类型表；typing 模块尚未导出 ForwardRef 或 NamedTuple。内部 binder 不等于公开构造已支持，type(ref)(...) 明确未实现。

保存原始字符串、module 和 flags 身份；提供真实 type/isinstance、未求值状态、repr、eq/hash。每次创建新对象，不永久 intern 或按文本合并身份。module 首切仅 None 或 exact str；代码对象、求值、元数据修改及复杂反射明确未实现。

验证器只识别有限 ASCII 类型引用，不查名字、不运行下标或 union。以显式栈线性推进，不使用 CLR 递归或正则回溯，不限制长裸名称。真实方括号最多 200 层，第 201 层进入前明确 NotImplementedError；这不是声称等同 CP 的 SyntaxError。复杂合法表达式、Unicode 名称及未识别输入也明确未实现。现运行时缺真实 SyntaxError 家族，因此空串等 CP 非法表达式同样返回明确 NI，不造同名假异常、不错误接受。

长属性/union 组合的 CP 编译资源时点尚未证明完全一致；OOM 不捕获重试，不承诺资源失败等价。普通 tuple 布局、比较、PIC 与哈希热路未增加分支。此阶段无性能测量。

## 验证记录

两生产文件与一测试文件完成独立源码审；字段 Kind→Context 的机械更名包含在实际首次运行字节中。Release 首次专项 15/15 通过，包含真实类型、身份/元数据、混合字典键、有限语法、200/201 层、100000 字符裸名称及自抛 caller/context。仅内部 C# 专项，不冒源级 CP/JIT 应用、完整门禁、NamedTuple 或 tomli 已通过。

阶段 A 完成时的后续计划为冷注解转换、私有类壳的一次终结与受控布局提交，再接普通方法/property、公开注册及真实差分。完成前不更新支持列表。未推送远端，未运行 AOT、性能或项目矩阵。

## 阶段 B0/B1：冷注解与私有布局

冷 NormalizeAnnotation 独立于原泛型 TypeArgument。None/string先真实转换；安全类型、标量和已闭合 typing 表示保原身份。未知用户比较协议、str 子类、复杂 exact tuple 错误等明确 NI，不伪造尚未实现的八个特殊对象；相关 CP 正行为保范围外参考。可变 typing 成员在各自实际消费阶段守卫，Any 提前返回及裸 special 错误先后不被无关后续 type 查询改变。原始 Form 身份来自内部只读 accessor，不以首次可写模块观察值为准。

私壳先 Pending，再准备完整布局、属性及绑定到同一 Owner 的 helpers，准备不提前挂 layout。唯一提交检查状态与 Owner，安装后失效缓存；失败壳不能回落成普通实例。采用现 GuardSpecialNew 的 Special 分支内部检查，不在普通 Invoke 新增无条件门控；普通 Special=false 保原分支，原生特殊类有额外判断与弱表查找成本。object.__new__ 的冷分配入口拒绝 Ready namedtuple 的不安全普通载体，不改变普通类分配。

既有 collections.namedtuple 的公开参数验证与消费正文未改，布局准备/提交内核发生实际重构，不能当纯机械变动。首次相关专项 73/73（B1新8、ForwardRef15、既有namedtuple50）通过；随后冷转换新12与ForwardRef15合计27/27通过。两轮为不同过滤集合，不能相加为100项不同测试。A/B0/B1 累计新增内部测试共35项；公共工厂、类语法、CP/JIT应用及完整门禁仍待。无公开占位注册或项目运行。

## 阶段 B2 与内部功能式工厂

B2 新增真实函数根的内部身份、单父类私壳与有限注解终结；尚不向 typing 导出。类体默认值连续性检查先于注解转换，实际 collections 工厂守卫在转换之后；失败壳不回落普通实例，准备完毕才提交。新 annotations 字典由类和生成 __new__ 共享，空默认值仍为真实 tuple。类 module=None 的 CP 内部 typing 回退仍明确 NI，不错误保存 None；普通成员与编译器语法在后阶段闭合。

首次相关专项 97/98，唯一失败为旧 PyFunction 元数据缺少公开 __class__；并非新增根不是真实函数。单行测试改用已有 PyObject.TypeOf 验实际类型，原完整 C# 期待及 Python 正例保 pending，不修全局函数反射。复验 98/98，其中既有 namedtuple 50 项，本切累计内部新增 48 项。

C-functional 内部实现真实两遍 fields 消费：第一次收集名字，第二次转换注解并填新字典；不将一次性迭代器偷偷物化重放。调用者来源在首次消费前捕获，实际 collections 工厂在注解转换后读取，最终 orig_bases 取当时 typing.NamedTuple。有限 exact 字段与 pair 域、可变 typing 守卫及不支持用户注解比较等边界不伪造协议。新增 15 项内部测试后相关集合首次 113/113；尚未将真实函数根转发到该实现或公开注册，不据此声称源级 NamedTuple、tomli、完整门禁或性能已通过。

## Runtime 类体元数据参考

独立于此前 14 组参考，追加七组固定 CPython 3.12.13 观察，唯一 28 秒 TERM 加 2 秒 KILL 窗口，实际 0.064575 秒、exit 0、空 stderr。证实类体开始前读取当前 globals.__name__，缺失时查 builtins，两处都缺失则 body 尚未执行便 NameError；base/mro 副作用早于初始化。类体删除元数据后的读取沿 globals/builtins 回退，普通类完成与 NamedTuple 缺 module 的 KeyError 不同。该观察不证明普通完成应缓存最初 module，也不等于 RyuPy 已支持 class-body del。后续按全部实际 MakeClass 冷路径作共有窄修，不改 Static 数值/函数 ABI；IO 特殊元数据约束必须保持回归。

现有 class-body del 与自定义可变 builtins 环境模型不在本切新增范围，保真实 CP 正例为 pending；缺 __name__ 的环境继续列为未覆盖；现 getter 没有结构化缺名信号，因此不得按异常类型或消息猜测并改包，getter 自抛异常仍原身份传播。仅真实未绑定哨兵才可明确 NI，不伪称已实现 CP 的 builtins 回退或 NameError。七组原流是合同取证，不是七组 RyuPy 支持结论。

## C-members：内部类体成员

有限 typed 模式现在接入普通函数、staticmethod/classmethod 和 property 读写删。保存的绑定方法保 owner/self/function，属性访问器调用真实用户函数，异常原传播；字段始终只读，没有给 tuple 实例添加字典。普通 collections.namedtuple 保原能力边界，晚类修改仍在写入及版本变化前明确拒绝。可能由外部类后加 descriptor 协议的普通实例常量明确 NI，字段默认值不误拒，保真实 CP 正例为后续。

该阶段新增七项内部测试，相关集合首次 120/120（既有50＋本切新增70）通过；当时公共根尚未转发，类体 metadata/compiler、源应用及完整门禁仍待。只在已有 namedtuple 对象入口判断 typed 模式，不扩普通 tuple 布局或通用实例的 descriptor 热扫描；未做性能测量。


## Runtime 元数据冷接线内部验证

实际 MakeClass 在原冷来源查询点一次初始化 module/qualname，typing.NamedTuple 私壳早分流仍独立取一次，不重复读取。未知内部宿主来源保持旧构造，不猜 __main__；只有真实 PyUnbound 哨兵能明确 NI，getter 的任意异常原身份传播。普通类属性读取保显式覆盖的原对象；非字符串 qualname 在类体完成后的校验点报对应 TypeError，str 子类有限 NI。

IO 派生类共用初始化，定义期 qualname 由原直接 NI 迁为真实默认/覆盖与末期校验，类后 reserved 修改仍拒绝。此为必要共享语义变更，旧 IO 该一项期待迁移，其余原断言保留。新增七项元数据测试后相关集合首次 156/156（上阶段120＋新7＋原IO29），不重复相加累计通过数。仅冷类创建增加来源查询与字典写入，没有新增每实例字段或普通 Invoke 无条件门控；未测性能。编译器隐式 ClassLocal/首 docstring 与公共源应用尚待，未完成全门禁。


## 公开接线与首次源验证

真实根单次转发到功能式实现，typing 模块导出同一函数；ForwardRef 未公开。旧阶段未导出期待按该变化迁移，15个功能式专项改从真实 Root.Invoke 进入。补充四个真实 MakeClass 宿主路径（字符串、用户后代、内建异常派生、异常后代）核一次来源与最终覆盖，相关集合首次160/160；其中新增81项与旧namedtuple50/IO29，不将历次重叠过滤相加。覆盖宿主路径不等于每类别均新跑了独立CP/JIT应用。

HIR以实际MakeClass路径登记隐式 ClassLocal，不新增module/qualname的MIR写入或ABI；仅外层原首字符串降低现Attr作为doc，if及后续字符串不是doc。该共有域包括普通无基类及用户类，并非仅ClassBase::Runtime；Enum/TypedDict分流保原，TypedDict新增HIR断言、Enum沿旧回归而非新增metadata专项。首次HIR6/6（新3＋旧3）通过。

首次driver6/6为五项CP/JIT应用完整原流及一项结构，不是六应用。覆盖功能式两遍、future注解/默认顺序、真实普通成员/property、根别名/来源/失败发布、完整用户异常链；普通shadow类还锁初始metadata与显式覆盖。provider.make实际Dynamic来源结构单独锁，所有成功证据目录保原流/hash，不额外重跑应用。仅Linux JIT源差分，不宣tomli/main/AOT/性能。该阶段 cargo fmt 后启动首次正常 make check，后续终态如下。

首次正常完整门禁已完成 fmt/严格clippy、Release零警告及CP基线228；C#1606/1607唯一失败为旧Quitter测试仍期待普通类显式module写入被忽略。该期待与本切已批准的共有元数据读取变化冲突，详细单项原流另保；当时待独审窄迁后重验，未预标完整门禁通过。

Quitter旧期待已独审test-only窄迁：先保普通默认module，再核显式新字符串Same；Quitter全部setter/repr/delete与普通repr/构造器断言不变。随后Quitter整类10/10通过，随后启动第二次正常 make check，首失败日志不覆盖。


## 正常完整门禁终态

第二次 make check 实际 exit 0，日志 SHA256 `2d12e5b63881daea890c319b5da3fb130a7626511b0a9c6b4bfd1ec4933d06a1`。
fmt/严格clippy、Release构建零警告错误、1607项C#（零失败/跳过）、固定CPython基线228、Rust workspace及e2e202通过＋30原ignored全部完成。本切六项driver在完整门禁中仍通过，未迁移任何快照或削减原判据。
原B2函数反射97/98、首完整门禁旧Quitter期待1606/1607及窄迁原流均保留。当前仅本地兼容切通过，不代表完整typing或tomli入口已通过，也无新AOT/性能结果；matmul禁分层回归旧债未关闭。

## 后续固定 tomli 单入口

最终恢复包持久保存后，在未发布候选 `c4020ab1d71bc09eaf4486f058908ded6780f4b0`（tree `447e4ecf0d475895208d039dd66063009f32f9ef`）唯一重验原 tomli 2.0.1 入口。官方锁定 sdist SHA256 `de526c12914f0c550d15924c62d72abc48d6fe7364aa87328337a31007fe8a4f` 与九源码成员逐字校验，未安装或改源。

一次CLI准备 exit0，0.698071秒；随后CP0、编译0、JIT1，各一次、无超时，合计0.595705秒（CP/编译/JIT限30/90/30、总180秒，首失败停）。CP stdout为原40B golden且stderr空，JIT stdout空，真实stderr只有 `TypeError: startswith() takes exactly one argument (2 given)`，没有帧。错误不再是 NamedTuple 导入阻断，但不能从无帧消息指定唯一调用点，也不能称完整解析或项目通过。

result SHA256 `1cd63e04a348121dc4f4446ab6fe3052b1b0d130166209727977374fda3c574c`；原流、入口/CLI/runtime/解释器与三个编译产物hash独立核验。仅Linux JIT原入口失败记录，不更新历史5/20或外推main、完整库、AOT、性能。
