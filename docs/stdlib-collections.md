# 容器协议与惰性迭代工具：M3.6 原生 API 范围

本文按当前实现列支持范围，不把模块导入成功等同于完整标准库兼容。
`functools`、`contextlib`、`dataclasses`、`typing` 由对应 API 表独立登记。
所有工具留在 C# runtime，编译器只发既有动态调用与迭代协议，不引入反射发射或 CPython 隐式回退。

## collections

### deque

| API | 当前行为与边界 |
| --- | --- |
| `deque(iterable=(), maxlen=None)` | 输入逐项消费；`maxlen` 走整数索引协议，负值 `ValueError`，超宿主索引范围 `OverflowError`。类型对象缓存，`isinstance` 可用；不支持 Python 层继承原生类。 |
| `append/appendleft`、`pop/popleft` | 有界插入淘汰另一端；空取出 `IndexError`。环形数组端点均摊 O(1)，淘汰槽清引用。 |
| `extend/extendleft` | 逐项更新，异常保留此前更新；左扩展反转输入次序。自身扩展先保留开始时的引用快照；`maxlen=0` 仍完整消费输入。 |
| `len/bool/in`、正反向迭代 | 真正游标而非元素快照；结构修改使推进抛 `RuntimeError`。替换元素和 reverse 不错误标记结构失效；空 clear、长度不超过一的 rotate 不失效。 |
| 整数下标读取/赋值/删除 | 支持负索引；越界 `IndexError`；不支持切片。中间删除为 O(n)。 |
| `insert(index, value)` | 位置裁剪到合法端点；有界已满时 `IndexError`，不静默淘汰。中间插入为 O(n)。 |
| `index(value, start=0, stop=...)`、`count/remove` | 使用 Python 相等比较和身份快判，比较回调若改变结构则 `RuntimeError`；不存在 index/remove 为 `ValueError`。 |
| `rotate(n=1)`、`reverse`、`clear`、`copy/__copy__` | rotate 选择较短方向，成本 O(min(k, n-k))；copy 浅复制、保留 maxlen。 |
| `maxlen`、repr、相等性 | maxlen 只读；递归 repr 有保护；只与 deque 按元素比较相等；不可哈希。 |
| 未开放 | 排序比较、拼接/重复运算、pickle、原生类型继承等。不是完整 deque API。 |

### defaultdict

| API | 当前行为与边界 |
| --- | --- |
| `defaultdict(default_factory=None, [mapping_or_pairs], **kwargs)` | 工厂初始化须 callable 或 None；组合有序 `PyDict`，不建立另一套哈希存储。 |
| 缺键下标读取、`__missing__` | 无参数调用工厂，成功后写入并返回同一对象；工厂异常不代替用户异常，未成功返回时不自行插入键。 |
| `default_factory` | 可读取/赋值；后续赋成非 callable 的值在实际调用时 `TypeError`，不是赋值时调用工厂。删除属性尚未实现。 |
| `get/in/pop/setdefault` | 普通映射规则，不调用缺键工厂。 |
| `keys/values/items`、迭代、len/bool | 复用现有活视图和字典结构版本号，不把字典键迭代伪装成键值对迭代。 |
| `update/clear/copy/popitem`、下标赋值/删除 | update 支持本地 dict/原生映射包装或键值对来源及关键字；copy 浅复制并保留同一工厂；popitem 取最后插入项。 |
| `dict(value)`、isinstance/issubclass | 通过明确原生映射接口复制条目；dict 祖先关系显式登记，不能因只是包装对象而判假。 |
| 未开放 | Python 层继承、通用自定义 Mapping 的 keys 协议（发现未知映射时明确 NotImplementedError）、联合运算、fromkeys、pickle 等。 |

### Counter

支持构造、下标/缺键零值、普通映射方法，以及 `update/subtract/total/elements/most_common`。

- 缺键读取返回零但不插入；显式零和负计数保留在存储及遍历中。
- update 的普通可迭代来源逐项加一；映射来源在空 Counter 时直接复制，不强行计算 `0 + value`。
- 非空映射 update 保留“新计数 + 旧计数”顺序；subtract 为“旧计数 - 新计数”，不删除非正计数。
- elements 惰性遍历，忽略非正整数计数，非整数计数报错；结构修改遵守底层字典游标规则。
- most_common 保持相同计数的插入顺序；使用现有稳定 Python 排序，比较异常不包成 CLR 异常。有限 n 暂无堆优化，仍可能排序全部项目。
- Counter 之间相等性把缺键视作零；普通映射比较继续采用映射规则。
- Counter 间 `+/-/|/&` 与一元 `+/-` 返回新对象，只保留正计数；联合相等计数保留左操作数对应值。
- `+=/-=/|=/&=` 保留接收者身份，支持 dict/原生映射；逐项立即写回，中途异常不能回滚已更新计数。
- repr 优先按计数降序展示；不可排序时使用插入顺序。copy 浅复制；不可哈希。
- 未开放完整富比较、Python 层继承、通用第三方 Mapping 桥接（不静默把未知 items 映射当计数序列）、pickle 等；不能据此宣称完整 Counter 兼容。

### namedtuple（本地候选，有限范围）

`namedtuple(typename, field_names, *, rename=False, defaults=None, module=None)` 已有真实工厂/类型，不是导入占位。50项C#及五组CP/JIT首次通过；正常完整门禁首次通过（982C#、228CP基线、202e2e+30原ignored），尚未公开发布或重新验tabulate。

- ASCII名字/关键字/重复与rename校验，普通自定义iterable、str转换、truth副作用及defaults引用保持；fields逐项转换，名称错误在defaults消费之前。
- 新类独立身份、tuple/object祖先与readonly字段；位置/关键字构造及右对齐defaults，_fields/__match_args__、_field_defaults独立字典和稳定类元数据。
- 普通tuple混合比较/hash/键、len/bool/迭代/下标/切片/拼接/重复、格式/JSON等代表值协议；转换结果按对应规则回plain tuple。字段可遮蔽count/index，方法属性先于参数求值读取。
- _make完整消费、_replace按原闭包与实际receiver、_asdict/getnewargs/repr；普通helper支持跨生成类self。显式__new__及_make.__func__仅允许原定义类，复杂跨cls布局不假成功。
- 默认module用真实当前来源；显式非None值不调用bool/str、不查来源。Static未知来源明确NI，不把结构检查当运行成功。
- 类整体冻结、__dict__、用户继承、dataclass修改、copy/deepcopy、duck helper、factory实例方法绑定及高级反射明确NI。tuple.index start/stop与部分原错误消息不在本切修复。
- 非ASCII/NFKC名与部分str子类输入保明确边界；普通__str__返回精确str可用。typename str子类仍按原intern类型边界报错，rename真分支对字段str子类动态验证NI。
- 不使用全局永久字符串intern；只保证schema内名字/metadata稳定，跨类名字is原CP期待保pending。hint协议照常校验但不按巨大值预分配，CP资源失败时点不等价。

实现与成本、原始参考和暂缓项见 [ADR0077](adr/0077-namedtuple-layout.md)。该API的局部通过不改变历史5/20项目统计，也不代表完整collections或AOT验收。

## collections.abc

导出有限类型：`Iterable`、`Iterator`、`Generator`、`Sized`、`Container`、`Collection`、
`Sequence`、`MutableSequence`、`Mapping`、`MutableMapping`、`Set`、`MutableSet`、`Callable`。

- 类型对象缓存；支持已登记类型的 isinstance/issubclass 及列出的 ABC 继承关系。
- Iterable/Sized/Iterator 等结构检查只看类型成员；不调用用户 iter、len、contains，不消费输入。
- `__iter__ = None` 不算可迭代协议；只有 `__getitem__` 的旧式序列后备不会自动成为 Iterable。
- Sequence/Mapping 等采用已知内建/原生类型范围，不把任意同名方法集合误判成注册的 ABC。
- `Hashable` 暂不导出：普通类创建期隐式 `__hash__ = None` 的有限候选见 ADR0066；ABC 的结构判断、正负缓存及动态变更边界未随之完成，不能用内部容器预筛或尝试 hash 的结果冒充。正向与动态修改用例继续保留 `m3_6_hashable_pending.py`。
- 不开放 `register`、虚拟子类注册、ABC mixin 实现、Python 类直接继承原生 ABC。
- 类型参数订阅仅由有限 typing 元数据接口处理，不提供完整运行时类型检查或反射。

## itertools

| API | 消费与参数规则 |
| --- | --- |
| `repeat(object, times省略)` | 省略次数无限；有限次数接受整数索引，负次数为空，显式 None 是 TypeError。重复同一对象引用，不复制值；支持 object/times 关键字。 |
| `chain(*iterables)` | 构造不调用各输入的 iter；按需要才创建当前子迭代器；所有输入串联而非预先物化。 |
| `chain.from_iterable(iterable)` | 构造立即建立外层迭代器，但不消费外层元素；之后逐项建立子迭代器。 |
| chain 异常 | 外层 next 或子迭代器构造失败后永久耗尽；已经建立的内层 next 抛普通异常则保留内层，后续可继续。 |
| `islice(iterable, stop)`、`islice(iterable, start, stop[, step])` | 只收位置参数；None 起点为零、None 步长为一、None 终点无界。负索引或零/负步长 ValueError。构造立即建立源迭代器，即使 stop 为零。 |
| islice 消费 | 结束时消费到 max(start, stop)；start 大于 stop 也不能直接不消费；步长不能多取边界外元素。 |
| 未开放 | 本表之外的 itertools 工具、pickle、完整 length_hint/repr 细节等。逐项目依赖另行安排，不以三个工具代指整个模块。 |

## 验证与成本

- 新 deque/defaultdict/Counter 相等性使用线程局部深度守卫，上限 256；直接递归容器比较抛可捕获 RecursionError，异常后恢复计数。该保守边界防 CLR 栈溢出，不声称模拟 CPython 的精确递归层数；普通 dict/list 的既有递归比较欠账未由本切一并解决。

- `tests/e2e/phase3/m3_6_collections_iterators.py`：有界容器、工厂、计数、别名、异常后局部状态及精确迭代消费。
- `tests/e2e/phase3/m3_6_collection_recursion.py`：递归相等性安全与异常后深度恢复。
- `tests/e2e/phase3/m3_6_abc.py`：类型关系、关闭协议、无调用副作用的结构检查。
- `runtime/RyuPy.Runtime.Tests/PyCollectionsTests.cs`、`PyItertoolsTests.cs`：错误路径、类型身份、构造及消费边界。
- 原 `m3_6_tools.py` 仍负责整个工具组合验收，不被上述细分用例替代。
- 映射复制复用现有 PyDict 实现，方法包装复用 PyFunction 参数绑定器；仍有包装/装箱分配。
- deque repr、copy、自身扩展需要引用快照；Counter most_common 使用 O(n log n) 稳定排序与 O(n) 临时空间；不宣称性能已经测得更快。
- 项目证据：progress 用有界 deque；click 用 deque/repeat/Sequence；tomli 用 Iterable 注解；tabulate 还需要 zip_longest 等。sortedcontainers 仍受 dict/ABC 继承阻断，不能把 API 用例通过计成项目通过。
