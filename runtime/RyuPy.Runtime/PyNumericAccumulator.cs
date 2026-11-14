using System.Runtime.CompilerServices;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>有界局部累加的内部编译器ABI；不作为Python可见对象暴露。</summary>
public static class PyNumericAccumulator
{
    /// <summary>
    /// 编译器内部累加槽：一次函数调用只分配一次。整数延迟装箱，精确字符串延迟物化，其它值保留原动态对象。
    /// MIR 仅在局部不逃逸、无 cell / yield / 异常区域且没有循环内观察时生成此表示。
    /// </summary>
    private sealed class NumericAccumulator
    {
        internal bool IsInteger;
        internal long Integer;
        internal object? Boxed;
        internal StringBuilder? Text;

        internal NumericAccumulator(object? initial) => Store(initial);

        // 动态结果必须原样保存：用户 __radd__ / __iadd__ 可能返回已有整数盒或可变对象。
        internal void Store(object? value)
        {
            Text = null;
            Boxed = value;
            IsInteger = value is long;
            if (value is long integer) Integer = integer;
        }

        internal object? Value() => IsInteger ? Boxed ??= PyInt.Box(Integer)
            : Text is not null ? Boxed ??= Text.ToString() : Boxed;

        // 仅内部精确字符串进入此路径；快照仍是不可变string，不向Python暴露builder。
        internal void Append(string right)
        {
            if (right.Length == 0) return; // 空追加保留当前版本身份。
            if (Text is null)
            {
                var left = (string)Boxed!;
                if (left.Length == 0) { Store(right); return; }
                // 完成首次分配/复制后才发布新状态；失败不损坏旧值。
                var next = new StringBuilder(left);
                next.Append(right);
                Text = next;
            }
            else
            {
                // 预先完成容量检查与几何增长，Append不再分配，避免失败留下部分追加。
                int required = checked(Text.Length + right.Length);
                if (required > Text.Capacity)
                {
                    int capacity = (int)Math.Min(int.MaxValue,
                        Math.Max((long)required, (long)Text.Capacity * 2));
                    Text.EnsureCapacity(capacity);
                }
                Text.Append(right);
            }
            Boxed = null;
        }
    }

    /// <summary>创建内部累加槽，保留初始 Python 值的身份。</summary>
    public static object Create(object? initial) => new NumericAccumulator(initial);

    /// <summary>
    /// 执行完整 += 协议；实际 long + long 延迟装箱，精确string追加延迟物化；其它类型回动态运算。
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
        if (rhs is string text && (state.Text is not null || state.Boxed is string))
        {
            state.Append(text);
            return;
        }
        // 先完成动态运算再更新状态；失败时旧值保留。inplace标志保留__iadd__/容器原地协议。
        object result = PyOps.Binary(PyOps.Add | PyOps.InPlaceFlag, state.Value()!, rhs!);
        state.Store(result);
    }

    /// <summary>物化观察值；同一版本多次观察复用同一个不可变值，不改变身份。</summary>
    public static object? Value(object holder) => ((NumericAccumulator)holder).Value();
}
