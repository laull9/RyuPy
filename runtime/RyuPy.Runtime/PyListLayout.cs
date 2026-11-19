// list派生实例的独立可变载体；普通实例不加payload，精确PyList及数值背板保持原表示。
using System;

namespace RyuPy.Runtime;

internal sealed class PyListInstance : PyInstance
{
    internal readonly PyList Payload;
    internal PyListInstance(PyClass cls) : base(cls) => Payload = new PyList();
}

internal static class PyListLayout
{
    internal static bool IsClass(PyClass cls) => cls.HasListLayout;

    internal static bool TryGet(object? value, out PyList payload)
    {
        switch (value)
        {
            case PyList exact: payload = exact; return true;
            case PyListInstance instance: payload = instance.Payload; return true;
            default: payload = null!; return false;
        }
    }

    internal static PyList RequireRaw(object? value) => TryGet(value, out PyList payload) ? payload
        : throw Own(new PyTypeError($"expected list, not '{PyRepr.TypeName(value)}'"));

    // 分配器只创建空载体；源的消费完全属于实际选中的init，用户init收到全部原始实参。
    internal static object Construct(PyClass cls, object?[] args, string[] names, object?[] values)
    {
        cls.RequireListReady();
        var instance = new PyListInstance(cls);
        cls.InitializeListInstance(instance, args, names, values);
        return instance;
    }

    // CPython的list根allocator忽略额外位置和关键字参数；显式调用不会运行init。
    internal static object New(object?[] args, string[] names, object?[] values)
    {
        if (args.Length == 0) throw Own(new PyTypeError("list.__new__(): not enough arguments"));
        object? target = args[0];
        if (ReferenceEquals(target, PyType.Get("list"))) return new PyList();
        if (target is PyClass cls && IsClass(cls))
        {
            cls.RequireListReady();
            return new PyListInstance(cls);
        }
        if (target is PyClass or PyType or PyNativeType)
        {
            string name = target switch { PyClass c => c.Name, PyType t => t.Name, PyNativeType n => n.Name, _ => "?" };
            throw Own(new PyTypeError($"list.__new__({name}): {name} is not a subtype of list"));
        }
        throw Own(new PyTypeError($"list.__new__(X): X is not a type object ({PyRepr.TypeName(target)})"));
    }

    internal static object Init(object? receiver, object?[] args, string[] names, object?[] values)
    {
        if (!TryGet(receiver, out PyList payload))
            throw Own(new PyTypeError($"descriptor '__init__' requires a 'list' object but received a '{PyRepr.TypeName(receiver)}'"));
        // 参数绑定失败保留原值；绑定成功先clear，iter/hint/next失败则保留已提交前缀。
        if (names.Length != 0) throw Own(new PyTypeError("list() takes no keyword arguments"));
        if (args.Length > 1) throw Own(new PyTypeError($"list expected at most 1 argument, got {args.Length}"));
        payload.Clear();
        if (args.Length == 1) payload.Extend(args[0]);
        return PyNone.Instance;
    }

    internal static bool TryInstanceMetadata(PyInstance instance, string name, out object? value)
    {
        value = null;
        if (instance is not PyListInstance) return false;
        if (name == "__dict__") throw Own(new PyNotImplementedError("list subclass instance __dict__ is not supported"));
        if (name != "__class__") return false;
        value = instance.Class; return true;
    }

    internal static void GuardInstanceMutation(PyInstance instance, string name)
    {
        if (instance is PyListInstance && name is "__class__" or "__dict__")
            throw Own(new PyNotImplementedError($"list subclass instance {name} mutation is not supported"));
    }

    internal static bool TryClassMetadata(PyClass cls, string name, out object? value)
    {
        value = null;
        if (!IsClass(cls)) return false;
        if (name == "__dict__") throw Own(new PyNotImplementedError("list subclass class __dict__ is not supported"));
        if (name == "__base__") { value = (object?)cls.BaseClass ?? PyType.Get("list"); return true; }
        if (name == "__bases__") { value = new PyTuple([(object?)cls.BaseClass ?? PyType.Get("list")]); return true; }
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
