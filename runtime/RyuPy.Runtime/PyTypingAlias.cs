using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>有限参数化typing别名：保留origin/args、结构相等及哈希，不用于isinstance运行时类型检查。</summary>
public sealed class PyTypingAlias(string name, object origin, object?[] arguments) : PyNativeObject, IPyNativeCallable
{
    /// <summary>typing入口名称。</summary>
    public string Name { get; } = name;
    // 同一载体也用于原生 PEP585；它不参与 typing._GenericAlias 的 origin 禁止集合。
    internal bool IsNativeAlias => char.IsLower(Name[0]);
    /// <summary>底层构造器或Union特殊形式。</summary>
    public object Origin { get; } = origin;
    /// <summary>内部参数快照，不通过Python属性暴露可变数组。</summary>
    internal object?[] Arguments { get; } = (object?[])arguments.Clone();
    // 冷反射首次请求才收集参数，旧 concrete alias 创建路径不新增容器分配。
    private static readonly PyTuple EmptyParameters = new([]);
    private sealed class Metadata
    {
        internal PyTuple? Arguments;
        internal PyTuple? Parameters;
    }
    // 复用原 _parameters 的单引用槽；只有反射才创建 holder，不扩大旧 alias 冷布局。
    private Metadata? _metadata;
    private Metadata Reflection => _metadata ??= new();
    internal PyTuple Parameters => Reflection.Parameters ??= CollectParameters(Arguments);
    private PyTuple ArgumentTuple => Reflection.Arguments ??= new PyTuple((object?[])Arguments.Clone());
    private static PyTuple CollectParameters(object?[] arguments)
    {
        List<object?>? ordered = null;
        HashSet<object>? seen = null;
        foreach (object? argument in arguments)
        {
            if (argument is PyTypingTypeVar variable) AddParameter(variable, ref ordered, ref seen);
            else if (argument is PyTypingAlias alias)
                foreach (object? parameter in alias.Parameters.Items) AddParameter(parameter!, ref ordered, ref seen);
        }
        return ordered is null ? EmptyParameters : new PyTuple(ordered.ToArray());
    }

    // 首个变量才分配；引用去重为线性成本，绝不执行变量 extra 中的 eq/hash。
    private static void AddParameter(object variable, ref List<object?>? ordered, ref HashSet<object>? seen)
    {
        seen ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!seen.Add(variable)) return;
        (ordered ??= new List<object?>()).Add(variable);
    }
    /// <inheritdoc />
    public override string TypeName => Name switch
    {
        "Union" => "_UnionGenericAlias", "Literal" => "_LiteralGenericAlias",
        "Callable" => "_CallableGenericAlias", _ => "_GenericAlias",
    };
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => PyObject.TypeOf(this),
        "__origin__" => Origin, "__args__" => ArgumentTuple,
        "__parameters__" => Parameters,
        "__module__" => "typing", "__name__" => Name,
        "__qualname__" when Name == "ClassVar" => Name,
        "__eq__" => PyNativeFunctions.Create("__eq__", ["other"], [null], 1, args => Equal(args[0])),
        "__hash__" => PyNativeFunctions.Create("__hash__", [], [], 0, _ => Hash()),
        "__getitem__" => PyNativeFunctions.Create("__getitem__", ["parameters"], [null], 1,
            args => Name == "ClassVar" ? PyTypingClassVar.Resubscribe(this, args[0])
                : throw new PyNotImplementedError("resubscribing parameterized typing aliases is not supported")),
        "__mro_entries__" when Name == "ClassVar" => PyNativeFunctions.Create(name, ["bases"], [null], 1,
            _ => PyTypingClassVar.RejectBase(this)),
        "__instancecheck__" or "__subclasscheck__" when Name == "ClassVar" => PyNativeFunctions.Create(name, ["cls"], [null], 1,
            _ => PyTypingGeneric.RejectRuntimeAlias()),
        "__call__" when Name == "ClassVar" => PyTypingClassVar.CallMethod(this),
        "_name" or "_inst" or "__dict__" when Name == "ClassVar" =>
            throw new PyNotImplementedError("ClassVar alias internal slot reflection is not supported"),
        _ when Name == "ClassVar" => throw new PyAttributeError(name),
        _ => throw Missing(name),
    };

    /// <summary>get_args的Callable形式把参数前缀还原为list，避免伪造普通tuple形状。</summary>
    public PyTuple PublicArguments()
    {
        if (Name != "Callable") return ArgumentTuple;
        var parameters = new object?[Arguments.Length - 1];
        Array.Copy(Arguments, parameters, parameters.Length);
        return new PyTuple([new PyList(parameters), Arguments[^1]]);
    }

    /// <inheritdoc />
    public override void SetAttr(string name, object? value) =>
        throw new PyNotImplementedError("typing alias attribute mutation is not supported");
    /// <inheritdoc />
    public override void DelAttr(string name) =>
        throw new PyNotImplementedError("typing alias attribute deletion is not supported");

    private object Equal(object? other)
    {
        // 作为缓存键的真实比较也会消费分类 helper；同对象指针命中不会进入此处。
        // 原生 PEP585 alias 的比较不读取 typing 模块；两类对象仅共用本载体。
        if (!char.IsLower(Name[0]))
            PyTyping.GuardHelper(Name == "Union" ? "_UnionGenericAlias" : Name == "Literal" ? "_LiteralGenericAlias" : "_GenericAlias");
        if (Name == "Union") PyTyping.GuardHelper("types");
        if (Name == "Literal") PyTyping.GuardHelper("_value_and_type_iter");
        if (other is not PyTypingAlias alias) return PyNotImplemented.Instance;
        if (Name != alias.Name || Arguments.Length != alias.Arguments.Length) return false;
        if (Name is not ("Union" or "Literal") && !ReferenceEquals(Origin, alias.Origin)) return false;
        if (Name == "Literal")
        {
            foreach (object? arg in Arguments)
            {
                bool found = false;
                foreach (object? candidate in alias.Arguments)
                    if (PyTypingLiteral.Equal(arg, candidate)) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }
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
        if (Name == "ClassVar") return PyHash.Of(new PyTuple([Origin, ArgumentTuple]));
        if (Name == "Literal")
        {
            PyTyping.GuardHelper("_value_and_type_iter");
            return PyTypingLiteral.Hash(Arguments);
        }
        if (Name != "Union") return PyHash.Of(new PyTuple([Name, new PyTuple(Arguments)]));
        long hash = 0;
        foreach (object? arg in Arguments) hash ^= PyHash.Of(arg);
        return hash == -1 ? -2 : hash;
    }

    /// <inheritdoc />
    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        if (Name == "ClassVar") throw PyTypingClassVar.Error(new PyTypeError("Cannot instantiate typing.ClassVar"));
        if (!char.IsLower(Name[0])) throw new PyTypeError($"Cannot instantiate typing.{Name}");
        return PyOps.CallValue(Origin, args, names, values);
    }

    /// <inheritdoc />
    public override string PythonRepr()
    {
        var parts = new List<string>();
        foreach (object? arg in Arguments) parts.Add(Name == "Literal" ? PyRepr.Repr(arg) : PyTyping.Display(arg));
        if (Name == "Literal" && parts.Count == 0) return "typing.Literal[()]";
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
