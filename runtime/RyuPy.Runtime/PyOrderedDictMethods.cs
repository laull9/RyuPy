// OrderedDict 独立描述符与绑定器：继承 dict 的元数据保持真实，所有绑定错误先于用户回调。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;

internal static class PyOrderedDictMethods
{
    internal static readonly string[] Names = ["__init__", "__new__", "__len__", "__iter__", "__reversed__",
        "__getitem__", "__setitem__", "__delitem__", "__contains__", "__eq__", "__ne__", "__lt__", "__le__", "__gt__", "__ge__",
        "__repr__", "__str__", "__hash__", "get", "pop", "setdefault", "update", "clear", "copy", "__copy__",
        "keys", "values", "items", "move_to_end", "popitem", "fromkeys", "__or__", "__ror__", "__ior__"];
    private static readonly HashSet<string> Members = new(Names, StringComparer.Ordinal);
    private static readonly object Absent = new();
    internal static bool Has(string name) => Members.Contains(name);
    internal static object Unbound(string name) => Inherited(name) ? PyDictDescriptors.Unbound(name) : new Method(name, null);
    internal static object Bind(PyOrderedDict self, string name) => new Method(name, self);
    private static bool Inherited(string name) => name is "__len__" or "__getitem__" or "__contains__" or "get" or "__str__";
    private static bool Named(string name) => !name.StartsWith("__", StringComparison.Ordinal)
        || name is "__getitem__" or "__contains__" or "__reversed__" or "__copy__";
    private static string Owner(string name) => name == "__str__" ? "object" : Inherited(name) ? "dict" : "OrderedDict";

    private sealed class Method(string name, PyOrderedDict? receiver) : PyNativeObject, IPyNativeCallable
    {
        private bool ClassBound => name == "fromkeys";
        public override string TypeName => ClassBound ? "builtin_function_or_method" : receiver is null
            ? Named(name) ? "method_descriptor" : "wrapper_descriptor"
            : Named(name) ? "builtin_function_or_method" : "method-wrapper";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name,
            "__qualname__" => (receiver is not null && Named(name) ? "OrderedDict" : Owner(name)) + "." + name,
            "__class__" => PyType.Get(TypeName),
            "__self__" when ClassBound => PyOrderedDict.Type,
            "__self__" when receiver is not null => receiver,
            "__objclass__" when !ClassBound && (receiver is null || !Named(name)) => Owner(name) switch
            { "dict" => PyType.Get("dict"), "object" => PyBaseObject.Type, _ => PyOrderedDict.Type },
            _ => throw Missing(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (ClassBound)
            {
                object?[] bound = Keywords(name, ["iterable", "value"], [Absent, PyNone.Instance], 1, args, names, values);
                return PyOps.OrderedDictFromKeys(bound[0], bound[1]);
            }
            if (name == "__new__") throw PyOrderedDict.Unsupported("explicit allocator");
            if (receiver is null && args.Length == 0)
                throw Error(Named(name) ? $"unbound method OrderedDict.{name}() needs an argument"
                    : $"descriptor '{name}' of 'collections.OrderedDict' object needs an argument");
            object? self = receiver ?? args[0];
            if (self is not PyOrderedDict ordered)
                throw Error(Named(name)
                    ? $"descriptor '{name}' for 'collections.OrderedDict' objects doesn't apply to a '{PyRepr.TypeName(self)}' object"
                    : $"descriptor '{name}' requires a 'collections.OrderedDict' object but received a '{PyRepr.TypeName(self)}'");
            object?[] rest = receiver is null ? args[1..] : args;
            if (name is "update" or "__init__")
                return PyOps.OrderedDictUpdate(ordered, rest, names, values, name == "__init__");
            if (name is "move_to_end" or "pop" or "setdefault" or "popitem")
            {
                object?[] bound = name == "popitem"
                    ? Keywords(name, ["last"], [true], 0, rest, names, values)
                    : Keywords(name, ["key", name == "move_to_end" ? "last" : "default"],
                        [Absent, name == "move_to_end" ? true : name == "pop" ? Absent : PyNone.Instance], 1, rest, names, values);
                return name switch
                {
                    "popitem" => ordered.PopItem(bound[0]),
                    "pop" => ordered.Pop(bound[0], !ReferenceEquals(bound[1], Absent), bound[1]),
                    "setdefault" => ordered.SetDefault(bound[0], bound[1]),
                    _ => Move(ordered, bound),
                };
            }
            Positional(name, rest, names);
            return name switch
            {
                "__len__" => (long)ordered.Count,
                "__iter__" => new PyOrderedDictIterator(ordered, PyDictViewKind.Keys),
                "__reversed__" => new PyOrderedDictIterator(ordered, PyDictViewKind.Keys, true),
                "__getitem__" => ordered.Get(rest[0]),
                "__contains__" => ordered.Contains(rest[0]),
                "__setitem__" => Set(ordered, rest),
                "__delitem__" => Delete(ordered, rest[0]),
                "get" => ordered.GetOrDefault(rest[0], rest.Length == 2 ? rest[1] : PyNone.Instance),
                "clear" => Clear(ordered),
                "copy" or "__copy__" => ordered.Copy(),
                "keys" => new PyOrderedDictView(ordered, PyDictViewKind.Keys),
                "values" => new PyOrderedDictView(ordered, PyDictViewKind.Values),
                "items" => new PyOrderedDictView(ordered, PyDictViewKind.Items),
                "__repr__" or "__str__" => ordered.PythonRepr(),
                "__eq__" or "__ne__" => PyOrderedDictComparison.Root(name, ordered, rest[0]),
                "__lt__" or "__le__" or "__gt__" or "__ge__" => PyNotImplemented.Instance,
                _ => throw PyOrderedDict.Unsupported(name),
            };
        }
    }
    private static object Move(PyOrderedDict self, object?[] args) { self.Move(args[0], args[1]); return PyNone.Instance; }
    private static object Set(PyOrderedDict self, object?[] args) { self.Set(args[0], args[1]); return PyNone.Instance; }
    private static object Delete(PyOrderedDict self, object? key) { self.Delete(key); return PyNone.Instance; }
    private static object Clear(PyOrderedDict self) { self.Clear(); return PyNone.Instance; }
    private static void Positional(string name, object?[] args, string[] names)
    {
        bool named = Named(name);
        string owner = Inherited(name) ? "dict" : "OrderedDict";
        if (names.Length != 0) throw Error(named ? $"{owner}.{name}() takes no keyword arguments"
            : $"wrapper {name}() takes no keyword arguments");
        if (name == "get")
        {
            if (args.Length < 1) throw Error($"get expected at least 1 argument, got {args.Length}");
            if (args.Length > 2) throw Error($"get expected at most 2 arguments, got {args.Length}");
            return;
        }
        if (name is "__getitem__" or "__contains__")
        {
            if (args.Length != 1) throw Error($"dict.{name}() takes exactly one argument ({args.Length} given)");
            return;
        }
        int count = named || name is "__len__" or "__iter__" or "__repr__" or "__str__" ? 0 : name == "__setitem__" ? 2 : 1;
        if (args.Length == count) return;
        if (named) throw Error($"OrderedDict.{name}() takes no arguments ({args.Length} given)");
        throw Error($"{(name == "__setitem__" ? " " : "")}expected {count} argument{(count == 1 ? "" : "s")}, got {args.Length}");
    }
    // CP 的旧原生 binder：先总参数数，再必需参数，再重复/非法 keyword；不提前执行 truth。
    private static object?[] Keywords(string method, string[] parameters, object?[] defaults, int required,
        object?[] args, string[] names, object?[] values)
    {
        int count = args.Length + names.Length;
        if (count > parameters.Length)
            throw Error($"{method}() takes at most {parameters.Length} argument{(parameters.Length == 1 ? "" : "s")} ({count} given)");
        for (int i = 0; i < required; i++)
            if (i >= args.Length && Array.IndexOf(names, parameters[i]) < 0)
                throw Error($"{method}() missing required argument '{parameters[i]}' (pos {i + 1})");
        var bound = (object?[])defaults.Clone();
        Array.Copy(args, bound, args.Length);
        for (int i = 0; i < names.Length; i++)
        {
            int position = Array.IndexOf(parameters, names[i]);
            if (position < 0) throw Error($"'{names[i]}' is an invalid keyword argument for {method}()");
            if (position < args.Length)
                throw Error($"argument for {method}() given by name ('{names[i]}') and position ({position + 1})");
            for (int prior = 0; prior < i; prior++)
                if (names[prior] == names[i]) throw Error($"{method}() got multiple values for keyword argument '{names[i]}'");
            bound[position] = values[i];
        }
        return bound;
    }
    private static PyTypeError Error(string message) => PyOrderedDict.Error(new PyTypeError(message));
}
