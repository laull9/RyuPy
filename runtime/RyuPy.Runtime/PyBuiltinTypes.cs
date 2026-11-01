using System;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    private static bool MatchesType(object? value, object? type) => type switch
    {
        PyNativeType native => native.Accepts(value),
        PyClass cls => PyObject.IsInstance(value, cls),
        PyType pyType => MatchesBuiltin(value, pyType.Name),
        _ => throw new PyTypeError(
            $"isinstance() arg 2 must be a type or tuple of types, not "
                + $"'{PyRepr.TypeName(type)}'"
        ),
    };

    /// <summary>内建类型名匹配：覆盖 bool/int 子类型、用户异常链与容器。</summary>
    private static bool MatchesBuiltin(object? value, string expected) => value switch
    {
        PyBaseException exception => exception.PythonClass is not null
            ? exception.PythonClass.IsSubclassOfBuiltin(expected)
            : PyExceptionHierarchy.IsSubclass(exception.PyTypeName, expected),
        PyClass => expected == "type",
        PyType => expected == "type",
        PyBoundMethod => expected == "method",
        PyFunction => expected == "function",
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
        if (args[1] is PyTuple alternatives)
        {
            foreach (object? candidate in alternatives.Items)
            {
                if (IsSubclassOf(args[0], candidate))
                {
                    return true;
                }
            }
            return false;
        }
        return IsSubclassOf(args[0], args[1]);
    }

    private static bool IsSubclassOf(object? child, object? parent) => (child, parent) switch
    {
        (PyNativeType derived, PyNativeType baseType) => derived.IsSubclassOf(baseType),
        (PyNativeType, PyType builtin) => builtin.Name == "object",
        (PyClass derived, PyClass baseClass) => derived.IsSubclassOf(baseClass),
        (PyClass derived, PyType builtin) => derived.IsSubclassOfBuiltin(builtin.Name)
            || builtin.Name == "object",
        (PyType derived, PyType baseType) => derived.Name == baseType.Name
            || (derived.Name == "bool" && baseType.Name == "int")
            || PyExceptionHierarchy.IsSubclass(derived.Name, baseType.Name),
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

    private static bool IsCallable(object? value) => value is PyType { Name: "JSONDecodeError" or "error" } || value is
        PyFunction or PyBuiltinFunction or PyBuiltinMethod or PyNativeType or PyClass or PyBoundMethod or PyStaticMethod or PyClassMethod
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
