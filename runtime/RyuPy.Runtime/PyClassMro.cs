// 普通多继承的冷C3权威；旧单继承仍保BaseClass链与原缓存，只有MI族持有线性化数组。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    private object?[]? _pendingMiBases;
    private PyClass[]? _ordinaryMro;
    private PyTuple? _miBasesTuple;
    private PyTuple? _miMroTuple;
    private bool _failedMi;
    internal bool HasMultipleInheritance => _ordinaryMro is not null || _pendingMiBases is not null;

    // 类体前只冻结真实引用及能力，不在此计算C3/重复基错误。
    internal void PrepareMultipleBases(object?[] bases)
    {
        foreach (object? value in bases)
        {
            if (ReferenceEquals(value, PyBaseObject.Type)) continue;
            if (value is not PyClass parent)
                throw new PyNotImplementedError("ordinary multiple inheritance requires real ordinary classes or object");
            PyLexicalClassFactory.ValidateFamily(parent);
            parent.ValidateMiStructuralMetadata();
        }
        _pendingMiBases = (object?[])bases.Clone();
        BaseClass = bases.Length == 0 ? null : bases[0] as PyClass;
    }

    // 正文成功后才合并C3；失败对象不发布，不回滚已经执行的正文副作用。
    private void CompleteMultipleInheritance()
    {
        if (_failedMi) throw new PyNotImplementedError("failed multiple inheritance definition");
        if (_pendingMiBases is null && BaseClass?.HasMultipleInheritance == true)
            _pendingMiBases = [BaseClass];
        if (_pendingMiBases is not { } bases) return;
        try
        {
            var unique = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var sequences = new List<List<object>>();
            foreach (object? value in bases)
            {
                if (value is null) throw new PyNotImplementedError("null MI base");
                if (!unique.Add(value)) throw new PyTypeError($"duplicate base class {BaseName(value)}");
                if (ReferenceEquals(value, PyBaseObject.Type)) sequences.Add([PyBaseObject.Type]);
                else if (value is PyClass parent)
                {
                    PyLexicalClassFactory.ValidateFamily(parent);
                    sequences.Add(parent.FullOrdinaryMro());
                }
                else throw new PyNotImplementedError("multiple inheritance special layout");
            }
            var direct = new List<object>();
            foreach (object? value in bases) direct.Add(value!);
            sequences.Add(direct);
            var merged = new List<PyClass> { this };
            while (true)
            {
                bool remaining = false;
                object? next = null;
                foreach (var sequence in sequences)
                {
                    if (sequence.Count == 0) continue;
                    remaining = true;
                    object head = sequence[0];
                    bool inTail = false;
                    foreach (var other in sequences)
                        for (int i = 1; i < other.Count; i++)
                            inTail |= ReferenceEquals(other[i], head);
                    if (!inTail) { next = head; break; }
                }
                if (!remaining) break;
                if (next is null)
                {
                    var heads = new List<string>();
                    var seenHeads = new HashSet<object>(ReferenceEqualityComparer.Instance);
                    foreach (var sequence in sequences)
                        if (sequence.Count > 0 && seenHeads.Add(sequence[0])) heads.Add(BaseName(sequence[0]));
                    throw new PyTypeError("Cannot create a consistent method resolution\norder (MRO) for bases "
                        + string.Join(", ", heads));
                }
                if (next is PyClass cls) merged.Add(cls);
                foreach (var sequence in sequences)
                    if (sequence.Count != 0 && ReferenceEquals(sequence[0], next)) sequence.RemoveAt(0);
            }
            _ordinaryMro = merged.ToArray();
            _miBasesTuple = new PyTuple((object?[])bases.Clone());
            var mroValues = new object?[merged.Count + 1];
            for (int i = 0; i < merged.Count; i++) mroValues[i] = merged[i];
            mroValues[^1] = PyBaseObject.Type;
            _miMroTuple = new PyTuple(mroValues);
            _pendingMiBases = null;
            IsLexicalFactoryFamily = true;
        }
        catch { _failedMi = true; throw; }
    }

    // 只在新MI合同消费时拒绝结构同名项；旧非MI词法类namespace准入保持不变。
    internal void ValidateMiStructuralMetadata()
    {
        foreach (PyClass cls in MroClasses())
            if (cls.Attributes.ContainsKey("__bases__") || cls.Attributes.ContainsKey("__mro__"))
                throw new PyNotImplementedError("MI structural namespace overrides are not supported");
    }

    private static string BaseName(object value) => value is PyClass cls ? cls.Name : "object";

    private List<object> FullOrdinaryMro()
    {
        var result = new List<object>();
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        foreach (PyClass cls in MroClasses())
        {
            if (!seen.Add(cls)) throw new PyNotImplementedError("cyclic ordinary inheritance");
            result.Add(cls);
        }
        result.Add(PyBaseObject.Type);
        return result;
    }

    internal PyTuple DirectBasesTuple() => _miBasesTuple is { } bases ? bases
        : BaseClass is { } parent ? new PyTuple([parent])
        : new PyTuple([BuiltinBase is { } name ? PyType.Get(name) : PyBaseObject.Type]);

    internal PyTuple MroTuple()
    {
        RequireLexicalFactoryReady();
        if (!HasOrdinaryLexicalLayout) throw new PyNotImplementedError("special-layout MRO reflection");
        return _miMroTuple ?? new PyTuple(FullOrdinaryMro().ToArray());
    }

    // struct遍历器不分配；无MI族仍直接沿原单链，热Walk/IsSubclass另保原循环。
    internal MroSequence MroClasses() => new(this, null);
    internal static MroSequence SuperClasses(PyClass owner, object? receiver)
    {
        PyClass? actual = receiver switch { PyInstance instance => instance.Class, PyClass cls => cls, _ => null };
        if (actual?._ordinaryMro is not null) return new(actual, owner);
        return new(owner.BaseClass, null);
    }

    internal readonly struct MroSequence(PyClass? first, PyClass? after)
    {
        public MroIterator GetEnumerator() => new(first, after);
    }

    internal struct MroIterator
    {
        private readonly PyClass[]? _array;
        private PyClass? _next;
        private int _index;
        public PyClass Current { get; private set; }
        internal MroIterator(PyClass? first, PyClass? after)
        {
            _array = first?._ordinaryMro;
            _next = first; _index = 0; Current = null!;
            if (after is not null && _array is not null)
            {
                while (_index < _array.Length && !ReferenceEquals(_array[_index], after)) _index++;
                if (_index == _array.Length) throw new PyTypeError("super owner is not in receiver MRO");
                _index++;
            }
        }
        public bool MoveNext()
        {
            if (_array is not null)
            {
                if (_index == _array.Length) return false;
                Current = _array[_index++]; return true;
            }
            if (_next is null) return false;
            Current = _next; _next = _next.BaseClass; return true;
        }
    }
}
