// 原生及内建异常的 Python 父类表，与真实 CLR catch 继承同步。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>
/// 内建异常的 Python 继承关系（`isinstance(e, Exception)` 用）。
/// </summary>
/// <remarks>
/// CIL 的类型化 catch 使用 CLR 主继承树；UnsupportedOperation 的额外父边由 MIR 同步展开。这里在运行时回答
/// “某个异常类型是否继承自另一个内建类型”，供 <c>isinstance</c> / <c>issubclass</c>
/// 与用户异常链使用。新增内建异常时与 <see cref="PyExceptionTypes"/> 同步。
/// </remarks>
public static class PyExceptionHierarchy
{
    private static readonly Dictionary<string, string?> Parents = new(StringComparer.Ordinal)
    {
        ["BaseException"] = null,
        ["Exception"] = "BaseException",
        ["Warning"] = "Exception",
        ["UserWarning"] = "Warning",
        ["DeprecationWarning"] = "Warning",
        ["PendingDeprecationWarning"] = "Warning",
        ["RuntimeWarning"] = "Warning",
        ["SyntaxWarning"] = "Warning",
        ["FutureWarning"] = "Warning",
        ["ImportWarning"] = "Warning",
        ["UnicodeWarning"] = "Warning",
        ["BytesWarning"] = "Warning",
        ["ResourceWarning"] = "Warning",
        ["SystemExit"] = "BaseException",
        ["KeyboardInterrupt"] = "BaseException",
        ["GeneratorExit"] = "BaseException",
        ["ArithmeticError"] = "Exception",
        ["ZeroDivisionError"] = "ArithmeticError",
        ["OverflowError"] = "ArithmeticError",
        ["LookupError"] = "Exception",
        ["IndexError"] = "LookupError",
        ["KeyError"] = "LookupError",
        ["SyntaxError"] = "Exception",
        ["ParseError"] = "SyntaxError",
        ["ValueError"] = "Exception",
        ["JSONDecodeError"] = "ValueError",
        ["error"] = "Exception",
        ["BadZipFile"] = "Exception",
        ["zlib.error"] = "Exception",
        ["BadGzipFile"] = "OSError",
        ["MemoryError"] = "Exception",
        ["GetoptError"] = "Exception",
        ["OptParseError"] = "Exception",
        ["OptionError"] = "OptParseError",
        ["OptionConflictError"] = "OptionError",
        ["OptionValueError"] = "OptParseError",
        ["BadOptionError"] = "OptParseError",
        ["AmbiguousOptionError"] = "BadOptionError",
        ["UnicodeError"] = "ValueError",
        ["UnicodeDecodeError"] = "UnicodeError",
        ["UnicodeEncodeError"] = "UnicodeError",
        ["TypeError"] = "Exception",
        ["NameError"] = "Exception",
        ["UnboundLocalError"] = "NameError",
        ["AttributeError"] = "Exception",
        ["FrozenInstanceError"] = "AttributeError",
        ["RuntimeError"] = "Exception",
        ["NotImplementedError"] = "RuntimeError",
        ["RecursionError"] = "RuntimeError",
        ["AssertionError"] = "Exception",
        ["ImportError"] = "Exception",
        ["ModuleNotFoundError"] = "ImportError",
        ["OSError"] = "Exception",
        ["TimeoutError"] = "OSError",
        ["URLError"] = "OSError",
        ["HTTPError"] = "URLError",
        ["SSLError"] = "OSError",
        ["SSLCertVerificationError"] = "SSLError",
        ["HTTPException"] = "Exception",
        ["InvalidURL"] = "HTTPException",
        ["IncompleteRead"] = "HTTPException",
        ["UnsupportedOperation"] = "OSError",
        ["IsADirectoryError"] = "OSError",
        ["NotADirectoryError"] = "OSError",
        ["FileNotFoundError"] = "OSError",
        ["FileExistsError"] = "OSError",
        ["PermissionError"] = "OSError",
        ["StopIteration"] = "Exception",
        ["StopAsyncIteration"] = "Exception",
        ["EOFError"] = "Exception",
        ["BufferError"] = "Exception",
    };

    /// <summary><paramref name="derived"/> 是否等于或继承自 <paramref name="baseName"/>。</summary>
    public static bool IsSubclass(string derived, string baseName)
    {
        string? current = derived;
        for (int guard = 0; current is not null && guard < 64; guard++)
        {
            // CLR 无多继承；此额外边必须与 MIR 的 ValueError handler 展开共同维护。
            if (current == "UnsupportedOperation" && baseName == "ValueError") return true;
            if (string.Equals(current, baseName, StringComparison.Ordinal))
            {
                return true;
            }
            current = Parents.TryGetValue(current, out string? parent) ? parent : null;
        }
        return false;
    }

    /// <summary>该名字是否是已登记的内建异常类型。</summary>
    public static bool IsKnown(string typeName) => Parents.ContainsKey(typeName);
}
