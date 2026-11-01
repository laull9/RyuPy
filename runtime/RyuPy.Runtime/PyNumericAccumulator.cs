using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>数值累加的内部编译器ABI；不作为Python可见对象暴露。</summary>
public static class PyNumericAccumulator
{
    /// <summary>
    /// 编译器内部累加槽：一次函数调用只分配一次。整数结果延迟装箱，其它值保留原动态对象。
    /// MIR 仅在局部不逃逸、无 cell / yield / 异常区域且没有循环内观察时生成此表示。
    /// </summary>
    private sealed class NumericAccumulator
    {
        internal bool IsInteger;
        internal long Integer;
        internal object? Boxed;

        internal NumericAccumulator(object? initial) => Store(initial);

        // 动态结果必须原样保存：用户 __radd__ / __iadd__ 可能返回已有整数盒或可变对象。
        internal void Store(object? value)
        {
            Boxed = value;
            IsInteger = value is long;
            if (value is long integer) Integer = integer;
        }

        internal object? Value() => IsInteger ? Boxed ??= PyInt.Box(Integer) : Boxed;
    }

    /// <summary>创建内部累加槽，保留初始 Python 值的身份。</summary>
    public static object Create(object? initial) => new NumericAccumulator(initial);

    /// <summary>
    /// 执行完整 += 协议；仅实际 long + long 延迟装箱，溢出或其它类型立即回动态运算。
    /// 调用方须已按源程序顺序求出 rhs；此内部槽不得暴露给 Python 代码。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(object holder, object? rhs)
    {
        var state = (NumericAccumulator)holder;
        if (state.IsInteger && rhs is long right)
        {
            long left = state.Integer;
            long sum = unchecked(left + right);
            // 两个同号操作数得到异号结果才溢出；不以CLR异常作为正常循环的控制流。
            if (((left ^ sum) & (right ^ sum)) >= 0)
            {
                state.Integer = sum;
                state.Boxed = null;
                return;
            }
        }
        // 先完成动态运算再更新状态；失败时旧值保留。inplace标志保留__iadd__/容器原地协议。
        object result = PyOps.Binary(PyOps.Add | PyOps.InPlaceFlag, state.Value()!, rhs!);
        state.Store(result);
    }

    /// <summary>物化观察值；同一数值版本多次观察复用同一个盒，不改变身份。</summary>
    public static object? Value(object holder) => ((NumericAccumulator)holder).Value();
}
