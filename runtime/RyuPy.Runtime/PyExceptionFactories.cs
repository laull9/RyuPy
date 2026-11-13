// 异常类型到真实CLR构造器的有限注册表；与异常类定义分离，不改变旧公开签名。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

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
        ["zlib.error"] = m => new PyZlibError(m),
        ["BadGzipFile"] = m => new PyBadGzipFile(m),
        ["MemoryError"] = m => new PyMemoryError(m),
        ["SystemExit"] = m => new PySystemExit(m),
        ["KeyboardInterrupt"] = m => new PyKeyboardInterrupt(m),
        ["GeneratorExit"] = m => new PyGeneratorExit(m),
        ["ArithmeticError"] = m => new PyArithmeticError(m),
        ["ZeroDivisionError"] = m => new PyZeroDivisionError(m),
        ["OverflowError"] = m => new PyOverflowError(m),
        ["LookupError"] = m => new PyLookupError(m),
        ["IndexError"] = m => new PyIndexError(m),
        ["KeyError"] = m => new PyKeyError(m),
        ["SyntaxError"] = m => m.Length == 0 ? PySyntaxError.Construct("SyntaxError", [], []) : new PySyntaxError(m),
        ["ParseError"] = m => m.Length == 0 ? PySyntaxError.Construct("ParseError", [], []) : new PyXmlParseError(m),
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
