// inspect.Parameter的不可变快照；默认值和注解保持原始对象身份，仅replace创建新参数。
using System;

namespace RyuPy.Runtime;

/// <summary>原生Parameter。</summary>
public sealed class PyInspectParameter : PyNativeObject
{
    /// <summary>真实类型身份。</summary>
    public static readonly PyNativeType Type = new("Parameter", "inspect", Construct, x => x is PyInspectParameter);
    /// <summary>参数名。</summary>
    public string Name { get; }
    /// <summary>参数种类。</summary>
    public PyInspectKind Kind { get; }
    /// <summary>原默认值或empty。</summary>
    public object? Default { get; }
    /// <summary>原注解或empty。</summary>
    public object? Annotation { get; }
    static PyInspectParameter()
    {
        PyInspectModule.RegisterIdentity(Type, 11);
        Type.SetAttr("empty", PyInspectModule.Empty);
        foreach (var kind in PyInspectKind.Values) Type.SetAttr((string)kind.GetAttr("name")!, kind);
    }
    /// <summary>验证不可变参数；不对默认对象执行转换。</summary>
    public PyInspectParameter(string name, PyInspectKind kind, object? defaultValue, object? annotation)
    {
        if (name.StartsWith(".", StringComparison.Ordinal) && int.TryParse(name.AsSpan(1), out int index))
        {
            if (kind.Value != 1) throw new PyValueError("implicit arguments must be passed as positional or keyword arguments");
            name = "implicit" + index; kind = PyInspectKind.Values[0];
        }
        if (!PyInspectIdentifier.Valid(name) || (kind.Value != 0 && IsKeyword(name)))
            throw new PyValueError($"'{name}' is not a valid parameter name");
        if (kind.Value is 2 or 4 && !ReferenceEquals(defaultValue, PyInspectModule.Empty))
            throw new PyValueError($"{kind.GetAttr("description")} parameters cannot have default values");
        Name = name; Kind = kind; Default = defaultValue; Annotation = annotation;
    }
    private static bool IsKeyword(string name) => (" False None True and as assert async await break class continue def del elif else except finally for from global if import in is lambda nonlocal not or pass raise return try while with yield ").Contains(" " + name + " ", StringComparison.Ordinal);
    private static object Construct(object?[] a, string[] n, object?[] v)
    {
        var b = PyInspectModule.Arguments("Parameter", ["name", "kind", "default", "annotation"],
            ["pos", "pos", "kwonly", "kwonly"], [null, null, PyInspectModule.Empty, PyInspectModule.Empty], 2, a, n, v);
        if (b[0] is not string name) throw new PyTypeError("name must be a str, not " + PyRepr.Str(PyObject.TypeOf(b[0])));
        return new PyInspectParameter(name, PyInspectKind.Of(b[1]), b[2], b[3]);
    }
    /// <inheritdoc/>
    public override string TypeName => "Parameter";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type, "name" or "_name" => Name, "kind" or "_kind" => Kind,
        "default" or "_default" => Default, "annotation" or "_annotation" => Annotation,
        "empty" => PyInspectModule.Empty,
        "replace" => PyInspectModule.Method(name, Replace),
        "__eq__" or "__ne__" => PyInspectModule.Method(name, (a, n, v) =>
        { var b = PyInspectModule.Arguments(name, ["other"], ["posonly"], [null], 1, a, n, v); return b[0] is PyInspectParameter ? EqualsParameter(b[0]) != (name == "__ne__") : PyNotImplemented.Instance; }),
        "__hash__" => PyInspectModule.Method(name, (a, n, v) =>
        { PyInspectModule.Arguments(name, [], [], [], 0, a, n, v); return Hash(); }),
        _ => throw Missing(name),
    };
    private object Replace(object?[] a, string[] n, object?[] v)
    {
        var b = PyInspectModule.Arguments("replace", ["name", "kind", "default", "annotation"],
            ["kwonly", "kwonly", "kwonly", "kwonly"], [Name, Kind, Default, Annotation], 0, a, n, v);
        if (b[0] is not string name) throw new PyTypeError("name must be a str");
        return new PyInspectParameter(name, PyInspectKind.Of(b[1]), b[2], b[3]);
    }
    internal bool EqualsParameter(object? other) => other is PyInspectParameter p && Name == p.Name
        && Kind.Value == p.Kind.Value && PyHash.Equal(Default, p.Default) && PyHash.Equal(Annotation, p.Annotation);
    internal long Hash() => PyHash.Of(new PyTuple([Name, (long)Kind.Value, Default, Annotation]));
    /// <inheritdoc/>
    public override string PythonStr()
    {
        string result = (Kind.Value == 2 ? "*" : Kind.Value == 4 ? "**" : "") + Name;
        bool annotated = !ReferenceEquals(Annotation, PyInspectModule.Empty);
        if (annotated) result += ": " + PyInspectFormat.Annotation(Annotation);
        if (!ReferenceEquals(Default, PyInspectModule.Empty)) result += (annotated ? " = " : "=") + PyRepr.Repr(Default);
        return result;
    }
    /// <inheritdoc/>
    public override string PythonRepr() => "<Parameter " + PyRepr.Repr(PythonStr()) + ">";
}
