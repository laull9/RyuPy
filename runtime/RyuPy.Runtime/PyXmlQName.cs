// QName 的有限真类型：精确字符串构造和可写 text、值比较和随字段更新的哈希。
using System;

namespace RyuPy.Runtime;

/// <summary>不以字符串冒充 QName；非字符串 text 写入前明确拒绝，不扩展通用比较协议。</summary>
public sealed class PyXmlQName : PyNativeObject
{
    private object? _text;
    private PyXmlQName(object? text) => _text = text;
    /// <inheritdoc />
    public override string TypeName => "QName";
    private static readonly PyFunction Constructor = PyNativeFunctions.Create("QName.__init__",
        ["self", "text_or_uri", "tag"], [null, null, PyNone.Instance], 2, a =>
        {
            if (a[1] is not string text || a[2] is not (null or PyNone or string))
                throw PyXmlElementTree.Unsupported("QName construction outside exact str");
            return new PyXmlQName(a[2] is string { Length: > 0 } tag ? "{" + text + "}" + tag : text);
        });
    internal static object? Create(object?[] args, string[] names, object?[] values) =>
        PyXmlElementTree.InvokeInitializer("QName", Constructor, 2, args, names, values);
    private static readonly PyFunction Equal = PyNativeFunctions.Create("QName.__eq__", ["self", "other"], [null, null], 2,
        a => ((PyXmlQName)a[0]!).Compare(a[1], false));
    private static readonly PyFunction Hash = PyNativeFunctions.Create("QName.__hash__", ["self"], [null], 1,
        a => PyHash.Of(((PyXmlQName)a[0]!).Text()));
    private static readonly PyFunction Str = PyNativeFunctions.Create("QName.__str__", ["self"], [null], 1,
        a => ((PyXmlQName)a[0]!).Text());
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "text" => _text,
        "__class__" => PyXmlElementTree.QNameType,
        "__str__" => PyXmlElementTree.BoundPython("QName.__str__", this, Str),
        "__hash__" => PyXmlElementTree.BoundPython("QName.__hash__", this, Hash),
        "__eq__" => PyXmlElementTree.BoundPython("QName.__eq__", this, Equal),
        "__ne__" => PyXmlElementTree.Wrapper("__ne__", 1, a => Compare(a[0], true)),
        "__repr__" or "__lt__" or "__le__" or "__gt__" or "__ge__" or "__dict__" or "__reduce__" or "__reduce_ex__"
            => throw PyXmlElementTree.Unsupported("QName." + name),
        _ => throw Missing(name),
    };
    /// <inheritdoc />
    public override void SetAttr(string name, object? value)
    {
        if (name == "text")
        {
            if (value is not string) throw PyXmlElementTree.Unsupported("QName non-string text assignment");
            _text = value; return;
        }
        throw PyXmlElementTree.Unsupported("QName dynamic attributes");
    }
    /// <inheritdoc />
    public override void DelAttr(string name) => throw PyXmlElementTree.Unsupported("QName attribute deletion");
    private string Text() => _text as string ?? throw PyXmlElementTree.Unsupported("QName non-string text consumption");
    private object Compare(object? other, bool invert)
    {
        string text = Text();
        if (other is PyXmlQName qname) return (text == qname.Text()) != invert;
        if (other is string exact) return (text == exact) != invert;
        if (PyObject.MayHaveMethods(other)) throw PyXmlElementTree.Unsupported("QName user comparison");
        return invert;
    }
    /// <inheritdoc />
    public override string PythonStr() => Text();
    /// <inheritdoc />
    public override string PythonRepr() => throw PyXmlElementTree.Unsupported("QName representation");
}
