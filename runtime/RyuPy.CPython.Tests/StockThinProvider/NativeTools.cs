// 独立测试动态库的静态 ABI；不进入产品程序集或默认 shim。
using System.Runtime.InteropServices;

namespace RyuPy.CPython.Tests;

internal static class ThinTestTools
{
    [DllImport("stock_thin_test_tools", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int thin_test_reserve();
    [DllImport("stock_thin_test_tools", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int thin_test_free_slots();
}
