// 新dict视图的局部相等分派；表达式受bool ABI约束，容器内部仍按RichCompareBool求真。
namespace RyuPy.Runtime;

internal static class PyDictViewComparisonDispatch
{
    internal static bool Compare(int op, object? left, object? right, bool expression)
    {
        bool leftIsView = left is PyDictView { UseLayoutProtocol: true };
        var view = (PyDictView)(leftIsView ? left : right)!;
        object? other = leftIsView ? right : left;
        // 两个内建视图无用户比较覆盖；values仅在视图间采用身份，其余外部类型仍有反射机会。
        object root = PyDictViewProtocols.Compare(op, view, other);
        if (root is not PyNotImplemented) return Result(root, expression);
        string slot = op == PyOps.Eq ? "__eq__" : "__ne__";
        if (PyObject.TryMethod1(other, slot, view, out object? result))
        {
            if (result is not PyNotImplemented) return Result(result, expression);
            // 显式ne返回NotImplemented时不再执行eq，否则会多调用一次错误的槽。
        }
        else if (op == PyOps.Ne && PyObject.TryMethod1(other, "__eq__", view, out object? equal)
            && equal is not PyNotImplemented)
        {
            // 只有缺ne时才继承object.__ne__的Truthy取反合同。
            return !PyOps.Truthy(equal);
        }
        bool same = ReferenceEquals(left, right);
        return op == PyOps.Eq ? same : !same;
    }

    private static bool Result(object? result, bool expression)
    {
        if (!expression) return PyOps.Truthy(result);
        if (result is bool flag) return flag;
        throw PyDictLayout.Own(new PyNotImplementedError(
            "dict view comparison expressions returning non-bool require object comparison ABI"));
    }
}
