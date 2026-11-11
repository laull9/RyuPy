// 实例模式的零参super只消费显式词法frame；一等类型及无frame调用绝不借用旧TLS。
using System;

namespace RyuPy.Runtime;

/// <summary>真实super类型身份与当前有限的普通单继承实例构造协议。</summary>
public static class PyLexicalSuper
{
    /// <summary>所有模块共享的真实内建super类型。</summary>
    public static PyNativeType Type { get; } = new("super", "builtins", ConstructWithoutFrame,
        static value => value is PySuper);

    private static object? ConstructWithoutFrame(object?[] args, string[] names, object?[] values)
    {
        if (args.Length != 0 || names.Length != 0)
            return PyExplicitSuper.Construct(args, names);
        throw new PyNotImplementedError("zero-argument super requires an explicit lexical call frame");
    }

    // 保留旧PE零参ABI/TLS；显式参数与一等类型走同一冷合同，不创建捕获委托。
    internal static object LegacyConstruct(object?[] args, string[] kwNames)
    {
        if (args.Length != 0 || kwNames.Length != 0)
            return PyExplicitSuper.Construct(args, kwNames);
        return PyMethodContext.MakeSuper();
    }

    internal static object Construct(object? environment, long classSlot, object? arg0, long argKind)
    {
        if (argKind == -1)
            throw new PyNotImplementedError("super lexical frame shape is not supported");
        // 错误次序按CP：缺首参、删除首参、缺/空类cell，最后才校验类及接收者。
        if (argKind == 0) throw new PyRuntimeError("super(): no arguments");
        object? receiver = argKind switch
        {
            1 => arg0,
            2 => ReadCell(arg0),
            _ => throw new PyNotImplementedError("super argument frame kind is not supported"),
        };
        if (receiver is null or PyUnbound) throw new PyRuntimeError("super(): arg[0] deleted");
        if (classSlot < 0) throw new PyRuntimeError("super(): __class__ cell not found");
        if (environment is not object?[] cells || classSlot >= cells.LongLength)
            throw new PyNotImplementedError("super class environment is not supported");
        object? classValue = ReadCell(cells[classSlot]);
        if (classValue is null or PyUnbound) throw new PyRuntimeError("super(): empty __class__ cell");
        if (classValue is not PyClass start)
        {
            if (classValue is PyType or PyNativeType)
                throw new PyNotImplementedError("super native starting classes are not supported");
            throw new PyRuntimeError($"super(): __class__ is not a type ({PyRepr.TypeName(classValue)})");
        }
        GuardOrdinaryClass(start);
        // CP将真实None首参解释为未绑定super；本切不开放未绑定代理。
        if (receiver is PyNone)
            throw new PyNotImplementedError("unbound super receivers are not supported");
        if (receiver is PyClass or PyType or PyNativeType or PyNativeObject or PyNamedTupleInstance or PyBaseException)
            throw new PyNotImplementedError("super class receivers and native layouts are not supported");
        if (receiver is not PyInstance instance)
            throw new PyTypeError("super(type, obj): obj must be an instance or subtype of type");
        instance.Class.RequireLexicalFactoryReady();
        bool related = false;
        for (PyClass? current = instance.Class; current is not null; current = current.BaseClass)
        {
            GuardOrdinaryClass(current);
            related |= ReferenceEquals(current, start);
        }
        if (!related)
        {
            // CP可能继续询问代理的__class__；此切不执行自定义属性协议，也不伪装成功。
            if (instance.Class.TryLookup("__class__", out _, out _)
                || instance.Class.TryLookup("__getattribute__", out _, out _)
                || instance.Class.TryLookup("__getattr__", out _, out _)
                || instance.Class.TryGetInstanceSlot("__class__", out int slot) && instance.IsSlotSet(slot))
                throw new PyNotImplementedError("super proxy __class__ fallback is not supported");
            throw new PyTypeError("super(type, obj): obj must be an instance or subtype of type");
        }
        return new PySuper(start, receiver);
    }

    private static object? ReadCell(object? cell) => cell is object?[] { Length: 1 } values ? values[0]
        : throw new PyNotImplementedError("super requires a real lexical cell");

    private static void GuardOrdinaryClass(PyClass cls)
    {
        if (cls.BuiltinBase is not null || cls.HasStringLayout || cls.HasListLayout || cls.HasDictLayout
            || cls.NamedTupleLayout is not null || cls.HasEnumMetadata || cls.IsTypingIoFamily || cls.IsTypingGenericFamily)
            throw new PyNotImplementedError("super native class layouts are not supported");
    }

    // CP除__class__外可先查基类同名值再回退自身成员；此切尚未实现完整反射，统一明确拒绝。
    internal static void GuardAttribute(string name)
    {
        if (name is "__thisclass__" or "__self__" or "__self_class__" or "__get__"
            or "__mro__" or "mro" or "__dict__" or "__bases__" or "__base__")
            throw new PyNotImplementedError($"super reflection '{name}' is not supported");
    }

    internal static void GuardDescriptor(object? raw)
    {
        if (raw is IPyNativeDescriptor)
            throw new PyNotImplementedError("super native descriptor binding is not supported");
        PyClass? type = raw switch
        {
            PyInstance instance => instance.Class,
            PyNamedTupleInstance tuple => tuple.Class,
            PyBaseException exception => exception.PythonClass,
            _ => null,
        };
        if (type is not null && type.TryLookup("__get__", out _, out _))
            throw new PyNotImplementedError("super custom descriptor binding is not supported");
    }
}

public static partial class PyOps
{
    /// <summary>先按真实callee身份分流，再惰性读取词法Env类cell及当前首个位置参数。</summary>
    /// <param name="site">沿用原零参值调用的PIC站点，普通callee保留既有缓存与失效规则。</param>
    /// <param name="callee">已求值并冻结的调用目标。</param>
    /// <param name="environment">当前函数真实闭包环境；非canonical调用完全不读取。</param>
    /// <param name="classSlot">类cell的真实自由变量槽；-1表示不存在。</param>
    /// <param name="arg0">当前首参值或原始own cell，不是函数进入时快照。</param>
    /// <param name="argKind">0无位置形参、1普通值、2真实cell、-1未支持frame。</param>
    public static object? CallZeroWithFrame(int site, object? callee, object? environment, long classSlot, object? arg0, long argKind)
        => ReferenceEquals(callee, PyLexicalSuper.Type)
            ? PyLexicalSuper.Construct(environment, classSlot, arg0, argKind)
            : PyPic.CallValue0(site, callee);
}
