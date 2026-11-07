// OD 内容比较始终先沿 raw dict 插入序，再比较公开链；eq/ne 同名反射独立，不以!eq冒充ne。
using System;
namespace RyuPy.Runtime;

internal static class PyOrderedDictComparison
{
    [ThreadStatic] private static int _depth;
    internal static object Root(string slot, PyOrderedDict self, object? other)
    {
        if (other is IPyNativeMapping) throw PyOrderedDict.Unsupported("comparison with other native mappings");
        PyDict? right = other is PyOrderedDict ordered ? ordered.Raw
            : PyDictLayout.TryGet(other, out PyDict raw) ? raw : null;
        if (right is null) return PyNotImplemented.Instance;
        if (ReferenceEquals(self, other)) return slot == "__eq__";
        if (self.Count != right.Count) return slot == "__ne__";
        if (_depth >= 256) throw PyOrderedDict.Error(new PyRecursionError("maximum recursion depth exceeded in comparison"));
        _depth++;
        try
        {
            PyDictProtocols.RequireScalarContents(right);
            long lv = self.OrderVersion, rv = right.Version, ro = (other as PyOrderedDict)?.OrderVersion ?? 0;
            // 即使两个OD公开顺序不同，也先逐值比较。plain==OD按子类型优先使用OD为self。
            foreach (var (key, value) in self.Raw.Entries())
            {
                if (!right.TryGet(key, out object? found)) return slot == "__ne__";
                bool equal = PyListRootMethods.Equal(value, found);
                if (self.OrderVersion != lv || right.Version != rv || other is PyOrderedDict changed && changed.OrderVersion != ro)
                    throw PyOrderedDict.Unsupported("comparison with structural mutation");
                if (!equal) return slot == "__ne__";
            }
            if (other is PyOrderedDict second)
            {
                PyOrderedDict.Node? leftNode = self.First, rightNode = second.First;
                while (leftNode is not null && rightNode is not null)
                {
                    if (!ReferenceEquals(leftNode.Key, rightNode.Key) && !PyHash.Equal(leftNode.Key, rightNode.Key))
                        return slot == "__ne__";
                    leftNode = leftNode.Next; rightNode = rightNode.Next;
                }
            }
            return slot == "__eq__";
        }
        finally { _depth--; }
    }
    internal static bool Expression(int op, object? left, object? right, bool expression = true)
    {
        string slot = op == PyOps.Eq ? "__eq__" : "__ne__";
        // 无继承优先级的兄弟 dict 子类在左时，仍先执行它继承的 dict 根槽。
        if (left is PyDictInstance && right is PyOrderedDict)
            return Result(PyDictProtocols.Compare(slot, left, right), expression);
        bool leftOwn = left is PyOrderedDict or PyOrderedDictView;
        object own = (leftOwn ? left : right)!;
        object? other = leftOwn ? right : left;
        object root = own is PyOrderedDict od ? Root(slot, od, other) : ((PyOrderedDictView)own).Compare(op, other);
        if (root is not PyNotImplemented) return Result(root, expression);
        if (PyObject.TryMethod1(other, slot, own, out object? result))
        {
            if (result is not PyNotImplemented) return Result(result, expression);
        }
        else if (op == PyOps.Ne && PyObject.TryMethod1(other, "__eq__", own, out object? equal)
            && equal is not PyNotImplemented) return !PyOps.Truthy(equal);
        bool same = ReferenceEquals(left, right);
        return op == PyOps.Eq ? same : !same;
    }
    private static bool Result(object? value, bool expression) => !expression ? PyOps.Truthy(value)
        : value is bool flag ? flag : throw PyOrderedDict.Unsupported("comparison expressions returning non-bool (object comparison ABI)");
}
