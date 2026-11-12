// 显式super的冷构造合同：真实类型/接收者身份，不借词法frame或旧方法TLS。
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyExplicitSuper
{
    // 两条公开调用入口均在实参求值/展开后到达这里；签名错误先于类型与能力检查。
    internal static object Construct(object?[] args, string[] names)
    {
        if (names.Length != 0) throw new PyTypeError("super() takes no keyword arguments");
        if (args.Length > 2)
            throw new PyTypeError($"super() expected at most 2 arguments, got {args.Length}");
        if (args.Length == 0)
            throw new PyNotImplementedError("zero-argument super requires an explicit lexical call frame");
        if (args[0] is not PyClass start)
        {
            if (args[0] is PyType or PyNativeType)
                throw Unsupported("native starting classes");
            throw new PyTypeError($"super() argument 1 must be a type, not {PyRepr.TypeName(args[0])}");
        }
        ValidateChain(start, null);
        if (args.Length == 1 || args[1] is PyNone)
            throw Unsupported("unbound receivers");
        object? receiver = args[1];
        if (receiver is PyClass { HasMultipleInheritance: true } receiverClass)
        {
            receiverClass.RequireLexicalFactoryReady();
            if (!ValidateChain(receiverClass, start)) throw Unrelated();
            return new PySuper(start, receiverClass);
        }
        if (receiver is PyClass or PyType or PyNativeType or PyNativeObject or PyNamedTupleInstance or PyBaseException)
            throw Unsupported("class receivers and native layouts");
        if (receiver is not PyInstance instance) throw Unrelated();
        instance.Class.RequireLexicalFactoryReady();
        // 检查完整链，不能找到start就跳过后面的特殊布局；不调用用户hash/eq/getter。
        if (!ValidateChain(instance.Class, start))
        {
            if (instance.Class.TryLookup("__class__", out _, out _)
                || instance.Class.TryLookup("__getattribute__", out _, out _)
                || instance.Class.TryLookup("__getattr__", out _, out _)
                || instance.Class.TryGetInstanceSlot("__class__", out int slot) && instance.IsSlotSet(slot))
                throw Unsupported("proxy __class__ fallback");
            throw Unrelated();
        }
        return new PySuper(start, instance);
    }

    // 单链与普通MI共用真实MRO；不以专用类的PyClass外壳冒充通用MRO。
    private static bool ValidateChain(PyClass cls, PyClass? start)
    {
        bool related = false;
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        foreach (PyClass current in cls.MroClasses())
        {
            if (!seen.Add(current)) throw Unsupported("cyclic class chains");
            if (current.BuiltinBase is not null || current.ExceptionFactory is not null
                || current.HasStringLayout || current.HasListLayout || current.HasDictLayout
                || current.NamedTupleLayout is not null || current.HasEnumMetadata
                || current.IsTypingIoFamily || current.IsTypingGenericFamily
                || current.DataclassActive || current.IsNativeRuntimeClass)
                throw Unsupported("native class layouts");
            related |= ReferenceEquals(current, start);
        }
        return related;
    }

    private static PyTypeError Unrelated() =>
        new("super(type, obj): obj must be an instance or subtype of type");
    private static PyNotImplementedError Unsupported(string feature) =>
        new($"explicit super {feature} are not supported");
}
