// TreeBuilder、新语法错误族及保存工厂的有限冷错误源；不接受或重新标记用户异常。
namespace RyuPy.Runtime;

internal static class PyXmlTreeBuilderErrors
{
    // 返回类型仅供throw表达式定型，所有分支都在本方法抛出。
    // V1的new/Fresh/throw必须连续，legacy附帧只在独立分支执行。
    internal static PyTypeError Type(string message)
    {
        if (PyTraceProduct.Enabled) throw PyTraceProduct.NativeFresh(new PyTypeError(message));
        var error = new PyTypeError(message); PyTraceback.Attach(error); PyErrors.Prepare(error); throw error;
    }
    internal static PyNotImplementedError Unsupported(string area)
    {
        string message = area + " is not supported";
        if (PyTraceProduct.Enabled) throw PyTraceProduct.NativeFresh(new PyNotImplementedError(message));
        var error = new PyNotImplementedError(message); PyTraceback.Attach(error); PyErrors.Prepare(error); throw error;
    }
    internal static PyIndexError Index(string message)
    {
        if (PyTraceProduct.Enabled) throw PyTraceProduct.NativeFresh(new PyIndexError(message));
        var error = new PyIndexError(message); PyTraceback.Attach(error); PyErrors.Prepare(error); throw error;
    }
    internal static PyXmlParseError Parse(string message)
    {
        if (PyTraceProduct.Enabled) throw PyTraceProduct.NativeFresh(new PyXmlParseError(message));
        var error = new PyXmlParseError(message); PyTraceback.Attach(error); PyErrors.Prepare(error); throw error;
    }
    internal static PyAttributeError Attribute(string message)
    {
        if (PyTraceProduct.Enabled) throw PyTraceProduct.NativeFresh(new PyAttributeError(message));
        var error = new PyAttributeError(message); PyTraceback.Attach(error); PyErrors.Prepare(error); throw error;
    }
    internal static PyNameError Name(string message)
    {
        if (PyTraceProduct.Enabled) throw PyTraceProduct.NativeFresh(new PyNameError(message));
        var error = new PyNameError(message); PyTraceback.Attach(error); PyErrors.Prepare(error); throw error;
    }
}
