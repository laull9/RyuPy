// 编译模块反向导入与CPython单一sys.modules：只为RyuPy拥有的模块安装延迟查找器。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    /// <inheritdoc />
    public PyModule Import(string name)
    {
        return PythonCall(() =>
        {
        if (imported.TryGetValue(name, out PyModule? module)) return module;
        nint pointer = Check(Native.rp_op(1, s: name));
        // 模块视图与CPython sys.modules中的对象共享唯一指针；反向传参复用该对象。
        var foreign = new PythonObject(this, pointer);
        module = PyProvider.Module(name, foreign.GetAttr, (_, key, value) => foreign.SetAttr(key, value));
        imported[name] = module; objects[pointer] = new(module); reverse.Add(module, new ManagedReference(0, pointer, foreign));
        return module;
        });
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
        Native.rp_decref(Check(Native.rp_op(29, s: """
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
