# RyuPy 编译器架构与五阶段技术路线设计文档

> **修订说明（2026-10-31）**：本文是项目的**愿景文档**，保留原貌。落地时的调整如下，以对应文档为准：
>
> - 实际 crate 分层（新增 `ryupy-base` / `ryupy-syntax` / `ryupy-driver` / `ryupy-e2e`）见 [architecture.md](architecture.md)。
> - MIR 采用非 SSA 形式，见 [ADR-0002](adr/0002-mir-non-ssa.md)。
> - **Phase 2 的 `PyValue` 示例布局在 CLR 上不合法**（引用字段不能与 `long`/`double` 重叠），见 [ADR-0003](adr/0003-pyvalue-layout.md)。
> - Phase 4 的 `NdView1D<T>` 是 `ref struct`，不能存入 `PyValue`、堆对象或跨 `yield`/`await`，见 ADR-0003「后果」。
> - Phase 3 生成器「实现 `IEnumerator<T>` 的状态机结构体」：通过接口使用时会被装箱，建议用 class（与 C# 编译器一致）。
> - 「第一周启动清单」已被 [TODO.md](../TODO.md) 中按里程碑拆分的任务取代。
> - 运行生成程序先用子进程 `dotnet x.dll`，`netcorehost` 推迟引入。

**项目代号：** `RyuPy`（Python on RyuJIT & .NET Native AOT）

**宿主语言：** Rust（编译器前端、中端优化、CIL 代码生成） + C#（极简运行时底座 `RyuPy.Runtime`）

**核心哲学：** **“静态代码走原生拆箱快车道，动态代码走值类型内联缓存，成熟基建 100% 复用现有生态。”**

---

## 一、 核心架构哲学：为什么 RyuPy 能兼顾“极致性能”与“Native AOT”？

传统动态语言编译器（如 IronPython）之所以慢且无法支持 AOT，是因为它将所有变量无脑装箱为堆上的 `System.Object`，并在运行期依赖 `Reflection.Emit` 动态生成字节码。

**RyuPy 采用“动静双轨 + 零运行时 Emit”架构：**

1. **静态快车道（Unboxed Fast-Path）：** 凡是有类型标注（Type Hints）或能被前端通过数据流推导出的变量、函数与类，直接编译为 .NET 原生值类型（`int64`, `float64`, `bool`）和静态强类型 `struct`/`class`，生成纯粹的静态 CIL 指令，交由 .NET **RyuJIT** 进行自动向量化（AVX-512）与内联。
2. **动态慢车道（Zero-Allocation Tagged Union）：** 无法静态推导的动态变量，**绝不使用 `System.Object` 装箱**，而是统一编译为 `RyuPy.Runtime` 提供的 16 字节栈上值类型结构体 `PyValue`（包含类型 Tag 与 64 位数据槽位）。标量类型在动态模式下依然是**零堆分配、零 GC 压力**。
3. **100% 兼容 .NET Native AOT：** 所有的动态分发（运算符重载、属性查找）均通过预编译的静态多态内联缓存（Polymorphic Inline Cache, PIC）状态机完成，完全不依赖运行期动态生成 IL。这意味着任何 RyuPy 编译出的 `.dll`，都能直接通过微软官方的 **Native AOT (`ilc`)** 编译成无虚拟机依赖的原生机器码可执行文件。

---

## 二、 “站在巨人肩膀上”：成熟第三方库选型矩阵

遵循“绝不重复造轮子”**的原则，RyuPy 将词法分析、语法解析、CFG 图算法、PE/CIL 二进制封装及宿主调用全部卸载给成熟的工业级开源库，让核心开发只聚焦于**类型推导、中端降级与运行时语义。

| 模块层级 | 选用的成熟第三方库 | 替你省掉的工作量与核心作用 |
| --- | --- | --- |
| **Python 词法/语法前端** | **`ruff_python_parser`** + **`ruff_python_ast`** | **直接省去 3 万行代码。** Astral（Ruff / uv 团队）出品的工业级解析器，完美处理 Python 3.12+ 缩进状态机、f-string、模式匹配，速度极快且自带容错 AST。 |
| **源码位置与诊断报错** | **`ruff_text_size`** + **`ariadne`** | 零成本管理源码字节偏移（`TextRange`），一行代码渲染出 `rustc` 级别的多行彩色类型报错与修复提示。 |
| **扁平内存池与标识符驻留** | **`la-arena`** + **`ustr`** (或 **`lasso`**) + **`rustc-hash`** | `la-arena` 提供类型安全的 `Idx<T>` 索引句柄（彻底消除 Rust 生命周期传染）；`lasso` 实现多线程字符串驻留（String Interning）。 |
| **控制流图与中端优化** | **`petgraph`** + **`egglog`** (可选) | 用 `petgraph` 直接计算支配树（Dominator Tree）、强连通分量与死代码消除；进阶阶段可直接引入 `egglog` 做基于等式饱和（E-Graph）的算子代数化简。 |
| **CIL 字节码与 `.dll` 生成** | **`dotnetdll`** | **直接省去 2 万行二进制封装代码。** 纯 Rust 编写的 ECMA-335 标准库（`rustc_codegen_clr` 同款底座），直接用 Rust 构造类型表、方法表与 CIL 指令流并序列化为合法 `.dll`。 |
| **CoreCLR 进程内联调** | **`netcorehost`** + **`insta`** | 在 Rust 测试进程中直接拉起本地 .NET 运行时，内存中编译并立即执行生成的 `.dll`；配合 `insta` 对生成的 CIL 汇编文本进行自动化快照回归测试。 |
| **NumPy / C 扩展互操作** | **`pyo3-ffi`** + **`dlpack`** + **Python Buffer Protocol** | 直接复用 `pyo3-ffi` 的 CPython ABI 绑定与 `Py_buffer` 零拷贝内存视图协议，将 NumPy 底层连续内存直接映射为 .NET 的 `Span<T>` / `UnmanagedMemory`。 |

---

## 三、 Cargo Workspace 模块划分与编译流水线

### 1. 仓库目录结构

```text
ryupy/
├── Cargo.toml
├── crates/
│   ├── ryupy-cli/          # 命令行入口 (build / run / aot / test)
│   ├── ryupy-hir/          # 将 ruff_python_ast 降级为去糖后的 HIR，完成名称解析
│   ├── ryupy-ty/           # 双向类型推导、流敏感类型收窄、动静边界标记 (Fast/Slow Path)
│   ├── ryupy-mir/          # 基于 CFG 基本块的 SSA 中端表示、逃逸分析、yield 状态机降级
│   └── ryupy-codegen-cil/  # MIR 栈化 (Stackification)、基于 dotnetdll 生成 .NET 程序集
└── runtime/
    └── RyuPy.Runtime/      # C# 编写的运行时核心库 (PyValue 结构体、DLR 缓存表、内置类型、NumPy 桥)

```

### 2. 核心编译流水线

```text
Python 源码 (.py)
  │
  ├─► [1. ruff_python_parser] ──► Python AST (借用现成解析器)
  │
  ├─► [2. ryupy-hir]          ──► 语法糖展开 (推导式/装饰器/with) + 作用域符号表 (SymbolId)
  │
  ├─► [3. ryupy-ty]           ──► 渐进类型推导 (区分 NativeType 与 Dynamic PyValue)
  │
  ├─► [4. ryupy-mir]          ──► 构造 CFG 基本块 + 闭包捕获提升 + Generator 状态机转换 + 插入 Box/Unbox 边界守卫
  │
  ├─► [5. ryupy-codegen-cil]  ──► 寄存器转求值栈 (Stackification) + dotnetdll 吐出 .dll
  │
  └─► [6. .NET CLR / ILC]     ──► RyuJIT 即时编译执行  或  Native AOT 编译为独立二进制 (.exe)

```

---

## 四、 五阶段演进技术路线图（Phased Roadmap）

### Phase 1：静态强类型子集与原生值类型底座（MVP 闭环）

**目标：** 打通从 Python 源码到 `.NET .dll` 再到 **Native AOT 独立可执行文件**的全链路，在纯静态数值与结构化计算上达到与原生 C# 1:1 的性能。

* **语言特性范围：**
* 要求函数签名带有类型标注（`def foo(a: int, b: float) -> float:`），函数体内局部变量由 `ryupy-ty` 自动推导。
* 支持基础标量（`int` 映射为 `int64`，`float` 映射为 `float64`，`bool`、`str`）、算术与位运算、`if/elif/else`、`while`、`for i in range(...)`（前端直接识别 `range` 语法糖并降级为原生计数循环，不生成迭代器对象）。
* 支持 `@dataclass` 与静态类：属性固定，禁止动态增删字段，直接编译为 .NET 的 `class` 或值类型 `struct`（当标记 `@dataclass(frozen=True)` 时）。


* **核心编译技术任务：**
1. **AST 到 HIR 降级：** 对接 `ruff_python_parser`，实现词法作用域分析（Lexical Scope Resolution），区分局部变量（`LocalId`）与全局变量。
2. **局部类型推导器：** 基于并查集（Union-Find）实现函数内变量类型推导与类型一致性校验。
3. **CIL 栈化代码生成（Stackification）：** 将三地址 MIR 指令转换为满足 ECMA-335 栈平衡校验（Stack Verification）的 CIL 指令序列，利用 `dotnetdll` 输出 `.dll` 与 `.runtimeconfig.json`。


* **阶段里程碑（Milestone 1）：**
* 编译运行 Fibonacci、Mandelbrot 集、矩阵乘法、N-Body 物理模拟，耗时比 CPython 快 **50~100 倍**，且能通过 `dotnet publish /p:PublishAot=true` 生成脱离 .NET 环境直接运行的 2MB 二进制程序。



---

### Phase 2：渐进式动态类型支持与零堆分配 `PyValue`

**目标：** 移除“必须写类型标注”的限制，支持未标注的动态 Python 代码与静态类型代码无缝混写，动态路径通过内联缓存预热提速。

* **核心运行时设计（在 `RyuPy.Runtime` 中实现 `PyValue`）：**
为了避免传统 `object` 装箱带来的 GC 灾难，在 C# 运行时中定义一个 16 字节对齐的只读值类型联合体：
```csharp
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct PyValue
{
    [FieldOffset(0)] public readonly PyTag Tag;      // Int64, Float64, Bool, None, ObjectRef
    [FieldOffset(8)] public readonly long IntVal;    // 标量直接存在栈/寄存器上，零堆分配！
    [FieldOffset(8)] public readonly double FloatVal;
    [FieldOffset(8)] public readonly object? ObjRef; // 仅当类型为 str/list/dict/自定义类时使用堆引用
}

```


* **核心编译技术任务：**
1. **渐进类型边界注入（Gradual Boundary Lowering）：**
* 当静态 `int64` 传入未标注类型的动态函数时，MIR 自动插入 `PromoteToPyValue` 指令（构造栈上 `PyValue`）。
* 当动态 `PyValue` 赋值给标注了 `x: int` 的静态变量时，MIR 自动插入 `UnboxInt64OrThrow` 守卫（检查 `Tag == PyTag.Int64`，失败则抛出标准 Python `TypeError`）。


2. **AOT 友好的内联缓存（Static CallSite PIC）：**
* 动态运算 `a + b` 编译为 `PyOps.Add(ref callSiteCache, a, b)`。
* `callSiteCache` 是一个静态结构体槽位，记录最近一次执行成功的类型对与函数指针（如 `(Int64, Int64) -> &AddInt64`）。预热 1 次后，后续循环仅需一次整数比较即可直接命中快路径，且完美兼容 .NET 9/10 的 **Dynamic PGO（动态剖析引导优化）**。


3. **核心动态容器接入：** 在 `RyuPy.Runtime` 中基于 .NET 高性能集合封装符合 Python 语义的 `PyList`、`PyDict`（保证插入顺序）、`PyTuple` 和 `PySet`。


* **阶段里程碑（Milestone 2）：**
* 不加任何类型标注运行标准 Python 脚本；一旦在热点函数加上 `x: int` 标注，编译器自动消去 `PyValue` Tag 检查，性能呈阶梯式飙升。



---

### Phase 3：中端编译优化与高级控制流降级

**目标：** 攻克 Python 最具表现力的高级语法（闭包、生成器、协程、模式匹配），并在 `ryupy-mir` 层引入深度静态优化，把动态开销榨干。

* **核心编译技术任务：**
1. **流敏感类型收窄（Flow-Sensitive Type Narrowing）：**
在 CFG 上运行数据流分析：如果动态变量 `x` 经过了 `if isinstance(x, int):` 分支，或者在循环前被赋值为整数且循环内仅有整数加法，MIR 优化 Pass 自动将该基本块支配子树（Dominator Subtree）内的 `PyValue` **自动拆箱（Auto-Unboxing）** 为原生 `int64` 局部变量。
2. **逃逸分析与标量替换（Escape Analysis & Scalar Replacement）：**
Python 代码极其喜欢用临时元组交换变量或返回多值（如 `a, b = b, a` 或 `return x, y`）。MIR 逃逸分析识别未逃逸出当前函数的临时 `PyTuple` 或小型对象，直接在栈上拆解为独立的 CIL 局部变量（或利用 .NET 的 `ValueTuple<T1, T2>`），实现**零堆内存分配**。
3. **闭包转换（Closure Conversion）：**
分析嵌套函数捕获的 `nonlocal` 变量，自动在 CIL 中生成隐藏的闭包环境类（`DisplayClass`），将跨作用域读写的变量提升为环境类的字段。
4. **生成器（`yield`）与协程（`async/await`）状态机降级：**
* 将含 `yield` 的函数在 MIR 层重写为实现 .NET `IEnumerable<T>` / `IEnumerator<T>` 的状态机结构体，利用 CIL 的 `switch` 指令实现零开销跳转恢复。
* 将 Python 的 `async def` 降级为 .NET 原生的 `AsyncTaskMethodBuilder` 状态机，底层直接跑在 .NET 世界级的线程池（Work-Stealing ThreadPool）上。




* **阶段里程碑（Milestone 3）：**
* 完整支持列表/字典推导式、`yield` 生成器管道、装饰器与异步并发，且生成的 `IEnumerable` 和 `Task` 可被 C# 代码直接无缝调用。



---

### Phase 4：NumPy 与原生 C 扩展生态无缝兼容

**目标：** 打破非官方 Python 编译器无法使用科学计算与量化库的魔咒，通过成熟协议实现 **NumPy / PyTorch / C 扩展的零拷贝互通**。

为了避免自己重写几百万行的 C 扩展，本阶段采用“双引擎桥接 + 零拷贝内存视图”策略：

```text
+-----------------------------------------------------------------------+
|                         RyuPy 编译生成的 .NET 进程                     |
|                                                                       |
|  [RyuPy 静态/JIT 代码]  ◄─── 零拷贝 Span<double> ───► [NumPy ndarray]  |
|         │                    (Py_buffer / DLPack协议)        ▲        |
|         │                                                    │        |
|         └──────► 通过 pyo3-ffi / P/Invoke 调度控制流 ─────────┘        |
|                                                  (内嵌 libpython.so)  |
+-----------------------------------------------------------------------+

```

* **核心实现机制：**
1. **控制流桥接（复用 `pyo3-ffi` / `libpython`）：**
当 RyuPy 编译器检测到 `import numpy as np` 等外部 C 扩展导入时，由 `RyuPy.Runtime` 通过 P/Invoke 按需在进程内初始化轻量级 `libpython` 宿主，负责 C 扩展模块的加载与高层算子调用（如 `np.linalg.inv`）。
2. **数据面零拷贝直通（Python Buffer Protocol $\leftrightarrow$ .NET `Span<T>`）：**
这是性能封神的关键：当用户在 RyuPy 的循环或自定义算子中访问 `ndarray` 时，绝不通过 CPython API 逐个读取元素！
* `RyuPy.Runtime` 调用一次 C-API `PyObject_GetBuffer` 获取 `Py_buffer` 结构体，直接拿到 NumPy 数组的底层裸指针 `void* buf`、维度 `shape` 与步长 `strides`。
* 将该裸指针直接包装为 .NET 的无栈开销泛型切片 `Span<double>`（或自定义的值类型多维视图 `NdView<double>`）。
* **RyuPy 的静态编译器将针对该视图的下标访问 `arr[i, j]` 直接编译为原生指针偏移指令（`ldind.r8` / `stind.r8`）**，让 .NET RyuJIT 直接在 NumPy 的内存块上进行 **SIMD（AVX2/AVX-512）向量化计算**。


3. **DLPack 零拷贝张量协议接入：**
引入 `dlpack` 标准头结构，支持将 RyuPy/NumPy/PyTorch 的 CPU 及 CUDA 显存指针与 .NET 生态（如 `TorchSharp`、ONNX Runtime 或自定义 CUDA 内核）进行零拷贝跨语言传递。


* **阶段里程碑（Milestone 4）：**
* 在同一个 `.py` 脚本中直接 `import numpy as np` 生成矩阵，随后进入 RyuPy 编写的密集双层循环指标计算函数，无需改写为向量化算子，执行速度直接持平手写 C++/Cython。



---

### Phase 5：99.9% 语言长尾语义与一键 Native AOT 工具链

**目标：** 补齐 Python 动态元编程长尾特性，打造媲美 `cargo` / `uv` 体验的现代化单文件编译器 CLI。

* **核心长尾语义攻关：**
1. **大整数溢出自动升级（Overflow Promotion）：**
在要求 100% 严格兼容的模式下，静态 `int` 算术指令发射 CIL 的 `add.ovf` / `mul.ovf`（硬件级溢出标志检查）；一旦捕获 `OverflowException`，自动转入慢路径升级为 .NET 的 `System.Numerics.BigInteger`。
2. **完整对象模型（MRO 与描述符协议）：**
在 `RyuPy.Runtime` 中实现 C3 线性化多重继承算法、描述符协议（`__get__`, `__set__`）、元类（`metaclass`）以及猴子补丁（Monkey Patching）版本号失效机制（当类的属性字典发生修改时，递增类的 `VersionTag`，使关联的 PIC 内联缓存自动失效并重新绑定）。
3. **异常模型精确映射：**
将 Python 的异常继承树直接建模为继承自 `System.Exception` 的 .NET 异常类，利用 CIL 原生的 `.try` / `catch` / `finally` 异常表实现零开销正常路径执行。


* **工具链与 Native AOT 终极集成：**
* 封装 `ryupy build main.py --aot`：Rust 编译器先在几毫秒内吐出 `main.dll` 并链接 `RyuPy.Runtime.dll`，随后在后台自动调用 .NET SDK 的 **ILCompiler (`ilc`)**，直接输出单一的原生机器码可执行文件或供其他语言调用的 C-ABI 动态库。



---

## 五、 核心数据结构与代码范式蓝本（立即可建仓起步）

为了让你建仓时直接拥有最清爽的工业级骨架，以下是 Rust 中端 IR 与零拷贝 NumPy 桥接的核心接口设计：

### 1. Rust 侧：零生命周期传染的混合类型 MIR（`crates/ryupy-mir/src/lib.rs`）

```rust
use la_arena::{Arena, Idx};

pub type LocalId = Idx<LocalDecl>;
pub type BlockId = Idx<BasicBlock>;

/// 动静双轨类型表示：决定生成原生 CIL 指令还是 PyValue 操作
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum MirType {
    /// 静态快车道：直接映射为 CIL 原生类型，零装箱
    I64,
    F64,
    Bool,
    Str,
    Class(u32),
    /// 零拷贝数值视图：直接映射为裸指针 + 步长，支持 RyuJIT SIMD
    NdArrayView { elem_ty: Box<MirType>, ndim: u8 },
    /// 动态慢车道：映射为 RyuPy.Runtime.PyValue (16字节栈上 Struct)
    Dynamic,
}

pub struct LocalDecl {
    pub ty: MirType,
    pub is_captured: bool, // 若为 true，由 Closure Conversion Pass 提升至 DisplayClass
}

pub enum Rvalue {
    /// 原生无装箱算术（对应 CIL: add, sub, mul, div）
    FastBinOp { op: BinOp, lhs: LocalId, rhs: LocalId },
    /// 动态内联缓存调用（对应 RyuPy.Runtime.PyOps::Add(ref site, a, b)）
    DynamicBinOp { site_id: u32, op: BinOp, lhs: LocalId, rhs: LocalId },
    /// 动静边界转换指令
    BoxToPyValue { src: LocalId, src_ty: MirType },
    UnboxFromPyValue { src: LocalId, target_ty: MirType },
}

pub struct BasicBlock {
    pub stmts: Vec<(LocalId, Rvalue)>,
    pub terminator: Terminator,
}

```

### 2. C# 运行时侧：NumPy 零拷贝内存桥接（`runtime/RyuPy.Runtime/NumPyBridge.cs`）

```csharp
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

/// <summary>
/// 零堆分配的一维/多维 NumPy 内存视图，RyuPy 静态循环可直接内联访问
/// </summary>
public readonly unsafe ref struct NdView1D<T> where T : unmanaged
{
    private readonly T* _dataPtr;
    private readonly nint _length;
    private readonly nint _strideBytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NdView1D(void* buf, nint length, nint strideBytes)
    {
        _dataPtr = (T*)buf;
        _length = length;
        _strideBytes = strideBytes;
    }

    public ref T this[nint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 当步长连续时，RyuJIT 会直接消除乘法并触发 AVX-512 自动向量化
            byte* bytePtr = (byte*)_dataPtr + (index * _strideBytes);
            return ref *(T*)bytePtr;
        }
    }

    public nint Length => _length;
}

```

---

## 六、 第一周启动清单（Day 1 ~ Day 7 Action Plan）

1. **Day 1：** 初始化 Cargo Workspace，引入 `ruff_python_parser`、`la-arena`、`dotnetdll` 和 `insta`，写 50 行代码跑通“输入一段 Python 源码字符串 $\rightarrow$ 打印 Ruff AST”。
2. **Day 2~3：** 编写 `ryupy-hir` 与 `ryupy-ty` 的最小闭环，支持 `int`、`float`、`bool` 标量的双向类型推导，对类型不匹配的表达式通过 `ariadne` 报出带源码高亮的错误。
3. **Day 4~5：** 在 `ryupy-codegen-cil` 中用 `dotnetdll` 构造第一个包含 `Main` 入口和静态方法的 `.NET .dll`，实现表达式求值栈化（Stackification）和 `while`/`if` 分支跳转。
4. **Day 6~7：** 跑通第一个完整的端到端测试：用 `ryupy` 编译计算第 45 项递归/迭代斐波那契数列的 Python 脚本，用 `dotnet` 运行生成的 `.dll`，并执行 `dotnet publish /p:PublishAot=true` 拿到第一个原生机器码版本的 `RyuPy` 可执行文件。
