using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>typing的有限运行时元数据：保留注解对象，不执行类型检查、前向引用求值或完整泛型推导。</summary>
public static partial class PyTyping
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
            ["Generic"] = PyTypingGeneric.Root, ["TYPE_CHECKING"] = false, ["TypedDict"] = PyTypedDict.Factory, ["TypeVar"] = PyTypingTypeVar.Type,
            ["is_typeddict"] = PyNativeFunctions.Create("is_typeddict", ["tp"], [null], 1,
                args => PyTypedDict.IsTypedDict(args[0])),
        };
        foreach (string name in new[] { "Any", "NoReturn", "Optional", "Union", "ClassVar", "Literal", "List", "Dict", "Tuple", "Set", "FrozenSet", "Callable", "Iterable", "Iterator", "Sequence", "Mapping", "MutableMapping", "Collection", "Container" })
        {
            var form = new PyTypingForm(name); Forms[name] = form; result[name] = form;
        }
        result["get_origin"] = PyNativeFunctions.Create("get_origin", ["tp"], [null], 1,
            args => GetOrigin(args[0]));
        result["get_args"] = PyNativeFunctions.Create("get_args", ["tp"], [null], 1,
            args => GetArguments(args[0]));
        result["cast"] = PyNativeFunctions.Create("cast", ["typ", "val"], [null, null], 2, args => args[1]);
        // 工厂先完成整个 IO 族；模块惰性表只在三根均可用后一次发布真实身份。
        foreach (string name in new[] { "IO", "BinaryIO", "TextIO" }) result[name] = PyTypingIo.Root(name);
        result["NamedTuple"] = PyTypingNamedTuple.Root;
        PyTypingOverload.Install(result);
        return result;
    }

    // alias 分支在读取 Generic 全局之前早返；普通类型查询才消费当前导出。
    private static object GetOrigin(object? value)
    {
        GuardHelper("_AnnotatedAlias"); GuardHelper("_BaseGenericAlias");
        GuardHelper("GenericAlias"); GuardHelper("ParamSpecArgs"); GuardHelper("ParamSpecKwargs");
        if (value is PyTypingGenericAlias) return PyTypingGeneric.Root;
        if (value is PyTypingAlias alias) return alias.Origin;
        if (value is PyTypingForm form && form.Name is not ("Any" or "NoReturn" or "Optional" or "Union" or "ClassVar" or "Literal")) return Origin(form.Name);
        PyTypingGeneric.GuardExport(Attributes.Value);
        if (PyTypingGeneric.IsRoot(value)) return PyTypingGeneric.Root;
        GuardHelper("types");
        return PyNone.Instance;
    }

    // 冷建类协议读取原始身份；公开模块字典可写，不能用其当前值冒充canonical。
    internal static PyTypingForm OriginalForm(string name)
    {
        _ = Attributes.Value;
        return Forms[name];
    }

    internal static object Origin(string name) => name switch
    {
        "Union" => Forms["Union"], "Literal" => Forms["Literal"], "Optional" or "Any" => PyNone.Instance,
        "List" => PyType.Get("list"), "Dict" => PyType.Get("dict"), "Tuple" => PyType.Get("tuple"),
        "Set" => PyType.Get("set"), "FrozenSet" => PyType.Get("frozenset"),
        // Callable的ABC身份在collections.abc注册后解析，而非另造同名对象。
        "Callable" or "Iterable" or "Iterator" or "Sequence" or "Mapping" or "MutableMapping" or "Collection" or "Container"
            => PyAbc.RootType(name),
        _ => throw new PyNotImplementedError("unsupported typing form"),
    };

    internal static object Subscribe(PyTypingForm form, object? parameter)
    {
        string name = form.Name;
        if (name is "Optional" or "Union" or "ClassVar" or "NoReturn") return PyTypingSpecialFormCache.Subscribe(form, parameter);
        if (form.IsOrdinaryGeneric) return OrdinaryCache.Subscribe(form, parameter);
        if (name == "Tuple") return TupleCache.Subscribe(form, parameter);
        if (name == "Callable") return CallableSubscribe(form, parameter);
        _ = Attributes.Value;
        if (name == "Any") throw new PyTypeError("type 'Any' is not subscriptable");
        object?[] args = parameter is PyTuple tuple ? tuple.Items : [parameter];
        if (name == "Literal") return Intern(name, name, Forms[name], PyTypingLiteral.Normalize(args));
        int required = name is "Dict" or "Mapping" or "MutableMapping" ? 2 : 1;
        if (required >= 0 && args.Length != required)
            throw new PyTypeError($"typing.{name} requires {required} type argument(s)");
        var normalized = new object?[args.Length];
        for (int i = 0; i < args.Length; i++) normalized[i] = TypeArgument(args[i]);
        return Intern(name, name, Origin(name), normalized);
    }

    /// <summary>内建容器/ABC的有限PEP585订阅；元数据不能当作isinstance类型。</summary>
    public static object SubscribeNative(object origin, object? parameter)
    {
        if (origin is PyClass abc && PyAbc.IsAbc(abc))
        {
            if (abc.Name == "Sized") throw new PyTypeError("type 'Sized' is not subscriptable");
            if (abc.Name == "Callable")
            {
                if (parameter is not PyTuple { Items.Length: 2 } callable || callable.Items[0] is not (PyList or PyTuple or PyEllipsis))
                    throw new PyTypeError("Callable must be used as Callable[[arg, ...], result]");
                throw new PyNotImplementedError("collections.abc.Callable generic aliases are not supported");
            }
        }
        object?[] args = parameter is PyTuple tuple ? tuple.Items : [parameter];
        var normalized = new object?[args.Length];
        for (int i = 0; i < args.Length; i++) normalized[i] = TypeArgument(args[i]);
        string name = origin switch
        {
            PyType type => type.Name,
            PyNativeType type => type.Module + "." + type.Name,
            PyClass type when PyAbc.IsAbc(type) => "collections.abc." + type.Name,
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

    private static object TypeArgument(object? value)
    {
        PyTypingGeneric.GuardExport(Attributes.Value);
        PyTypingGeneric.ValidateTypeArgument(value);
        return TypeArgumentCore(value);
    }

    private static object TypeArgumentCore(object? value) => value switch
    {
        null or PyNone => PyType.Get("NoneType"),
        PyTypingForm { Name: "Optional" or "Union" or "Literal" } => throw new PyTypeError("Plain special form is not valid as a type argument"),
        PyType or PyClass or PyNativeType or PyTypingAlias or PyTypingForm or PyTypedDictType or PyTypingTypeVar => value,
        string => throw new PyNotImplementedError("typing forward references are not supported yet"),
        _ => throw new PyTypeError("Parameters to generic types must be types."),
    };

    internal static string Display(object? value) => value switch
    {
        PyEllipsis => "...",
        PyTypedDictType typed => typed.Module + "." + typed.Name,
        PyType type => type.Name,
        PyClass cls => cls.IsTypingIoFamily ? PyTypingIo.Display(cls) : "__main__." + cls.Name,
        PyNativeType type => type.Module + "." + type.Name,
        PyTypingAlias alias => alias.PythonRepr(), PyTypingForm form => form.PythonRepr(),
        PyTypingTypeVar variable => variable.PythonRepr(),
        _ => PyRepr.Repr(value),
    };
}

/// <summary>typing的稳定特殊形式或未参数化别名；订阅返回独立不可变元数据。</summary>
public sealed class PyTypingForm(string name) : PyNativeObject, IPyNativeCallable
{
    /// <summary>公开形式名称。</summary>
    public string Name { get; } = name;
    // 只有这些根继承同一个 _SpecialGenericAlias 订阅入口；Tuple/Callable 各有覆盖。
    internal bool IsOrdinaryGeneric { get; } = name is "List" or "Dict" or "Set" or "FrozenSet"
        or "Iterable" or "Iterator" or "Sequence" or "Mapping" or "MutableMapping" or "Collection" or "Container";
    /// <inheritdoc />
    public override string TypeName => Name switch
    {
        "Any" => "_AnyMeta", "Literal" => "_LiteralSpecialForm",
        "Optional" or "Union" or "ClassVar" or "NoReturn" => "_SpecialForm", "Tuple" => "_TupleType",
        "Callable" => "_CallableType", _ => "_SpecialGenericAlias",
    };
    /// <inheritdoc />
    public override object? GetAttr(string name) => Name == "NoReturn" ? PyTypingNoReturn.GetAttr(this, name) : name switch
    {
        "__class__" => PyObject.TypeOf(this),
        "__name__" or "__qualname__" => Name, "__module__" => "typing",
        "__getitem__" => PyNativeFunctions.Create("__getitem__", ["parameters"], [null], 1, args => PyTyping.Subscribe(this, args[0])),
        "__mro_entries__" when Name == "ClassVar" => PyNativeFunctions.Create(name, ["bases"], [null], 1,
            _ => PyTypingClassVar.RejectBase(this)),
        // 两个静态委托不捕获 name，避免所有普通 GetAttr 调用被提升为闭包分配。
        "__instancecheck__" when Name == "ClassVar" => PyNativeFunctions.Create(name, ["cls"], [null], 1,
            static _ => PyTypingClassVar.RejectRootCheck(subclass: false)),
        "__subclasscheck__" when Name == "ClassVar" => PyNativeFunctions.Create(name, ["cls"], [null], 1,
            static _ => PyTypingClassVar.RejectRootCheck(subclass: true)),
        "__call__" when Name == "ClassVar" => PyTypingClassVar.CallMethod(this),
        "_name" or "_getitem" or "__dict__" => throw new PyNotImplementedError("typing root internal slot reflection is not supported"),
        _ when Name == "ClassVar" => throw new PyAttributeError(name),
        _ => throw Missing(name),
    };
    /// <inheritdoc />
    public override void SetAttr(string name, object? value) =>
        throw new PyNotImplementedError("typing root attribute mutation is not supported");
    /// <inheritdoc />
    public override void DelAttr(string name) =>
        throw new PyNotImplementedError("typing root attribute deletion is not supported");
    /// <inheritdoc />
    public object? Invoke(object?[] args, string[] names, object?[] values) =>
        throw (Name == "NoReturn" ? PyTypingNoReturn.CannotInstantiate() : Name == "ClassVar" ? PyTypingClassVar.Error(new PyTypeError("Cannot instantiate typing.ClassVar"))
            : new PyTypeError($"Type {Name} cannot be instantiated"));
    /// <inheritdoc />
    public override string PythonRepr() => "typing." + Name;
}
