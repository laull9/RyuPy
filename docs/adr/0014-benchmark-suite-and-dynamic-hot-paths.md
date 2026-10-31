# ADR-0014：基准体系与动态热路径第二轮优化

- 状态：已接受
- 日期：2026-10-31
- 关联里程碑：M2.12
- 补充：[ADR-0012](0012-bounded-dynamic-pic.md)、[ADR-0013](0013-dynamic-hot-path-allocations.md)

## 背景

M2.11 之前的 `make bench` 只有 4 个基准（fib / mandelbrot / dynamic / containers），只报墙钟中位数。
它有三个问题：

1. **覆盖面窄**：两个是全标注静态路径，项目代码最常见的对象、容器、字符串、排序、异常几乎没有测到；
2. **口径单一**：墙钟里混着 .NET 宿主启动与 JIT，看不出分配量、GC、内存，路线图要求的「冷启动 / 稳态 /
   分配 / 内存」无从记录；
3. **不能定位**：没有剖析入口，也不能和历史结果对比，优化只能靠猜。

扩充后的基准立刻暴露了语义缺口（`[0] * n`、`list + list`、`list +=` 别名、`a[f()] += 1` 重复求值、
`x is True`），以及若干与语义无关的固定开销。

## 决定

### 1. 基准体系

- `tests/benchmarks/` 一个文件一个基准，文件头 `# bench:` 声明类别、说明、变体（同一 DLL 换环境变量）
  与 pending 里程碑。现有 22 个可运行基准覆盖 startup / static / numeric / dynamic / objects /
  containers / strings / algorithms / control / project，另有 4 个 Phase 3 基准（闭包、推导式、生成器、
  装饰器）标 pending，CPython 侧已可运行，特性落地后直接转正；
- 测量拆成墙钟（含启动）与**进程内执行耗时**。RyuPy 侧新增 `RYUPY_STATS=<文件>`（`PyStats.cs`）：
  进程退出时写出执行耗时、托管分配字节、各代 GC 次数与暂停、JIT 耗时与方法数；CPython 侧用计时外壳
  记录执行耗时与 GC 次数；双方的峰值内存来自 `wait4` 的 rusage。未设置环境变量时零成本；
- 报告给出中位数、波动、按类别与总体的几何平均；结果保存为 JSON，可 `--compare` 逐项对比；
  `--profile` 调用 `dotnet-trace` 输出自身 / 包含耗时最高的函数与 speedscope 火焰图；
- 所有基准的 stdout 与 CPython 逐字节比对，不一致即失败。基准仍不进入 `make check`。

### 2. 语义补齐（基准暴露的缺口，均有 e2e）

序列 `+` / `*`；增量赋值带 `inplace` 标记、可变对象原地修改；增量赋值目标的子表达式只求值一次；
`True` / `False` 与 [-5, 256] 整数的 `is` 与 CPython 一致；排序只调用 `<`。详见 semantics.md §4 #39–#43。

### 3. 动态热路径优化（按剖析结果排序）

| 项 | 问题 | 做法 | 正确性边界 |
| --- | --- | --- | --- |
| 动态函数直调 | 顶层 `def` 在 HIR 里已静态解析，静态函数早已直接 `call`，动态函数却仍走「实参数组 → PIC → Invoke → thunk」 | 编译期绑定计划成立且无 `*` / `**` 时直接 `call` 对应方法，实参装箱 | 与静态直调同一套名字绑定规则；绑定失败或带星号仍走运行时 binder，TypeError 消息不变 |
| i64 溢出检测 | `checked` + `catch OverflowException`：每次越界一次 CLR 异常分派（bigint 基准 67% 时间） | 位运算判断加减溢出，`Math.BigMul` 判断乘法溢出 | 越界仍升级 BigInteger；方法不含异常处理，可被内联 |
| 比较 / 相等快路径 | `PyOps.Compare` 每次先探测 `__eq__` / `__lt__`，再做形态分类 | long / double / str 两侧同类时直接比较 | 这些类型不可能有用户魔术方法；double 的 IEEE 比较满足 NaN 规则 |
| `TryMethod` 实参数组 | `TryMethod(a, "__eq__", [b])` 对内建值也先分配数组（sorting 1.8 GB 分配的主因） | `TryMethod1`：先判断是否可能有 Python 方法 | 只跳过不可能命中的查找 |
| 装箱缓存 | 比较结果、循环变量、小整数逐次 `box` | bool 复用运行时单例；[-5, 256] 走缓存（与 CPython 相同范围） | `PyOps.Is` 对 bool 与小整数按值判定，不依赖装箱来源 |
| 排序 | 每次比较 `==` + `<`；在 `List` 上逐元素回写 | 数组上二分插入（32 一段）+ 自底向上 ping-pong 归并；数值 / 字符串 / 元组比较快路径 | 只用 `<`；严格「右侧先于左侧」才取右侧，保证稳定；中途抛异常时列表不变 |
| PIC 替换策略 | 写入位置是 `site % EntriesPerSite`，同一站点永远写同一格——实际是单态缓存 | 每张表按站点轮转替换，容量 2 → 4 | 仍有界；未命中走完整通用路径 |
| 属性缓存 | 类上有任何 property 就整类放弃写入缓存；property、类属性每次走完整查找 | 写入只在「该名字不是 property」时缓存；新增 property getter 与类属性值条目（记录同名实例槽位以处理遮蔽） | 键为类对象 + 全局类版本，任何类修改都失效；数据描述符优先于实例属性 |
| `super().m()` | 每次遍历基类链并分配绑定方法 | 按站点缓存「定义类 + 版本 → 目标函数」 | 只缓存普通函数；property / classmethod 走通用路径 |
| 其它 | 二元运算的形状缓存命中与否都执行同一分派；实例槽位逐个扩容 | 去掉无效缓存查找，补 `//` `%` 位运算与 float 真除法快路径；实例槽位按类槽位数预分配 | `long / long` 只在两侧 ≤ 2^53 时用 double 除法（保持正确舍入） |

## 实测

macOS arm64（Apple M5）、.NET 10.0.403、Release、CPython 3.12.13，1 次热身 + 5 次中位数，
墙钟含进程启动（RyuPy 约 20 ms、CPython 约 15 ms）。「优化前」为 M2.11 代码加上本里程碑的语义补齐
与二元运算快路径重写后的首次全量测量（`pre-opt`；原 4 个基准在 M2.11 原始代码上分别为
containers 167、dynamic 195、fib 158、mandelbrot 263 ms）。

| 基准 | 类别 | CPython ms | 优化前 RyuPy ms | 优化后 RyuPy ms | 加速比（CPython / RyuPy） | 分配 MB 前 → 后 |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| fib | static | 305 | 154 | 168 | 1.82x | 0.2 → 0.2 |
| mandelbrot | static | 1096 | 265 | 263 | 4.18x | 7.7 → 7.7 |
| bigint | numeric | 69 | 529 | 123 | 0.56x | 212 → 120 |
| matmul | numeric | 63 | 190 | 183 | 0.34x | 212 → 144 |
| nbody | numeric | 231 | 361 | 362 | 0.64x | 758 → 653 |
| sieve | numeric | 85 | 317 | 264 | 0.32x | 500 → 134 |
| spectral_norm | numeric | 76 | 78 | 77 | 1.00x | 95 → 73 |
| calls | dynamic | 170 | 362 | 180 | 0.94x | 846 → 400 |
| dynamic | dynamic | 183 | 196 | 182 | 1.01x | 230 → 232 |
| attrs | objects | 119 | 336 | 162 | 0.74x | 204 → 168 |
| oop | objects | 92 | 334 | 186 | 0.49x | 150 → 92 |
| containers | containers | 42 | 170 | 190 | 0.22x | 2421 → 2417 |
| dict_ops | containers | 54 | 153 | 135 | 0.40x | 105 → 68 |
| list_ops | containers | 60 | 184 | 158 | 0.38x | 195 → 117 |
| set_tuple | containers | 75 | 173 | 157 | 0.48x | 196 → 142 |
| str_concat | strings | 182 | 1278 | 764 | 0.24x | 19088 → 19086 |
| strings | strings | 63 | 180 | 168 | 0.38x | 165 → 159 |
| nqueens | algorithms | 62 | 344 | 228 | 0.27x | 277 → 3 |
| sorting | algorithms | 137 | 787 | 275 | 0.50x | 1798 → 69 |
| exceptions | control | 58 | 235 | 213 | 0.27x | 114 → 90 |
| records | project | 72 | 335 | 285 | 0.25x | 223 → 86 |

几何平均加速比（不含 startup）：**0.40x → 0.53x**；RyuPy 自身相对优化前 **1.35x**。
按类别：static 2.76x、dynamic 0.98x、objects 0.60x、numeric 0.52x、algorithms 0.37x、containers 0.35x、
strings 0.30x、control 0.27x、project 0.25x。str_concat 的「优化前」一列波动达 53%，变化不应归因于本次改动。

## 仍落后的部分（不在本次范围）

- **容器与项目式代码（0.25–0.5x）**：下标、迭代、`in` 每次都要按 CLR 类型分派，值全部装箱。真正的
  改善依赖流敏感收窄、带守卫特化与逃逸分析（M3.9），本次不做；
- **异常（0.27x）**：Python 异常就是 CLR 异常，抛出成本由运行时决定（栈展开、栈追踪）。
  需要改变异常表示或在编译期识别「同函数内抛捕」，单独立项；
- **字符串累加（0.24x）**：`.NET string` 不可变，CPython 在引用计数为 1 时原地扩容。沿用 ADR-0013 的判断，
  不为此改变 `str` 的表示；
- **启动（0.75x）**：.NET 宿主约 20 ms 对 15 ms，留给 Phase 5（AOT / 启动优化）。

## 后果

- 新的优化需要先在 `make bench-save` / `bench-compare` 上给出对照，热点用 `bench-profile` 定位；
  `RYUPY_PIC=0` 仍可关闭 PIC 做 A/B（dynamic 基准：182 ms 对 293 ms）；
- PIC 表从每站点 2 条增至 4 条，静态数组总计约 1–2 MB（按需分页，hello 启动时间未见变化）；
- 小整数缓存让 [-5, 256] 的 `is` 与 CPython 一致，范围外仍按对象身份——与 CPython 一样不应依赖；
- 动态函数直调绕过了 `PyFunction` 对象：只对编译期静态解析为顶层 `def` 的调用成立。M3.0 的嵌套函数 /
  lambda 是运行时值，必须继续走 `call_value`；若以后允许运行时替换模块级函数绑定，需要给直调加身份守卫；
- 运行时文件按职责拆分后都 ≤ 600 行；Rust 侧超限文件登记在 TODO 技术债，M3.0 前先拆。
