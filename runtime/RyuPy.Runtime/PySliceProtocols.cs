// slice 的有限值比较与三字段哈希；不构造临时 tuple，不缓存用户 hash，也不真值化未知排序结果。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

public sealed partial class PySlice
{
    // 共享比较ABI只能返回bool。混合用户比较也不能经旧反射槽把任意对象静默真值化。
    internal static void GuardMixedComparison(object? left, object? right)
    {
        object? other = left is PySlice && right is not PySlice ? right
            : right is PySlice && left is not PySlice ? left : null;
        PyClass? cls = other is PyInstance instance ? instance.Class
            : other is PyBaseException exception ? exception.PythonClass : null;
        if (other is PyNativeObject || cls is not null
            && (cls.TryLookup("__eq__", out _, out _) || cls.TryLookup("__ne__", out _, out _)
                || cls.TryLookup("__lt__", out _, out _) || cls.TryLookup("__le__", out _, out _)
                || cls.TryLookup("__gt__", out _, out _) || cls.TryLookup("__ge__", out _, out _)))
            throw PublicError(new PyNotImplementedError("slice mixed user comparisons require object-valued comparison support"));
    }

    internal bool CompareFields(int op, PySlice other)
    {
        try
        {
            for (int index = 0; index < 3; index++)
            {
                object? left = Component(index), right = other.Component(index);
                // 同一字段不调用用户 eq，包括同一 NaN 与未开放的对象比较域。
                if (ReferenceEquals(left, right)) continue;
                if (!ComparisonField(left) || !ComparisonField(right))
                    throw PublicError(new PyNotImplementedError("slice comparisons with non-identical user or container fields are not supported"));
                if (PyHash.Equal(left, right)) continue;
                if (op == PyOps.Eq) return false;
                if (op == PyOps.Ne) return true;
                return PyOps.Compare(op, left ?? PyNone.Instance, right ?? PyNone.Instance);
            }
            return op is PyOps.Eq or PyOps.Le or PyOps.Ge;
        }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
    }

    private object? Component(int index) => index switch { 0 => Start, 1 => Stop, _ => Step };

    // 仅进入当前访问字段时守卫，不检查已短路的后续字段。任意对象返回比较需要另开 MIR ABI。
    private static bool ComparisonField(object? value) => value is null or PyNone or bool or int or long
        or BigInteger or double or string or PyBytes or PyByteArray or PySlice;

    internal long ContentHash()
    {
        try
        {
            // 与tuple用不同域种子；沿项目既有哈希合同，只承诺等值哈希相同，不承诺CP数值。
            long hash = 0x87654321L;
            hash = unchecked(hash * 1000003 ^ HashComponent(Start));
            hash = unchecked(hash * 1000003 ^ HashComponent(Stop));
            hash = unchecked(hash * 1000003 ^ HashComponent(Step));
            return hash == -1 ? -2 : hash;
        }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
    }

    private static long HashComponent(object? value)
    {
        // 不递归预筛整棵字段树而抹掉前项副作用；未知容器/native hash域在实际访问时明确拒绝。
        if (value is PyTuple or PyFrozenSet or PyNativeObject)
            throw PublicError(new PyNotImplementedError("slice nested container or native hash fields are not supported"));
        GuardProtocol(value, "__hash__");
        return PyHash.Of(value);
    }
}
