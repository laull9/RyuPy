// 外部异常进入编译域时记录真实调用点；既有异常已保存的compiled帧不被后续往返覆盖。
using System;

namespace RyuPy.Runtime;

public static partial class PyErrors
{
    /// <summary>运行时入口和外部回调共享的可恢复CLR异常归一化。</summary>
    public static PyBaseException FromClr(Exception exception)
    {
        PyBaseException mapped = exception switch
        {
            PyBaseException python => python,
            OverflowException => new PyOverflowError("int too large to fit in a 64-bit integer (RyuPy static integer limit)"),
            DivideByZeroException => new PyZeroDivisionError("integer division or modulo by zero"),
            _ => new PyRuntimeError(exception.Message),
        };
        if (PyTraceProduct.Enabled) PyTraceProduct.NativeFresh(mapped);
        else PyTraceback.Attach(mapped);
        return Prepare(mapped);
    }

    /// <summary>外部异常传播入口；对象读取与捕获前检查不提前建立传播帧。</summary>
    public static PyBaseException ReceiveForeign(PyBaseException error)
    {
        if (PyTraceProduct.Enabled)
            throw new PyNotImplementedError("CPython provider does not support the experimental trace V1 execution domain");
        PyTraceback.Attach(error);
        return Prepare(error);
    }
}
