// 动态except专用冷表：驻留类对象映射真实CLR族，不用实例显示名推断类型。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static partial class PyExceptionMatch
{
    private sealed record NativeTarget(string Name, Func<PyBaseException, bool> Accepts);

    // 显式静态构造器阻止beforefieldinit提前初始化；只由异常分派首次调用触发。
    static PyExceptionMatch() { }

    private static readonly Dictionary<PyType, NativeTarget> NativeTargets = new(ReferenceEqualityComparer.Instance)
    {
        [PyType.Get("BaseException")] = new("BaseException", static error => error is PyBaseException),
        [PyType.Get("Exception")] = new("Exception", static error => error is PyException),
        [PyType.Get("Warning")] = new("Warning", static error => error is PyWarning),
        [PyType.Get("UserWarning")] = new("UserWarning", static error => error is PyUserWarning),
        [PyType.Get("DeprecationWarning")] = new("DeprecationWarning", static error => error is PyDeprecationWarning),
        [PyType.Get("PendingDeprecationWarning")] = new("PendingDeprecationWarning", static error => error is PyPendingDeprecationWarning),
        [PyType.Get("RuntimeWarning")] = new("RuntimeWarning", static error => error is PyRuntimeWarning),
        [PyType.Get("SyntaxWarning")] = new("SyntaxWarning", static error => error is PySyntaxWarning),
        [PyType.Get("FutureWarning")] = new("FutureWarning", static error => error is PyFutureWarning),
        [PyType.Get("ImportWarning")] = new("ImportWarning", static error => error is PyImportWarning),
        [PyType.Get("UnicodeWarning")] = new("UnicodeWarning", static error => error is PyUnicodeWarning),
        [PyType.Get("BytesWarning")] = new("BytesWarning", static error => error is PyBytesWarning),
        [PyType.Get("ResourceWarning")] = new("ResourceWarning", static error => error is PyResourceWarning),
        [PyType.Get("SystemExit")] = new("SystemExit", static error => error is PySystemExit),
        [PyType.Get("KeyboardInterrupt")] = new("KeyboardInterrupt", static error => error is PyKeyboardInterrupt),
        [PyType.Get("GeneratorExit")] = new("GeneratorExit", static error => error is PyGeneratorExit),
        [PyType.Get("ArithmeticError")] = new("ArithmeticError", static error => error is PyArithmeticError),
        [PyType.Get("ZeroDivisionError")] = new("ZeroDivisionError", static error => error is PyZeroDivisionError),
        [PyType.Get("OverflowError")] = new("OverflowError", static error => error is PyOverflowError),
        [PyType.Get("LookupError")] = new("LookupError", static error => error is PyLookupError),
        [PyType.Get("IndexError")] = new("IndexError", static error => error is PyIndexError),
        [PyType.Get("KeyError")] = new("KeyError", static error => error is PyKeyError),
        [PyType.Get("ValueError")] = new("ValueError", static error => error is PyValueError or PyUnsupportedOperation),
        [PyType.Get("JSONDecodeError")] = new("JSONDecodeError", static error => error is PyJSONDecodeError),
        [PyType.Get("error")] = new("error", static error => error is PyRegexError),
        [PyType.Get("BadZipFile")] = new("BadZipFile", static error => error is PyBadZipFile),
        [PyType.Get("GetoptError")] = new("GetoptError", static error => error is PyGetoptError),
        [PyType.Get("OptParseError")] = new("OptParseError", static error => error is PyOptParseError),
        [PyType.Get("OptionError")] = new("OptionError", static error => error is PyOptionError),
        [PyType.Get("OptionConflictError")] = new("OptionConflictError", static error => error is PyOptionConflictError),
        [PyType.Get("OptionValueError")] = new("OptionValueError", static error => error is PyOptionValueError),
        [PyType.Get("BadOptionError")] = new("BadOptionError", static error => error is PyBadOptionError),
        [PyType.Get("AmbiguousOptionError")] = new("AmbiguousOptionError", static error => error is PyAmbiguousOptionError),
        [PyType.Get("UnicodeError")] = new("UnicodeError", static error => error is PyUnicodeError),
        [PyType.Get("UnicodeDecodeError")] = new("UnicodeDecodeError", static error => error is PyUnicodeDecodeError),
        [PyType.Get("UnicodeEncodeError")] = new("UnicodeEncodeError", static error => error is PyUnicodeEncodeError),
        [PyType.Get("TypeError")] = new("TypeError", static error => error is PyTypeError),
        [PyType.Get("NameError")] = new("NameError", static error => error is PyNameError),
        [PyType.Get("UnboundLocalError")] = new("UnboundLocalError", static error => error is PyUnboundLocalError),
        [PyType.Get("AttributeError")] = new("AttributeError", static error => error is PyAttributeError),
        [PyType.Get("FrozenInstanceError")] = new("FrozenInstanceError", static error => error is PyFrozenInstanceError),
        [PyType.Get("RuntimeError")] = new("RuntimeError", static error => error is PyRuntimeError),
        [PyType.Get("NotImplementedError")] = new("NotImplementedError", static error => error is PyNotImplementedError),
        [PyType.Get("RecursionError")] = new("RecursionError", static error => error is PyRecursionError),
        [PyType.Get("AssertionError")] = new("AssertionError", static error => error is PyAssertionError),
        [PyType.Get("ImportError")] = new("ImportError", static error => error is PyImportError),
        [PyType.Get("ModuleNotFoundError")] = new("ModuleNotFoundError", static error => error is PyModuleNotFoundError),
        [PyType.Get("OSError")] = new("OSError", static error => error is PyOSError),
        [PyType.Get("UnsupportedOperation")] = new("UnsupportedOperation", static error => error is PyUnsupportedOperation),
        [PyType.Get("IsADirectoryError")] = new("IsADirectoryError", static error => error is PyIsADirectoryError),
        [PyType.Get("NotADirectoryError")] = new("NotADirectoryError", static error => error is PyNotADirectoryError),
        [PyType.Get("FileNotFoundError")] = new("FileNotFoundError", static error => error is PyFileNotFoundError),
        [PyType.Get("FileExistsError")] = new("FileExistsError", static error => error is PyFileExistsError),
        [PyType.Get("PermissionError")] = new("PermissionError", static error => error is PyPermissionError),
        [PyType.Get("StopIteration")] = new("StopIteration", static error => error is PyStopIteration),
        [PyType.Get("StopAsyncIteration")] = new("StopAsyncIteration", static error => error is PyStopAsyncIteration),
        [PyType.Get("EOFError")] = new("EOFError", static error => error is PyEOFError),
        [PyType.Get("BufferError")] = new("BufferError", static error => error is PyBufferError),
    };
}
