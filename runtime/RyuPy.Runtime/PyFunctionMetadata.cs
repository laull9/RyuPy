// 函数真实元数据与普通属性分离；module在定义处捕获，帧只持函数身份并冷读取当前值。
namespace RyuPy.Runtime;

public sealed partial class PyFunction
{
    private object? _doc;
    private PyDict? _annotations;
    private PyDict? _attributes;
    // 与普通__dict__条目独立；删除与未捕获都以null表示，Python读取统一得到None。
    private object? _module;

    /// <summary>读取真实模块成员；不调用用户真值、字符串或属性协议。</summary>
    internal object Module => _module ?? PyNone.Instance;

    /// <summary>定义位置捕获的值保留原引用，不随模块全局名字后续变化。</summary>
    internal void CaptureModule(object? module) => _module = module;

    internal object? GetMetadata(string name) => PyTypingIo.TryFunctionMetadata(this, name, out object? ioValue)
        ? ioValue : name switch
    {
        "__name__" => Name,
        "__qualname__" => PyFunctionDefinition.GetQualname(this),
        "__code__" => throw UnsupportedCode(),
        "__closure__" when Body.Target is PyFunctionOwner => throw new PyNotImplementedError("instance function closure reflection is not supported"),
        "__module__" => Module,
        "__globals__" => PyFunctionOwner.GetContext(this).State.Globals,
        "__builtins__" => PyFunctionOwner.GetContext(this).Builtins,
        "__doc__" => _doc ?? PyNone.Instance,
        "__annotations__" => _annotations ??= new PyDict(),
        "__dict__" => _attributes ??= new PyDict(),
        _ => _attributes is not null && _attributes.TryGet(name, out object? value)
            ? value : throw MissingMetadata(name),
    };

    internal void SetMetadata(string name, object? value)
    {
        PyTypingIo.GuardFunctionMutation(this, name);
        // 元数据字段仍存精确字典；不能剥离派生owner后伪装保存了原对象身份。
        if (value is PyOrderedDict && name is "__annotations__" or "__dict__")
            throw PyOrderedDict.Unsupported("function metadata storage");
        if (value is PyDictInstance && name is "__annotations__" or "__dict__")
            throw PyDictLayout.Own(new PyNotImplementedError("function metadata with dict subclass storage is not supported"));
        switch (name)
        {
            case "__name__":
                Name = value as string ?? throw new PyTypeError("__name__ must be set to a string object");
                break;
            case "__qualname__": PyFunctionDefinition.SetQualname(this, value); break;
            case "__code__": throw UnsupportedCode();
            case "__closure__" when Body.Target is PyFunctionOwner: throw new PyAttributeError("readonly attribute");
            case "__globals__":
            case "__builtins__": throw new PyAttributeError("readonly attribute");
            case "__doc__": _doc = value; break;
            case "__module__": _module = value; break;
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
        PyTypingIo.GuardFunctionMutation(this, name);
        switch (name)
        {
            case "__name__": throw new PyTypeError("__name__ must be set to a string object");
            case "__qualname__": throw new PyTypeError("__qualname__ must be set to a string object");
            case "__code__": throw UnsupportedCode();
            case "__closure__" when Body.Target is PyFunctionOwner: throw new PyAttributeError("readonly attribute");
            case "__globals__":
            case "__builtins__": throw new PyAttributeError("readonly attribute");
            case "__doc__": _doc = null; break;
            case "__module__": _module = null; break;
            case "__annotations__": _annotations = null; break;
            case "__dict__": throw new PyTypeError("cannot delete __dict__");
            default:
                if (_attributes is null || !_attributes.TryGet(name, out _)) throw MissingMetadata(name);
                _attributes.Delete(name);
                break;
        }
    }

    // 代码读写删均明确拒绝，不能落入普通字典假装替换后继续消费旧来源。
    private static PyNotImplementedError UnsupportedCode() => new("function code reflection is not supported");

    private static PyAttributeError MissingMetadata(string name) =>
        new($"'function' object has no attribute '{name}'");
}

public static partial class PyOps
{
    /// <summary>在真实def位置完成默认值与标注后、应用装饰器前捕获模块成员。</summary>
    public static void FunctionModule(object function, object? module)
    {
        if (function is not PyFunction target)
            throw new PyTypeError("function module target must be a function");
        target.CaptureModule(module);
    }

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
