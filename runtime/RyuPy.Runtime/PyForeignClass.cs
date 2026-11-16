// 跨域派生类与实例的托管侧语义接入：管理外部基类链、类终结同步、实例构造与属性反射转发。
// 真实实例数据与内存布局驻留在外部执行域，本文件不复制或伪造 C 结构体。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    /// <summary>直接继承的外部类型对象（例如 CPython 的 <c>tk.Tk</c>）；普通类为 <c>null</c>。</summary>
    internal IPyForeignObject? ForeignBase { get; set; }

    /// <summary>包含外部类型的多继承基类列表；单继承时为 <c>null</c>。</summary>
    internal object?[]? ForeignMiBases { get; set; }

    /// <summary>类体完成之后在外部执行域创建的真实类型对象。</summary>
    internal IPyForeignObject? ForeignClass { get; set; }

    /// <summary>当前类或任意祖先是否继承自外部类型。</summary>
    internal bool HasForeignHierarchy =>
        ForeignBase is not null || ForeignMiBases is not null || BaseClass?.HasForeignHierarchy == true;

    /// <summary>在多继承序列中检验当前类链（用于 MI + 外部类型的 MRO）。</summary>
    internal bool HasCompletedForeignAncestor
    {
        get
        {
            if (ForeignClass is null) return false;
            return BaseClass is null || !BaseClass.HasForeignHierarchy || BaseClass.HasCompletedForeignAncestor;
        }
    }

    /// <summary>完成跨域派生类的类体校验与外部真实类型构造。</summary>
    internal void FinalizeForeignDerivedConstruction()
    {
        if (_ordinaryFinalized) throw new PyNotImplementedError("ordinary class is already finalized");
        if (Attributes.TryGetValue("__qualname__", out object? qualname))
        {
            PyClassNamespaceMetadata.ValidateQualname(this, qualname, typed: false);
        }

        // 先检查用户类体自定义的 __new__ / __init__ 合法性（不限制外部基类本身的槽）。
        if (Attributes.TryGetValue("__new__", out object? ownNew))
        {
            EnsureSupportedSlot(ownNew, "__new__");
            if (RootWrapped(ownNew)) throw new PyNotImplementedError("wrapped root __new__ is not supported");
        }
        if (Attributes.TryGetValue("__init__", out object? ownInit))
        {
            EnsureSupportedSlot(ownInit, "__init__");
        }

        object?[] bases = ForeignCreationBases();
        var namespaceAttributes = new Dictionary<string, object?>(Attributes, StringComparer.Ordinal);
        if (ownNew is PyFunction newFunction)
        {
            namespaceAttributes["__new__"] = new PyStaticMethod(newFunction);
        }
        if (namespaceAttributes.ContainsKey("__eq__") && !namespaceAttributes.ContainsKey("__hash__"))
        {
            namespaceAttributes["__hash__"] = PyNone.Instance;
        }

        // 外部类型创建可能因不可继承基类、元类冲突或 __init_subclass__ 抛错；失败前不改动本类状态。
        IPyForeignObject created = PyForeignClassBridge.Require().CreateDerivedClass(this, Name, bases, namespaceAttributes);

        if (ownNew is PyFunction)
        {
            Attributes["__new__"] = namespaceAttributes["__new__"];
        }
        if (Attributes.ContainsKey("__eq__") && !Attributes.ContainsKey("__hash__"))
        {
            Attributes["__hash__"] = PyNone.Instance;
        }
        ForeignClass = created;
        _ordinaryFinalized = true;
        Touch();
    }

    internal object?[] ForeignCreationBases()
    {
        if (ForeignMiBases is { } miBases)
        {
            return miBases;
        }
        if (BaseClass is { } parent)
        {
            if (parent.ForeignClass is null)
            {
                throw new PyNotImplementedError($"base foreign-derived class '{parent.Name}' is not finalized");
            }
            return [parent];
        }
        if (ForeignBase is { } foreignBase)
        {
            return [foreignBase];
        }
        throw new PyRuntimeError($"foreign-derived class '{Name}' is missing a foreign base");
    }

    /// <summary>若当前类已关联外部真实类型，则将属性写入同步到外部执行域。</summary>
    internal bool TryForeignSetAttr(string name, object? value)
    {
        if (ForeignClass is not { } foreignClass) return false;
        PyForeignClassBridge.Require().SetClassAttribute(foreignClass, this, name, value);
        return true;
    }

    /// <summary>若当前类已关联外部真实类型，则将属性删除同步到外部执行域。</summary>
    internal bool TryForeignDeleteAttr(string name)
    {
        if (ForeignClass is not { } foreignClass) return false;
        PyForeignClassBridge.Require().DeleteClassAttribute(foreignClass, name);
        return true;
    }

    /// <summary>通过外部真实类型构造实例（执行 CPython 的 __new__ 与 __init__）。</summary>
    internal object? ForeignInvoke(object?[] args, string[] kwNames, object?[] kwValues)
    {
        if (ForeignClass is not { } foreignClass)
        {
            throw new PyNotImplementedError($"foreign-derived class '{Name}' is not finalized");
        }
        return foreignClass.Invoke(args, kwNames, kwValues);
    }

    /// <summary>通过 <c>object.__new__(cls, ...)</c> 分配真实外部实例。</summary>
    internal object? ForeignAllocateFromObject(object?[] extraArgs, string[] kwNames, object?[] kwValues)
    {
        if (ForeignClass is not { } foreignClass)
        {
            throw new PyNotImplementedError($"foreign-derived class '{Name}' is not finalized");
        }
        return PyForeignClassBridge.Require().AllocateFromBaseObject(foreignClass, extraArgs, kwNames, kwValues);
    }

    /// <summary>读取跨域派生类的属性与真实反射元数据。</summary>
    internal bool TryForeignGetClassAttr(string name, out object? value)
    {
        if (ForeignClass is not { } foreignClass)
        {
            value = null;
            return false;
        }
        switch (name)
        {
            case "__class__":
                object meta = foreignClass.ForeignType;
                value = PyForeignClassBridge.IsPlainType(meta) ? PyType.Get("type") : meta;
                return true;
            case "__name__":
                value = Name;
                return true;
            case "__qualname__":
                value = Attributes.TryGetValue(name, out object? qualname) ? qualname : Name;
                return true;
            case "__module__":
                if (Attributes.TryGetValue(name, out object? module))
                {
                    value = module;
                    return true;
                }
                value = PyObject.GetAttr(foreignClass, "__module__");
                return true;
            default:
                value = PyObject.GetAttr(foreignClass, name);
                return true;
        }
    }
}

public partial class PyInstance
{
    /// <summary>跨域派生类实例关联的外部真实实例；普通原生实例为 <c>null</c>。</summary>
    internal IPyForeignObject? ForeignInstance { get; set; }

    /// <summary>构造关联外部真实实例的托管代理视图，不分配原生 <c>_slots</c> 数组。</summary>
    internal PyInstance(PyClass cls, IPyForeignObject foreignInstance) : this(cls, skipSlots: true)
    {
        ForeignInstance = foreignInstance;
    }
}
