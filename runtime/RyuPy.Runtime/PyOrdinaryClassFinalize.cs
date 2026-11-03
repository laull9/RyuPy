// 仅普通类创建结束处理__new__隐式staticmethod；类后重绑绝不普遍包装。
using System;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal void FinalizeOrdinaryConstruction()
    {
        if (_ordinaryFinalized) throw new PyNotImplementedError("ordinary class is already finalized");
        ConstructionSlots slots = Construction();
        if (slots.Special)
        {
            if (slots.CustomNew) throw new PyNotImplementedError("custom __new__ for special class layouts is not supported");
            return; // 已有TestCase等合法专用链不包装、不标普通完成、不改变其mutation规则。
        }
        // 先验证所有最终值；验证失败不包装、不标完成、不改变版本。
        EnsureSupportedSlot(slots.New, "__new__");
        EnsureSupportedSlot(slots.Init, "__init__");
        if (Attributes.TryGetValue("__new__", out object? ownNew) && RootWrapped(ownNew))
            throw new PyNotImplementedError("wrapped root __new__ is not supported");
        if (ownNew is PyFunction function) SetAttr("__new__", new PyStaticMethod(function));
        _ordinaryFinalized = true;
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

    // 普通类的逻辑object终点只提供本切已实现的构造槽，不扩完整MRO/descriptor协议。
    internal bool TryRootConstructionAttribute(string name, object? self, out object? value)
    {
        value = null;
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
        try
        {
            if (value is not PyClass cls)
                throw new PyTypeError("ordinary class finalization requires a class");
            cls.FinalizeOrdinaryConstruction();
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }
}
