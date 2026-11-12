// NamedTuple专用冷注解转换：真实None/ForwardRef和值身份，不复用过严的泛型TypeArgument。
// 只承诺无用户比较回调的输入域；实际typing全局改绑在对应消费点NI，不造缺失special占位。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTypingNamedTupleAnnotations
{
    private static readonly string[] BeforeConvert = ["_type_check", "Generic", "Protocol"];
    private static readonly string[] AfterClassVar = ["Final", "_type_convert"];
    private static readonly string[] BeforeString = ["isinstance", "str"];
    private static readonly string[] BeforeAlias = ["isinstance", "_GenericAlias"];
    private static readonly string[] MembershipTail = ["Never", "Self", "TypeAlias"];

    internal static object NormalizeAnnotation(object? value, string fieldName)
    {
        Dictionary<string, object?> attributes = PyTyping.CreateAttributes();
        RejectAdded(attributes, BeforeConvert);
        object? classVar = PyTypingClassVar.ReadExport();
        RejectAdded(attributes, AfterClassVar);
        object normalized;
        if (value is null or PyNone)
        {
            // _type_convert的None分支在这里调用type(None)，不是等到末尾exact tuple判定。
            RejectAdded(attributes, "type");
            normalized = PyType.Get("NoneType");
        }
        else
        {
            RejectAdded(attributes, BeforeString);
            if (value is PyStringInstance)
                throw Unsupported(fieldName, "str subclass conversion is not supported");
            if (value is string text)
            {
                // A/B尚未公开ForwardRef；后续公开真type时必须改成canonical身份守卫。
                RejectAdded(attributes, "ForwardRef");
                normalized = PyTypingForwardRef.Create(text);
            }
            else normalized = value;
        }

        RejectAdded(attributes, BeforeAlias);
        if (normalized is PyTypingGenericAlias) PyTypingGeneric.ValidateTypeArgument(normalized);
        PyTypingClassVar.ValidateAlias(normalized, classVar);
        if (normalized is PyTypingAlias alias && !SafeAliasOrigin(alias.Origin))
            throw Unsupported(fieldName, "typing alias origin with unknown comparison behavior is not supported");

        // 这些全局在转换之后才被读取。原Any来自私有Forms，不以首次观察的可写值冒canonical。
        if (!attributes.TryGetValue("Any", out object? currentAny)
            || !ReferenceEquals(currentAny, PyTyping.OriginalForm("Any")))
            throw PyTypingForwardRef.Unsupported("NamedTuple annotation lookup after rebinding typing.Any is not supported");
        RejectAdded(attributes, "LiteralString");
        object? noReturn = PyTypingNoReturn.ReadExport();
        RejectAdded(attributes, MembershipTail);
        if (PyTypingNoReturn.Matches(normalized, currentAny, noReturn, fieldName)) return normalized;

        // Any已提前返回；裸special也在type(...)之前报原TypeError，不让未消费的type改绑抢先。
        RejectAdded(attributes, "_SpecialForm");
        if (normalized is PyTypingForm form)
        {
            if (!KnownForm(form.Name) || !ReferenceEquals(form, PyTyping.OriginalForm(form.Name)))
                throw Unsupported(fieldName, "unknown typing form identity is not supported");
            if (form.Name is "Union" or "Optional" or "ClassVar" or "Literal" or "NoReturn")
                throw (PyTypeError)PyErrors.PrepareRaise(new PyTypeError($"Plain typing.{form.Name} is not valid as type argument"));
        }
        if (PyTypingGeneric.IsRoot(normalized)) PyTypingGeneric.ValidateTypeArgument(normalized);
        RejectAdded(attributes, "type");

        // 类实例的__eq__不等于类对象元类的__eq__；当前PyClass尚无用户metaclass。
        // alias与这些special跨种类比较会先返回NotImplemented，不扫描参数或执行里面的用户eq。
        if (normalized is PyType or PyClass or PyNativeType or PyTypingForwardRef or PyTypingAlias or PyTypingForm or PyTypingTypeVar
            or bool or long or int or BigInteger or double)
            return normalized;
        // exact tuple的CP错误需完整repr/截断及真实副作用；本切明确NI，也不把tuple派生误判为exact。
        throw Unsupported(fieldName, "annotation value with unsupported validation or comparison behavior");
    }

    private static bool SafeAliasOrigin(object origin) => origin is PyType or PyNativeType
        || origin is PyClass abc && PyAbc.IsAbc(abc)
        || origin is PyTypingForm { Name: "Union" or "ClassVar" or "Literal" } form
            && ReferenceEquals(form, PyTyping.OriginalForm(form.Name));

    private static bool KnownForm(string name) => name is "Any" or "NoReturn" or "Optional" or "Union" or "ClassVar" or "Literal"
        or "List" or "Dict" or "Tuple" or "Set" or "FrozenSet" or "Callable" or "Iterable" or "Iterator"
        or "Sequence" or "Mapping" or "MutableMapping" or "Collection" or "Container";

    private static void RejectAdded(Dictionary<string, object?> attributes, string[] names)
    {
        foreach (string name in names) RejectAdded(attributes, name);
    }

    private static void RejectAdded(Dictionary<string, object?> attributes, string name)
    {
        if (name == "Generic") { PyTypingGeneric.GuardExport(attributes); return; }
        if (attributes.ContainsKey(name))
            throw PyTypingForwardRef.Unsupported($"NamedTuple annotation lookup after setting typing.{name} is not supported");
    }

    private static PyNotImplementedError Unsupported(string fieldName, string detail) =>
        PyTypingForwardRef.Unsupported($"NamedTuple field {fieldName}: {detail}");
}
