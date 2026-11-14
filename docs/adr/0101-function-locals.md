# ADR-0101：普通函数直接locals的有限编译实现

日期：2026-11-05。状态：25项运行时专项、三unique应用、边界与第三次正常完整门禁通过，未发布；项目后续结果另记。

## 真实项目依据

在干净候选8fa7023对binaryornot0.4.4原固定入口执行唯一CP/compile窗口。
锁定官方sdist为371054字节，SHA256为359501dfc9d40632edc9fac890e19542db1a287bbcfa58175b66658392018061；
84归档成员/76普通文件逐字验证，依赖复用已核chardet5.2.0源码。入口、golden、包源码不改。
CP3.12.13 exit0且原golden一致，编译exit1：check.py:23的locals()为E0100；JIT未启动。
helpers.py还有七处普通函数locals用于logger.debug映射，不能把它替换为空映射或省略求值。
其它静态需求不是这次运行首错；不把历史5/20更新为新项目通过。

## 支持合同与分层

仅真实名称解析为builtin的普通def/lambda直接调用。每次函数调用有独立惰性dict缓存，
同一调用多次locals返回同对象；只在调用点刷新，不是自动实时视图，也不是每次新快照。
逐源码槽原始值更新，未绑定/null哨兵删除对应名字，合法None保留。外部额外键保留，
外部dict写入不回写fast locals，值对象身份/可变别名保持，异常不回滚已同步的前槽。

HIR Body.function_locals显式记录真实源码LocalId与CP编译访问顺序，不按名字前缀猜临时槽。
参数顺序为位置、kwonly、*args、**kwargs；赋值RHS先于目标，try正常else先于handler。
纯注解且没有生成读写的名字不占槽；不可达读写仍在表内。隐藏with/异常/求值临时槽排除。
含直接locals的函数局部保Dynamic对象身份；其它静态函数不降级，不全程序堆化或创建解释器帧。
MIR直接读取原始local，不能用会抛UnboundLocalError的普通读取来收集未绑定值。
新增三个显式RuntimeFn并同步CIL引用/类型验证：Validate、Ensure、Sync。后端不解释Python。
无参直调省略Validate和三组实参数组。Validate成功后Ensure一次，再逐名Sync；每个函数只有
一个缓存local，CLR initlocals每次调用清空，递归自然隔离。普通无locals函数无额外缓存槽/helper。

局部删除是这份合同的必要路径：首轮两份CP正例暴露现有裸del目标E0100，原源保留pending，
不删除真实绑定清理断言。仅普通函数Dynamic非cell局部接既有LoadChecked/MakeUnbound：
未绑定删除抛UnboundLocalError，成功后撤确定赋值事实，按多目标次序保已发生副作用。
静态槽、模块/global/nonlocal/cell及嵌套删除形状不借此称完整支持。

## 调用与明确边界

kwargs错误优先arity，非法调用不刷新映射。重复keyword合并错误早于builtin不收keyword；
非string键检查在唯一裸star展开之后。原通用调用器不能正确表达所有分阶段展开，本切不
伪装完整修复：混合位置/star、多个star、任何**后仍有关键字组在MIR明确E0305。
允许唯一裸star的精确list/tuple/bytes/bytearray/range/str；协议iterable明确NI。
最终唯一**仅现有dict/native mapping与有限primitive键，custom mapping/协议键明确NI。
保存builtin后间接调用保持真实函数身份但无帧槽信息，合法调用明确NI，不能返回调用器局部。

module/class、generator、cell/free以及含推导式、class/match、带注解嵌套def的外层函数NI。
CP3.12推导式内联局部与当前独立HIR去糖不同，不能套旧版本规则。sys._getframe/f_locals、
trace/debugger、执行代码与完整builtin fallback不纳入；用户局部/参数/模块同名遮蔽走原调用。

## 字典协议及安全边界

普通碰撞key的hash/eq仍执行；等价key保原key身份，回调能看到部分刷新并改写已处理项。
未绑定槽删除单次查找；按CP3.12压制KeyError，包括用户eq抛的KeyError，其它异常原样传播。
新增DeleteIfPresent避免正常缺键分配异常，不改变一般Delete仍抛KeyError的合同。

只为locals返回的dict选择owner-aware comparer，不给所有dict新增字段或常规mutation分支。
用户hash/eq回调完成后若Version变化，在返回CLR Dictionary前抛NI，避免失效slot/ref再写入。
回调已经完成的结构副作用保留，不回滚；已有值覆盖不改Version仍允许。guard持续适用于
返回dict后续操作，不只限Sync。CP允许这些结构重入，本切明确拒绝，不把NI当作CP一致。
达到原墓碑阈值时，仅有效key全精确str的locals字典才Compact，避免部分搬槽后执行用户hash。
含额外非str key时保墓碑：有内存增长与阈值后扫描成本，不宣称免费兼容或恒定总内存。
普通dict仍原共享comparer/布局与整理规则。

## 性能与验证边界

每次Sync均做真实dict协议；locals专用comparer多版本读取/比较，首次调用另分配comparer。
一次调用O(L)源码槽同步，生成代码也随调用点数×槽数增长，不声称通用性能优化。
新增无locals函数结构不变、同一cache/精确槽序/无参不建数组、真实PE ABI和initlocals验证。
只测暖态分配及独立生成CIL对照，不报吞吐倍率、AOT、跨平台或完整项目矩阵。

固定CP参考包含边界外行为，不将全参考计入实现通过。首C#编译因宿主测试误用私有StarredArg
及旧帧API失败，修测试API后18项通过；扩展结构重入/绑定测试25项通过，原日志保留。
首三unique公开应用仅binding_chain通过；state_identity/collision_contract因裸局部del编译失败，
两原源逐字保pending，生产窄补删除后再定向复验，不改断言规避。

首正常make check通过fmt/clippy/Release/CP228及1840C#，在旧class_if测试停止：测试用locals
充当“未实现内建”哨兵，但此切已提供真实builtin值，Absent读取而未调用时应成功。
独立CP原源严格输出仅bound，与当前候选一致；只将旧测试哨兵改为仍未实现globals，
保全部惰性回退及原输出断言。原源/首门禁失败日志保留，不称首门禁通过。

三unique公开应用最终全部通过（首1/3后两原源定向2/2，再总3/3），精确MIR结构及7项
独立边界/删除测试通过。暖态共享协议1000次实测0字节；实际生成CIL的100次调用×1000循环
对照44,800字节、locals1,855,200字节，两次独立编译/执行记录一致。此差额不是经剖析确认
的单helper成本，不能外推为零分配或吞吐改善。

第二正常门禁再次通过1840C#，在旧getopt_core局部导入负例停止：原index4明确把Name del当
独立未实现语法，而本切已支持普通Dynamic局部删除。原source不改，迁至既有accept_dynamic
验证仍保local.Error动态目标，不能删后继续猜原生异常身份；另留global del的真实拒绝锚点。
前两次失败日志分别保留，最终门禁结果另记，未将中止阶段标成通过。

getopt_core旧case迁移后全部6项通过，新增CP/JIT验证：正常try不读取已删异常绑定，
真正匹配except时才抛UnboundLocalError；这不是移除错误断言。

编译期会为各普通函数扫描源码槽元数据，因此“无locals函数无运行时helper”不等于编译零成本。
去重用FxHashSet而非每个Name线性搜索Vec，输出仍保Vec顺序；仅算法复杂度审查，不报告测得提速。
validate固定ABI名单移到专门模块，保全部已有验证与新三项，避免单文件越600行；不是删除校验。

第三次正常make check exit0：1840项C#、228项CP基线、202项e2e通过及30项原ignored；
fmt/clippy/workspace全部通过。最终日志SHA256：77671360df1937194b3566766ae018c2d63f68aebad206b66c60eb46c4af4663。
前两次原日志保留，未改ignored、项目分母或CP期望。

## 原项目后续唯一窗口

功能提交c1ed0158f7465496f8c8294216877b1a9c160cd2的原binaryornot入口复测：
CP/golden一致、编译成功，JIT首错为bytes缺translate属性（stdout空、原stderr无帧）。
源码与chardet依赖逐文件和前轮证据一致，产物runtime同哈希验证通过；没有重试、改库或加shim。
这确认locals编译阻断已解除，不确认完整binaryornot通过。后续bytes.translate另切；不并入本功能。
