// Signature保持参数声明顺序及只读映射；仅关键字参数的顺序不影响相等/哈希。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>原生inspect.Signature，不与调用ABI的PySignature混用。</summary>
public sealed class PyInspectSignature : PyNativeObject
{
    /// <summary>真实原生类型。</summary>
    public static readonly PyNativeType Type = new("Signature", "inspect", Construct, x => x is PyInspectSignature);
    /// <summary>不可变参数序列，桥接只读schema使用。</summary>
    public IReadOnlyList<PyInspectParameter> Parameters { get; }
    /// <summary>返回注解或empty。</summary>
    public object? ReturnAnnotation { get; }
    private readonly PyMappingProxy mapping;
    static PyInspectSignature()
    {
        PyInspectModule.RegisterIdentity(Type, 12);
        Type.SetAttr("empty", PyInspectModule.Empty);
        Type.SetAttr("from_callable", PyInspectModule.Method("from_callable", PyInspectCallable.SignatureCall));
    }
    /// <summary>检查参数顺序、重复名字和必填位置参数。</summary>
    public PyInspectSignature(IEnumerable<PyInspectParameter> parameters, object? annotation, bool validate = true)
    {
        var list = new List<PyInspectParameter>(); var dict = new PyDict();
        int previous = 0; bool defaultSeen = false;
        foreach (var p in parameters)
        {
            int kind = p.Kind.Value;
            if (dict.TryGet(p.Name, out _)) throw new PyValueError($"duplicate parameter name: '{p.Name}'");
            if (validate)
            {
                if (kind < previous) throw new PyValueError("wrong parameter order");
                if (kind is 0 or 1)
                {
                    if (!ReferenceEquals(p.Default, PyInspectModule.Empty)) defaultSeen = true;
                    else if (defaultSeen) throw new PyValueError("non-default argument follows default argument");
                }
            }
            previous = kind; list.Add(p); dict.Set(p.Name, p);
        }
        Parameters = list.AsReadOnly(); ReturnAnnotation = annotation;
        mapping = (PyMappingProxy)PyMappingProxy.Create([dict], [], []);
    }
    private static object Construct(object?[] a, string[] n, object?[] v)
    {
        var b = PyInspectModule.Arguments("Signature", ["parameters", "return_annotation", "__validate_parameters__"],
            ["pos", "kwonly", "kwonly"], [PyNone.Instance, PyInspectModule.Empty, true], 0, a, n, v);
        return FromSequence(b[0], b[1], PyOps.Truthy(b[2]));
    }
    private static PyInspectSignature FromSequence(object? sequence, object? annotation, bool validate = true)
    {
        var list = new List<PyInspectParameter>();
        if (!PyInspectModule.IsNone(sequence))
            foreach (object? value in PyInspectModule.Sequence(sequence))
                list.Add(value as PyInspectParameter ?? throw new PyTypeError("parameters must be Parameter objects"));
        return new PyInspectSignature(list, annotation, validate);
    }
    /// <inheritdoc/>
    public override string TypeName => "Signature";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type, "parameters" or "_parameters" => mapping,
        "return_annotation" or "_return_annotation" => ReturnAnnotation, "empty" => PyInspectModule.Empty,
        "bind" or "bind_partial" => PyInspectModule.Method(name, (a, n, v) => Bind(a, n, v, name == "bind_partial")),
        "replace" => PyInspectModule.Method(name, Replace),
        "__eq__" or "__ne__" => PyInspectModule.Method(name, (a, n, v) =>
        { var b = PyInspectModule.Arguments(name, ["other"], ["posonly"], [null], 1, a, n, v); return EqualsSignature(b[0]) != (name == "__ne__"); }),
        "__hash__" => PyInspectModule.Method(name, (a, n, v) =>
        { PyInspectModule.Arguments(name, [], [], [], 0, a, n, v); return Hash(); }),
        _ => throw Missing(name),
    };
    private object Replace(object?[] a, string[] n, object?[] v)
    {
        var marker = new object();
        var b = PyInspectModule.Arguments("replace", ["parameters", "return_annotation"], ["kwonly", "kwonly"],
            [marker, ReturnAnnotation], 0, a, n, v);
        return ReferenceEquals(b[0], marker) ? new PyInspectSignature(Parameters, b[1]) : FromSequence(b[0], b[1]);
    }
    /// <summary>只绑定实参，既不填默认值也不执行函数正文。</summary>
    public PyInspectBoundArguments Bind(object?[] a, string[] n, object?[] v, bool partial = false) =>
        PyInspectBinding.Bind(this, a, n, v, partial);
    internal bool EqualsSignature(object? value)
    {
        if (value is not PyInspectSignature other || !PyHash.Equal(ReturnAnnotation, other.ReturnAnnotation)
            || Parameters.Count != other.Parameters.Count) return false;
        var left = new List<PyInspectParameter>(); var right = new List<PyInspectParameter>();
        var keywords = new Dictionary<string, PyInspectParameter>(StringComparer.Ordinal);
        foreach (var p in other.Parameters) if (p.Kind.Value == 3) keywords[p.Name] = p; else right.Add(p);
        foreach (var p in Parameters)
            if (p.Kind.Value == 3)
            { if (!keywords.TryGetValue(p.Name, out var q) || !p.EqualsParameter(q)) return false; }
            else left.Add(p);
        if (left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++) if (!left[i].EqualsParameter(right[i])) return false;
        return true;
    }
    private long Hash()
    {
        long hash = PyHash.Of(ReturnAnnotation); long unordered = 0;
        foreach (var p in Parameters)
            if (p.Kind.Value == 3) unordered ^= p.Hash(); else hash = unchecked(hash * 1000003 ^ p.Hash());
        return hash ^ unordered;
    }
    /// <inheritdoc/>
    public override string PythonStr()
    {
        var parts = new List<string>(); bool star = false;
        for (int i = 0; i < Parameters.Count; i++)
        {
            var p = Parameters[i];
            if (p.Kind.Value == 3 && !star) { parts.Add("*"); star = true; }
            if (p.Kind.Value == 2) star = true;
            parts.Add(p.PythonStr());
            if (p.Kind.Value == 0 && (i + 1 == Parameters.Count || Parameters[i + 1].Kind.Value != 0)) parts.Add("/");
        }
        string result = "(" + string.Join(", ", parts) + ")";
        if (!ReferenceEquals(ReturnAnnotation, PyInspectModule.Empty)) result += " -> " + PyInspectFormat.Annotation(ReturnAnnotation);
        return result;
    }
    /// <inheritdoc/>
    public override string PythonRepr() => "<Signature " + PythonStr() + ">";
}

// 格式化只消费当前对象，字符串保留字符串注解身份，不执行eval或用户源码。
internal static class PyInspectFormat
{
    internal static string Annotation(object? value)
    {
        if (PyInspectModule.IsNone(value)) return "None";
        if (value is string text) return PyRepr.Repr(text);
        if (value is PyNativeType type) return type.Module == "builtins" ? type.Name : type.Module + "." + type.Name;
        if (value is PyClass cls) return cls.Name;
        string repr = PyRepr.Repr(value);
        return repr.StartsWith("<class '", StringComparison.Ordinal) && repr.EndsWith("'>", StringComparison.Ordinal)
            ? repr[8..^2] : repr;
    }
}
