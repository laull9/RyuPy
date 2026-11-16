# ADR-0100：delattr 有限公开入口与删除边界

日期：2026-11-05。状态：23项运行时专项、三unique应用、结构及正常完整门禁通过，本地未发布。

## 依据与范围

两个独立原失败源均因 builtin delattr 缺失被 E0100 拒绝：typing.IO 的只读 property
合同和后续 slice 公开类型合同。此前各自仅改测试写法为有限显式del分支，原源逐字保留。
本切直接include两份原源复验，不更改库源码、历史失败证据或项目分母。

新增HIR Dynamic名称和运行时dispatch，保存值仍是真实PyBuiltinFunction，不新增RuntimeFn。
参数完整求值/展开后按keyword→arity→名称类型检查，严格两个positional-only参数，成功None。
精确str直接使用原文本；已有封闭str子类读取不可变payload，不调用__str__转换。
CP参考确认任意str子类名称可能触发自定义hash；当前str布局已禁止eq/hash覆写，因此不能把
payload路线外推为任意str子类的完整语义，也不能宣称CP从不调用这些协议。

复用普通实例槽、类自身属性及版本失效、普通函数property deleter、已有函数metadata、
slice/typing.IO只读属性和冻结布局检查。实例同名__delattr__普通属性不冒类型槽。
类自身名为__delattr__的属性可删除，因为它管其实例，不能错当元类hook。

## 共有删除守卫

只在公开builtin入口挡未知协议会让显式del继续静默绕过，因此两者共用PyObject.DelAttr的
窄守卫。普通类级__delattr__、一般数据descriptor的__set__/__delete__、未知property fdel形态
明确NI，不假执行或删除自身槽。普通实例和已有用户异常载体均检查类型链，不读实例伪hook。
模块删除需要同步静态全局槽/特殊元数据，本切NI；不只删除DynamicAttributes造假完整模块支持。

原String/Enum/frozen检查搬到守卫，次序不变，不重复执行；冻结拒绝不能被新增NI盖住。
正常property优先于普通保留元数据拒绝。普通__dict__/__class__的完整布局修改、类保留反射元数据、
函数尚未实现defaults/code/globals等仍NI。已有原生类模块元数据与函数module/doc/annotations规则保留。
只支持普通PyFunction deleter；返回值丢弃，用户异常保身份、已有帧，按PrepareRaise补缺失现场/context。

IPyNativeDescriptor只说明已知绑定包装器，不能一概当数据descriptor。新增无状态
IPyNativeDataDescriptor分类，仅现有tuple字段标记为数据；其余已知lru/html/方法包装仍为非数据。
新标记只用于删除守卫，不改绑定读取、MIR/CIL ABI或通用descriptor实现。非数据包装不应阻止
删除实例自己的遮蔽槽；数据字段不能误放行。此处不把未知Python descriptor纳入支持。

PyType代表驻留内建类型，其删除按CP已观测的cannot set…immutable type文案拒绝，
修正slice旧删除文案。对普通class元类或完整反射不作推断。

## 成本与真实性能边界

没有增加实例字段、全局缓存或属性读取/设置热路判断。新公开入口复用实参数组绑定和原删除
分派；原支持显式del仍走同一RuntimeFn。删除普通对象增加类型链hook/属性查找，property随后
会沿既有实现再次查找，存在常数成本；不称零开销。实际槽清理/类版本Touch逻辑不复制，不重建
属性字典、不为了优化省略回调、冻结错误或参数副作用。

计划只测重用目标/名称/实参数组的暖态托管分配，不作为吞吐倍率、完整Python调用零分配或
跨平台性能结论。完整项目矩阵、已过固定入口、NativeAOT和性能组不重复跑。

## 验证过程

固定CPython3.12.13参考原源/双流保留，包含未实现hook/descriptor/模块域，不能把参考都计通过。
18项初次C#公开入口专项通过。独立源审发现冻结/保留名优先级、非数据native包装误拒及
用户异常descriptor漏检，已修并准备定向锚点。

新公开fixture在CP冻结阶段两次因动态property错误全文断言失败，未开始JIT，原失败保留。
仅改为Box类体@property取得真实名称绑定，完整消息断言不降级；第三次CP冻结成功，
预期uncaught cause/context及全部六用户帧成立。该调整独立审源，不扩property __set_name__框架。
三公开应用中的另两项逐字include旧失败源，另有一项只编译结构测试，不增加应用分母。
动态赋值property的__set_name__名称记录仍是既有边界，缺deleter诊断不能据本切视为完整对齐；
本切保类体命名的只读property合同，不通过删消息断言掩盖它。

增补23项专项首21/23，两个失败为宿主测试构造/身份断言：namedtuple明确module后避无Python帧，
类.__dict__须查原始字典成员而非把类属性映射当作property。两项仅测试修正后定向2/2通过，
其余断言不删。暖态直调及保存函数各1000次set+delete实测32,000字节，包含旧设置/调用器，
尚未归因，不声称新删除守卫分配32字节或无分配。

公开首轮2/3：两份历史原失败源通过。新应用在模块未绑定delattr的builtin fallback处NI，
未进入其余合同，属于既有完整fallback边界；原CP成功/失败JIT源逐字保pending。
仅改为已绑定suspend_delete模块别名，仍在yield后重绑并验证callee捕获，saved保持原builtin
供异常链。没有实现fallback或削除该观察；独立核diff后定向重验新应用及结构。
最终正常完整门禁与恢复验证待后续记录。

首次正常make check的fmt/clippy、Release及CP228通过，C#1814/1815，唯一失败为新增
保留名NI盖住typing.IO普通派生的__module__既有TypeError。未修改旧测试，守卫已按IO族
保留原错误规则，并把所有类__doc__置于未开放反射域，防止native类被误删普通字典项。
该轮控制测量：shared set+delete为0字节，direct/saved各32,000字节/1000次。只证明所测
暖态组合路径公开调用器有额外分配，未用IL/分配剖析确定来源，不声称吞吐提升。

IO守卫修后24项相关定向全部通过，随后完成正常完整门禁。
第二次正常make check exit0：fmt、严格clippy、Release、workspace通过，1815项C#零失败跳过、固定CP基线228、e2e202及原30 ignored全部通过。三项新公开差分及结构在门禁中亦通过。最终日志SHA256 `13bd3f9487fee86a66ecdf804639a328a2d9f3cd613b97206e1bd59e74736865`。
原失败、测试窄修、所有原流与分配JSON保留，无新增项目/AOT/吞吐结论。
