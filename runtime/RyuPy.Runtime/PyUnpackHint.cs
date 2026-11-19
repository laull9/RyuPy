// 仅星号解包的冷长度估计与类级特殊绑定：len可用index，hint必须真实int，异常边界不混合。
// 不增加公共list/iter协议；property查找失败与返回callable后调用失败必须区分。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    private static long UnpackExLengthHint(IPyIterator iterator)
    {
        if (iterator is UnpackExIndexedIterator indexed)
        {
            // CP序列游标自己的hint代理源len-index，不读取源__length_hint__。
            return UnpackExTryLength(indexed.Source, out long size)
                ? Math.Max(0, size - indexed.Index) : 8;
        }
        object source = iterator is PyInstanceIterator user ? user.Instance : iterator;
        if (UnpackExTryLength(source, out long length)) return length;
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try
        {
            // 查找/描述符绑定在允许忽略TypeError的CALL区外；不能吞property getter异常。
            if (!UnpackExLookupSpecial(source, "__length_hint__", out object? callable)) return 8;
            object? result;
            try { result = CallValue(callable, [], [], []); }
            catch (PyTypeError) { return 8; }
            if (result is PyNotImplemented) return 8;
            // 返回校验也在CALL catch之外：非int TypeError必须传播，禁止尝试__index__。
            return UnpackExHintResult(result);
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

    private static bool UnpackExTryLength(object source, out long length)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        length = 0;
        try
        {
            // 正确调用len属性getter/所得callable，不能把raw property误当不可调用再吞掉。
            if (!UnpackExCallSpecial(source, "__len__", [], out object? result)) return false;
            length = LengthResult(result, "starred unpack");
            return true;
        }
        catch (PyTypeError) { return false; }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
        finally
        {
            // 被允许忽略的len TypeError也先恢复ambient，再开始hint或尾部next。
            PyTraceback.PopTo(depth + 1);
            PyErrors.LeaveHandler(context);
        }
    }

    private static long UnpackExHintResult(object? value)
    {
        long result;
        switch (value)
        {
            case bool flag: result = flag ? 1 : 0; break;
            case int small: result = small; break;
            case long machine: result = machine; break;
            case BigInteger big:
                // 与len不同：先做ssize_t范围检查，所以超大负数也是OverflowError。
                if (big < long.MinValue || big > long.MaxValue)
                    throw new PyOverflowError("Python int too large to convert to C ssize_t");
                result = (long)big;
                break;
            default:
                throw new PyTypeError($"__length_hint__ must be an integer, not {PyRepr.TypeName(value)}");
        }
        if (result < 0) throw new PyValueError("__length_hint__() should return >= 0");
        return result;
    }

    private static bool UnpackExLookupSpecial(object source, string name, out object? callable)
    {
        if (source is PyInstance instance && instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        {
            callable = UnpackExBindSpecial(instance, owner, name, raw);
            return true;
        }
        if (source is PyNativeObject native)
        {
            // 可写实例字段不构成类型槽；其余原生对象仍沿既有显式协议边界。
            return PyObject.TryGetNativeSpecial(native, name, out callable);
        }
        callable = null;
        return false;
    }

    private static object? UnpackExBindSpecial(PyInstance instance, PyClass owner, string name, object? raw)
    {
        // 普通class尚可声明任意__get__描述符，但公共Bind不实现它。必须明确拒绝，
        // 不能跳过getter直接调用raw而伪装成功；只有__call__的对象仍是合法普通槽值。
        if (raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
            throw new PyNotImplementedError($"starred unpack: custom descriptor for {name} is not supported");
        if (raw is PyProperty property)
        {
            if (property.Getter is null or PyNone)
                throw new PyAttributeError($"property '{name}' has no getter");
            return PyObject.InvokeBound(property.Getter, owner, instance, [], [], []);
        }
        return PyObject.Bind(raw, owner, instance);
    }

    private static bool UnpackExCallSpecial(object source, string name, object?[] args, out object? result)
    {
        if (source is PyInstance instance && instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        {
            // 普通next/getitem保直接调用，不每个元素额外分配PyBoundMethod包装。
            result = raw is PyFunction or PyStaticMethod or PyClassMethod
                ? PyObject.InvokeBound(raw, owner, instance, args, [], [])
                : CallValue(UnpackExBindSpecial(instance, owner, name, raw), args, [], []);
            return true;
        }
        if (UnpackExLookupSpecial(source, name, out object? callable))
        {
            result = CallValue(callable, args, [], []);
            return true;
        }
        result = null;
        return false;
    }
}
