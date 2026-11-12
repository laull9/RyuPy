// 普通空slots与认证原生固定槽共用冻结布局；特殊布局不借此获得普通存储能力。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    private bool _slotLayoutReady;
    private string[]? _nativeSlots;
    private readonly Dictionary<string, PyInstanceSlotDescriptor> _nativeSlotDescriptors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PyClass> _fixedSlotOwners = new(StringComparer.Ordinal);
    internal bool HasSlotLayout { get; private set; }
    internal bool HasInstanceDictionary { get; private set; } = true;

    internal void ConfigureNativeSlots(string[] names)
    {
        if (_slotLayoutReady || _nativeSlots is not null) throw new PyNotImplementedError("slot layout already configured");
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in names)
            if (name is "__dict__" or "__weakref__" || !unique.Add(name))
                throw new PyTypeError("invalid native slot name");
        _nativeSlots = (string[])names.Clone();
        foreach (string name in names)
        {
            var descriptor = new PyInstanceSlotDescriptor(this, name);
            _nativeSlotDescriptors[name] = descriptor;
            Attributes[name] = descriptor;
        }
        Attributes["__slots__"] = new PyTuple(Array.ConvertAll(names, name => (object?)name));
    }

    internal static bool EmptySlots(object? value) => value is PyTuple { Items.Length: 0 } or PyList { Items.Count: 0 };
    internal bool AllowsSlotsNamespace(object? value)
    {
        if (_slotLayoutReady || EmptySlots(value)) return true;
        if (_nativeSlots is null || value is not PyTuple tuple || tuple.Items.Length != _nativeSlots.Length) return false;
        for (int i = 0; i < _nativeSlots.Length; i++)
            if (tuple.Items[i] is not string name || name != _nativeSlots[i]) return false;
        return true;
    }
    internal bool AllowsSlotDescriptor(string name, PyInstanceSlotDescriptor descriptor) =>
        name == descriptor.Name && descriptor.Owner.OwnsNativeDescriptor(descriptor) && IsSubclassOf(descriptor.Owner);
    internal bool OwnsNativeDescriptor(PyInstanceSlotDescriptor descriptor) =>
        _nativeSlotDescriptors.TryGetValue(descriptor.Name, out var known) && ReferenceEquals(known, descriptor);

    private void FinalizeSlotLayout()
    {
        if (_slotLayoutReady || !HasOrdinaryLexicalLayout) return;
        bool own = Attributes.TryGetValue("__slots__", out object? declaration);
        if (own && !AllowsSlotsNamespace(declaration)) throw new PyNotImplementedError("nonempty user __slots__ is not supported");
        bool inherited = false, dictionary = false;
        var parents = new List<PyClass>();
        if (_miBasesTuple is not null)
        {
            foreach (object? value in _miBasesTuple.Items) if (value is PyClass parent) parents.Add(parent);
        }
        else if (BaseClass is not null) parents.Add(BaseClass);
        foreach (PyClass parent in parents)
        {
            inherited |= parent.HasSlotLayout;
            dictionary |= parent.HasInstanceDictionary;
            foreach (var pair in parent._fixedSlotOwners)
            {
                foreach (PyClass prior in _fixedSlotOwners.Values)
                    if (!prior.IsSubclassOf(pair.Value) && !pair.Value.IsSubclassOf(prior))
                        throw new PyTypeError("multiple bases have instance lay-out conflict");
                if (_fixedSlotOwners.TryGetValue(pair.Key, out PyClass? priorOwner) && !ReferenceEquals(priorOwner, pair.Value))
                    throw new PyTypeError("multiple bases have instance lay-out conflict");
                _fixedSlotOwners[pair.Key] = pair.Value;
            }
        }
        HasSlotLayout = own || inherited;
        if (HasSlotLayout)
        {
            if (Attributes.ContainsKey("__dict__")) throw new PyNotImplementedError("slot class __dict__ override is not supported");
            HasInstanceDictionary = !own || dictionary;
            if (_nativeSlots is not null)
                foreach (string name in _nativeSlots)
                {
                    if (_fixedSlotOwners.ContainsKey(name)) throw new PyTypeError("duplicate native storage slot");
                    _fixedSlotOwners[name] = this;
                }
            foreach (string name in _fixedSlotOwners.Keys) GetOrAddInstanceSlot(name);
        }
        _slotLayoutReady = true;
    }

    private void GuardSlotClassMutation(string name)
    {
        if (HasSlotLayout && name == "__dict__") throw new PyNotImplementedError("slot class __dict__ mutation is not supported");
    }
}

// 名称经实际接收者布局映射到数组下标；继承时不复用父类数值索引。
internal sealed class PyInstanceSlotDescriptor(PyClass owner, string name) : PyNativeObject, IPyNativeDataDescriptor
{
    internal PyClass Owner => owner;
    internal string Name => name;
    public override string TypeName => "member_descriptor";
    public object? Bind(PyClass definingClass, object? self) => self is null or PyNone ? this : Read(self);
    private PyInstance Receiver(object? self) => self is PyInstance instance && instance.Class.IsSubclassOf(owner)
        ? instance : throw new PyTypeError($"descriptor '{name}' for '{owner.Name}' objects does not apply to this object");
    internal object? Read(object? self)
    {
        PyInstance instance = Receiver(self);
        if (instance.Class.TryGetInstanceSlot(name, out int slot) && instance.TryGetSlot(slot, out object? value)) return value;
        throw new PyAttributeError($"'{instance.Class.Name}' object has no attribute '{name}'");
    }
    internal void Write(object? self, object? value)
    {
        PyInstance instance = Receiver(self);
        instance.SetSlot(instance.Class.GetOrAddInstanceSlot(name), value);
    }
    internal void Delete(object? self)
    {
        PyInstance instance = Receiver(self);
        if (!instance.Class.TryGetInstanceSlot(name, out int slot) || !instance.IsSlotSet(slot))
            throw new PyAttributeError($"'{instance.Class.Name}' object has no attribute '{name}'");
        instance.ClearSlot(slot);
    }
    public override object? GetAttr(string attribute) => attribute switch
    {
        "__name__" => name, "__objclass__" => owner,
        "__get__" or "__set__" or "__delete__" => new Accessor(this, attribute),
        _ => throw Missing(attribute),
    };
    private sealed class Accessor(PyInstanceSlotDescriptor descriptor, string operation) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method-wrapper";
        public override object? GetAttr(string attribute) => attribute == "__name__" ? operation : throw Missing(attribute);
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            int count = operation == "__set__" ? 2 : 1;
            if (names.Length != 0 || args.Length < count || args.Length > (operation == "__get__" ? 2 : count))
                throw new PyTypeError("invalid slot descriptor arguments");
            if (operation == "__get__")
            {
                if (args[0] is null or PyNone)
                    return args.Length == 2 && args[1] is not null and not PyNone ? descriptor : throw new PyTypeError("__get__(None, None) is invalid");
                return descriptor.Read(args[0]);
            }
            if (operation == "__set__") descriptor.Write(args[0], args[1]); else descriptor.Delete(args[0]);
            return PyNone.Instance;
        }
    }
}
