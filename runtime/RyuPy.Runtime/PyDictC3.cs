// dict+普通类冷族：完整C3节点决定成员胜者，solid布局与最佳直接基类独立决定唯一payload。
using System;
using System.Collections.Generic;
using System.Linq;
namespace RyuPy.Runtime;
public sealed partial class PyClass
{
    private object[]? _dictC3;
    private object? _dictBestBase;
    internal bool HasDictC3Layout => _dictC3 is not null;
    internal object DictBestBase => _dictBestBase ?? (object?)BaseClass ?? PyType.Get("dict");
    internal object[] DictC3Nodes => _dictC3 ?? throw new PyNotImplementedError("dict C3 is not finalized");
    internal static bool HasDictProvider(object? value) => ReferenceEquals(value, PyType.Get("dict")) || value is PyClass { HasDictLayout: true };
    private static bool DictCandidate(object?[] bases) => Array.Exists(bases, HasDictProvider);

    // 已知异构native仅作为延迟冲突token；不因此开放list/str多基能力。
    internal static void ValidateDictC3Base(object? value)
    {
        if (ReferenceEquals(value, PyBaseObject.Type) || ReferenceEquals(value, PyType.Get("dict"))
            || ReferenceEquals(value, PyType.Get("list")) || ReferenceEquals(value, PyType.Get("str"))) return;
        if (value is not PyClass cls) throw new PyNotImplementedError("dict C3 requires ordinary classes or supported native layout identities");
        if (cls.HasDictLayout) { cls.RequireDictReady(); cls.ValidateDictC3Family(); return; }
        if (cls.HasListLayout || cls.HasStringLayout) return;
        PyLexicalClassFactory.ValidateDictC3Ancestor(cls);
    }
    private static List<object> FullDictCandidateMro(object value)
    {
        if (ReferenceEquals(value, PyBaseObject.Type)) return [PyBaseObject.Type];
        if (ReferenceEquals(value, PyType.Get("dict"))) return [value, PyBaseObject.Type];
        var cls = (PyClass)value;
        if (cls._dictC3 is { } full) return [.. full];
        if (!cls.HasDictLayout) return cls.FullOrdinaryMro();
        var result = new List<object>(); var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? current = cls; current is not null; current = current.BaseClass)
        {
            if (!seen.Add(current)) throw new PyNotImplementedError("cyclic dict base");
            result.Add(current);
        }
        result.Add(PyType.Get("dict")); result.Add(PyBaseObject.Type); return result;
    }
    private void CompleteDictC3(object?[] bases)
    {
        var unique = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (object? value in bases)
        {
            if (value is null) throw new PyTypeError("null base");
            if (!unique.Add(value)) throw new PyTypeError("duplicate base class " + (value is PyClass c ? c.Name : ((PyType)value).Name));
            ValidateDictC3Base(value);
            if (ReferenceEquals(value, PyType.Get("list")) || ReferenceEquals(value, PyType.Get("str"))
                || value is PyClass { HasListLayout: true } or PyClass { HasStringLayout: true })
                throw new PyTypeError("multiple bases have instance lay-out conflict");
        }
        // dict solid-base强于object；两个用户dict子类共享同一个solid，不是两个payload。
        object best = bases[0]!;
        foreach (object? value in bases) if (!HasDictProvider(best) && HasDictProvider(value)) best = value!;
        var sequences = bases.Select(value => FullDictCandidateMro(value!)).ToList();
        sequences.Add(bases.Select(value => value!).ToList());
        var merged = new List<object> { this };
        while (sequences.Any(seq => seq.Count != 0))
        {
            object? next = null;
            foreach (var seq in sequences)
            {
                if (seq.Count == 0) continue;
                object head = seq[0];
                if (!sequences.Any(other => other.Skip(1).Any(item => ReferenceEquals(item, head)))) { next = head; break; }
            }
            if (next is null) throw new PyTypeError("Cannot create a consistent method resolution order (MRO) for bases");
            merged.Add(next);
            foreach (var seq in sequences) if (seq.Count != 0 && ReferenceEquals(seq[0], next)) seq.RemoveAt(0);
        }
        _dictC3 = merged.ToArray(); _dictBestBase = best;
        _ordinaryMro = merged.OfType<PyClass>().ToArray();
        _miBasesTuple = new PyTuple((object?[])bases.Clone()); _miMroTuple = new PyTuple(_dictC3);
        _pendingMiBases = null; HasDictLayout = true;
        // BaseClass只保传统单父字段，不是此族的布局/MRO/反射权威。
        BaseClass = best as PyClass; IsLexicalFactoryFamily = true;
    }
    internal void ValidateDictC3Family()
    {
        if (HasDictC3Layout)
        {
            foreach (PyClass cls in _ordinaryMro!) ValidateDictC3UserClass(cls, ReferenceEquals(cls, this));
        }
        else
        {
            var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
            for (PyClass? cls = this; cls is not null; cls = cls.BaseClass)
            {
                if (!seen.Add(cls)) throw new PyNotImplementedError("cyclic dict ancestor");
                ValidateDictC3UserClass(cls, ReferenceEquals(cls, this));
            }
        }
    }
    private static void ValidateDictC3UserClass(PyClass cls, bool self)
    {
        if (cls.HasStringLayout || cls.HasListLayout || cls.BuiltinBase is not null || cls.ExceptionFactory is not null
            || cls.NamedTupleLayout is not null || cls.HasEnumMetadata || cls.DataclassActive || cls.IsNativeRuntimeClass
            || cls.IsTypingGenericFamily || cls.IsTypingIoFamily)
            throw new PyNotImplementedError("unsupported dict C3 class layout");
        if (!self && cls.HasDictLayout) cls.RequireDictReady();
        if (!cls.HasDictLayout && !cls.IsCompletedOrdinaryLexicalBase)
            throw new PyNotImplementedError("dict C3 ordinary ancestor is not finalized");
        // 已完成祖先的真实slot布局已冻结，后来重绑__slots__只是元数据。
        if (cls._fixedSlotOwners.Count != 0) throw new PyTypeError("multiple bases have instance lay-out conflict");
        foreach (var pair in cls.Attributes)
        {
            if (pair.Key == "__slots__" && (cls._slotLayoutReady || cls._dictReady)) continue;
            ValidateDictC3Slot(pair.Key, pair.Value);
        }
    }
    private static void ValidateDictC3Slot(string name, object? value)
    {
        if (name == "__slots__")
        {
            if (EmptySlots(value)) return;
            throw new PyTypeError("multiple bases have instance lay-out conflict");
        }
        if (name == "__getitem__") { EnsureSupportedSlot(value, name); return; }
        ValidateDictSlot(name, value);
    }
}

// native owner是dict/object真实PyType；绝不创建同名PyClass占位。
internal readonly record struct PyC3Resolution(object Owner, object? Raw, string Name)
{
    internal object? Bind(object? receiver, bool classAccess = false)
    {
        if (Owner is PyClass user)
        {
            if (classAccess) return Raw switch
            { PyClassMethod method => new PyBoundMethod(user, receiver, method.Value), PyStaticMethod method => method.Value, _ => Raw };
            return PyObject.Bind(Raw, user, receiver);
        }
        if (ReferenceEquals(Owner, PyType.Get("dict")))
            return Name == "fromkeys" ? PyDictDescriptors.Bind(receiver!, Name)
                : classAccess ? PyDictDescriptors.Unbound(Name) : PyDictDescriptors.Bind(receiver!, Name);
        if (classAccess) return Raw;
        // object.__str__已有跨内建repr桥；不能重造只接受裸object的旧Method。
        if (ReferenceEquals(Raw, PyBaseObjectMethods.Unbound("__str__"))) return PyListDescriptors.Bind(receiver!, "__str__");
        return PyBaseObjectMethods.BindConstruction(receiver!, Name);
    }
    internal object? Invoke(object? receiver, object?[] args, string[] names, object?[] values) => Owner is PyClass user
        ? PyObject.InvokeBound(Raw, user, receiver, args, names, values)
        : PyOps.CallValue(Bind(receiver), args, names, values);
}
public sealed partial class PyClass
{
    private Dictionary<string, (long Version, PyC3Resolution? Value)>? _dictC3Lookup;
    internal static bool NativeC3Own(object node, string name, out object? raw)
    {
        if (ReferenceEquals(node, PyType.Get("dict")) && name != "__str__" && PyDictDescriptors.IsAttribute(name))
        { raw = PyDictDescriptors.Unbound(name); return true; }
        if (ReferenceEquals(node, PyBaseObject.Type) && PyBaseObjectMethods.IsMethod(name))
        { raw = PyBaseObjectMethods.Unbound(name); return true; }
        raw = null; return false;
    }
    internal bool TryResolveDictC3(string name, out PyC3Resolution result)
    {
        RequireLexicalFactoryReady();
        _dictC3Lookup ??= new(StringComparer.Ordinal);
        if (_dictC3Lookup.TryGetValue(name, out var cached) && cached.Version == GlobalVersion)
        { result = cached.Value.GetValueOrDefault(); return cached.Value.HasValue; }
        PyC3Resolution? found = null;
        foreach (object node in DictC3Nodes)
        {
            object? raw;
            bool present = node is PyClass user ? user.Attributes.TryGetValue(name, out raw) : NativeC3Own(node, name, out raw);
            if (present) { found = new(node, raw, name); break; }
        }
        if (_dictC3Lookup.Count >= 256) _dictC3Lookup.Clear();
        _dictC3Lookup[name] = (GlobalVersion, found); result = found.GetValueOrDefault(); return found.HasValue;
    }
}
