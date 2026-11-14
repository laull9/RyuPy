// 普通类体完成时处理new隐式staticmethod与eq的hash抑制；类后重绑不重算这些规则。
using System;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal void FinalizeOrdinaryConstruction()
    {
        CompleteMultipleInheritance();
        if (!IsLexicalFactoryFamily) { FinalizeOrdinaryConstructionCore(); return; }
        // 所有special早返之前验证新族，不能通过旧创建路径洗掉继承的能力身份。
        PyLexicalClassFactory.ValidateDefinition(this);
        _lexicalFinalizing = true;
        try { FinalizeOrdinaryConstructionCore(); }
        finally { _lexicalFinalizing = false; }
        RequireLexicalFactoryReady();
    }

    private void FinalizeOrdinaryConstructionCore()
    {
        PyNamedTupleLayout.GuardMutation(this);
        if (IsNativeRuntimeClass) PyTypingNamedTupleShell.RejectPending(this);
        if (Attributes.TryGetValue("__qualname__", out object? qualname))
            PyClassNamespaceMetadata.ValidateQualname(this, qualname, typed: false);
        if (HasStringLayout) { FinalizeStringConstruction(); return; }
        if (HasListLayout) { FinalizeListConstruction(); return; }
        if (HasDictLayout) { FinalizeDictConstruction(); return; }
        if (_ordinaryFinalized) throw new PyNotImplementedError("ordinary class is already finalized");
        FinalizeTypingGenericDefinition();
        ValidateTypingIoDefinition();
        ConstructionSlots slots = Construction();
        if (slots.Special)
        {
            if (slots.CustomNew) throw new PyNotImplementedError("custom __new__ for special class layouts is not supported");
            return; // 已有TestCase等合法专用链不包装、不标普通完成、不改变其mutation规则。
        }
        // 先验证所有最终值；验证失败不包装、不标完成、不改变版本。
        // 根协议改绑在真正构造时消费，不能抢在类体终结时替用户调用。
        if (slots.NewOwner?.IsTypingGenericRoot != true) EnsureSupportedSlot(slots.New, "__new__");
        if (slots.InitOwner?.IsTypingGenericRoot != true) EnsureSupportedSlot(slots.Init, "__init__");
        if (Attributes.TryGetValue("__new__", out object? ownNew) && RootWrapped(ownNew))
            throw new PyNotImplementedError("wrapped root __new__ is not supported");
        if (ownNew is PyFunction function) SetAttr("__new__", new PyStaticMethod(function));
        // 只看类体最终own命名空间：继承eq不抑制hash，own eq即使不可调用也抑制。
        // 真正写入None，后续删除/重绑按普通类属性与现有版本缓存处理。
        if (Attributes.ContainsKey("__eq__") && !Attributes.ContainsKey("__hash__"))
            SetAttr("__hash__", PyNone.Instance);
        FinalizeSlotLayout();
        PyAbc.FinalizeAbstractMethods(this);
        _ordinaryFinalized = true;
        CompleteTypingIoDefinition();
        CompleteTypingGenericDefinition();

    }

    private static bool RootWrapped(object? value) => value switch
    {
        PyStaticMethod wrapper => PyBaseObjectMethods.IsRootNew(wrapper.Value)
            || wrapper.Value is PyStaticMethod or PyClassMethod,
        PyClassMethod wrapper => PyBaseObjectMethods.IsRootNew(wrapper.Value)
            || wrapper.Value is PyStaticMethod or PyClassMethod,
        _ => false,
    };

    private void GuardConstructionSet(string name, object? value)
    {
        if (!_ordinaryFinalized || name is not ("__new__" or "__init__")) return;
        if (name == "__new__" && (PyBaseObjectMethods.IsRootNew(value) || RootWrapped(value)))
            throw (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError("resetting finalized __new__ to the root slot is not supported"));
        try { EnsureSupportedSlot(value, name); }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    private void GuardConstructionDelete(string name)
    {
        if (_ordinaryFinalized && name == "__new__" && Attributes.ContainsKey(name))
            throw (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError("deleting finalized __new__ is not supported"));
    }

    // 普通类只补构造槽及hash根；String仍走已核根描述符，不注入隐式TryMethod。
    internal bool TryRootConstructionAttribute(string name, object? self, out object? value)
    {
        value = null;
        if (NamedTupleLayout is not null) return false;
        if (HasDictLayout && PyDictDescriptors.IsAttribute(name))
        {
            // fromkeys绑定实际访问类；不能在根回退时退回canonical dict。
            value = name == "fromkeys" ? PyDictDescriptors.Bind(self ?? this, name)
                : self is null ? PyDictDescriptors.Unbound(name) : PyDictDescriptors.Bind(self, name);
            return true;
        }
        if (HasListLayout && PyListDescriptors.IsAttribute(name))
        {
            value = self is null ? PyListDescriptors.Unbound(name) : PyListDescriptors.Bind(self, name);
            return true;
        }
        if (HasStringLayout && PyStringDescriptors.IsMethod(name))
        {
            value = self is null ? PyStringDescriptors.Unbound(name) : PyStringDescriptors.Bind(self, name);
            return true;
        }
        // dataclass装饰前已经完成普通类；之后Special布局标记不撤销hash根资格。
        if (name == "__hash__" && HasOrdinaryHashSlots)
        {
            value = self is null ? PyBaseObjectMethods.Unbound(name)
                : PyBaseObjectMethods.BindConstruction(self, name);
            return true;
        }
        if (name is "__setattr__" or "__delattr__")
        {
            value = self is null ? PyBaseObjectMethods.Unbound(name)
                : PyBaseObjectMethods.BindConstruction(self, name);
            return true;
        }
        if (name is not ("__new__" or "__init__") || Construction().Special) return false;
        value = name == "__new__" || self is null ? PyBaseObjectMethods.Unbound(name)
            : PyBaseObjectMethods.BindConstruction(self, name);
        return true;
    }
}

public static partial class PyOps
{
    /// <summary>普通类体结束、类装饰器之前的唯一冷完成接点。</summary>
    /// <param name="value">实际普通类对象；不替换身份。</param>
    public static void FinalizeOrdinaryClass(object value)
    {
        if (PyTypingNamedTuple.TryFinishDefinition(value)) return;
        try
        {
            if (value is not PyClass cls)
                throw new PyTypeError("ordinary class finalization requires a class");
            cls.FinalizeOrdinaryConstruction();
        }
        catch (PyBaseException error)
        {
            if (value is PyClass failed) failed.FailTypingGenericDefinition();
            throw (PyBaseException)PyErrors.PrepareRaise(error);
        }
    }
}
