# ADR-0099：slice 公共类型的有限原生入口

日期：2026-11-05。状态：21项运行时专项、三unique应用、结构与正常完整门禁通过，本地未发布。

## 背景与范围

ADR0098 的 bytearray 切片赋值基础应用曾因合法 slice(...) 构造器被 E0100 阻断。
原应用逐字保留为 pending。此次复用该失败证据补公开类型入口，不再改写测试。
现有切片语法已创建 PySlice；只开放名字会留下身份、成员、比较和哈希的静默错误，
因此以既有 sealed PySlice 为唯一载体，导出驻留 PyType.Get("slice")，不另包假函数。

支持 slice(stop)、slice(start,stop[,step]) 及保存构造器，所有字段保原身份、不提前调用
整数协议。参数完整求值/展开后，按 keyword 优先于 arity 报错。start/stop/step 只读，
类型不能修改或派生；直接/别名基类都由真实运行时值拒绝。普通、保存及暂停方法调用
统一绑定真实对象；公开 indices、repr、hash 和六比较提供有限根方法入口。

字段 descriptor、__new__、pickle/reduce、完整类反射、方法 descriptor 重绑定暂明确 NI。
继承 str/format/sizeof/dir 等未开放显式方法、doc与方法签名反射也NI；未知属性仍AttributeError。
方法对象按同接收者身份及方法名相等/哈希，repr暂NI，不返回CLR占位。不得称这些合法域为CP非法。

## 语义合同

公共 indices(length) 独立于容器旧 Indices(long)，全程 BigInteger，保留巨大 length
和 step，不做 ssize_t 夹限。先 length 整数协议及非负检查，再 step 及零检查，最后
start/stop；每项只调用一次，失败停止，保用户异常身份与既有帧/context。
负步长省略 stop=-1 与显式 stop=-1 区分。支持精确 int/bool 和普通类型级 __index__；
需 DeprecationWarning 的 bool 返回、property/自定义描述符与未知可调用槽形态仍NI。
公共三元组归一化不宣称修复 list/str/bytes 等内部切片旧 long/协议边界。

比较直接按 start/stop/step 扫描，逐字段身份快判后相等测试，不预筛未访问字段。
开放精确 None/int/bool/float/str/bytes/bytearray 和递归 slice；相同任意字段身份可跳过。
访问非同一用户/容器字段则NI。混合 slice 与带比较槽的普通/异常用户对象或原生包装也NI，
因为当前 MIR 比较 ABI 返回 bool，不能把合法任意对象排序/相等结果偷偷真值化。
显式六比较方法遇非slice返回NotImplemented。域外左右反射/派生优先级不冒兼容。

哈希按 start/stop/step 调用既有 PyHash.Of，每次执行、不缓存用户hash，失败不访问后项。
支持原生标量、递归slice和普通函数/static/class及已核object根hash槽；不可哈希字段仍TypeError。
复杂hash descriptor/可调用槽、tuple/frozenset或未知native字段暂NI，避免嵌套走旧协议误绑定。
守卫在各字段实际访问时执行，不提前筛后字段而丢失先前副作用。支持域混合数值等值slice
有相同哈希，可作dict/set键。沿项目既有政策不承诺CP具体hash值。

## 算法与成本

构造只创建原PySlice，不分配字段tuple或wrapper。比较/hash直接读三字段，无临时序列，
用户hash不能缓存。indices统一采用BigInteger，普通小整数通常使用内联表示，但结果tuple/
装箱仍分配；未为未经测量的快路径复制算法。根方法按名字驻留缓存，bound方法读取创建小包装。
共享类型/调用/比较入口增加常数类型判断，无临时tuple不等于零开销。无吞吐或跨平台性能验收。

## 验证设计与初次结果

固定CPython3.12.13独立参考涵盖字段、宽整数、错误优先级、任意比较返回及hash回调。
复杂富比较/descriptor合法正例保pending，不进通过分母。最多三个公开CP-first应用：
新构造/indices合同、新比较/hash/六用户帧异常链、上轮原始bytearray构造器失败源。
另有只读MIR结构与运行时专项，最后正常make check；不重跑项目、chardet、AOT或性能组。

首次14项C#全通过；公开JIT前独立审源发现直接/暂停方法、不可派生与混合比较四处漏口。
增补首次18项16通过2失败，定位误用模块target捕获协议，已改直接GetAttr；原失败流保留。
随后收紧复杂协议槽边界。不能用GetAttr+CallValue测试替代真正直接/暂停调用验收。
最终结果见验证记录，不改写初次历史。

增补协议守卫后21项C#全部通过。首次公开3应用批次1通过2失败，但误用了旧Release
runtime，整轮不作为当前候选验收；原流保留。构造应用还遇旧delattr内建名称拒绝，
首源另保pending，仅将三字段删除断言迁为显式del属性，不扩大生产delattr。
随后先重建Release、核DLL哈希，再统一复验三应用与结构。

最新Release的有效首轮公开结果2/3：比较/完整异常链与原pending均通过；构造应用到暂停
indices通过后，别名继承被PyRuntimeBases.Validate的旧通用NI提前截断。按真实slice单例
在该守卫补精确TypeError与现场，保其他基类策略，随后只重验该应用及编译结构。

定向复验：包含MakeClass和Validate的同一C#测试1/1通过，最新Release重建后构造应用
CP-first/JIT 1/1、MIR结构首次1/1通过。三个unique应用各通过，非有效首轮3/3。
MIR证明保存slice为真实TypeObject，直调仍DynBuiltin且四项回调不被删除，用户参数同名绑定不改。

首次正常完整make check exit0：fmt、严格clippy、workspace通过，1792项C#零失败跳过、固定CP基线228、e2e202及原30 ignored通过。新三应用与结构在门禁中全部通过。日志SHA256 `ddf3714906126f0968454472fc871d704e3c7d87ae34b44bf6090a9266d9d113`。
无新增项目/AOT/性能结论；完整原流和产物哈希归档于六件恢复集。
