# ADR0107：局部绑定与单次扫描的通用正则 split

- 日期：2026-11-05
- 状态：有限域语义、真实性能及正常完整门禁已验；原docopt窗口另记
- 前置：ADR0080/0081/0082 的正则语法与错误合同、ADR0096 的bytes及可变buffer

## 动机与范围

真实dict派生完成后的干净c0786d1，未改docopt0.6.2原入口已编译通过，但JIT在docopt.py:466调用re.split时缺少属性。本切提供一般re.split和Pattern.split，不识别项目名、输入模板或类名，也不以局部API通过代替项目原入口通过。

固定CP3.12.13先发现160项观察，再冻结原stdout并独立verify逐字复核。该参考仅证明CP合同，不算Ryu通过。实现范围为现有str、exact bytes、bytearray与连续B/c memoryview，不顺带开放宽格式/多维buffer、任意exporter、原生子类、完整flags或高阶index描述符。

## 绑定、顺序与错误

模块split参数为pattern、string、maxsplit=0、flags=0；Pattern.split为string、maxsplit=0。CP3.12两者接受对应关键字，但Python函数与C签名在参数总数、缺项、重复和未知关键字的联合错误上不同，因此split使用局部绑定，不能修改共享PyBinder来匹配单一API。

正文顺序为模块Compile、maxsplit的ssize_t转换、subject验证、扫描。Pattern直接从maxsplit开始。普通index一次，原用户异常身份/链和副作用保留。0不设拆分次数上限，正数按分隔匹配而非结果项计数，负数不扫描但仍验证subject。无论text还是bytes都不能将maxsplit缩窄为C int。

普通flags沿用现有Compile支持域。旧Compile对特殊flags、部分非法pattern文案和memoryview pattern的边界不会因新split局部binder而自动闭合。CP合法但未支持的输入必须单列pending，不改CP期望冒充成功。

## 扫描与表示

使用已验证模式的宿主匹配器，独立raw循环沿用空匹配后同位置非空重试算法，不调用.NET Regex.Split。每次有效分隔先追加前缀，再按Python组号插入全部捕获；未参与捕获为None，空捕获保空对象。最后无条件追加尾片。达到正maxsplit后立即停止，不再扫描尾部。

text输出按匹配给出的UTF16范围切原string，空匹配失败后仅跳过真实高低代理对或一个码元，避免拆开astral码点，也不把孤立高代理误跳两个码元。整片text/bytes保原对象身份。输出列表之外不收集完整Match列表或每匹配快照。

mutable/view在index完成后取得一次租约。非负maxsplit仅做一次Latin1宿主编码；扫描期间无Python回调，输出片段直接复制到最终bytes。负maxsplit仍取得并验证租约，但跳过编码与匹配。using/finally释放导出，返回后源可resize且输出不受改写影响。异常释放通过可控局部executor测试，不依赖灾难回溯触发超时。

## 成本与验证边界

旧search/findall/sub不迁移到新循环，不增加旧对象字段；这只是结构事实，不代表JIT或公共入口零CPU成本。预定同宿主、同公共CIL、替换runtime比较旧regex及普通调用sentinel；新split无旧实现，不编造基线，另测不同规模、域、捕获和提前停止的CPU与分配。

29项新增C#专项与全2033项通过。旧runtime三主应用CP/compile成功、JIT均真实缺split；新candidate001首次三主1过2败，失败来自既有非连续view创建NI及匿名父view依赖CP即时回收的D001边界。保留v2原源/golden/原失败后，v3显式保存并release父view、非连续与自动回收依赖独立pending；替换为有效view跨域错误后的release/resize检查。v3三主同candidate001严格CP/JIT3/3通过，不能说v2已经通过。

独立17观察的首次JIT有14项一致、3项旧边界：两项孤立代理源字面量被ruff替换为U+FFFD，另一项非连续view在构造阶段NI。保原源与原流，单独抽14支持合同重新CP先行后严格通过。另用chr真实构造D800/DC00并先核ord，再执行三组split，独立应用也严格一致，证明动态输入的split推进，而非前端字面量修复。C#受控executor另证异常释放与maxsplit立即停止。

旧Compile非法pattern/memoryview pattern与特殊flags、高阶index/bool返回warning、宽或多维view、非连续view构造、原生子类及全局空bytes驻留仍明确边界。三主应用晋升只改pending/ignore元数据，原CP golden不改。完整门禁及成本完成后补入本节，不能把专项数字冒充全门禁通过。

### 实际性能窗口

默认配置同host、同公共CIL，旧/新runtime四进程ABBA；12旧API/普通调用对照，候选另60个split案例，五样本，共840样本，语义checksum与原CP oracle一致。12项旧新B/op完全相同，但时间阶段变化明显，如search_bytes旧A1/A2为5648/2379ns，sub_bytes新B1/B2为160836/98163ns。默认ordinary_method约慢26%、search_str约慢7%的不利值保留，不能只挑表面提速。

因此另立固定两侧DOTNET_TieredCompilation=0的12公共案例四ABBA，240样本，仍用原host/CIL。ordinary_method旧56.3/56.6ns、新57.8/60.1ns，约慢4.4%；search_str约慢1.6%。其它不利为ordinary_attribute约4.0%、ordinary_call约2.2%、sub_str约1.9%、search_miss约1.2%、sub_bytes约0.8%、sub_limited约0.2%；不视为微小差异的统计胜负。两侧分配仍逐例相同，不跨配置混比；固定配置也不等同默认最终PGO。

新split没有旧实现，60例只报告候选绝对值和256/4096规模。4096bytes早停约13096B/op，密集输出631448B/op；str无匹配两规模均464B/op，bytes为1000/8680，bytearray/view为1352/12872，完整适配和不可变输出仍有长度成本。有限样本不证明最坏复杂度，没有整体加速或零CPU代价结论。保全部1080样本、每样本真实迭代/墙钟/CPU/分配/GC及哈希，共享CPU无affinity；测量窗口停其它build/test/project。

### 正常完整门禁

首次正常make check退出0：2033 C#、234 CP基线、205 e2e通过，33 ignored。此前30个ignored逐名完整保留，新增三个是noncontiguous_pending、anonymous_parent_pending与long_tail，不把它们计作支持。三主driver3/3和晋升后三主真实e2e再次通过，原golden保持。

最终runtime SHA256为c0f749627b2d752de558e4c2537771074a684e2b53b23cbe3a710d019d15cd1a；没有重编译新源改变生产后再借用旧测试结果。原失败/CP参考/独审/两配置性能和稳定矩阵均保留，docopt在干净功能提交后单独复测。

### 原docopt唯一新窗口

干净功能提交bd06269f77cd33880f1f394f0de2e9fdd95848e7、tree74ed16d7645a12683565f2b02b9ed6c1d68b5c42，官方docopt0.6.2原入口CP/compile/JIT各执行一次。CP退出0、16字节stdout等于原golden；原四条SyntaxWarning保留，本轮隔离路径较长，stderr840字节，不假称与旧732字节原流相同。compile退出0；JIT退出1、stdout空、stderr58字节为 `AttributeError: 'str' object has no attribute 'partition'`，未提供traceback。

仅只读原源码调用顺序可定位首个partition为docopt.py:197（docopt:559→parse_defaults:458→Option.parse:197），这是静态定位，不冒称运行栈。总0.580秒、无超时或重试；28源码文件、归档、原入口/golden、工具、HEAD/tree/clean及复制runtime前后哈希不变。re.split原首错已解除，docopt仍未通过，下一切片为通用str.partition/rpartition。

此单入口窗口不重写此前e066238完整矩阵，也不把新候选未跑的其它19项当自动重验。完整库、AOT和跨平台均未新增验收。
