// 固定stock解释器的IMPORT_FROM入口；原生helper经普通调用carrier进入，属性getter与错误保原对象。
using System.Runtime.InteropServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyCompiledImportProvider
{
    /// <inheritdoc />
    public object? ImportAttribute(object? imported, string name) => PythonCall(() =>
    {
        using var helper = new PythonObject(this, Check(CompiledImportNative.rp_import_attribute_callable()));
        return helper.Invoke([imported, name], [], []);
    });
}

internal static class CompiledImportNative
{
    [DllImport("ryupy_cpython", CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_import_attribute_callable();
}
