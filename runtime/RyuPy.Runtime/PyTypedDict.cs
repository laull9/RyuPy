using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>TypedDict的冷创建协议：根factory保持函数身份，生成类型独立于普通PyClass热点。</summary>
public static class PyTypedDict
{
    private sealed record Builder(string Name, string Module, PyTypedDictType? Parent, bool Total);
    private static readonly ConditionalWeakTable<PyClass, Builder> Builders = new();

    /// <summary>typing.TypedDict真实函数；功能式定义与类语句复用同一元数据计算。</summary>
    public static PyFunction Factory { get; } = PyNativeFunctions.Create("TypedDict",
        ["typename", "fields", "total"], [null, null, true], 2, arguments =>
        {
            if (arguments[0] is not string name) throw new PyTypeError("TypedDict name must be a string");
            if (arguments[1] is not PyDict fields)
                throw new PyNotImplementedError("TypedDict fields currently require a dict");
            if (arguments[2] is not bool total)
                throw new PyNotImplementedError("TypedDict total currently requires a bool");
            string module = PyModules.ModuleForFile(PyTraceback.GetCallerLocation(1).File) ?? "__main__";
            return Create(name, module, null, total, fields);
        }, ["posonly", "posonly", "kwonly"]);

    static PyTypedDict()
    {
        Factory.SetMetadata("__module__", "typing");
        Factory.SetMetadata("__qualname__", "TypedDict");
    }

    /// <summary>验证实际基值并创建私有命名空间壳；壳不能发布为Python TypedDict类。</summary>
    /// <param name="name">类名。</param><param name="baseValue">真实根factory或单个TypedDict父。</param>
    /// <param name="total">本类字段是否必填。</param><param name="module">定义模块。</param>
    public static object Begin(string name, object? baseValue, bool total, string module)
    {
        PyTypedDictType? parent = baseValue as PyTypedDictType;
        if (parent is null && !ReferenceEquals(baseValue, Factory))
            throw new PyNotImplementedError("TypedDict bases require the real factory or a TypedDict parent");
        var shell = new PyClass(name);
        shell.SetAttr("__annotations__", new PyDict());
        Builders.Add(shell, new Builder(name, module, parent, total));
        return shell;
    }

    /// <summary>类体注解全部求值后合并元数据；成功产物才可应用装饰器并公开绑定。</summary>
    /// <param name="value">Begin返回的私有构建壳。</param>
    public static object Finish(object? value)
    {
        if (value is not PyClass shell || !Builders.TryGetValue(shell, out Builder? info))
            throw new PyNotImplementedError("TypedDict finalization requires its private build namespace");
        if (!shell.Attributes.TryGetValue("__annotations__", out object? raw) || raw is not PyDict fields)
            throw new PyTypeError("TypedDict __annotations__ must be a dict");
        PyTypedDictType result = Create(info.Name, info.Module, info.Parent, info.Total, fields);
        if (shell.Attributes.TryGetValue("__doc__", out object? doc)) result.Doc = doc;
        Builders.Remove(shell);
        return result;
    }

    private static PyTypedDictType Create(string name, string module, PyTypedDictType? parent,
        bool total, PyDict fields)
    {
        var annotations = new PyDict();
        var required = new HashSet<string>(StringComparer.Ordinal);
        var optional = new HashSet<string>(StringComparer.Ordinal);
        if (parent is not null)
        {
            foreach ((object? key, object? value) in parent.Annotations.Entries()) annotations.Set(key, value);
            required.UnionWith(parent.Required);
            optional.UnionWith(parent.Optional);
        }
        foreach ((object? key, object? value) in fields.Entries())
        {
            if (key is not string field) throw new PyTypeError("TypedDict field names must be strings");
            // 不伪造ForwardRef，也不将尚未实现的Required/NotRequired误按total分类。
            if (value is string || value is PyTypingAlias { Name: "Required" or "NotRequired" }
                || value is PyTypingForm { Name: "Required" or "NotRequired" })
                throw new PyNotImplementedError("TypedDict forward references and field qualifiers are not supported yet");
            if (value is not (null or PyNone or PyType or PyClass or PyNativeType or PyTypingAlias or PyTypingForm or PyTypedDictType))
                throw new PyNotImplementedError("TypedDict currently supports concrete type annotations");
            annotations.Set(field, value is null or PyNone ? PyType.Get("NoneType") : value);
            required.Remove(field);
            optional.Remove(field);
            (total ? required : optional).Add(field);
        }
        return new PyTypedDictType(name, module, total, annotations, required, optional);
    }

    /// <summary>typing.is_typeddict只接受真实产物，不按名称或__annotations__鸭子判断。</summary>
    public static bool IsTypedDict(object? value) => value is PyTypedDictType;
}

/// <summary>TypedDict元数据对象；调用它产生dict，不是自定义类实例。</summary>
public sealed class PyTypedDictType : PyNativeObject, IPyNativeCallable
{
    internal object? Doc { get; set; } = PyNone.Instance;
    internal string Name { get; }
    internal string Module { get; }
    internal bool Total { get; }
    internal PyDict Annotations { get; }
    internal HashSet<string> Required { get; }
    internal HashSet<string> Optional { get; }
    private readonly PyFrozenSet _requiredKeys;
    private readonly PyFrozenSet _optionalKeys;

    internal PyTypedDictType(string name, string module, bool total, PyDict annotations,
        HashSet<string> required, HashSet<string> optional)
    {
        Name = name; Module = module; Total = total; Annotations = annotations;
        Required = required; Optional = optional;
        _requiredKeys = new PyFrozenSet(required);
        _optionalKeys = new PyFrozenSet(optional);
    }

    /// <inheritdoc />
    public override string TypeName => "_TypedDictMeta";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => PyType.Get("_TypedDictMeta"),
        "__name__" or "__qualname__" => Name, "__module__" => Module,
        "__doc__" => Doc,
        "__annotations__" => Annotations, "__total__" => Total,
        "__required_keys__" => _requiredKeys, "__optional_keys__" => _optionalKeys,
        "__bases__" => new PyTuple([PyType.Get("dict")]),
        _ => throw Missing(name),
    };
    /// <inheritdoc />
    public override void SetAttr(string name, object? value) =>
        throw new PyNotImplementedError("TypedDict attribute rebinding is not supported yet; its annotation dict remains mutable");
    /// <inheritdoc />
    public override void DelAttr(string name) =>
        throw new PyNotImplementedError("TypedDict attribute deletion is not supported yet");
    /// <inheritdoc />
    public object? Invoke(object?[] args, string[] names, object?[] values) =>
        PyOps.CallBuiltin("dict", args, names, values);
    /// <inheritdoc />
    public override string PythonRepr() => $"<class '{Module}.{Name}'>";

    /// <summary>TypedDict不能用作运行时instance/subclass检查的右操作数。</summary>
    public static bool RejectRuntimeCheck() =>
        throw new PyTypeError("TypedDict does not support instance and class checks");
}
