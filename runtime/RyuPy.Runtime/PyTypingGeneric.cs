// Generic 的真实根及有限订阅入口；不映射 CLR 泛型，也不占用普通类的特殊布局标记。
// canonical 身份独立于 typing 的可写导出与 Lazy 表，避免注解守卫递归初始化。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTypingGeneric
{
    internal static readonly PyClass Root = CreateRoot();
    private static PyClass CreateRoot()
    {
        var root = new PyClass("Generic") { NativeDefinitionModule = "typing", IsTypingGenericRoot = true };
        root.SetAttr("__module__", "typing");
        return root;
    }

    internal static bool IsRoot(object? value) => value is PyClass { IsTypingGenericRoot: true };
    internal static void GuardExport(Dictionary<string, object?> attributes)
    {
        if (!attributes.TryGetValue("Generic", out object? current) || !ReferenceEquals(current, Root))
            throw Unsupported("typing.Generic rebinding is not supported at this consumer");
    }

    // 与 _type_check 的 Generic 判定同处消费；不能因真实 PyClass 而落入普通类白名单。
    internal static void ValidateTypeArgument(object? value)
    {
        if (IsRoot(value)) throw Error(new PyTypeError("Plain " + Root + " is not valid as type argument"));
        if (value is PyTypingGenericAlias alias)
            throw Error(new PyTypeError(alias.PythonRepr() + " is not valid as type argument"));
    }

    internal static object Subscribe(object? parameter)
    {
        GuardProtocol("__class_getitem__");
        // CP 缓存先 hash 原始输入。未知 hash/eq 不能因最终参数非法而被悄悄跳过。
        RequireKnownInput(parameter);
        object?[] arguments = parameter is PyTuple tuple ? tuple.Items : [parameter];
        if (arguments.Length == 0)
            throw SubscriptionError("Parameter list to Generic[...] cannot be empty");
        foreach (object? argument in arguments)
            if (argument is not PyTypingTypeVar)
                throw SubscriptionError("Parameters to Generic[...] must all be type variables or parameter specification variables.");
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (object? argument in arguments)
            if (!seen.Add(argument!)) throw SubscriptionError("Parameters to Generic[...] must all be unique");
        return PyTypingGenericCache.Intern(parameter is PyTuple, arguments);
    }

    // 非法参数不可能有旧缓存；仍须先消费 Generic 全局，避免改绑后伪造旧错误。
    private static PyTypeError SubscriptionError(string message)
    {
        GuardExport(PyTyping.CreateAttributes());
        return Error(new PyTypeError(message));
    }

    private static void RequireKnownInput(object? value)
    {
        if (value is PyTuple tuple && tuple.GetType() == typeof(PyTuple))
        {
            foreach (object? item in tuple.Items) RequireKnownInput(item);
            return;
        }
        if (value is null or PyNone or PyTypingTypeVar or PyType or PyNativeType or PyClass or string or bool or long or int or BigInteger or double
            or PyList or PyDict or PySet) return;
        throw Unsupported("Generic subscription with unknown hash or equality behavior is not supported");
    }

    internal static object Construct(object?[] args, string[] names)
    {
        GuardProtocol("__new__", "__init__");
        if (args.Length != 0 || names.Length != 0)
            throw Error(new PyTypeError("typing.Generic() takes no arguments"));
        return new PyInstance(Root);
    }

    internal static void GuardProtocol(params string[] names)
    {
        foreach (string name in names)
            if (Root.Attributes.ContainsKey(name))
                throw Unsupported($"changed Generic protocol {name} is not supported");
    }

    internal static bool RejectRuntimeAlias() => throw Error(new PyTypeError(
        "Subscripted generics cannot be used with class and instance checks"));
    internal static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
    internal static PyNotImplementedError Unsupported(string message) => Error(new PyNotImplementedError(message));
}

public sealed partial class PyClass
{
    // 独立根标记只影响该根；未来普通后代仍使用普通 new/init 和实例字段布局。
    internal bool IsTypingGenericRoot { get; init; }
}
