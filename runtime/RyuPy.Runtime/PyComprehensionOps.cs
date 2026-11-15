// 编译器在精确enumerate守卫后拆分临时二元组；保留索引盒先于底层Current求值的顺序。
using System.Runtime.CompilerServices;
namespace RyuPy.Runtime;
public static partial class PyOps
{
    /// <summary>为无过滤单层列表推导式预留精确range的剩余容量；不执行length_hint或其它用户协议。</summary>
    public static object MakeComprehensionList(object iterator) => new PyList(iterator is PyRangeIter range ? range.RemainingCapacity() : 0);
    /// <summary>仅检测运行时精确enumerate游标，不调用Python协议。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnumerateIterator(object value) => value is PyEnumerateIter;
    /// <summary>守卫成功后先物化索引盒，沿普通Current的索引规则。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object EnumerateCurrentIndex(object value) => ((PyEnumerateIter)value).CurrentIndex();
    /// <summary>守卫成功后读取一次底层当前值，保留原引用。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? EnumerateCurrentValue(object value) => ((PyEnumerateIter)value).CurrentValue();
}

public sealed partial class PyList
{
    // 容量只影响分配，不预填元素；公开Items仍为同一背板。
    internal PyList(int capacity) => _items = new System.Collections.Generic.List<object?>(capacity);
}
