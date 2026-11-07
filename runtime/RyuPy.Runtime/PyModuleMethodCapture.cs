// 有界方法目标预捕获；保留旧PE的历史ABI名，不将判据解释为Python模块类型判断。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>历史捕获判据ABI；名称并非Python模块类型测试。</summary>
    public static bool IsModule(object? receiver) => RequiresEarlyMethodCapture(receiver);

    /// <summary>哪些接收者必须在实参前固定属性目标；不扩普通实例的既有调用域。</summary>
    public static bool RequiresEarlyMethodCapture(object? receiver)
    {
        // PyList为sealed精确载体；普通list不需捕获，避免逐个探测新增冷布局。
        if (receiver is PyList or PyDict) return false;
        return receiver is PyModule or PySuper or PyNamedTupleInstance or PyStringInstance or PyListInstance or PyDictInstance
            || receiver is PyClass { NamedTupleLayout: not null } or PyClass { HasListLayout: true } or PyClass { HasDictLayout: true }
            || receiver is PyType type && (ReferenceEquals(type, PyType.Get("list")) || ReferenceEquals(type, PyType.Get("dict")));
    }

    /// <summary>旧PE的目标捕获ABI；转交意义明确的通用有界入口。</summary>
    public static object? CaptureModuleTarget(object? receiver, string name) => CaptureMethodTarget(receiver, name);

    /// <summary>有界接收者在实参前读取属性；其他接收者保持身份，不分配包装。</summary>
    public static object? CaptureMethodTarget(object? receiver, string name)
    {
        if (!RequiresEarlyMethodCapture(receiver)) return receiver;
        // 字符串子类property/__getattr__的用户正文不广catch；查找自身的错误在自造点准备。
        if (receiver is PySuper or PyStringInstance or PyListInstance or PyDictInstance or PyClass { HasListLayout: true } or PyClass { HasDictLayout: true }
            || receiver is PyType type && (ReferenceEquals(type, PyType.Get("list")) || ReferenceEquals(type, PyType.Get("dict"))))
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
