// Union / Optional / ClassVar 的冷语义及 typing 内部类型元数据；缓存命中绕过这些全局消费点。
using System.Collections.Generic;

namespace RyuPy.Runtime;

public static partial class PyTyping
{
    internal static object SpecialUncached(PyTypingForm receiver, object? parameter)
    {
        if (receiver.Name == "ClassVar")
        {
            object argument = SpecialTypeArgument(parameter, "typing.ClassVar accepts only single type.");
            GuardHelper("_GenericAlias");
            GuardHelper("_TypingEllipsis");
            GuardHelper("_collect_parameters");
            // 只对新 ClassVar 冷结果固定参数快照，不给旧别名增加字段或默认分配。
            PyTypingClassVar.RequireSafeParameters(argument);
            var alias = new PyTypingAlias("ClassVar", receiver, [argument]);
            _ = alias.Parameters;
            return alias;
        }
        if (receiver.Name == "Optional")
        {
            object argument = SpecialTypeArgument(parameter, "typing.Optional requires a single type.");
            // Optional 的函数体引用当前 Union；保存的 Union 根自身订阅则始终保留 self。
            if (!Attributes.Value.TryGetValue("Union", out object? current))
                throw new PyNameError("name 'Union' is not defined");
            if (current is not (null or PyNone or bool or int or long or double or System.Numerics.BigInteger
                or PyEllipsis or string or PyType or PyNativeType or PyTypingForm))
                throw new PyNotImplementedError("rebound typing.Union subscription protocol is not supported");
            return PyOps.GetItem(current, new PyTuple([argument, PyType.Get("NoneType")]))!;
        }
        object?[] args = parameter is PyTuple tuple ? tuple.Items : [parameter];
        if (args.Length == 0) throw new PyTypeError("Cannot take a Union of no types.");
        var values = new object?[args.Length];
        for (int i = 0; i < args.Length; i++)
            values[i] = SpecialTypeArgument(args[i], "Union[arg, ...]: each arg must be a type.");
        GuardHelper("_remove_dups_flatten");
        GuardHelper("_UnionGenericAlias");
        GuardHelper("types");
        var flattened = new List<object?>();
        foreach (object? value in values)
        {
            if (value is PyTypingAlias { Name: "Union" } union) flattened.AddRange(union.Arguments);
            else flattened.Add(value);
        }
        GuardHelper("_deduplicate");
        var distinct = new List<object?>();
        foreach (object? value in flattened)
        {
            bool duplicate = false;
            foreach (object? existing in distinct)
                if (ReferenceEquals(existing, value) || PyHash.Equal(existing, value)) { duplicate = true; break; }
            if (!duplicate) distinct.Add(value);
        }
        if (distinct.Count == 1) return distinct[0]!;
        GuardHelper("_TypingEllipsis");
        GuardHelper("_collect_parameters");
        return new PyTypingAlias("Union", receiver, distinct.ToArray());
    }

    private static object SpecialTypeArgument(object? value, string message)
    {
        GuardHelper("_type_check");
        PyTypingGeneric.GuardExport(Attributes.Value);
        // 这些尚未提供的全局被用户写入时，不能继续假装执行原始 _type_check。
        GuardHelper("Protocol");
        object? classVar = PyTypingClassVar.ReadExport();
        GuardHelper("Final");
        GuardHelper("_type_convert");
        object? converted = value is null or PyNone ? PyType.Get("NoneType") : value;
        if (converted is string)
            throw new PyNotImplementedError("typing special-form forward references are not supported");
        GuardHelper("_GenericAlias");
        if (converted is PyTypingGenericAlias) PyTypingGeneric.ValidateTypeArgument(converted);
        PyTypingClassVar.ValidateAlias(converted, classVar);
        if (!Attributes.Value.TryGetValue("Any", out object? any) || !ReferenceEquals(any, Forms["Any"]))
            throw new PyNotImplementedError("rebound typing.Any type-argument comparison is not supported");
        GuardHelper("LiteralString"); GuardHelper("NoReturn"); GuardHelper("Never");
        GuardHelper("Self"); GuardHelper("TypeAlias");
        if (ReferenceEquals(converted, any)) return converted!;
        GuardHelper("_SpecialForm");
        if (converted is PyTypingForm { Name: "Optional" or "Union" or "ClassVar" or "Literal" })
            throw new PyTypeError($"Plain {Display(converted)} is not valid as type argument");
        if (PyTypingGeneric.IsRoot(converted)) PyTypingGeneric.ValidateTypeArgument(converted);
        if (converted is PyTuple)
        {
            string repr = PyRepr.Repr(converted);
            // Python 格式精度按码点截断；共用字符串工具也保留孤立代理项。
            int limit = PyText.OffsetOf(repr, 100);
            throw new PyTypeError(message + " Got " + (limit < 0 ? repr : repr[..limit]) + ".");
        }
        if (converted is PyType or PyNativeType or PyClass or PyTypedDictType or PyTypingTypeVar or PyTypingAlias or PyTypingForm)
            return converted;
        // CP 接受多种非类型值；未实现的合法域不能伪造 Parameters must be types。
        throw new PyNotImplementedError("typing special-form non-type arguments are not supported");
    }

    private static object GetArguments(object? value)
    {
        GuardHelper("_AnnotatedAlias"); GuardHelper("_GenericAlias"); GuardHelper("GenericAlias");
        if (value is PyTypingGenericAlias generic)
        {
            GuardHelper("_should_unflatten_callable_args");
            return generic.Arguments;
        }
        if (value is PyTypingAlias alias)
        {
            GuardHelper("_should_unflatten_callable_args");
            return alias.PublicArguments();
        }
        GuardHelper("types");
        return value is PyClass cls && cls.IsTypingIoFamily ? PyTypingIo.Empty : new PyTuple([]);
    }

    internal static void GuardHelper(string name)
    {
        if (Attributes.Value.ContainsKey(name))
            throw new PyNotImplementedError($"changed typing helper {name} is not supported at this consumer");
    }

    // 仅扩充现有 PyType 名称表；不登记替代 PyNativeType，不改变已驻留类型对象身份。
    internal static bool IsMetadataType(string name) => name is
        "_TypedDictMeta" or "_GenericAlias" or "_UnionGenericAlias" or "_LiteralGenericAlias" or "_CallableGenericAlias"
        or "_SpecialForm" or "_LiteralSpecialForm" or "_SpecialGenericAlias" or "_TupleType"
        or "_CallableType" or "_AnyMeta";
}
