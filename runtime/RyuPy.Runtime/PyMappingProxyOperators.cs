using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// mappingproxy 专有运算：union 每侧只解一层，显式比较保原结果；不改变全局 bool 比较 ABI。
/// 新增精确 dict union 限无回调标量键，用户映射仍执行真实槽，复杂派生协议明确拒绝。
/// </summary>
internal static class PyMappingProxyOperators
{
    internal static object Union(object? left, object? right)
    {
        if (left is PyMappingProxy a) left = a.Mapping;
        if (right is PyMappingProxy b) right = b.Mapping;
        GuardDerived(left, right);
        // 每侧只有轮到时才读取、守门和调用；左回调可以动态替换右侧槽。
        if (TrySlot(left, "__or__", right, out object? forward) && forward is not PyNotImplemented)
            return forward!;
        if (TrySlot(right, "__ror__", left, out object? reflected) && reflected is not PyNotImplemented)
            return reflected!;
        if (left is IPyNativeMapping || right is IPyNativeMapping)
            throw PyMappingProxy.Unsupported("native dictionary union fallback");
        // 共用内建值叶子，绝不回到Binary重放已返回NI的用户槽。
        return PyOps.BinaryOrValues(left!, right!, inplace: false);
    }

    internal static object InPlace() => throw PyMappingProxy.Error(new PyTypeError(
        "'|=' is not supported by mappingproxy; use '|' instead"));

    /// <summary>仅新普通 union 叶子调用；预检不hash、不eq，不影响已有dict操作成功域。</summary>
    internal static PyDict DictUnion(PyDict left, PyDict right)
    {
        CheckKeys(left); CheckKeys(right);
        var result = new PyDict();
        foreach (var (key, value) in left.Entries()) result.Set(key, value);
        foreach (var (key, value) in right.Entries()) result.Set(key, value);
        return result;
    }
    private static void CheckKeys(PyDict dictionary)
    {
        foreach (var (key, _) in dictionary.Entries())
            if (!Primitive(key)) throw PyMappingProxy.Unsupported("dictionary union with custom keys");
    }
    private static bool Primitive(object? value) =>
        value is null or PyNone or string or long or int or bool or BigInteger or double;

    /// <summary>每frame左右候选各一次；proxy候选只递减自身层，不提前剥用户可见的右对象。</summary>
    internal static object? Compare(object? left, object? right, bool unequal)
    {
        GuardDerived(left, right);
        object? first = Candidate(left, right, unequal);
        if (first is not PyNotImplemented) return first;
        object? second = Candidate(right, left, unequal);
        if (second is not PyNotImplemented) return second;
        bool same = ReferenceEquals(left, right);
        return unequal ? !same : same;
    }

    private static object? Candidate(object? receiver, object? other, bool unequal)
    {
        if (receiver is PyMappingProxy proxy) return Compare(proxy.Mapping, other, unequal);
        if (receiver is PyDict dictionary)
        {
            if (other is not PyDict target) return PyNotImplemented.Instance;
            bool equal = PyHash.Equal(dictionary, target);
            return unequal ? !equal : equal;
        }
        if (receiver is PyStringInstance)
            throw PyMappingProxy.Unsupported("string subclass comparison");
        string name = unequal ? "__ne__" : "__eq__";
        if (TrySlot(receiver, name, other, out object? result)) return result;
        // 只有缺失ne才使用object.ne默认规则；显式ne=NI由上面原样送回反射调度。
        if (unequal && TrySlot(receiver, "__eq__", other, out result))
            return result is PyNotImplemented ? result : !PyOps.Truthy(result);
        if (receiver is IPyNativeMapping)
            throw PyMappingProxy.Unsupported("native dictionary comparison fallback");
        // 简单值只比较真正兼容的终端域；用户对象必须得到反射机会，不借Equal重放槽。
        bool compatible = (receiver is null or PyNone) && (other is null or PyNone)
            || receiver is string && other is string
            || PyHash.IsNumber(receiver!) && PyHash.IsNumber(other!);
        if (!compatible) return PyNotImplemented.Instance;
        bool simple = PyHash.Equal(receiver, other);
        return unequal ? !simple : simple;
    }

    private static bool TrySlot(object? receiver, string name, object? other, out object? result)
    {
        GuardSlot(receiver, name);
        return PyObject.TryMethod1(receiver, name, other, out result);
    }
    private static void GuardSlot(object? receiver, string name)
    {
        PyClass? cls = ReceiverClass(receiver);
        if (cls is null) return;
        if (receiver is PyInstance instance && (instance.Class.BuiltinBase is not null || instance is PyStringInstance))
            throw PyMappingProxy.Unsupported("builtin-derived operator " + name);
        if (!cls.TryLookup(name, out _, out object? raw)) return;
        // 与B1有限普通槽域一致；异常实例同样可能有Python类。None交真实调用报错。
        if (raw is PyProperty || raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
            throw PyMappingProxy.Unsupported("descriptor for " + name);
    }
    private static PyClass? ReceiverClass(object? value) => value switch
    {
        PyInstance instance => instance.Class,
        PyBaseException error => error.PythonClass,
        _ => null,
    };
    private static void GuardDerived(object? left, object? right)
    {
        PyClass? a = ReceiverClass(left), b = ReceiverClass(right);
        if (a is not null && b is not null && !ReferenceEquals(a, b)
            && (a.IsSubclassOf(b) || b.IsSubclassOf(a)))
            throw PyMappingProxy.Unsupported("strict-subtype reflected operator priority");
    }
}
