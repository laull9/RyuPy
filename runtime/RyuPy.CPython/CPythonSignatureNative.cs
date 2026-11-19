// inspect桥的静态ABI；新对象均返回owned引用，托管端不读取CPython结构偏移。
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

internal static class SignatureNative
{
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_inspect_prepare();
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern long rp_inspect_lifecycle(int field);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int rp_inspect_tag(nint value);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_inspect_identity(int tag);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_inspect_parameter(nint name, int kind, nint defaultValue, nint annotation);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_inspect_signature(nint parameters, nint annotation);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_inspect_bound(nint signature, nint arguments);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_inspect_payload(nint value);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)] internal static extern nint rp_inspect_function_schema(nint value);
}
