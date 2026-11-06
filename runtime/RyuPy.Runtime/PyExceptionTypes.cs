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
//    通常 CLR 主继承树与 Python 单继承一致：`except ArithmeticError` 要能捕获
//    `ZeroDivisionError`，那 `PyZeroDivisionError` 就必须真的派生自 `PyArithmeticError`。
//    io.UnsupportedOperation 的额外 ValueError 父边由 MIR 在每个 ValueError handler 原位置展开额外 catch；不能只改 isinstance。
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

/// <summary><c>SystemExit</c>：解释器退出请求。</summary>
public class PySystemExit : PyBaseException
{
    /// <summary>退出码；<c>None</c> 表示 0，字符串表示打印后退出 1。</summary>
    public object? Code { get; }

    /// <summary>构造 <c>SystemExit</c>。</summary>
    /// <param name="code">退出码或消息。</param>
    public PySystemExit(object? code = null)
        : base("SystemExit", "", code is null ? [] : [code])
    {
        Code = code;
    }

    /// <summary>异常消息只在显式格式化时读取，构造不调用用户str。</summary>
    public override string PyMessage => Code is null ? "" : PyRepr.Str(Code);
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
    /// <summary>构造无参数 <c>GeneratorExit</c>，保留已有无参 ABI。</summary>
    public PyGeneratorExit() : this("") { }

    /// <summary>构造带消息的 <c>GeneratorExit</c>。</summary>
    /// <param name="message">异常消息。</param>
    public PyGeneratorExit(string message)
        : base("GeneratorExit", message) { }

    /// <summary>供用户异常子类保留类型名、消息和完整参数。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyGeneratorExit(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
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

    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyArithmeticError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>ZeroDivisionError</c>（<c>ArithmeticError</c> 子类）。</summary>
public class PyZeroDivisionError : PyArithmeticError
{
    /// <summary>构造 <c>ZeroDivisionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyZeroDivisionError(string message)
        : base("ZeroDivisionError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyZeroDivisionError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>OverflowError</c>（<c>ArithmeticError</c> 子类）。</summary>
public class PyOverflowError : PyArithmeticError
{
    /// <summary>构造 <c>OverflowError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyOverflowError(string message)
        : base("OverflowError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyOverflowError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

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

    /// <summary>供子类指定类型名与位置参数。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyLookupError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>IndexError</c>。</summary>
public class PyIndexError : PyLookupError
{
    /// <summary>构造 <c>IndexError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyIndexError(string message)
        : base("IndexError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyIndexError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>KeyError</c>。</summary>
public class PyKeyError : PyLookupError
{
    /// <summary>构造 <c>KeyError</c>。</summary>
    /// <param name="message">消息（<c>repr(key)</c>）。</param>
    public PyKeyError(string message)
        : base("KeyError", message) { }

    private PyKeyError(string message, object?[] args)
        : base("KeyError", message, args) { }

    /// <summary>
    /// 由缺失的键构造：<c>args</c> 保存**键本身**而不是消息字符串。
    /// </summary>
    /// <remarks>
    /// CPython 的 <c>KeyError</c> 把键放进 <c>args[0]</c>，
    /// 所以 <c>print(error.args)</c> 输出 <c>('missing',)</c>——
    /// 直接拿消息字符串当参数会变成 <c>("'missing'",)</c>，把引号也包进去。
    /// </remarks>
    /// <param name="key">缺失的键。</param>
    public static PyKeyError ForKey(object? key) =>
        new(PyRepr.Repr(key), [key ?? PyNone.Instance]);
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyKeyError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

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

    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyValueError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>TypeError</c>。</summary>
public class PyTypeError : PyException
{
    /// <summary>构造 <c>TypeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyTypeError(string message)
        : base("TypeError", message) { }

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyTypeError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
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

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyNameError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>UnboundLocalError</c>：读取未赋值的局部变量。</summary>
public class PyUnboundLocalError : PyNameError
{
    /// <summary>构造 <c>UnboundLocalError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnboundLocalError(string message)
        : base("UnboundLocalError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyUnboundLocalError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>AttributeError</c>。</summary>
public class PyAttributeError : PyException
{
    /// <summary>构造 <c>AttributeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyAttributeError(string message)
        : base("AttributeError", message) { }

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyAttributeError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
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

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyRuntimeError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>NotImplementedError</c>。</summary>
public class PyNotImplementedError : PyRuntimeError
{
    /// <summary>构造 <c>NotImplementedError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyNotImplementedError(string message)
        : base("NotImplementedError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyNotImplementedError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>RecursionError</c>。</summary>
public class PyRecursionError : PyRuntimeError
{
    /// <summary>构造 <c>RecursionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyRecursionError(string message)
        : base("RecursionError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyRecursionError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>AssertionError</c>。</summary>
public class PyAssertionError : PyException
{
    /// <summary>构造 <c>AssertionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyAssertionError(string message)
        : base("AssertionError", message) { }

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyAssertionError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
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

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyImportError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>ModuleNotFoundError</c>。</summary>
public class PyModuleNotFoundError : PyImportError
{
    /// <summary>构造 <c>ModuleNotFoundError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyModuleNotFoundError(string message)
        : base("ModuleNotFoundError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyModuleNotFoundError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

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

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyOSError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>FileNotFoundError</c>。</summary>
public class PyFileNotFoundError : PyOSError
{
    /// <summary>构造 <c>FileNotFoundError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyFileNotFoundError(string message)
        : base("FileNotFoundError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyFileNotFoundError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>FileExistsError</c>。</summary>
public class PyFileExistsError : PyOSError
{
    /// <summary>构造 <c>FileExistsError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyFileExistsError(string message)
        : base("FileExistsError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyFileExistsError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>PermissionError</c>。</summary>
public class PyPermissionError : PyOSError
{
    /// <summary>构造 <c>PermissionError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyPermissionError(string message)
        : base("PermissionError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyPermissionError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>StopIteration</c>：迭代器耗尽的信号，保留 <c>value</c>（M2.4 使用）。</summary>
public class PyStopIteration : PyException
{
    /// <summary><c>StopIteration.value</c>。</summary>
    public object? Value { get; }

    /// <summary>仅在请求消息时按完整 args 格式化；完成生成器不可调用返回值的 __str__。</summary>
    public override string PyMessage => PyErrors.UserExceptionMessage(Args);

    /// <summary>CLR 消息读取同样使用延迟格式化，不缓存可变返回值。</summary>
    public override string Message => PyMessage;

    /// <summary>构造 <c>StopIteration</c>，并保留返回值。</summary>
    /// <param name="value">返回值。</param>
    public PyStopIteration(object? value = null)
        : base("StopIteration", "", value is null or PyNone ? [] : [value])
    {
        // 内部迭代完成使用此构造；None 代表无终值，tuple 等对象仍是一个参数。
        Value = value is PyNone ? null : value;
    }

    /// <summary>供生成的用户子类保留真实名字与全部参数，不提前格式化。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">保留派生构造 ABI；消息由原始参数延迟生成。</param>
    /// <param name="args">位置参数；独占浅拷贝，元素身份不变。</param>
    protected PyStopIteration(string typeName, string message, object?[] args)
        : base(typeName, "", (object?[])args.Clone())
        => Value = Args.Length == 0 ? null : Args[0];
}

/// <summary><c>StopAsyncIteration</c>。</summary>
public class PyStopAsyncIteration : PyException
{
    /// <summary>构造 <c>StopAsyncIteration</c>。</summary>
    public PyStopAsyncIteration()
        : base("StopAsyncIteration", "") { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyStopAsyncIteration(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>EOFError</c>。</summary>
public class PyEOFError : PyException
{
    /// <summary>构造 <c>EOFError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyEOFError(string message)
        : base("EOFError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyEOFError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

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
        ["BadZipFile"] = m => m.Length == 0 ? new PyBadZipFile() : new PyBadZipFile(m),
        ["SystemExit"] = m => new PySystemExit(m),
        ["KeyboardInterrupt"] = m => new PyKeyboardInterrupt(m),
        ["GeneratorExit"] = m => new PyGeneratorExit(m),
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
        ["FrozenInstanceError"] = m => new PyFrozenInstanceError(m),
        ["RuntimeError"] = m => new PyRuntimeError(m),
        ["NotImplementedError"] = m => new PyNotImplementedError(m),
        ["RecursionError"] = m => new PyRecursionError(m),
        ["AssertionError"] = m => new PyAssertionError(m),
        ["ImportError"] = m => new PyImportError(m),
        ["ModuleNotFoundError"] = m => new PyModuleNotFoundError(m),
        ["OSError"] = m => new PyOSError(m),
        ["UnsupportedOperation"] = m => m.Length == 0 ? new PyUnsupportedOperation() : new PyUnsupportedOperation(m),
        ["IsADirectoryError"] = m => new PyIsADirectoryError(m),
        ["NotADirectoryError"] = m => new PyNotADirectoryError(m),
        ["FileNotFoundError"] = m => new PyFileNotFoundError(m),
        ["FileExistsError"] = m => new PyFileExistsError(m),
        ["PermissionError"] = m => new PyPermissionError(m),
        ["StopIteration"] = _ => new PyStopIteration(null),
        ["StopAsyncIteration"] = _ => new PyStopAsyncIteration(),
        ["EOFError"] = m => new PyEOFError(m),
        ["BufferError"] = m => new PyBufferError(m),
    };

    /// <summary>该名字是否是已知的 Python 异常类型（供编译期诊断使用）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    public static bool IsKnown(string typeName) => typeName == "GetoptError" || PyOptParseExceptions.IsType(typeName) || Factories.ContainsKey(typeName)
        || PyExceptionHierarchy.IsSubclass(typeName, "Warning");

    /// <summary>按类型名构造异常对象；未知名字退化成 <c>Exception</c> 家族。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    public static PyBaseException Create(string typeName, string message) =>
        typeName == "GetoptError" ? new PyGetoptError(message, "") :
        PyOptParseExceptions.IsType(typeName) ? PyOptParseExceptions.FromMessage(typeName, message) :
        PyExceptionHierarchy.IsSubclass(typeName, "Warning") ? PyWarningTypes.Create(typeName, [message]) :
        Factories.TryGetValue(typeName, out var factory)
            ? factory(message)
            : new PyException(typeName, message);
}
