using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>collections.abc canonical类族的有限协议检查；结构判断不执行用户iter/len，不开放动态注册。</summary>
public static partial class PyAbc
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    private static readonly Dictionary<string, PyClass> Roots = new(StringComparer.Ordinal);
    private static readonly HashSet<PyClass> RootIdentities = new(ReferenceEqualityComparer.Instance);
    /// <summary>ABC模块属性表；canonical身份独立于可重绑导出。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    /// <summary>保留旧ABI；原生代理不再是ABC根。</summary>
    public static bool IsAbc(PyNativeType type) => false;
    /// <summary>保留旧ABI；原生代理不再是ABC根。</summary>
    public static bool IsSubclass(object? child, PyNativeType parent) => false;
    internal static bool IsAbc(PyClass cls) => RootIdentities.Contains(cls);
    internal static PyClass RootType(string name) { _ = Attributes.Value; return Roots[name]; }
    internal static bool Accepts(PyClass root, object? value) =>
        PyObject.IsInstance(value, root) || Accepts(root.Name, value);
    /// <summary>有限 issubclass 检查；使用声明的类成员而非实例操作。</summary>
    internal static bool IsSubclass(object? child, PyClass parent)
    {
        if (!IsAbc(parent)) return false;
        // OD视图/游标的type()由驻留内建type表示，类级ABC不执行实例协议或按用户类名猜测。
        if (ReferenceEquals(child, PyType.Get("odict_keys"))) return Known(parent.Name, "dict_keys");
        if (ReferenceEquals(child, PyType.Get("odict_values"))) return Known(parent.Name, "dict_values");
        if (ReferenceEquals(child, PyType.Get("odict_items"))) return Known(parent.Name, "dict_items");
        if (ReferenceEquals(child, PyType.Get("odict_iterator"))) return Inherits("Iterator", parent.Name);
        if (ReferenceEquals(child, PyOrderedDict.Type)) return Known(parent.Name, "dict");
        if (ReferenceEquals(child, PyMappingProxy.Type)) return ReadOnlyMapping(parent.Name);
        if (child is PyClass canonical && canonical.IsSubclassOf(parent)) return true;
        if (child is PyClass { NamedTupleLayout: not null }) return Known(parent.Name, "tuple");
        if (child is PyClass { HasListLayout: true } listClass)
            return Known(parent.Name, "list") || Structural(parent.Name, member => HasClassMember(listClass, member));
        if (child is PyClass { HasDictLayout: true } dictClass)
            return Known(parent.Name, "dict") || Structural(parent.Name, member => HasClassMember(dictClass, member));
        if (child is PyClass cls) return Structural(parent.Name, member => HasClassMember(cls, member));
        string? name = child switch { PyType builtin => builtin.Name, PyNativeType type => type.Name, _ => null };
        if (name is null) throw new PyTypeError("issubclass() arg 1 must be a class");
        return Known(parent.Name, name);
    }
    private static bool Inherits(string child, string parent) => child == parent || (child switch
    {
        "MutableMapping" => parent is "Mapping" or "Collection" or "Iterable" or "Sized" or "Container",
        "Mapping" or "Sequence" or "Set" => parent is "Collection" or "Iterable" or "Sized" or "Container",
        "MutableSequence" => parent is "Sequence" or "Collection" or "Iterable" or "Sized" or "Container",
        "MutableSet" => parent is "Set" or "Collection" or "Iterable" or "Sized" or "Container",
        "Collection" => parent is "Iterable" or "Sized" or "Container",
        "Iterator" => parent == "Iterable",
        "Generator" => parent is "Iterator" or "Iterable",
        _ => false,
    });
    private static bool Accepts(string protocol, object? value)
    {
        if (value is PyOrderedDict) return Known(protocol, "dict");
        if (value is PyOrderedDictView odView) return Known(protocol, odView.Kind switch
        { PyDictViewKind.Keys => "dict_keys", PyDictViewKind.Values => "dict_values", _ => "dict_items" });
        if (value is PyMappingProxy) return ReadOnlyMapping(protocol);
        if (value is PyNamedTupleInstance) return Known(protocol, "tuple");
        if (value is PyListInstance list)
            return Known(protocol, "list") || Structural(protocol, name => HasClassMember(list.Class, name));
        if (value is PyDictInstance dict)
            return Known(protocol, "dict") || Structural(protocol, name => HasClassMember(dict.Class, name));
        if (value is PyInstance instance) return Structural(protocol, name => HasClassMember(instance.Class, name));
        if (value is PyBaseException exception && exception.PythonClass is not null)
            return Structural(protocol, name => HasClassMember(exception.PythonClass, name));
        if (protocol == "Callable" && value is IPyNativeCallable) return true;
        if (value is PyNativeObject native)
        {
            if (Known(protocol, native.TypeName)) return true;
            return Structural(protocol, name => HasNativeMember(native, name));
        }
        if (protocol == "Generator" && value is PyGenerator) return true;
        if (protocol == "Iterator" && value is IPyIterator) return true;
        if (protocol == "Iterable" && value is IPyIterator) return true;
        if (protocol == "Callable") return PyOps.Truthy(PyOps.CallBuiltin("callable", [value], [], []));
        return Known(protocol, value is PyRange ? "range" : PyRepr.TypeName(value));
    }
    private static bool HasClassMember(PyClass cls, string name)
    {
        if (cls.TryLookup(name, out _, out object? member)) return member is not null and not PyNone;
        // list根槽不写入用户类字典；结构检查也须见到继承的iter等有效槽，hash=None不算。
        return name != "__hash__" && (cls.HasListLayout && PyListDescriptors.IsAttribute(name)
            || cls.HasDictLayout && PyDictDescriptors.IsAttribute(name));
    }
    private static bool HasNativeMember(PyNativeObject value, string name)
    {
        return PyObject.TryGetNativeSpecial(value, name, out object? member) && member is not null and not PyNone;
    }
    private static bool Structural(string protocol, Func<string, bool> has) => protocol switch
    {
        "Iterable" => has("__iter__"),
        "Reversible" => has("__iter__") && has("__reversed__"),
        "Iterator" => has("__iter__") && has("__next__"),
        "Generator" => has("__iter__") && has("__next__") && has("send") && has("throw") && has("close"),
        "Sized" => has("__len__"),
        "Container" => has("__contains__"),
        "Collection" => has("__iter__") && has("__len__") && has("__contains__"),
        "Callable" => has("__call__"),
        _ => false,
    };
    // 仅真实载体/稳定type进入，不靠同名native对象冒充映射。
    private static bool ReadOnlyMapping(string protocol) =>
        protocol is "Mapping" or "Iterable" or "Sized" or "Container" or "Collection" or "Reversible";

    private static bool Known(string protocol, string name)
    {
        bool sequence = name is "list" or "tuple" or "str" or "bytes" or "bytearray" or "range" or "deque" or "version_info";
        bool mapping = name is "dict" or "defaultdict" or "Counter";
        bool set = name is "set" or "frozenset" or "dict_keys" or "dict_items";
        bool view = name is "dict_values" or "dict_keys" or "dict_items";
        bool iterator = name is "map" or "generator" or "chain" or "islice" or "repeat" or "_deque_iterator" or "_deque_reverse_iterator";
        return protocol switch
        {
            "Reversible" => sequence || mapping || view,
            "MappingView" => view,
            "KeysView" => name == "dict_keys",
            "ItemsView" => name == "dict_items",
            "ValuesView" => name == "dict_values",
            "Sequence" => sequence,
            "MutableSequence" => name is "list" or "bytearray" or "deque",
            "Mapping" or "MutableMapping" => mapping,
            "Set" => set,
            "MutableSet" => name == "set",
            "Iterable" => sequence || mapping || set || view || iterator,
            "Iterator" => iterator,
            "Generator" => name == "generator",
            "Sized" or "Container" or "Collection" => sequence || mapping || set || view,
            "Callable" => name is "function" or "builtin_function_or_method" or "method" or "type",
            _ => false,
        };
    }
}
