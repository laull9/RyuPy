// CPython文本回读只在UTF8无法表达代理码点时走此路径；无损边界复用Runtime中性工厂。
using RyuPy.Runtime;
namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private unsafe string ReadUnicodeCodePoints(nint pointer)
    {
        long length = Native.rp_unicode_codepoints(pointer, null, 0);
        if (length < 0) ThrowError();
        if (length > 0x3fffffdf) throw new PyOverflowError("foreign Unicode exceeds managed string capacity");
        string points = string.Create((int)length, (Host: this, pointer), static (destination, state) =>
        {
            fixed (char* output = destination)
                if (Native.rp_unicode_codepoints(state.pointer, output, destination.Length) < 0) state.Host.ThrowError();
        });
        return PyOps.UnicodeFromCodePoints(points);
    }
}
