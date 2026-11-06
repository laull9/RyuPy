# ADR-0056：bool 绑定与普通实例长度真值

- 状态：隔离工作树完整门禁通过，未远端验收
- 基线：float 本地冻结提交 `5a0993c4236291bcf9f3470099ae978f840ace6f`

## 真实缺口

bool 已通过完整动态调用求值，不需要修改编译器生产分派。旧运行时却只取第一个参数：真实CLI执行 `bool(Item(), extra())` 会先打印extra，再错误调用Item的`__bool__`并成功；CP13在求完参数后直接TypeError，不触转换。另一实际差分是`__len__`返回-1，旧真值路径返回True，CP13应ValueError。

## 最小连贯修复

bool冷binder只允许零或一个位置参数，关键字优先拒绝；成功绑定才进入一次Truthy。现有primitive/container switch保持，通用调用和展开不加EH、name guard或数组。

普通实例的`__bool__`仍优先且只接受actual bool；缺失才调用`__len__`。长度结果允许实际bool/int/long/BigInteger，或结果对象的一次类型级`__index__`；不调用`__int__`或重复求长度。负值先ValueError，正向超过64位sys.maxsize才OverflowError，不能先窄化把负超大整数误判溢出。实际bool/int/long直接判断，不统一制造BigInteger表示；只有BigInteger走宽比较。

这是共享Truthy冷协议修复，真实影响bool、if、not、and/or及其它现有真值消费者，不称仅影响构造器。用户协议正文不catch；仅自抛绑定、非法结果、负值/溢出记录现场，保原异常身份/已有帧与上下文。条件的原数值静态路径及短路返回operand身份需要真实driver守门，不以更正正确性声称测得加速。

不修改独立`len()`内建、通用PyCast/Index或其它native对象分派。普通Python异常实例自定义真值尚有既有default-true边界，不假称本切全覆盖。直接长度bool合法，但索引协议返回bool/整数子类的弃用警告长尾未实现，完整CP正期望[单独保留](../../tests/fixtures/bool_constructor/deprecated_index_returns.py.pending)。[通用无效星号帧原期望](../../tests/fixtures/bool_constructor/invalid_star_frames.py.pending)保留，不新增passed或ignored分母。

## 验证

固定CPython3.12.13参考覆盖签名/错误优先级、协议顺序与副作用、负超大长度/正向上界和短路身份。隔离工作树首12项C#通过；真实driver首3/3通过，含两个完整异常链case、短路identity及数值静态条件/MIR守门；正常完整门禁首次通过：639项C#、228项CP基线、202项e2e与30项原ignored，fmt、strict clippy及workspace均通过，无快照更新。重建本工作树CLI后，两项原复现stdout与固定CP逐字相等且stderr为空。当前float已冻结提交和恢复包不受本切改动影响；不启动原生、项目矩阵或性能组，历史5/20与matmul禁分层回归结论不变。
