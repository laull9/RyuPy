// inspect组合入口：CIL函数只有一份元数据，外部可调用的明确签名按受信schema转换。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyInspectForeignBridge
{
    private bool inspectProfileReady;
    internal void PrepareInspectModule(string name) { if (name == "inspect") RequireInspectProfile(); }
    private void RequireInspectProfile()
    {
        if (!inspectProfileReady) throw new PyNotImplementedError("inspect bridge requires explicit inspect provider ownership at host bootstrap");
    }
    internal void InstallInspectBridge()
    {
        using Lease lease = Enter();
        if (modules.Contains("inspect"))
        {
            if (SignatureNative.rp_inspect_prepare() < 0) ThrowError();
            inspectProfileReady = true;
        }
        PyInspectForeign.Current = this;
    }
    internal void RemoveInspectBridge()
    { if (ReferenceEquals(PyInspectForeign.Current, this)) PyInspectForeign.Current = null; }

    // 仅跨域原生函数代理具有计算视图；Python层函数本身仍没有合成__signature__属性。
    internal object? InspectAttribute(object? target, string name)
    {
        if (target is ForeignMethodTarget method && name is "__signature__" or "__globals__") target = method.Function;
        if (target is PyFunction function && name == "__signature__")
        {
            try { return PyObject.GetAttr(function, name); }
            catch (PyAttributeError) { }
            // 真wrapped链交给CPinspect默认unwrap；false不得静默返回展开后的签名。
            try { _ = PyObject.GetAttr(function, "__wrapped__"); }
            catch (PyAttributeError) { return PyInspectCallable.Signature(function, false); }
            throw new PyAttributeError("__signature__");
        }
        return PyObject.GetAttr(target, name);
    }
    /// <inheritdoc/>
    public PyInspectSignature Signature(IPyForeignObject value, bool followWrapped) => PythonCall(() =>
    {
        RequireInspectProfile();
        PythonObject foreign = RequireForeignObject(value);
        object? explicitSignature;
        try { explicitSignature = foreign.GetAttr("__signature__"); }
        catch (PyAttributeError) { return ReadForeignFunctionSignature(foreign); }
        return explicitSignature as PyInspectSignature
            ?? throw new PyValueError("external callable requires an explicit supported Signature");
    });
}
