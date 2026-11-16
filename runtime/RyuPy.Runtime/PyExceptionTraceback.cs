// 原生异常传播状态与公开traceback身份；显式raise、真实接收、bare raise使用独立操作。
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed class NativeTracebackState
{
    internal PyNativeTraceback? Head;
    internal readonly HashSet<PyNativeFrame> Seen = new(ReferenceEqualityComparer.Instance);
    internal bool Active;
    internal void BeginReceive()
    {
        if (Active) return;
        Seen.Clear();
        Active = true;
    }
    internal void EnterHandler()
    {
        Active = false;
    }
    internal void Reraise()
    {
        Seen.Clear();
        var current = PyTraceback.CurrentNativeFrame();
        // RERAISE本身不为当前指令帧新增节点；跨出调用者时仍按真实接收补外层帧。
        if (current is not null) Seen.Add(current);
        Active = true;
    }
}

public partial class PyBaseException
{
    // 按真实异常冷分配，不随普通函数调用或源码with是否曾出现而改变。
    internal NativeTracebackState? NativeTraceback;
}

public static partial class PyErrors
{
    /// <summary>源码显式raise开始新的传播，保留已有traceback作为尾链。</summary>
    public static object PrepareExplicitRaise(object exception)
    {
        var error = (PyBaseException)PrepareRaise(exception);
        var state = error.NativeTraceback ??= new NativeTracebackState();
        state.Seen.Clear();
        state.Active = true;
        if (PyTraceback.CurrentNativeFrame() is { } frame)
        {
            state.Seen.Add(frame);
            state.Head = new(frame, frame.Line, state.Head);
        }
        return error;
    }
    /// <summary>读取本域真实传播链；未抛出的异常为None，外部异常保原provider来源。</summary>
    public static object ExceptionTraceback(object exception)
    {
        if (exception is not PyBaseException error) throw new PyTypeError("expected an exception");
        if (error.ForeignObject is PyNativeObject foreign) return foreign.GetAttr("__traceback__") ?? PyNone.Instance;
        return (object?)error.NativeTraceback?.Head ?? PyNone.Instance;
    }
}

public static partial class PyOps
{
    /// <summary>真实普通catch的中性接收ABI。</summary>
    public static void TracebackReceive(object exception, long receiverDepth) => PyTraceback.ReceiveException(exception, receiverDepth);
    /// <summary>真实生成器catch按独立存储载体选择接收激活。</summary>
    public static void GeneratorTracebackReceive(object frame, object exception) => PyTraceback.ReceiveGeneratorException(frame, exception);
    /// <summary>动态except真正匹配后结束传播，不更改当前异常token。</summary>
    public static void TracebackHandled(object exception)
    { if (exception is PyBaseException error) error.NativeTraceback?.EnterHandler(); }
    /// <summary>上下文退出读取真实链头；不调用用户属性覆盖。</summary>
    public static object TracebackOf(object exception) => PyErrors.ExceptionTraceback(exception);
}
