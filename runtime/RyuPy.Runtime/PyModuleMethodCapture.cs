namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>历史ABI名称保留；模块、生成tuple类/实例和真实str子类实例须在实参前取属性。</summary>
    public static bool IsModule(object? receiver)
    {
        // PyList为sealed精确载体；普通list不需捕获，避免逐个探测新增冷布局。
        if (receiver is PyList or PyDict) return false;
        return receiver is PyModule or PyNamedTupleInstance or PyStringInstance or PyListInstance or PyDictInstance
            || receiver is PyClass { NamedTupleLayout: not null } or PyClass { HasListLayout: true } or PyClass { HasDictLayout: true }
            or PySuper { Owner.HasListLayout: true } or PySuper { Owner.HasDictLayout: true } || receiver is PyType type && (ReferenceEquals(type, PyType.Get("list")) || ReferenceEquals(type, PyType.Get("dict")));
    }

    /// <summary>有界接收者在实参前读取原对象；其他接收者保持身份，不分配包装。</summary>
    public static object? CaptureModuleTarget(object? receiver, string name)
    {
        if (!IsModule(receiver)) return receiver;
        // 字符串子类property/__getattr__的用户正文不广catch；查找自身的错误在自造点准备。
        if (receiver is PyStringInstance or PyListInstance or PyDictInstance or PyClass { HasListLayout: true } or PyClass { HasDictLayout: true }
            or PySuper { Owner.HasListLayout: true } or PySuper { Owner.HasDictLayout: true } || receiver is PyType type && (ReferenceEquals(type, PyType.Get("list")) || ReferenceEquals(type, PyType.Get("dict"))))
            return PyObject.GetAttr(receiver, name);
        return CaptureActualModuleTarget(receiver!, name);
    }

    // 冷属性查找独立承担异常准备，使普通接收者身份路径不含EH；不强制内联。
    private static object? CaptureActualModuleTarget(object receiver, string name)
    {
        try { return PyObject.GetAttr(receiver, name); }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    /// <summary>数组调用只展开一次；未捕获接收者继续原方法协议。</summary>
    public static object? CallCapturedMethod(bool captured, object? target, string name,
        object?[] rawArgs, object?[] rawNames, object?[] rawValues) =>
        captured ? CallValue(target, rawArgs, rawNames, rawValues)
            : CallMethod(target, name, rawArgs, rawNames, rawValues);
}
