using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>
/// 内建异常的 Python 继承关系（`isinstance(e, Exception)` 用）。
/// </summary>
/// <remarks>
/// CIL 的类型化 catch 直接使用 CLR 继承树；这里维护同一张表是为了在运行时回答
/// “某个异常类型是否继承自另一个内建类型”，供 <c>isinstance</c> / <c>issubclass</c>
/// 与用户异常链使用。新增内建异常时与 <see cref="PyExceptionTypes"/> 同步。
/// </remarks>
public static class PyExceptionHierarchy
{
    private static readonly Dictionary<string, string?> Parents = new(StringComparer.Ordinal)
    {
        ["BaseException"] = null,
        ["Exception"] = "BaseException",
        ["SystemExit"] = "BaseException",
        ["KeyboardInterrupt"] = "BaseException",
        ["GeneratorExit"] = "BaseException",
        ["ArithmeticError"] = "Exception",
        ["ZeroDivisionError"] = "ArithmeticError",
        ["OverflowError"] = "ArithmeticError",
        ["LookupError"] = "Exception",
        ["IndexError"] = "LookupError",
        ["KeyError"] = "LookupError",
        ["ValueError"] = "Exception",
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
        ["FileNotFoundError"] = "OSError",
        ["FileExistsError"] = "OSError",
        ["PermissionError"] = "OSError",
        ["StopIteration"] = "Exception",
        ["StopAsyncIteration"] = "Exception",
        ["EOFError"] = "Exception",
    };

    /// <summary><paramref name="derived"/> 是否等于或继承自 <paramref name="baseName"/>。</summary>
    public static bool IsSubclass(string derived, string baseName)
    {
        string? current = derived;
        for (int guard = 0; current is not null && guard < 64; guard++)
        {
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
