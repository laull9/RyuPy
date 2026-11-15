// 精确tuple的相等比较直接访问数组；元素身份、Python比较回调及短路顺序均保持原合同。
namespace RyuPy.Runtime;

public static partial class PyHash
{
    private static bool ExactTupleEqual(object?[] left, object?[] right)
    {
        // tuple先比较共同前缀，再比较长度；长度不同也不能跳过元素回调。
        int common = System.Math.Min(left.Length, right.Length);
        for (int index = 0; index < common; index++)
        {
            object? a = left[index], b = right[index];
            if (!ReferenceEquals(a, b) && !Equal(a, b)) return false;
        }
        return left.Length == right.Length;
    }
}
