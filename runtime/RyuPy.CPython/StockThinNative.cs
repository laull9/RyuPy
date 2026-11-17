// 显式 stock A 薄函数的固定原生 ABI；仅启用内部候选时调用，默认 Host.Start 无额外工作。
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

internal static unsafe partial class Native
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_thin_enable(delegate* unmanaged[Cdecl]<nint, nint, nint, nint> invoke,
        delegate* unmanaged[Cdecl]<nint, void> release);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_thin_make(nint globals, nint token, int failure);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_thin_token(nint function);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_thin_validate(nint function);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_thin_global(nint function, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void rp_thin_stats(out long live, out long destroyed, out long calls);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void rp_thin_abort(nint function);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_thin_clear();
}
