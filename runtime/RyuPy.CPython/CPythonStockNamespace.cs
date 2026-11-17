// 原版 libpython 的权威模块字典后端；每次读写直达真实 dict，不经过容器同步或快照。
// 本文件只提供冷适配能力，编译入口晋升与 callable 跨堆所有权各自独立验收。
using System.Runtime.CompilerServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyCompiledModuleNamespaceProvider, IPyCompiledSourceNamespaceProvider
{
    private readonly ConditionalWeakTable<PythonObject, StockNamespace> stockNamespaces = new();
    private bool stockEntryCreated;

    /// <inheritdoc />
    public PyCompiledModuleNamespace CreateEntryNamespace(string name) => PythonCall(() =>
    {
        if (name != "__main__") throw new PyNotImplementedError("stock namespace factory requires a compiled entry");
        if (stockEntryCreated) throw new PyNotImplementedError("compiled stock entry cannot be instantiated a second time");
        var module = new PythonObject(this, Check(ModuleNamespaceNative.rp_entry_module()));
        try
        {
            var storage = new PyCompiledModuleNamespace(StockModuleNamespace(module), module);
            stockEntryCreated = true;
            return storage;
        }
        catch { module.Dispose(); throw; }
    });

    /// <inheritdoc />
    public PyCompiledModuleNamespace CreateModuleNamespace(string name)
    {
        // __main__是语言入口身份，沿现原D；普通源码模块由loader事务决定何时进入缓存。
        if (name == "__main__") return CreateEntryNamespace(name);
        return PythonCall(() =>
        {
            nint key = ToPython(name);
            try
            {
                var module = new PythonObject(this, Check(ModuleNamespaceNative.rp_compiled_module(key)));
                try { return new PyCompiledModuleNamespace(StockModuleNamespace(module), module); }
                catch { module.Dispose(); throw; }
            }
            finally { Native.rp_decref(key); }
        });
    }

    /// <inheritdoc />
    public void PublishEntryNamespace(PyModule module, PyCompiledModuleNamespace storage)
        => PublishStockNamespace(module, storage);

    /// <inheritdoc />
    public void PublishModuleNamespace(PyModule module, PyCompiledModuleNamespace storage)
        => PublishStockNamespace(module, storage);

    private void PublishStockNamespace(PyModule module, PyCompiledModuleNamespace storage)
    {
        using Lease lease = Enter();
        if (storage.Module is not PythonObject foreign || !ReferenceEquals(foreign.Host, this)
            || !ReferenceEquals(module.ForeignObject, foreign))
            throw new PyRuntimeError("compiled namespace has a different module owner");
        // 原生module持字典；这里只有CLR->CP owned边，没有CP->CLR模块代理根。
        reverse.Add(module, new ManagedReference(0, foreign.Pointer, foreign));
        objects[foreign.Pointer] = new(module);
    }

    internal IPyModuleNamespace StockModuleNamespace(object value) => PythonCall(() =>
    {
        PythonObject foreign = value switch
        {
            PythonObject direct when ReferenceEquals(direct.Host, this) => direct,
            PyModule { ForeignObject: PythonObject module } when ReferenceEquals(module.Host, this) => module,
            _ => throw new PyTypeError("module namespace belongs to a different execution domain"),
        };
        // native helper 只接受精确dict或真实module；module子类的属性钩子不能替换md_dict。
        object? view = FromOwned(Check(ModuleNamespaceNative.rp_module_namespace(foreign.Pointer)));
        if (view is not PythonObject dictionary || Native.rp_container_kind(dictionary.Pointer) != 2)
            throw new PyRuntimeError("stock module namespace lost its exact dictionary identity");
        return (IPyModuleNamespace)stockNamespaces.GetValue(dictionary, key => new StockNamespace(this, key));
    });

    private sealed class StockNamespace(CPythonHost host, PythonObject dictionary)
        : IPyModuleNamespace, IPyModuleDictionaryNamespace
    {
        public object ViewObject => dictionary;

        // SETUP_ANNOTATIONS必须创建目标解释器的精确dict，不能经过可覆盖builtins或容器proxy。
        public object CreateDictionary() => host.PythonCall(() =>
            host.FromOwned(host.Check(Native.rp_op(16)))
                ?? throw new PyRuntimeError("new module dictionary returned null"));

        public bool TryGet(string name, out object? value)
        {
            object? read = null;
            bool found = host.PythonCall(() =>
            {
                nint key = host.ToPython(name);
                try
                {
                    int status = ModuleNamespaceNative.rp_namespace_lookup(dictionary.Pointer, key, out nint item);
                    if (status < 0) host.ThrowError();
                    if (status != 0) read = host.FromOwned(item);
                    return status != 0;
                }
                finally { Native.rp_decref(key); }
            });
            value = read;
            return found;
        }

        public void Set(string name, object? value) => host.PythonCall(() =>
        {
            nint key = host.ToPython(name), item = 0;
            try
            {
                item = host.ToPython(value);
                Native.rp_decref(host.Check(Native.rp_op(7, dictionary.Pointer, key, item)));
            }
            finally { Native.rp_decref(key); Native.rp_decref(item); }
            return 0;
        });

        public bool DeleteIfPresent(string name) => host.PythonCall(() =>
        {
            nint key = host.ToPython(name);
            try
            {
                int status = ModuleNamespaceNative.rp_namespace_delete(dictionary.Pointer, key);
                if (status < 0) host.ThrowError();
                return status != 0;
            }
            finally { Native.rp_decref(key); }
        });
    }
}
