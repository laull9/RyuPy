// textwrap旧类与快捷函数保共享字典/动态工厂；新增dedent四字段只归真实导入owner。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>textwrap模块：普通可继承TextWrapper与动态构造的wrap/fill快捷函数。</summary>
public static class PyTextWrap
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>旧TextWrapper/wrap/fill的共享字典；新dedent字段归真实导入owner，不从此宿主入口公开。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;

    // 旧三名继续共享原字典；仅四个新增名字归当前owner，读写绝不双写或shadow旧工厂。
    internal static void Register(PyTextDedentCompile witness)
    {
        Dictionary<string, object?> attributes = CreateAttributes();
        PyModules.RegisterNativeLazy("textwrap", "", "", () => PyTextDedent.Initialize(witness),
            name => !PyTextDedent.Owns(name) && attributes.TryGetValue(name, out object? value) ? value
                : throw new PyAttributeError($"module 'textwrap' has no attribute '{name}'"),
            (owner, name, value) =>
            {
                if (PyTextDedent.Owns(name)) owner.DynamicAttributes[name] = value;
                else attributes[name] = value;
            });
    }

    private static Dictionary<string, object?> Build()
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        attributes["TextWrapper"] = PyTextWrapper.Build(attributes);
        foreach (string name in new[] { "wrap", "fill" })
        {
            string method = name;
            attributes[name] = PyTextWrapper.Function(name, ["text", "width", "kwargs"],
                [null, 70L, null], 1, args => Invoke(attributes, method, args), ["pos", "pos", "varkw"]);
        }
        return attributes;
    }

    private static object? Invoke(Dictionary<string, object?> attributes, string method, object?[] args)
    {
        // 未知关键字交给当前构造器，允许用户工厂接收自己的额外参数。
        var extra = (PyDict)args[2]!;
        var names = new string[extra.Count + 1];
        var values = new object?[extra.Count + 1];
        names[0] = "width"; values[0] = args[1];
        int index = 1;
        foreach (var (key, value) in extra.Entries())
        {
            names[index] = (string)key!; values[index++] = value;
        }
        if (!attributes.TryGetValue("TextWrapper", out object? constructor))
            throw new PyNameError("name 'TextWrapper' is not defined");
        object? instance = PyOps.CallValue(constructor, [], names, values);
        return PyTextWrapper.Call(instance, method, args[0]);
    }
}
