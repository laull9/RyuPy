# ADR-0084：str.format 的增量模板协议

状态：本地完整验收通过、待提交；内部39项与公共8项合计47专项通过，四应用分别通过，正常完整门禁通过。不是项目通过或公开发布。

## 背景与参考

ADR0083的固定tabulate原入口在运行时遇到无帧的str.format缺失，不能指定唯一调用。本切实现通用模板，不以既有builtin format冒模板，也不改旧f-string。

固定CPython3.12.13一次参考exit0、空stderr、无timeout，178具名观察及完整stdout异常链。输入SHA `d662d93a8186f363044b161313c59604cf59351cbd0bbbce2fa6de59d21c8680`，stdout SHA `99dd83689d2cc2e4c4b9fdd40616689ce0bd32b75b129061d379cb0c449fa704`；0.064200秒仅作预算记录。参考不是Ryu测试通过。

## 模板与对象语义

目标为位置/命名字段、自动编号、属性/索引导航、转义花括号、!s/!r/!a及动态格式规格。逐字段执行，不预验证整个模板而抹去前字段副作用；当前字段外框词法检查与逐步路径查找分开，路径尾错误保之前已发生lookup。自动/手动位置编号互斥，命名字段不切换模式，嵌套规格共享编号状态。

转换先于嵌套规格，再按真实类型级__format__处理最终值。模板复用PyBuiltinFormat内部值入口，规格取exact文本，不调用公开binder构造每字段参数数组。用户属性/下标/转换/格式正文异常不广catch，自造冷错误准备当前帧与context。

引用[固定CP模板源码](https://github.com/python/cpython/blob/v3.12.13/Objects/stringlib/unicode_format.h)及[Unicode writer源码](https://github.com/python/cpython/blob/v3.12.13/Objects/unicodeobject.c)核独立状态规则：非空无字段子类模板、终端单整对象格式结果可保原身份；此前已物化或非终端非空写入不能因后续空字段恢复身份。全空统一exact空串，部分字面/转义需复制。空/S/空等补充组合来自源码推导，非另跑CP参考。内层规格临时结果即使为子类，交外层slot前转exact文本，不能冒公开builtin传入规格的身份合同相同。

## 范围与性能

其他字符串方法/编译器ABI/旧f-string不改。无全局模板缓存，避免保留用户文本和对象；增量游标与有限层规格，不建完整AST。命名字段按实参数组ordinal线性查找，成本O(字段数×关键字数)，常见零关键字路径不建字典。输出延迟物化但不越过可观察身份规则；有真实输出、字段片段和调用端数组成本，未测加速。

Unicode Nd按码点逐位解析及即时long溢出，负号/引号/空格不当Python表达式执行。容量仅沿真实CLR字符串上限，超过明确NI，不新增百万字符人为上限、不捕获OOM重试；CP资源异常差异如实保留。既有UTF16孤代理组合限制不借此宣称修复。

模板值规格沿既有文本/整数及float g/G有限域，其他合法未实现规格、str子类自定义__format__、任意descriptor/native默认协议保边界。format_map、string.Formatter、通用调用展开/异常框架不在本切；正常*与**成功域已由本轮绑定应用验证，不能把它们整体列为不支持。

## 阶段A

4个新内部文件360行与PyBuiltinFormat的单行private→internal接点，未公开str.format。首次Release专项39/39、零失败跳过（21模板/容量、18对象协议），覆盖逐字段错误时点、writer身份、动态槽、用户异常身份与冷错误帧；纯数值容量守门不申请巨额字符串。

该阶段的公共direct/prepared/saved/root/子类接线、四应用和正常完整门禁尚待；未新跑固定项目、AOT或性能组。

## 阶段B接线

四个既有公共接点窄分流format：direct在关键字拒绝前接模板；prepared新增真实存在性/任意位置数并保全部关键字；root与子类在只取raw前保原receiver；根缺self消息局部处理。其他方法绑定不改。首次累计47/47（A39+B8）、零失败跳过；当时四应用及正常门禁仍待，未据此宣称固定项目通过。

## 首次应用反馈

四应用首次2/4：字段报告与完整用户异常链通过，绑定及转换协议在JIT失败；全部CP与构建成功，各应用原流/源码/产物保留。显式super(Template,sub)被旧builtin忽略参数后按零参上下文处理；另protocol的孤代理字面量已在固定ruff解析器替换为U+FFFD，原DLL offset39647的UTF16实字节为é😀UFFFD，并非模板!a损坏真实D800。C#真实孤代理输入已在A专项通过。

随后仅窄迁测试而不扩框架：类内零参super真实验证根方法；repr通过已支持chr(0xD800)构造同一真实输入、保完整!a期望。两个首次CP成功全源另存pending。report双侧同字面量比较不算孤代理literal保真。两个精确复验各1/1通过，连同首次两个成功应用，四应用分别闭合；未重复首次成功的两项。当时正常门禁进行中，不把旧边界改为错误语言期待。

## 最终本地验收

首次正常make check终态exit0：fmt、严格clippy、workspace与Release运行时构建通过（零warning/error），1458项C#零失败跳过、固定CPython3.12.13基线228通过，端到端202通过及30项原ignored。无快照迁移。四应用专项累计6次版本探测/6CP/6构建/6JIT（首4加两个精确复验）；完整门禁再各4次，总计10次版本探测/10CP/10构建/10JIT，4个独立应用。所有首次失败原流与完整正例pending保留，不将重跑次数计为新覆盖。

该完整门禁阶段没有新跑固定tabulate项目、AOT或性能组，未公开发布；模板能力有限，不据此修改历史项目分子。

## 后续固定项目单入口

在本地候选 `6d62580c13bd9c12a49d70ad48bca798525598d1`（tree `222bfdd5f613b74ebf13c265d4f6d434f74ddfa6`）上，仅执行一次锁定tabulate 0.9.0原入口：CLI准备exit0，随后CP0、compile0、JIT0，三阶段均无timeout，原stdout逐字等于既有golden且CP/JIT stderr均空。预算仍30/90/30秒与180秒总窗，实际2.764026秒仅为预算记录；CLI准备0.217122秒单列，不作性能结论。

官方sdist SHA `0095b12bf5966de529c0feb1fa08671671b3368eec77d7ef7ab114be2c068b3c`，32个原源码文件逐成员重核不变；结果记录SHA `56eded5e035ea862dae367da992ae0b60826be25b268e46b60dbecec586787ba`，CP/JIT stdout SHA均为既有golden的 `95db25d24eea531e43aa75dcb084c7422afa0364d4e46ad15c45de813d45cf95`。未改项目源码或入口，未运行全库、AOT、性能组。

这只证明该未发布候选的Linux JIT固定入口成功，不代表main、完整tabulate、全部格式规格或原生能力；历史5/20分子保持原样。
