// 仅八个ASCII分类的bytes/bytearray根描述符；不泛化其它根成员、派生布局或descriptor协议。
using System.Collections.Concurrent;

namespace RyuPy.Runtime;

internal static class PyBinaryClassificationDescriptors
{
    private static readonly ConcurrentDictionary<(PyType Owner, string Name), Method> Methods = new();

    internal static bool Has(PyType owner, string name) => (owner.Name is "bytes" or "bytearray")
        && PyBinaryClassification.Has(name);

    internal static object Unbound(PyType owner, string name) =>
        Methods.GetOrAdd((owner, name), static key => new Method(key.Owner, key.Name));

    private sealed class Method(PyType owner, string name) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method_descriptor";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name, "__qualname__" => owner.Name + "." + name,
            "__objclass__" => owner, "__class__" => PyType.Get(TypeName),
            _ => throw Missing(attribute),
        };
        public override string PythonRepr() => $"<method '{name}' of '{owner.Name}' objects>";

        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            // 必须先取真正的位置self；kwargs中的self不能满足要求，也不能先报kwargs错误。
            if (args.Length == 0)
                throw PyBinaryClassification.Error($"unbound method {owner.Name}.{name}() needs an argument");
            object? self = args[0];
            if (owner.Name == "bytes" ? self is not PyBytes : self is not PyByteArray)
                throw PyBinaryClassification.Error($"descriptor '{name}' for '{owner.Name}' objects doesn't apply to a '{PyRepr.TypeName(self)}' object");
            // 不切片args[1..]；剩余位置数即可决定零参方法是否可执行。
            return PyBinaryClassification.Invoke(self!, name, args.Length - 1, names.Length);
        }
    }
}
