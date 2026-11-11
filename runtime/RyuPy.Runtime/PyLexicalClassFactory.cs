// 新定义位置普通类的私有namespace事务；不创建CLR类，不借用ambient模块，不修改旧创建路径。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>静态编译的词法类体使用的冷事务入口；成功终结后由编译代码填cell并发布。</summary>
public static class PyLexicalClassFactory
{
    private sealed class Frame(string name, PyClass? parent, object? module, string qualname)
    {
        internal readonly string Name = name;
        internal readonly PyClass? Parent = parent;
        internal readonly Dictionary<string, object?> Namespace = new(StringComparer.Ordinal)
        { ["__module__"] = module ?? PyNone.Instance, ["__qualname__"] = qualname };
        // 0正文、1终结中、2完成、3失败；所有失败frame都不能重试成另一个类。
        internal byte Phase;
    }

    /// <summary>验证真实base能力并冻结引用；此时只创建namespace，不创建或发布类。</summary>
    public static object Prepare(string name, string qualname, object? module, object?[] bases)
    {
        try
        {
            if (bases.Length > 1) throw Unsupported("multiple bases");
            PyClass? parent = null;
            if (bases.Length == 1 && !ReferenceEquals(bases[0], PyBaseObject.Type))
            {
                parent = bases[0] as PyClass ?? throw Unsupported("non-ordinary base or metaclass protocol");
                ValidateAncestors(parent);
            }
            return new Frame(name, parent, module, qualname);
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    /// <summary>只判断本次定义的原始namespace，None仍是真实绑定。</summary>
    public static bool NamespaceContains(object frame, string name) => Open(frame).Namespace.ContainsKey(name);

    /// <summary>读取原始namespace，不走继承或描述符；缺名由编译代码选择回退或报错。</summary>
    public static object? NamespaceGet(object frame, string name) =>
        Open(frame).Namespace.TryGetValue(name, out object? value) ? value
        : throw (PyBaseException)PyErrors.PrepareRaise(new PyNameError($"name '{name}' is not defined"));

    /// <summary>正文写入只记录值，hook和descriptor能力在Finish统一检查。</summary>
    public static void NamespaceSet(object frame, string name, object? value) => Open(frame).Namespace[name] = value;

    /// <summary>正文成功之后重查完整父链和最终namespace，然后创建真实普通类。</summary>
    public static object Finish(object frame)
    {
        Frame current = Open(frame);
        current.Phase = 1;
        try
        {
            ValidateAncestors(current.Parent);
            ValidateNamespace(current.Namespace);
            var result = new PyClass(current.Name) { BaseClass = current.Parent };
            foreach (var pair in current.Namespace) result.Attributes[pair.Key] = pair.Value;
            result.FinalizeOrdinaryConstruction();
            result.EnableLexicalFactoryFamily();
            current.Phase = 2;
            return result;
        }
        catch (PyBaseException error)
        {
            current.Phase = 3;
            throw (PyBaseException)PyErrors.PrepareRaise(error);
        }
    }

    private static Frame Open(object value)
    {
        if (value is not Frame { Phase: 0 } frame)
            throw (PyBaseException)PyErrors.PrepareRaise(Unsupported("inactive definition namespace"));
        return frame;
    }

    internal static void ValidateDefinition(PyClass cls)
    {
        try
        {
            if (!cls.HasOrdinaryLexicalLayout) throw Unsupported("special-layout definition");
            ValidateAncestors(cls.BaseClass);
            ValidateNamespace(cls.Attributes);
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    internal static void ValidateFamily(PyClass cls)
    {
        try { ValidateAncestors(cls); }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    private static void ValidateAncestors(PyClass? parent)
    {
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? current = parent; current is not null; current = current.BaseClass)
        {
            if (!seen.Add(current) || !current.IsCompletedOrdinaryLexicalBase)
                throw Unsupported("unfinished or special-layout ancestor");
            ValidateNamespace(current.Attributes);
        }
    }

    private static void ValidateNamespace(Dictionary<string, object?> attributes)
    {
        foreach (var pair in attributes)
        {
            // 不搬用dict槽禁令：普通类已有数值/容器方法仍保留。__class__只是普通属性。
            if (pair.Key is "__slots__" or "__init_subclass__" or "__class_getitem__"
                or "__mro_entries__" or "__prepare__" or "__set_name__" or "__classcell__"
                or "__getattribute__" or "__setattr__" or "__delattr__")
                throw Unsupported($"class protocol {pair.Key}");
            if (pair.Value is PyProperty or PyStaticMethod or PyClassMethod) continue;
            if (pair.Value is IPyNativeDescriptor || HasDescriptorProtocol(pair.Value))
                throw Unsupported("custom descriptor");
        }
    }

    private static bool HasDescriptorProtocol(object? value)
    {
        PyClass? type = value switch
        {
            PyInstance instance => instance.Class,
            PyNamedTupleInstance tuple => tuple.Class,
            PyBaseException error => error.PythonClass,
            _ => null,
        };
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (; type is not null; type = type.BaseClass)
        {
            if (!seen.Add(type)) throw Unsupported("cyclic descriptor type");
            foreach (string name in new[] { "__get__", "__set__", "__delete__", "__set_name__" })
                if (type.Attributes.ContainsKey(name)) return true;
        }
        return false;
    }

    private static PyNotImplementedError Unsupported(string detail) =>
        new($"lexical class factory does not support {detail}");
}

public sealed partial class PyClass
{
    // 普通完成事实来自现有终结协议；既有类不新增守卫或改变布局。
    internal bool IsCompletedOrdinaryLexicalBase => _ordinaryFinalized && HasOrdinaryLexicalLayout;
    internal bool HasOrdinaryLexicalLayout => BuiltinBase is null && ExceptionFactory is null
        && !HasStringLayout && !HasListLayout && !HasDictLayout && NamedTupleLayout is null
        && !HasEnumMetadata && !DataclassActive && !IsNativeRuntimeClass
        && !IsTypingGenericFamily && !IsTypingIoFamily;
}
