# 函数工具与生成器上下文：逐 API 实现范围

本页记录 M3.6 的 `functools` / `contextlib` 原生实现，不把两个模块的导入成功写成完整兼容。包装器不经过 CLR 反射调用；共享入口为 `IPyNativeCallable` 和有限的非数据描述符 `IPyNativeDescriptor`。

## functools

### partial

`partial(func, /, *args, **keywords)` 构造部分应用，构造时验证目标可调用，不执行函数体。

- 调用时先拼接保存的位置实参与本次位置实参，再合并关键字；本次关键字覆盖保存值，已有键的位置不变。最终参数绑定仍由被调用对象处理，因此重复位置/关键字、缺参等错误不会被包装器吞掉。
- `func`、`args`、`keywords` 属性只读。args 是 tuple；keywords 是**实时可变的 dict**，修改它会影响后续调用。加入非字符串键会在调用时触发 `TypeError`。
- 部分应用持有 func、位置实参和关键字值的强引用；不复制它们指向的可变对象。用户属性存于可写 `__dict__`。
- 当内层 partial 尚未分配用户属性字典时，嵌套 partial 扁平化为底层函数和合并参数；即使只是读取过空的 `__dict__`，也保留内层 partial 身份，不再扁平化。
- partial **不是方法描述符**：作为类属性从实例读取时，不自动补 self。这与普通 Python 函数及 lru_cache 包装器不同。
- 不自动复制被包装函数的 `__name__`、文档、标注或 `__wrapped__`。未实现 partialmethod、pickle/重建协议、完整专用 repr 与所有标准库对象内省属性。

### reduce

`reduce(function, iterable[, initial], /)` 的三个参数只能按位置传入。

- 每次仅消费下一个元素并调用归约函数；不提前物化 iterable。
- 省略 initial 时，第一个元素成为初值；空迭代器报 `TypeError`。显式 None 是有效初值，不与省略混淆。
- 迭代器、归约函数的错误原样传播；归约函数抛出的 `StopIteration` 不当成正常耗尽吞掉。
- 单元素无初值或空迭代器有初值时不会调用 function，因此不额外提前验证其可调用性。

### update_wrapper / wraps

提供：

- `update_wrapper(wrapper, wrapped, assigned=WRAPPER_ASSIGNMENTS, updated=WRAPPER_UPDATES)`
- `wraps(wrapped, assigned=WRAPPER_ASSIGNMENTS, updated=WRAPPER_UPDATES)`
- 常量 `WRAPPER_ASSIGNMENTS` 和 `WRAPPER_UPDATES`

默认 assigned 为 `('__module__', '__name__', '__qualname__', '__doc__', '__annotations__', '__type_params__')`，updated 为 `('__dict__',)`。

1. 依次读取并赋值 assigned 属性；wrapped 缺少某属性时跳过，不伪造元数据。
2. 依次调用 wrapper 对应属性的 `update`，将 wrapped 对应属性合入；wrapped 缺少该属性时使用空 dict。
3. **最后**设置 `wrapper.__wrapped__ = wrapped`，避免 wrapped 的旧 `__wrapped__` 经字典合并覆盖这一层关系。

复制为浅赋值：`__annotations__` 保留对象身份，用户字典内容合并而不是替换整个 wrapper 字典。wraps 返回以 update_wrapper 为目标的 partial，不执行被装饰函数。

沿用 M3.3 元数据边界：原始函数具备 `__name__`、`__doc__`、`__annotations__` 与用户属性字典；尚未由编译器提供的完整 `__qualname__`、`__module__`、类型参数元数据不能通过 wraps 凭空补齐。用户显式提供的同名属性可以按上述协议复制。

### lru_cache

支持 `@lru_cache`、`@lru_cache()`、`@lru_cache(maxsize=128, typed=False)`，也可先调用返回装饰器再应用于目标函数。

| 配置 | 行为 |
| --- | --- |
| 默认 maxsize=128 | 最多保存 128 个结果，命中提升为最近使用 |
| maxsize=None | 无界缓存，需由用户清理或释放包装器 |
| maxsize=0 或负整数 | 不保存结果；负数按 0；不构造或哈希参数键，但仍计 miss |
| 超出有符号平台索引范围的正整数 | 应用装饰器创建包装器时 `OverflowError`，不悄悄钳制 |
| typed=False | 不普遍附加类型；保留下面的精确 int/str 单参数快键行为 |
| typed=True | 将直接位置参数和关键字值的类型纳入键；不递归区分容器内部数值类型 |

显式配置形式允许 typed 使用真值协议，转换在**应用装饰器**时进行；`cache_parameters()` 保留原选项对象。直接将 callable 作为 lru_cache 第一个参数的简写形式要求 typed 是 bool。

#### 键、身份与错误

- 键保留位置参数顺序、位置/关键字边界及关键字调用顺序。`f(a=1,b=2)` 与 `f(b=2,a=1)` 可以形成两个条目；不先将函数形参绑定成规范形式。
- typed=False、无关键字、只有一个精确 int 或 str 参数时使用独立快键；因此 `f(1)` 不保证与 `f(1.0)` 同条目。一般元组键仍使用 Python 相等/哈希规则，float 和 bool 可相等命中。
- 除 maxsize=0 外，参数必须可哈希。不可哈希错误发生于 miss 计数和函数调用之前。
- 每次调用构建键时计算 Python hash；桶查找、插入及淘汰复用该次已算出的 hash，不在淘汰旧条目时重新调用用户 `__hash__`。
- 命中返回同一个结果对象，不复制可变结果。异常结果不入缓存，失败调用仍计 miss。
- 键和结果被强引用；淘汰、cache_clear 或包装器不再可达后释放缓存持有的引用。采用 GC，不承诺 CPython 引用计数式析构时间。
- 包装器是非数据描述符，作为方法读取时正确绑定 self；self 也是键的一部分，可能因缓存而延长生命周期。

#### 包装器接口

- `cache_info()` 返回一次统计快照，具备 hits、misses、maxsize、currsize 四个只读属性；支持 repr、迭代、len、下标/切片、相等与哈希，以及 tuple 类型判定。
- CacheInfo 尚未实现完整 namedtuple 的 `_asdict`、`_replace` 等扩展及全部 tuple 算术/比较接口；不能据类型判定将这些操作列为已支持。
- `cache_clear()` 删除所有条目并将 hits/misses 归零。
- `cache_parameters()` 每次返回新 dict；修改返回字典不改变缓存配置。bool maxsize 在参数字典中保留 bool，在 cache_info 中显示规范整数容量。
- 使用 update_wrapper 规则复制可用元数据；`__wrapped__` 可直接调用以绕过缓存。用户属性字典可变，普通方法属性可被显式用户属性遮蔽。
- 不包含 `cached_property`、`cache` 别名、`singledispatch`、`cmp_to_key` 等未列 API，也未实现缓存对象序列化或任意自定义描述符协议。

#### 缓存成本与重入

实现采用缓存哈希桶和双向 LRU 链表。一般命中/提升与淘汰预期为 O(1)；构造键与参数数量成正比，碰撞桶查找与桶内候选数成正比。无界模式不做淘汰。

缓存状态由锁保护，用户函数在锁外执行；函数体递归或并发插入同一键后，外层返回时重新检查，不覆盖已经插入的结果。用户 `__eq__` 可清缓存或递归修改缓存：查找检查结构版本并在变化后重试，避免返回已经删除的条目。并发缺失可能重复执行函数，不保证“恰好计算一次”。尚无吞吐基准支持“比 CPython 更快”的结论。

## contextlib.contextmanager

`contextmanager(func)` 返回带 wraps 元数据的工厂。调用工厂执行 func 以得到生成器，不提前恢复生成器函数体。

### with 协议

- `__enter__` 恢复一次，返回 yield 的值；正常耗尽而未 yield 报 `RuntimeError("generator didn't yield")`。进入时释放仅供重建使用的工厂/实参引用。
- 正常 `__exit__` 再恢复一次，正常耗尽返回 False；再次 yield 报 `RuntimeError("generator didn't stop")`，并保证调用 close 清理。
- 异常退出把**原异常实例**传给生成器 throw：生成器吞掉异常并正常完成时可抑制；重新抛出同一个异常返回 False，不用类型相同来代替身份比较。
- with 体抛出的 StopIteration 经生成器 PEP479 转为 RuntimeError、且 Cause 正是原异常时，返回 False，让原 StopIteration 继续传播，不把它误变成包装错误。
- 生成器抛出不同异常时传播新异常；throw 后仍 yield 报 `RuntimeError("generator didn't stop after throw()")` 并 close。close 清理过程的新异常可以覆盖协议错误。
- 保留生成器跨 yield 的自身异常状态；没有自身活动 handler 时才借用调用方当前异常。内部协议错误和清理过程建立/恢复 Python 异常上下文，不让 CLR catch 代替 Python handler。
- 复用同一个已进入的上下文对象不受支持，会报属性状态错误；不承诺可重入 with。

### ContextDecorator

工厂产生的上下文对象也可用作 `@resource()` 装饰器。每次调用被装饰函数时重新创建独立上下文，保留原实参；不会复用上次已经耗尽的生成器。

装饰器体抛异常时显式进入 Python handler，再调用 Exit，并在返回/重抛/清理失败时恢复旧 handler。因此生成器协议错误的 `__context__` 是函数体异常，调用者外层 except 中的裸 raise 仍指向原外层异常。

### 范围与成本

只实现同步 contextmanager 及其生成器上下文/装饰器路径，不包含 asynccontextmanager、ExitStack、suppress、closing 等未列 API。非迭代器返回值不会被自动转换为 iterable；底层 next/throw/close 不存在或执行失败时保留对应 Python 错误。

每个 with 或装饰器调用创建一个独立上下文及生成器状态；清理通过既有生成器恢复入口完成，不建立线程或忙等。公开 `__traceback__` 仍沿用既有运行时边界，不能将内部帧保存/恢复写成完整 traceback 对象支持。共享异常对象以独立 `__suppress_context__` 标志实现 `raise ... from None`：保留 Context 供程序读取，报告时不显示隐式链；该标志仅接受 bool，恢复 False 后可重新显示。显式 cause 在原异常实例上设置，不复制异常身份；显式原因优先于隐式报告链。

## 验收文件

- `PyFunctoolsTests.cs`：partial 绑定/字典身份/嵌套、reduce、元数据浅赋值。
- `PyLruCacheTests.cs`：有界淘汰、键与参数顺序、typed、hash 次数、异常不缓存、递归/比较重入、方法绑定、CacheInfo、配置时序及 cache_clear 释放强引用。
- `PyContextManagerTests.cs`：产出次数、异常身份、PEP479、装饰器重建、非法 yield 后清理、异常上下文与外层恢复。
- `m3_6_functools.py`、`m3_6_lru_cache.py`、`m3_6_contextmanager.py`、`m3_6_context_exception_state.py`：先 pending，再执行 CPython 基线和真实差分。
- 原始综合 `m3_6_tools.py` 的转正依赖同里程碑的 collections/itertools 等，不因单个子模块通过而提前改变期望。


## itertools.zip_longest（本地验收通过，未发布）

构造先验证fillvalue再左到右取iter；next逐槽立即取值，耗尽槽永久填同一fill对象，最后活跃槽耗尽结束，不返回全fill行。其它输入错误原样传播且整个zip永久结束，不回滚已消费元素。每行新tuple/局部数组，不全量缓存，也不承诺CP引用计数复用的分配成本相同。

局部普通实例支持类型槽/property与缺iter时getitem，清理槽取得错误后的fallback与真正方法正文异常分开；不更改全PyIter。真实type/iter身份及模块重绑不影响已有对象，fillvalue反射/pickle、用户派生与任意自定义descriptor未完整支持，正例保pending。内部/公共与StopIteration必要ABI前置累计25项及3应用CP/JIT已过，保Z2首失败与修后复验，正常完整门禁1019C#/228CP/202e2e+30原ignored已过，详见[ADR0079](adr/0079-zip-longest.md)。
