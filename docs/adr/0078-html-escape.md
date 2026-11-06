# ADR 0078：html.escape 有限真实替换协议

状态：源码独立审后已接线；12项runtime专项与3应用CP/JIT通过，正常完整门禁已通过，尚未公开发布。首编译XML错误与首门禁格式失败均保留。

## 动机与范围

锁定 tabulate 0.9.0 在候选4521固定 Linux JIT 入口实际因缺 html 停止，详见ADR0077。源码只使用 html.escape，但本切实现通用API，不按项目、字段或调用行特判；导入解除不代表项目通过。

支持 escape(s, quote=True) 完整位置/关键字绑定。五步依次是 &、<、>、双引号、单引号；前三步成功后才对quote求真值一次，为假不运行后两步。每步重新读取上一步结果的replace，不入口强转str，最终任意对象（包括None）保原身份。动态属性/property、实例callable、普通__getattr__、str子类覆盖沿真实协议。

绑定阶段单独准备错误帧/context；用户replace和truth正文不catch重包。超量位置时先按前两槽验证关键字，保unknown/duplicate顺序，再报告可选参数范围；不更改全局PyBinder。原内部html源码帧不是本实现的Python帧，真实差分只投影实际CP模块路径的固定19行escape帧，完整用户链与原stderr保留。

## 性能与边界

每步仅真实CLR string直接Ordinal.Replace，固定非空ASCII旧/新片段，无PyFunction或实参数组；无匹配保同对象。不合并为单扫描，不unwrap子类。其余值通过真实GetAttr/CallValue，有实际调用和数组成本；普通用户实例在实际消费点增加冷类槽检查，不能称所有路径零成本或已测性能。

保护原生函数对象公开稳定name/qualname/module/defaults/class，模块重绑正常。复杂反射、metadata写入和借类绑定不提供假可写语义。会被现框架错误直接调用的自定义descriptor仅在实际被选择的类槽消费点NI；普通实例覆盖、property replace等保支持。property真值槽、bytes/bytearray replace缺失属于旧失败域，保CP正期望pending，不伪装成当前精确错误一致。

unescape、entities和parser尚未实现。unescape涉及HTML5实体表、最长前缀、numeric无效码点及共享表修改语义，不能五字符反替换冒充。本切没有全局intern或新增用户输入数量上限，不捕获OOM重试。

## 参考与验证记录

固定CPython3.12.13唯一参考以29秒TERM+1秒KILL运行：exit0、0.070912秒、49条JSON含complete、空stderr。源SHA a7fea9b9d4fad7281d79e23c9ee46b3865b27b4a5cfec6bb73a56c7d10e227fc，stdout SHA39b24453da042041eedbe3f8c7fda34ff7769a3ffddee5fdf2a2083a77a6805d，独立审已核。参考证实调用顺序、重绑、终值身份、绑定错误先后及原异常context，不把CP通过计入Ryu通过。

验证范围为12项runtime专项及3个真实CP/JIT应用：文本绑定、动态replace/quote、完整用户context；3次版本探针另计。五份pending保真实CP期待，不注册通过计数。首次runtime编译只报CS1591缺类XML，未进入测试；保原日志，不削警告级别。该阶段尚未重跑tabulate、AOT或性能组。

补类XML注释后runtime首次实际测试12/12通过、零失败跳过。3应用依序首次CP→编译→JIT均通过，文本/协议原双流完整相等，错误组精确投影完整用户链；20+15+17个标签均严格校验，3个版本探针另计。外层600秒包含cargo准备及全部3应用，首失败停，没有单应用独立超时声明。

首正常门禁仅在新Rust测试/support的rustfmt换行检查失败，未继续后续阶段；cargo fmt机械整理后当时第二次正常门禁进行中，不削断言或lint。上述单次应用结果不代表新项目、AOT或性能通过。

第二次正常完整门禁终态exit0：fmt/strict clippy/workspace、994项C#零失败跳过、固定CP基线228、e2e202通过与30原ignored，Release零warning/error。完整log SHA256为 `5d776dfbaa1f83fab165b7afa9697a3d421de358c41402d66d12e64f208bb8d1`。3应用在正常门禁内再次通过，因此本切保存首轮3根及gate3根，不能称总共只执行3应用。五份pending仍不计通过；该门禁阶段尚未重试固定tabulate，不外推main/全HTML库/AOT/性能。

## 后续固定 tabulate 单入口

候选 `2a7a13c7318dc960e0c461f1812214dca6e11ec2`（tree `b1059222c79dbef3b5f1af4ba016c284c7e655cd`）完成一次CLI准备（exit0、0.146535秒），随后只运行一次锁定tabulate0.9.0原入口 CP→编译→JIT。官方81090字节sdist SHA仍为 `0095b12bf5966de529c0feb1fa08671671b3368eec77d7ef7ab114be2c068b3c`，32源码逐成员与归档完全一致。

CP exit0、stdout精确匹配原golden且stderr空；编译exit0；JIT exit1、stdout空，原stderr仅 `ImportError: cannot import name 'zip_longest' from 'itertools'`，无traceback帧。阶段分别0.114642、2.071643、0.166324秒，总2.353882秒，无timeout；保30/90/30阶段及180总限、首失败停，未重试。runner exit0仅记录完成，不是项目通过。

原源码第6行导入早于第52/55/89行namedtuple工厂及后面的escape调用，故只确认本轮错误已不是缺html，不能称项目内这些API已实际执行。独立审核tar/32源码、入口/golden、候选CLI/runtime/解释器、三个产物及六原流SHA全部相符；result SHA `a729626cdc0186ee5f1be10c5c40c118999571d1dc394f6609de5aa876cf1b1a`。该未发布候选固定Linux JIT入口仍失败，不更新历史5/20，不冒main、全库、AOT或性能；本段文档未重新运行完整门禁。
