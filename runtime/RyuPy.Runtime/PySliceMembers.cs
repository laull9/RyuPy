// slice 的有限绑定/根方法与只读属性。接收者校验先于方法参数，不复制 args 或解析字符串签名。
using System;
using System.Collections.Concurrent;

namespace RyuPy.Runtime;

internal static class PySliceMembers
{
    private static readonly ConcurrentDictionary<string, Method> Roots = new(StringComparer.Ordinal);
    internal static bool HasMethod(string name) => name is "indices" or "__repr__" or "__hash__"
        or "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__";

    internal static object Bound(PySlice receiver, string name) => new Method(name, receiver);
    internal static object TypeAttribute(string name) => name switch
    {
        "__name__" or "__qualname__" => "slice", "__module__" => "builtins", "__class__" => PyType.Get("type"),
        _ when HasMethod(name) => Roots.GetOrAdd(name, static method => new Method(method, null)),
        "start" or "stop" or "step" or "__new__" or "__dict__" or "__bases__" or "__base__" or "__mro__"
            or "__reduce__" or "__reduce_ex__" or "__getstate__" or "__doc__"
            or "__str__" or "__format__" or "__sizeof__" or "__dir__" =>
            throw PySlice.PublicError(new PyNotImplementedError($"slice.{name} descriptor or reflection is not supported")),
        _ => throw PySlice.PublicError(new PyAttributeError($"type object 'slice' has no attribute '{name}'")),
    };

    internal static void RejectMutation(string name, object? value, bool deleting)
    {
        if (name is "start" or "stop" or "step")
            throw PySlice.PublicError(new PyAttributeError("readonly attribute"));
        if (name == "__class__")
            throw PySlice.PublicError(new PyTypeError(deleting ? "can't delete __class__ attribute"
                : value is PyType or PyNativeType or PyClass
                    ? "__class__ assignment only supported for mutable types or ModuleType subclasses"
                    : $"__class__ must be set to a class, not '{PyRepr.TypeName(value)}' object"));
        throw PySlice.PublicError(new PyAttributeError(HasMethod(name)
            ? $"'slice' object attribute '{name}' is read-only" : $"'slice' object has no attribute '{name}'"));
    }

    internal static void RejectTypeMutation(string name, bool deleting) =>
        throw PySlice.PublicError(new PyTypeError($"cannot {(deleting ? "delete" : "set")} '{name}' attribute of immutable type 'slice'"));

    internal sealed class Method(string name, PySlice? receiver) : PyNativeObject, IPyNativeCallable
    {
        private string Name => name;
        private PySlice? Receiver => receiver;
        internal bool SameMethod(Method other) => name == other.Name && ReferenceEquals(receiver, other.Receiver);
        internal long IdentityHash() => HashCode.Combine(name,
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(receiver ?? (object)PySlice.Type));
        public override string PythonRepr() => throw PySlice.PublicError(new PyNotImplementedError("slice method repr is not supported"));
        public override string TypeName => receiver is null
            ? (name == "indices" ? "method_descriptor" : "wrapper_descriptor")
            : (name == "indices" ? "builtin_function_or_method" : "method-wrapper");
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name, "__qualname__" => "slice." + name,
            "__self__" when receiver is not null => receiver,
            "__module__" when receiver is not null && name == "indices" => PyNone.Instance,
            "__objclass__" when receiver is null || name != "indices" => PySlice.Type,
            "__class__" => PyType.Get(TypeName),
            "__get__" or "__doc__" or "__text_signature__" or "__repr__" or "__reduce__" or "__reduce_ex__" =>
                throw PySlice.PublicError(new PyNotImplementedError("slice method descriptor reflection is not supported")),
            _ => throw Missing(attribute),
        };

        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            PySlice self;
            int offset;
            if (receiver is not null) { self = receiver; offset = 0; }
            else
            {
                if (args.Length == 0)
                    throw PySlice.PublicError(new PyTypeError(name == "indices"
                        ? "unbound method slice.indices() needs an argument"
                        : $"descriptor '{name}' of 'slice' object needs an argument"));
                if (args[0] is not PySlice slice)
                    throw PySlice.PublicError(new PyTypeError(name == "indices"
                        ? $"descriptor 'indices' for 'slice' objects doesn't apply to a '{PyRepr.TypeName(args[0])}' object"
                        : $"descriptor '{name}' requires a 'slice' object but received a '{PyRepr.TypeName(args[0])}'"));
                self = slice; offset = 1;
            }
            if (names.Length != 0)
                throw PySlice.PublicError(new PyTypeError(name == "indices"
                    ? "slice.indices() takes no keyword arguments" : $"wrapper {name}() takes no keyword arguments"));
            int count = args.Length - offset;
            int required = name is "__hash__" or "__repr__" ? 0 : 1;
            if (count != required)
                throw PySlice.PublicError(new PyTypeError(name == "indices"
                    ? $"slice.indices() takes exactly one argument ({count} given)"
                    : $"expected {required} argument{(required == 1 ? "" : "s")}, got {count}"));
            if (name == "indices") return self.PublicIndices(args[offset]);
            if (name == "__repr__") return self.ToString();
            if (name == "__hash__") return self.ContentHash();
            if (args[offset] is not PySlice other) return PyNotImplemented.Instance;
            int op = name switch
            {
                "__eq__" => PyOps.Eq, "__ne__" => PyOps.Ne, "__lt__" => PyOps.Lt,
                "__le__" => PyOps.Le, "__gt__" => PyOps.Gt, _ => PyOps.Ge,
            };
            return self.CompareFields(op, other);
        }
    }
}
