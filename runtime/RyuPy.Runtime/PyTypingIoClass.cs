// IO 族的有限定义与空布局不变量；不改变普通类的 descriptor 框架或实例槽快路径。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal enum PyTypingIoKind : byte { None, IO, Binary, Text, Derived }

public sealed partial class PyClass
{
    internal PyTypingIoKind TypingIoKind { get; set; }
    internal bool IsTypingIoFamily => TypingIoKind != PyTypingIoKind.None;
    internal bool IsTypingIoRoot => TypingIoKind is PyTypingIoKind.IO or PyTypingIoKind.Binary or PyTypingIoKind.Text;
    private bool _typingIoReady;
    private PyTuple? _typingIoBases;

    private static readonly HashSet<string> TypingIoReserved = new(StringComparer.Ordinal)
    {
        "__base__", "__bases__", "__mro__", "mro", "__orig_bases__", "__parameters__", "__slots__",
        "__class__", "__dict__", "__name__", "__qualname__", "__init_subclass__", "__class_getitem__", "__mro_entries__", "__prepare__",
    };

    internal void RequireTypingIoReady()
    {
        if (IsTypingIoFamily && !_typingIoReady)
            throw PyTypingIo.Unsupported("typing IO class is not finalized");
    }

    private void ValidateTypingIoDefinition()
    {
        if (!IsTypingIoFamily) return;
        BaseClass?.RequireTypingIoReady();
        foreach (string name in Attributes.Keys)
        {
            if (!TypingIoReserved.Contains(name) || name == "__qualname__") continue;
            if (IsTypingIoRoot && name == "__slots__") continue;
            if (TypingIoKind is PyTypingIoKind.Binary or PyTypingIoKind.Text && name == "__parameters__") continue;
            throw PyTypingIo.Unsupported($"typing IO class slot {name} is not supported");
        }
    }

    private void CompleteTypingIoDefinition()
    {
        if (!IsTypingIoFamily) return;
        if (TypingIoKind != PyTypingIoKind.IO) _typingIoBases = new PyTuple([BaseClass!]);
        _typingIoReady = true;
    }

    private void GuardTypingIoMutation(string name, bool deleting)
    {
        if (!IsTypingIoFamily || !_typingIoReady) return;
        if (deleting && name == "__module__")
            throw PyTypingIo.Error(new PyTypeError($"cannot delete '__module__' attribute of type '{Name}'"));
        if (TypingIoReserved.Contains(name) || name is "__name__" or "__qualname__")
            throw PyTypingIo.Unsupported($"changing typing IO class metadata {name} is not supported");
    }

    private void GuardTypingIoInstanceSlot(string name)
    {
        if (!IsTypingIoFamily) return;
        PyTypingIo.GuardInstanceReservedMutation(this, name);
        if (IsTypingIoRoot) PyTypingIo.RejectRootInstanceMutation(this, name);
    }

    internal PyTuple TypingIoBases => _typingIoBases
        ?? throw PyTypingIo.Unsupported("typing IO generic base reflection is not supported");
}
