// 固定静态 ABI：UTF-8 字符串与明确 owned/borrowed 引用，兼容 NativeAOT。
using System.Runtime.InteropServices;

internal static unsafe class Native
{
    private const string Library = "core_handles";
    [DllImport(Library)] internal static extern int ch_init(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string home,
        delegate* unmanaged[Cdecl]<nint, long, long, long*, int> invoke,
        delegate* unmanaged[Cdecl]<nint, void> release);
    [DllImport(Library)] internal static extern int ch_run([MarshalAs(UnmanagedType.LPUTF8Str)] string source);
    [DllImport(Library)] internal static extern nint ch_eval([MarshalAs(UnmanagedType.LPUTF8Str)] string expression);
    [DllImport(Library)] internal static extern nint ch_make(nint globals, nint token);
    [DllImport(Library)] internal static extern nint ch_token(nint function);
    [DllImport(Library)] internal static extern int ch_set(nint globals, nint function);
    [DllImport(Library)] internal static extern void ch_inc(nint value);
    [DllImport(Library)] internal static extern void ch_dec(nint value);
    [DllImport(Library)] internal static extern long ch_live();
    [DllImport(Library)] internal static extern long ch_destroyed();
    [DllImport(Library)] internal static extern int ch_finish();
}

internal static class Calls
{
    [DllImport("core_handles")] internal static extern int ch_call(nint f, long x, out long result);
}
