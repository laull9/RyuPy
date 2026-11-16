# 固定码点宽度后顾首切验收

本切只修改 regex parser 冷编译路径，不改 compiler ABI、匹配消费者、Unicode集合翻译、
可变subject活读取、64项缓存、现有翻译/深度限制或2秒匹配预算。

## 合同

支持通用正/负 lookbehind：正文由既有字面量、字符escape、dot、字符类/分类、非捕获组、
连接、等宽分支和固定重复组成。全局 I/A/S/X 与 text/bytes 沿原翻译。宽度按Python码点或
字节，不按UTF16单元；混合 a/😀 的一字符分支合法。空正文、零次数及零宽正文可变量词
按宽度代数准确归零。pos之前的原左上下文仍可见，endpos仍裁右侧。

正文内部捕获、backref、条件、嵌套断言、显式anchor/boundary明确NI，外围原有语法不变。
.NET反向执行会改变重复捕获最后一次的跨度，因此不直接放开这些可观察副作用。
单次计数或最终固定宽度超过int.MaxValue为明确资源NI；正文累计宽度可零乘归零，但
单个超大计数即使随后零乘仍属NI。后顾外部{0}不免除该断言的编译检查。CP极大数的
OverflowError/looks too much behind与完整优先级继续pending。

变宽在整份受支持语法解析成功后，按后顾源码顺序报真正re.error：
look-behind requires fixed-width pattern，pattern/pos/lineno/colno全部None。
只在首次后顾分配宽度状态/list，并用冷BigInteger做加法、分支及重复，避免CLR溢出。
没有从已译巨型正则字符串猜宽度，也没有运行Python源码或逐候选回调的解释器后备。

## 两项共享错误修正

未知ASCII字母escape的text错误定位从字母改为反斜线，涵盖字符类，bytes原已正确；
这一错误与新宽度错误只在创建点PrepareRaise，保caller/context，不扩大Compile catch。
其它旧text re.error现场并未因此全面修复。

有限min>max量词在公共token消费处以Span比较检查，防止后顾宽度错误吞掉外围后续
区间语法错误。覆盖无后顾、后顾前后、前导零、text/bytes。不是全部正则语法错误、
超大数顺序或后端错误消息的全面兼容声明。

## 验收和保留的失败

最终正常make check通过：2474 C#、900非e2e Rust、5清单自检、549 CP、549框架自检、
365编译端到端，188 ignored。源内容/权限在门禁前后完全一致。当前Release runtime
SHA256为 b660980447e38f124fac5f7064c19c5c22eda42c3f3562c609f5388d75fd4661；CLI仍为1757a3f1…030f4db，未改Rust。

16份完整新增Python用例先以固定CPython3.12.13独立参考，再用真实CLI/PE/JIT验证。
7例转正：01固定宽度、03位置/迭代、09原html.parser三份完整模式、10码点、14错误元数据、
15已支持flags/外围组、16二进制活生命周期。转正仅移除pending元数据，AST与golden未变。
其余9例完整保留：02、04、05、06、07、08、11、12、13，不删断言、不改CP oracle。
05仍受旧ascii内建编译限制；12/13还含旧groupindex或Match额外元数据，全部保留，15/16
是独立完整合同场景，没有删掉旧列后把旧例算通过。

原16份清单包含Rust接受、Python清单拒绝的expect_stdout/timeout字段。集成前将两行
改成说明注释，默认exact/30秒不变；逐份用真实Python parse_manifest预检，未改任一
Python执行语句/断言。未通过修改测试框架让清单蒙混过关。

隔离C#定向最终500/500通过，0 skipped。全部7轮真实日志保留：前两轮为TMPDIR/审计
缓存只读、第三轮测试与构建重叠且8失败及2重复参数被过滤、第四轮1个现场失败、第五轮
编译强转错误、第六轮498通过、第七轮补资源守卫后500通过。原失败不作验收证据。
最终孤立代理在Fact体内构造，避免xUnit参数序列化变replacement；未删此覆盖。
定向构建关闭NuGet网络审计，最终canonical门禁采用正常配置。

设计阶段原矩阵2496例为2390一致、106差异。普通域2320一致；D009相邻独立代理域176例
中70一致/106差异，原始CP/.NET结果全部保留。该既有UTF16表示差异仍未修，不能以过滤后
2320结果冒充完整Unicode通过；生产另有真实码点正例及完整相邻代理pending。

## 原项目与后续

最终Release下，原docopt入口编译/JIT通过，16字节golden一致。默认Markdown仍原
find_spec source metadata NI（375字节stderr）。四源窄配置原源未改，已越过后顾编译，
下一错误为sys.modules mutation is not supported（59字节stderr）。这些输入与历史CP
身份均前后核验，项目CP复用不重跑。不是新20项目矩阵，也不把窄配置推进算默认项目通过。

本切未新增性能采样或全DLL/CIL等价审计。普通parser增加冷状态检查及量词区间Span比较，
后顾增加宽度与后端解析成本；匹配消费者源码未改不等于零成本。S1/B2及所有更早不利
观察继续开放，旧runtime性能不能迁移为本runtime无回退结论。AOT、跨平台未验。
