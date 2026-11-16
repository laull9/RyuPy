// C1 显式原生方法描述符候选；默认保留兼容包装器，候选不安装 Python 方法转发函数。
namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private readonly bool nativeMethodsEnabled = ReadNativeMethodsOption();
    private static bool ReadNativeMethodsOption() => Environment.GetEnvironmentVariable("RYUPY_CPYTHON_NATIVE_METHODS") switch
    {
        null or "" or "0" => false,
        "1" => true,
        _ => throw new InvalidDataException("RYUPY_CPYTHON_NATIVE_METHODS只接受0或1"),
    };

    private nint CreateNativeMethodOwned(object target, string name, string qualname)
    {
        nint targetPtr = ToPython(target), namePtr = 0, qualnamePtr = 0;
        try
        {
            namePtr = ToPython(name); qualnamePtr = ToPython(qualname);
            return Check(Native.rp_native_method(targetPtr, namePtr, qualnamePtr));
        }
        finally
        {
            Native.rp_decref(targetPtr); Native.rp_decref(namePtr); Native.rp_decref(qualnamePtr);
        }
    }

    // 旧兼容域仍依赖真正的 PyFunction；显式候选仅通过静态 C 类型调用托管体。
    private const string LegacyMethodWrapper = """

def _rp_wrap_method(fn, name, qualname):
    def _rp_method(*args, **kwargs):
        return fn(*args, **kwargs)
    _rp_method.__name__ = name
    _rp_method.__qualname__ = qualname
    return _rp_method
""";
}
