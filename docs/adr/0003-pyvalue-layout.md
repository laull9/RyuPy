# ADR-0003：`PyValue` 的内存布局

- 状态：提议（Phase 2 开工前定稿）
- 日期：2026-10-31

## 背景

roadmap 设计了一个 16 字节的 `PyValue`：

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
即 IronPython 的做法，违背「动态路径零堆分配」的目标，排除。

## 倾向

倾向 B。Phase 2 开工时先写一个 BenchmarkDotNet 微基准对比 A 与 B（`a + b` 热循环），用数据定稿。

## 后果

- 无论选 A 还是 B，都需要更新 roadmap 中的示例代码。
- 另需注意：roadmap Phase 4 的 `NdView1D<T>` 被设计为 `ref struct`，它**不能**作为 `PyValue` 的字段、
  不能放进堆对象、不能跨 `yield`/`await` 存活。动态路径持有 ndarray 时需要另一个非 ref 的句柄类型。
