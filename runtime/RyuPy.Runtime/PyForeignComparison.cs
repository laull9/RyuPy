// 动态比较表达式保留用户/外部对象的原始结果；只有布尔出口才调用Truthy。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>比较表达式的object ABI，与现有bool条件ABI分别使用。</summary>
    public static object CompareValue(int op, object a, object b)
    {
        if (a is IPyForeignObject left) return left.RichCompare(op, b, false);
        if (b is IPyForeignObject right) return right.RichCompare(op, a, true);
        if (PyOrdinaryRichComparison.TryCompare(op, a, b, out object? result)) return result!;
        return BoxBool(Compare(op, a, b));
    }
}
