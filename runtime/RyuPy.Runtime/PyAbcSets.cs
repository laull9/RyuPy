// Set/MutableSet mixin保留用户contains、比较、_from_iterable和变更协议的可观察分派。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;
public static partial class PyAbc
{
    private static PyKeyError MissingSetValue(object? value)
    {
        var error = new PyKeyError(""); error.InitializePositionalArguments([value]); return error;
    }
    private static bool IsIterable(object? value) => Accepts(RootType("Iterable"), value);
    private static bool IsSet(object? value) => Accepts(RootType("Set"), value);
    private static object? FromSet(object? self, IEnumerable<object?> values) => Call(self, "_from_iterable", new PyAbcIterator(values.GetEnumerator()));
    private static IEnumerable<object?> Iterate(object? value)
    {
        var iterator = PyIter.Create(value); while (iterator.MoveNext()) yield return iterator.Current();
    }
    private static IEnumerable<object?> SelectSet(object? first, object? second, bool included) =>
        // Python生成器表达式立即取得最外层iter，过滤正文仍延迟到消费时。
        SelectSetLoop(PyIter.Create(first), second, included);
    private static IEnumerable<object?> SelectSetLoop(IPyIterator iterator, object? second, bool included)
    {
        while (iterator.MoveNext())
        {
            object? value = iterator.Current();
            if (PyOps.Contains(second, value) == included) yield return value;
        }
    }
    private static IEnumerable<object?> UnionSet(object? first, object? second)
    {
        foreach (object? value in Iterate(first)) yield return value;
        foreach (object? value in Iterate(second)) yield return value;
    }
    private static void InstallSet(PyClass cls)
    {
        if (cls.Name == "Set")
        {
            var factory = Method(cls, "_from_iterable", ["cls", "it"], a => PyOps.CallValue(a[0], [a[1]], [], []));
            cls.SetAttr("_from_iterable", new PyClassMethod(factory));
            Method(cls, "__le__", ["self", "other"], a =>
            {
                if (!IsSet(a[1])) return PyNotImplemented.Instance;
                if (PyOps.Len(a[0]) > PyOps.Len(a[1])) return false;
                foreach (object? value in Iterate(a[0])) if (!PyOps.Contains(a[1], value)) return false;
                return true;
            });
            Method(cls, "__ge__", ["self", "other"], a =>
            {
                if (!IsSet(a[1])) return PyNotImplemented.Instance;
                if (PyOps.Len(a[0]) < PyOps.Len(a[1])) return false;
                foreach (object? value in Iterate(a[1])) if (!PyOps.Contains(a[0], value)) return false;
                return true;
            });
            foreach (string name in new[] { "__lt__", "__gt__", "__eq__" })
                Method(cls, name, ["self", "other"], a =>
                {
                    if (!IsSet(a[1])) return PyNotImplemented.Instance;
                    long left = PyOps.Len(a[0]), right = PyOps.Len(a[1]);
                    if (!(name == "__lt__" ? left < right : name == "__gt__" ? left > right : left == right)) return false;
                    return Call(a[0], name == "__gt__" ? "__ge__" : "__le__", a[1]);
                });
            Method(cls, "isdisjoint", ["self", "other"], a =>
            { foreach (object? value in Iterate(a[1])) if (PyOps.Contains(a[0], value)) return false; return true; });
            Method(cls, "__and__", ["self", "other"], a => IsIterable(a[1]) ? FromSet(a[0], SelectSet(a[1], a[0], true)) : PyNotImplemented.Instance);
            Method(cls, "__or__", ["self", "other"], a => IsIterable(a[1]) ? FromSet(a[0], UnionSet(a[0], a[1])) : PyNotImplemented.Instance);
            Method(cls, "__sub__", ["self", "other"], a => IsIterable(a[1]) ? FromSet(a[0], SelectSet(a[0], IsSet(a[1]) ? a[1] : Call(a[0], "_from_iterable", a[1]), false)) : PyNotImplemented.Instance);
            Method(cls, "__rsub__", ["self", "other"], a => IsIterable(a[1]) ? FromSet(a[0], SelectSet(IsSet(a[1]) ? a[1] : Call(a[0], "_from_iterable", a[1]), a[0], false)) : PyNotImplemented.Instance);
            Method(cls, "__xor__", ["self", "other"], a =>
            {
                if (!IsIterable(a[1])) return PyNotImplemented.Instance;
                object? other = IsSet(a[1]) ? a[1] : Call(a[0], "_from_iterable", a[1]);
                return PyOps.Binary(PyOps.BitOr, PyOps.Binary(PyOps.Sub, a[0]!, other!), PyOps.Binary(PyOps.Sub, other!, a[0]!));
            });
            foreach (string op in new[] { "and", "or", "xor" }) cls.SetAttr("__r" + op + "__", cls.Attributes["__" + op + "__"]);
            Method(cls, "_hash", ["self"], _ => throw new PyNotImplementedError("Set._hash requires coherent frozenset hashing"));
        }
        if (cls.Name != "MutableSet") return;
        Method(cls, "remove", ["self", "value"], a =>
        {
            if (!PyOps.Contains(a[0], a[1])) throw MissingSetValue(a[1]);
            Call(a[0], "discard", a[1]); return PyNone.Instance;
        });
        Method(cls, "pop", ["self"], a =>
        {
            var iterator = PyIter.Create(a[0]); if (!iterator.MoveNext()) throw new PyKeyError("pop from an empty set");
            object? value = iterator.Current(); Call(a[0], "discard", value); return value;
        });
        Method(cls, "clear", ["self"], a => { try { while (true) Call(a[0], "pop"); } catch (PyKeyError) { return PyNone.Instance; } });
        Method(cls, "__ior__", ["self", "it"], a => { foreach (object? value in Iterate(a[1])) Call(a[0], "add", value); return a[0]; });
        Method(cls, "__iand__", ["self", "it"], a =>
        { foreach (object? value in Iterate(PyOps.Binary(PyOps.Sub, a[0]!, a[1]!))) Call(a[0], "discard", value); return a[0]; });
        Method(cls, "__ixor__", ["self", "it"], a =>
        {
            if (ReferenceEquals(a[0], a[1])) { Call(a[0], "clear"); return a[0]; }
            object? values = IsSet(a[1]) ? a[1] : Call(a[0], "_from_iterable", a[1]);
            foreach (object? value in Iterate(values)) Call(a[0], PyOps.Contains(a[0], value) ? "discard" : "add", value);
            return a[0];
        });
        Method(cls, "__isub__", ["self", "it"], a =>
        {
            if (ReferenceEquals(a[0], a[1])) Call(a[0], "clear");
            else foreach (object? value in Iterate(a[1])) Call(a[0], "discard", value);
            return a[0];
        });
    }
}
