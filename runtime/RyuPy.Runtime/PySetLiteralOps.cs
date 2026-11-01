using System.Collections.Generic;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>按字面量求值顺序向集合插入一个元素。</summary>
    /// <param name="target">正在构造的集合。</param>
    /// <param name="value">已经求值的元素。</param>
    public static void SetAdd(object target, object? value) => LiteralSet(target).Add(value);

    /// <summary>流式展开星号元素，每取出一个值立即哈希并插入。</summary>
    /// <param name="target">正在构造的集合。</param>
    /// <param name="iterable">星号展开源。</param>
    public static void SetExtend(object target, object? iterable)
    {
        PySet set = LiteralSet(target);
        // CPython 从集合复制时复用源中缓存的哈希。现有 HashSet 不公开缓存值，
        // 对用户对象重新哈希会引入副作用甚至失败，必须在任何目标写入前明确拒绝。
        IEnumerable<object>? source = iterable switch
        {
            PySet values => values.Items,
            PyFrozenSet values => values.Items,
            _ => null,
        };
        if (source is not null)
        {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (object value in source)
                if (ContainsUserHash(value, visited))
                    throw new PyNotImplementedError("set cached-hash copying of user objects is not supported yet");
        }
        IPyIterator iterator = PyIter.Create(iterable);
        // 不先物化列表：哈希可能执行用户代码或抛异常，必须先于下一次迭代。
        while (iterator.MoveNext()) set.Add(iterator.Current());
    }

    private static bool ContainsUserHash(object? value, HashSet<object> visited)
    {
        if (value is PyInstance || value is PyBaseException { PythonClass: not null }) return true;
        if (value is not (PyTuple or PyFrozenSet) || !visited.Add(value)) return false;
        IEnumerable<object?> items = value is PyTuple tuple ? tuple.Items : ((PyFrozenSet)value).Items;
        foreach (object? item in items)
            if (ContainsUserHash(item, visited)) return true;
        return false;
    }

    private static PySet LiteralSet(object target) => target as PySet
        ?? throw new PyTypeError($"set literal target must be set, not {PyRepr.TypeName(target)}");
}
