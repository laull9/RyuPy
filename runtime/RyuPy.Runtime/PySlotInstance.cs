// 仅有限slots族公开真实实例dict；旧普通实例继续使用原数组，固定槽不复制进dict。
using System.Collections.Generic;

namespace RyuPy.Runtime;

public partial class PyInstance
{
    private PyDict? _layoutDictionary;
    internal PyDict SlotDictionary => Class.HasInstanceDictionary ? _layoutDictionary ??= new PyDict()
        : throw new PyAttributeError($"'{Class.Name}' object has no attribute '__dict__'");
    internal IEnumerable<string> SlotDictionaryNames()
    {
        if (_layoutDictionary is null) yield break;
        foreach (object? key in _layoutDictionary.Keys) if (key is string name) yield return name;
    }
    internal bool TrySlotDictionaryField(string name, out object? value)
    {
        value = null;
        return _layoutDictionary is not null && _layoutDictionary.TryGet(name, out value);
    }
    internal void WriteSlotDictionaryField(string name, object? value)
    {
        if (name == "__class__") throw new PyNotImplementedError("slot instance __class__ reassignment is not supported");
        if (!Class.HasInstanceDictionary) throw new PyAttributeError($"'{Class.Name}' object has no attribute '{name}'");
        if (name == "__dict__")
        {
            if (value is PyDict dictionary) { _layoutDictionary = dictionary; return; }
            if (value is PyDictInstance or IPyNativeMapping or PyOrderedDict)
                throw new PyNotImplementedError("dict subclass instance dictionary replacement is not supported");
            throw new PyTypeError("__dict__ must be set to a dictionary");
        }
        SlotDictionary.Set(name, value);
    }
    internal bool DeleteSlotDictionaryField(string name)
    {
        if (!Class.HasInstanceDictionary) return false;
        if (name == "__dict__") { _layoutDictionary = null; return true; }
        if (_layoutDictionary is null || !_layoutDictionary.Contains(name)) return false;
        _layoutDictionary.Delete(name);
        return true;
    }
    internal bool TryOwnAttribute(string name, out object? value)
    {
        value = null;
        if (Class.HasSlotLayout)
        {
            if (Class.TryLookup(name, out _, out object? raw) && raw is PyInstanceSlotDescriptor)
                return Class.TryGetInstanceSlot(name, out int fixedSlot) && TryGetSlot(fixedSlot, out value);
            return TrySlotDictionaryField(name, out value);
        }
        return Class.TryGetInstanceSlot(name, out int slot) && TryGetSlot(slot, out value);
    }
}
