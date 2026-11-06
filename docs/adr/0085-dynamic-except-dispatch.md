# ADR-0085：有限动态 except 的真实类型分派

状态：阶段A 8项、编译库8项和MIR结构1项通过；首次源级应用2/4，修复后四应用4/4及结构1/1通过；第三次正常完整门禁通过。尚未公开发布，不表示tomli通过。

## 背景与范围

锁定tomli2.0.1原入口在候选069f9a9编译时因第8行 `except tomli.TOMLDecodeError` 报E0100；CP成功，JIT未运行。实际类在源码模块定义、包重导出，不能按项目名称加入原生白名单，也不能用可写的 `__module__` 或类显示名代替身份。

本切拟对普通模块/函数中未知异常目标支持Name、从Name开始的属性链与有限tuple表达式。只有异常发生且轮到该handler才求值。含一项动态目标的try整体按源码顺序分派，纯静态try保原typed catch路径，不给全部try增加属性查找或匹配调用。Call/Subscript等未纳入形状及新增动态生成器域须明确诊断，不能静默误编译；现有静态生成器能力保持。

## 固定参考与上下文

固定CPython3.12.13唯一参考35JSON记录（34语义加complete），exit0、stderr空、无timeout；0.033196117秒仅为28秒TERM加2秒KILL预算记录。脚本SHA `0b7775bdf800257c5e94cd43b9015268c887af6826333d00932ba6f93aecae63`，stdout SHA `d7ce82e6b091355cdfbc59ede208267624e46e9d5a779d894758130cc7ce66ee`，已独立核验。

元组所有直接成员必须先验证，再匹配；首项命中不能掩盖后项非法类，嵌套tuple非法，空tuple不匹配。类型表达式在原异常active context中执行；表达式报错不得尝试同try后项，原异常成为新错误context。命中后才写as绑定，所有退出清理已绑定名字；未命中不删原同名变量。整个分派含表达式失败均须恢复外层context，保finally/else/return/break/continue。无匹配直接重抛保存的同一原异常，不重新Prepare或改写已有帧。

该阶段这些仅为设计及CP证据，不表示编译器已实现。裸raise探针只验证身份/ambient，完整链另用未被探测的新异常。纯static既有nested-tuple flatten偏差不在本切扩大修复，不能以新动态matcher的校验冒全部except统一。

## 阶段A运行时与成本

新增纯 `PyErrors.MatchDynamicException(original, target)`，仅验证已求值目标并按真实类身份/祖先匹配，不调用用户构造、属性、eq/hash或通用isinstance。58种既有原生异常以canonical类对象和真实CLR族登记；用户异常按PythonClass引用祖先，保io.UnsupportedOperation额外父边。显示名不能伪装身份。非法目标仅自造准备过的TypeError，正常匹配不修改原异常。

两个运行时新文件与一个测试文件完成独立源审，首次Release专项8/8、零失败跳过，包含58×58原生正反祖先矩阵、用户同名身份、tuple全校验、精确caller17帧与ambient恢复。测试使用既有帧栈串行集合，避免相互污染。当时没有编译器调用点，该结果仅内部能力。

注册表只在matcher首次使用时冷初始化；用户祖先防环使用无分配快慢指针，不为每次匹配建集合。动态try增加EH/冷分派和上下文节点的真实成本，不能声称零JIT或体积影响；纯静态结构需单独回归证明。未运行性能/AOT或项目矩阵。

## 阶段B1：HIR与引用接线（尚不可执行新语法）

HIR两遍判断：仅发现未知handler的try才保留所有非bare目标表达式，纯静态try不新增目标表达式或改变原typed名单。第一遍采用无副作用peek，不把mixed链的导入标为静态受保护，避免错误冻结本应可变的导出。属性形状限定Name起点链；closure/free/nonlocal读取及仅当前函数的生成器边界都有结构测试。MIR RuntimeFn与真实PE引用绑定 `bool PyErrors.MatchDynamicException(object, object)`。

首次库专项在新PE测试调用不存在的dotnetdll显示API时报E0599，未运行测试；改真实signature与parent精确断言后，PE1项通过、HIR5/6，唯一失败是新测试把模块表达式arena误期待为空。实际旧模块固定有三个空字符串metadata初始化；仅把测试改为精确3项及头三个__name__/__package__/__file__赋值，保typed名单和保护集。第三次同一专项PE1/1、HIR6/6、ty1/1通过。两次首失败完整保留，没有放宽真实类型或副作用断言。

此时MIR动态分派尚未接入，禁止执行新增语法目标；不能把HIR/PE通过冒Python应用成功。当时正常完整门禁尚未运行。

## B2：动态分派接线（该阶段尚待应用验证）

独立源审后接入六个文件：混合 try 使用真实目标表达式顺序分派，原异常先完成现有 Prepare/帧恢复，再建立 handler context；目标求值与正文共用清理保护，只有绑定成功的 `as` 名字会删除。未匹配路径保存原异常并在退出 handler context 后直接抛出，不重新创建异常。实际 except 头行仅在动态目标即将求值时发出，静态 handler 原路径不改。

接线后的首次库专项通过 PE 1、HIR 6、ty 1。此结果证明编译与这些结构断言通过，不代表新增 MIR 清理结构专项或真实 CLR 应用已验。当时四个 CP/JIT 应用、第五项 MIR 结构与完整门禁仍待；不新增项目、AOT 或性能执行。接线脚本第一次因本地补丁文件名误写而在读取阶段失败，生产未写入；按实际 `b2.patch` 名称核对原/新 SHA 后只应用一次。

### 首次真实应用验收与两项阻断

第五项未优化 MIR 结构首次通过；四个应用均先通过固定 CPython 原流先验，再各编译/JIT 一次，结果 2/4：完整异常链、目标求值与 tuple 校验通过。cleanup 中 `as` 名字已写入 Unbound，但编译器确定赋值集合仍把它视作已赋值，后续裸读取漏掉检查；这是新增动态清理必须修复的问题，原断言不变。identity 中 `del facade.Error` 在旧 `PyObject.DelAttr` 没有模块分支时先抛 AttributeError，尚未执行原异常的 raise；不能把这个输出解释成同级 handler 错误捕获了目标查找异常。首轮源码、CP/JIT 原流与两个失败保留，后续修复和旧域边界分别审核，不将首轮 2/4 改写为通过。

模块删除原例的四份 Python 源逐字保留为 `tests/fixtures/dynamic_except/pending/module_delete/*.py.pending`；active identity 改为在原异常之后读取实际缺失成员，仍严格锁定查找错误不得由同 try 后项捕获及外层 context 身份。原模块重绑定、同名不同类和重导出案例保持，不宣称模块 `del` 能力。另补同一 dispatch 应用内的未命中前项绑定、try 正文未完成的目标赋值与 handler 正文读取三个边界，不增加独立应用进程。普通局部 `del name` 也属现有 HIR 未支持域，未为本切扩充删除框架。

实际事实修复仅作用于新增动态分派：既定 Dynamic 函数在异常入口撤掉动态局部的确定赋值事实，各 handler 从同一保守状态开始，成功 `as` 绑定只影响该分支，清理后撤销其事实。后继代码也可能因此增加 `LoadChecked`，包含 try 正常落下后的共享路径；不能把全部成本称为仅异常时冷成本。原纯静态 try、数值槽和函数 ABI 不因此统一降级。结构第二轮1/1、四应用第二轮4/4通过，当时完整门禁仍待。

### 局部修复后真实复验

仅 `build_dynamic_try.rs` 调整既定 Dynamic 函数的动态槽事实，没有改原 cleanup 源码或期待，也没有改 pure-static typed catch、数值槽或全局异常框架。第二轮结构1/1、四应用4/4通过，四应用各先固定CP原流守门再编译/JIT；因此累计专项为两个窗口各4个实际CP/JIT应用对，另各有固定版本probe，不把首轮两项失败抹掉。新增三项边界位于原dispatch同一应用，不增加应用数。

三项原先以E0100拒绝动态目标的旧模块来源测试保留输入，迁为真实source模块图、HIR目标身份及MIR匹配结构断言；仅删除两个已失效的负诊断快照，静态别名/保护政策相邻门禁保留。该迁移首编译通过，实际执行随正常完整门禁。当时完整门禁尚未运行，不表示tomli或AOT通过。

首次 `make check` 在 strict clippy 的新 `peek` 局部 `let_and_return` 停止，退出2，尚未进入该门禁的运行时、CP与应用测试。随后仅将同一个 match 直接作为返回表达式，不允许加 lint 豁免或改匹配分支；首日志与后续结果分别记录。

第二次正常完整门禁通过 fmt、strict clippy、Release 构建、固定 CP 基线228项及运行时1466项（零失败/跳过），随后 workspace 的 `getopt_core::local_identity_no_hoist_and_rejected_shadowing` 在旧 `result.is_err()` 期待处失败。此测试仍把新支持的动态异常目标当作必须E0100，不能直接删除测试或泛化成成功；继续盘点所有相关旧negative，保原输入并迁为真实来源/动态目标结构，普通 `del name` 等仍未支持项保持拒绝。当时完整门禁尚未通过，原日志和实际已执行范围保留。

全量静态盘点 driver/HIR 相关拒绝门后，六个测试文件精确迁移：getopt原9例保 `del Error` 拒绝、其余8例及真实源码shadow锁动态身份；zipfile两项、io两项保源码遮蔽而不猜原生类，io运行时基类守卫不变；HIR函数导入两项读同一真实Local槽，star与`del re`仍拒绝。所有输入字符串保留，正常调用/错误链断言不减。集中复验 HIR13+3、getopt1、io2、m3_5五项、zip1及诊断快照1全部通过；不把编译结构案例计为额外CP/JIT应用。

`except 1` 单独实际生成候选后独审精迁：仍E0100、3:8与数字span原样，只有说明句变为准确有限形状，另新增Insta `assertion_line` 元数据。old SHA `45d0829b4ad79fc2c21132a55a50865d7a7f332ec5f895a12b3fdf18fdfae047`，new SHA `857f55b405371cf6d9af366d8f8eedcc87e6393335d3f5e8914ee5bc1857f7be`。其它原生typed HIR/MIR/CIL快照实际保持通过，无批量自动接受。第三次正常完整门禁随后完成，见下文。

## 最终本地验收

第三次 `make check` 退出0：fmt、strict clippy、Release零warning/error、运行时1466项零失败/跳过、固定CP3.12.13基线228项、workspace与e2e202项加30原ignored全部通过。日志SHA `347c3066df6ef76f38837a3be466a6c462fac7b98acf2b5569c5003486bbab56`。纯静态原生异常及数值结构回归保持；这不是性能测量。

新四应用除两个专项窗口各4对外，第二与第三完整门禁也各实际运行4对，合计16个CP/JIT应用对，版本probe另计。完整链只规范既有框架源码回显/空行排版，用户帧、行号、链顺序与消息保留。首次2/4、clippy与旧负门失败以及唯一诊断快照变更均如上保留。未运行新tomli入口、AOT、性能或全项目矩阵；未公开发布，不改变历史项目分子。
