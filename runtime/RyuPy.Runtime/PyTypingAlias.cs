using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>有限参数化typing别名：保留origin/args、结构相等及哈希，不用于isinstance运行时类型检查。</summary>
public sealed class PyTypingAlias(string name, object origin, object?[] arguments) : PyNativeObject, IPyNativeCallable
{
    /// <summary>typing入口名称。</summary>
    public string Name { get; } = name;
    /// <summary>底层构造器或Union特殊形式。</summary>
    public object Origin { get; } = origin;
    /// <summary>内部参数快照，不通过Python属性暴露可变数组。</summary>
    internal object?[] Arguments { get; } = (object?[])arguments.Clone();
    /// <inheritdoc />
    public override string TypeName => Name == "Union" ? "_UnionGenericAlias" : "_GenericAlias";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__origin__" => Origin, "__args__" => new PyTuple((object?[])Arguments.Clone()),
        "__module__" => "typing", "__name__" => Name,
        "__eq__" => PyNativeFunctions.Create("__eq__", ["other"], [null], 1, args => Equal(args[0])),
        "__hash__" => PyNativeFunctions.Create("__hash__", [], [], 0, _ => Hash()),
        "__getitem__" => PyNativeFunctions.Create("__getitem__", ["parameters"], [null], 1,
            _ => throw new PyNotImplementedError("resubscribing parameterized typing aliases is not supported")),
        _ => throw Missing(name),
    };

    /// <summary>get_args的Callable形式把参数前缀还原为list，避免伪造普通tuple形状。</summary>
    public PyTuple PublicArguments()
    {
        if (Name != "Callable") return new PyTuple((object?[])Arguments.Clone());
        var parameters = new object?[Arguments.Length - 1];
        Array.Copy(Arguments, parameters, parameters.Length);
        return new PyTuple([new PyList(parameters), Arguments[^1]]);
    }

    private object Equal(object? other)
    {
        if (other is not PyTypingAlias alias) return PyNotImplemented.Instance;
        if (Name != alias.Name || Arguments.Length != alias.Arguments.Length) return false;
        if (Name != "Union")
        {
            for (int i = 0; i < Arguments.Length; i++) if (!PyHash.Equal(Arguments[i], alias.Arguments[i])) return false;
            return true;
        }
        foreach (object? arg in Arguments)
        {
            bool found = false;
            foreach (object? candidate in alias.Arguments) if (PyHash.Equal(arg, candidate)) { found = true; break; }
            if (!found) return false;
        }
        return true;
    }

    private long Hash()
    {
        if (Name != "Union") return PyHash.Of(new PyTuple([Name, new PyTuple(Arguments)]));
        long hash = 0;
        foreach (object? arg in Arguments) hash ^= PyHash.Of(arg);
        return hash == -1 ? -2 : hash;
    }

    /// <inheritdoc />
    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        if (!char.IsLower(Name[0])) throw new PyTypeError($"Cannot instantiate typing.{Name}");
        return PyOps.CallValue(Origin, args, names, values);
    }

    /// <inheritdoc />
    public override string PythonRepr()
    {
        var parts = new List<string>();
        foreach (object? arg in Arguments) parts.Add(PyTyping.Display(arg));
        if (Name == "Union" && Arguments.Length == 2)
        {
            int none = Arguments[0] is PyType { Name: "NoneType" } ? 0 : Arguments[1] is PyType { Name: "NoneType" } ? 1 : -1;
            if (none >= 0) return "typing.Optional[" + parts[1 - none] + "]";
        }
        if (Name == "Callable")
            return "typing.Callable[[" + string.Join(", ", parts.GetRange(0, parts.Count - 1)) + "], " + parts[^1] + "]";
        string prefix = char.IsLower(Name[0]) ? "" : "typing.";
        if (Name == "Tuple" && parts.Count == 0) return "typing.Tuple[()]";
        return prefix + Name + "[" + string.Join(", ", parts) + "]";
    }
}
