// 非星号解包：先冻结本层值，再交给编译器按目标顺序写入；不改变通用迭代器协议。
using System;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>读取指定数量的元素并验证耗尽；成功结果始终是供目标写入使用的元组。</summary>
    /// <remarks>普通实例缺少 __iter__ 时局部回退到整数 __getitem__；星号不在本入口范围内。</remarks>
    public static object UnpackSequence(object? source, long required)
    {
        // 数量来自编译器，不根据输入的 len / length_hint 分配，也不截断窄化转换。
        if (required < 0 || required > Array.MaxLength)
            throw UnpackError("invalid unpack target count");
        int count = (int)required;
        if (source is PyTuple tuple)
        {
            UnpackExactCount(tuple.Length, required);
            return tuple;
        }
        if (source is PyList list)
        {
            UnpackExactCount(list.Count, required);
            var frozen = new object?[count];
            // Items 会使数值背板永久物化；GetItem 只冻结所需盒的身份。
            for (int i = 0; i < count; i++) frozen[i] = list.GetItem(i);
            return new PyTuple(frozen);
        }

        IPyIterator iterator = UnpackIterator(source);
        var values = new object?[count];
        for (int i = 0; i < count; i++)
        {
            if (!UnpackNext(iterator, out values[i]))
                throw UnpackError($"not enough values to unpack (expected {required}, got {i})");
        }
        // 只探测多出的一项：不能耗尽无限游标，也不能吞掉探测时的其它异常。
        if (UnpackNext(iterator, out _))
            throw UnpackError($"too many values to unpack (expected {required})");
        return new PyTuple(values);
    }

    private static void UnpackExactCount(long actual, long required)
    {
        if (actual < required)
            throw UnpackError($"not enough values to unpack (expected {required}, got {actual})");
        if (actual > required)
            throw UnpackError($"too many values to unpack (expected {required})");
    }

    private static PyBaseException UnpackError(string message) =>
        (PyBaseException)PyErrors.PrepareRaise(new PyValueError(message));

    private static IPyIterator UnpackIterator(object? source)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try
        {
            // TypeVar 类型无迭代槽，extra 不构成协议；解包错误不同于显式 iter 的错误。
            if (source is PyTypingTypeVar) throw UnpackExNotIterable(source, initial: true);
            // 只检查类级特殊方法。存在但为 None 或抛错的 __iter__ 绝不能降级。
            if (source is PyInstance instance
                && !instance.Class.TryLookup("__iter__", out _, out _)
                && instance.Class.TryLookup("__getitem__", out _, out _))
                return new UnpackIndexedIterator(instance);
            // 获取阶段的 StopIteration 也必须原样传播；不重写用户 TypeError。
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

    private static bool UnpackNext(IPyIterator iterator, out object? value)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        value = null;
        try
        {
            // 与 map 一样读取真实 next，不让本次耗尽把用户适配器永久标成 finished。
            if (iterator is PyInstanceIterator user)
            {
                if (!PyObject.TryMethod(user.Instance, "__next__", [], out value))
                    throw new PyTypeError("object is not an iterator");
                return true;
            }
            if (!iterator.MoveNext()) return false;
            value = iterator.Current();
            return true;
        }
        catch (PyStopIteration)
        {
            // 仅推进阶段解释耗尽；不要建立 handler，否则会污染后续数量错误的 context。
            return false;
        }
        catch (PyBaseException error)
        {
            // 先保存原回调的帧和上下文，再恢复调用者；已有快照与异常身份均不替换。
            PyErrors.PrepareRaise(error);
            throw;
        }
        finally
        {
            // map 等适配器可能已把 StopIteration 变成 false，成功出口也需要恢复。
            PyTraceback.PopTo(depth + 1);
            PyErrors.LeaveHandler(context);
        }
    }

    /// <summary>仅供解包的序列回退；不改变 for / list / map 的全局迭代协议。</summary>
    private sealed class UnpackIndexedIterator(PyInstance instance) : IPyIterator
    {
        private long _index;
        private object? _current;

        public bool MoveNext()
        {
            try
            {
                // 不调用 len，也不请求切片；类方法若在消费中改变则使用当前方法。
                if (!PyObject.TryMethod(instance, "__getitem__", [_index], out _current))
                    throw new PyTypeError("object is not subscriptable");
                _index++;
                return true;
            }
            // 只有序列回退把 IndexError 解释为结束；普通 next 的同名异常仍传播。
            // StopIteration 交由外层推进边界处理，两条耗尽路径均恢复帧和上下文。
            catch (PyIndexError) { return false; }
        }

        public object? Current() => _current;
    }
}
