# 通用 str.format 四源合同

首次四源CP全部成功；JIT的report和完整异常链通过，binding_identity和protocol暴露下述旧域首败。当前两个窄迁候选尚未复验。四个 `.py` 由 `crates/ryupy-driver/tests/string_format.rs` 独立编译差分，不自动宣称普通e2e、项目、AOT或性能通过。每源恰好一个CP应用进程和一个JIT进程；另计每例一个固定CPython版本probe。

- report.py：字段报告、命名/位置/Unicode Nd、转义、有限值规格、nested spec、语法/编号/depth/超界错误与先前副作用。
- protocol.py：真实property/getattr/getitem、转换、late slot、nested spec exact化、重入编号、六处原sentinel身份与短路。
- binding_identity.py：direct/saved/root、正常*list/*tuple/**dict、普通子类覆盖/保存/super/实例遮蔽、真实writer身份及无关exact方法/静态数值MIR。
- traceback.py：完整未捕获module cause→ambient→caller/render/__format__/fail链，以及finally stdout。全部用户帧、行号、消息与cause/context标记参加比较，不移除帧。

唯一前置语言参考是CP3.12.13的178例：源SHA d662d93a8186f363044b161313c59604cf59351cbd0bbbce2fa6de59d21c8680，stdout SHA 99dd83689d2cc2e4c4b9fdd40616689ce0bd32b75b129061d379cb0c449fa704。该参考是设计证据，不是本目录四源已跑记录。测试不依赖临时绝对路径或parent私有参考文件。

空片writer终端资格、nested S spec精确化、数字前缀即时溢出来自固定CPython v3.12.13官方源码：Objects/stringlib/unicode_format.h、Objects/unicodeobject.c、Objects/object.c。源中注释标出这些推导，首次四源CP必须先验证断言，不以目标结果反写参考。

long_tail.py.pending不被driver引用或自动执行；保str子类自定义__format__的正CP期望，现未支持数值presentation等独立边界。正常星号调用不属于此长尾。旧nested-caught EH、具体已证共享CALL失败不由本切放宽。

执行器保存原源码、版本输出、CP/JIT stdout/stderr/exit、构建diagnostics，以及binding应用MIR。创建成功才持有独占tmp Root，成功和失败均保留整个目录、原流和产物供独立复核，不自动删除。完整链仅规范化路径前缀、源码回显/caret，不删用户帧。首次运行和任何复验由父统一窗口安排。

## 首败保留与窄迁范围

- `pending/binding_identity_explicit_super.py.pending`逐字保存原完整CP成功源，SHA 8819efd55f6189a27e662b3ca1ba01102001bfab0bb1b0d30ab39f01e9fa7da9。首次JIT停于模块级显式`super(Template, sub)`，现builtin忽略显式参数并只走零参方法上下文入口。active改为同一个Template的root_format方法内零参super，保原root/saved/override/identity断言；不修改全局super实现。
- `pending/protocol_surrogate_literal.py.pending`逐字保存原完整CP成功源，SHA 3de49bd0e001df44c96956fb8ae6278d8ec4040d8d144e9bb3537b1560763a72。首次JIT停于!a断言；上游ruff把源码孤立代理转为U+FFFD，产物已核。active仅把Converted.__repr__的构造改为`'é😀' + chr(0xD800)`，沿当前UnicodeChr的BMP真实字符路径生成孤代理；完整原!a正期望、é、非BMP、ASCII控制、事件次数和所有其余协议断言不变。没有把CP期望改为U+FFFD，也未削去代理字符覆盖。
- report.py的同一孤代理字面量出现在输入和比较侧，两侧共同替换也可能通过；该断言不证明源码孤代理保真，report未因窄迁改动。真实D800的模板!a行为已有C#运行时专项验证，修改后的protocol将另外验证chr构造的真实输入；二者都不能据此宣称前端源码孤代理literal已兼容。
- 原两份pending不被driver引用；首轮原流及其产物必须继续保留。迁后两个应用需各自重新CP-first，不借首轮CP成功宣称修改源通过。report与完整链不变，不重复运行来扩充通过数。
