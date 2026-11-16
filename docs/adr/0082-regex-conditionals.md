# ADR-0082：闭合捕获与数字前向条件正则

状态：本地候选；283项运行时专项与3项真实CP/JIT应用首次通过，正常完整门禁首次通过。尚未公开发布。

## 背景与取舍

ADR0081的实际原入口定位到tabulate源码767行条件正则定义。此处推进通用`(?(id/name)yes|no)`，不识别项目或模式白名单，不将API验收称为项目通过。

固定CPython3.12.13一次参考保留370条JSON；输入SHA `d3ab306de85b0f80be64c9972cf7e43f694aad1152a29c3be17c9d865f79efdb`，原stdout SHA `ff814c44c5c473ed880fccadeeade2f3657b44d0789f19a041f3b357388338e3`，exit0、空stderr。随后唯一.NET10.0.14后端探针构建/运行均0，总3.241535秒仅作预算记录。手工ASCII译文106条支持候选全部捕获与跨度相等；8条开放捕获对照中4条真实不同：间隔重复的`xacxac`在CP成功/.NET失败，`xacxab`反向。连续重复另4条相同，故既不能透传开放条件，也不能把它恒改false。

探针只是后端参与/回溯状态证据，不代替真实parser、flags或消费者测试。

## 有限支持与边界

- text和exact PyBytes支持已闭合捕获的ASCII编号/名字条件及数字前向条件；编号保持Python源码顺序，条件不占捕获号。名字须已登记，数字前向在完整语法与既有flags检查后验证。
- 两支均可为空、no可省；嵌套条件、普通组内交替、转义和字符类中的竖线分别处理。I/A/X沿既有分支语义，条件本身不是反向引用。
- 当前开放的自身或祖先捕获统一Prepared NotImplementedError，真实CP成功期待保pending，不造错误的匹配结果。后顾、scoped flags等旧边界不扩大。
- text非ASCII条件token沿旧非ASCII名字NI边界，包含CP本应拒绝的Unicode数字/符号；不声称完整Unicode标识符错误分类。bytes高位名字按真实re.error拒绝。
- 全ASCII数字token不截断：4300位以内规范化去零并比较固定MAXGROUPS，普通成功编号不用BigInteger；超过4300位明确NI。这是与固定CP整数转换资源异常不同的极冷边界，未新增实测CP参考，不冒同型错误。非数字非法ASCII名字不受此NI截断。
- pattern长度、256层嵌套、200000译文及2秒匹配预算保持；拆分yes/no不能绕过合并译文长度守卫。

## 实现与性能

唯一生产修改是PyRegexParser。普通Sequence仍保任意交替，条件分支在当前层停止；header按转义token读取，不向.NET透传用户条件名。后端只接内部ryuN名。前向记录惰性分配，未知引用在自有前端报错，不借.NET的ExpressionConditional解释。

没有每次match的新字典、标记组、capture镜像或重试。新增成本包括compile分支检查、条件header/分支字符串、惰性引用记录和后端条件/回溯指令；未测性能，不宣称零成本或更快。原Pattern/Match/替换/bytes执行器与编译器均未改。

旧冷帧测试仅把已支持的closed条件NI例迁为open条件专用NI；atomic/comment/scoped等消息不改，原真实`(?i:a)`错误现场与完整链保留。

## 当前验证

新增运行时数据来自已保存CP原流：200匹配、52错误字段，以及9项Ryu明确边界/恢复实例，共261项；加原22项Unsupported专项，首次283/283通过、零失败跳过。原计划曾将9项边界误计11，实际代码未删测试，以真实283终态为准。

3个真实CP-first/JIT应用首次通过，版本探针另计3次：K1 73标签/200subject锁捕获与组号；K2 22标签锁既有消费者、空匹配推进及身份；K3 53标签锁错误字段和11项完整用户异常链投影。共148标签，不把标签当独立应用。开放组和Unicode正例pending未注册、不计通过。

上述专项阶段完整门禁尚待；未重跑固定项目、AOT或性能组；历史项目分子不变。所有原流、源/工具/产物哈希与独立审记录另存恢复集。

## 最终正常门禁

首次make check退出0：fmt、严格clippy、workspace、Release零warning/error、1342项C#零失败/跳过、228项固定CP基线、202项e2e与原30 ignored通过，无快照迁移。原日志SHA `05419c3c0ae9bb2160b2a7933024c4257b913ec4b8e132b0789acb4339a57ea1`。

三应用在专项及正常门禁各执行一次，共6个CP/JIT应用对（版本探针另计6次），不是6个独立用例。旧scoped-flags真实错误现场和bytes四应用仍由完整门禁回归。该门禁阶段未重跑锁定tabulate入口、AOT、性能或公开发布，项目状态当时保持ADR0081最后失败证据。

## 后续固定项目单入口

最终候选`3f1112918d5d0973a3ef2268924c0a228c236146`（tree `add11014d59d9b1f37102bf583841014f6c08900`）持久备份后，使用原锁定tabulate0.9.0包、32源文件和原入口，各执行一次CP、编译、JIT。CLI准备一次exit0、0.118386秒；三阶段退出0/0/1，总2.200412836秒、无timeout，维持30/90/30阶段及180秒总预算、首失败停止。

CP原stdout等于既有golden且stderr空。新JIT stdout空，原stderr仅`AttributeError: module 'textwrap' has no attribute 'TextWrapper'`，没有调用帧。说明本次已越过原条件正则导入阻断，不能据此指定唯一触发源码位置，也不等于完整表格入口或库通过。

结果SHA `60441e5e4093b56aadcd259c4155617249e752213475531d13aae2d91d10d8de`；独立核验官方tar与32源exactset/逐字hash、入口/golden、工具、六原流及三产物。没有重试、AOT、性能或公开发布，历史项目分子不变。
