// 新list派生的表达式相等/不等冷分派；不改变普通容器内部相等判断的Truthy合同。
namespace RyuPy.Runtime;

internal static class PyListComparisonDispatch
{
    internal static bool Compare(int op, object left, object right)
    {
        string slot = op == PyOps.Eq ? "__eq__" : "__ne__";
        // 两端真实list布局没有用户比较覆盖，直接执行对应根槽，不将ne改写成eq。
        if (PyListLayout.TryGet(left, out _) && PyListLayout.TryGet(right, out _))
            return RequireBool(PyListProtocols.Compare(slot, left, right));
        // 根槽面对外部对象仅返回NotImplemented；外部对象的同名槽最多调用一次。
        object foreign = left is PyListInstance ? right : left;
        object list = left is PyListInstance ? left : right;
        if (PyObject.TryMethod1(foreign, slot, list, out object? result))
        {
            if (result is not PyNotImplemented) return RequireBool(result);
            // 显式ne已经拒绝时不能再回调eq；最终按对象身份兜底。
        }
        else if (op == PyOps.Ne && PyObject.TryMethod1(foreign, "__eq__", list, out object? equal)
            && equal is not PyNotImplemented)
        {
            // 缺失ne才继承object.__ne__，其职责本来就是对eq结果求真并取反。
            return !PyOps.Truthy(equal);
        }
        bool same = ReferenceEquals(left, right);
        return op == PyOps.Eq ? same : !same;
    }

    private static bool RequireBool(object? result) => result is bool flag ? flag
        : throw PyListLayout.Own(new PyNotImplementedError(
            "list subclass comparison expressions returning non-bool require object comparison ABI"));
}
