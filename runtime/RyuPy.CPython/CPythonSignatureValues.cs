// 原生inspect快照与真实CPython对象互转，沿用弱身份缓存；所有结构操作只发生在冷反射路径。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    internal bool TryExportInspect(object value, out nint pointer)
    {
        int tag = PyInspectModule.BridgeIdentity(value);
        pointer = 0;
        if (tag != 0) { RequireInspectProfile(); pointer = Check(SignatureNative.rp_inspect_identity(tag)); return true; }
        if (value is not (PyInspectParameter or PyInspectSignature or PyInspectBoundArguments)) return false;
        RequireInspectProfile();
        if (reverse.TryGetValue(value, out ManagedReference? existing))
        { pointer = Native.rp_incref(existing.Pointer); return true; }
        nint created = value switch
        {
            PyInspectParameter parameter => ExportInspectParameter(parameter),
            PyInspectSignature signature => ExportInspectSignature(signature),
            PyInspectBoundArguments bound => ExportInspectBound(bound),
            _ => throw new InvalidOperationException(),
        };
        var owner = new PythonObject(this, Check(created));
        reverse.Add(value, new ManagedReference(0, created, owner)); objects[created] = new(value);
        if (value is PyInspectBoundArguments exportedBound) exportedBound.AttachStorage(new InspectBoundStorage(this, owner));
        pointer = Native.rp_incref(created); return true;
    }
    private nint ExportInspectParameter(PyInspectParameter value)
    {
        nint name = ToPython(value.Name), defaultValue = 0, annotation = 0;
        try
        {
            defaultValue = ToPython(value.Default); annotation = ToPython(value.Annotation);
            return Check(SignatureNative.rp_inspect_parameter(name, value.Kind.Value, defaultValue, annotation));
        }
        finally { Native.rp_decref(name); Native.rp_decref(defaultValue); Native.rp_decref(annotation); }
    }
    private nint ExportInspectSignature(PyInspectSignature value)
    {
        nint parameters = Check(Native.rp_op(16)), annotation = 0;
        try
        {
            foreach (var p in value.Parameters)
            {
                nint parameter = ToPython(p);
                try { Native.rp_decref(Check(Native.rp_op(17, parameters, parameter, s: p.Name))); }
                finally { Native.rp_decref(parameter); }
            }
            annotation = ToPython(value.ReturnAnnotation);
            return Check(SignatureNative.rp_inspect_signature(parameters, annotation));
        }
        finally { Native.rp_decref(parameters); Native.rp_decref(annotation); }
    }
    private nint ExportInspectBound(PyInspectBoundArguments value)
    {
        if (!synchronizeContainers) throw new PyNotImplementedError("cross-domain BoundArguments requires synchronized containers");
        nint signature = ToPython(value.Signature), arguments = 0;
        try
        {
            arguments = ToPython(value.Arguments);
            return Check(SignatureNative.rp_inspect_bound(signature, arguments));
        }
        finally { Native.rp_decref(signature); Native.rp_decref(arguments); }
    }
    internal bool TryImportInspect(nint pointer, out object? value)
    {
        value = null; if (!inspectProfileReady) return false;
        int tag = SignatureNative.rp_inspect_tag(pointer);
        if (tag < 0) { try { ThrowError(); } finally { Native.rp_decref(pointer); } }
        if (tag == 0) return false;
        if (tag < 20)
        {
            value = tag switch
            {
                1 => PyInspectModule.Empty, >= 2 and <= 6 => PyInspectKind.Values[tag - 2],
                11 => PyInspectParameter.Type, 12 => PyInspectSignature.Type,
                13 => PyInspectBoundArguments.Type, 14 => PyInspectKind.Type,
                15 => PyInspectModule.Export("signature"), 16 => PyInspectModule.Export("unwrap"),
                _ => throw new PyRuntimeError("invalid inspect schema tag"),
            };
            Native.rp_decref(pointer); return true;
        }
        if (objects.TryGetValue(pointer, out WeakReference<object>? weak) && weak.TryGetTarget(out object? cached))
        { value = cached; Native.rp_decref(pointer); return true; }
        try
        {
            nint payload = Check(SignatureNative.rp_inspect_payload(pointer));
            try
            {
                if (tag == 20)
                {
                    string name = ReadTextOwned(Native.rp_op(18, payload, n: 0));
                    object? kind = ReadInspectItem(payload, 1);
                    value = new PyInspectParameter(name, PyInspectKind.Of(kind), ReadInspectItem(payload, 2), ReadInspectItem(payload, 3));
                }
                else if (tag == 21)
                {
                    nint sequence = Check(Native.rp_op(18, payload, n: 0));
                    object?[] parameters;
                    try { parameters = ReadTuple(sequence); } finally { Native.rp_decref(sequence); }
                    value = new PyInspectSignature(parameters.Select(p => p as PyInspectParameter
                        ?? throw new PyNotImplementedError("foreign Signature contains unsupported Parameter subclass")), ReadInspectItem(payload, 1), false);
                }
                else
                {
                    var signature = ReadInspectItem(payload, 0) as PyInspectSignature
                        ?? throw new PyNotImplementedError("foreign BoundArguments requires exact Signature");
                    nint dictionary = Check(Native.rp_op(18, payload, n: 1));
                    try { value = new PyInspectBoundArguments(signature, ImportInspectDictionary(dictionary)); }
                    finally { Native.rp_decref(dictionary); }
                }
            }
            finally { Native.rp_decref(payload); }
            var owner = new PythonObject(this, pointer);
            reverse.Add(value!, new ManagedReference(0, pointer, owner)); objects[pointer] = new(value!);
            if (value is PyInspectBoundArguments importedBound) importedBound.AttachStorage(new InspectBoundStorage(this, owner));
            return true;
        }
        catch { Native.rp_decref(pointer); throw; }
    }
    // 注解内建类型映射到Runtime同一类型；其它值保留外部对象或已有canonical托管身份。
    private object? ReadInspectItem(nint tuple, int index)
    {
        nint item = Check(Native.rp_op(18, tuple, n: index));
        foreach (string name in new[] { "int", "str", "float", "bool", "list", "dict", "tuple", "set", "bytes", "object", "type" })
        {
            nint builtin = Check(Native.rp_builtin(name)); bool same = item == builtin; Native.rp_decref(builtin);
            if (same) { Native.rp_decref(item); return PyType.Get(name); }
        }
        if (Native.rp_scalar_kind(item) == 4) return ReadTextOwned(item);
        return FromOwned(item);
    }
}
