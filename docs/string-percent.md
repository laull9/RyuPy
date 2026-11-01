# 字符串百分号格式：有限竖切

当前有限子集已通过固定 CPython 3.12.13 差分、十一项运行时专测、两模式真实进程 traceback
验证与 termcolor 强制 ANSI 上游移植。整 M3.7 门禁仍由主线程聚合，不以子集通过冒称完整里程碑。

## API 与结果边界

`str % values` 支持无修饰的 `%s`、`%r`、`%a`、`%d`、`%i`、`%u` 与 `%%`，以及这些转换的 `%(key)` 映射键形式：

- tuple 是参数表；其它对象是一个参数。要把 tuple 作为 `%s` 的一个值，外面再包一层 tuple。
- `%s` 调 `str`，`%r` 调 `repr`，`%a` 仅调用一次 `repr` 后对非 ASCII 码点转义。
- 十进制转换接受真实整数/布尔/浮点，浮点向零截断；NaN 为 ValueError，Infinity 为
  OverflowError。字符串 `'12'` 不会经 `int` 偷偷被接受。
- 用户数值协议优先 `__int__`、缺失才 `__index__`；返回值必须是整数。
  返回 bool 的弃用警告路径暂明确 NotImplementedError，不静默吞警告。
- `%u` 保留负号，不转换成 CLR 无符号整数。
- 从左到右取参并立即转换，后项缺参/余参/转换异常不撤销或重复此前副作用。
  未知转换与不完整格式报 ValueError；参数不足/过量与不合类型报 TypeError。
- 精确内建 str 左操作数先执行自身格式协议，右侧 `__rmod__` 不能抢占；普通数值取模不变。

## 明确未支持

flags、宽度、precision、`*`、长度修饰与 `%o/%x/%X/%e/%E/%f/%F/%g/%G/%c`
均明确 NotImplementedError，不近似输出。尚未提供 bytes 百分号格式。

无转换项的余参规则不是简单的“只允许 dict”：固定 CPython 也允许具有 mapping slot 的
list/bytes/bytearray/range/memoryview。当前接受这些已确认内建类型、原生字典包装，以及
类或用户异常类中存在 `__getitem__` 的实例；不会为了探测而调用该方法。即使 slot 值为
None，无转换格式也合法；实际取键才会报不可调用的 TypeError。deque 的下标协议不等于
mapping slot，未消费参数时为 TypeError。其它尚未确认 slot 的 native 对象仅在确实需要
mapping/余参判定时明确 NotImplementedError，原有 `%s` 消费此对象不受影响。

## 映射键与副作用

- mapping 资格在格式化入口读取一次；前项 `__str__` 后来增加/删除类方法，不追溯改变资格。
- 格式中每个键重新调用真实 `PyOps.GetItem`，不缓存值。前一转换回调修改映射时，后续键可见新值。
- 键使用平衡括号解析，空键与包含括号的键均保留原字符串；缺少右括号不调用 `__getitem__`。
- 先确认 mapping 资格、解析完整键、取值，再检查转换字符。因此有完整键但缺转换字符仍先取键；
  缺键异常先于后续非法转换错误。原 `%s/%r/%a` 与十进制协议逐项执行，不预扫描执行用户转换。
- tuple 始终是位置参数表，str 也不作为 mapping 参数。`%s/%(x)s` 可先消费映射对象本身再取键，
  `%(x)s/%s` 则在取键后报告位置参数不足；不因出现映射键而跳过全部参数消费检查。

不提供全部 mapping/tuple 子类协议。底层字符串仍为 UTF-16，孤立代理拼成配对的既有边界
未由本格式器消除，不宣称完整 Python 字符串存储等价。

## 成本与验证

单遍扫描格式串，用 StringBuilder 累积输出；tuple 直接读取已有元素数组，单值不另建参数数组。
映射键解析只分配每次实际使用的键字符串，不建立映射快照或每键缓存；入口资格检查是纯类型/类字典查询。
不预格式化参数、不为每项创建函数/环境；`%a` 需要 repr 结果与一次额外转义输出。
不承诺测量前的加速数字，不复用 CLR 复合格式伪装 Python 语义。

- `m3_7_percent_format.py`：基础转换、Unicode、参数形状、逐次副作用、异常恢复、
  右反射优先级与数值取模不回归；已转正。
- `PyPercentFormatTests` 与 `PyPercentMappingTests`：十一项专测通过。
- `m3_7_percent_mapping.py`：unused自定义mapping、逐键转换与修改后重读、平衡键、错误顺序、入口资格快照。
- `tests/upstream/termcolor_force.py`：固定 2.4.0 的颜色/背景/属性及组合共 16 场景；
  原测试名、选定参数与 MIT 许可见该目录 README，已通过。

`percent_traceback.rs` 两模式锁定：用户数值协议的 TypeError 被重写时清理旧回调帧，
替换错误保留模块调用帧；ValueError 原样传播仍保留 `__int__` 帧。
日志 `/tmp/ryupy-m3_7-percent-{runtime,driver,e2e}.log` 与
`/tmp/ryupy-m3_7-percent-results.jsonl` 保存子集证据。上游最终复核日志为
`/tmp/ryupy-m3_7-percent-upstream-frozen-cli-results.jsonl`，使用本窗口已编 CLI 与最新
Release runtime；该历史局部运行不包含当时的模块 bound 改动。后续整树已由
`/tmp/ryupy-m3_7-milestone-final.log`最终门禁统一通过，progress固定入口亦原生通过。
