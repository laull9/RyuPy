using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>有类型模块槽的独立绑定位；成功路径不装箱数值，不把普通运算放入动态解释循环。</summary>
public static partial class PyOps
{
    /// <summary>读取模块名字前检查绑定；真未知名字抛 NameError，未实现内建回退明确拒绝，成功后直接读取 typed 字段。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckGlobalBound([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool bound, string name)
    {
        if (!bound) ThrowUnboundGlobal(name);
    }
    // 固定 CPython 3.12.13 的内建名称事实。只在未绑定的模块槽回退分支使用，
    // 不干扰真实用户绑定；完整 builtin fallback 尚未实现时必须明确拒绝，不能伪造 NameError。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnboundGlobal(string name)
    {
        if (name is "ArithmeticError" or "AssertionError" or "AttributeError" or "BaseException" or "BaseExceptionGroup"
            or "BlockingIOError" or "BrokenPipeError" or "BufferError" or "BytesWarning" or "ChildProcessError"
            or "ConnectionAbortedError" or "ConnectionError" or "ConnectionRefusedError" or "ConnectionResetError" or "DeprecationWarning"
            or "EOFError" or "Ellipsis" or "EncodingWarning" or "EnvironmentError" or "Exception"
            or "ExceptionGroup" or "False" or "FileExistsError" or "FileNotFoundError" or "FloatingPointError"
            or "FutureWarning" or "GeneratorExit" or "IOError" or "ImportError" or "ImportWarning"
            or "IndentationError" or "IndexError" or "InterruptedError" or "IsADirectoryError" or "KeyError"
            or "KeyboardInterrupt" or "LookupError" or "MemoryError" or "ModuleNotFoundError" or "NameError"
            or "None" or "NotADirectoryError" or "NotImplemented" or "NotImplementedError" or "OSError"
            or "OverflowError" or "PendingDeprecationWarning" or "PermissionError" or "ProcessLookupError" or "RecursionError"
            or "ReferenceError" or "ResourceWarning" or "RuntimeError" or "RuntimeWarning" or "StopAsyncIteration"
            or "StopIteration" or "SyntaxError" or "SyntaxWarning" or "SystemError" or "SystemExit"
            or "TabError" or "TimeoutError" or "True" or "TypeError" or "UnboundLocalError"
            or "UnicodeDecodeError" or "UnicodeEncodeError" or "UnicodeError" or "UnicodeTranslateError" or "UnicodeWarning"
            or "UserWarning" or "ValueError" or "Warning" or "ZeroDivisionError" or "__build_class__"
            or "__debug__" or "__doc__" or "__import__" or "__loader__" or "__name__"
            or "__package__" or "__spec__" or "abs" or "aiter" or "all"
            or "anext" or "any" or "ascii" or "bin" or "bool"
            or "breakpoint" or "bytearray" or "bytes" or "callable" or "chr"
            or "classmethod" or "compile" or "complex" or "copyright" or "credits"
            or "delattr" or "dict" or "dir" or "divmod" or "enumerate"
            or "eval" or "exec" or "exit" or "filter" or "float"
            or "format" or "frozenset" or "getattr" or "globals" or "hasattr"
            or "hash" or "help" or "hex" or "id" or "input"
            or "int" or "isinstance" or "issubclass" or "iter" or "len"
            or "license" or "list" or "locals" or "map" or "max"
            or "memoryview" or "min" or "next" or "object" or "oct"
            or "open" or "ord" or "pow" or "print" or "property"
            or "quit" or "range" or "repr" or "reversed" or "round"
            or "set" or "setattr" or "slice" or "sorted" or "staticmethod"
            or "str" or "sum" or "super" or "tuple" or "type"
            or "vars" or "zip")
            throw new PyNotImplementedError($"builtin fallback for unbound module name '{name}' is not supported");
        throw new PyNameError($"name '{name}' is not defined");
    }

}

/// <summary>模块属性协议与内部名字协议分离：缺属性抛 AttributeError，而非 NameError。</summary>
public static partial class PyModules
{
    /// <summary>静态导出槽使用显式绑定位，不把数值零或 false 当作“尚未赋值”。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckBound(bool bound, string module, string name)
    {
        if (!bound) MissingAttr(module, name);
    }

    /// <summary>动态导出槽检查未绑定哨兵；合法的 Python None 对象原样返回。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object LoadAttribute(object? value, string module, string name)
    {
        if (value is null or PyUnbound) MissingAttr(module, name);
        return value!;
    }
}
