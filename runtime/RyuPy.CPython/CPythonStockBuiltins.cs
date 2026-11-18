// 真实CP模块字典共享canonical builtins；规范globals/import守卫比较初始化时保留的原对象。
// namespace分配与薄函数能力分别授权，未创建真实namespace的旧provider保原环境。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyCompiledBuiltinsProvider
{
    private IPyModuleNamespace? stockBuiltins;
    private PythonObject? stockGlobalsBuiltin, stockImportBuiltin, stockBuildClassBuiltin;

    /// <inheritdoc />
    public bool TryGetCompiledBuiltins(out IPyModuleNamespace? builtins)
    {
        if (!stockThinEnabled && !stockNamespaceCreated) { builtins = null; return false; }
        builtins = PythonCall(() =>
        {
            if (stockBuiltins is not null) return stockBuiltins;
            // 共享原字典的活内容，规范callable另用初始化见证；用户提前替换也不能冒领规范身份。
            object dictionary = FromOwned(Check(ModuleNamespaceNative.rp_initial_builtins()))
                ?? throw new PyRuntimeError("initial builtins dictionary returned null");
            PythonObject? globals = null, importer = null, buildClass = null;
            try
            {
                var storage = StockModuleNamespace(dictionary);
                globals = new PythonObject(this, Check(ModuleNamespaceNative.rp_initial_builtin("globals")));
                importer = new PythonObject(this, Check(ModuleNamespaceNative.rp_initial_builtin("__import__")));
                buildClass = new PythonObject(this, Check(ModuleNamespaceNative.rp_initial_builtin("__build_class__")));
                stockGlobalsBuiltin = globals; stockImportBuiltin = importer; stockBuildClassBuiltin = buildClass;
                stockBuiltins = storage;
                globals = null; importer = null; buildClass = null;
                return storage;
            }
            finally { buildClass?.Dispose(); importer?.Dispose(); globals?.Dispose(); }
        });
        return true;
    }

    /// <inheritdoc />
    public bool TryGetCompiledBuiltinsNamespace(object value, out IPyModuleNamespace? builtins)
    {
        builtins = null;
        if ((!stockThinEnabled && !stockNamespaceCreated) || value is not PythonObject foreign || foreign.IsDisposed
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
            "__build_class__" => stockBuildClassBuiltin,
            _ => null,
        };
        // 终态包装已交还 owned 权利；不能读取失效 Pointer，也不缓存悬空原地址。
        return original is { IsDisposed: false } && value is PythonObject { IsDisposed: false } actual
            && ReferenceEquals(actual.Host, this) && actual.Pointer == original.Pointer;
    }
}
