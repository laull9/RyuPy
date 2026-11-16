// CPython 3.12.13 严格零字节码内核专属 shim 静态互操作声明（libryupy_cpython_strict）。
// 仅供 StrictKernel 模式通过 ProcessNativeBinding 单一解析器调用；compat 3.9+ 永不触碰本类型。
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

internal static unsafe class StrictNative
{
    internal const string Library = "ryupy_cpython_strict";

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_strict_build_version();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_strict_python_version();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_strict_kernel_version();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_has_import_abi();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_set_env(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? value);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_start(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string home,
        nint* paths,
        int pathCount,
        nint* argv,
        int argc,
        delegate* unmanaged[Cdecl]<nint, int, nint, nint, nint, long, nint, nint> callback,
        delegate* unmanaged[Cdecl]<void> finiFenceCallback);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_stop();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_strict_init_error();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_set_phase(int phase);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_get_phase();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_get_strict_reject();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_reject_never_disabled();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_fini_core_calls();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_strict_is_finalized();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long rp_strict_eval_entry_count(int phase);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long rp_strict_eval_executed_count(int phase);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long rp_strict_eval_rejected_count(int phase);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long rp_strict_eval_total_entries();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long rp_strict_eval_total_rejected();
}
