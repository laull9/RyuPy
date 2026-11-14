// list.extend / += 的局部消费协议：先 iter，再源长度提示，逐项立即提交；不改全局 for/list/map。
// 精确 list/tuple 不经过用户协议或全量中间数组；其它回调恢复帧和 ambient，保留原异常链。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    internal static void ExtendList(PyList target, object? source)
    {
        if (source is PyList list) { target.ExtendExact(list); return; }
        if (source is PyListInstance derived) { target.ExtendExact(derived.Payload); return; }
        if (source is PyTuple tuple && source.GetType() == typeof(PyTuple))
        { target.ExtendExact(tuple); return; }
        // 显式 iter(user) 的适配器不是新的 Python 实例；再次消费必须调用真实 __iter__。
        if (source is PyInstanceIterator adapter) source = adapter.Instance;
        IPyIterator iterator = ListExtendIterator(source);
        long hint = ListExtendHint(source);
        target.ReserveExtend(hint);
        while (ListExtendNext(iterator, out object? value)) target.Append(value);
    }

    private static IPyIterator ListExtendIterator(object? source)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try
        {
            if (source is PyStringInstance or PyListInstance or PyDictInstance) return PyIter.Create(source);
            if (source is PyInstance instance)
            {
                if (!instance.Class.TryLookup("__iter__", out PyClass owner, out object? raw))
                {
                    if (instance.Class.TryLookup("__getitem__", out _, out _))
                        return new ListExtendIndexedIterator(instance);
                    throw ListExtendError(new PyTypeError($"'{instance.Class.Name}' object is not iterable"));
                }
                object? callable = ListExtendBind(instance, owner, "__iter__", raw);
                if (callable is null or PyNone)
                    throw ListExtendError(new PyTypeError($"'{instance.Class.Name}' object is not iterable"));
                object? result = CallValue(callable, [], [], []);
                // 返回值只验证 next 槽；不能再调用返回游标的 iter 或向它查询 hint。
                if (result is IPyIterator ready) return ready;
                if (result is PyInstance user && user.Class.TryLookup("__next__", out _, out _))
                    return new PyInstanceIterator(user);
                throw ListExtendError(new PyTypeError($"iter() returned non-iterator of type '{PyRepr.TypeName(result)}'"));
            }
            return PyIter.Create(source);
        }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static long ListExtendHint(object? source)
    {
        if (ListExtendTryLength(source, out long length)) return length;
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try
        {
            // hint 的 getter/绑定与返回类型检查均在 CALL 的 TypeError 忽略区之外。
            if (!ListExtendLookup(source, "__length_hint__", out object? callable)) return 8;
            object? result;
            try { result = CallValue(callable, [], [], []); }
            catch (PyTypeError) { return 8; }
            if (result is PyNotImplemented) return 8;
            long hint;
            switch (result)
            {
                case bool flag: hint = flag ? 1 : 0; break;
                case int small: hint = small; break;
                case long machine: hint = machine; break;
                case BigInteger big:
                    // hint 先查 ssize_t 范围，再查负数；len 的负大整数顺序不同。
                    if (big < long.MinValue || big > long.MaxValue)
                        throw ListExtendError(new PyOverflowError("Python int too large to convert to C ssize_t"));
                    hint = (long)big;
                    break;
                default:
                    throw ListExtendError(new PyTypeError($"__length_hint__ must be an integer, not {PyRepr.TypeName(result)}"));
            }
            if (hint < 0) throw ListExtendError(new PyValueError("__length_hint__() should return >= 0"));
            return hint;
        }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static bool ListExtendTryLength(object? source, out long length)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        length = 0;
        try
        {
            if (IsBuiltinSized(source) || source is PyStringInstance or PyListInstance or PyDictInstance)
            { length = Len(source); return true; }
            if (source is PyRange range) { length = ListExtendRangeLength(range); return true; }
            if (!ListExtendCall(source, "__len__", [], out object? result)) return false;
            length = LengthResult(result, "list.extend");
            return true;
        }
        // len 的 TypeError 包含 getter、调用和 __index__ 转换，均允许回退源 hint。
        catch (PyTypeError) { return false; }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static long ListExtendRangeLength(PyRange range)
    {
        // 只修本API的提示计算，避免旧range.Length中间加法回绕成负容量；不改range迭代器。
        // 大跨度可有少量元素，先用宽差值求商，再按CP ssize_t边界拒绝真正超大长度。
        BigInteger span = range.Step > 0 ? (BigInteger)range.Stop - range.Start : (BigInteger)range.Start - range.Stop;
        if (span <= 0) return 0;
        BigInteger count = (span - 1) / BigInteger.Abs(range.Step) + 1;
        if (count > long.MaxValue)
            throw ListExtendError(new PyOverflowError("Python int too large to convert to C ssize_t"));
        return (long)count;
    }

    private static bool ListExtendNext(IPyIterator iterator, out object? value)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        value = null;
        try
        {
            if (iterator is PyInstanceIterator user)
            {
                // 用户 next 的一次 Stop 只结束本次消费，不能永久封死可恢复的用户游标。
                if (!ListExtendCall(user.Instance, "__next__", [], out value))
                    throw ListExtendError(new PyTypeError("object is not an iterator"));
                return true;
            }
            if (!iterator.MoveNext()) return false;
            value = iterator.Current();
            return true;
        }
        catch (PyStopIteration) { return false; }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static bool ListExtendLookup(object? source, string name, out object? callable)
    {
        if (source is PyInstance instance && instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        { callable = ListExtendBind(instance, owner, name, raw); return true; }
        if (source is PyNativeObject native)
        {
            return PyObject.TryGetNativeSpecial(native, name, out callable);
        }
        callable = null;
        return false;
    }

    private static object? ListExtendBind(PyInstance instance, PyClass owner, string name, object? raw)
    {
        // 沿用普通对象的受支持绑定；自定义描述符不能绕过 __get__ 伪装成普通 callable。
        if (raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
            throw ListExtendError(new PyNotImplementedError($"list.extend: custom descriptor for {name} is not supported"));
        if (raw is PyProperty property)
        {
            if (property.Getter is null or PyNone)
                throw ListExtendError(new PyAttributeError($"property '{name}' has no getter"));
            return PyObject.InvokeBound(property.Getter, owner, instance, [], [], []);
        }
        return PyObject.Bind(raw, owner, instance);
    }

    private static bool ListExtendCall(object? source, string name, object?[] args, out object? result)
    {
        if (source is PyInstance instance && instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        {
            // 常见 next 函数直接绑定调用，不为每个元素分配 PyBoundMethod 包装。
            result = raw is PyFunction or PyStaticMethod or PyClassMethod
                ? PyObject.InvokeBound(raw, owner, instance, args, [], [])
                : CallValue(ListExtendBind(instance, owner, name, raw), args, [], []);
            return true;
        }
        if (ListExtendLookup(source, name, out object? callable))
        { result = CallValue(callable, args, [], []); return true; }
        result = null;
        return false;
    }

    // 仅新造的本层协议错误准备上下文；用户异常只 Attach 缺失帧，绝不重写原链。
    private static PyBaseException ListExtendError(PyBaseException error) =>
        (PyBaseException)PyErrors.PrepareRaise(error);

    /// <summary>仅 extend 的序列回退；不扩大 for/list/map 的全局迭代支持域。</summary>
    private sealed class ListExtendIndexedIterator(PyInstance source) : IPyIterator
    {
        private long _index;
        private object? _current;
        private bool _finished;
        public bool MoveNext()
        {
            if (_finished) return false;
            if (_index == long.MaxValue) throw ListExtendError(new PyOverflowError("iter index too large"));
            try
            {
                if (!ListExtendCall(source, "__getitem__", [_index], out _current))
                    throw ListExtendError(new PyTypeError("object is not subscriptable"));
                _index++;
                return true;
            }
            catch (PyIndexError) { _finished = true; return false; }
            catch (PyStopIteration) { _finished = true; return false; }
        }
        public object? Current() => _current;
    }
}

public sealed partial class PyList
{
    internal void ExtendExact(PyList source)
    {
        // List.AddRange 原生支持 self；无额外全量数组。数值路固定原长，保持别名有限复制。
        if (_items is { } target && source._items is { } items) { target.AddRange(items); return; }
        int count = source.Count;
        ReserveExtend(count);
        for (int index = 0; index < count; index++) Append(source.GetItem(index));
    }

    internal void ExtendExact(PyTuple source)
    {
        if (_items is { } target) { target.AddRange(source.Items); return; }
        ReserveExtend(source.Length);
        foreach (object? value in source.Items) Append(value);
    }

    internal void ReserveExtend(long hint)
    {
        // ADR0068同类有界估计：完整验证提示，只预留至多4096额外槽；实际元素不设上限。
        // 不捕获 OOM 重试；巨大合法 hint 的资源失败时点不承诺与 CP 逐分配等价。
        int capacity = (int)Math.Min(int.MaxValue, Count + Math.Min(hint, 4096));
        if (_items is { } items) items.EnsureCapacity(capacity);
        else _numeric!.EnsureCapacity(capacity);
    }
}
