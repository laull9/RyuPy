using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>编译器专用数值列表ABI；guard不物化、不调用用户协议、不改变Python可见类型。</summary>
public static class PyNumericListOps
{
    /// <summary>创建仅由编译器证明fresh构造路径调用的数值背板列表。</summary>
    public static object Create() => PyList.CreateNumeric();

    /// <summary>追加编译器证明唯一sink的fresh浮点结果；普通模式仍正常新建盒。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AppendF64(object list, double value)
    {
        if (list is not PyList target) return AppendError();
        target.AppendFreshF64(value);
        return PyNone.Instance;
    }

    /// <summary>精确列表和actual long下标的纯探测，失败返回CLR null供编译器守卫。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? PeekList(object list, object index) =>
        list is PyList target && index is long integer && target.TryNumericPosition(integer, out int position)
            ? target.PeekChildList(position) : null;

    /// <summary>精确列表/long/范围/actual double的纯守卫；不得执行__index__/__getitem__。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanReadF64(object list, object index) =>
        list is PyList target && index is long integer && target.TryNumericPosition(integer, out int position)
            && target.TryReadF64(position, out _);

    /// <summary>守卫后的无盒读取；误用仍转Python异常，不泄漏CLR类型或越界异常。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadF64(object list, object index)
    {
        if (list is not PyList target) return ReadError(0);
        if (index is not long integer) return ReadError(1);
        if (!target.TryNumericPosition(integer, out int position)) return ReadError(2);
        if (!target.TryReadF64(position, out double value)) return ReadError(3);
        return value;
    }

    /// <summary>
    /// 编译器可信入口：must分析须证明同一list/index的CanReadF64真边支配，
    /// 且中间没有写入或回调。不是通用Python API；非法IR必须在编译期拒绝。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadCheckedF64(object list, object index) =>
        ((PyList)list).ReadCheckedNumeric((long)index);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object AppendError() => throw new PyTypeError("numeric append requires a list");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double ReadError(int kind) => throw kind switch
    {
        0 => new PyTypeError("numeric read requires a list"),
        1 => new PyTypeError("numeric read requires an exact integer index"),
        2 => new PyIndexError("list index out of range"),
        _ => new PyTypeError("numeric read requires a float element"),
    };
}
