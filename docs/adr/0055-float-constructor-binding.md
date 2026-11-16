# ADR-0055：float 完整参数绑定与有限冷转换

- 状态：本地完整门禁通过，未远端验收
- 基线：main `d4d8243a34bc8466eefb6995edfde42f3bf87eb5`

## 真实缺口

原CLI执行 `float('3.5', extra())` 直接输出3.5、exit0；固定CPython3.12.13先执行extra打印，再因多参数抛TypeError。原MIR无关键字分支只取first，运行时也忽略额外位置参数；仅修编译器无法覆盖一等float入口。

## 有限合同

零或一个位置参数，任意关键字拒绝，所有参数表达式沿既有顺序执行一次，绑定失败不触转换协议。仅精确一个普通位置参数保原静态F64/I64/Bool数值快路，其余完整动态绑定，无新RuntimeFn或数值ABI。

冷对象按类型槽先`__float__`，缺失才`__index__`；存在但不可调用、抛错或返回错误时不后退。动态exact double与协议double结果保原对象。自身绑定、错误返回、非法文本和大整数溢出记录现场；用户协议正文不宽catch，通用CallBuiltin/CallValue/Flatten不加EH或热guard。

str/bytes/bytearray先完整验证十进制浮点grammar，再调用CLR舍入。字符串复用固定Unicode15数字/空白归一化，bytes只认ASCII；下划线仅数字之间，拒重复/端点、hexfloat、NUL和内部空白。inf/infinity/nan大小写及符号、负零、文本溢出/下溢均有参考；float没有int的4300位限制。超长指数不得只按符号猜inf/0，零与长尾数scale抵消必须真实测试。合法grammar若后端不能转换则明确NotImplementedError，不伪称非法Python文本。

一般原生数值子类、弃用协议返回的warning、memoryview/通用buffer仍不在首切支持；[完整CP正期望](../../tests/fixtures/float_constructor/float_protocol_extended.py.pending)独立保留，不计通过或ignored分母。通用预callee展开帧与静态数值对象身份旧边界不凭本切口头关闭，source-level identity由真实driver验证。不得因有限支持增加历史项目通过数。

## 验证计划

最多16项runtime、3项真实driver及ty/MIR结构；先专项，后正常完整门禁。参考包括40位指数、零/负零、5000位尾数scale抵消、极限舍入/子正常数和返回identity。原CLI失败结果保留，尚未把源码审或CP参考记作RyuPy通过；本切不启动原生、项目或性能组。

首轮runtime16/16通过，包含超长指数、零/负零、5000位scale抵消与极限舍入；ty与MIR结构各1项通过。真实driver首次2/3，core辅助函数使用既有不支持的动态`except kind`而未编译，错误链和MIR测试通过。随后以静态异常组捕获且保异常真实class等于kind的断言迁移，不改变float实现或原case期待，修后driver3/3通过，literal/direct/savedalias/普通参数与注解参数内部的source identity均保真实正断言。正常完整门禁首次通过：627项C#、228项CP基线、202项e2e与30项原ignored，workspace、fmt、strict clippy均通过；无快照更新。重建CLI后的最初多参复现与CP原stdout逐字相同且stderr为空。未运行新的远端、原生或性能组。CLI诊断首误用不支持的check子命令只得到usage，随后build诊断明确E0100；不计作转换执行失败。
