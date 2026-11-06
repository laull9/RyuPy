// dict 根描述符：保存公开 self、区分 method/wrapper 元数据，显式根调用永不重派发覆盖。
using System;
using System.Collections.Concurrent;
namespace RyuPy.Runtime;

internal static class PyDictDescriptors
{
    private static readonly ConcurrentDictionary<string, Method> Methods = new(StringComparer.Ordinal);
    internal static bool IsAttribute(string name) => PyDictRootMethods.Has(name) || name is "__new__" or "__init__"
        or "__reversed__" or "__str__" or "__repr__" or "__hash__" or "__len__" or "__iter__" or "__getitem__"
        or "__setitem__" or "__delitem__" or "__contains__" or "__or__" or "__ror__" or "__ior__"
        or "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__";
    internal static object Unbound(string name) => name switch
    {
        "__hash__" => PyNone.Instance,
        "__str__" => PyBaseObjectMethods.Unbound(name),
        "fromkeys" => Methods.GetOrAdd(name, static n => new Method(n, PyType.Get("dict"))),
        _ => Methods.GetOrAdd(name, static n => new Method(n, null)),
    };
    internal static object Bind(object receiver, string name)
    {
        if (name is "__hash__" or "__new__") return Unbound(name);
        if (name == "fromkeys") receiver = receiver is PyDictInstance instance ? instance.Class
            : receiver is PyDict ? PyType.Get("dict") : receiver;
        return new Method(name, receiver);
    }
    private static bool IsMethod(string name) => PyDictRootMethods.Has(name) || name is "__getitem__" or "__reversed__" or "__contains__";

    private sealed class Method(string name, object? receiver) : PyNativeObject, IPyNativeCallable
    {
        private string Owner => name == "__str__" ? "object" : "dict";
        public override string TypeName => name == "__new__" ? "builtin_function_or_method"
            : receiver is null ? IsMethod(name) ? "method_descriptor" : "wrapper_descriptor"
            : IsMethod(name) ? "builtin_function_or_method" : "method-wrapper";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name, "__qualname__" => Owner + "." + name,
            "__objclass__" when name != "__new__" && (receiver is null || !IsMethod(name)) => PyType.Get(Owner),
            "__self__" when name == "__new__" => PyType.Get("dict"),
            "__self__" when receiver is not null => receiver,
            "__class__" => PyType.Get(TypeName),
            _ => throw Missing(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (name == "__new__") return PyDictLayout.New(args, names, values);
            if (name == "fromkeys") return PyDictRootMethods.Invoke(receiver!, name, args, names, values);
            if (receiver is null && args.Length == 0)
                throw Error(IsMethod(name) ? $"unbound method dict.{name}() needs an argument"
                    : $"descriptor '{name}' of '{Owner}' object needs an argument");
            object? self = receiver ?? args[0];
            // object.__str__ 接受任意对象，但只调用 repr，不调用用户 str。
            if (name != "__str__" && !PyDictLayout.TryGet(self, out _))
                throw Error(IsMethod(name)
                    ? $"descriptor '{name}' for 'dict' objects doesn't apply to a '{PyRepr.TypeName(self)}' object"
                    : $"descriptor '{name}' requires a 'dict' object but received a '{PyRepr.TypeName(self)}'");
            object?[] rest = receiver is null ? args[1..] : args;
            if (name == "__init__") return PyDictLayout.Init(self!, rest, names, values);
            if (PyDictRootMethods.Has(name)) return PyDictRootMethods.Invoke(self!, name, rest, names, values);
            if (name == "__reversed__")
            {
                if (names.Length != 0) throw Error("dict.__reversed__() takes no keyword arguments");
                if (rest.Length != 0) throw Error($"dict.__reversed__() takes no arguments ({rest.Length} given)");
            }
            else if (name is "__getitem__" or "__contains__")
            {
                if (names.Length != 0) throw Error($"dict.{name}() takes no keyword arguments");
                if (rest.Length != 1) throw Error($"dict.{name}() takes exactly one argument ({rest.Length} given)");
            }
            else
            {
                if (names.Length != 0) throw Error($"wrapper {name}() takes no keyword arguments");
                int count = name is "__str__" or "__repr__" or "__len__" or "__iter__" or "__reversed__" ? 0 : name == "__setitem__" ? 2 : 1;
                if (rest.Length != count)
                {
                    string prefix = name is "__setitem__" or "__mul__" or "__rmul__" or "__imul__" ? " " : "";
                    throw Error($"{prefix}expected {count} argument{(count == 1 ? "" : "s")}, got {rest.Length}");
                }
            }
            return PyDictProtocols.Invoke(name, self!, rest);
        }
        private static PyTypeError Error(string message) => PyDictLayout.Own(new PyTypeError(message));
    }
}
