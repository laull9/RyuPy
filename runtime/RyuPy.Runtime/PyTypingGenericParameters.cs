// canonical Generic.__init_subclass__ 的有限参数采集；仅消费最终own orig，不复制父类参数。
// 身份去重不调用TypeVar实例extra的hash/eq；未知迭代、比较及参数协议均在此冷消费点明确拒绝。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTypingGenericParameters
{
    private static readonly PyTuple Empty = new([]);

    internal static void Finish(PyClass cls)
    {
        var attributes = PyTyping.CreateAttributes();
        if (attributes.ContainsKey("_generic_init_subclass"))
            throw PyTypingGeneric.Unsupported("changed typing Generic initialization helper is not supported");
        PyTypingGeneric.GuardExport(attributes);
        if (!cls.Attributes.TryGetValue("__orig_bases__", out object? original))
        {
            if (PyTypingGeneric.IsRoot(cls.BaseClass))
            {
                if (cls.Name == "Protocol")
                    throw PyTypingGeneric.Unsupported("Generic Protocol definition is not supported");
                throw PlainGeneric();
            }
            cls.SetAttr("__parameters__", Empty);
            return;
        }
        if (original is null or PyNone or bool or long or int or double or BigInteger)
            throw PyTypingGeneric.Error(new PyTypeError($"argument of type '{PyRepr.TypeName(original)}' is not iterable"));
        if (original is not PyTuple tuple || tuple.GetType() != typeof(PyTuple))
            throw PyTypingGeneric.Unsupported("Generic original bases currently require an exact tuple");
        // CP先做Generic成员检查，之后才调用_collect_parameters；不让helper改写抢先覆盖plain错误。
        foreach (object? item in tuple.Items)
        {
            if (PyTypingGeneric.IsRoot(item)) throw PlainGeneric();
            RequireKnown(item);
        }
        if (attributes.ContainsKey("_collect_parameters"))
            throw PyTypingGeneric.Unsupported("changed typing parameter collector is not supported");
        var ordered = new List<object?>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (object? item in tuple.Items)
        {
            if (item is PyTypingTypeVar variable) Add(variable, ordered, seen);
            else if (item is PyTypingGenericAlias generic)
                foreach (object? parameter in generic.Arguments.Items) Add(parameter!, ordered, seen);
            else if (item is PyTypingAlias alias)
                foreach (object? parameter in alias.Parameters.Items) Add(parameter!, ordered, seen);
        }
        if (tuple.Items.Length != 0 && attributes.ContainsKey("_GenericAlias"))
            throw PyTypingGeneric.Unsupported("changed typing Generic alias classifier is not supported");
        PyTuple? genericParameters = null;
        foreach (object? item in tuple.Items)
        {
            if (item is not PyTypingGenericAlias alias) continue;
            if (genericParameters is not null)
                throw PyTypingGeneric.Error(new PyTypeError("Cannot inherit from Generic[...] multiple times."));
            genericParameters = alias.Parameters;
        }
        if (genericParameters is not null)
        {
            var declared = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (object? parameter in genericParameters.Items) declared.Add(parameter!);
            var missing = new List<string>();
            foreach (object? parameter in ordered)
                if (!declared.Contains(parameter!)) missing.Add(PyRepr.Str(parameter));
            if (missing.Count != 0)
            {
                var names = new List<string>();
                foreach (object? parameter in genericParameters.Items) names.Add(PyRepr.Str(parameter));
                throw PyTypingGeneric.Error(new PyTypeError($"Some type variables ({string.Join(", ", missing)}) are not listed in Generic[{string.Join(", ", names)}]"));
            }
            // CP tuple(existing_tuple)保留Generic alias的parameters身份。
            cls.SetAttr("__parameters__", genericParameters);
        }
        else cls.SetAttr("__parameters__", ordered.Count == 0 ? Empty : new PyTuple(ordered.ToArray()));
    }

    private static void Add(object parameter, List<object?> ordered, HashSet<object> seen)
    {
        if (seen.Add(parameter)) ordered.Add(parameter);
    }

    private static void RequireKnown(object? value)
    {
        if (value is null or PyNone or string or bool or long or int or double or BigInteger
            or PyType or PyNativeType or PyClass or PyTypingTypeVar or PyTypingGenericAlias or PyTypingAlias) return;
        throw PyTypingGeneric.Unsupported("Generic original base comparison or parameter protocol is not supported");
    }

    private static PyTypeError PlainGeneric() => PyTypingGeneric.Error(new PyTypeError("Cannot inherit from plain Generic"));
}
