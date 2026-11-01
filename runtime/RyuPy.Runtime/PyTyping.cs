using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>typing的有限运行时元数据：保留注解对象，不执行类型检查、前向引用求值或完整泛型推导。</summary>
public static class PyTyping
{
    private static readonly Dictionary<string, PyTypingForm> Forms = new(StringComparer.Ordinal);
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    private static readonly Dictionary<object, PyTypingAlias> Aliases = new(PyKeyComparer.Instance);
    private static readonly Queue<object> AliasOrder = new();
    private static readonly object AliasLock = new();

    /// <summary>稳定元数据对象与辅助API；模块重复读取不重新构造Any/Union等身份。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;

    private static Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?>
        {
            ["TYPE_CHECKING"] = false, ["TypedDict"] = PyTypedDict.Factory,
            ["is_typeddict"] = PyNativeFunctions.Create("is_typeddict", ["tp"], [null], 1,
                args => PyTypedDict.IsTypedDict(args[0])),
        };
        foreach (string name in new[] { "Any", "Optional", "Union", "Literal", "List", "Dict", "Tuple", "Set", "FrozenSet", "Callable", "Iterable", "Iterator", "Sequence", "Mapping", "MutableMapping", "Collection", "Container" })
        {
            var form = new PyTypingForm(name); Forms[name] = form; result[name] = form;
        }
        result["get_origin"] = PyNativeFunctions.Create("get_origin", ["tp"], [null], 1,
            args => args[0] is PyTypingAlias alias ? alias.Origin : args[0] is PyTypingForm form && form.Name is not ("Union" or "Literal") ? Origin(form.Name) : PyNone.Instance);
        result["get_args"] = PyNativeFunctions.Create("get_args", ["tp"], [null], 1,
            args => args[0] is PyTypingAlias alias ? alias.PublicArguments() : new PyTuple([]));
        result["cast"] = PyNativeFunctions.Create("cast", ["typ", "val"], [null, null], 2, args => args[1]);
        return result;
    }

    internal static object Origin(string name) => name switch
    {
        "Union" => Forms["Union"], "Literal" => Forms["Literal"], "Optional" or "Any" => PyNone.Instance,
        "List" => PyType.Get("list"), "Dict" => PyType.Get("dict"), "Tuple" => PyType.Get("tuple"),
        "Set" => PyType.Get("set"), "FrozenSet" => PyType.Get("frozenset"),
        // Callable的ABC身份在collections.abc注册后解析，而非另造同名对象。
        "Callable" or "Iterable" or "Iterator" or "Sequence" or "Mapping" or "MutableMapping" or "Collection" or "Container"
            => PyObject.GetAttr(PyModules.Import("collections.abc"), name)!,
        _ => throw new PyNotImplementedError("unsupported typing form"),
    };

    internal static object Subscribe(string name, object? parameter)
    {
        _ = Attributes.Value;
        if (name == "Any") throw new PyTypeError("type 'Any' is not subscriptable");
        object?[] args = parameter is PyTuple tuple ? tuple.Items : [parameter];
        if (name == "Literal") return Intern(name, name, Forms[name], PyTypingLiteral.Normalize(args));
        if (name == "Optional")
        {
            if (args.Length != 1) throw new PyTypeError("typing.Optional requires a single type");
            return Union([TypeArgument(args[0]), PyType.Get("NoneType")], "Optional");
        }
        if (name == "Union")
        {
            if (args.Length == 0) throw new PyTypeError("Cannot take a Union of no types.");
            var values = new object?[args.Length];
            for (int i = 0; i < args.Length; i++) values[i] = TypeArgument(args[i]);
            return Union(values, "Union");
        }
        if (name == "Callable")
        {
            if (args.Length != 2 || args[0] is not PyList parameters)
                throw new PyNotImplementedError("Callable currently requires an explicit list of argument types");
            var callArgs = new object?[parameters.Count + 1];
            for (int i = 0; i < parameters.Count; i++) callArgs[i] = TypeArgument(parameters.Items[i]);
            callArgs[^1] = TypeArgument(args[1]);
            return Intern(name, name, Origin(name), callArgs);
        }
        if (name == "Tuple" && Array.Exists(args, value => value is PyEllipsis))
        {
            if (args.Length != 2 || args[1] is not PyEllipsis)
                throw new PyNotImplementedError("typing.Tuple currently supports Ellipsis only as Tuple[T, ...]");
            return Intern(name, name, Origin(name), [TypeArgument(args[0]), PyEllipsis.Instance]);
        }
        int required = name is "Dict" or "Mapping" or "MutableMapping" ? 2 : name == "Tuple" ? -1 : 1;
        if (required >= 0 && args.Length != required)
            throw new PyTypeError($"typing.{name} requires {required} type argument(s)");
        var normalized = new object?[args.Length];
        for (int i = 0; i < args.Length; i++) normalized[i] = TypeArgument(args[i]);
        return Intern(name, name, Origin(name), normalized);
    }

    private static object Union(object?[] args, string cacheName)
    {
        var result = new List<object?>();
        foreach (object? arg in args)
        {
            object?[] inner = arg is PyTypingAlias { Name: "Union" } union ? union.Arguments : [arg];
            foreach (object? item in inner)
            {
                bool duplicate = false;
                foreach (object? existing in result) if (PyHash.Equal(existing, item)) { duplicate = true; break; }
                if (!duplicate) result.Add(item);
            }
        }
        return result.Count == 1 ? result[0]! : Intern(cacheName, "Union", Forms["Union"], result.ToArray());
    }

    /// <summary>内建容器/ABC的有限PEP585订阅；元数据不能当作isinstance类型。</summary>
    public static object SubscribeNative(object origin, object? parameter)
    {
        object?[] args = parameter is PyTuple tuple ? tuple.Items : [parameter];
        var normalized = new object?[args.Length];
        for (int i = 0; i < args.Length; i++) normalized[i] = TypeArgument(args[i]);
        string name = origin switch
        {
            PyType type => type.Name,
            PyNativeType type => type.Module + "." + type.Name,
            _ => throw new PyTypeError("not a supported generic type"),
        };
        return Intern("native:" + name, name, origin, normalized);
    }

    private static PyTypingAlias Intern(string cacheName, string name, object origin, object?[] args)
    {
        object key = new PyTuple([cacheName, name == "Literal" ? PyTypingLiteral.Key(args) : new PyTuple(args)]);
        lock (AliasLock)
        {
            if (Aliases.TryGetValue(key, out PyTypingAlias? existing)) return existing;
            var result = new PyTypingAlias(name, origin, args);
            if (Aliases.Count == 128) Aliases.Remove(AliasOrder.Dequeue());
            Aliases.Add(key, result); AliasOrder.Enqueue(key); return result;
        }
    }

    private static object TypeArgument(object? value) => value switch
    {
        null or PyNone => PyType.Get("NoneType"),
        PyTypingForm { Name: "Optional" or "Union" or "Literal" } => throw new PyTypeError("Plain special form is not valid as a type argument"),
        PyType or PyClass or PyNativeType or PyTypingAlias or PyTypingForm or PyTypedDictType => value,
        string => throw new PyNotImplementedError("typing forward references are not supported yet"),
        _ => throw new PyTypeError("Parameters to generic types must be types."),
    };

    internal static string Display(object? value) => value switch
    {
        PyEllipsis => "...",
        PyTypedDictType typed => typed.Module + "." + typed.Name,
        PyType type => type.Name,
        PyClass cls => "__main__." + cls.Name,
        PyNativeType type => type.Module + "." + type.Name,
        PyTypingAlias alias => alias.PythonRepr(), PyTypingForm form => form.PythonRepr(),
        _ => PyRepr.Repr(value),
    };
}

/// <summary>typing的稳定特殊形式或未参数化别名；订阅返回独立不可变元数据。</summary>
public sealed class PyTypingForm(string name) : PyNativeObject, IPyNativeCallable
{
    /// <summary>公开形式名称。</summary>
    public string Name { get; } = name;
    /// <inheritdoc />
    public override string TypeName => "_SpecialForm";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__name__" => Name, "__module__" => "typing",
        "__getitem__" => PyNativeFunctions.Create("__getitem__", ["parameters"], [null], 1, args => PyTyping.Subscribe(Name, args[0])),
        _ => throw Missing(name),
    };
    /// <inheritdoc />
    public object? Invoke(object?[] args, string[] names, object?[] values) =>
        throw new PyTypeError($"Type {Name} cannot be instantiated");
    /// <inheritdoc />
    public override string PythonRepr() => "typing." + Name;
}
