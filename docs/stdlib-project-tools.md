# 项目入口工具逐 API 状态（M3.7）

本页记录 M3.7 已实现的具体 API，不以首阻断解除、导入成功或个别调用成功代表
整个固定项目通过。固定项目总分母与最新结果仍见 `compatibility.md`。

## 内建 `map`

| API / 行为 | 已实现范围 | 验证 |
|---|---|---|
| `map(function, iterable, *iterables)` | 至少两个位置参数；构造时从左到右取得各输入迭代器，不消费元素；不提前检查 function 是否可调用 | `m3_7_map.py`、`PyMapTests` |
| 逐项推进 | 每次从左到右取各输入一项，然后调用 function；最短输入结束；已从更早输入取出的项不回退 | 同上 |
| `iter` / `next` / `__next__` | 自身迭代、默认值、函数及输入异常传播；显式 next 保留 `StopIteration.value`；后续调用仍可继续可恢复用户迭代器或函数 | 同上 |
| 类型与一等值 | 稳定 `builtins.map` 类型对象；`type(m) is map`、`isinstance(m, map)`；`collections.abc.Iterator/Iterable` 的实例与子类判定 | `m3_7_map.py` |
| 身份与重入 | 不复制函数返回对象；返回的 `*args` 元组不受内部参数槽清理影响；重入同一 map 不覆盖外层实参 | 同上 |

### 版本和边界

- 对齐固定 CPython **3.12.13** 的位置参数 API。关键字参数（包括新版本的
  `strict`）抛 `TypeError`，不是接受后忽略。
- 不承诺原生类型继承、pickle/reduce、泛型订阅或线程并发推进；这些能力不因
  `map` 基本迭代可用而自动视为支持。
- `for` / `list` 等 bool 迭代入口将 `StopIteration` 当作本轮耗尽；显式 `next`
  则保留异常终值。不会因一次停止永久锁住 `map` 自身。

### 存储与成本

每个实例保存 k 个迭代器和一个 k 项实参数组，正常推进复用数组；每轮按顺序
取项并在 `finally` 清空参数槽，成本为 O(k)，不预先物化输入。仅在同一对象被
重入时分配额外实参数组，保证外层调用数据不受覆盖。函数仍经过动态调用、
参数绑定与必要装箱；最近一次结果仍由现有 `IPyIterator.Current` 槽持有，
不承诺 CPython 的即时析构/引用释放时机。本页不作未经基准测量的速度提升承诺。

### 固定项目关联

- wcwidth 0.2.13：`wcwidth/wcwidth.py:227` 的 `tuple(map(int, ...))`。
- Markdown 3.5.2：`markdown/__meta__.py:40` 的版本字符串转换。

以上只是具体调用依据，不表示两个项目已经通过；后续模块或语言阻断须通过
固定入口重新运行确认，不能用这两个表达式的通过替代项目验收。

## `unittest` 基础测试执行（已验证子集）

| API | 当前实现范围 | 边界 |
|---|---|---|
| `TestCase(methodName='runTest')` | 真实 Python 类表示，可由用户单继承；方法按原有 MRO/描述符绑定，支持用户覆盖 | 仅稳定的 `unittest.TestCase` 原生导入基类，不开放任意原生继承 |
| `setUp` / 测试方法 / `tearDown` / `run(result=None)` | setUp 失败不执行测试与 tearDown；测试失败仍执行 tearDown；返回原 result 对象，记录执行次数 | run 仅归类普通 `Exception`；`BaseException`/`SystemExit` 向调用者传播，不宣称 CPython TestCase 全部退出异常策略 |
| `addCleanup(function, *args, **kwargs)` / `doCleanups()` | LIFO；setUp 失败仍清理；普通清理异常记录后继续剩余清理 | 不包括 class/module cleanup 或异步清理 |
| `assertEqual/NotEqual/True/False/Is/IsNot/IsNone/IsNotNone`、`fail` | 常用参数及可选 msg；failureException 可被用户类覆盖 | 不包括容器专门 diff 格式、longMessage/maxDiff 控制及完整断言集合 |
| `assertRaises(expected, ...)` | 上下文与 callable 两种形态、异常类型/元组验证、匹配后 `context.exception`；显式 expected SystemExit 可捕获 | 不包括 `assertRaisesRegex`；异常 traceback 引用清理不承诺 CPython 精确对象生命周期 |
| `TestResult()` | testsRun、failures/errors、wasSuccessful、start/stopTest、addSuccess/Failure/Error、stop/shouldStop | 错误项为测试对象与格式化文本，不承诺 unittest 特有的栈过滤/文本逐字一致；无 buffer/failfast 配置 |
| `TestLoader()` / `defaultTestLoader` | loadTestsFromTestCase、getTestCaseNames；`test` 前缀，名称排序，子类覆盖不重复，无测试方法时回退 runTest | 不支持 discover、loadTestsFromName/Module、自定义排序/前缀、类或模块 fixtures |
| `TestSuite(tests=())` | run、addTest(s)、countTestCases、活迭代器；嵌套 suite；尊重 shouldStop，执行后清除引用并保留计数 | 消费后的 suite 重跑报 TypeError；debug 模式和类 fixtures 明确 NotImplementedError |

TestResult/TestLoader/TestSuite 为有限原生类型，不开放 Python 继承；TestCase 是
专门登记的真实 `PyClass`。未提供的属性通过 AttributeError 明确失败，不自动
转 CPython。类 fixture 在 loader 和手工 suite 执行两处均拒绝，不能静默跳过。
`skip`、`subTest`、expectedFailure、TextTestRunner、完整发现及 CLI 尚未实现。

执行路径复用动态调用和既有 Python 异常上下文；清理调用与测试方法通过相同
绑定机制，避免反射逐项查方法。loader 收集并排序方法名，suite 持有测试列表，
result 保留失败测试及文本；这是正确性优先的首版，不作速度提升承诺。

验收入口为原 `m3_7_unittest.py`、继承与生命周期差分，以及 `PyUnittestTests`。
上述三条 CPython 3.12.13 差分与五项 runtime 专测已通过；这不代表整个 M3.7
或固定上游项目已通过。

## 内建 `dir`（用户对象目录子集，已验证）

| 调用 | 当前行为 | 边界 |
|---|---|---|
| `dir(user_instance)` | 合并该实例实际已赋槽与用户类继承链中的真实属性名，去重排序 | 不暴露同类其它实例或已删除的槽；不读取每个属性值，不触发被枚举 property |
| `dir(user_class)` | 合并用户类与已有单继承链属性名，去重排序 | 类属性里的实例 `__dir__` 不作为类自身钩子调用；不实现元类 |
| 自定义 `__dir__` | 从实例的类型查找调用，支持普通/静态/类方法及 property 返回 callable；全字符串结果按 Python 码点稳定排序；非字符串结果复用现有排序协议 | 实例字典同名属性不遮蔽特殊钩子；保留重复项，不强制把返回项转字符串；错误正常传播 |
| `dir()` | 明确 NotImplementedError | 尚未实现局部帧目录 |
| 内建/原生对象目录 | 明确 NotImplementedError | 尚无统一完整属性目录表 |

**准确偏差**：默认用户对象目录只返回当前实现中真实存在的用户属性，不合成
CPython `object/type` 的完整默认 dunder 列表。这是明确登记的目录子集，不是
完整 `dir` 兼容；`m3_7_dir_full_pending.py` 保留完整目录对照失败，未转正。
普通目录回归按非 dunder 用户属性验证，不能据此消去这个独立欠账。

每次默认目录调用收集 O(n) 名称并排序，不维护额外每实例字典、不逐属性绑定
方法或访问 descriptor。自定义钩子结果先经通用迭代物化；全字符串使用专用码点比较，非字符串沿现有
排序协议（其通用比较边界仍适用），不宣称全局 sorted 的字符串比较已修复。不能按某个
第三方库的类名或属性名特判目录。

## `__future__`：编译指令与事实元数据分离

原生模块提供固定 CPython 3.12.13 的十项 `_Feature` 对象、`all_feature_names`、
`optional/mandatory/compiler_flag` 与两个 release 查询方法。同名 feature 对象
重复导入保持身份；普通 `import __future__` 可读取尚未启用的 feature 元数据。

编译器接受已经生效的八项 mandatory feature，以及明确子集的 `annotations`：
注解保存规范化字符串，不在定义时求其名字/表达式；逐 AST 形态、模块注解表和仍拒绝的
复杂形式见 [future-annotations.md](future-annotations.md)。导入必须位于模块 docstring 后、
其它语句之前；嵌套、晚出现、未知或 `*` future 导入明确 E0100。
`barry_as_FLUFL` 指令仍拒绝。future 导入执行真实模块绑定，不在 lowering 中简单删除，
从而保留 `from __future__ import division as d` 的名称和对象身份。
`_Feature` 的直接构造和属性修改不开放。验收：`m3_7_future.py`、`PyFutureTests`
及 HIR/driver 位置和拒绝诊断快照。数据是规范事实，代码独立实现，无复制标准库源码。

## `copy` / `string`（已验证子集）

`copy.copy` 支持内建 list/dict 浅复制、tuple 和受支持不可变值原身份；
`copy.deepcopy` 支持 list/dict/tuple 递归身份图，包括自环、间接环、共享子节点，
无变化的不可变 tuple 保留原对象。身份 memo 不使用 Python hash/eq；先登记可变
容器再递归，tuple 递归返回后重查 memo，避免 tuple→list→tuple 产生重复副本。
最大递归层数为 256，超过抛 RecursionError，不把输入深度泄漏为 CLR 栈溢出。

外部 memo、用户 `__copy__/__deepcopy__`、pickle/reduce 重建、set/frozenset、
bytearray 及未列出的原生对象尚未支持，明确 NotImplementedError。浅复制字典时
若键会运行用户 hash，先拒绝，因为当前底层 Copy 重新计算 hash，不能伪装成
CPython 的 cached-hash 复制。该边界与已有通用字典存储欠账一致，不消除原欠账。
算法以正确身份图为先，额外 O(n) memo/新容器与递归栈；未宣称优于 CPython。

`string` 当前只公开 `ascii_lowercase/uppercase/letters`、`digits/hexdigits/octdigits`、
`punctuation/whitespace/printable` 九项固定 ASCII 常量，不依赖宿主 locale。
Template、Formatter、capwords 尚未实现；Unicode capwords 的独立 pending 保留，
不会用 CLR 单字符 ToUpper 近似会扩展多个码点的 Python 标题化行为。
验收入口：`m3_7_copy_string.py` 与三项 `PyCopyTests` 已通过，独立 capwords 用例仍 pending。

## `enum.Enum`

普通枚举的逐 API 范围、真实类表示、只读映射及性能成本单列于
[`stdlib-enum.md`](stdlib-enum.md)，不把普通 Enum 子集冒称完整元类/Flag 支持。

## 显式退出回调

`atexit` 的注册、相等注销、LIFO、受控注册变化与进程退出行为已按固定基线验证，
见 [stdlib-atexit.md](stdlib-atexit.md)。退出回调错误报告后继续其它回调，不改主体退出码；
最后的标准流刷新失败仍为 120。没有用 CLR ProcessExit 重复注册，也不自动关闭用户流。

## unicodedata（固定 UCD 15.0.0）

| API | 已实现范围 | 验收与边界 |
|---|---|---|
| `unidata_version` | `15.0.0` | 与固定 CPython 3.12.13 一致，不随 .NET 变化 |
| `east_asian_width(chr, /)` | 全码点的 N/Na/A/H/W/F，包括孤代理与未分配值 | 单字符 str；缺参/多参/关键字/非字符 TypeError |
| `combining(chr, /)` | 全码点 canonical combining class | 同上；非零 CCC 不等于所有组合字符 |

官方数据、完整 Unicode 许可与 SHA256 均随仓库保存；生成器逐码点交叉比较
1,114,112 个码点零差异，xUnit 独立 API 摘要与真实编译差分通过。运行时二分
315/388 个闭区间，不用宿主 Unicode 分类，不增加每字符查表对象分配。
`normalize/name/category/decomposition` 等仍未实现，UTF16 代理拼接的既有表示限制保留。
见 [实现与来源记录](notes/2026-11-01-m3.7-unicodedata.md)。texttable 定向复测
随后经 textwrap 与绑定 __len__ 竖切后，固定入口已原生通过；不把单一模块完成计作项目通过。

## textwrap 的 wrap / fill

| API / 参数 | 已实现范围 | 边界 |
|---|---|---|
| `wrap(text, width=70, **options)` | 真正 str 按 Unicode 码点换行，返回 list[str] | 不按终端显示宽度计数 |
| `fill(...)` | 相同规则后以换行符连接 | 不额外模拟段落检测 |
| indent / tabs / whitespace | initial/subsequent_indent、expand_tabs、tabsize、replace_whitespace、drop_whitespace | 非字符串 indent 明确拒绝 |
| 分词 / 长词 | break_long_words、break_on_hyphens | 固定 Unicode15 分类，不用宿主分类 |

旗标当前要求真实 bool，width/tabsize 为声明范围的整数；自定义协议、非整数宽度、
fix_sentence_endings、max_lines、非默认 placeholder 明确未支持，TextWrapper 等未导出。
独立状态机，不复制 CPython 实现或分词正则。固定 1200 组合及 87360 小字母表
组合摘要、五项 C#（含长连字符线性扫描回归）、真实编译差分通过。源码区间减少词块字符串复制，但仍分配码点、
边界与输出存储，不作未测量的性能承诺。精确边界与证据见
[实现记录](notes/2026-11-01-m3.7-textwrap-plan.md)。后续绑定 __len__ 已解除该阻断，见下节实际项目证据。

## buffer 与内建长度方法

- `memoryview`：一维连续 B/c 的真实共享存储、切片/cast独立租约、显式 release/context、
  rawbuffer 复制以及 bytearray 导出期间 resize 锁已通过差分与重入/错误报告验收。
  任意格式、多维、非连续布局与切片写入等仍明确未支持；GC自动释放沿D001边界。
  逐 API 和成本见 [memoryview 表](stdlib-memoryview.md)。
- 精确 list/tuple/dict/set/frozenset/dict视图/bytes/bytearray/str 的绑定 `__len__`
  统一支持直接和一等调用，调用时读取实际长度并校验零参数，保接收者变动可见。
  `method-wrapper` 的声明元数据已验收；未绑定 slot descriptor 及完整 types 模块
  不因此可用，见 [长度方法记录](notes/2026-11-01-m3.7-builtin-length.md)。

texttable 固定入口及九个原上游布局场景通过，M3.7最终完整矩阵确认原生 5/20（新增progress）。
chardet 后续定向复测已越过 TypedDict(total=False) 与 Str 属性诊断，
Flag 子集现已通过真实差分，chardet 定向仍因 bytes 正则未实现而运行失败，
按有界收尾计划记录新链，不继续扩本里程碑。


## TypedDict 与 Ellipsis 元数据

TypedDict 的真实 factory、单父 total-only 类定义、字段元数据及生成 dict 的调用
已完成定向差分；不折叠 TYPE_CHECKING，也不以名字拼写替代实际基值守卫。
Required/NotRequired、前向引用、多继承等仍明确未支持，完整用例独立 pending。
定义时私有命名空间与元数据复制属于冷路径，不给普通实例/PIC 增加每次检查。
逐 API 与验收见 [TypedDict 范围](typing-typed-dict.md)。

Ellipsis 字面量与内建名字共用真实单例，type(Ellipsis)() 返回该单例；
typing.Tuple[T, ...] 保留末位参数身份。名字重绑不能修改源码字面量；同名模块
槽的有限回退见 [模块绑定](module-binding-state.md)。其它 typing 泛型能力不能据此推断可用。
