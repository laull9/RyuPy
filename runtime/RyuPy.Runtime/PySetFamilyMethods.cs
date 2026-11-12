// 精确 set 的查询、流式更新及集合方法；保存方法与直接/PIC调用共享绑定和语义。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    internal static bool HasSetFamilyMethod(string name) => name is
        "isdisjoint" or "issubset" or "issuperset" or "copy" or "update"
        or "union" or "intersection" or "difference" or "symmetric_difference"
        or "intersection_update" or "difference_update" or "symmetric_difference_update";

    internal static object? CallSetFamilyMethod(PySet self, string name, object?[] args, string[] names)
    {
        if (names.Length != 0) throw new PyTypeError($"set.{name}() takes no keyword arguments");
        bool single = name is "isdisjoint" or "issubset" or "issuperset"
            or "symmetric_difference" or "symmetric_difference_update";
        if (single && args.Length != 1)
            throw new PyTypeError($"set.{name}() takes exactly one argument ({args.Length} given)");
        if (name == "copy" && args.Length != 0)
            throw new PyTypeError($"set.copy() takes no arguments ({args.Length} given)");
        if (name is "isdisjoint" or "issuperset")
        {
            object? other = args[0];
            if (ReferenceEquals(self, other)) return name == "issuperset" || self.Count == 0;
            CheckSetCachedSource(other);
            IPyIterator iterator = PyIter.Create(other);
            while (iterator.MoveNext())
            {
                bool found = self.Contains(iterator.Current());
                if (name == "isdisjoint" ? found : !found) return false;
            }
            return true;
        }
        if (name == "issubset")
        {
            if (ReferenceEquals(self, args[0])) return true;
            // 非集合参数通过交集消费，找到self全部元素便停止，不能先物化整个源。
            return SetIntersection(self, args).Count == self.Count;
        }
        if (name == "update")
        {
            foreach (object? source in args) if (!ReferenceEquals(self, source)) SetExtend(self, source);
            return PyNone.Instance;
        }
        if (name == "difference_update")
        {
            foreach (object? source in args) DifferenceUpdate(self, source);
            return PyNone.Instance;
        }
        if (name == "intersection_update")
        {
            PySet result = SetIntersection(self, args);
            self.Items.Clear();
            self.Items.UnionWith(result.Items);
            return PyNone.Instance;
        }
        if (name is "symmetric_difference" or "symmetric_difference_update")
        {
            PySet result = name == "symmetric_difference" ? MaterializeSet(self) : self;
            if (ReferenceEquals(result, args[0])) result.Clear();
            else
            {
                PySet other = MaterializeSet(args[0]);
                CheckSetCachedSource(other);
                foreach (object item in other.Items)
                    if (!result.Items.Remove(item)) result.Add(item);
            }
            return name == "symmetric_difference" ? result : PyNone.Instance;
        }
        if (name == "intersection") return SetIntersection(self, args);
        PySet copy = MaterializeSet(self);
        foreach (object? source in args)
        {
            if (name == "difference") DifferenceUpdate(copy, source);
            else SetExtend(copy, source);
        }
        return copy;
    }

    private static PySet MaterializeSet(object? source)
    {
        var result = new PySet();
        SetExtend(result, source);
        return result;
    }

    private static void DifferenceUpdate(PySet target, object? source)
    {
        if (ReferenceEquals(target, source)) { target.Clear(); return; }
        CheckSetCachedSource(source);
        IPyIterator iterator = PyIter.Create(source);
        while (iterator.MoveNext()) target.Discard(iterator.Current());
    }

    private static PySet SetIntersection(PySet self, object?[] sources)
    {
        if (sources.Length == 0) return MaterializeSet(self);
        PySet current = self;
        foreach (object? source in sources)
        {
            if (ReferenceEquals(current, source)) { current = MaterializeSet(current); continue; }
            CheckSetCachedSource(source);
            var next = new PySet();
            IPyIterator iterator = PyIter.Create(source);
            while (iterator.MoveNext())
            {
                object? value = iterator.Current();
                if (value is PyInstance or PyTuple or PyFrozenSet or PyBaseException
                    && ContainsUserHash(value, new System.Collections.Generic.HashSet<object>(System.Collections.Generic.ReferenceEqualityComparer.Instance)))
                    throw new PyNotImplementedError("set intersection cached-hash operations of user objects are not supported yet");
                if (current.Contains(value))
                {
                    next.Add(value);
                    if (next.Count == current.Count) break;
                }
            }
            current = next;
        }
        return current;
    }

    // HashSet不暴露缓存hash；精确set源含用户hash时不偷偷重复回调。
    private static void CheckSetCachedSource(object? source)
    {
        System.Collections.Generic.IEnumerable<object>? items = source switch
        {
            PySet set => set.Items, PyFrozenSet frozen => frozen.Items, _ => null,
        };
        if (items is null) return;
        var visited = new System.Collections.Generic.HashSet<object>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        foreach (object value in items)
            if (ContainsUserHash(value, visited))
                throw new PyNotImplementedError("set cached-hash operations of user objects are not supported yet");
    }
}
