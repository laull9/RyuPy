// Slice 1 独立 C 探针静态互操作声明（libdealloc_probe）：
// 遵守固定声明式 P/Invoke 约定，仅为 Runner 程序集注册一次 DllImportResolver。
using System.Runtime.InteropServices;

internal static class ProbeNative
{
    private const string Library = "dealloc_probe";
    private static readonly object Gate = new();
    private static nint _handle;
    private static bool _resolverRegistered;

    internal static void Initialize(string probePath)
    {
        lock (Gate)
        {
            if (_handle == 0) _handle = NativeLibrary.Load(probePath);
            if (!_resolverRegistered)
            {
                _resolverRegistered = true;
                NativeLibrary.SetDllImportResolver(typeof(ProbeNative).Assembly, (name, _, _) =>
                    name == Library ? _handle : 0);
            }
        }
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void probe_reset();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long probe_count();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long probe_gil_valid_count();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long probe_late_guard_count();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint probe_create(nint child);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint probe_create_late_guard(nint child);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint probe_create_with_callback(nint child, nint cb);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void probe_toggle_strict_reject_0_to_1();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void probe_reset_eval_stats();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long probe_replace_cil_callbacks_twice();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long probe_verify_fini_fence_immutable();
}
