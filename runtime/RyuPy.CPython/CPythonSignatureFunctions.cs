// 真实CP函数code schema转成原生Signature；只读取code/默认值/注解数据，不运行inspect.py。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private PyInspectSignature ReadForeignFunctionSignature(PythonObject callable, bool evalStr)
    {
        nint schema = Check(SignatureNative.rp_inspect_function_schema(callable.Pointer));
        try
        {
            int ReadInt(int index)
            {
                object? value = FromOwned(Native.rp_op(18, schema, n: index));
                return checked((int)(long)value!);
            }
            int positional = ReadInt(1), positionalOnly = ReadInt(2), keywordOnly = ReadInt(3), flags = ReadInt(4);
            int skip = ReadInt(8);
            nint names = Check(Native.rp_op(18, schema, n: 0));
            nint defaults = Check(Native.rp_op(18, schema, n: 5));
            nint keywordDefaults = Check(Native.rp_op(18, schema, n: 6));
            nint annotations = Check(Native.rp_op(18, schema, n: 7));
            try
            {
                object?[] defaultValues = Native.rp_is_none(defaults) != 0 ? [] : ReadTuple(defaults);
                object? Lookup(nint mapping, string name)
                {
                    if (Native.rp_is_none(mapping) != 0) return PyInspectModule.Empty;
                    nint key = ToPython(name);
                    try
                    {
                        int found = Native.rp_dict_lookup(mapping, key, out nint item);
                        if (found < 0) ThrowError();
                        if (found == 0) return PyInspectModule.Empty;
                        try { return ReadInspectValue(item); } finally { Native.rp_decref(item); }
                    }
                    finally { Native.rp_decref(key); }
                }
                string Name(int index) => ReadTextOwned(Native.rp_op(18, names, n: index));
                PyInspectParameter Parameter(int index, int kind, object? defaultValue)
                {
                    string name = Name(index);
                    return new(name, PyInspectKind.Values[kind], defaultValue, Lookup(annotations, name));
                }
                var parameters = new List<PyInspectParameter>();
                for (int i = skip; i < positional; i++)
                    parameters.Add(Parameter(i, i < positionalOnly ? 0 : 1,
                        i >= positional - defaultValues.Length ? defaultValues[i - positional + defaultValues.Length] : PyInspectModule.Empty));
                int variadic = positional + keywordOnly;
                if ((flags & 4) != 0) parameters.Add(Parameter(variadic++, 2, PyInspectModule.Empty));
                for (int i = positional; i < positional + keywordOnly; i++)
                    parameters.Add(Parameter(i, 3, Lookup(keywordDefaults, Name(i))));
                if ((flags & 8) != 0) parameters.Add(Parameter(variadic, 4, PyInspectModule.Empty));
                var signature = new PyInspectSignature(parameters, Lookup(annotations, "return"), false);
                if (evalStr && (signature.ReturnAnnotation is string || parameters.Any(p => p.Annotation is string)))
                    throw new PyNotImplementedError("inspect.signature eval_str with string annotations is not supported");
                return signature;
            }
            finally { Native.rp_decref(names); Native.rp_decref(defaults); Native.rp_decref(keywordDefaults); Native.rp_decref(annotations); }
        }
        finally { Native.rp_decref(schema); }
    }
    // item为借用引用，元数据需要保持真实对象/empty/kind/内建类型的权威身份。
    private object? ReadInspectValue(nint item)
    {
        foreach (string name in new[] { "int", "str", "float", "bool", "list", "dict", "tuple", "set", "bytes", "object", "type" })
        {
            nint builtin = Check(Native.rp_builtin(name)); bool same = item == builtin; Native.rp_decref(builtin);
            if (same) return PyType.Get(name);
        }
        if (Native.rp_scalar_kind(item) == 4)
        {
            if (objects.TryGetValue(item, out WeakReference<object>? weak) && weak.TryGetTarget(out object? cached) && cached is string text) return text;
            string value = ReadText(item); CacheInspectValue(value, Native.rp_incref(item)); return value;
        }
        return FromBorrowed(item);
    }
}
