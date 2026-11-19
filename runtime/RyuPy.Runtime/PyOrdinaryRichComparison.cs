// 普通对象富比较的唯一原始值协议；operator 保留结果，旧 bool ABI 只在出口求真值。
namespace RyuPy.Runtime;

internal static class PyOrdinaryRichComparison
{
    internal static bool TryCompare(int op, object left, object right, out object? value)
    {
        value = null;
        bool a = Ordinary(left), b = Ordinary(right);
        // 特殊布局仍由既有专用分派负责，不把 native/容器派生类当普通类。
        if ((!a && !b) || !Eligible(left) || !Eligible(right)) return false;
        string forward = op switch { PyOps.Eq => "__eq__", PyOps.Ne => "__ne__", PyOps.Lt => "__lt__",
            PyOps.Le => "__le__", PyOps.Gt => "__gt__", PyOps.Ge => "__ge__", _ => "" };
        if (forward.Length == 0) return false;
        string reflected = op switch { PyOps.Lt => "__gt__", PyOps.Le => "__ge__",
            PyOps.Gt => "__lt__", PyOps.Ge => "__le__", _ => forward };
        bool rightFirst = a && b && !ReferenceEquals(((PyInstance)left).Class, ((PyInstance)right).Class)
            && ((PyInstance)right).Class.IsSubclassOf(((PyInstance)left).Class);
        if (rightFirst && Invoke(right, reflected, left, out value)) return true;
        if (Invoke(left, forward, right, out value)) return true;
        if (!rightFirst && Invoke(right, reflected, left, out value)) return true;
        if (op is PyOps.Eq or PyOps.Ne)
        {
            value = PyOps.BoxBool(op == PyOps.Eq ? ReferenceEquals(left, right) : !ReferenceEquals(left, right));
            return true;
        }
        string symbol = op switch { PyOps.Lt => "<", PyOps.Le => "<=", PyOps.Gt => ">", _ => ">=" };
        throw new PyTypeError($"'{symbol}' not supported between instances of '{PyRepr.TypeName(left)}' and '{PyRepr.TypeName(right)}'");
    }

    private static bool Ordinary(object value) => value is PyInstance && value.GetType() == typeof(PyInstance)
        && ((PyInstance)value).Class.HasOrdinaryLexicalLayout;
    // slice 与 dict 视图有明确的混合比较保护/表达式 ABI 拒绝；不能先执行用户回调绕过它们。
    private static bool Eligible(object value) => (value is not PyInstance && value is not PyNativeObject
        && value is not PyBaseException && value is not PySlice && value is not PyDictView) || Ordinary(value);
    private static bool Invoke(object self, string name, object other, out object? value)
    {
        if (PyObject.TryMethod1(self, name, other, out value)) return value is not PyNotImplemented;
        // object.__ne__ 的默认委托仅在 __ne__ 缺席时存在，显式 NI 不应偷偷回退同侧 eq。
        if (name == "__ne__" && PyObject.TryMethod1(self, "__eq__", other, out value))
        {
            if (value is PyNotImplemented) return false;
            value = PyOps.BoxBool(!PyOps.Truthy(value));
            return true;
        }
        return false;
    }
}
