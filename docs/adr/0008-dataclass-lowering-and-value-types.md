# ADR-0008：@dataclass 降级与 .NET 值类型映射

- **状态**：Accepted
- **日期**：2026-10-31
- **决策者**：大喵老师 & USER
- **相关 issue / PR**：M1.5

---

## 1. 背景

在 Python 中，`@dataclass` 是表达结构化数据的标准方式。CPython 的所有类实例（包括 dataclass 和 `frozen=True` 的 dataclass）在语言底层都是堆分配的引用类型（`PyObject*`），变量之间的传递总是传递引用（指针别名）。

而 RyuPy 的目标是将静态类型的 Python 高效编译为 .NET CIL：
- 在科学计算、物理仿真、游戏引擎等领域，诸如向量（`Vec2`、`Vec3`）、复数、点坐标等小对象往往被数百万次地创建、传递和计算；
- 若全部使用 .NET `class`，每次实例化都会带来 GC 堆分配与指针解引用开销，严重影响局部性与吞吐量；
- .NET 提供了第一类的值类型支持（`System.ValueType`，即 struct），能够在栈上或内联在包含对象中分配，实现零 GC 分配的高性能计算。

因此需要决定：如何将 `@dataclass` 降级为 .NET 类型，以及如何处理 Python 引用语义与 .NET 值语义的差异。

---

## 2. 备选方案

### 方案 A：所有 `@dataclass` 均映射为 .NET `class`
- **优点**：与 Python 对象的引用语义完全一致（引用共享、别名修改行为一致）。
- **缺点**：对于小对象和不可变向量失去 .NET 原生值类型的性能优势，无法达到 C# / Rust 级别的内存紧凑性。

### 方案 B：普通 `@dataclass` 映射为 `class`，`@dataclass(frozen=True)` 映射为 `struct`（采用）
- **优点**：
  1. 普通 `@dataclass` 具备完全的引用语义与可变性；
  2. `frozen=True` 在 Python 语义上即声明为「不可变数据」；编译为 .NET `valuetype` 后实现零堆分配、紧凑内存布局（Sequential Layout）与高缓存局部性；
  3. 由于 `frozen=True` 实例的所有字段不可变，传参或赋值产生的值拷贝**无法通过属性修改被外部感知**，因而在语义上对纯数据运算与 CPython 观测等价。
- **潜在语义差异**：
  - 在 Python 中，同一不可变对象在赋值后 `a is b` 为 `True`（比较对象标识）；而在 .NET 中，值类型会被值拷贝，若发生装箱其 `is` 或引用比较将失效。
  - 防范措施：Phase 1 暂不开放 `is` 运算符；在类型检查阶段强制禁止任何对 `frozen=True` 属性的修改赋值（报 `E0203` 错误）。

---

## 3. 决定

采用**方案 B**：
1. **HIR 降级**：
   - 解析 `@dataclass` 与 `@dataclass(frozen=True)` 修饰器，标记 `Class.frozen`；
   - 提取所有字段的名称与类型标注，拦截不支持的类继承与方法定义（Phase 1 范围限制）。
2. **类型检查**：
   - 校验构造函数参数数量与类型兼容（支持 `int -> float` 提升）；
   - 校验属性读取字段是否存在；
   - 属性写入时，检查目标类的不可变性：若 `cls.frozen == true`，直接报告 `E0203`（不可修改 frozen dataclass 实例）。
3. **MIR 表达**：
   - `MirType::Class(ClassDefId)` 区分 `is_value_type`；
   - 发射 `Rvalue::NewObject` 表达构造、`Rvalue::GetField` 表达属性读取、`Statement::SetField` 表达字段赋值。
4. **CIL 代码生成**：
   - 普通 dataclass：继承 `[System.Runtime]System.Object`，标记 `sealed`；生成调用基类 `.ctor` 的构造函数与 `ldfld` / `stfld`；
   - frozen dataclass：继承 `[System.Runtime]System.ValueType`，标记 `sealed`、`Sequential` 内存布局；生成赋值各字段的构造函数；
   - 方法参数与局部变量在签名中发射 `valuetype RyuPy.Generated.<Name>`，实现完全原生值传递。

---

## 4. 后果与影响

- **收益**：
  - 物理仿真与数学计算场景中，向量运算（如 `m1_5_dataclass.py` 中的 `Vec2` 点乘）完全避免了堆分配，达到近乎纯数值计算的机器码级性能；
  - 架构分层清晰，HIR/Ty/MIR/Codegen 逐级精化，全部文件保持在 600 行以内。
- **局限与未来演进**：
  - 后续阶段若支持 `id()` 或 `is` 运算符，需要明确规范值类型的比较语义（或禁止对值类型使用 `is`，对齐 Python 社区实践）。
