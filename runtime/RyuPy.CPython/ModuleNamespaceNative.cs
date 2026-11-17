// stock namespace 的固定 C ABI；原始指针仅在宿主租约/GIL 内使用，字典引用由 PythonObject 管理。
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

internal static class ModuleNamespaceNative
{
    private const string Library = "ryupy_cpython";
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_entry_module();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_module_namespace(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_namespace_lookup(nint dict, nint key, out nint result);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_namespace_delete(nint dict, nint key);
}
