// bytes/bytearray.decode 的有限根描述符：稳定身份、严格self与共享参数绑定，不扩展派生布局。
using System;

namespace RyuPy.Runtime;

internal static class PyBinaryDecodeDescriptors
{
    private static readonly Lazy<Method> Bytes = new(() => new Method(PyType.Get("bytes")));
    private static readonly Lazy<Method> ByteArray = new(() => new Method(PyType.Get("bytearray")));

    internal static bool Has(PyType owner, string name) => name == "decode"
        && (ReferenceEquals(owner, PyType.Get("bytes")) || ReferenceEquals(owner, PyType.Get("bytearray")));

    internal static object Unbound(PyType owner) => ReferenceEquals(owner, PyType.Get("bytes"))
        ? Bytes.Value : ByteArray.Value;

    private sealed class Method(PyType owner) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method_descriptor";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => "decode", "__qualname__" => owner.Name + ".decode",
            "__objclass__" => owner, "__class__" => PyType.Get(TypeName),
            _ => throw Missing(attribute),
        };
        public override string PythonRepr() => $"<method 'decode' of '{owner.Name}' objects>";

        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            // 根self只来自位置参数；先验证真正载体，再进行encoding/errors绑定。
            if (args.Length == 0)
                throw PyBinaryDecode.Error(new PyTypeError($"unbound method {owner.Name}.decode() needs an argument"));
            object? self = args[0];
            if (owner.Name == "bytes" ? self is not PyBytes : self is not PyByteArray)
                throw PyBinaryDecode.Error(new PyTypeError(
                    $"descriptor 'decode' for '{owner.Name}' objects doesn't apply to a '{PyRepr.TypeName(self)}' object"));
            return PyBinaryDecode.Invoke(self!, args, names, values, offset: 1);
        }
    }
}
