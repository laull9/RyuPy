// inspect组合入口：CIL函数只有一份元数据，外部可调用的明确签名按受信schema转换。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyInspectForeignBridge
{
    private bool inspectProfileReady;
    private void RequireInspectProfile()
    {
        if (!inspectProfileReady) throw new PyNotImplementedError("inspect bridge requires --cpython-dynamic-module inspect at host bootstrap");
        if (SignatureNative.rp_inspect_prepare() < 0) ThrowError();
    }
    internal void InstallInspectBridge(bool requested)
    {
        using Lease lease = Enter();
        if (requested)
        {
            if (SignatureNative.rp_inspect_prepare() < 0) ThrowError();
            inspectProfileReady = true;
        }
        PyInspectForeign.Current = this;
    }
    private void WriteInspectReport(System.Text.Json.Utf8JsonWriter writer)
    {
        writer.WriteStartObject("native_inspect_profile"); writer.WriteBoolean("enabled", inspectProfileReady);
        if (inspectProfileReady)
        {
            writer.WriteNumber("active", SignatureNative.rp_inspect_lifecycle(0));
            writer.WriteNumber("owners_created", SignatureNative.rp_inspect_lifecycle(1));
            writer.WriteNumber("owners_cleared", SignatureNative.rp_inspect_lifecycle(2));
            writer.WriteNumber("owners_freed", SignatureNative.rp_inspect_lifecycle(3));
            writer.WriteNumber("aliases_null", SignatureNative.rp_inspect_lifecycle(4));
        }
        writer.WriteEndObject();
    }
    internal void RemoveInspectBridge()
    { if (ReferenceEquals(PyInspectForeign.Current, this)) PyInspectForeign.Current = null; }

    // 仅跨域原生函数代理具有计算视图；Python层函数本身仍没有合成__signature__属性。
    internal object? InspectAttribute(object? target, string name)
    {
        if (!inspectProfileReady) return PyObject.GetAttr(target, name);
        if (target is PyFunction && name is "__signature__" or "__code__"
            || target is ForeignMethodTarget && name is "__signature__" or "__globals__") RequireInspectProfile();
        if (name == "__code__" && target is PyFunction direct && PyInspectFunctionKind.IsOrdinaryModule(direct))
            throw new PyAttributeError("opaque CIL callable has no CPython __code__");
        if (target is ForeignMethodTarget method && name is "__signature__" or "__globals__") target = method.Function;
        if (target is PyFunction function && name == "__signature__")
        {
            try { return PyObject.GetAttr(function, name); }
            catch (PyAttributeError) { }
            // 真wrapped链交给CPinspect默认unwrap；false不得静默返回展开后的签名。
            try { _ = PyObject.GetAttr(function, "__wrapped__"); }
            catch (PyAttributeError)
            {
                var signature = PyInspectCallable.Signature(function, false);
                if (signature.ReturnAnnotation is string || signature.Parameters.Any(p => p.Annotation is string))
                    throw new PyNotImplementedError("cross-domain string annotations require native inspect with eval_str=False");
                return signature;
            }
            throw new PyAttributeError("__signature__");
        }
        return PyObject.GetAttr(target, name);
    }
    /// <inheritdoc/>
    public PyInspectSignature Signature(IPyForeignObject value, bool followWrapped, bool evalStr, Func<object?, PyInspectSignature> resolve) => PythonCall(() =>
    {
        RequireInspectProfile();
        PythonObject foreign = RequireForeignObject(value);
        bool hasSignature = true; object? explicitSignature;
        try { explicitSignature = foreign.GetAttr("__signature__"); }
        catch (PyAttributeError) { hasSignature = false; explicitSignature = PyNone.Instance; }
        if (explicitSignature is not (null or PyNone))
            return explicitSignature as PyInspectSignature ?? throw new PyTypeError("unexpected object in __signature__ attribute");
        if (followWrapped && !hasSignature && Native.rp_is_type(foreign.Pointer) == 0)
        {
            object? wrapped;
            try { wrapped = foreign.GetAttr("__wrapped__"); }
            catch (PyAttributeError) { return ReadForeignFunctionSignature(foreign, evalStr); }
            return resolve(wrapped);
        }
        return ReadForeignFunctionSignature(foreign, evalStr);
    });
}
