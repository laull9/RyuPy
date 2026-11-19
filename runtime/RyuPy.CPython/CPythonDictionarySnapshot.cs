// 批量拉回精确 dict，避免为每项构建键列表/元组、重复查找或来回跨 C ABI。
using System.Runtime.InteropServices;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private unsafe (object? Key, object? Value)[] ReadDictSnapshot(nint dict)
    {
        int count = checked((int)Native.rp_length(dict)); ThrowIfError();
        var entries = new (object?, object?)[count];
        var pointers = new nint[checked(count * 2)];
        fixed (nint* buffer = pointers)
            if (Native.rp_dict_snapshot(dict, buffer, count) < 0) ThrowError();
        var consumed = 0;
        try
        {
            for (int i = 0; i < count; i++)
            {
                object? key = FromOwned(pointers[consumed++]);
                RequireTransferKey(key);
                entries[i] = (key, FromOwned(pointers[consumed++]));
            }
            return entries;
        }
        finally
        {
            // 校验或转换异常只交还尚未移交给 FromOwned 的引用。
            while (consumed < pointers.Length) Native.rp_decref(pointers[consumed++]);
        }
    }
}

internal static unsafe partial class Native
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rp_dict_snapshot(nint dict, nint* items, nint count);
}
