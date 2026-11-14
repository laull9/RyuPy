// dict派生实例的独立键值载体；普通实例不加payload，精确PyDict保持原哈希存储表示。
using System;

namespace RyuPy.Runtime;

internal sealed class PyDictInstance : PyInstance
{
    internal readonly PyDict Payload;
    internal PyDictInstance(PyClass cls) : base(cls) => Payload = new PyDict();
}

internal static class PyDictLayout
{
    internal static bool IsClass(PyClass cls) => cls.HasDictLayout;

    internal static bool TryGet(object? value, out PyDict payload)
    {
        switch (value)
        {
            case PyDict exact: payload = exact; return true;
            case PyDictInstance instance: payload = instance.Payload; return true;
            default: payload = null!; return false;
        }
    }

    internal static PyDict RequireRaw(object? value) => TryGet(value, out PyDict payload) ? payload
        : throw Own(new PyTypeError($"expected dict, not '{PyRepr.TypeName(value)}'"));

    // 分配器只创建空载体；源的消费完全属于实际选中的init，用户init收到全部原始实参。
    internal static object Construct(PyClass cls, object?[] args, string[] names, object?[] values)
    {
        cls.RequireDictReady();
        var instance = new PyDictInstance(cls);
        cls.InitializeDictInstance(instance, args, names, values);
        return instance;
    }

    // CPython的dict根allocator忽略额外位置和关键字参数；显式调用不会运行init。
    internal static object New(object?[] args, string[] names, object?[] values)
    {
        if (args.Length == 0) throw Own(new PyTypeError("dict.__new__(): not enough arguments"));
        object? target = args[0];
        if (ReferenceEquals(target, PyOrderedDict.Type)) throw PyOrderedDict.Unsupported("dict.__new__");
        if (ReferenceEquals(target, PyType.Get("dict"))) return new PyDict();
        if (target is PyClass cls && IsClass(cls))
        {
            cls.RequireDictReady();
            if (cls.HasDictC3Layout) cls.RequireLexicalFactoryReady();
            return new PyDictInstance(cls);
        }
        if (target is PyClass or PyType or PyNativeType)
        {
            string name = target switch { PyClass c => c.Name, PyType t => t.Name, PyNativeType n => n.Name, _ => "?" };
            throw Own(new PyTypeError($"dict.__new__({name}): {name} is not a subtype of dict"));
        }
        throw Own(new PyTypeError($"dict.__new__(X): X is not a type object ({PyRepr.TypeName(target)})"));
    }

    internal static object Init(object? receiver, object?[] args, string[] names, object?[] values)
    {
        if (!TryGet(receiver, out _))
            throw Own(new PyTypeError($"descriptor '__init__' requires a 'dict' object but received a '{PyRepr.TypeName(receiver)}'"));
        // dict重复初始化只更新同一存储；签名与逐项提交由唯一有限更新入口负责。
        PyDictProtocols.CallUpdate(receiver!, args, names, values, construct: true);
        return PyNone.Instance;
    }

    internal static bool TryInstanceMetadata(PyInstance instance, string name, out object? value)
    {
        value = null;
        if (instance is not PyDictInstance) return false;
        if (name == "__dict__") throw Own(new PyNotImplementedError("dict subclass instance __dict__ is not supported"));
        if (name != "__class__") return false;
        // 用户类项可遮蔽根类型属性；交给统一property/实例字段/类链次序，真实TypeOf不变。
        if (instance.Class.TryLookup(name, out _, out _)) return false;
        value = instance.Class; return true;
    }

    internal static void GuardInstanceMutation(PyInstance instance, string name)
    {
        if (instance is not PyDictInstance) return;
        // 类链遮蔽后的写/删是普通descriptor或字段操作，不是改变真实Class/layout。
        if (name == "__class__" && instance.Class.TryLookup(name, out _, out _)) return;
        if (name is "__class__" or "__dict__")
            throw Own(new PyNotImplementedError($"dict subclass instance {name} mutation is not supported"));
    }

    internal static bool TryClassMetadata(PyClass cls, string name, out object? value)
    {
        value = null;
        if (!IsClass(cls)) return false;
        if (name == "__dict__") throw Own(new PyNotImplementedError("dict subclass class __dict__ is not supported"));
        if (cls.HasDictC3Layout)
        {
            if (name == "__base__") { value = cls.DictBestBase; return true; }
            if (name == "__bases__") { value = cls.DirectBasesTuple(); return true; }
            if (name == "__mro__") { value = cls.MroTuple(); return true; }
        }
        if (name == "__base__") { value = (object?)cls.BaseClass ?? PyType.Get("dict"); return true; }
        if (name == "__bases__") { value = new PyTuple([(object?)cls.BaseClass ?? PyType.Get("dict")]); return true; }
        return false;
    }

    // CP类型repr只读本类元数据；非字符串模块与builtins回Name，绝不触发用户转换。
    internal static string ClassRepr(PyClass cls)
    {
        if (cls.Attributes.TryGetValue("__module__", out object? rawModule)
            && PyStringLayout.TryGet(rawModule, out string module) && module != "builtins")
        {
            string qualname = cls.Attributes.TryGetValue("__qualname__", out object? rawName)
                && PyStringLayout.TryGet(rawName, out string text) ? text : cls.Name;
            return $"<class '{module}.{qualname}'>";
        }
        return $"<class '{cls.Name}'>";
    }

    internal static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
