// 星号解包独立入口：先读前缀，再取一次iterator并求首iterator的hint，耗尽后冻结全部目标值。
// 不改变非星号/全局迭代协议；精确容器不物化输入数值背板，用户回调在本地边界恢复异常状态。
using System;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>把平坦星号解包准备为前缀、新list、后缀组成的精确tuple。</summary>
    public static object UnpackEx(object? source, long before, long after)
    {
        // 编译器计数不窄化溢出；验证在分配前完成，实际迭代输入不设私自截断上限。
        if (before < 0 || after < 0 || before >= Array.MaxLength
            || after > Array.MaxLength - 1L - before)
            throw UnpackError("invalid unpack target count");
        int prefix = (int)before, suffix = (int)after;
        int fixedCount = prefix + suffix;
        var values = new object?[fixedCount + 1];
        if (source is PyTuple tuple)
        {
            UnpackExMinimum(tuple.Length, fixedCount);
            var middle = new PyList();
            for (int index = 0; index < prefix; index++) values[index] = tuple.Items[index];
            for (int index = prefix; index < tuple.Length - suffix; index++) middle.Append(tuple.Items[index]);
            values[prefix] = middle;
            for (int index = 0; index < suffix; index++) values[prefix + 1 + index] = tuple.Items[tuple.Length - suffix + index];
            return new PyTuple(values);
        }
        if (source is PyList list)
        {
            int count = list.Count;
            UnpackExMinimum(count, fixedCount);
            var middle = new PyList();
            // 逐项GetItem缓存原盒身份；访问源Items会永久退化数值背板，不能拿来省几个调用。
            for (int index = 0; index < prefix; index++) values[index] = list.GetItem(index);
            for (int index = prefix; index < count - suffix; index++) middle.Append(list.GetItem(index));
            values[prefix] = middle;
            for (int index = 0; index < suffix; index++) values[prefix + 1 + index] = list.GetItem(count - suffix + index);
            return new PyTuple(values);
        }

        IPyIterator first = UnpackExIterator(source, initial: true);
        for (int index = 0; index < prefix; index++)
        {
            if (!UnpackExNext(first, out values[index]))
                throw UnpackError($"not enough values to unpack (expected at least {fixedCount}, got {index})");
        }
        // I1可以不同于I0；hint始终属于I0，并发生在再次iter之后、尾部next之前。
        IPyIterator remaining = UnpackExIterator(first, initial: false);
        long hint = UnpackExLengthHint(first);
        var rest = new PyList();
        // 仅预留容量有界：全部hint协议/错误保留，实际内容仍可任意增长，不捕获OOM。
        // 与CP巨大hint资源失败时点的差异由本切ADR登记，不是元素数量上限。
        rest.Items.EnsureCapacity((int)Math.Min(hint, 4096));
        while (UnpackExNext(remaining, out object? value)) rest.Append(value);
        if (rest.Count < suffix)
            throw UnpackError($"not enough values to unpack (expected at least {fixedCount}, got {(long)prefix + rest.Count})");
        int middleCount = rest.Count - suffix;
        for (int index = 0; index < suffix; index++) values[prefix + 1 + index] = rest.GetItem(middleCount + index);
        // 先固定全部后缀引用，才把中间list暴露给setter或跨yield恢复的调用者。
        if (suffix != 0) rest.Items.RemoveRange(middleCount, suffix);
        values[prefix] = rest;
        return new PyTuple(values);
    }

    private static void UnpackExMinimum(int actual, int required)
    {
        if (actual < required)
            throw UnpackError($"not enough values to unpack (expected at least {required}, got {actual})");
    }

    private static IPyIterator UnpackExIterator(object? source, bool initial)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try
        {
            // 显式iter(user)的隐藏适配器不是另一个Python对象；两次获取都恢复真实实例语义。
            if (source is PyInstanceIterator adapter) source = adapter.Instance;
            // str派生载体继承PyInstance但根槽不存普通类字典；现布局守卫禁止覆写iter。
            // 必须在普通实例查找之前保留已有按码点迭代，不能把合法str子类误当不可迭代。
            if (source is PyStringInstance or PyListInstance or PyDictInstance) return PyIter.Create(source);
            if (source is PyInstance instance)
            {
                if (!instance.Class.TryLookup("__iter__", out PyClass owner, out object? raw))
                {
                    if (instance.Class.TryLookup("__getitem__", out _, out _))
                        return new UnpackExIndexedIterator(instance);
                    throw UnpackExNotIterable(instance, initial);
                }
                object? callable = UnpackExBindSpecial(instance, owner, "__iter__", raw);
                if (callable is null or PyNone) throw UnpackExNotIterable(instance, initial: false);
                object? iterator = CallValue(callable, [], [], []);
                // 验证结果的next槽即可，不能在这里多调用第三次iter。
                if (iterator is IPyIterator ready) return ready;
                if (iterator is PyInstance user && user.Class.TryLookup("__next__", out _, out _))
                    return new PyInstanceIterator(user);
                throw new PyTypeError($"iter() returned non-iterator of type '{PyRepr.TypeName(iterator)}'");
            }
            // 原生流保留PyIter既有关闭状态守卫；其它内建游标的iter返回自身。
            if (source is PyClass cls && !PyEnum.IsEnumClass(cls))
                throw UnpackExNotIterable(source, initial);
            if (source is null or PyNone or PyTypingTypeVar || source is not (IPyIterator or PyNativeObject
                or PyList or PyTuple or PyDict or PyDictView or PySet or PyFrozenSet
                or string or PyStringInstance or PyBytes or PyByteArray or PyRange or PyClass))
                throw UnpackExNotIterable(source, initial);
            return PyIter.Create(source);
        }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
        finally
        {
            PyTraceback.PopTo(depth + 1);
            PyErrors.LeaveHandler(context);
        }
    }

    private static PyTypeError UnpackExNotIterable(object? source, bool initial) => new(initial
        ? $"cannot unpack non-iterable {PyRepr.TypeName(source)} object"
        : $"'{PyRepr.TypeName(source)}' object is not iterable");

    private static bool UnpackExNext(IPyIterator iterator, out object? value)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        value = null;
        try
        {
            if (iterator is PyInstanceIterator user)
            {
                // 不缓存用户方法正文或适配器finished：临时Stop后外部next仍可恢复。
                if (!UnpackExCallSpecial(user.Instance, "__next__", [], out value))
                    throw new PyTypeError("object is not an iterator");
                return true;
            }
            if (!iterator.MoveNext()) return false;
            value = iterator.Current();
            return true;
        }
        catch (PyStopIteration) { return false; }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
        finally
        {
            // map可能内部已吞Stop并返回false；所有出口均恢复，避免污染数量错误context。
            PyTraceback.PopTo(depth + 1);
            PyErrors.LeaveHandler(context);
        }
    }

    /// <summary>星号专用getitem回退，暴露源与已消费index仅供本helper的length_hint使用。</summary>
    private sealed class UnpackExIndexedIterator(PyInstance source) : IPyIterator
    {
        internal PyInstance Source => source;
        internal long Index { get; private set; }
        private object? _current;
        private bool _finished;
        public bool MoveNext()
        {
            if (_finished) return false;
            if (Index == long.MaxValue) throw new PyOverflowError("iter index too large");
            try
            {
                if (!UnpackExCallSpecial(source, "__getitem__", [Index], out _current))
                    throw new PyTypeError("object is not subscriptable");
                Index++;
                return true;
            }
            catch (PyIndexError) { _finished = true; return false; }
            catch (PyStopIteration) { _finished = true; return false; }
        }
        public object? Current() => _current;
    }
}
