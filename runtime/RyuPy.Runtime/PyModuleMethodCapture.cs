namespace RyuPy.Runtime;

public static partial class PyOps
{
    // token不导出到Python；每次求值独立保存原值，不能按token类型缓存目标。
    private sealed class CapturedModuleMethod(object? value)
    {
        internal object? Value { get; } = value;
    }

    /// <summary>模块属性在实参求值前读取；其他接收者保持原协议且零分配。</summary>
    public static object? CaptureModuleMethod(object? receiver, string name)
    {
        if (receiver is not PyModule) return receiver;
        object? value;
        try { value = PyObject.GetAttr(receiver, name); }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
        // 不提前判断可调用性：非可调用值仍须先求值/展开实参。
        return new CapturedModuleMethod(value);
    }

    internal static bool TryCapturedModuleMethod(object? receiver, out object? value)
    {
        if (receiver is CapturedModuleMethod captured)
        {
            value = captured.Value;
            return true;
        }
        value = null;
        return false;
    }
}
