# ADR0075：有限 getopt 与函数局部稳定异常导入

状态：本地有限候选，尚未公开发布。阶段 A/B、真实源差分及正常完整门禁本地通过。

## 背景与目标

锁定 tabulate 0.9.0 的原入口在候选 57f857e 编译时报告 `format` 与 `getopt.GetoptError` 不支持。
`getopt` 的三处使用位于 `_main()` 内：函数直属 import、真实 getopt 调用及异常捕获。
固定库入口不调用该 CLI 函数，但编译仍处理函数体；不能通过改上游源码、空模块或只加异常名字绕过。
先前 format 两阶段已独立完成；本切尚未重新运行 tabulate，不据静态接线增加项目通过数。

## 有限真实运行时

实现普通 `getopt(args, shortopts, longopts=[])`、真实 `GetoptError` 与同一类型别名 `error`。
参数扫描域为确切 list/tuple 与确切字符串；未消费时保原 args 身份，消费后采用真实尾切片并保容器类型及元素身份。
longopts 在读取 args 前规范化；支持确切字符串单项或 list/tuple 浅复制，保声明顺序与重复项。
元素不提前检查：仅短项时不检查 longopts 内容，仅长项时才完整扫描候选，然后依精确无参、精确有参、唯一缩写顺序选择。
短选项支持组合、Unicode 码点及粘连/独立参数；首个普通参数或单独 `-` 停止，`--` 消费后停止。
长项独立参数的 `or ''` 真值回调恰一次，短项保原值不测真值；回调前尾切片已经独立，用户异常不重包。
一般输入协议、GNU 扫描、gettext、模块内部 helper、异常用户派生保持明确未支持，不冒完整 getopt。

`GetoptError` 为真实 CLR/Python 异常，父链 Exception→BaseException；构造无论位置或关键词均保存 `(msg, opt)` 两项 args。
msg/opt 不转换，写入不改原 args，删除后回落类默认空字符串；直接 `__str__` 返回原 msg，公开 str 校验真实字符串并保子类对象身份。
args 重赋值、描述符元编程和用户派生未纳入。两个模块异常导出均受保护，避免静态 catch 与运行时属性身份分离。

## 函数局部身份证明

独立函数表只接纳 driver 证明原生来源的函数直属、无条件 import/from-import。
导入须严格先于 handler 所属的函数直属语句，不能仅以字符位置判断 try 内 import 已完成。
完整扫描参数、赋值、删除、循环/with/except/match 绑定、walrus、重复/条件导入、global/nonlocal；捕获 cell 首切保守拒绝。
无效 local 不回退模块或内建异常；元组须全部成立。局部别名不进入模块 exports/protected_exports，不提前执行导入。
同名源码模块仍优先，不能按 getopt 拼写强认原生。普通动态 except 与任意闭包身份传播不在本切。

## 性能取舍

短 cluster 用码点 offset，仅遇带参项才截余串，避免无参组合逐字符复制尾串。
args 真实切片最坏累计 O(n²)，保长参数回调修改原 list 时的隔离；不能仅因 exact list 就改成读取原 list 的游标。
长项完整扫描保重复及晚错误顺序，不去重、不缓存可变声明，不声称性能测量或零额外分配。
绑定错误的原选择仍由既有 binder 决定；本模块专属冷 catch 仅校正完整匹配的两种既有文本差异，不改共享 binder 或用户正文。

## 当前验证记录

唯一固定 CPython 3.12.13 参考仅运行默认 matrix，70 行原 stdout，exit 0、stderr 空、0.073664 秒，无超时；28 秒 TERM 加 2 秒清理宽限。
脚本 SHA256 `6117d1ee0021acf3ca4ae183189e598c3c7fe1281fc0ee3863cd69253b790f8a`，stdout SHA256 `7b7bced1a59487986391739597c12a832d96332d0d08966a9e2fab8ee805af46`。
三个独立链模式未在参考阶段执行；一般 iterable 的 CP 原行为只用于边界，不算候选支持。

阶段 A 首次 C# 专项为 19/21：两项失败均是共享 binder 原诊断文本（缺两个参数用逗号而非 and、带默认值多参未显示范围）；同时测试有一处可空警告。
只在两个新入口绑定阶段精确修正文案，测试用真实类型断言消除警告，保原期望不放宽。
第二次 C# 专项 21/21，零失败/跳过，编译无 warning/error；真实异常 PE 引用 1/1、HIR 原生别名及派生拒绝 1/1 首次通过。
上述不是完整门禁或项目验收，也不含 AOT、性能组或远端发布。

阶段 B 首次 HIR 局部证明 4/4，通过独立函数表、支配和失效负例（阶段 A 的另 1 项不重复计入）。
五项真实源 driver 首轮 2/5：局部导入与长参数回调完整链通过；解析/对象两项分别遇既有内建 TypeError 的 `__module__` 未暴露及新 GetoptError 的编译方法调用 `__str__` 路径缺口。
解析错误 cause 链在 CP 原流前置检查即失败：固定期待误加中间异常的 module 帧，该项尚未运行 JIT；不能据此称本轮再次实证 runtime 嵌套捕获旧债。
原始流与失败现场已保留；当时待独立审核测试边界与真实方法接线，未把失败算作通过。


后续窄修只让真实 PyGetoptError 进入既有 CallMethod 属性分派，不改变其它 native 异常或 PIC 策略；原 C# Fact 补 PIC/captured/prepared/saved receiver、实例属性覆盖与隐式str区分。
普通内建异常 __module__ 探针独立保pending，活动脚本仍锁GetoptError自身模块身份、全部错误文本及直接__str__。
解析错误链改模块捕获再显式cause包装，完整保三段和所有用户帧；原源与首错误期待保留。此选择依据既有format历史，不以未执行的首轮JIT推断新失败。
第三次C#专项21/21，第二次driver五项全部CP/JIT通过；局部导入额外编译阴影负例亦保持。当时完整门禁正在运行。

首轮完整门禁在strict clippy发现新PE测试单元素for，尚未进入全套测试。仅以普通作用域块和固定name代替该循环，保所有PE引用断言、不加allow或生产变更；第二轮门禁随后遇到测试签名的type_complexity lint。

第二轮strict clippy唯一失败是driver的嵌套链预期类型；仅提取私有ChainOracle生命周期类型别名，保值、路径与全部原流断言，不加allow。第三轮正常完整门禁随后通过。

最终第三轮 `make check` exit0：fmt、严格clippy、Release构建零warning/error、920项C#（零失败/跳过）、固定CPython3.12.13基线228项、workspace及e2e202项通过，30项原ignored保留；五项getopt差分再次通过。日志SHA256 `8ed26ccef5f36f563b14f37846028e807d63b8df7bf8bf485d6bb46df7fd9efe`。无快照/workflow变更，无新项目、AOT或性能运行；本地候选尚未公开发布。
