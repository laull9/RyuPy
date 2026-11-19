# ADR0108：保对象身份的通用字符串 partition

- 日期：2026-11-05
- 状态：有限域、独审与候选002完整门禁通过；性能回退风险未排除并明确记录
- 前置：ADR0063/0089 的真实str布局与方法捕获；D009表示边界保持

## 动机与有限范围

干净bd06269的docopt0.6.2原入口已越过re.split，JIT仍失败于str.partition缺属性。
原stderr没有traceback，docopt.py:197只由只读源码顺序静态定位，不能称运行栈。
本切实现通用partition/rpartition，不识别项目名或模板，不改官方源、入口与golden。
它不是re模块扩展，不顺带修复公共属性捕获或UTF16底层表示的全部长尾。

独立CP3.12.13参考先发现、原流冻结，再第二次严格逐字核验：主160观察和单列pending16，
均exit0且stderr空。此数只证明CP合同，不是RyuPy通过数量。三个实际主用例随后先以
pending落盘并CP发现/复验，主输出175行；另外既有__dict__与D009边界独立pending；独审后另登记类属性捕获的完整CP源。
Unicode原首版完整保留，revision-002只追加非法代理边界候选与合法候选重叠的反例。

## 对象与绑定合同

两个方法都只接受一个位置参数sep，无隐式__str__、__index__或迭代转换。
合法sep是真实str或str子类；空sep总是ValueError("empty separator")，包括空receiver。
根描述符先检查self，再在合法receiver上按keyword、arity、sep类型、空sep顺序验证。
错误标签使用str.partition()/str.rpartition()，保存绑定方法也保相同标签。
局部binder只准备自身创建的错误，不以广catch重新包装用户属性或实参回调异常。

搜索用真实不可变文本载体，但原receiver、原sep引用单独保留。命中返回三项tuple，
中项为原sep对象，含真实子类；两侧为exact str。未命中partition的第0项、rpartition的
第2项保原receiver，含空子类，不先展开载体再返回。空片段复用空串；不把CP单字符缓存
观察推广成所有新片段驻留保证。普通str实例的已有实参前方法捕获、保存方法与暂停调用
应保持；显式str根入口绕过覆盖。各调用形态的通过范围以实际差分为准。

## 搜索结构与Unicode

使用Ordinal IndexOf/LastIndexOf选择最左/最右起点，保重叠，不先split所有片段。
正常UTF16代理对内部不能是命中起点或终点；每个候选只作常数边界检查，失败后继续
前后搜索，不能把首个非法半对候选当成整体未命中。反向缩窗也必须相对完整原text检查
边界，不能把临时span末端制造成合法孤代理。

动态chr构造的孤立高/低代理是本API支持输入，测试不使用会触发旧ruff替换的源字面量。
高低代理分别作为两个Python码点再相邻拼接，与单个astral在现CLR string表示中不可
恢复地区分，仍属D009；本切边界守卫不宣称解决此表示债。

设计不创建全串码点表、不预先扫描逻辑长度、不复制完整receiver，不增加全局缓存。
只在命中后创建必要两侧片段和tuple；不存在旧partition实现可作提速基线。
旧字符串方法分派仍可能因新名字判断产生成本，因此必须另以同CIL公共sentinel比较，
新API仅报告绝对时间/分配及有限规模。不能由代码结构宣称零CPU成本或整体提速。

## 已有证据与待完成门禁

旧ba764f4运行时SHA256 c0f749627b2d752de558e4c2537771074a684e2b53b23cbe3a710d019d15cd1a，
三主CP/compile通过，JIT均缺partition；原stdout/stderr/exit、源与golden完整保留。
独立补充CP30观察覆盖非法候选后合法重叠、继承内建捕获与prepared错误次序，已双跑一致。
首个Release构建零警告/错误，12项新增C#专项全过，三个主应用严格CP/compile/JIT3/3通过。
冻结runtime SHA256为ccd00ba3f35299d89868276cc0faae6eb2cdbceb25411b3f6c88c3966e01ac6d。
独审主30观察CP/compile/JIT严格一致，2012字节stdout、空stderr；已转正三主真实e2e再过3/3。
类捕获独审源CP与编译成功，JIT在第22行首个Text.partition调用的事件断言失败，stdout空。
这确认继承类对象在实参重绑方法期间的旧捕获边界；后续五观察未到达，类覆盖、super与
rpartition后续项不能称已实测失败。原完整源/golden已另列class_capture_pending，独立
CP复验364字节逐字一致，不削断言换取支持。
真实性能回退风险未排除，完整门禁及有界诊断结果见下。
功能提交干净后才复测原docopt，不能提前改变项目通过口径。

### 首轮性能与有界修订

candidate001同host/CIL的默认ABBA900样本及固定tier公共ABBA252样本全部语义/hash通过，
21条旧路径分配逐项相同。默认阶段漂移很大，不能把表面改善当收益；固定tier则出现
find_prepared约+37%、rfind_direct约+16.4%、rfind_saved约+27.3%、rfind_root约+72.9%的
疑点。ordinary_call37/63/35/62ns等进程噪声同时存在，既不直接归因，也不忽略大不利值。
原1152样本及所有不利值完整保留。

非计时FullOpts诊断发现StringMethod从1520增至1702bytes；原FindText725bytes与
PyText.Length66bytes未改变主体。静态调用链核出三个新方法正常公开入口不可达的冗余：
StringMethod新增switch臂、ValidatePreparedArity新增签名、PreparedText新增分支。
精确str直接由CallMethod提前进入窄binder；保存/暂停经存在性表创建PyBuiltinMethod，
其Invoke在arity与委托之前截入；根/真实子类经PyStringMethods.Invoke提前截入。
因此候选002仅撤这三处，保存在性表与三个专用入口，不减弱语义检查。

候选002 Release零警告、12C#及三主CP/JIT再次全过；独立原30观察也再次逐字通过。
运行时SHA256为0b253ca9be28a8f22cea38ce1932e4b210a44c73361a3747f2f8dc98e86b7d03。
只复制原host/CIL/oracle而不重编，完整21/69 smoke通过；StringMethod机器码恢复
1520bytes且指令归一化相同。FindText只见错误字符串元数据令牌重定位，主算法相同。
这些只是代码生成证据，不能据此说回退已消除；后续真实复验如下。

### 候选002正常完整门禁

2026-11-05T07:27:14Z至07:29:06Z，首次正常make check退出0：2045 C#、240 CP、
208 e2e通过，36 ignored。此前33个ignored逐名完整保留，新增3个为class_capture_pending、
dict_setup_pending、separate_surrogates_pending；不把pending计作支持。全部原主golden不改。
Release runtime前后仍为0b253ca9be28a8f22cea38ce1932e4b210a44c73361a3747f2f8dc98e86b7d03，
完整日志SHA256 5c6982228de050c2de5cb1bca5887692ce03d31a32257635132cdf1ee262c017。
门禁通过与性能风险是否解除分开判断；未复测原docopt，不计新项目通过。

### 候选002成本复验与停止条件

候选002复用原host/CIL/oracle，不重新编译基准；默认900和固定tier252样本全部值、身份、
checksum及前后hash通过，21条旧路径分配仍逐项完全相同。原四疑点在该轮自身对照中为
find_prepared约+26.3%、rfind_direct约+8.5%、rfind_saved约+3.0%、rfind_root约+43.5%。
这不是跨轮确定改善，不能以候选001更差就宣称优化有效。固定组另有find_direct约+47.8%、
startswith_direct/root约+44%、format_direct约+37.4%等不利；默认也出现普通调用的大漂移。
两候选共2304样本、全部不利项及各A1/B1/B2/A2值保留，不择优配置或删离群点。

最终只再开一个有界诊断窗口：新adapter机械复用原Verify/Bench主体，改变加载与初始化
编排，使A基线、B候选002、C同基线分别处于独立AssemblyLoadContext。原host本身使用
Assembly.LoadFrom/AppContext.BaseDirectory，不能原样放入ALC后假称隔离，因此该诊断
明确不是原host二进制复用。三侧runtime/CIL/adapter同ALC、Default无Ryu、真实CIL静态
槽101/202/303独立读回且恢复、Main各一次与重复smoke126结果均严格验证。固定合法CPU0，
未改系统设置。独立方法审查通过后才运行A/B的ABBA+BAAB及A/C的ABBA，共756样本，
41.30秒完成，全部值/身份/隔离/hash通过，21项B/op十二阶段完全相同。

四个相邻A/B配对中，root rfind依次约-12.4/+40.5/-23.3/+54.2%，find_prepared约
+50.6/+22.7/-32.6/-12.3%；同基线A/C也有ordinary_attribute约+90.0/-40.4%、
ordinary_method约+95.8/-43.3%波动。因此无法可靠把原大百分比归因新增hook，也不能
证明没有回退。固定角色、后置placebo、共享CLR/GC/JIT及新的绑定闭包布局限制均保留；
不使用placebo校正百分比，不用该窗口覆盖原跨进程结果。3060样本完整保留，到此停止
扩展诊断，root rfind/find预备等仍登记为未排除性能回退，项目验收后再独立优化。

新API没有旧实现。候选002未命中/半码点拒绝恒72B，256→4096的early/late结果为
608→8288B，overlap600→8280B，astral632→8312B；子类receiver未命中136B。
4096半码点拒绝约26.85–72.64µs，普通命中/缺失约0.49–2.70µs；拒绝密集非法候选
需要反复边界检查，成本不隐藏。只报告有限样本绝对值，不宣最坏复杂度或整体加速。

### 原docopt唯一新窗口

干净功能提交56aa6c62ba284d2ea46eb9b3dd5f6964ac604299，tree
0a70ea40e78f23c2ccef51a471b1de4d326f05e7，官方docopt0.6.2原入口CP/compile/JIT各一次。
CP退出0，16字节stdout逐字等于原golden，4条原SyntaxWarning保留，本轮隔离路径对应
stderr780字节；compile退出0。JIT退出1，stdout为空，stderr63字节只有
`AttributeError: 'Argument' object has no attribute '__class__'`，没有traceback。

只读源码可静态定位ChildPattern.__repr__中的docopt.py:114，可能经:576→:41→:49→:38
到达，但不能称实测调用栈。总约0.933秒，无超时或重试；28官方源码、归档、入口/golden、
工具、34个冻结副本文件及HEAD/tree/clean前后全部一致，复制runtime仍为0b253ca9…。
partition缺属性已解除，docopt仍未通过。下一通用切片为默认__class__读取，须保对象/类型
身份及用户重写语义，不按Argument类名特判，不把读属性扩成动态改类。

本单入口窗口不改写e066238的完整20项目矩阵，不把其它19项当自动重验；AOT与跨平台未验。
生产提交之后只作本节及TODO/兼容记录的docs-only归档，完整门禁生产源与DLL未变。
