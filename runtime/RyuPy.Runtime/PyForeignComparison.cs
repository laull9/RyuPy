// 动态比较表达式保留用户/外部对象的原始结果；只有布尔出口才调用Truthy。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>比较表达式的object ABI，与现有bool条件ABI分别使用。</summary>
    public static object CompareValue(int op, object a, object b)
    {
        // 精确long没有可覆盖协议；比较用原值而非相减，保留两端边界及bool单例。
        if (a is long x && b is long y)
            return BoxBool(op switch
            {
                Eq => x == y, Ne => x != y, Lt => x < y,
                Le => x <= y, Gt => x > y, Ge => x >= y,
                _ => throw new PyRuntimeError($"unknown compare opcode {op}"),
            });
        if (a is IPyForeignObject left) return left.RichCompare(op, b, false);
        if (a is PyInstance { ForeignInstance: { } leftDerived }) return leftDerived.RichCompare(op, b, false);
        if (b is IPyForeignObject right) return right.RichCompare(op, a, true);
        if (b is PyInstance { ForeignInstance: { } rightDerived }) return rightDerived.RichCompare(op, a, true);
        if (PyOrdinaryRichComparison.TryCompare(op, a, b, out object? result)) return result!;
        return BoxBool(Compare(op, a, b));
    }
}
