// TypeVar 的 C 风格绑定和有限 bound 校验。约束完全不转换，flags 的真值副作用固定次序。
// 未实现的动态 typing helper/未知比较对象明确失败，不伪造无副作用的 _type_check。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTypingTypeVarFactory
{
    internal static object Invoke(object?[] args, string[] names, object?[] values)
    {
        object? name = args.Length == 0 ? null : args[0];
        bool hasName = args.Length != 0, duplicateName = false;
        string? unknown = null;
        object? bound = PyNone.Instance, co = false, contra = false, infer = false;
        for (int index = 0; index < names.Length; index++)
        {
            switch (names[index])
            {
                case "name":
                    if (hasName) duplicateName = true;
                    name = values[index]; hasName = true; break;
                case "bound": bound = values[index]; break;
                case "covariant": co = values[index]; break;
                case "contravariant": contra = values[index]; break;
                case "infer_variance": infer = values[index]; break;
                default: unknown ??= names[index]; break;
            }
        }
        if (!hasName) throw Error("typevar() missing required argument 'name' (pos 1)");
        if (unknown is not null) throw Error($"'{unknown}' is an invalid keyword argument for typevar()");
        if (duplicateName) throw Error("invalid keyword argument for typevar()");
        if (name is not (string or PyStringInstance))
            throw Error("typevar() argument 'name' must be str, not "
                + (name is null or PyNone ? "None" : PyRepr.TypeName(name)));
        bool covariant = PyOps.Truthy(co);
        bool contravariant = PyOps.Truthy(contra);
        bool inferVariance = PyOps.Truthy(infer);
        if (covariant && contravariant)
            throw PyTypingTypeVar.Own(new PyValueError("Bivariant types are not supported."));
        if (inferVariance && (covariant || contravariant))
            throw PyTypingTypeVar.Own(new PyValueError("Variance cannot be specified with infer_variance."));
        object normalizedBound = bound is null or PyNone ? PyNone.Instance : NormalizeBound(bound);
        int count = Math.Max(0, args.Length - 1);
        if (count == 1) throw Error("A single constraint is not allowed");
        if (count != 0 && normalizedBound is not PyNone)
            throw Error("Constraints cannot be combined with bound=...");
        var constraints = new object?[count];
        if (count != 0) Array.Copy(args, 1, constraints, 0, count);
        // 当前帧来源保持原值；禁止真值fallback或把 None 猜成 __main__。
        object module = PyTraceback.GetCallerModule();
        return new PyTypingTypeVar(name!, normalizedBound, new PyTuple(constraints),
            covariant, contravariant, inferVariance, module);
    }

    private static object NormalizeBound(object value)
    {
        Dictionary<string, object?> attributes = PyTyping.CreateAttributes();
        RejectAdded(attributes, "_type_check");
        PyTypingGeneric.GuardExport(attributes);
        RejectAdded(attributes, "Protocol");
        object? classVar = PyTypingClassVar.ReadExport();
        RejectAdded(attributes, "Final", "_type_convert");
        RejectAdded(attributes, "isinstance", "str");
        if (value is PyStringInstance) throw Unsupported("bound str subclass conversion is not supported");
        object normalized = value;
        if (value is string text)
        {
            RejectAdded(attributes, "ForwardRef");
            normalized = PyTypingForwardRef.Create(text);
        }
        RejectAdded(attributes, "_GenericAlias");
        PyTypingGeneric.ValidateTypeArgument(normalized);
        PyTypingClassVar.ValidateAlias(normalized, classVar);
        if (!attributes.TryGetValue("Any", out object? any)
            || !ReferenceEquals(any, PyTyping.OriginalForm("Any")))
            throw Unsupported("bound lookup after rebinding typing.Any is not supported");
        RejectAdded(attributes, "LiteralString", "NoReturn", "Never", "Self", "TypeAlias");
        if (ReferenceEquals(normalized, any)) return normalized;
        RejectAdded(attributes, "_SpecialForm");
        if (normalized is PyTypingForm { Name: "Union" or "Optional" or "ClassVar" or "Literal" } form)
            throw Error($"Plain typing.{form.Name} is not valid as type argument");
        RejectAdded(attributes, "type");
        if (normalized is PyTuple tuple && tuple.GetType() == typeof(PyTuple))
        {
            string representation = PyRepr.Repr(tuple);
            // CP 按 Unicode 码点截取 repr，不截断 UTF-16 代理对。
            int end = 0, characters = 0;
            while (end < representation.Length && characters++ < 100)
            {
                end += char.IsHighSurrogate(representation[end]) && end + 1 < representation.Length
                    && char.IsLowSurrogate(representation[end + 1]) ? 2 : 1;
            }
            representation = representation[..end];
            throw Error("Bound must be a type. Got " + representation + ".");
        }
        // exact list 与 special 的比较不读取元素；原引用保存，修改随后可观察。
        if (normalized is PyType or PyClass or PyNativeType or PyTypingTypeVar or PyTypingForwardRef
            or PyTypingAlias or PyTypingForm or bool or int or long or BigInteger or double or PyList)
            return normalized;
        throw Unsupported("bound value with unsupported validation or comparison behavior");
    }

    private static void RejectAdded(Dictionary<string, object?> attributes, params string[] names)
    {
        foreach (string name in names)
            if (attributes.ContainsKey(name))
                throw Unsupported($"bound lookup after setting typing.{name} is not supported");
    }
    private static PyTypeError Error(string message) => PyTypingTypeVar.Own(new PyTypeError(message));
    private static PyNotImplementedError Unsupported(string detail) =>
        PyTypingTypeVar.Own(new PyNotImplementedError("TypeVar " + detail));
}
