// namedtuple 的 defaults/_make 专用 tuple 物化；先 iter，再原对象 length hint，最后完整逐项消费。
// 只在此冷入口绑定类级特殊槽、清理被吞异常，不更改通用 tuple/list/iter 或编译器 ABI。
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyNamedTupleSequence
{
    internal static PyTuple Materialize(object? source)
    {
        if (source is PyTuple tuple && source is not PyNamedTupleInstance) return tuple;
        if (source is PyList list)
        {
            var copied = new object?[list.Count];
            for (int index = 0; index < copied.Length; index++) copied[index] = list.GetItem(index);
            return new PyTuple(copied); // 不以 Items 访问永久退化列表的数值背板。
        }
        IPyIterator iterator = Start(source);
        CheckHint(source);
        var items = new List<object?>();
        while (Next(iterator, out object? value)) items.Add(value);
        return new PyTuple([.. items]);
    }

    internal static IPyIterator Start(object? source)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try
        {
            if (source is PyInstanceIterator adapter) source = adapter.Instance;
            if (source is PyNativeObject || source is PyInstance and not PyStringInstance and not PyListInstance and not PyDictInstance)
            {
                bool hasIterator = Lookup(source, "__iter__", out object? callable);
                if (!hasIterator || callable is null or PyNone)
                {
                    if (!hasIterator && source is PyInstance instance && instance.Class.TryLookup("__getitem__", out _, out _))
                        throw PyNamedTupleLayout.Unsupported("namedtuple sequence-only __getitem__ iteration is not supported");
                    throw PyNamedTupleLayout.Error(new PyTypeError($"'{PyRepr.TypeName(source)}' object is not iterable"));
                }
                object? result = PyOps.CallValue(callable, [], [], []);
                if (result is IPyIterator iterator) return iterator;
                if (result is PyInstance next && next.Class.TryLookup("__next__", out _, out _))
                    return new PyInstanceIterator(next);
                throw PyNamedTupleLayout.Error(new PyTypeError($"iter() returned non-iterator of type '{PyRepr.TypeName(result)}'"));
            }
            return PyIter.Create(source);
        }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    internal static bool Next(IPyIterator iterator, out object? value)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        value = null;
        try
        {
            // 不经过旧适配器的 finished/cache；next 的 property 绑定及耗尽清理都在本层。
            if (iterator is PyInstanceIterator user)
            {
                if (!Lookup(user.Instance, "__next__", out object? callable))
                    throw PyNamedTupleLayout.Error(new PyTypeError("object is not an iterator"));
                value = PyOps.CallValue(callable, [], [], []); return true;
            }
            if (!iterator.MoveNext()) return false;
            value = iterator.Current(); return true;
        }
        catch (PyStopIteration) { return false; }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static void CheckHint(object? source)
    {
        if (source is PyInstanceIterator adapter) source = adapter.Instance;
        // 普通不可改槽内建值无用户提示副作用；提示不用于不可信的巨量预分配。
        if (source is not (PyInstance or PyNativeObject) || source is PyStringInstance or PyListInstance or PyDictInstance) return;
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try
        {
            if (TryLength(source)) return;
            // lookup/getter/bind 都在可吞 TypeError 的 CALL 区之外。
            if (!Lookup(source, "__length_hint__", out object? callable)) return;
            object? result;
            try { result = PyOps.CallValue(callable, [], [], []); }
            catch (PyTypeError) { return; }
            if (result is PyNotImplemented) return;
            if (!PyInt.IsInt(result))
                throw PyNamedTupleLayout.Error(new PyTypeError($"__length_hint__ must be an integer, not {PyRepr.TypeName(result)}"));
            BigInteger hint = PyInt.ToBigInteger(result!);
            if (hint > long.MaxValue || hint < long.MinValue)
                throw PyNamedTupleLayout.Error(new PyOverflowError("Python int too large to convert to C ssize_t"));
            if (hint.Sign < 0)
                throw PyNamedTupleLayout.Error(new PyValueError("__length_hint__() should return >= 0"));
        }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static bool TryLength(object source)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try
        {
            if (!Lookup(source, "__len__", out object? callable)) return false;
            PyOps.NamedTupleLengthResult(PyOps.CallValue(callable, [], [], [])); return true;
        }
        catch (PyTypeError) { return false; }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    private static bool Lookup(object source, string name, out object? callable)
    {
        if (source is PyInstance instance && instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        {
            if (raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
                throw PyNamedTupleLayout.Unsupported($"namedtuple: custom descriptor for {name} is not supported");
            if (raw is PyProperty property)
            {
                if (property.Getter is null or PyNone)
                    throw PyNamedTupleLayout.Error(new PyAttributeError($"property '{name}' has no getter"));
                callable = PyObject.InvokeBound(property.Getter, owner, instance, [], [], []);
            }
            else callable = PyObject.Bind(raw, owner, instance);
            return true;
        }
        if (source is PyNativeObject native)
        {
            return PyObject.TryGetNativeSpecial(native, name, out callable);
        }
        callable = null; return false;
    }
}

public static partial class PyOps
{
    // 仅内部冷桥复用既有 index/len 校验，不增加 runtime 引用或公共编译 ABI。
    internal static long NamedTupleLengthResult(object? value) => LengthResult(value, "namedtuple");
}
