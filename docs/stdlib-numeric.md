# 数值与数据标准库：逐 API 验收

本页记录 M3.5 的实际原生实现范围。导入成功不等于完整模块兼容，也不增加固定项目集的原生通过数。

## math

入口：`PyMathModule.CreateAttributes`。以下函数仅接受位置参数；无参数、过多参数与关键字参数报 `TypeError`。

| API | 参数与返回值 | 已实现边界 |
| --- | --- | --- |
| `sqrt(x)` | 实数 → float | 负数（含负无穷）报 `ValueError`；保留负零；正无穷和 NaN 传播 |
| `floor(x)` / `ceil(x)` / `trunc(x)` | 内建数值 → 任意精度 int | 整数不先转 float；NaN 报 `ValueError`，无穷报 `OverflowError` |
| `fabs(x)` | 实数 → float | 负零变正零；无穷和 NaN 保留相应结果 |
| `isfinite(x)` / `isinf(x)` / `isnan(x)` | 实数 → bool | 遵循 float 转换协议；不能转换为有限 float 的巨大整数报 `OverflowError`，不跳过转换 |
| `log(x[, base])` | 实数 → float | x 或 base ≤ 0 报 `ValueError`；base 为 1 报 `ZeroDivisionError`；显式 None 不是省略 base |
| `log10(x)` | 实数 → float | x ≤ 0 报 `ValueError`；正无穷和 NaN 传播 |
| `exp(x)` | 实数 → float | 有限输入结果溢出报 `OverflowError`；下溢允许零，负无穷得零，正无穷得正无穷 |
| `pow(x, y)` | 两个实数 → float | 不等同于支持复数的 `**`；有限负底数和非整数有限指数报 `ValueError`；零的有限负次幂报 `ValueError`；有限输入溢出报 `OverflowError` |
| `sin(x)` / `cos(x)` / `tan(x)` | 实数 → float | 无穷报 `ValueError`；NaN 传播 |
| `pi` / `e` / `tau` / `inf` / `nan` | float 常量 | tau 为 2 × pi；inf 为正无穷 |

### 转换和特殊值

- 普通实数参数接受 int、bool、float；其他对象依次尝试 `__float__`、`__index__`。`__float__` 的结果必须是 float，`__index__` 的结果必须是整数；用户方法抛出的异常原样传播。不接受数字字符串。
- `floor` / `ceil` 对其他对象先调用 `__floor__` / `__ceil__`，缺失时才走实数转换；`trunc` 仅使用 `__trunc__`，不以 `__float__` 或 `__index__` 代替。与 CPython 一致，显式取整协议的返回值不再次强制转成 int。
- `log` / `log10` 对巨大正整数使用大整数对数路径，避免答案有限而输入转 float 先溢出；其他标量 API 不因此放宽巨大整数转换。
- `pow(nan, 0)` 与 `pow(1, nan)` 为 1；零的负无穷次幂为正无穷。NaN 不是普遍的定义域错误。
- 未列出的 `math` API 未实现；不声明整个模块或底层 libm 逐位兼容。普通有限输入使用宿主数学库，三角和对数结果以明确容差验收，跨平台末位可能不同。

### 成本与验收

普通标量使用 double 和宿主数学函数；任意精度取整与巨大整数对数使用 `BigInteger`，不为小整数引入统一大整数存储。没有据此宣称未经测量的性能提升。

- `runtime/RyuPy.Runtime.Tests/PyMathModuleTests.cs`：6 项测试覆盖逐 API、错误类型、NaN/无穷/负零、大整数、协议顺序及参数约束。
- `tests/e2e/phase3/m3_5_math_time.py`：CPython 差分覆盖常用结果、协议、定义域与溢出。

## time

入口：`PyTimeModule.CreateAttributes`。首批不包含日历、时区、格式化、CPU 计时和 `_ns` API。

| API | 参数与返回值 | 实际保证 |
| --- | --- | --- |
| `time()` | 无参数 → float 秒数 | UTC Unix epoch 墙钟；可能因系统校时回退，不作为耗时基准 |
| `monotonic()` | 无参数 → float 秒数 | 使用 Stopwatch 单调时钟；只承诺顺序和差值，不承诺与 CPython 相同的绝对锚点 |
| `perf_counter()` | 无参数 → float 秒数 | 使用同一个 Stopwatch 高分辨率计时源；适合计时差值 |
| `sleep(secs)` | 一个仅位置参数 → None | 零值让出调度机会；正值至少等待请求时长，允许调度造成延迟 |

`sleep` 接受 float 或整数协议 `__index__`，不接受仅提供 `__float__` 的对象。NaN 报 `ValueError`，无穷及超出有符号 64 位纳秒内部范围的值报 `OverflowError`；可表示的负数报 `ValueError`。普通时钟拒绝所有参数，sleep 拒绝关键字和错误参数数量。

### 等待策略、成本与验收

- 墙钟从 UTC ticks 转换成 float 秒；单调时钟由 Stopwatch ticks / Frequency 得到，不把墙钟用于等待截止判断。
- 正数 sleep 使用单调已逝时间校验剩余时间，并以向上取整的毫秒片段调用 `Thread.Sleep`。这避免 CLR 单次等待参数上限与提前返回，但可能增加约一个调度片段的等待；不忙等追求纳秒精度，不承诺实时调度。
- 宿主 `ThreadInterruptedException` 不泄漏给 Python：正数等待继续检查剩余时长，零等待结束调度让出。不宣称已经实现 Python 信号处理或线程 API。
- `runtime/RyuPy.Runtime.Tests/PyTimeModuleTests.cs`：4 项测试覆盖 epoch 范围、单调性、0 / 1 ms 等待、转换、域和溢出。没有长时间等待，也不设置易受 CI 负载影响的严格等待上界。
- 差分用例只比较时钟顺序、正的 epoch 值与返回 None，不比较不可重复的绝对时间输出。

## JSON：原生递归解析与增量编码

| API | 当前参数/行为 | 边界 |
| --- | --- | --- |
| `loads(s, *, cls=None, object_hook=None, parse_float=None, parse_int=None, parse_constant=None, object_pairs_hook=None, strict=True)` | str/bytes/bytearray；大整数、NaN/Infinity、插入顺序与重复键覆盖；pairs hook优先于object hook，数字hook收到原token | 自定义cls明确拒绝；未知参数TypeError；strict仅bool/int/None |
| `load(fp, **同loads选项)` | 调用一次fp.read，再用同一解析器 | 必须返回受支持的文本/字节；不管理fp生命周期 |
| `dumps(obj, *, skipkeys=False, ensure_ascii=True, check_circular=True, allow_nan=True, cls=None, indent=None, separators=None, default=None, sort_keys=False)` | None/bool/int/float/str/list/tuple/dict；支持default回调、缩进字符串/整数、分隔符对、稳定键排序、引用循环检测 | 自定义cls拒绝；布尔选项仅bool/int/None，自定义真值对象明确NotImplementedError而非提前执行副作用 |
| `dump(obj, fp, **同dumps选项)` | 增量编码，逐块读取当前fp.write；失败保留已写前缀，返回None | 不自动flush/close；不把自定义write副作用吞成一次整串写入 |
| `JSONDecodeError(msg, doc, pos)` | 真正ValueError子类型，保留msg/doc/pos/lineno/colno，公开位置按码点；导入别名可用于普通和生成器handler | 构造msg/doc需str、pos整数且处于文档范围；范围外明确拒绝；暂不提供异常属性任意写入 |

- bytes采用BOM/零字节模式检测UTF8/16/32；严格解码。CPython的surrogatepass孤立代理码点字节编码
  尚未实现，抛UnicodeDecodeError，独立pending保留；不称完整bytes JSON域。
- 深度上限256，超过报RecursionError；默认十进制整数转换上限4300位，解析自定义parse_int收到完整token。
  不提供sys动态调整这两个上限。大整数键和值使用同一限制。
- 错误位置与常见消息固定CPython3.12.13差分；不是仅验证.NET解析异常。无效UTF编码错误的完整字段/消息尚未统一。
- 保留UTF16底层关于两个独立代理项拼接的既有边界；ensure_ascii转义DEL及补充平面，字符串键按码点排序。
- JSONDecodeError导出只读；用于静态handler的import/from别名禁止跨模块改写，未被handler使用的普通别名不受该限制。
  本地同名json.py优先，不能误认为原生异常模块；动态except表达式仍未支持。
- 回归：`m3_5_json.py`、原`m3_5_json_math.py`、`m3_5_exception_aliases.py`与`PyJsonTests`。
  `m3_5_json_surrogate_bytes.py`、`m3_5_json_flag_objects.py`保留未完成范围。
- 成本：解析容器/字符串、大整数与排序键需要分配；dumps汇总编码块，dump不保存完整结果字符串；
  无吞吐加速测量，不承诺快于CPython。


## re：经过语法翻译的有限字符串正则

入口为 `PyRegex.CreateAttributes`，并非直接透传 `.NET Regex`。本节的支持范围只覆盖字符串模式与字符串输入，不代表完整 `re` 模块。

| API | 当前支持 | 边界 |
| --- | --- | --- |
| `compile(pattern, flags=0)` | str 或已有 Pattern；相同模式/flags 命中有界缓存 | 对已有 Pattern 另传非零flags报ValueError；bytes/bytearray明确NotImplementedError |
| `match/search/fullmatch(pattern, string, flags=0)` | 返回 Match 或 None；match限定起点，fullmatch限定整个范围 | 不把带末尾换行的`a$`搜索结果误作fullmatch |
| `findall/finditer(pattern, string, flags=0)` | 无捕获返回完整匹配；单捕获返回字符串；多个捕获返回tuple；finditer惰性 | findall未参与捕获使用空串；Match.group未参与捕获使用None |
| `sub/subn(pattern, repl, string, count=0, flags=0)` | 字符串模板或回调；subn保留次数；负count不替换 | 回调返回None表示空替换，其它非str返回TypeError；模板即使无匹配也先验证 |
| `escape(pattern)` / `purge()` | 按Python需转义字符处理str；清空编译缓存 | 不实现bytes escape |
| `Pattern.match/search/fullmatch/findall/finditer(string, pos=0, endpos=...)` | pos/endpos按码点裁切，锚点仍相对原输入起点；范围外整数夹紧到输入长度 | 当前显式整数参数需可转有符号32位；不支持任意大pos/endpos |
| `Pattern.sub/subn(repl, string, count=0)` | 与模块入口使用同一替换实现 | 不额外接受flags |
| `Pattern.pattern/flags/groups` | 原模式、整数flags、捕获组数量 | groupindex只读映射尚未实现，明确NotImplementedError；不提供完整Pattern对象协议 |
| `Match.group(*groups)/groups(default=None)/groupdict(default=None)` | 编号或命名组；默认缺失值；组号保持Python源码顺序 | group仅位置参数；不采用.NET命名组后置编号 |
| `Match.start/end/span(group=0)` / `expand(template)` | 码点位置、未参与组为-1、独立Python模板展开 | start/end/span的group为仅位置参数 |
| `Match.re/string/pos/endpos` | 原Pattern/字符串与本次搜索范围 | lastindex/lastgroup、下标与完整Match协议暂未开放 |
| `error(msg, pattern=None, pos=None)` | 真正Exception子类；保留msg/pattern/pos/lineno/colno和args；可用于原生模块typed handler | 无位置不提前格式化msg；有位置要求msg和pattern为str、pos为受支持整数 |

### 语法与 flags

- 支持字面量、字符类/否定类/范围、点号、交替、普通捕获、`(?P<name>...)`、非捕获组、正/负前瞻、常用贪婪/懒惰量词（含`{,n}`）、`^/$/\A/\Z`与`\b/\B`。
- 支持`\w/\W/\d/\D/\s/\S`，常用控制字符、十六进制与Unicode转义；模式中的已闭合编号/命名反向引用只在非IGNORECASE模式开放。字符类中的非零八进制转义当前明确拒绝。
- 字符串替换独立解析`\1`、`\g<name>`、`\g<0>`与控制/八进制转义；美元符号始终是普通文本，不借用.NET替换语法。
- 公开`I/IGNORECASE`、`M/MULTILINE`、`S/DOTALL`、`X/VERBOSE`、`A/ASCII`、`U/UNICODE`、`NOFLAG`的整数值，可按位组合；尚无完整RegexFlag/IntFlag对象。支持模式开头的全局flags。
- `LOCALE`用于str模式按Python报ValueError；ASCII与UNICODE同时出现报ValueError。其它未开放flags、scoped flags、后顾、条件组、原子组、占有量词、非ASCII组名、命名Unicode转义、IGNORECASE反向引用明确NotImplementedError。
- 无效语法抛真实`re.error`；不把`.NET (?<name>...)`、`\p{...}`等方言当作Python合法模式。常见位置由原模式码点计算；未逐条承诺所有错误消息与CPython完全一致。
- 未列出的模块入口（例如split/scanner）、对象方法与元数据不冒称已兼容。

### Unicode、空匹配与表示边界

`\w`采用Python字母/数字/下划线集合而非.NET的组合附标/连接符集合，`\s`包括Python支持的U+001C等控制字符。IGNORECASE按固定等价组扩展字面量/范围，但不改写字符类别本身；覆盖İ/ı/ſ/K与U+0345边界。ASCII模式使用ASCII类别与大小写关系。

字符集合翻译为完整Unicode标量对应的UTF16原子，根匹配与原子匹配均不落到有效代理对中间。每次输入操作建立一次码点边界表，span/pos和空匹配推进都按码点。既有字符串无法区分“两个独立代理项拼接”与“同一补充字符”的表示边界仍保留，不借本功能宣称该全局边界已解决。

空匹配后，在同一位置使用受控辅助模式重试非空匹配，失败才前进一个码点；以固定CPython与真实.NET验证`.*?`、`|a`、`a*`、`(?=a)`及sub/subn，不直接使用宿主Matches的空匹配推进规则。

### 容量、性能与数据来源

- 编译对象缓存最多64项FIFO；非空重试与fullmatch辅助执行器按需创建，不无条件编译三份模式。
- 用户模式上限16384个UTF16单元，翻译主体上限200000字符，超限抛NotImplementedError；组嵌套深度上限256，超限抛RecursionError，防止宿主不可捕获栈溢出。
- 每次宿主Regex.Match超过2秒抛RuntimeError。这不是整个findall/sub操作的总时间上限，也不是CPython无内建匹配超时行为；属于显式资源保护边界。
- 字符分类/大小写表固定CPython **3.12.13 / Unicode 15.0.0**，生成器为`tests/generate_regex_unicode.py`，产物`PyRegexUnicodeData.cs`。脚本校验解释器和Unicode版本；按固定解释器重新运行可逐字节复现，运行产物不依赖Python安装。
- 数据依据CPython `_sre.unicode_tolower`、`re._casefix`及`re.fullmatch`分类结果；[CPython 3.12.13源码](https://github.com/python/cpython/tree/v3.12.13/Lib/re)按PSF-2.0许可发布，生成脚本中保留来源说明。
- 编译后的Unicode集合、输入索引、Match包装与替换结果仍有分配成本；辅助执行器惰性创建和有界缓存限制重复工作/驻留量，没有跨引擎性能测量，不宣称比CPython快。

### 验收证据

`m3_5_regex.py`保持原验收内容；`m3_5_regex_edges.py`覆盖Unicode、组序、替换与空匹配；`m3_5_regex_boundaries.py`覆盖类别/flags矩阵、码点范围、回调None及error构造。`PyRegexTests`另验证显式拒绝、无匹配时模板验证与资源保护。差分基线固定CPython3.12.13，门禁结果以当前测试日志为准。
