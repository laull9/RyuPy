// BoundArguments的arguments是真实可变dict；args和kwargs每次从当前字典及签名重建。
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>原生inspect.BoundArguments。</summary>
public sealed class PyInspectBoundArguments : PyNativeObject
{
    /// <summary>真实类型。</summary>
    public static readonly PyNativeType Type = new("BoundArguments", "inspect", Construct, x => x is PyInspectBoundArguments);
    static PyInspectBoundArguments() { PyInspectModule.RegisterIdentity(Type, 13); }
    /// <summary>绑定所用签名。</summary>
    public PyInspectSignature Signature { get; }
    /// <summary>可替换的实参字典。</summary>
    public PyDict Arguments
    {
        get => storage?.Read() ?? arguments;
        private set { arguments = value; storage?.Write(value); }
    }
    private PyDict arguments;
    private IPyInspectBoundStorage? storage;
    /// <summary>仅受信跨域组合根连接同一BoundArguments的活arguments存储。</summary>
    public void AttachStorage(IPyInspectBoundStorage value) => storage = value;
    /// <summary>保留输入字典身份。</summary>
    public PyInspectBoundArguments(PyInspectSignature signature, PyDict arguments) { Signature = signature; this.arguments = arguments; }
    private static object Construct(object?[] a, string[] n, object?[] v)
    {
        var b = PyInspectModule.Arguments("BoundArguments", ["signature", "arguments"], ["pos", "pos"], [null, null], 2, a, n, v);
        return new PyInspectBoundArguments(b[0] as PyInspectSignature ?? throw new PyTypeError("signature must be a Signature"),
            b[1] as PyDict ?? throw new PyTypeError("arguments must be a dictionary"));
    }
    /// <inheritdoc/>
    public override string TypeName => "BoundArguments";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type, "signature" or "_signature" => Signature, "arguments" => Arguments,
        "args" => Positionals(), "kwargs" => Keywords(),
        "apply_defaults" => PyInspectModule.Method(name, (a, n, v) =>
        { PyInspectModule.Arguments(name, [], [], [], 0, a, n, v); ApplyDefaults(); return PyNone.Instance; }),
        "__eq__" or "__ne__" => PyInspectModule.Method(name, (a, n, v) =>
        { var b = PyInspectModule.Arguments(name, ["other"], ["posonly"], [null], 1, a, n, v);
            bool equal = b[0] is PyInspectBoundArguments other && Signature.EqualsSignature(other.Signature) && PyHash.Equal(Arguments, other.Arguments);
            return equal != (name == "__ne__"); }),
        _ => throw Missing(name),
    };
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name != "arguments") throw Missing(name);
        Arguments = value as PyDict ?? throw new PyTypeError("arguments must be a dictionary");
    }
    /// <summary>只补存在的默认值与变参空容器，必填缺失仍缺失；按签名顺序替换字典。</summary>
    public void ApplyDefaults()
    {
        var result = new PyDict();
        foreach (var p in Signature.Parameters)
        {
            if (Arguments.TryGet(p.Name, out object? value)) result.Set(p.Name, value);
            else if (!ReferenceEquals(p.Default, PyInspectModule.Empty)) result.Set(p.Name, p.Default);
            else if (p.Kind.Value == 2) result.Set(p.Name, new PyTuple([]));
            else if (p.Kind.Value == 4) result.Set(p.Name, new PyDict());
        }
        Arguments = result;
    }
    private PyTuple Positionals()
    {
        var result = new List<object?>();
        foreach (var p in Signature.Parameters)
        {
            if (p.Kind.Value is 3 or 4 || !Arguments.TryGet(p.Name, out object? value)) break;
            if (p.Kind.Value == 2) result.AddRange(PyInspectModule.Sequence(value)); else result.Add(value);
        }
        return new PyTuple(result.ToArray());
    }
    private PyDict Keywords()
    {
        var result = new PyDict(); bool useKeywords = false;
        foreach (var p in Signature.Parameters)
        {
            if (p.Kind.Value == 3) useKeywords = true;
            if (!Arguments.TryGet(p.Name, out object? value)) { useKeywords = true; continue; }
            if (p.Kind.Value == 4)
            {
                if (value is not PyDict dict) throw new PyTypeError("kwargs must be a dictionary");
                result.Update(dict);
            }
            else if (useKeywords && p.Kind.Value is 1 or 3) result.Set(p.Name, value);
        }
        return result;
    }
    /// <inheritdoc/>
    public override string PythonRepr()
    {
        var entries = new List<string>();
        foreach (var (key, value) in Arguments.Entries()) entries.Add(key + "=" + PyRepr.Repr(value));
        return "<BoundArguments (" + string.Join(", ", entries) + ")>";
    }
}

/// <summary>冷反射对象的跨域可变字段接口；普通函数布局/调用不依赖此协议。</summary>
public interface IPyInspectBoundStorage
{
    /// <summary>读取当前真实arguments字典。</summary>
    PyDict Read();
    /// <summary>替换当前真实arguments字典。</summary>
    void Write(PyDict value);
}
