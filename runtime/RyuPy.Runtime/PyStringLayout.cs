// 字符串派生实例的独立不可变载体；普通实例不新增payload字段，精确CLR string不换表示。
using System;

namespace RyuPy.Runtime;

internal sealed class PyStringInstance : PyInstance
{
    internal readonly string Value;
    internal PyStringInstance(PyClass cls, string value) : base(cls) => Value = value;
}

internal static class PyStringLayout
{
    internal static bool IsClass(PyClass cls) => cls.HasStringLayout;

    internal static bool TryGet(object? value, out string text)
    {
        switch (value)
        {
            case string exact: text = exact; return true;
            case PyStringInstance instance: text = instance.Value; return true;
            default: text = null!; return false;
        }
    }

    internal static string RequireRaw(object? value) => TryGet(value, out string text) ? text
        : throw Own(new PyTypeError($"expected str, not '{PyRepr.TypeName(value)}'"));

    // 先完成转换再分配；init收到原调用数组，不为转换重写或丢弃关键字。
    internal static object Construct(PyClass cls, object?[] args, string[] names, object?[] values)
    {
        cls.RequireStringReady();
        object? input = PyStringConstructor.Bind(args, 0, names, values);
        var instance = Allocate(cls, input);
        cls.InitializeStringInstance(instance, args, names, values);
        return instance;
    }

    private static PyStringInstance Allocate(PyClass cls, object? input) =>
        new(cls, RequireRaw(PyRepr.StrObject(input)));

    // 显式根allocator包含cls首参；不会调用__init__，也不按类名猜布局。
    internal static object New(object?[] args, string[] names, object?[] values)
    {
        if (args.Length == 0) throw Own(new PyTypeError("str.__new__(): not enough arguments"));
        object? target = args[0];
        if (ReferenceEquals(target, PyType.Get("str")))
            return PyRepr.StrObject(PyStringConstructor.Bind(args, 1, names, values));
        if (target is PyClass cls && IsClass(cls))
        {
            cls.RequireStringReady();
            return Allocate(cls, PyStringConstructor.Bind(args, 1, names, values));
        }
        if (target is PyClass or PyType or PyNativeType)
        {
            string name = target switch { PyClass c => c.Name, PyType t => t.Name, PyNativeType n => n.Name, _ => "?" };
            throw Own(new PyTypeError($"str.__new__({name}): {name} is not a subtype of str"));
        }
        throw Own(new PyTypeError($"str.__new__(X): X is not a type object ({PyRepr.TypeName(target)})"));
    }

    internal static object Init(object? receiver, object?[] args, string[] names, object?[] values)
    {
        if (!TryGet(receiver, out _))
            throw Own(new PyTypeError("str.__init__ requires a str instance"));
        if (receiver is PyStringInstance instance)
            instance.Class.CheckStringRootInit(args.Length, names.Length);
        return PyNone.Instance;
    }

    internal static bool TryInstanceMetadata(PyInstance instance, string name, out object? value)
    {
        value = null;
        if (instance is not PyStringInstance) return false;
        if (name == "__dict__") throw Own(new PyNotImplementedError("str subclass instance __dict__ is not supported"));
        if (name != "__class__") return false;
        value = instance.Class; return true;
    }

    internal static void GuardInstanceMutation(PyInstance instance, string name)
    {
        if (instance is PyStringInstance && name is "__class__" or "__dict__")
            throw Own(new PyNotImplementedError($"str subclass instance {name} mutation is not supported"));
    }

    internal static bool TryClassMetadata(PyClass cls, string name, out object? value)
    {
        value = null;
        if (!IsClass(cls)) return false;
        if (name == "__dict__") throw Own(new PyNotImplementedError("str subclass class __dict__ is not supported"));
        if (name == "__base__") { value = (object?)cls.BaseClass ?? PyType.Get("str"); return true; }
        if (name == "__bases__") { value = new PyTuple([(object?)cls.BaseClass ?? PyType.Get("str")]); return true; }
        return false;
    }

    internal static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
