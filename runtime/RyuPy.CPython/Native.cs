// shim静态互操作声明；NativeAOT无需动态IL、反射寻址或运行时委托生成。
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

internal static unsafe class Native
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_container_kind(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_list_item(nint value, long index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_dict_next(nint value, ref long position, out nint key, out nint item);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_dict_lookup(nint value, nint key, out nint item);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_exception_type([MarshalAs(UnmanagedType.LPUTF8Str)] string name, nint parent, nint metadata);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_install_imports([MarshalAs(UnmanagedType.LPUTF8Str)] string source);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_make_error([MarshalAs(UnmanagedType.LPUTF8Str)] string type, nint args);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_scalar_kind(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_builtin_exception(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_builtin([MarshalAs(UnmanagedType.LPUTF8Str)] string name);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_list();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_list_append(nint list, nint item);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_list_replace(nint list, nint source);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_dict_delete(nint dict, nint key);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_bytes_view(nint value, out long length);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_exact_tuple(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_notimplemented();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_loaded_modules();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_exception_instance(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_is_module(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_small_integer(nint value, out long number);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_exception_args(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_exception_link(nint value, int cause);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_exception_name(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_exception_suppress(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void rp_exception_set_chain(nint value, nint cause, nint context, int suppress);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_traceback_frame(nint error, int index, out nint file, out nint function, out int line);
    internal const string Library = "ryupy_cpython";
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_start([MarshalAs(UnmanagedType.LPUTF8Str)] string home, nint* paths, int pathCount, nint* argv, int argc, delegate* unmanaged[Cdecl]<nint, int, nint, nint, nint, long, nint, nint> callback);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_init_error();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_gil();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void rp_gil_release(int state);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_stop();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_op(int op, nint a = 0, nint b = 0, nint c = 0, long n = 0, [MarshalAs(UnmanagedType.LPUTF8Str)] string? s = null);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void rp_decref(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_incref(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_managed(nint identity);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_identity(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_none();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_is_none(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_int(long value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_float(double value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_bool(int value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_text(byte* value, long size);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_bytes(byte* value, long size);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_utf8(nint value, out long size);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern long rp_long(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern double rp_double(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_truth(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern long rp_length(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern long rp_hash(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_error();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_take_error();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void rp_restore_error(nint value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void rp_error_text([MarshalAs(UnmanagedType.LPUTF8Str)] string type, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_exception_matches(nint error, nint type);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_is_exception_type(nint type);
}
