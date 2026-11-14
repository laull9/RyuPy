# ADR-0072：用户 hash 返回归约与必要整数表示一致性

状态：有限合同及唯一CP参考已独立审核；三runtime文件已接线，首编译类型转换已窄修；新11项C#及原16项同批通过，新3应用及原4应用通过，首次正常完整门禁通过，未发布。

## 两种不同语义

修复前普通/通用用户 __hash__ 把结果直接拆为Int64，因而漏掉 -1 保留值规范化，
超 i64 的合法整数结果报溢出，非整数也不是CP的专用错误。本切不宣称旧代码曾调用
__int__/__index__；PyCast.ToInt64 本来就严格检查实际整数表示。

用户函数返回实际 bool/int/long/BigInteger：可容纳 i64 时保原值，只把 -1 改为 -2；
超 i64 时按有符号整数hash归约。其它类型报 `__hash__ method should return an integer`，
本层新错准备 caller/context；用户hash只调用一次，其正文异常不catch、不重包。
返回值的 __int__/__index__/__hash__ 不能被额外调用。

这与整数本身的hash不同：整数自身按 P=2^61−1 归约，再规范 -1。
用户函数直接返回 P 时 CP hash(obj)=P，但 hash(P)=0；不能混用两个辅助器。

## 必要数值一致性闭包

仅修用户返回会新破坏已有混键：用户hash(-1)变-2，primitive -1/-1.0若仍-1将互查失败。
先前设想把 fits-i64 BigInteger 改成旧long原值也被否决，因为 BigInteger(P)原正确的0会退化。
因此本切纳入必要数值闭包，而不是接受新回归或只迎合一个例子。

long 的常用范围 -P<n<P 直接返回并规范-1，宽long才做signed remainder，不对long.MinValue取Abs。
int全部处于该范围，仅规范-1；bool原路不变。BigInteger统一有符号余数，整数double沿现精确
long区间或BigInteger分支复用相同规则。非整数double、NaN/无穷、字符串随机化等其它hash不扩。

常见整数入口增加边界/保留值分支；普通用户结果也有严格分派。不能写成只有冷路径改动，
不能把结构守门当零性能成本或实测性能提升。没有新字典/集合布局、缓存或重复用户hash调用。

## 范围边界

真实int子类布局仍未实现，保CP正例pending；空表有效键的hash调用次数是独立旧债。
原 hash_return_and_empty_lookup.py.pending 混合两个问题，保持原字节历史，不能整份转正。
混键只用CP自身有效的eq/hash合同，不要求错误地返回未归约大值的用户键与相等内置数命中。

## 唯一参考

固定CPython3.12.13/64位，28秒TERM＋2秒KILL一次执行，exit0、stderr空、无timeout，
85行含结束标记，0.046720秒仅是预算记录。固定数值oracle、i64边界/模数/±2^100、
真实等值double双向dict/set、非整数返回与无转换协议、原对象异常均有原流。
脚本SHA256 `4f5944bc08dbeb32d462e9be9316910ff39a9731156cbe9c72e1694389607ca6`；
stdout SHA256 `ceb5c81bb75c9daccc3bf19b887575faba82a22eefbebfd2acd11e7eaaefa6f3`。
未运行项目/AOT/性能组，不变更历史项目通过数。

## 首次编译

首Release编译在新错误分支出现CS0266/CS8619：PrepareRaise公开返回object，throw缺少明确
PyBaseException转换。保原日志，修正仅返回同一异常对象的静态类型，不改数值算法或错误边界；
此阶段没有测试/应用运行，不把编译错误说成语义差分失败。

## 首次专项

错误分支补同实例类型转换后Release构建通过，零warning/error。新11项C#与原普通hash槽16项
共27/27通过；新3项真实CP/JIT应用及原hash槽4项回归全部通过。新应用先核CP完整原流/固定链
再构建JIT，比对原输出与全部用户异常帧。整数表示混键使用固定数值oracle，不靠CLR32位hash折叠遮错。
当时完整门禁尚待，未提前把专项计作全量验收；原混合pending与新空表pending均不计通过。

## 完整门禁终态

首次正常 `make check` 退出0：fmt、strict clippy、workspace、Release零warning/error，
850项C#零失败/跳过、固定CP基线228项通过、e2e202项通过及原30项ignored保持。
新三个应用与原四个hash槽应用在workspace再次通过；未改编译器、快照、工作流或ignored。
只做本地候选提交和可恢复备份，不公开推送；没有新增项目/AOT/性能执行。
