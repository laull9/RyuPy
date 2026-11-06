// bytes/bytearray.translate 的有限根描述符：稳定身份、严格self与共享参数绑定，不扩展派生布局。
using System;

namespace RyuPy.Runtime;

internal static class PyBinaryTranslateDescriptors
{
    private static readonly Lazy<Method> Bytes = new(() => new Method(PyType.Get("bytes")));
    private static readonly Lazy<Method> ByteArray = new(() => new Method(PyType.Get("bytearray")));

    internal static bool Has(PyType owner, string name) => name == "translate"
        && (ReferenceEquals(owner, PyType.Get("bytes")) || ReferenceEquals(owner, PyType.Get("bytearray")));

    internal static object Unbound(PyType owner) => ReferenceEquals(owner, PyType.Get("bytes"))
        ? Bytes.Value : ByteArray.Value;

    private sealed class Method(PyType owner) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method_descriptor";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => "translate", "__qualname__" => owner.Name + ".translate",
            "__objclass__" => owner, "__class__" => PyType.Get(TypeName),
            _ => throw Missing(attribute),
        };
        public override string PythonRepr() => $"<method 'translate' of '{owner.Name}' objects>";

        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            // 根self只来自位置参数；先验证真正载体，再进行table/delete绑定。
            if (args.Length == 0)
                throw PyBinaryTranslate.Error(new PyTypeError($"unbound method {owner.Name}.translate() needs an argument"));
            object? self = args[0];
            if (owner.Name == "bytes" ? self is not PyBytes : self is not PyByteArray)
                throw PyBinaryTranslate.Error(new PyTypeError(
                    $"descriptor 'translate' for '{owner.Name}' objects doesn't apply to a '{PyRepr.TypeName(self)}' object"));
            return PyBinaryTranslate.Invoke(self!, args, names, values, offset: 1);
        }
    }
}
