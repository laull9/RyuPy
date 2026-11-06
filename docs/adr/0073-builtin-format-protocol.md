# ADR-0073：公共 format 的对象协议与分期格式域

状态：有限合同及唯一CP参考已独立审；阶段2已接线；首次Release及27项C#通过，四源差分首次3/4，异常链旧债窄迁后1/1通过；精度守门补齐后第三次正常完整门禁通过，未发布。

## 来自真实入口的需求

候选57f857e的tabulate0.9.0原固定入口CP成功而编译失败：三处format内建未实现及一处
getopt.GetoptError静态异常识别。见compatibility.md的独立证据；修format不等于修全部阻断，
不得将本切测试数或导入成功计为完整项目通过。

## 分域而非暴露旧函数

公共format(value, format_spec='', /)使用现Dynamic内建通路，返回object，保用户__format__
返回的真实str子类身份；不新造RuntimeFn或把任意对象强转CLR string。位置参数、关键字拒绝、
规格类型检查有明确先后。规格为str子类时向用户槽传原对象，不偷偷调用它的str/len/bool。

精确数值/字符串分派与冷类型槽分开；普通类忽略实例同名属性与__getattr__，动态类槽变更可见，
协议一次调用。用户正文不放入本层宽catch；本层新错误准备caller/context。默认object空规格
使用str协议并保返回身份，非空直接TypeError，不能先执行__str__。未知原生默认协议显式拒绝，
不凭普通属性查找猜测特殊槽。

阶段2为协议及有限文本/整数规格；阶段3另审常用浮点规格，尤其g默认有效数字、舍入与阈值。
合法未实现规格与非法规格区分，前者明确NotImplementedError，后者正确ValueError。
不得给合法宽度任意加入一百万上限；实际UTF16结果容量须按平台可表示长度检查，
不捕获OOM后重试、不截断输出，也不以小测试声称巨额分配成功。

## 与旧 f-string 的边界

现FormatValue只是旧f-string子集，有bool规格、整数precision、浮点g默认、文本宽度等已知差异。
本切不将它直接导出，也不顺手改变旧f-string的CLR string ABI。CP单字段f-string能够保
__format__结果子类身份，现路径仍有债；公共builtin完成不能冒称两者已经统一。

现通用CALL还存在重复**键与部分混合*求值时点债，本切不改共享FlattenArgs或全编译调用栈。
活跃测试应锁本切真实支持域，长尾保存CP正期待，不用NI作为CP答案。

## 唯一参考

固定CPython3.12.13，脚本SHA256
`232bc59d0186681131ada48be7d9e264852b9938649916d34b90c1f3eded6756`；
stdout SHA256 `a4a7ead8f4bd6954775d58030a832f397f4e7a88c06df99017e4ea6a3cc35d5a`。
28秒TERM加2秒KILL唯一窗口，实际0.066748秒、exit0、空stderr、无timeout，253案例加done。
这只是CP语言参考，不是候选通过或性能数据。原流已独立核验。

参考确认bool非空宽度按整数，非法s仍报bool类型；int的.2d非法而.2f合法；
文本Unicode十进制宽度/精度及非BMP fill合法。str子类空规格调用__str__，非空s可返回子类；
该条参考未直接检查原对象is；随后活跃protocol差分已补is并通过，不能仅凭原参考type宣称身份已验。

## 性能与验收边界

新公共入口仍经过现有动态实参表示与binder，不声称零分配；精确载体不查询用户类槽，
冷槽不能为便利而引入全局字典或缓存失效体系。二/八/十六进制应避免逐位大整数除法的
二次复制，用实际表示长度驱动的转换设计。未做新性能测量，不关闭既有matmul禁分层回归。
当时尚待隔离源码末审、候选差分及正常完整门禁；不运行项目矩阵、AOT或性能组，不推远端。

## 阶段2首次实际反馈

首HIR全库68/69：旧负例仍期待format(1)报E0100。窄迁保locals/eval原负例并新增注册、别名和
遮蔽结构锁，第二次71/71通过。Release首次构建零warning/error，新27项C#首次全部通过。
四项真实源差分前三项首次通过，含no-op子类is、4300/4301位边界及非十进制精确回读。
异常源CP oracle通过，Ryu内层显式cause额外带module/caller/convert三帧，这是旧nested-caught
全栈快照债，非本切协议重复调用。保原流和完整CP正源pending，不削投影、不改trace框架；
当时活跃链拟将cause建立移至模块级，仍保槽抛外错、finally及完整用户链，修后当时待验。


异常源原字节留在nested_cause.py.pending；新源只把cause建立移至模块级，完整外层链、
finally原输出与原投影不变，定向首次复验1/1通过，其它三源未为此重复运行。
首次正常gate停在rustfmt排版差异，尚未进入其后测试；使用固定工具链cargo fmt后保留原失败，
当时第二次正常gate进行中，不把已过专项冒全量完成。

阶段2最终源码收口：规格为局部struct而非额外heap对象，整数二幂进制按无符号字节线性提取；
整个最终UTF16容量在拼接前检查，真实CLR字符串上限0x3FFFFFDF之外明确NI，普通OOM不捕获。
本层确定不可调用的槽/wrapper/property getter或结果自行准备错误，用户正文仍在catch之外。
默认object域沿继承链排除专门布局。十进制输出默认4300位只作用于新format，不改全局str/repr。


## 阶段2完整门禁终态

第二次正常make check退出0：fmt/严格clippy/workspace、Release零warning/error、
877项C#零失败跳过、固定CP3.12.13基线228通过、RyuPy e2e202通过及30原ignored均绿；
四项新driver在完整gate中也全部通过。原pending不计支持，未改快照、原ignored或AOT工作流。
日志SHA256 `b467f713bf90b30c8dc658f1b85f51640f5b5cd02e4fc5f9d8e62ce6ad90f7cc`。
这是阶段2本地候选结果，不包含阶段3非空浮点格式、再次tabulate项目验证、性能或公开发布。


阶段3只读审又核出一个阶段2冷错误边界：固定CP3.12.13的formatter_unicode.c先校验float规格
类型，再检查precision>INT_MAX并报ValueError("precision too big")，早于NaN/Inf处理。
因此仅真实Float入口补两行Own检查及原Fact中finite/NaN/Inf断言；不把它放进共享Validate，
避免改变尚未支持的int浮点presentation先转换再格式化顺序。
首次接线误取原runtime overlay，runtime-second27/27仅为原版重复，不冒新守门验收；
更正为独立precision补丁后runtime-third27/27真实通过。

修后第三次正常make check退出0，仍为877C#零失败跳过、228CP基线、202e2e+30原ignored，
fmt/严格clippy/workspace及所有新driver通过；源码终态未含任何阶段3g/G实现。
最终日志SHA256 `4c57b08bb00e3c732721a13b33a2a8f53c09b470281d035261bc7c61268eff62`。
原第二次绿色日志只代表该冷补之前的版本，两轮证据不混淆。
