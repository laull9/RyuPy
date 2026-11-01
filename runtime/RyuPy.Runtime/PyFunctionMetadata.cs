namespace RyuPy.Runtime;

public sealed partial class PyFunction
{
    private object? _doc;
    private PyDict? _annotations;
    private PyDict? _attributes;

    internal object? GetMetadata(string name) => name switch
    {
        "__name__" => Name,
        "__doc__" => _doc ?? PyNone.Instance,
        "__annotations__" => _annotations ??= new PyDict(),
        "__dict__" => _attributes ??= new PyDict(),
        _ => _attributes is not null && _attributes.TryGet(name, out object? value)
            ? value : throw MissingMetadata(name),
    };

    internal void SetMetadata(string name, object? value)
    {
        switch (name)
        {
            case "__name__":
                Name = value as string ?? throw new PyTypeError("__name__ must be set to a string object");
                break;
            case "__doc__": _doc = value; break;
            case "__annotations__":
                if (value is not (null or PyNone or PyDict))
                    throw new PyTypeError("__annotations__ must be set to a dict object");
                _annotations = value as PyDict;
                break;
            case "__dict__":
                _attributes = value as PyDict ?? throw new PyTypeError(
                    $"__dict__ must be set to a dictionary, not a '{PyRepr.TypeName(value)}'");
                break;
            default: (_attributes ??= new PyDict()).Set(name, value); break;
        }
    }

    internal void DeleteMetadata(string name)
    {
        switch (name)
        {
            case "__name__": throw new PyTypeError("__name__ must be set to a string object");
            case "__doc__": _doc = null; break;
            case "__annotations__": _annotations = null; break;
            case "__dict__": throw new PyTypeError("cannot delete __dict__");
            default:
                if (_attributes is null || !_attributes.TryGet(name, out _)) throw MissingMetadata(name);
                _attributes.Delete(name);
                break;
        }
    }

    private static PyAttributeError MissingMetadata(string name) =>
        new($"'function' object has no attribute '{name}'");
}

public static partial class PyOps
{
    /// <summary>在函数定义处登记文档与已降级为真实 Python 对象的标注，不自动复制装饰器结果。</summary>
    /// <param name="function">原始函数对象。</param>
    /// <param name="doc">文档字符串或 None。</param>
    /// <param name="names">参数名及可选 return 名。</param>
    /// <param name="values">与名字对应的标注对象。</param>
    public static void FunctionMetadata(object function, object? doc, object?[] names, object?[] values)
    {
        if (function is not PyFunction target)
            throw new PyTypeError("function metadata target must be a function");
        if (names.Length != values.Length)
            throw new PyRuntimeError("function annotation names and values have different lengths");
        var annotations = new PyDict();
        for (int index = 0; index < names.Length; index++)
        {
            if (names[index] is not string name)
                throw new PyTypeError("function annotation names must be strings");
            annotations.Set(name, values[index] ?? PyNone.Instance);
        }
        target.SetMetadata("__doc__", doc);
        target.SetMetadata("__annotations__", annotations);
    }
}
