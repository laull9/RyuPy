// 类型判定与相关对象内建：保留真实类别及有限父链，未知协议不伪造类型关系。
using System;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    private static bool MatchesType(object? value, object? type) => type switch
    {
        PyTuple tuple => MatchesTuple(value, tuple),
        IPyForeignObject foreign when foreign.IsInstanceOf(PyType.Get("tuple")) => MatchesForeignTuple(value, foreign),
        IPyForeignObject foreign => foreign.IsInstanceOf(PyType.Get("type")) ? foreign.Accepts(value) : throw new PyTypeError("isinstance() arg 2 must be a type, a tuple of types, or a union"),
        PyTypingForm { Name: "NoReturn" } => PyTypingNoReturn.RejectCheck(subclass: false),
        PyTypingForm { Name: "ClassVar" } => PyTypingClassVar.RejectRootCheck(subclass: false),
        PyTypingAlias { Name: "ClassVar" } => PyTypingGeneric.RejectRuntimeAlias(),
        PyTypingGenericAlias => PyTypingGeneric.RejectRuntimeAlias(),
        PyTypedDictType => PyTypedDictType.RejectRuntimeCheck(),
        // 到这里第二参数已经是合法的真实根类型；tuple短路仍由调用方逐项执行。
        PyType root when ReferenceEquals(root, PyBaseObject.Type) => true,
        PyType root when ReferenceEquals(root, PySlice.Type) => value is PySlice,
        PyNativeType native => native.Accepts(value),
        PyClass protocol when ReferenceEquals(protocol, PyDateTimeTzInfo.Root) => value is PyDateTimeZone || PyObject.IsInstance(value, protocol),
        PyClass abc when PyAbc.IsAbc(abc) => PyAbc.Accepts(abc, value),
        PyClass cls => PyObject.IsInstance(value, cls),
        PyType pyType => MatchesBuiltin(value, pyType.Name),
        _ => throw new PyTypeError(
            "isinstance() arg 2 must be a type, a tuple of types, or a union"
        ),
    };

    private static bool MatchesTuple(object? value, PyTuple tuple)
    {
        foreach (object? candidate in tuple.Items)
        {
            if (MatchesType(value, candidate))
            {
                return true;
            }
        }
        return false;
    }

    private static bool MatchesForeignTuple(object? value, IPyForeignObject foreignTuple)
    {
        IPyIterator iterator = foreignTuple.Iterate();
        while (iterator.MoveNext())
        {
            object? candidate = iterator.Current();
            if (MatchesType(value, candidate))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>内建类型名匹配：覆盖 bool/int 子类型、用户异常链与容器。</summary>
    private static bool MatchesBuiltin(object? value, string expected) => value switch
    {
        IPyForeignObject foreign => foreign.IsInstanceOf(PyType.Get(expected)),
        PyBaseException exception => exception.PythonClass is not null
            ? exception.PythonClass.IsSubclassOfBuiltin(expected)
            : PyExceptionHierarchy.IsSubclass(exception.PyTypeName, expected),
        PyTypedDictType => expected is "_TypedDictMeta" or "type" or "object",
        PyEllipsis => expected is "ellipsis" or "object",
        PyNamedTupleInstance => expected is "tuple" or "object",
        PyUrlResult => expected is "tuple" or "object",
        IPyNativeMapping => expected is "dict" or "object",
        PyNativeObject native => PyNativeType.LookupByName(native.TypeName)?.IsSubclassOfBuiltin(expected)
            ?? (string.Equals(native.TypeName, expected, StringComparison.Ordinal)
                // 仅补 Callable 的已证直接父边；原生类型登记与同名命中仍优先。
                || native is PyTypingAlias { Name: "Callable" }
                    && PyTyping.IsCallableAliasBase(native.TypeName, expected)),
        PyClass => expected == "type",
        PyType => expected == "type",
        PyBoundMethod => expected == "method",
        PyFunction => expected == "function",
        PyListInstance => expected is "list" or "object",
        PyDictInstance => expected is "dict" or "object",
        PyStringInstance => expected is "str" or "object",
        PyInstance instance => expected == "object"
            || string.Equals(instance.Class.Name, expected, StringComparison.Ordinal),
        _ => string.Equals(PyRepr.TypeName(value), expected, StringComparison.Ordinal)
            || (expected == "int" && PyRepr.TypeName(value) == "bool"),
    };

    private static object BuiltinIsSubclass(object?[] args, string[] names)
    {
        RejectUnknownKeywords("issubclass", names);
        if (args.Length != 2)
        {
            throw new PyTypeError($"issubclass expected 2 arguments, got {args.Length}");
        }
        return IsSubclassOf(args[0], args[1]);
    }

    private static bool IsSubclassOf(object? child, object? parent) => parent switch
    {
        PyTuple tuple => IsSubclassOfTuple(child, tuple),
        IPyForeignObject foreign when foreign.IsInstanceOf(PyType.Get("tuple")) => IsSubclassOfForeignTuple(child, foreign),
        _ => IsSubclassOfCore(child, parent),
    };

    private static bool IsSubclassOfTuple(object? child, PyTuple tuple)
    {
        foreach (object? candidate in tuple.Items)
        {
            if (IsSubclassOf(child, candidate))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsSubclassOfForeignTuple(object? child, IPyForeignObject foreignTuple)
    {
        IPyIterator iterator = foreignTuple.Iterate();
        while (iterator.MoveNext())
        {
            object? candidate = iterator.Current();
            if (IsSubclassOf(child, candidate))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsSubclassOfCore(object? child, object? parent) => (child, parent) switch
    {
        (_, PyTypingForm { Name: "NoReturn" }) => PyTypingNoReturn.RejectCheck(subclass: true),
        (_, PyTypingForm { Name: "ClassVar" }) => PyTypingClassVar.RejectRootCheck(subclass: true),
        (_, PyTypingAlias { Name: "ClassVar" }) => PyTypingGeneric.RejectRuntimeAlias(),
        (_, PyTypingGenericAlias) => PyTypingGeneric.RejectRuntimeAlias(),
        (PyClass { IsTypingIoFamily: true }, PyClass { IsTypingGenericRoot: true }) =>
            throw PyTypingGeneric.Unsupported("typing IO Generic ancestry is not supported yet"),
        (PyType or PyNativeType, PyClass { IsTypingGenericRoot: true }) => false,
        (_, PyClass { IsTypingGenericRoot: true }) when child is not PyClass =>
            throw PyTypingGeneric.Error(new PyTypeError("issubclass() arg 1 must be a class")),
        (_, PyTypedDictType) => PyTypedDictType.RejectRuntimeCheck(),
        (PyClass or PyType or PyNativeType or PyTypedDictType, PyType root)
            when ReferenceEquals(root, PyBaseObject.Type) => true,
        (PyTypedDictType, PyType { Name: "dict" or "object" }) => true,
        (PyTypedDictType, _) => IsSubclassOf(PyType.Get("dict"), parent),
        (PyType { Name: "_TypedDictMeta" }, PyType { Name: "type" or "object" }) => true,
        (PyType { Name: "ellipsis" }, PyType { Name: "object" }) => true,
        (_, PyClass abc) when PyAbc.IsAbc(abc) => PyAbc.IsSubclass(child, abc),
        (PyNativeType derived, PyClass protocol) when derived.IsIdentitySubclassOf(protocol) => true,
        (PyNativeType derived, PyNativeType baseType) => derived.IsSubclassOf(baseType),
        (PyNativeType derived, PyType builtin) => derived.IsSubclassOfBuiltin(builtin.Name),
        // 普通真实类不能继承有独立native载体的类型；无关系不是非法class参数。
        (PyClass, PyNativeType) => false,
        (PyClass derived, PyClass baseClass) => derived.IsSubclassOf(baseClass),
        (PyType or PyNativeType or PyTypedDictType, PyClass target) when target.IsTypingIoFamily => false,
        (_, PyClass target) when target.IsTypingIoFamily => throw new PyTypeError("issubclass() arg 1 must be a class"),
        (PyType or PyNativeType, PyClass) => false,
        (PyClass derived, PyType builtin) => (derived.HasStringLayout && ReferenceEquals(builtin, PyType.Get("str")))
            || (derived.HasListLayout && ReferenceEquals(builtin, PyType.Get("list")))
            || (derived.HasDictLayout && ReferenceEquals(builtin, PyType.Get("dict")))
            || (derived.NamedTupleLayout is not null && ReferenceEquals(builtin, PyType.Get("tuple")))
            || derived.IsSubclassOfBuiltin(builtin.Name)
            || builtin.Name == "object",
        (PyType derived, PyType baseType) => derived.Name == baseType.Name
            || (derived.Name == "bool" && baseType.Name == "int")
            || PyExceptionHierarchy.IsSubclass(derived.Name, baseType.Name)
            // 已有同名、数值和异常关系早返；不在普通成功路径增加 typing 查表。
            || (derived.Name == "_CallableGenericAlias" && PyTyping.IsCallableAliasBase(derived.Name, baseType.Name)),
        _ => throw new PyTypeError(
            "issubclass() arg 1 must be a class, arg 2 must be a class or tuple of classes"
        ),
    };

    private static object BuiltinProperty(object?[] args, string[] names)
    {
        RejectUnknownKeywords("property", names);
        return args.Length switch
        {
            1 => new PyProperty(args[0]),
            2 => new PyProperty(args[0], args[1]),
            _ => throw new PyTypeError(
                $"property expected at most 2 arguments, got {args.Length}"
            ),
        };
    }

    private static object? BuiltinDescriptorWrapper(
        object?[] args,
        string[] names,
        string name,
        Func<object?, object> wrap
    )
    {
        RejectUnknownKeywords(name, names);
        return wrap(Single(name, args));
    }

    // 本族真实普通实例只由类型链的调用槽决定；实例同名字段不改变 callable。
    private static bool IsCallable(object? value) => value is PyInstance instance && (instance.Class.IsTypingIoFamily || instance.Class.IsTypingGenericFamily)
        ? instance.Class.TryLookup("__call__", out _, out _) : value is PyType type
        && (IsBuiltinConstructor(type.Name) || PyExceptionTypes.IsKnown(type.Name) || PyOptParseExceptions.IsType(type.Name) || type.Name is "JSONDecodeError" or "GetoptError" or "BadZipFile" or "error" or "UnsupportedOperation" or "ellipsis" or "_TypedDictMeta") || value is
        IPyNativeCallable or PyFunction or PyBuiltinFunction or PyBuiltinMethod or PyNativeType or PyClass or PyBoundMethod or PyStaticMethod or PyClassMethod
        or PyInstance;
    private static object? BuiltinGetAttr(object?[] args, string[] names)
    {
        RejectUnknownKeywords("getattr", names);
        if (args.Length is < 2 or > 3)
        {
            throw new PyTypeError($"getattr expected 2 or 3 arguments, got {args.Length}");
        }
        string attr = args[1] as string
            ?? throw new PyTypeError("attribute name must be string");
        try
        {
            return PyObject.GetAttr(args[0], attr) ?? PyNone.Instance;
        }
        catch (PyAttributeError) when (args.Length == 3)
        {
            return args[2];
        }
    }

    private static object BuiltinHasAttr(object?[] args, string[] names)
    {
        RejectUnknownKeywords("hasattr", names);
        if (args.Length != 2)
        {
            throw new PyTypeError($"hasattr expected 2 arguments, got {args.Length}");
        }
        string attr = args[1] as string
            ?? throw new PyTypeError("attribute name must be string");
        try
        {
            PyObject.GetAttr(args[0], attr);
            return true;
        }
        catch (PyAttributeError)
        {
            return false;
        }
    }

    private static object? BuiltinSetAttr(object?[] args, string[] names)
    {
        RejectUnknownKeywords("setattr", names);
        if (args.Length != 3)
        {
            throw new PyTypeError($"setattr expected 3 arguments, got {args.Length}");
        }
        string attr = args[1] as string
            ?? throw new PyTypeError("attribute name must be string");
        PyObject.SetAttr(args[0], attr, args[2]);
        return PyNone.Instance;
    }
}
