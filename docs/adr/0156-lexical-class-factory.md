# ADR-0156：定义位置的有限普通类工厂

状态：WIP 实现候选，未编译、未执行产品验收，不提升 toml 或固定项目通过数。

## 范围与表达

新增函数内及类体内普通 class；每个 ClassId 仅代表静态源码模板。表达式保存在真实
定义函数的 HIR arena；owner 独立记录，不借用 main arena。MIR 在定义者 CFG 中展开
冷事务，不引入解释器、Reflection.Emit 或新的解析固定点。旧正确顶层路径保留。

每次执行按 decorator 表达式、base、body、Finish、逆序 decorator、公开 binding 的顺序
完成。Prepare 冻结真实单父类，分配私有 namespace；Finish 重查完整普通祖先及 namespace，
成功才创建类、填原始 class cell。失败不覆盖外 binding，replacement decorator 不改变
原方法捕获的原类 cell。方法模板静态，函数对象、defaults、Env、namespace、类与 cell
每次重新分配。类体局部绑定集合按完整 suite 分类，方法 free 不捕获类属性。
类体对定义函数局部的读取同样走真实 cell/free 语义，不误用 LOAD_FAST 错误分类。

公开 __module__ 来自定义函数所属模块的当前 __name__ 绑定，允许非字符串；内部源码
身份仍走原独立来源链。Instance 模块删除该绑定后沿真实 builtins 回退；Legacy 默认
模块删除初始元数据的原有 Diagnostic 不因此放开。不能以调用者 ambient 或源码常量
替代公开元数据。

## 边界

只允许 0 或 1 个真实 object/已完成普通 user base；MI、native dict/list/str、特殊族、
custom metaclass、mro_entries、slots、创建 hooks、custom descriptor 明确拒绝。
Prepare 与 Finish 均检查父链，Finish 检查 body 引入的能力；不改变 MRO/dict/super 算法。
类体已有不支持的 global/nonlocal/import/raise 等语句仍拒绝；函数/方法已有声明不收窄。
生成器或 async 定义者，以及当前不支持的暂停头表达式仍结构拒绝，不扩旧帧证明域。

本单元两项明确暂缓：新 factory 词法域真正需 private mangling 的名称静态 E0100，
保 dunder/字符串/全下划线类名规则与旧域；旧默认顶层 Outer 到新 Inner 类体直接读取
外层 __class__ 静态 E0100，不把它伪装为 global。函数内同语义则保真实空外层 cell。

## Late 能力与来源证明

新 family 标记在普通派生创建时继承，不能被旧路径 early-return 洗掉；只对标记族
在已有普通属性、PIC、构造、super 与明确 bound-call 消费入口检查全局类版本。
验证不执行用户 getter，不递归 lookup，不扫描任意函数参数，不全局禁止旧类修改。
现有 __dict__ 返回 copy 只用于说明没有该绕写通道，不声称完整 mappingproxy 等价。

MIR 记录 cell owner、namespace、唯一 Prepare/Finish、填充与真实 local/cell/free/global/
namespace 发布接点，并验证 raw cell 不逃逸。直接外层 cell 读取只用固定单元素临时 Env。
该证明复用原类 cell 相位/异常 CFG 检查，不引入通用 heap 分析。

## 验证状态

pending CP 差分 fixture、Rust 正反来源测试、C# 冷事务与 late mutation 测试均保留。
本地只执行格式/静态检查及现存 CPython 小研究，未构建 Rust/.NET；正式同源 JIT、
有限 NativeAOT、旧20/Markdown/公共 CLR ABI 无删门由专用 hosted CI 验证后才可更新状态。

## 本单元明确保留的 traceback 偏差

沿用 ADR-0081 已登记的 inline class-body 缺独立帧及逐项行归属边界，不声称全面
CPython stderr兼容。原未捕获异常反例必须保存CP与产品完整stdout/stderr/exit，缺帧
标记 unaligned、对应 overall 失败且不计通过；不得删CP类帧或只比异常末行。
异常传播、失败不覆盖binding、原cell状态及副作用以独立捕获事务fixture严格验证，
不能用它替换未捕获反例。补类体帧属于后续架构债，本单元不为toml扩trace框架。

## 公开ABI许可计数

五个冷方法不是五个ABI总条目：公开static类型PyLexicalClassFactory另占一条T记录，
本单元固定许可为1T＋5M共6条，目标1716＝历史1659＋原51＋本次6。原baseline DLL、
六张旧允许表及V1专属12合同保留；独立六字面量表不从候选PE差集推导。真实PE门仍
拒重复、完整排序SequenceEqual并硬验before1659/after1716；合成反例不替代实测。
