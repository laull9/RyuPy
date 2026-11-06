// 真str根描述符与保存的绑定方法；普通属性覆盖由PyObject类链处理，显式根调用不再次派发。
using System;
using System.Collections.Concurrent;
namespace RyuPy.Runtime;

internal static class PyStringDescriptors
{
    private static readonly ConcurrentDictionary<string, Method> Methods = new(StringComparer.Ordinal);
    internal static bool IsMethod(string name) => PyStringMethods.Has(name) || name is "__new__" or "__init__"
        or "__str__" or "__repr__" or "__hash__" or "__len__" or "__iter__" or "__getitem__"
        or "__contains__" or "__add__" or "__mul__" or "__rmul__" or "__mod__"
        or "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__";
    internal static object Unbound(string name) => name == "__init__" ? PyBaseObjectMethods.Unbound(name)
        : Methods.GetOrAdd(name, static value => new Method(value, null));
    internal static object Bind(object receiver, string name) => name == "__init__"
        ? PyBaseObjectMethods.BindConstruction(receiver, name)
        : name == "__new__" ? Unbound(name) : new Method(name, receiver);

    private sealed class Method(string name, object? receiver) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => name == "__new__" ? "builtin_function_or_method"
            : receiver is null ? PyStringMethods.Has(name) ? "method_descriptor" : "wrapper_descriptor"
            : PyStringMethods.Has(name) ? "builtin_function_or_method" : "method-wrapper";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name, "__qualname__" => "str." + name,
            "__objclass__" when name != "__new__" && (receiver is null || !PyStringMethods.Has(name)) => PyType.Get("str"),
            "__self__" when name == "__new__" => PyType.Get("str"),
            "__self__" when receiver is not null => receiver,
            "__class__" => PyType.Get(TypeName),
            _ => throw Missing(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            // allocator自行准备绑定/转换错误，不再次包住用户__str__正文。
            if (name == "__new__") return PyStringLayout.New(args, names, values);
            return InvokeCore(args, names, values);
        }
        private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
        private object? InvokeCore(object?[] args, string[] names, object?[] values)
        {
            if (receiver is null && args.Length == 0)
                throw Own(new PyTypeError(name is "format" or "startswith" or "endswith" or "partition" or "rpartition"
                    ? $"unbound method str.{name}() needs an argument"
                    : $"descriptor '{name}' of 'str' object needs an argument"));
            object? self = receiver ?? args[0];
            if (!PyStringLayout.TryGet(self, out _))
                throw Own(new PyTypeError($"descriptor '{name}' for 'str' objects doesn't apply to a '{PyRepr.TypeName(self)}' object"));
            object?[] rest = receiver is null ? args[1..] : args;
            if (PyStringMethods.Has(name)) return PyStringMethods.Invoke(self!, name, rest, names, values, receiver is not null);
            int count = name is "__str__" or "__repr__" or "__hash__" or "__len__" or "__iter__" ? 0 : 1;
            if (rest.Length != count || names.Length != 0)
                throw Own(new PyTypeError($"str.{name}() requires {count + 1} positional arguments"));
            return PyStringProtocols.Invoke(name, self!, rest);
        }
    }
}
