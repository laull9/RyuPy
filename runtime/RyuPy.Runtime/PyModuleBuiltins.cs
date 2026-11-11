// 有限已支持builtin清单与HIR scope::resolve_builtin同步；真实共享字典是fallback唯一存储。
namespace RyuPy.Runtime;
internal static class PyModuleBuiltins
{
    internal static readonly PyDict Default = Create();
    // 默认有限域中已知但未实现的builtin必须明确拒绝；不影响用户字典缺键的NameError。
    internal static bool IsUnsupported(string name) => name is
        "aiter" or "anext" or "breakpoint" or "compile" or "complex" or
        "credits" or "copyright" or "divmod" or "eval" or "exec" or "filter" or
        "help" or "license" or "vars" or "BaseExceptionGroup" or "ExceptionGroup" or
        "BlockingIOError" or "BrokenPipeError" or "ChildProcessError" or "ConnectionAbortedError" or
        "ConnectionError" or "ConnectionRefusedError" or "ConnectionResetError" or "EncodingWarning" or
        "InterruptedError" or "ProcessLookupError" or "StopAsyncIteration" or
        "IndentationError" or "TabError" or "TimeoutError" or "UnicodeTranslateError" or "MemoryError" or
        "ReferenceError" or "SystemError" or "FloatingPointError" or "ascii" or "bin" or "hex" or "oct";

    private static PyDict Create()
    {
        var values = new PyDict();
        values.Set("__name__", "builtins");
        values.Set("globals", PyInstanceGlobals.Function);
        values.Set("__import__", PyModuleImport.Function);
        values.Set("__build_class__", PyInstanceClassHook.Function);
        values.Set("int", PyOps.TypeObject("int"));
        values.Set("float", PyOps.TypeObject("float"));
        values.Set("str", PyOps.TypeObject("str"));
        values.Set("type", PyOps.TypeObject("type"));
        values.Set("object", PyOps.TypeObject("object"));
        values.Set("list", PyOps.TypeObject("list"));
        values.Set("tuple", PyOps.TypeObject("tuple"));
        values.Set("dict", PyOps.TypeObject("dict"));
        values.Set("set", PyOps.TypeObject("set"));
        values.Set("frozenset", PyOps.TypeObject("frozenset"));
        values.Set("bytes", PyOps.TypeObject("bytes"));
        values.Set("bytearray", PyOps.TypeObject("bytearray"));
        values.Set("slice", PyOps.TypeObject("slice"));
        values.Set("bool", PyOps.TypeObject("bool"));
        values.Set("BaseException", PyOps.TypeObject("BaseException"));
        values.Set("Exception", PyOps.TypeObject("Exception"));
        values.Set("Warning", PyOps.TypeObject("Warning"));
        values.Set("UserWarning", PyOps.TypeObject("UserWarning"));
        values.Set("DeprecationWarning", PyOps.TypeObject("DeprecationWarning"));
        values.Set("PendingDeprecationWarning", PyOps.TypeObject("PendingDeprecationWarning"));
        values.Set("RuntimeWarning", PyOps.TypeObject("RuntimeWarning"));
        values.Set("SyntaxWarning", PyOps.TypeObject("SyntaxWarning"));
        values.Set("FutureWarning", PyOps.TypeObject("FutureWarning"));
        values.Set("ImportWarning", PyOps.TypeObject("ImportWarning"));
        values.Set("UnicodeWarning", PyOps.TypeObject("UnicodeWarning"));
        values.Set("BytesWarning", PyOps.TypeObject("BytesWarning"));
        values.Set("ResourceWarning", PyOps.TypeObject("ResourceWarning"));
        values.Set("SystemExit", PyOps.TypeObject("SystemExit"));
        values.Set("KeyboardInterrupt", PyOps.TypeObject("KeyboardInterrupt"));
        values.Set("GeneratorExit", PyOps.TypeObject("GeneratorExit"));
        values.Set("ArithmeticError", PyOps.TypeObject("ArithmeticError"));
        values.Set("BufferError", PyOps.TypeObject("BufferError"));
        values.Set("ZeroDivisionError", PyOps.TypeObject("ZeroDivisionError"));
        values.Set("OverflowError", PyOps.TypeObject("OverflowError"));
        values.Set("LookupError", PyOps.TypeObject("LookupError"));
        values.Set("IndexError", PyOps.TypeObject("IndexError"));
        values.Set("SyntaxError", PyOps.TypeObject("SyntaxError"));
        values.Set("KeyError", PyOps.TypeObject("KeyError"));
        values.Set("ValueError", PyOps.TypeObject("ValueError"));
        values.Set("UnicodeError", PyOps.TypeObject("UnicodeError"));
        values.Set("UnicodeDecodeError", PyOps.TypeObject("UnicodeDecodeError"));
        values.Set("UnicodeEncodeError", PyOps.TypeObject("UnicodeEncodeError"));
        values.Set("TypeError", PyOps.TypeObject("TypeError"));
        values.Set("NameError", PyOps.TypeObject("NameError"));
        values.Set("UnboundLocalError", PyOps.TypeObject("UnboundLocalError"));
        values.Set("AttributeError", PyOps.TypeObject("AttributeError"));
        values.Set("RuntimeError", PyOps.TypeObject("RuntimeError"));
        values.Set("NotImplementedError", PyOps.TypeObject("NotImplementedError"));
        values.Set("RecursionError", PyOps.TypeObject("RecursionError"));
        values.Set("AssertionError", PyOps.TypeObject("AssertionError"));
        values.Set("ImportError", PyOps.TypeObject("ImportError"));
        values.Set("ModuleNotFoundError", PyOps.TypeObject("ModuleNotFoundError"));
        values.Set("OSError", PyOps.TypeObject("OSError"));
        values.Set("FileNotFoundError", PyOps.TypeObject("FileNotFoundError"));
        values.Set("FileExistsError", PyOps.TypeObject("FileExistsError"));
        values.Set("PermissionError", PyOps.TypeObject("PermissionError"));
        values.Set("IsADirectoryError", PyOps.TypeObject("IsADirectoryError"));
        values.Set("NotADirectoryError", PyOps.TypeObject("NotADirectoryError"));
        values.Set("StopIteration", PyOps.TypeObject("StopIteration"));
        values.Set("EOFError", PyOps.TypeObject("EOFError"));
        values.Set("print", PyOps.BuiltinFunction("print"));
        values.Set("open", PyOps.BuiltinFunction("open"));
        values.Set("input", PyOps.BuiltinFunction("input"));
        values.Set("pow", PyOps.BuiltinFunction("pow"));
        values.Set("abs", PyOps.BuiltinFunction("abs"));
        values.Set("len", PyOps.BuiltinFunction("len"));
        values.Set("ord", PyOps.BuiltinFunction("ord"));
        values.Set("chr", PyOps.BuiltinFunction("chr"));
        values.Set("memoryview", PyOps.BuiltinFunction("memoryview"));
        values.Set("sorted", PyOps.BuiltinFunction("sorted"));
        values.Set("hash", PyOps.BuiltinFunction("hash"));
        values.Set("repr", PyOps.BuiltinFunction("repr"));
        values.Set("format", PyOps.BuiltinFunction("format"));
        values.Set("dir", PyOps.BuiltinFunction("dir"));
        values.Set("Ellipsis", PyOps.BuiltinFunction("Ellipsis"));
        values.Set("NotImplemented", PyOps.BuiltinFunction("NotImplemented"));
        values.Set("iter", PyOps.BuiltinFunction("iter"));
        values.Set("next", PyOps.BuiltinFunction("next"));
        values.Set("range", PyOps.BuiltinFunction("range"));
        values.Set("enumerate", PyOps.BuiltinFunction("enumerate"));
        values.Set("zip", PyOps.BuiltinFunction("zip"));
        values.Set("map", PyOps.BuiltinFunction("map"));
        values.Set("exit", PyOps.BuiltinFunction("exit"));
        values.Set("quit", PyOps.BuiltinFunction("quit"));
        values.Set("reversed", PyOps.BuiltinFunction("reversed"));
        values.Set("sum", PyOps.BuiltinFunction("sum"));
        values.Set("min", PyOps.BuiltinFunction("min"));
        values.Set("max", PyOps.BuiltinFunction("max"));
        values.Set("any", PyOps.BuiltinFunction("any"));
        values.Set("all", PyOps.BuiltinFunction("all"));
        values.Set("round", PyOps.BuiltinFunction("round"));
        values.Set("isinstance", PyOps.BuiltinFunction("isinstance"));
        values.Set("issubclass", PyOps.BuiltinFunction("issubclass"));
        values.Set("property", PyOps.BuiltinFunction("property"));
        values.Set("staticmethod", PyOps.BuiltinFunction("staticmethod"));
        values.Set("classmethod", PyOps.BuiltinFunction("classmethod"));
        values.Set("super", PyOps.BuiltinFunction("super"));
        values.Set("id", PyOps.BuiltinFunction("id"));
        values.Set("getattr", PyOps.BuiltinFunction("getattr"));
        values.Set("setattr", PyOps.BuiltinFunction("setattr"));
        values.Set("delattr", PyOps.BuiltinFunction("delattr"));
        values.Set("locals", PyOps.BuiltinFunction("locals"));
        values.Set("hasattr", PyOps.BuiltinFunction("hasattr"));
        values.Set("callable", PyOps.BuiltinFunction("callable"));
        values.Set("IOError", PyOps.TypeObject("OSError"));
        values.Set("EnvironmentError", PyOps.TypeObject("OSError"));
        return values;
    }
}
