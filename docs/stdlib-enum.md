# enum 逐 API 范围（M3.7）

本页描述普通 `Enum` 与严格 `Flag` 的限定原生子集，不代表完整 enum 模块、整个 M3.7 或任何
固定项目已经通过。对应决策与未支持项仍须在 TODO / semantics 中保留。

## 表示与类定义

`enum.Enum` 是稳定的真实 `PyClass` 基类，成员为对应子类的 `PyInstance`。
不使用 CLR enum，不按名称猜类身份，也不逐对象反射。HIR 仅在驱动确认实际
原生导入身份时允许该基类路径；模块别名和 from-import 别名仍受稳定绑定门禁。

专用 `EnumCreate` 只接受稳定根 Enum/Flag 单例及无异常工厂的参数，类体求值后由
`EnumFinalize` 按源码成员顺序建立元数据，在应用类装饰器前完成。普通
`MakeClass` 拒绝任何已登记 Enum 基类，防止装饰器将普通基类替换成 Enum 后
绕过 HIR 验证，构造出未初始化的伪枚举。

允许的成员 RHS：整数（含负数和大整数）、字符串、bool、None 字面量，以及
此前已声明成员的别名。允许普通无装饰的非特殊方法，沿普通方法绑定/MRO
执行；不允许特殊钩子、混入类、方法装饰器、name/value 方法覆盖、复杂 RHS
或重复名称。再次派生枚举（包括空枚举）仍明确拒绝，不以成员数猜测安全性。

## API 表

| API | 已实现语义 | 错误与未支持 |
|---|---|---|
| 成员定义与别名 | 按 Python 哈希/相等语义合并同值成员；True 与 1 可成为别名；别名共享对象，规范名字与值来自首定义 | 非允许字面量/复杂表达式由 HIR 拒绝 |
| `Color(value)`、`Color(value=...)` | 查找现有成员，不新建；已有同类成员保身份；不可哈希查询值退回顺序相等比较 | 无匹配 ValueError；空枚举值构造 TypeError；functional `Enum(...)` 动态建类 NotImplementedError |
| `iter(Color)`、`len(Color)` | 按定义顺序迭代规范成员，不重复别名 | 不承诺 `reversed(Color)` 或完整 EnumType 协议 |
| `Color[name]` | 按名称查成员，接受别名 | 缺失名称 KeyError |
| `value in Color` | Python 3.12 形式，接受成员对象或匹配的原值 | 不扩展 Flag 的伪成员语义 |
| `member.name` / `.value` | 只读，别名读取规范名字和值；成员名称本身为 name/value 时也不覆盖实例描述符 | 赋值/删除 AttributeError；私有元数据修改明确拒绝 |
| 类成员绑定 | 类属性返回真实成员；不能重绑定/删除成员 | AttributeError；动态特殊钩子/描述符元数据修改 NotImplementedError |
| `str` / `repr` / `hash` | str 使用 `Color.RED`，repr 包括原值；hash 取规范名字；普通 Enum 不与原值自动相等 | 不支持用户自定义特殊钩子与 IntEnum 的整数语义 |
| `EnumType` / `EnumMeta` | 两个导出指向同一类型元数据；`type(Color)`、类/成员 `__class__`、isinstance、type 祖先判定 | 不开放元类构造、元类派生或可编程 EnumType |
| `__members__` | 含别名的只读活映射；名称按定义顺序，值保成员身份 | 不是 dict 子类，不能写入/删除，不自动接受 JSON |
| 映射 proxy 方法 | 下标、get、keys/values/items、copy、iter、len、contains、相等比较 | `copy` 与 `dict(proxy)` 返回可变副本，不改变枚举；未提供可变映射方法 |

未实现 `IntEnum`、`StrEnum`、`IntFlag`、`auto`、`unique`、`verify`、
完整 sunder/dunder 元数据、pickle、复杂成员工厂或自定义 EnumType。缺失导出
与非法调用明确失败，不隐式转 CPython，不针对固定项目做名称特判。

## 性能与成本

- 每类持有名称映射、值映射和规范成员列表；别名不额外创建成员实例。
- 类/成员元数据以弱表附着，普通类先经 `HasEnumMetadata` 标记快速排除，
  不在每次普通构造/属性访问上查询弱表。
- 枚举值查找平均 O(1)；不可哈希查询值为 O(n)。定义时需构造两张映射及成员
  对象，不能把这个额外成本隐藏为“零成本枚举”。
- 枚举实例属性走通用元数据路径而非普通属性 PIC，保证 name/value 描述符
  优先于同名类成员。普通实例只增加标记判断；枚举方法仍复用普通绑定。
- proxy 共享成员表，`copy`/`dict(proxy)` 明确分配副本；未引入反射扫描。

本页不作未经基准测量的加速承诺，也不承诺 CPython 的即时析构时机。

## 验证

已通过三项 `PyEnumTests` 及 `m3_7_enum.py`、`m3_7_enum_runtime.py` 两条真实差分。
差分以固定 CPython 3.12.13 为基准，覆盖别名身份、负数/字符串/None、普通方法、
装饰器失败旧绑定、name/value 同名成员、元类身份、只读 proxy 与非法查询。
动态基类替换及拒绝的 HIR 结构由独立诊断/运行时用例验证，不能伪装成正向兼容。


## 严格 Flag 子集

`Flag` 是继承 Enum 的真实 Python 类，Flag 成员不是 int；`type(Flags)` 仍为 EnumType。
复用 EnumCreate/EnumFinalize ABI 与只读元数据，不新增后端 Python 规则。
Enum 与 Flag 根导出都禁止重绑定，以保持前端稳定原生基类证明；其它普通模块槽不扩大保护。

| API | 范围与语义 | 明确边界 |
|---|---|---|
| 定义 | 非负整数/bool字面量、前置成员别名、递归 `\| & ^` 表达式；源码顺序完成后公开类绑定 | 负成员、auto、IntFlag、边界配置、mixin、复杂RHS、特殊钩子/再次派生拒绝 |
| 掩码 | 所有有效bit必须分别有单bit成员；可同时声明zero/复合成员/alias | 只有复合值但缺少单bit声明时 NotImplementedError，不冒称完整Flag分解协议 |
| 构造 | 已有值缓存先查找；未知合法整数组合生成同类成员并按值缓存；同类输入保身份 | 未知非整数或超严格掩码 ValueError，空类 TypeError |
| 负数构造 | 按掩码最高有效位宽正规化，再严格拒绝缺位；不等于允许负成员定义 | 小于负完整位宽范围的值 ValueError |
| `\| & ^ ~` | 同类成员运算，返回同类缓存成员；`~` 只补有效单bit，不制造稀疏缺位 | 与int/其它Flag类不混用，TypeError；不提供int自动转换 |
| 类iter/len | 只迭代按定义顺序的单bit规范成员；zero、复合、alias仍在 `__members__` | zero-only类的len为0，但不误判为完全空类 |
| 成员iter/len/bool | 迭代该值包含的单bit成员；len为设置位数；zero为False | 同类成员包含测试按bit子集，zero包含于任何同类值；错误类型TypeError |
| name/value/显示/hash | 命名组合保规范名字；新组合按单bit定义顺序拼接名字；无名zero的name为None | 同现Enum只读；无名zero显示 `Flags(0)` / `<Flags: 0>`，不是伪造名称 |

性能：首次组合构造扫描单bit成员并创建一个真实实例及组合名字，后续同值复用缓存；
缓存只随实际遇到的组合增长，不预枚举 `2**n`。成员迭代目前创建 O(单bit数) 的临时列表。
位运算使用 BigInteger 保留宽整数，普通Enum路径不改为整型；魔术方法安装在Flag根，
复用既有TryMethod/位算/真值分派，不在普通属性/PIC/算术热路加入Flag解释。
序列物化对类对象复用既有Enum迭代入口，该类分支放在既有容器/IPyIterator快分支之后。
这些是明确的正确性与缓存成本，不作未经测量的加速承诺。

本轮初始运行时422项全绿；补序列物化类分支后，Flag/Enum专测7项与Release再次通过。
HIR3项、driver1项（三快照）和workspace clippy通过；固定Python3.12.13真实差分
`m3_7_flag.py` 已unexpected-pass并转正（`/tmp/ryupy-flag-results-final.jsonl`），
独立只读必要审查通过。chardet定向复测现为运行失败：
`NotImplementedError: re: bytes patterns and subjects are not supported`，证据
`/tmp/ryupy-flag-chardet.jsonl`（native 0/1）。此新能力链仅登记，不在Flag收尾继续扩展；
不据此称chardet或完整Flag模块通过。
