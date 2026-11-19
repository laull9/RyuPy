// overload三项API的有限真实协议：参数先绑定，逐阶段读取helper，registry与导出槽分别保留身份。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static partial class PyTypingOverload
{
    private const string DummyMessage = "You should not call an overloaded function. A series of @overload-decorated functions outside a stub module should always be followed by an implementation that is not @overload-ed.";
    private static Dictionary<string, object?> Attributes => PyTyping.CreateAttributes();

    internal static void Install(Dictionary<string, object?> attributes)
    {
        attributes["_overload_registry"] = new PyDefaultDict(new PyPartial(DefaultDictRoot, [PyType.Get("dict")], [], []));
        attributes["overload"] = Function("overload", ["func"], ["pos"],
            static (_, args, names, values) => Overload(BindOne("overload", args, names, values)));
        attributes["get_overloads"] = Function("get_overloads", ["func"], ["pos"],
            static (_, args, names, values) => GetOverloads(BindOne("get_overloads", args, names, values)));
        attributes["clear_overloads"] = Function("clear_overloads", [], [], static (_, args, names, _) =>
        {
            BindZero(args, names);
            ClearRegistry(Global("_overload_registry"));
            return PyNone.Instance;
        });
        attributes["_overload_dummy"] = Function("_overload_dummy", ["args", "kwds"], ["varpos", "varkw"], static (_, _, _, _) =>
        {
            GuardHelper("NotImplementedError", PyType.Get("NotImplementedError"));
            throw new PyNotImplementedError(DummyMessage);
        });
    }

    private static PyFunction Function(string name, string[] names, string[] kinds, PyCall body)
    {
        var flags = new string[names.Length]; Array.Fill(flags, "0");
        var function = new PyFunction(name, PySignature.Create(name, names, kinds, flags), body, new object?[names.Length], false);
        function.CaptureModule("typing");
        // 已知原生API只登记真实名字，不为它们编造Python code首行。
        function.SetMetadata("__qualname__", name);
        return function;
    }

    // 只校准本组零/一参API；未知/重复关键字先于多余位置参数，不改通用binder热路。
    private static object? BindOne(string name, object?[] args, string[] names, object?[] values)
    {
        bool assigned = args.Length != 0;
        object? value = assigned ? args[0] : null;
        for (int index = 0; index < names.Length; index++)
        {
            if (names[index] != "func") throw new PyTypeError($"{name}() got an unexpected keyword argument '{names[index]}'");
            if (assigned) throw new PyTypeError($"{name}() got multiple values for argument 'func'");
            value = values[index]; assigned = true;
        }
        if (args.Length > 1) throw new PyTypeError($"{name}() takes 1 positional argument but {args.Length} were given");
        if (!assigned) throw new PyTypeError($"{name}() missing 1 required positional argument: 'func'");
        return value;
    }

    private static void BindZero(object?[] args, string[] names)
    {
        if (names.Length != 0) throw new PyTypeError($"clear_overloads() got an unexpected keyword argument '{names[0]}'");
        if (args.Length != 0) throw new PyTypeError($"clear_overloads() takes 0 positional arguments but {args.Length} {(args.Length == 1 ? "was" : "were")} given");
    }

    private static object? Overload(object? original)
    {
        object? function = Unwrap(original);
        try
        {
            // 顺序与Python下标赋值相同：先当前registry，再module桶，再qualname桶，最后code。
            object? registry = Global("_overload_registry");
            object? module = Item(registry, Attribute(function, "__module__"));
            object? lines = Item(module, Attribute(function, "__qualname__"));
            long line = function is PyFunction compiled ? compiled.DefinitionFirstLine
                : throw Unsupported("code source for this object");
            SetItem(lines, line, original);
        }
        catch (PyBaseException error)
        {
            // except类型只在真的有异常时求值；非AttributeError也必须经过这一步再传播。
            GuardHelper("AttributeError", PyType.Get("AttributeError"));
            if (error is not PyAttributeError) throw;
        }
        return Global("_overload_dummy");
    }

    private static PyList GetOverloads(object? original)
    {
        object? function = Unwrap(original);
        // 首次membership先读属性再读registry；miss直接造新list，不消费全局list helper。
        object? moduleKey = Attribute(function, "__module__");
        if (!Contains(Global("_overload_registry"), moduleKey)) return new PyList();
        object? registry = Global("_overload_registry");
        object? module = Item(registry, Attribute(function, "__module__"));
        if (!Contains(module, Attribute(function, "__qualname__"))) return new PyList();
        GuardHelper("list", PyType.Get("list"));
        PyDict lines = Mapping(Item(module, Attribute(function, "__qualname__")));
        var result = new PyList();
        foreach (var (_, value) in lines.Entries()) result.Items.Add(value);
        return result;
    }

    private static object? Unwrap(object? value)
    {
        GuardHelper("getattr", PyOps.BuiltinFunction("getattr"));
        // 真实method/wrapper的__func__不能从底层function的普通字典取；只解这一层。
        if (value is PyBoundMethod or PyClassMethod or PyStaticMethod) return PyObject.GetAttr(value, "__func__");
        if (value is PyFunction function)
        {
            try { return function.GetMetadata("__func__"); }
            catch (PyAttributeError) { return function; }
        }
        if (value is PyBuiltinFunction || IsKnownMissing(value)) return value;
        throw Unsupported("custom __func__ attribute lookup");
    }

    private static object? Attribute(object? value, string name)
    {
        if (value is PyFunction function) return function.GetMetadata(name);
        // 普通function的__func__可指向bound method；后续属性走真实代理，不能再次unwrap。
        if (value is PyBoundMethod method) return Attribute(method.Target, name);
        if (IsKnownMissing(value))
            throw new PyAttributeError($"'{PyRepr.TypeName(value)}' object has no attribute '{name}'");
        throw Unsupported("native or custom " + name + " metadata");
    }

    private static bool IsKnownMissing(object? value) => value is null or PyNone or bool or int or long
        or BigInteger or double or string or PyList or PyDict or PySet or PyFrozenSet or PyBytes or PyByteArray
        || value is PyTuple tuple && tuple.GetType() == typeof(PyTuple);

    private static object? Global(string name) => Attributes.TryGetValue(name, out object? value) ? value
        : throw new PyNameError($"name '{name}' is not defined");

    private static void GuardHelper(string name, object expected)
    {
        if (Attributes.TryGetValue(name, out object? value) && !ReferenceEquals(value, expected))
            throw Unsupported("changed typing helper " + name);
    }

    private static PyNotImplementedError Unsupported(string subject) =>
        new("typing overload " + subject + " is not supported");
}
