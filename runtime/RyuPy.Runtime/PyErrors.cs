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
public static partial class PyErrors
{
    [ThreadStatic]
    private static PyExceptionContext? _current;

    /// <summary>当前正在处理的异常；没有则为 <c>null</c>。</summary>
    public static PyBaseException? Current => _current?.FindException();

    /// <summary>
    /// 抛出前的准备：若当前有异常在处理且不是自己，则把 <c>__context__</c> 指向它。
    /// </summary>
    /// <param name="exception">即将抛出的异常。</param>
    /// <returns>同一个异常对象，便于链式调用。</returns>
    public static PyBaseException Prepare(PyBaseException exception)
    {
        PyBaseException? current = Current;
        if (
            current is not null
            && !ReferenceEquals(current, exception)
            && exception.Context is null
        )
        {
            // 显式 Cause 与隐式 Context 可以是同一实例；显示时优先 Cause，不应删掉可读的 Context。
            exception.Context = current;
            exception.ForeignObject?.SynchronizeException(exception, write: true);
        }
        return exception;
    }

    /// <summary>进入 except 体；内部传 None 时只取得 finally 清理所需的上下文快照。</summary>
    /// <param name="exception">捕获到的异常；null 或 PyNone 表示只读取 token，不修改状态。</param>
    /// <returns>进入之前的异常上下文 token，退出时传回 <see cref="LeaveHandler"/>。</returns>
    public static object? EnterHandler(object? exception)
    {
        if (exception is null or PyNone) return _current;
        if (exception is not PyBaseException caught)
        {
            throw new PyTypeError("exceptions must derive from BaseException");
        }
        PyExceptionContext? previous = _current;
        _current = new PyExceptionContext(caught, previous);
        return previous;
    }

    /// <summary>离开 <c>except</c> 体：恢复上一层正在处理的异常。</summary>
    /// <param name="previous"><see cref="EnterHandler"/> 的返回值。</param>
    public static void LeaveHandler(object? previous) => _current = previous as PyExceptionContext;

    /// <summary>裸 <c>raise</c>：重抛当前异常；没有则抛 <c>RuntimeError</c>。</summary>
    public static object Reraise() =>
        Current ?? throw new PyRuntimeError("No active exception to reraise");

    /// <summary>
    /// 抛出前的准备：记录 traceback 栈帧、挂上隐式上下文，返回同一个异常对象。
    /// </summary>
    /// <param name="exception">即将抛出的异常。</param>
    public static object PrepareRaise(object exception)
    {
        PyBaseException ex = NormalizeRaise(exception);
        PyTraceback.Attach(ex);
        return Prepare(ex);
    }

    // 归一化不附Frames，供隔离测试在旧Attach之前拒绝历史混用；默认顺序保持不变。
    internal static PyBaseException NormalizeRaise(object exception)
    {
        // `raise SomeClass` / `raise SomeClass(...)`：类对象先实例化（无参构造）。
        switch (exception)
        {
            case PyClass cls:
                exception = cls.Invoke([], [], [])
                    ?? throw new PyTypeError("exceptions must derive from BaseException");
                break;
            case PyType { Name: "error" }:
                exception = PyRegex.ConstructError([], [], [])!;
                break;
            case PyType { Name: "GetoptError" }:
                exception = PyGetoptError.Construct([], [], []);
                break;
            case PyType { Name: "JSONDecodeError" }:
                exception = PyJsonModule.ConstructError([], [], []);
                break;
            case PyType type when PyOptParseExceptions.IsType(type.Name):
                exception = PyOptParseExceptions.Construct(type.Name, [], [], []);
                break;
            case PyType type when PyExceptionHierarchy.IsKnown(type.Name):
                exception = PyExceptionTypes.Create(type.Name, string.Empty);
                break;
        }
        if (exception is not PyBaseException ex)
        {
            throw new PyTypeError("exceptions must derive from BaseException");
        }
        ex.ForeignObject?.SynchronizeException(ex, write: false);
        return ex;
    }

    /// <summary>按名字构造异常；<paramref name="arg"/> 为 <c>None</c> 时消息为空。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="arg">消息参数。</param>
    public static PyBaseException Make(string typeName, object? arg)
    {
        // StopIteration 的首参是终值对象，不能先 str() 再丢失身份或提前执行用户代码。
        // 当前单参 ABI 尚不区分无参和显式 None；二者沿用内部耗尽的空 args 约定。
        if (PyOptParseExceptions.IsType(typeName)) return PyOptParseExceptions.Construct(typeName, arg is null ? [] : [arg], [], []);
        if (typeName == "GetoptError") return PyGetoptError.Construct(arg is null ? [] : [arg], [], []);
        if (typeName == "StopIteration") return new PyStopIteration(arg);
        if (typeName == "SystemExit") return new PySystemExit(arg);
        if (PyExceptionHierarchy.IsSubclass(typeName, "Warning"))
            return PyWarningTypes.Create(typeName, arg is null ? [] : [arg]);
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

    /// <summary>
    /// 用户异常的 <c>str()</c> 内容：`args` 按 Python 规则拼接。
    /// </summary>
    /// <remarks>
    /// 一个参数直接输出它的 <c>str</c>；多个参数输出 <c>(a, b)</c> 形式；
    /// 没有参数时为空。生成代码的异常构造函数从这里取消息。
    /// </remarks>
    public static string UserExceptionMessageForBase(string builtinBase, object?[] args) =>
        PyExceptionHierarchy.IsSubclass(builtinBase, "Warning") || builtinBase is "StopIteration" or "UnsupportedOperation" or "io.UnsupportedOperation" or "BadZipFile" or "zipfile.BadZipFile" ? "" : UserExceptionMessage(args);

    /// <summary>按参数生成显式异常消息；警告仅在真正格式化时调用。</summary>
    public static string UserExceptionMessage(object?[] args)
    {
        if (args.Length == 0)
        {
            return string.Empty;
        }
        if (args.Length == 1)
        {
            return PyRepr.Str(args[0]);
        }
        var parts = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            parts[i] = PyRepr.Repr(args[i]);
        }
        return "(" + string.Join(", ", parts) + ")";
    }

    /// <summary>读取可能未赋值的局部变量；哨兵值表示从未赋值。</summary>
    /// <param name="value">变量当前内容。</param>
    /// <param name="name">变量名，用于错误消息。</param>
    public static object Load(object? value, string name)
    {
        if (value is null or PyUnbound)
        {
            throw PyTraceProduct.NativeFresh(new PyUnboundLocalError(
                $"cannot access local variable '{name}' where it is not associated with a value"
            ));
        }
        return value;
    }

    /// <summary>读取可能未赋值的模块级名字（未赋值时与 CPython 一样抛 <c>NameError</c>）。</summary>
    /// <param name="value">槽位当前内容。</param>
    /// <param name="name">名字，用于错误消息。</param>
    public static object LoadName(object? value, string name)
    {
        if (value is null or PyUnbound)
        {
            // Ellipsis的模块槽由类型层保为Dynamic，未绑定时可返回真实内建单例。
            // 已绑定值（包括None）必须原样保留；其它内建回退仍明确限制。
            if (name == "Ellipsis") return PyEllipsis.Instance;
            PyOps.CheckGlobalBound(false, name);
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
