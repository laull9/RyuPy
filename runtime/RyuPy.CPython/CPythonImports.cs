// 编译模块反向导入与CPython单一sys.modules：只为RyuPy拥有的模块安装延迟查找器。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    /// <inheritdoc />
    public PyModule Import(string name) => PythonCall(() =>
    {
        if (name == "__main__" && entryScript is not null && !entryStarted)
        {
            entryStarted = true;
            string source = File.ReadAllText(entryScript);
            if (Native.rp_run_entry(entryScript, source) != 0) ThrowError();
        }
        return ModuleFromOwned(Check(Native.rp_op(1, s: name)), name);
    });

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
import types as _rp_types
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
def _rp_calculate_meta(meta, bases):
    winner = meta
    for base in bases:
        base_meta = type(base)
        if issubclass(winner, base_meta):
            continue
        if issubclass(base_meta, winner):
            winner = base_meta
            continue
        raise TypeError("metaclass conflict: the metaclass of a derived class must be a (non-strict) subclass of the metaclasses of all its bases")
    return winner
def _rp_make_class(name, bases, ns):
    resolved_bases = _rp_types.resolve_bases(bases)
    meta = type
    if "__metaclass__" in ns:
        meta = ns.pop("__metaclass__")
    if isinstance(meta, type):
        meta = _rp_calculate_meta(meta, resolved_bases)
    prepare = getattr(meta, "__prepare__", None)
    namespace = prepare(name, resolved_bases) if prepare is not None else {}
    for k, v in ns.items():
        namespace[k] = v
    return meta(name, resolved_bases, namespace)
def _rp_wrap_method(fn, name, qualname):
    def _rp_method(*args, **kwargs):
        return fn(*args, **kwargs)
    _rp_method.__name__ = name
    _rp_method.__qualname__ = qualname
    return _rp_method
def _rp_object_new(cls, *args, **kwargs):
    return object.__new__(cls, *args, **kwargs)
def _rp_object_init(self, *args, **kwargs):
    return object.__init__(self, *args, **kwargs)
def _rp_object_setattr(self, name, value):
    return object.__setattr__(self, name, value)
def _rp_object_delattr(self, name):
    return object.__delattr__(self, name)
def _rp_super_getattr(start, receiver, name):
    proxy = super(start) if receiver is None else super(start, receiver)
    return getattr(proxy, name)
def _rp_super_create(start, receiver):
    return super(start) if receiver is None else super(start, receiver)
_rp_staticmethod = staticmethod
_rp_classmethod = classmethod
_rp_property = property
""")));
    }

}
