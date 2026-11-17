// stock 薄函数编译入口共享原 CP builtins；规范 globals/import 守卫仅比较受 owned 保护的原对象。
// 初始化发生在入口 owned 事务内，普通 provider 路径未启用时完全沿用旧环境。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyCompiledBuiltinsProvider
{
    private IPyModuleNamespace? stockBuiltins;
    private PythonObject? stockGlobalsBuiltin, stockImportBuiltin;

    /// <inheritdoc />
    public bool TryGetCompiledBuiltins(out IPyModuleNamespace? builtins)
    {
        if (!stockThinEnabled) { builtins = null; return false; }
        builtins = PythonCall(() =>
        {
            if (stockBuiltins is not null) return stockBuiltins;
            // 直接读取内建 module 的真实 md_dict；不通过可覆盖的 __dict__ 属性。
            var module = new PythonObject(this, Check(Native.rp_op(1, s: "builtins")));
            try
            {
                var dictionary = StockModuleNamespace(module);
                var globals = new PythonObject(this, Check(Native.rp_builtin("globals")));
                try
                {
                    var importer = new PythonObject(this, Check(Native.rp_builtin("__import__")));
                    try { CaptureStockFrameBuiltins(globals); }
                    catch { importer.Dispose(); throw; }
                    stockGlobalsBuiltin = globals; stockImportBuiltin = importer;
                    stockBuiltins = dictionary;
                    return dictionary;
                }
                catch { globals.Dispose(); throw; }
            }
            finally { module.Dispose(); }
        });
        return true;
    }

    /// <inheritdoc />
    public bool TryGetCompiledBuiltinsNamespace(object value, out IPyModuleNamespace? builtins)
    {
        builtins = null;
        if (!stockThinEnabled || value is not PythonObject foreign || foreign.IsDisposed
            || !ReferenceEquals(foreign.Host, this)) return false;
        // StockModuleNamespace 只适配 exact dict / module；非法值保持其真实 TypeError。
        builtins = StockModuleNamespace(foreign);
        return true;
    }

    /// <inheritdoc />
    public bool IsCanonicalCompiledBuiltin(object? value, string name)
    {
        PythonObject? original = name switch
        {
            "globals" => stockGlobalsBuiltin,
            "__import__" => stockImportBuiltin,
            _ => null,
        };
        // 终态包装已交还 owned 权利；不能读取失效 Pointer，也不缓存悬空原地址。
        return original is { IsDisposed: false } && value is PythonObject { IsDisposed: false } actual
            && ReferenceEquals(actual.Host, this) && actual.Pointer == original.Pointer;
    }
}
