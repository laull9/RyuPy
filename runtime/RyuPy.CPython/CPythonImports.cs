// 编译模块反向导入与CPython单一sys.modules：只为RyuPy拥有的模块安装延迟查找器。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    /// <inheritdoc />
    public PyModule Import(string name) => PythonCall(() =>
        ModuleFromOwned(Check(Native.rp_op(1, s: name)), name));

    /// <summary>所有CPython模块指针共用唯一视图；sys.modules是初始化与替换的权威。</summary>
    private PyModule ModuleFromOwned(nint pointer, string? name = null)
    {
        if (objects.TryGetValue(pointer, out WeakReference<object>? weak)
            && weak.TryGetTarget(out object? existing) && existing is PyModule cached
            && reverse.TryGetValue(cached, out ManagedReference? reference)
            && reference.Owner is { IsDisposed: false })
        {
            Native.rp_decref(pointer);
            if (name is not null) imported[name] = cached;
            return cached;
        }
        // 显式释放后的旧视图仍可能由用户持有；新导入必须建立可用的唯一视图。
        if (objects.TryGetValue(pointer, out weak) && weak.TryGetTarget(out object? stale))
            foreach (string key in imported.Where(pair => ReferenceEquals(pair.Value, stale)).Select(pair => pair.Key).ToArray())
                imported.Remove(key);
        string moduleName;
        try { moduleName = name ?? ReadTextOwned(Native.rp_op(2, pointer, s: "__name__")); }
        catch { Native.rp_decref(pointer); throw; }
        var foreign = new PythonObject(this, pointer);
        PyModule module = PyProvider.Module(moduleName, foreign.GetAttr, (_, key, value) => foreign.SetAttr(key, value), foreign);
        imported[moduleName] = module;
        objects[pointer] = new(module);
        reverse.Add(module, new ManagedReference(0, pointer, foreign));
        return module;
    }
    /// <inheritdoc />
    public void Registered(string name) { using Lease lease = Enter(); if (!Owns(name)) compiled.Add(name); }
    private readonly HashSet<string> compiled = new(StringComparer.Ordinal);
    /// <inheritdoc />
    public void Initializing(PyModule module)
    {
        using Lease lease = Enter(); nint pointer = ToPython(module);
        try { Native.rp_decref(Check(Native.rp_op(23, pointer, s: module.Name))); }
        finally { Native.rp_decref(pointer); }
    }
    /// <inheritdoc />
    public void Failed(string name) { using Lease lease = Enter(); Native.rp_decref(Check(Native.rp_op(30, s: name))); }
    private void InstallImports()
    {
        using Lease lease = Enter();
        Native.rp_decref(Check(Native.rp_install_imports("""
import sys as _rp_sys
import importlib.util as _rp_util
class _RyuPyLoader:
    def create_module(self, spec):
        return _rp_load(spec.name)
    def exec_module(self, module):
        pass
class _RyuPyFinder:
    def find_spec(self, fullname, path=None, target=None):
        if _rp_has(fullname):
            return _rp_util.spec_from_loader(fullname, _RyuPyLoader(), is_package=_rp_package(fullname))
_rp_sys.meta_path.insert(0, _RyuPyFinder())
""")));
    }

}
