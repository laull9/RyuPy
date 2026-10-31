# ADR-0003：`PyValue` 的内存布局

- 状态：提议（Phase 2 开工前定稿）
- 日期：2026-10-31

## 背景

旧版 roadmap 设计了一个 16 字节的 `PyValue`；下面保留问题示例，当前路线图已删除该布局：

```csharp
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct PyValue {
    [FieldOffset(0)] public readonly PyTag Tag;
    [FieldOffset(8)] public readonly long IntVal;
    [FieldOffset(8)] public readonly double FloatVal;
    [FieldOffset(8)] public readonly object? ObjRef;   // ✗ 与 long/double 重叠
}
```

**这个布局在 CLR 上不合法**：GC 必须能精确判断每个槽位是否为对象引用，因此运行时禁止引用字段与非引用字段重叠，
加载该类型时会抛出 `TypeLoadException`（"contains an object field at offset 8 that is incorrectly aligned or overlapped by a non-object field"）。

## 方案

### A. 24 字节：`Tag` + 8 字节数值联合 + 独立的 `object?` 字段
```csharp
public readonly struct PyValue {
    private readonly object? _obj;   // 8B
    private readonly long _bits;     // 8B，double 用 BitConverter.Int64BitsToDouble
    private readonly PyTag _tag;     // 1B，对齐后占 8B
}
```
简单直观；代价是体积增加 50%，按值传递时寄存器压力更大。

### B. 16 字节：用「哨兵对象」兼作类型标签
```csharp
public readonly struct PyValue {
    private readonly object? _obj;   // 堆对象本身，或标量类型的静态哨兵
    private readonly long _bits;     // 标量数据
}
// 例：Int 值 = (_obj: PyTags.Int, _bits: 42)；str 值 = (_obj: "hello", _bits: 0)
```
类型检查是一次指针比较（`ReferenceEquals(_obj, PyTags.Int)`），保持 16 字节且对 GC 合法。
缺点：取堆对象类型时需要先排除哨兵；`None` 也要一个哨兵。

### C. 标量直接装箱到 `object`
实现简单，但标量算术频繁装箱增加分配。保留为性能对照，优先比较 A / B；不把所有动态操作承诺为零分配。

## 倾向

倾向 B。M2.1 先用 BenchmarkDotNet 比较 A / B，覆盖算术、调用、容器访问与 GC，用数据定稿。

## 后果

- 无论选 A 还是 B，都需要记录 ABI、值传递成本、布局大小、标量与堆引用的生命周期，并同步运行时和后端引用。
- 另需注意：Phase 4 的短期数组视图可采用 `ref struct`，它**不能**作为 `PyValue` 的字段、
  不能放进堆对象、不能跨 `yield`/`await` 存活。动态路径持有 ndarray 时需要另一个非 ref 的句柄类型。
