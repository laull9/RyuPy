using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>
/// mappingproxy 专有迭代边界：公开 raw 身份，内部只适配一次；包含按项早停，dict 反向保活游标。
/// keys/items 消费者可复用 Start/ReadNext，不改变普通内建或任意对象的全局迭代实现。
/// </summary>
internal static class PyMappingProxyIteration
{
    /// <summary>返回真正的 Python iterator；检查返回槽而不调用结果的 iter/next。</summary>
    internal static object Raw(object? source)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try { return RawCore(source); }
        // 获取迭代器期间的 Stop 也是用户错误，保存同一对象后传播，绝不能当作空迭代器。
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static object RawCore(object? source)
    {
        if (source is PyMappingProxy proxy) return proxy.Iterate();
        if (source is PyStringInstance or PyListInstance or PyDictInstance) return PyIter.Create(source);
        if (source is PyInstance instance)
        {
            if (!Lookup(instance, "__iter__", out object? callable))
            {
                if (instance.Class.TryLookup("__getitem__", out _, out _)) return new Indexed(instance);
                throw NotIterable(instance);
            }
            if (callable is null or PyNone) throw NotIterable(instance);
            return ValidateIterator(PyOps.CallValue(callable, [], [], []));
        }
        if (source is PyNativeObject native)
        {
            if (!PyObject.TryMethod(native, "__iter__", [], out object? result)) throw NotIterable(native);
            return ValidateIterator(result);
        }
        return PyIter.Create(source);
    }

    private static object ValidateIterator(object? result)
    {
        if (result is IPyIterator) return result;
        if (result is PyInstance instance && instance.Class.TryLookup("__next__", out _, out _)) return instance;
        throw PyMappingProxy.Error(new PyTypeError($"iter() returned non-iterator of type '{PyRepr.TypeName(result)}'"));
    }

    /// <summary>对输入取 iter 一次，raw 用户对象只包内部 adapter，不二次取 iter。</summary>
    internal static IPyIterator Start(object? source)
    {
        object raw = Raw(source);
        return raw is IPyIterator ready ? ready : new PyInstanceIterator((PyInstance)raw);
    }

    /// <summary>按项取值，保存被吞的终止现场后恢复调用者上下文，不重包其他用户异常。</summary>
    internal static bool ReadNext(IPyIterator iterator, out object? value)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        value = null;
        try
        {
            // map.MoveNext 也会先吞用户的原 Stop；沿其真实 Next 保存这个对象而非丢弃现场。
            if (iterator is PyMap map) { value = map.Next(); return true; }
            // adapter.MoveNext 会先吞 StopIteration；这里直接调用真实 next 以保存该异常现场。
            if (iterator is PyInstanceIterator adapter)
            {
                if (!Lookup(adapter.Instance, "__next__", out object? callable))
                    throw PyMappingProxy.Error(new PyTypeError("object is not an iterator"));
                value = PyOps.CallValue(callable, [], [], []); return true;
            }
            if (!iterator.MoveNext()) return false;
            value = iterator.Current(); return true;
        }
        catch (PyStopIteration error) { PyErrors.PrepareRaise(error); return false; }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    /// <summary>只供明确要求物化的 proxy list/tuple 等入口；包含和消费者不借此预读全部值。</summary>
    internal static object?[] Materialize(PyMappingProxy proxy)
    {
        var values = new List<object?>(); IPyIterator iterator = Start(proxy);
        while (ReadNext(iterator, out object? value)) values.Add(value);
        return [.. values];
    }

    internal static bool Contains(object source, object? needle)
    {
        if (source is PyDict dictionary) return dictionary.Contains(needle);
        if (source is PyDictInstance) return PyDictProtocols.Contains(source, needle);
        if (source is string or PyStringInstance) return PyStringProtocols.Contains(source, needle);
        if (source is PyInstance instance)
        {
            if (Lookup(instance, "__contains__", out object? callable))
            {
                if (callable is null or PyNone) throw PyMappingProxy.Error(new PyTypeError(
                    $"'{PyRepr.TypeName(source)}' object is not a container"));
                return PyOps.Truthy(PyOps.CallValue(callable, [needle], [], []));
            }
        }
        else if (PyObject.TryMethod1(source, "__contains__", needle, out object? result))
            return PyOps.Truthy(result);
        IPyIterator iterator = Start(source);
        while (ReadNext(iterator, out object? value))
        {
            if (ReferenceEquals(value, needle) || PyHash.Equal(value, needle)) return true;
        }
        return false;
    }

    internal static object? Reverse(object source) => source is PyDict dictionary
        ? new ReverseDict(dictionary) : PyObject.InvokeMethod(source, "__reversed__", [], [], []);

    private static bool Lookup(PyInstance instance, string name, out object? callable)
    {
        if (!instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        { callable = null; return false; }
        if (raw is PyProperty || raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
            throw PyMappingProxy.Unsupported("descriptor for " + name);
        callable = PyObject.Bind(raw, owner, instance); return true;
    }

    private static PyBaseException NotIterable(object? source) => PyMappingProxy.Error(new PyTypeError(
        $"'{PyRepr.TypeName(source)}' object is not iterable"));

    // 只在成功读取后前移；None getitem 槽合法建立游标，实际 next 才报不可调用。
    private sealed class Indexed(PyInstance source) : IPyIterator
    {
        private long index;
        private object? current;
        private bool finished;
        public bool MoveNext()
        {
            if (finished) return false;
            if (index == long.MaxValue) throw PyMappingProxy.Error(new PyOverflowError("iter index too large"));
            long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
            try
            {
                if (!Lookup(source, "__getitem__", out object? callable))
                    throw PyMappingProxy.Error(new PyTypeError("object is not subscriptable"));
                current = PyOps.CallValue(callable, [index], [], []);
                index++; return true;
            }
            catch (PyIndexError error) { PyErrors.PrepareRaise(error); finished = true; return false; }
            catch (PyStopIteration error) { PyErrors.PrepareRaise(error); finished = true; return false; }
            catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
            finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
        }
        public object? Current() => current;
    }

    // 反向与正向使用同一槽位/版本规则；已耗尽后不因后续字典变化复活或抛错。
    private sealed class ReverseDict : IPyIterator
    {
        private readonly PyDict source;
        private readonly long version;
        private int slot;
        private object? current;
        private bool finished;
        internal ReverseDict(PyDict dictionary)
        { source = dictionary; version = dictionary.Version; slot = dictionary.SlotCount - 1; }
        public bool MoveNext()
        {
            if (finished) return false;
            if (source.Version != version) throw PyMappingProxy.Error(new PyRuntimeError("dictionary changed size during iteration"));
            while (slot >= 0)
            {
                if (source.TryKeyAt(slot--, out object? key)) { current = key; return true; }
            }
            finished = true; return false;
        }
        public object? Current() => current;
    }
}
