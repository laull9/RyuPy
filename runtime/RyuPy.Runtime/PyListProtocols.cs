// list 真实布局的默认根协议：公开 owner 不变，结果新容器精确降型，回调期间逐项重读长度。
using System;
using System.Numerics;
namespace RyuPy.Runtime;

internal static class PyListProtocols
{
    internal static long Len(object self) => PyListLayout.RequireRaw(self).Count;
    internal static bool Truthy(object self) => Len(self) != 0;
    internal static IPyIterator Iter(object self) => PyIter.Create(PyListLayout.RequireRaw(self));
    internal static IPyIterator Reversed(object self) => new ReverseIterator(PyListLayout.RequireRaw(self));
    // 尾索引只在创建时捕获；随后使用实时 payload，首次越界或耗尽即永久停止。
    private sealed class ReverseIterator(PyList list) : IPyIterator
    {
        private int _index = list.Count - 1;
        private object? _current;
        public bool MoveNext()
        {
            if (_index < 0 || _index >= list.Count) { _index = -1; _current = null; return false; }
            _current = list.GetItem(_index--);
            return true;
        }
        public object? Current() => _current;
    }
    internal static bool Contains(object self, object? value)
    {
        PyList list = PyListLayout.RequireRaw(self);
        for (int i = 0; i < list.Count; i++)
            if (PyListRootMethods.Equal(list.GetItem(i), value)) return true;
        return false;
    }
    internal static object? Item(object self, object? key)
    {
        PyList list = PyListLayout.RequireRaw(self);
        if (key is PySlice slice)
        {
            // 现有 slice 只接受原生整数，不调用用户槽；保留此已登记支持边界。
            ValidateSlice(slice);
            try { return list.GetSlice(slice); }
            catch (PyBaseException error) { throw PyListLayout.Own(error); }
        }
        long index = Index(key);
        try { return list.GetItem(index); }
        catch (PyBaseException error) { throw PyListLayout.Own(error); }
    }
    internal static void SetItem(object self, object? key, object? value)
    {
        PyList list = PyListLayout.RequireRaw(self);
        if (key is PySlice slice)
        {
            // 先检验/归一化切片，再消费源；step=0 不得触发 RHS 的 iter。
            ValidateSlice(slice);
            (long start, long stop, long step, long count) bounds;
            try { bounds = slice.Indices(list.Count); }
            catch (PyBaseException error) { throw PyListLayout.Own(error); }
            object?[] replacement = SliceSource(value);
            if (bounds.step == 1)
            {
                int start = (int)Math.Min(bounds.start, list.Count);
                int stop = (int)Math.Min(Math.Max(bounds.stop, start), list.Count);
                list.Items.RemoveRange(start, stop - start);
                list.Items.InsertRange(start, replacement);
                return;
            }
            if (replacement.LongLength != bounds.count)
                throw PyListLayout.Own(new PyValueError($"attempt to assign sequence of size {replacement.Length} to extended slice of size {bounds.count}"));
            // RHS 回调缩短表后的扩展切片重新定位尚不支持；明确失败，不发生 CLR 越界或部分写入。
            long last = bounds.start + (bounds.count - 1) * bounds.step;
            if (bounds.count != 0 && (bounds.start >= list.Count || last >= list.Count))
                throw PyListLayout.Own(new PyNotImplementedError("list extended slice assignment after source shrinks target is not supported"));
            long position = bounds.start;
            foreach (object? item in replacement) { list.SetItem(position, item); position += bounds.step; }
            return;
        }
        long index = Index(key);
        if (index < 0) index += list.Count;
        if (index < 0 || index >= list.Count) throw PyListLayout.Own(new PyIndexError("list assignment index out of range"));
        list.SetItem(index, value);
    }
    internal static void DeleteItem(object self, object? key)
    {
        PyList list = PyListLayout.RequireRaw(self);
        if (key is PySlice slice)
        {
            ValidateSlice(slice);
            try { list.DelSlice(slice); }
            catch (PyBaseException error) { throw PyListLayout.Own(error); }
            return;
        }
        long index = Index(key);
        if (index < 0) index += list.Count;
        if (index < 0 || index >= list.Count) throw PyListLayout.Own(new PyIndexError("list assignment index out of range"));
        list.DelItem(index);
    }
    private static object?[] SliceSource(object? source)
    {
        if (PyListLayout.TryGet(source, out PyList list)) return [.. list.Items];
        if (source is PyTuple tuple) return tuple.Items;
        if (source is null or PyNone or bool or int or long or BigInteger or double or PyBaseObject)
            throw PyListLayout.Own(new PyTypeError("must assign iterable to extended slice"));
        if (source is PyInstance user && !user.Class.TryLookup("__iter__", out _, out _)
            && source is not PyStringInstance)
        {
            if (user.Class.TryLookup("__getitem__", out _, out _))
                throw PyListLayout.Own(new PyNotImplementedError("list slice assignment from indexed-only sources is not supported"));
            throw PyListLayout.Own(new PyTypeError("must assign iterable to extended slice"));
        }
        // CP slice先取源游标，再让游标经历一次iter及长度提示；不向源查询len。
        IPyIterator cursor = PyIter.Create(source);
        var temporary = new PyList();
        PyOps.ExtendList(temporary, cursor);
        return [.. temporary.Items];
    }
    private static void ValidateSlice(PySlice slice)
    {
        // 只延续已有原生整数 slice 域；合法但未实现的用户 index/宽整数明确 NI。
        // 顺序保持 step/start/stop，零步长必须优先于后续分量与源迭代。
        ValidateSlicePart(slice.Step);
        if (slice.Step is long zero && zero == 0 || slice.Step is int smallZero && smallZero == 0 || slice.Step is false)
            throw PyListLayout.Own(new PyValueError("slice step cannot be zero"));
        if (slice.Step is long step && (step < -int.MaxValue || step > int.MaxValue))
            throw PyListLayout.Own(new PyNotImplementedError("list slices with wide step are not supported"));
        ValidateSlicePart(slice.Start);
        ValidateSlicePart(slice.Stop);
    }
    private static void ValidateSlicePart(object? value)
    {
        if (value is null or PyNone or bool or int or long) return;
        if (value is BigInteger || value is PyInstance user && user.Class.TryLookup("__index__", out _, out _))
            throw PyListLayout.Own(new PyNotImplementedError("list slices with wide or user-defined indices are not supported"));
        throw PyListLayout.Own(new PyTypeError("slice indices must be integers or None or have an __index__ method"));
    }
    private static long Index(object? key)
    {
        object? integer = key;
        if (!PyInt.IsInt(integer))
        {
            if (!PyObject.TryMethod(key, "__index__", [], out integer))
                throw PyListLayout.Own(new PyTypeError($"list indices must be integers or slices, not {PyRepr.TypeName(key)}"));
            if (integer is bool) throw PyListLayout.Own(new PyNotImplementedError("list indexing: __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer)) throw PyListLayout.Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        BigInteger wide = PyInt.ToBigInteger(integer!);
        if (wide < long.MinValue || wide > long.MaxValue)
            throw PyListLayout.Own(new PyIndexError($"cannot fit '{PyRepr.TypeName(key)}' into an index-sized integer"));
        return (long)wide;
    }
    internal static object Concat(object self, object? other)
    {
        PyList left = PyListLayout.RequireRaw(self);
        if (!PyListLayout.TryGet(other, out PyList right))
            throw PyListLayout.Own(new PyTypeError($"can only concatenate list (not \"{PyRepr.TypeName(other)}\") to list"));
        PyList result = left.Copy();
        result.ExtendExact(right);
        return result;
    }
    internal static object Repeat(object self, object? count) => RepeatCore(self, count, false);
    private static object RepeatCore(object self, object? count, bool root)
    {
        PyList list = PyListLayout.RequireRaw(self);
        long times = RepeatCount(count, root);
        CheckRepeat(list, times);
        var result = new PyList();
        if (list.Count == 0) return result;
        for (long i = 0; i < times; i++) result.ExtendExact(list);
        return result;
    }
    internal static object InPlaceAdd(object self, object? other)
    {
        PyOps.ExtendList(PyListLayout.RequireRaw(self), other);
        return self;
    }
    internal static object InPlaceMultiply(object self, object? count) => MultiplyCore(self, count, false);
    private static object MultiplyCore(object self, object? count, bool root)
    {
        PyList list = PyListLayout.RequireRaw(self);
        long times = RepeatCount(count, root);
        CheckRepeat(list, times);
        if (times <= 0) { list.Clear(); return self; }
        int length = list.Count;
        if (length == 0 || times == 1) return self;
        // 固定原长；不复制 attrs、不开新 wrapper，也不在重复过程中追踪增长后的尾部。
        for (long i = 1; i < times; i++)
            for (int j = 0; j < length; j++) list.Append(list.GetItem(j));
        return self;
    }
    private static long RepeatCount(object? count, bool root)
    {
        object? integer = count;
        if (!PyInt.IsInt(integer))
        {
            if (!PyObject.TryMethod(count, "__index__", [], out integer))
                throw PyListLayout.Own(new PyTypeError(root ? $"'{PyRepr.TypeName(count)}' object cannot be interpreted as an integer" : $"can't multiply sequence by non-int of type '{PyRepr.TypeName(count)}'"));
            if (integer is bool) throw PyListLayout.Own(new PyNotImplementedError("list repetition: __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer)) throw PyListLayout.Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        BigInteger wide = PyInt.ToBigInteger(integer!);
        if (wide < long.MinValue || wide > long.MaxValue)
            throw PyListLayout.Own(new PyOverflowError($"cannot fit '{PyRepr.TypeName(count)}' into an index-sized integer"));
        return (long)wide;
    }
    private static void CheckRepeat(PyList list, long times)
    {
        // 沿既有运行时的容量上限与 OverflowError 边界，不假称支持任意宽分配。
        if (list.Count != 0 && times > Array.MaxLength / list.Count)
            throw PyListLayout.Own(new PyOverflowError("repeated sequence is too long"));
    }
    internal static object Compare(string name, object self, object? other)
    {
        PyList left = PyListLayout.RequireRaw(self);
        if (!PyListLayout.TryGet(other, out PyList right)) return PyNotImplemented.Instance;
        if (name is "__eq__" or "__ne__" && left.Count != right.Count) return name == "__ne__";
        int i = 0;
        for (; i < left.Count && i < right.Count; i++)
            if (!PyListRootMethods.Equal(left.GetItem(i), right.GetItem(i))) break;
        // 等值回调可以改变双方长度；不对整个容器做快照，也不使用旧长度。
        if (i >= left.Count || i >= right.Count) return LengthCompare(name, left.Count, right.Count);
        if (name == "__eq__") return false;
        if (name == "__ne__") return true;
        int op = name switch { "__lt__" => PyOps.Lt, "__le__" => PyOps.Le, "__gt__" => PyOps.Gt, _ => PyOps.Ge };
        return ElementOrder(name, op, left.GetItem(i), right.GetItem(i));
    }
    private static object ElementOrder(string name, int op, object? left, object? right)
    {
        string reflected = name switch { "__lt__" => "__gt__", "__le__" => "__ge__", "__gt__" => "__lt__", _ => "__le__" };
        bool rightFirst = left is PyInstance a && right is PyInstance b
            && !ReferenceEquals(a.Class, b.Class) && b.Class.IsSubclassOf(a.Class);
        bool tried = false;
        if (rightFirst && TryElementSlot(right, reflected, left, ref tried, out object? result)) return result!;
        if (TryElementSlot(left, name, right, ref tried, out result)) return result!;
        if (!rightFirst && TryElementSlot(right, reflected, left, ref tried, out result)) return result!;
        if (!tried) return PyOps.Compare(op, left ?? PyNone.Instance, right ?? PyNone.Instance);
        // 槽已经返回 NI，不交回通用比较重跑副作用；此时只允许报告不可比较。
        string symbol = name switch { "__lt__" => "<", "__le__" => "<=", "__gt__" => ">", _ => ">=" };
        throw PyListLayout.Own(new PyTypeError($"'{symbol}' not supported between instances of '{PyRepr.TypeName(left)}' and '{PyRepr.TypeName(right)}'"));
    }
    private static bool TryElementSlot(object? self, string name, object? other, ref bool tried, out object? result)
    {
        if (!PyObject.TryMethod1(self, name, other, out result)) return false;
        tried = true;
        if (result is PyNotImplemented) return false;
        // 全局表达式比较目前是 bool ABI；冷拒非bool，不偷调返回对象的 __bool__。
        if (result is not bool)
            throw PyListLayout.Own(new PyNotImplementedError("list ordering with non-bool element comparison result is not supported"));
        return true;
    }
    private static bool LengthCompare(string name, int left, int right) => name switch
    {
        "__eq__" => left == right, "__ne__" => left != right, "__lt__" => left < right,
        "__le__" => left <= right, "__gt__" => left > right, "__ge__" => left >= right,
        _ => throw new PyRuntimeError("unknown list comparison slot"),
    };
    internal static object? Invoke(string name, object self, object?[] args)
    {
        switch (name)
        {
            case "__str__": return PyRepr.ReprObject(self);
            case "__repr__": return PyRepr.ListRaw(self, PyListLayout.RequireRaw(self));
            case "__len__": return Len(self);
            case "__iter__": return Iter(self);
            case "__reversed__": return Reversed(self);
            case "__getitem__": return Item(self, args[0]);
            case "__setitem__": SetItem(self, args[0], args[1]); return PyNone.Instance;
            case "__delitem__": DeleteItem(self, args[0]); return PyNone.Instance;
            case "__contains__": return Contains(self, args[0]);
            case "__add__": return Concat(self, args[0]);
            case "__mul__": case "__rmul__": return RepeatCore(self, args[0], true);
            case "__iadd__": return InPlaceAdd(self, args[0]);
            case "__imul__": return MultiplyCore(self, args[0], true);
            default: return Compare(name, self, args[0]);
        }
    }
}
