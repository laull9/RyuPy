using System;

namespace RyuPy.Runtime;

/// <summary>
/// Python 异常基类（M2.1 起的运行时错误模型）。
/// </summary>
/// <remarks>
/// 与 CLR 异常的关系：
/// <list type="bullet">
/// <item>Python 侧可见的类型名单独保存为 <see cref="PyTypeName"/>，不依赖 CLR 类型名——
/// 编译产物的错误信息必须是 <c>TypeError</c>，而不是 <c>System.InvalidCastException</c>；</item>
/// <item><see cref="Exception.Message"/> 只放 Python 消息本身，不拼接类型前缀；
/// traceback 的格式化（<c>TypeError: message</c>）由 M2.2 的顶层处理器负责；</item>
/// <item><see cref="Cause"/> 对应 <c>raise ... from ...</c> 的显式异常链，
/// <see cref="System.Exception.InnerException"/> 同步设置，便于 .NET 工具查看。</item>
/// </list>
/// 继承关系刻意与 CPython 一致（<c>ValueError</c> 是 <c>Exception</c> 的子类），
/// 这样 M2.2 的 <c>except Exception</c> 可以直接映射成 CLR 的 <c>catch PyException</c>。
/// </remarks>
public class PyBaseException : Exception
{
    /// <summary>Python 异常类名，例如 <c>TypeError</c>。</summary>
    public string PyTypeName { get; }

    /// <summary>Python 侧的消息（<c>str(exc)</c> 的原始内容）。</summary>
    public string PyMessage { get; }

    /// <summary><c>exc.args</c>；空表示没有参数。</summary>
    public object?[] Args { get; }

    /// <summary><c>raise ... from ...</c> 的显式原因。</summary>
    public PyBaseException? Cause { get; }

    /// <summary>用 Python 类型名与消息构造。</summary>
    public PyBaseException(
        string typeName,
        string message,
        object?[]? args = null,
        PyBaseException? cause = null
    )
        : base(message, cause)
    {
        PyTypeName = typeName;
        PyMessage = message;
        Args = args ?? (message.Length == 0 ? [] : [message]);
        Cause = cause;
    }

    /// <summary>返回挂上显式原因后的副本（<see cref="Cause"/> 只能在构造时确定）。</summary>
    public PyBaseException WithCause(PyBaseException? cause) =>
        new(PyTypeName, PyMessage, Args, cause);

    /// <summary>按 <c>TypeName: message</c> 渲染，供 traceback 与错误退出使用。</summary>
    public string Render() => PyMessage.Length == 0 ? PyTypeName : $"{PyTypeName}: {PyMessage}";
}

/// <summary><c>SystemExit</c>：进程退出语义由 M2.2 处理，这里只保留类型与消息。</summary>
public sealed class PySystemExit : PyBaseException
{
    /// <summary>构造 <c>SystemExit</c>。</summary>
    public PySystemExit(string message = "") : base("SystemExit", message) { }
}

/// <summary><c>KeyboardInterrupt</c>。</summary>
public sealed class PyKeyboardInterrupt : PyBaseException
{
    /// <summary>构造 <c>KeyboardInterrupt</c>。</summary>
    public PyKeyboardInterrupt(string message = "") : base("KeyboardInterrupt", message) { }
}

/// <summary>Python <c>Exception</c>：所有常规异常的基类。</summary>
public class PyException : PyBaseException
{
    /// <summary>用一个 Python 类型名构造 <c>Exception</c> 家族的异常。</summary>
    public PyException(
        string typeName,
        string message,
        object?[]? args = null,
        PyBaseException? cause = null
    )
        : base(typeName, message, args, cause) { }
}

/// <summary><c>ValueError</c>。</summary>
public sealed class PyValueError : PyException
{
    /// <summary>构造 <c>ValueError</c>。</summary>
    public PyValueError(string message) : base("ValueError", message) { }
}

/// <summary><c>TypeError</c>。</summary>
public sealed class PyTypeError : PyException
{
    /// <summary>构造 <c>TypeError</c>。</summary>
    public PyTypeError(string message) : base("TypeError", message) { }
}

/// <summary><c>ZeroDivisionError</c>。</summary>
public sealed class PyZeroDivisionError : PyException
{
    /// <summary>构造 <c>ZeroDivisionError</c>。</summary>
    public PyZeroDivisionError(string message) : base("ZeroDivisionError", message) { }
}

/// <summary><c>OverflowError</c>。</summary>
public sealed class PyOverflowError : PyException
{
    /// <summary>构造 <c>OverflowError</c>。</summary>
    public PyOverflowError(string message) : base("OverflowError", message) { }
}

/// <summary><c>ArithmeticError</c>。</summary>
public sealed class PyArithmeticError : PyException
{
    /// <summary>构造 <c>ArithmeticError</c>。</summary>
    public PyArithmeticError(string message) : base("ArithmeticError", message) { }
}

/// <summary><c>AttributeError</c>。</summary>
public sealed class PyAttributeError : PyException
{
    /// <summary>构造 <c>AttributeError</c>。</summary>
    public PyAttributeError(string message) : base("AttributeError", message) { }
}

/// <summary><c>IndexError</c>。</summary>
public sealed class PyIndexError : PyException
{
    /// <summary>构造 <c>IndexError</c>。</summary>
    public PyIndexError(string message) : base("IndexError", message) { }
}

/// <summary><c>KeyError</c>。</summary>
public sealed class PyKeyError : PyException
{
    /// <summary>构造 <c>KeyError</c>。</summary>
    public PyKeyError(string message) : base("KeyError", message) { }
}

/// <summary><c>NameError</c>。</summary>
public sealed class PyNameError : PyException
{
    /// <summary>构造 <c>NameError</c>。</summary>
    public PyNameError(string message) : base("NameError", message) { }
}

/// <summary><c>UnboundLocalError</c>：读取未赋值的局部变量。</summary>
public sealed class PyUnboundLocalError : PyException
{
    /// <summary>构造 <c>UnboundLocalError</c>。</summary>
    public PyUnboundLocalError(string message) : base("UnboundLocalError", message) { }
}

/// <summary><c>RuntimeError</c>。</summary>
public sealed class PyRuntimeError : PyException
{
    /// <summary>构造 <c>RuntimeError</c>。</summary>
    public PyRuntimeError(string message) : base("RuntimeError", message) { }
}

/// <summary><c>StopIteration</c>：迭代器耗尽的信号，保留 <c>value</c>（M2.4 使用）。</summary>
public sealed class PyStopIteration : PyException
{
    /// <summary><c>StopIteration.value</c>。</summary>
    public object? Value { get; }

    /// <summary>构造 <c>StopIteration</c>，并保留返回值。</summary>
    public PyStopIteration(object? value = null)
        : base("StopIteration", value is null ? "" : PyRepr.Str(value))
    {
        Value = value;
    }
}
