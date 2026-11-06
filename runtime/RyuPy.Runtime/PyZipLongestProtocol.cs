// zip_longest 专用迭代边界：类型槽绑定与正文调用分离，清理被吞耗尽/查找错误的帧及 ambient。
// 沿用内建游标，不改全局 PyIter；普通实例支持有限 property 槽和整数 getitem 回退。
namespace RyuPy.Runtime;

internal static class PyZipLongestProtocol
{
    internal static IPyIterator Start(object? source)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try
        {
            // 适配器不是另一个 Python 对象；显式 iter 返回的隐藏包装仍按真实实例重新取 iter。
            if (source is PyInstanceIterator adapter) source = adapter.Instance;
            if (source is PyStringInstance or PyListInstance or PyDictInstance) return PyIter.Create(source);
            if (source is not PyInstance instance) return PyIter.Create(source);
            if (!instance.Class.TryLookup("__iter__", out PyClass owner, out object? raw))
            {
                if (instance.Class.TryLookup("__getitem__", out _, out _)) return new Indexed(instance);
                throw NotIterable(instance);
            }
            // 未实现的任意 descriptor 不是用户 getter 失败，不能把该 NI 吞掉再回退。
            GuardDescriptor(raw, "__iter__");
            if (!TryBindLookup(instance, owner, raw, "__iter__", out object? callable))
            {
                // CP slot_tp_iter 在 lookup/绑定失败时清错误后验证 getitem；正文调用不在此 catch。
                if (!instance.Class.TryLookup("__getitem__", out PyClass itemOwner, out object? itemRaw))
                    throw NotIterable(instance);
                GuardDescriptor(itemRaw, "__getitem__");
                if (!TryBindLookup(instance, itemOwner, itemRaw, "__getitem__", out _)) throw NotIterable(instance);
                return new Indexed(instance);
            }
            if (callable is null or PyNone) throw NotIterable(instance);
            object? result = PyOps.CallValue(callable, [], [], []);
            if (result is IPyIterator ready) return ready;
            if (result is PyInstance user && user.Class.TryLookup("__next__", out _, out _))
                return new PyInstanceIterator(user);
            throw Own(new PyTypeError($"iter() returned non-iterator of type '{PyRepr.TypeName(result)}'"));
        }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    internal static bool ReadNext(IPyIterator iterator, out object? value)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        value = null;
        try
        {
            // map.MoveNext 会先吞原 Stop；用已有 Next 让本层保存该异常现场再退休此 zip 槽。
            if (iterator is PyMap map) { value = map.Next(); return true; }
            // 槽退休由 zip 管理；不能让一个共享用户适配器的 finished 位压掉另一槽的原始 next。
            if (iterator is PyInstanceIterator user)
            {
                if (!Lookup(user.Instance, "__next__", out object? callable))
                    throw Own(new PyTypeError("object is not an iterator"));
                value = PyOps.CallValue(callable, [], [], []); return true;
            }
            if (!iterator.MoveNext()) return false;
            value = iterator.Current(); // 先冻结值，再让外层推进下一个槽。
            return true;
        }
        catch (PyStopIteration error) { PyErrors.PrepareRaise(error); return false; }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally
        {
            // 原生 iterator 内部可能已吞 Stop；成功/false/异常出口都恢复真实调用者上下文。
            PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context);
        }
    }

    private static bool TryBindLookup(PyInstance instance, PyClass owner, object? raw, string name, out object? callable)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try { callable = Bind(instance, owner, raw, name); return true; }
        catch (PyBaseException error)
        {
            // 保存用户 getter 已有异常身份/现场后再清本次查找失败；不调用得到的 callable。
            PyErrors.PrepareRaise(error); callable = null; return false;
        }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static bool Lookup(PyInstance instance, string name, out object? callable)
    {
        if (!instance.Class.TryLookup(name, out PyClass owner, out object? raw)) { callable = null; return false; }
        GuardDescriptor(raw, name);
        callable = Bind(instance, owner, raw, name); return true;
    }

    private static object? Bind(PyInstance instance, PyClass owner, object? raw, string name)
    {
        if (raw is not PyProperty property) return PyObject.Bind(raw, owner, instance);
        if (property.Getter is null or PyNone) throw Own(new PyAttributeError($"property '{name}' has no getter"));
        return PyObject.InvokeBound(property.Getter, owner, instance, [], [], []);
    }

    private static void GuardDescriptor(object? raw, string name)
    {
        if (raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
            throw Unsupported($"custom descriptor for {name}");
    }

    private static PyBaseException NotIterable(object? source) => Own(new PyTypeError($"'{PyRepr.TypeName(source)}' object is not iterable"));
    internal static PyBaseException Own(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
    internal static PyBaseException Unsupported(string operation) => Own(new PyNotImplementedError($"zip_longest: {operation} is not supported"));

    // 私有序列游标不公开新迭代器类型；没有 len/hint，只有成功取值后增加下标。
    private sealed class Indexed(PyInstance source) : IPyIterator
    {
        private long index;
        private object? current;
        private bool finished;
        public bool MoveNext()
        {
            if (finished) return false;
            if (index == long.MaxValue) throw Own(new PyOverflowError("iter index too large"));
            try
            {
                if (!Lookup(source, "__getitem__", out object? callable))
                    throw Own(new PyTypeError("object is not subscriptable"));
                current = PyOps.CallValue(callable, [index], [], []);
                index++; return true;
            }
            catch (PyIndexError error) { PyErrors.PrepareRaise(error); finished = true; return false; }
            catch (PyStopIteration error) { PyErrors.PrepareRaise(error); finished = true; return false; }
        }
        public object? Current() => current;
    }
}
