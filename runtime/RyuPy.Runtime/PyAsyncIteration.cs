// async-for的类型级协议：入口同步、下一项逐轮重新绑定，耗尽身份和await错误包装独立于源码名字。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>同步调用一次__aiter__并检查返回对象的anext槽存在性，不执行该槽描述符。</summary>
    public static object? AsyncIterStart(object? source)
    {
        if (!TryAsyncMethod(source, "__aiter__", out object? method))
            throw new PyTypeError($"'async for' requires an object with __aiter__ method, got {AsyncTypeName(source)}");
        object? iterator = CallValue(method, [], [], []);
        if (!HasAsyncMethod(iterator, "__anext__"))
            throw new PyTypeError($"'async for' received an object from __aiter__ that does not implement __anext__: {AsyncTypeName(iterator)}");
        return iterator;
    }

    /// <summary>每轮重新取得并调用当前类型的anext槽；已开始的await不重新查询该方法。</summary>
    public static object? AsyncIterNext(object? iterator)
    {
        if (!TryAsyncMethod(iterator, "__anext__", out object? method))
            throw new PyTypeError($"'async for' requires an iterator with __anext__ method, got {AsyncTypeName(iterator)}");
        return CallValue(method, [], [], []);
    }

    /// <summary>只使用真实内建CLR异常身份；同名源码类或变量不能改变耗尽匹配。</summary>
    public static object AsyncIterIsStop(object? exception) => exception is PyStopAsyncIteration;

    /// <summary>GET_ANEXT在await迭代器获取失败时包装TypeError，迭代器执行失败仍直接传播。</summary>
    public static object CoroutineNextAwaitStart(object? source)
    {
        long depth = PyTraceback.CaptureDepth();
        try { return AwaitStart(source, null, true); }
        catch (PyBaseException error)
        {
            // 源__await__真实失败帧属于cause；包装错误产生于调用者，不能带上未弹出的callee。
            PyTraceback.ReceiveException(error, depth + 1);
            PyTraceback.PopTo(depth + 1);
            var wrapped = new PyTypeError($"'async for' received an invalid object from __anext__: {AsyncTypeName(source)}");
            wrapped.WithCause(error); wrapped.Context = error;
            throw wrapped;
        }
    }

    private static PyClass? AsyncClass(object? value) => value switch
    {
        PyInstance instance => instance.Class,
        PyBaseException error => error.PythonClass,
        _ => null,
    };
    private static bool HasAsyncMethod(object? value, string name)
    {
        if (AsyncClass(value) is not { } cls) return false;
        cls.GuardInheritedGenericProtocol(name);
        return cls.TryLookup(name, out _, out _);
    }
    private static bool TryAsyncMethod(object? value, string name, out object? method)
    {
        method = null;
        if (AsyncClass(value) is not { } cls) return false;
        cls.GuardInheritedGenericProtocol(name);
        if (!cls.TryLookup(name, out PyClass owner, out object? raw)) return false;
        PyTypingGenericProtocols.GuardDescriptorRead(cls, raw);
        long depth = PyTraceback.CaptureDepth();
        try
        {
            // 描述符绑定只发生于实际调用；槽存在检查不可执行getter。
            if (raw is PyProperty property)
            {
                if (property.Getter is null) throw new PyAttributeError($"property '{name}' has no getter");
                method = PyObject.InvokeBound(property.Getter, owner, value, [], [], []);
            }
            else if (raw is PyInstance && PyObject.TryMethod(raw, "__get__", [value, cls], out object? bound))
                method = bound;
            else method = PyObject.Bind(raw, owner, value);
        }
        catch (PyBaseException error)
        {
            // CPython slot_am_aiter/anext/await在lookup_maybe_method失败后覆盖为AttributeError。
            // 仅绑定失败进入此处；已经取得方法后的真实调用异常保持原身份。
            PyTraceback.ReceiveException(error, depth + 1);
            PyTraceback.PopTo(depth + 1);
            throw new PyAttributeError($"object {AsyncTypeName(value, 50)} does not have {name} method");
        }
        return true;
    }

    // PyErr_Format的%.Ns按UTF-8字节截断并替换不完整字符，不能按UTF-16或字符数截断。
    private static string AsyncTypeName(object? value, int precision = 100)
    {
        string name = PyRepr.TypeName(value);
        if (System.Text.Encoding.UTF8.GetByteCount(name) <= precision) return name;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(name);
        return System.Text.Encoding.UTF8.GetString(bytes, 0, precision);
    }
}
