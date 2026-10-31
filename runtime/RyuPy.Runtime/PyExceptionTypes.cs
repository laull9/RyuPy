using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

// ─────────────────────────────────────────────────────────────────────────────
// Python 内建异常继承树（M2.2）
// ─────────────────────────────────────────────────────────────────────────────
//
// 为什么每个 Python 异常类型都要有一个对应的 CLR 类：
//
// 1. `try/except` 的匹配由 CIL 的类型化 catch 子句完成（`catch PyValueError`），
//    CLR 的异常捕获天然按「类型及其基类」匹配，正好对应 Python 的 `except` 语义。
//    因此 **CLR 继承树必须与 Python 继承树同构**：`except ArithmeticError` 要能捕获
//    `ZeroDivisionError`，那 `PyZeroDivisionError` 就必须真的派生自 `PyArithmeticError`。
// 2. 反过来，任何「CLR 里平级、Python 里是父子」的偷懒写法都会造成语义错误，
//    而且是那种只在少数错误路径上才暴露的错误。
//
// 类型名（`TypeError` 而不是 `PyTypeError`）保存在基类的 `PyTypeName` 字段里，
// 由每个类在构造时写死；对外呈现（traceback、`type(e).__name__`）只读这个字段，
// 不依赖 CLR 类型名。子类通过 `protected (typeName, message)` 构造器传递自己的名字。
//
// 层次与 CPython 3.12 的 `BaseException` 子树一致；未列出的类型在
// `PyExceptionTypes.Create` 里退化成 `PyException`（类型名仍正确，只是无法被专门的
// `except Foo` 精确捕获——那种情况在诊断里有记录）。

/// <summary>
/// Python 异常基类：承载 Python 侧的类型名、消息、<c>args</c>、显式原因（<c>__cause__</c>）、
/// 隐式上下文（<c>__context__</c>）与 traceback 位置列表。
/// </summary>
/// <remarks>
/// 与 CLR 的关系：继承 <see cref="Exception"/>，因此可以被 CIL 的 catch 子句捕获，
/// 也能通过 <c>PyBaseException</c> 一次性兜住所有 Python 异常。
/// <see cref="Exception.Message"/> 只放 Python 消息本身，不拼类型前缀——
/// <c>TypeError: message</c> 的渲染属于 traceback 的职责。
/// </remarks>
public class PyBaseException : Exception
{
    /// <summary>Python 异常类名，例如 <c>TypeError</c>。</summary>
    public string PyTypeName { get; }

    /// <summary>Python 侧消息（<c>str(exc)</c> 的内容）。</summary>
    public string PyMessage { get; }

    /// <summary><c>exc.args</c>：构造时的位置参数。</summary>
    public object?[] Args { get; }

    /// <summary><c>raise ... from ...</c> 的显式原因（<c>__cause__</c>）。</summary>
    public PyBaseException? Cause { get; private set; }

    /// <summary>
    /// 隐式上下文（<c>__context__</c>）：在 <c>except</c> 处理期间抛出新的异常时，
    /// 新异常自动挂上前一个异常。与 <see cref="Cause"/> 的区别在于它是自动建立的。
    /// </summary>
    public PyBaseException? Context { get; set; }

    /// <summary>抛出点的栈帧快照，最内层在前；由 <see cref="PyTraceback"/> 在抛出时填写。</summary>
    public PyStackFrame[] Frames { get; internal set; } = [];

    /// <summary>用 Python 类型名与消息构造。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数；为 null 时按消息推断。</param>
    /// <param name="cause">显式原因。</param>
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

    /// <summary>
    /// 返回挂上显式原因的副本。
    /// </summary>
    /// <remarks>
    /// 必须用 <see cref="object.MemberwiseClone"/> 而不是「按类型名重新构造」：
    /// CLR 的 <c>catch PyRuntimeError</c> 依赖**运行时类型**，如果这里造出一个基类实例，
    /// Python 侧类型名看起来正确，但 `except RuntimeError` 永远捕获不到——
    /// 这种错误在 traceback 里完全看不出来。
    /// </remarks>
    /// <param name="cause">显式原因。</param>
    public PyBaseException WithCause(PyBaseException? cause)
    {
        var copy = (PyBaseException)MemberwiseClone();
        copy.Cause = cause;
        return copy;
    }

    /// <summary>按 <c>TypeName: message</c> 渲染，供 traceback 与错误退出使用。</summary>
    public string Render() => PyMessage.Length == 0 ? PyTypeName : $"{PyTypeName}: {PyMessage}";
}

/// <summary><c>SystemExit</c>：解释器退出请求。</summary>
public class PySystemExit : PyBaseException
{
    /// <summary>退出码；<c>None</c> 表示 0，字符串表示打印后退出 1。</summary>
    public object? Code { get; }

    /// <summary>构造 <c>SystemExit</c>。</summary>
    /// <param name="code">退出码或消息。</param>
    public PySystemExit(object? code = null)
        : base("SystemExit", code is null ? "" : PyRepr.Str(code))
    {
        Code = code;
    }
}

/// <summary><c>KeyboardInterrupt</c>。</summary>
public class PyKeyboardInterrupt : PyBaseException
{
    /// <summary>构造 <c>KeyboardInterrupt</c>。</summary>
    /// <param name="message">消息。</param>
    public PyKeyboardInterrupt(string message = "")
        : base("KeyboardInterrupt", message) { }
}

/// <summary><c>GeneratorExit</c>。</summary>
public class PyGeneratorExit : PyBaseException
{
    /// <summary>构造 <c>GeneratorExit</c>。</summary>
    public PyGeneratorExit()
        : base("GeneratorExit", "") { }
}

/// <summary>Python <c>Exception</c>：所有常规异常的共同基类。</summary>
public class PyException : PyBaseException
{
    /// <summary>用 Python 类型名构造 <c>Exception</c> 家族的异常。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    /// <param name="cause">显式原因。</param>
    public PyException(
        string typeName,
        string message,
        object?[]? args = null,
        PyBaseException? cause = null
    )
        : base(typeName, message, args, cause) { }
}

/// <summary><c>ArithmeticError</c>。</summary>
public class PyArithmeticError : PyException
{
    /// <summary>构造 <c>ArithmeticError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyArithmeticError(string message)
        : base("ArithmeticError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyArithmeticError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>ZeroDivisionError</c>（<c>ArithmeticError</c> 子类）。</summary>
public sealed class PyZeroDivisionError : PyArithmeticError
{
    /// <summary>构造 <c>ZeroDivisionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyZeroDivisionError(string message)
        : base("ZeroDivisionError", message) { }
}

/// <summary><c>OverflowError</c>（<c>ArithmeticError</c> 子类）。</summary>
public sealed class PyOverflowError : PyArithmeticError
{
    /// <summary>构造 <c>OverflowError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyOverflowError(string message)
        : base("OverflowError", message) { }
}

/// <summary><c>LookupError</c>：索引与键查找错误。</summary>
public class PyLookupError : PyException
{
    /// <summary>构造 <c>LookupError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyLookupError(string message)
        : base("LookupError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyLookupError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>IndexError</c>。</summary>
public sealed class PyIndexError : PyLookupError
{
    /// <summary>构造 <c>IndexError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyIndexError(string message)
        : base("IndexError", message) { }
}

/// <summary><c>KeyError</c>。</summary>
public sealed class PyKeyError : PyLookupError
{
    /// <summary>构造 <c>KeyError</c>。</summary>
    /// <param name="message">消息（<c>repr(key)</c>）。</param>
    public PyKeyError(string message)
        : base("KeyError", message) { }
}

/// <summary><c>ValueError</c>。</summary>
public class PyValueError : PyException
{
    /// <summary>构造 <c>ValueError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyValueError(string message)
        : base("ValueError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyValueError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>UnicodeError</c>（<c>ValueError</c> 子类）。</summary>
public class PyUnicodeError : PyValueError
{
    /// <summary>构造 <c>UnicodeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnicodeError(string message)
        : base("UnicodeError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyUnicodeError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>UnicodeDecodeError</c>。</summary>
public sealed class PyUnicodeDecodeError : PyUnicodeError
{
    /// <summary>构造 <c>UnicodeDecodeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnicodeDecodeError(string message)
        : base("UnicodeDecodeError", message) { }
}

/// <summary><c>UnicodeEncodeError</c>。</summary>
public sealed class PyUnicodeEncodeError : PyUnicodeError
{
    /// <summary>构造 <c>UnicodeEncodeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnicodeEncodeError(string message)
        : base("UnicodeEncodeError", message) { }
}

/// <summary><c>TypeError</c>。</summary>
public class PyTypeError : PyException
{
    /// <summary>构造 <c>TypeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyTypeError(string message)
        : base("TypeError", message) { }
}

/// <summary><c>NameError</c>。</summary>
public class PyNameError : PyException
{
    /// <summary>构造 <c>NameError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyNameError(string message)
        : base("NameError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyNameError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>UnboundLocalError</c>：读取未赋值的局部变量。</summary>
public sealed class PyUnboundLocalError : PyNameError
{
    /// <summary>构造 <c>UnboundLocalError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnboundLocalError(string message)
        : base("UnboundLocalError", message) { }
}

/// <summary><c>AttributeError</c>。</summary>
public class PyAttributeError : PyException
{
    /// <summary>构造 <c>AttributeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyAttributeError(string message)
        : base("AttributeError", message) { }
}

/// <summary><c>RuntimeError</c>。</summary>
public class PyRuntimeError : PyException
{
    /// <summary>构造 <c>RuntimeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyRuntimeError(string message)
        : base("RuntimeError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyRuntimeError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>NotImplementedError</c>。</summary>
public sealed class PyNotImplementedError : PyRuntimeError
{
    /// <summary>构造 <c>NotImplementedError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyNotImplementedError(string message)
        : base("NotImplementedError", message) { }
}

/// <summary><c>RecursionError</c>。</summary>
public sealed class PyRecursionError : PyRuntimeError
{
    /// <summary>构造 <c>RecursionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyRecursionError(string message)
        : base("RecursionError", message) { }
}

/// <summary><c>AssertionError</c>。</summary>
public class PyAssertionError : PyException
{
    /// <summary>构造 <c>AssertionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyAssertionError(string message)
        : base("AssertionError", message) { }
}

/// <summary><c>ImportError</c>。</summary>
public class PyImportError : PyException
{
    /// <summary>构造 <c>ImportError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyImportError(string message)
        : base("ImportError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyImportError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>ModuleNotFoundError</c>。</summary>
public sealed class PyModuleNotFoundError : PyImportError
{
    /// <summary>构造 <c>ModuleNotFoundError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyModuleNotFoundError(string message)
        : base("ModuleNotFoundError", message) { }
}

/// <summary><c>OSError</c>：文件与系统调用错误。</summary>
public class PyOSError : PyException
{
    /// <summary>构造 <c>OSError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyOSError(string message)
        : base("OSError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyOSError(string typeName, string message)
        : base(typeName, message) { }
}

/// <summary><c>FileNotFoundError</c>。</summary>
public sealed class PyFileNotFoundError : PyOSError
{
    /// <summary>构造 <c>FileNotFoundError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyFileNotFoundError(string message)
        : base("FileNotFoundError", message) { }
}

/// <summary><c>FileExistsError</c>。</summary>
public sealed class PyFileExistsError : PyOSError
{
    /// <summary>构造 <c>FileExistsError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyFileExistsError(string message)
        : base("FileExistsError", message) { }
}

/// <summary><c>PermissionError</c>。</summary>
public sealed class PyPermissionError : PyOSError
{
    /// <summary>构造 <c>PermissionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyPermissionError(string message)
        : base("PermissionError", message) { }
}

/// <summary><c>StopIteration</c>：迭代器耗尽的信号，保留 <c>value</c>（M2.4 使用）。</summary>
public class PyStopIteration : PyException
{
    /// <summary><c>StopIteration.value</c>。</summary>
    public object? Value { get; }

    /// <summary>构造 <c>StopIteration</c>，并保留返回值。</summary>
    /// <param name="value">返回值。</param>
    public PyStopIteration(object? value = null)
        : base("StopIteration", value is null ? "" : PyRepr.Str(value))
    {
        Value = value;
    }
}

/// <summary><c>StopAsyncIteration</c>。</summary>
public sealed class PyStopAsyncIteration : PyException
{
    /// <summary>构造 <c>StopAsyncIteration</c>。</summary>
    public PyStopAsyncIteration()
        : base("StopAsyncIteration", "") { }
}

/// <summary><c>EOFError</c>。</summary>
public sealed class PyEOFError : PyException
{
    /// <summary>构造 <c>EOFError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyEOFError(string message)
        : base("EOFError", message) { }
}

/// <summary>traceback 中的一帧：文件、函数名与行号。</summary>
/// <param name="File">源文件名。</param>
/// <param name="Function">函数名（模块顶层为 <c>&lt;module&gt;</c>）。</param>
/// <param name="Line">当前行号；0 表示未知。</param>
public readonly record struct PyStackFrame(string File, string Function, int Line);

/// <summary>异常类型注册表：按 Python 类型名构造对应的 CLR 异常对象。</summary>
/// <remarks>
/// 这里集中维护「Python 类型名 → CLR 类」的映射。CIL 的类型化 catch 依赖 CLR 类，
/// 所以新增可捕获的异常类型必须同时满足两件事：加一个类、在这张表里登记。
/// 编译期 <c>runtime/RyuPy.Runtime</c> 与 codegen 的引用表也按这张表同步。
/// </remarks>
public static class PyExceptionTypes
{
    private static readonly Dictionary<string, Func<string, PyBaseException>> Factories = new()
    {
        ["BaseException"] = m => new PyBaseException("BaseException", m),
        ["Exception"] = m => new PyException("Exception", m),
        ["SystemExit"] = m => new PySystemExit(m),
        ["KeyboardInterrupt"] = m => new PyKeyboardInterrupt(m),
        ["ArithmeticError"] = m => new PyArithmeticError(m),
        ["ZeroDivisionError"] = m => new PyZeroDivisionError(m),
        ["OverflowError"] = m => new PyOverflowError(m),
        ["LookupError"] = m => new PyLookupError(m),
        ["IndexError"] = m => new PyIndexError(m),
        ["KeyError"] = m => new PyKeyError(m),
        ["ValueError"] = m => new PyValueError(m),
        ["UnicodeError"] = m => new PyUnicodeError(m),
        ["UnicodeDecodeError"] = m => new PyUnicodeDecodeError(m),
        ["UnicodeEncodeError"] = m => new PyUnicodeEncodeError(m),
        ["TypeError"] = m => new PyTypeError(m),
        ["NameError"] = m => new PyNameError(m),
        ["UnboundLocalError"] = m => new PyUnboundLocalError(m),
        ["AttributeError"] = m => new PyAttributeError(m),
        ["RuntimeError"] = m => new PyRuntimeError(m),
        ["NotImplementedError"] = m => new PyNotImplementedError(m),
        ["RecursionError"] = m => new PyRecursionError(m),
        ["AssertionError"] = m => new PyAssertionError(m),
        ["ImportError"] = m => new PyImportError(m),
        ["ModuleNotFoundError"] = m => new PyModuleNotFoundError(m),
        ["OSError"] = m => new PyOSError(m),
        ["FileNotFoundError"] = m => new PyFileNotFoundError(m),
        ["FileExistsError"] = m => new PyFileExistsError(m),
        ["PermissionError"] = m => new PyPermissionError(m),
        ["StopIteration"] = _ => new PyStopIteration(null),
        ["StopAsyncIteration"] = _ => new PyStopAsyncIteration(),
        ["EOFError"] = m => new PyEOFError(m),
    };

    /// <summary>该名字是否是已知的 Python 异常类型（供编译期诊断使用）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    public static bool IsKnown(string typeName) => Factories.ContainsKey(typeName);

    /// <summary>按类型名构造异常对象；未知名字退化成 <c>Exception</c> 家族。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    public static PyBaseException Create(string typeName, string message) =>
        Factories.TryGetValue(typeName, out var factory)
            ? factory(message)
            : new PyException(typeName, message);
}
