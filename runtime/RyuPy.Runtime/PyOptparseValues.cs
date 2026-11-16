// Values 保存真实可变属性字典；初始化浅拷贝，显式传入解析时可保留对象身份。
using System;
namespace RyuPy.Runtime;

/// <summary>optparse 结果对象，与 argparse.Namespace 是不同的实际类型。</summary>
public sealed class PyOptparseValues : PyNativeObject
{
    internal PyDict Attributes = new();
    private static readonly PyFunction Factory = PyNativeFunctions.Create("Values", ["defaults"], [null], 0, a => new PyOptparseValues(a[0]));
    internal static object? Construct(object?[] args, string[] names, object?[] values) => PyOptparse.Call(Factory, args, names, values);
    internal PyOptparseValues(object? defaults)
    {
        if (!PyOps.Truthy(defaults)) return;
        if (defaults is not PyDict dict) throw PyOptparse.Unsupported("custom Values defaults mapping");
        foreach (var (key, value) in dict.Entries()) SetAttr(PyOptparse.Text(key), value);
    }
    /// <inheritdoc/>
    public override string TypeName => "Values";
    /// <inheritdoc/>
    public override object? GetAttr(string name)
    {
        if (name == "__dict__") return Attributes;
        if (name == "__class__") return PyOptparse.ValuesType;
        if (Attributes.TryGet(name, out object? value))
        {
            if (name.StartsWith("__", StringComparison.Ordinal)) throw PyOptparse.Unsupported("Values instance special slots via __dict__");
            return value ?? PyNone.Instance;
        }
        return name switch
        {
            "__class__" => PyOptparse.ValuesType,
            "__hash__" => PyNone.Instance,
            "__eq__" => PyNativeFunctions.Create(name, ["other"], [null], 1, a =>
                a[0] is PyOptparseValues other ? PyOps.Compare(PyOps.Eq, Attributes, other.Attributes)
                : a[0] is PyDict dict ? PyOps.Compare(PyOps.Eq, Attributes, dict) : PyNotImplemented.Instance),
            _ => throw Missing(name),
        };
    }
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name == "__dict__")
        {
            if (value is PyOrderedDict) throw PyOrderedDict.Unsupported("optparse Values metadata storage");
            Attributes = value as PyDict ?? throw PyOptparse.Own(new PyTypeError("__dict__ must be set to a dictionary")); return;
        }
        // 原生隐式槽尚无独立实例槽隔离，不能接受后错误改变内建身份/比较。
        if (name.StartsWith("__", StringComparison.Ordinal)) throw PyOptparse.Unsupported("Values special attribute mutation");
        Attributes.Set(name, value);
    }
    /// <inheritdoc/>
    public override void DelAttr(string name)
    {
        if (name.StartsWith("__", StringComparison.Ordinal)) throw PyOptparse.Unsupported("Values special attribute deletion");
        if (!Attributes.TryGet(name, out _)) throw Missing(name);
        Attributes.Delete(name);
    }
    /// <inheritdoc/>
    public override string PythonStr() => PyRepr.Str(Attributes);
    /// <inheritdoc/>
    public override string PythonRepr() => PyOptparse.ReprIdentity(this, "Values", PythonStr());
}
