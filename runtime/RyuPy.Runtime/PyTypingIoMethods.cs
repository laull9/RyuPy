// IO 的 28 个真实函数及有限反射；签名使用共享 binder，只有已知冷错误做本 API 校准。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static partial class PyTypingIo
{
    private sealed record FunctionInfo(string Qualname, object Defaults);
    private static readonly Dictionary<PyFunction, FunctionInfo> Functions = new(ReferenceEqualityComparer.Instance);

    private static void AddMethod(PyClass cls, string name, string[] names, object?[] defaults, int required) =>
        cls.SetAttr(name, MakePass(cls.Name + "." + name, name, names, defaults, required));

    private static void AddProperty(PyClass cls, string name) =>
        cls.SetAttr(name, new PyProperty(MakePass(cls.Name + "." + name, name, ["self"], [null], 1)));

    private static PyFunction MakePass(string qualname, string name, string[] names, object?[] defaults, int required)
    {
        var kinds = new string[names.Length];
        var flags = new string[names.Length];
        Array.Fill(kinds, "pos");
        for (int index = 0; index < flags.Length; index++) flags[index] = index < required ? "0" : "1";
        PySignature signature = PySignature.Create(qualname, names, kinds, flags);
        var function = new PyFunction(name, signature, (self, args, keywords, values) =>
        {
            try { _ = BindPass(signature, args, keywords, values, self.Defaults); return PyNone.Instance; }
            catch (PyBaseException error) { throw Error(error); }
        }, defaults, false);
        PyOps.FunctionModule(function, "typing");
        object exposedDefaults = required == defaults.Length ? PyNone.Instance : new PyTuple(defaults[required..]);
        Functions.Add(function, new FunctionInfo(qualname, exposedDefaults));
        return function;
    }

    private static object?[] BindPass(PySignature signature, object?[] args, string[] keywords,
        object?[] values, object?[] defaults)
    {
        int maximum = signature.Names.Length;
        if (args.Length > maximum)
        {
            // CP 先报告未知/重复关键字；截断只用于检查，绝不作为成功调用执行正文。
            _ = PyBinder.Bind(signature, args[..maximum], keywords, values, defaults);
            string expected = signature.RequiredCount == maximum ? maximum.ToString()
                : $"from {signature.RequiredCount} to {maximum}";
            string plural = maximum == 1 && signature.RequiredCount == maximum ? "" : "s";
            throw new PyTypeError($"{signature.Name}() takes {expected} positional argument{plural} but {args.Length} were given");
        }
        // 只有确定不存在关键字错误时，才校准多缺参列举；不按异常文本猜错误来源。
        if (args.Length < signature.RequiredCount && ValidKeywords(signature, args.Length, keywords))
        {
            var missing = new List<string>();
            for (int index = args.Length; index < signature.RequiredCount; index++)
                if (Array.IndexOf(keywords, signature.Names[index]) < 0) missing.Add(signature.Names[index]);
            if (missing.Count >= 2)
            {
                string listed = missing.Count == 2 ? $"'{missing[0]}' and '{missing[1]}'"
                    : "'" + string.Join("', '", missing.GetRange(0, missing.Count - 1)) + "', and '" + missing[^1] + "'";
                throw new PyTypeError($"{signature.Name}() missing {missing.Count} required positional arguments: {listed}");
            }
        }
        return PyBinder.Bind(signature, args, keywords, values, defaults);
    }

    private static bool ValidKeywords(PySignature signature, int positional, string[] names)
    {
        for (int index = 0; index < names.Length; index++)
        {
            int slot = Array.IndexOf(signature.Names, names[index]);
            if (slot < positional || slot < 0 || Array.IndexOf(names, names[index]) != index) return false;
        }
        return true;
    }

    internal static bool TryFunctionMetadata(PyFunction function, string name, out object? value)
    {
        value = null;
        if (!Functions.TryGetValue(function, out FunctionInfo? info)) return false;
        switch (name)
        {
            case "__qualname__": value = info.Qualname; return true;
            case "__defaults__": value = info.Defaults; return true;
            case "__kwdefaults__": value = PyNone.Instance; return true;
            case "__class__": value = PyType.Get("function"); return true;
            case "__isabstractmethod__": value = true; return true;
            case "__annotations__": case "__dict__": case "__code__": case "__closure__":
            case "__get__": case "__reduce__": case "__reduce_ex__":
                throw Unsupported("typing IO function reflection is not supported");
            default: return false;
        }
    }

    internal static void GuardFunctionMutation(PyFunction function, string name)
    {
        if (name is "__qualname__" or "__defaults__" or "__kwdefaults__" or "__annotations__" or "__dict__"
            or "__code__" or "__closure__" or "__class__" or "__isabstractmethod__" or "__get__"
            or "__reduce__" or "__reduce_ex__")
            if (Functions.ContainsKey(function)) throw Unsupported("changing typing IO function reflection is not supported");
    }

    internal static object? BoundMetadata(PyBoundMethod method, string name)
    {
        if (method.Target is PyFunction function && Functions.ContainsKey(function))
        {
            if (name == "__self__") return method.Self;
            if (name == "__func__") return function;
            if (name == "__class__") return PyType.Get("method");
        }
        return PyObject.GetAttr(method.Target, name);
    }
}
