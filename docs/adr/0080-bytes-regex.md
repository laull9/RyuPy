# ADR-0080：不可变 bytes 正则域的分阶段闭合

状态：本地完整验收与独立终审通过；独立提交并制作恢复备份，未发布。

## 背景与范围

候选 `105a826` 的 tabulate 0.9.0 原固定入口在 Linux JIT 返回 bytes 正则未实现错误。
该次错误没有帧，不能据此指定唯一模式或声称已执行后续项目逻辑；原证据见 ADR-0079。
本切实现通用不可变 `PyBytes` 域，不按项目名称、模式文本或入口作特判。

先闭合当前已有的 compile、匹配、查找、捕获、替换、expand 与 escape API，再开放公共入口。
当前原有 API 不包含 split、scanner；本切不追加仅 bytes 可用的实现。
bytearray、memoryview 的实时内容、导出租约与释放语义另行实现，首切在实际消费处明确拒绝，不能复制快照冒充支持。
这不是 chardet 的可变输入支持，也不代表 tabulate 或任何完整项目已通过。

## 表示与性能取舍

Pattern、Match 共用载体并保存真实域及原对象；bytes 执行使用一对一 Latin1 编码，偏移直接对应字节。
公开 pattern、string、捕获和错误 pattern 不暴露内部 Latin1 字符串。全片捕获保原 bytes 身份，局部捕获按需复制。
每次不可变 subject 操作编码一次，不建立文本域的码点偏移表；finditer 按需推进，不预先收集所有匹配。
字节字符分类和忽略大小写沿 ASCII 规则，不借用 .NET 的 Unicode IgnoreCase。
文本执行主体保持原路，但共享载体增加域字段、判断，不能宣称零热成本或已测性能收益。

cache 保留原有 64 项 FIFO 上限并区分文本/字节域，不引入永久无界缓存。
替换模板保持自身 str/bytes 域，不能一律编码或提前拒绝 str 模板。
回调返回片段按 Python 的实际消费阶段检查，不能为提前报错跳过本应执行的后续回调。
用户回调异常保持原对象；所属冷边界自抛错误准备现场帧，不在整个用户正文外增加广泛重包。

## 分阶段验收

1. A：内部 bytes 解析、匹配、捕获、span、原对象身份与惰性迭代；公共 compile 仍拒绝 bytes。
2. B：模板、expand、sub/subn、escape 与局部索引参数转换，锁错误次序和延迟片段检查。
3. C：公共模块/Pattern 分派及域缓存闭合，四个固定应用逐一 CP-first 差分，再运行正常完整门禁。

唯一 CPython 3.12.13 参考已经运行，输出 52 条 JSON（含完成记录），exit 0、stderr 空，耗时约 0.083 秒。
该参考不是 RyuPy 通过记录。在该参考阶段，四应用仅完成源码审查、尚未执行；可变输入及原有语法长尾的正例保持 pending，不计通过。

阶段 A 首次构建运行时成功，但测试编译因不存在的 `PyErrors.RestoreHandler` 报 CS0117，未运行任何测试。
随后仅将测试清理调用改为现有 EnterHandler/LeaveHandler 配对，保留原 token、帧及 context 断言。
修后阶段 A 的 12 项内部专项全部通过，零失败、零跳过；首次编译失败原日志保留。
当时 B/C、四个真实应用及完整门禁仍待完成。
本阶段尚未重新执行固定项目、AOT 或性能测试。

## 阶段 B 的内部结果

两个独立小文件实现 bytes 域模板和替换/参数消费，少量解析规则重复换取旧 text 模板及替换循环原路不变；不是引入全局模板框架。
模板自身拼接与 sub 最终 bytes 拼接分开，保回调结果原引用及最终类型检查顺序。
容量守门只针对真实 CLR Array.MaxLength 外的结果，且在片段类型检查后；该域明确 NI，不设任意更小上限、不捕获 OOM 重试。
非 ASCII str 模板组名及高阶特殊槽仍有明确未实现边界，不能据 bytes 语法闭合外推。

阶段 B 首次构建及专项通过：A 的 12 项加 B 的 16 项，共 28 项、零失败、零跳过。
该阶段公共入口、域缓存及 error/group 参数接线仍待 C，四个真实应用与完整门禁尚未执行。

## 阶段 C 与真实应用

公共 compile/cache、模块及 Pattern 匹配/替换、Match.expand、escape 全链已接线。bytes 组号的超大整数按真实组号范围报 IndexError，不误套 pos/count 的 ssize_t 溢出规则。
public bytes re.error 保原 pattern/pos，按格式化、两次边界转换及原 pos 减法顺序构造位置；原 text 转换边界不顺修。
re 局部绑定段增加 PyTypeError 的现场准备，用户正文仍在 catch 外；共享冷绑定存在 EH 成本，不能称原 text 所有字节或开销不变。
bytes 模式 cache miss 当前为键和构造各做一次 Latin1 编码，保该冷路复制成本，不为减少一次复制扩大本切范围。

C 首次累计 40 项专项通过，零失败、零跳过。四个既定应用逐一 CP-first 首次全部通过，85 标签及完整14条投影记录、9个用户帧无删减；4 次版本 probe 另计。
包括后补大组号、一次 index 及 bytes error 负/大位置和协议顺序断言，已通过同源 CP/JIT，而非仅源码推断。
随后启动首次正常 make check；未执行新固定项目、AOT 或性能组。

首次正常完整门禁已通过：fmt、严格 clippy、workspace、Release 构建、1059 项 C#（零失败/跳过）、228 项固定 CPython 基线、202 项 e2e 及原有30 ignored。
日志 SHA256 为 `e5256227e251823d5f20c396820905313447c65214aa0d16e609ce20b6ea69fa`，status 0；没有快照迁移。
四应用在专项和完整门禁各运行一次，共8个 CP/JIT 应用对照；对应版本 probe 另计，不把专项和门禁重复当成新用例数量。
上述门禁阶段没有再次执行 tabulate 固定入口、AOT 或性能组；历史项目分子和禁用分层 matmul 回归均不因此改变。

## 后续固定 tabulate 入口（2026-11-04）

在本切完整验收和恢复备份完成后，以候选 `25fd4fe3ea78e2787a91bb99a7a583f8310f352e`（tree `3f3db8eb681a3d93ac693abb8754091da755489c`）执行一次原锁定 tabulate 0.9.0 入口。
官方 sdist 的 32 个源码文件、入口和 golden 保持原字节；CLI 从该候选单次准备，exit 0，约 0.133 秒。
CP/编译/JIT 各一次，退出码为 0/0/1，总 2.301199 秒、无 timeout；限额仍为 30/90/30 秒及总180秒，首失败即停。
CP stdout 与原 golden 相等、stderr 空。JIT stdout 空，原错误为：

`NotImplementedError: re: unsupported lookbehind, atomic, conditional, comment or scoped flags`

该原错误无帧，不能指定唯一模式或将静态条件组线索称为运行定位，也不能声称完整项目通过。
原结果 SHA256：`c45ab2a868379483d05e8725a956b5591fdfa50966c240abeaba0c69a1dfdeaf`。
独立复核包含锁定包与源码 exactset、六份原双流、入口/golden、CLI/运行时/解释器及三个编译产物哈希；没有追加运行。
本次只代表未发布候选的 Linux JIT 固定入口结果，未改变历史 5/20、公开 main、AOT 或性能结论。
