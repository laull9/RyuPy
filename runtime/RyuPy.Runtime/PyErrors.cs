using System;

namespace RyuPy.Runtime;

/// <summary>
/// 运行时错误辅助：当前异常状态（<c>__context__</c> 与裸 <c>raise</c>）、异常构造与绑定检查。
/// </summary>
/// <remarks>
/// # 为什么需要「当前异常」
///
/// Python 的异常语义有两处依赖「正在处理的异常」这个概念：
/// <list type="bullet">
/// <item>裸 <c>raise</c>（不带操作数）重抛当前正在处理的异常；</item>
/// <item>在处理某个异常期间抛出新的异常时，新异常的 <c>__context__</c> 自动指向旧异常，
/// traceback 因此输出「During handling of the above exception...」。</item>
/// </list>
///
/// CIL 的 <c>rethrow</c> 只能直接写在 catch 子句体内，而 Python 允许裸 <c>raise</c> 出现在
/// catch 体里的任意位置（比如嵌套的 <c>if</c> 分支里），所以这里改用「异常对象存进局部变量 +
/// 运行时保存当前异常」的做法：进入 <c>except</c> 时登记当前异常，离开时恢复上一层。
///
/// # 未赋值局部变量
///
/// CPython 读取未赋值的局部变量抛 <c>UnboundLocalError</c>。为了不在每次读取时都做检查，
/// 编译器只给「可能未赋值」的变量插入 <see cref="Load"/> 调用；其余读取是裸读。
/// 这类变量在函数入口被初始化成 <see cref="PyUnbound"/> 哨兵，而不是 CLR 默认值
/// （<c>0</c> / <c>null</c>）——否则未赋值会被静默当成 <c>0</c> 或 <c>None</c>。
/// </remarks>
public static class PyErrors
{
    [ThreadStatic]
    private static PyBaseException? _current;

    /// <summary>当前正在处理的异常；没有则为 <c>null</c>。</summary>
    public static PyBaseException? Current => _current;

    /// <summary>
    /// 抛出前的准备：若当前有异常在处理且不是自己，则把 <c>__context__</c> 指向它。
    /// </summary>
    /// <param name="exception">即将抛出的异常。</param>
    /// <returns>同一个异常对象，便于链式调用。</returns>
    public static PyBaseException Prepare(PyBaseException exception)
    {
        if (
            _current is not null
            && !ReferenceEquals(_current, exception)
            && exception.Context is null
            && !ReferenceEquals(exception.Cause, _current)
        )
        {
            exception.Context = _current;
        }
        return exception;
    }

    /// <summary>进入 <c>except</c> 体：把当前异常换成刚捕获的这个。</summary>
    /// <param name="exception">捕获到的异常。</param>
    /// <returns>进入之前的当前异常，退出时传回 <see cref="LeaveHandler"/>。</returns>
    public static object? EnterHandler(object exception)
    {
        if (exception is not PyBaseException caught)
        {
            throw new PyTypeError("exceptions must derive from BaseException");
        }
        PyBaseException? previous = _current;
        _current = caught;
        return previous;
    }

    /// <summary>离开 <c>except</c> 体：恢复上一层正在处理的异常。</summary>
    /// <param name="previous"><see cref="EnterHandler"/> 的返回值。</param>
    public static void LeaveHandler(object? previous) => _current = previous as PyBaseException;

    /// <summary>裸 <c>raise</c>：重抛当前异常；没有则抛 <c>RuntimeError</c>。</summary>
    public static object Reraise() =>
        _current ?? throw new PyRuntimeError("No active exception to reraise");

    /// <summary>
    /// 抛出前的准备：记录 traceback 栈帧、挂上隐式上下文，返回同一个异常对象。
    /// </summary>
    /// <param name="exception">即将抛出的异常。</param>
    public static object PrepareRaise(object exception)
    {
        if (exception is not PyBaseException ex)
        {
            throw new PyTypeError("exceptions must derive from BaseException");
        }
        PyTraceback.Attach(ex);
        return Prepare(ex);
    }

    /// <summary>按名字构造异常；<paramref name="arg"/> 为 <c>None</c> 时消息为空。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="arg">消息参数。</param>
    public static PyBaseException Make(string typeName, object? arg)
    {
        string message = arg is null or PyNone ? string.Empty : PyRepr.Str(arg);
        return PyExceptionTypes.Create(typeName, message);
    }

    /// <summary><c>raise exc from cause</c>：返回挂了显式原因的新异常对象。</summary>
    /// <param name="exception">被抛出的异常。</param>
    /// <param name="cause">显式原因。</param>
    public static PyBaseException WithCause(object exception, object? cause)
    {
        if (exception is not PyBaseException ex)
        {
            throw new PyTypeError("exceptions must derive from BaseException");
        }
        PyBaseException? causeException = cause switch
        {
            PyBaseException c => c,
            null or PyNone => null,
            _ => throw new PyTypeError("exception causes must derive from BaseException"),
        };
        return ex.WithCause(causeException);
    }

    /// <summary>读取可能未赋值的局部变量；哨兵值表示从未赋值。</summary>
    /// <param name="value">变量当前内容。</param>
    /// <param name="name">变量名，用于错误消息。</param>
    public static object Load(object? value, string name)
    {
        if (value is null or PyUnbound)
        {
            throw new PyUnboundLocalError(
                $"cannot access local variable '{name}' where it is not associated with a value"
            );
        }
        return value;
    }

    /// <summary>未赋值局部变量的哨兵值。</summary>
    public static object Unbound() => PyUnbound.Instance;
}

/// <summary>
/// 「从未赋值」哨兵。与 <see cref="PyNone"/> 分开：<c>None</c> 是合法的 Python 值，
/// 而哨兵只表示「这个槽位还没被写过」，读到它必须抛 <c>UnboundLocalError</c>。
/// </summary>
public sealed class PyUnbound
{
    private PyUnbound() { }

    /// <summary>唯一实例。</summary>
    public static readonly PyUnbound Instance = new();

    /// <summary>调试输出；正常程序不应该打印到它。</summary>
    public override string ToString() => "<unbound>";
}
