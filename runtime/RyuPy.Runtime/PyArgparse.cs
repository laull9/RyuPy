// argparse 常用单值参数入口；选项定义、解析、显示分离，不借用宿主命令行解析器。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;
/// <summary>提供受控参数解析和 Namespace 类型的原生模块。</summary>
public static class PyArgparse
{
    /// <summary>创建 argparse 模块公开属性。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["ArgumentParser"] = new PyNativeType("ArgumentParser", "argparse", (args, names, values) =>
            PyOps.CallValue(PyNativeFunctions.Create("ArgumentParser", ["prog", "usage", "description", "epilog", "add_help", "allow_abbrev"],
                [null, null, null, null, true, true], 0, a => new PyArgparseParser(a)), args, names, values),
            value => value is PyArgparseParser),
        ["Namespace"] = new PyNativeType("Namespace", "argparse", (args, names, values) =>
        {
            if (args.Length != 0) throw new PyTypeError("Namespace() takes no positional arguments");
            var result = new PyArgparseNamespace();
            for (int i = 0; i < names.Length; i++) result.SetAttr(names[i], values[i]);
            return result;
        }, value => value is PyArgparseNamespace),
    };
    internal static string? OptionalString(object? value) => value is null or PyNone ? null : value as string
        ?? throw new PyTypeError("expected str or None");
}
/// <summary>解析结果的可变属性集合；不隐式转换为 CLR 字典。</summary>
public sealed class PyArgparseNamespace : PyNativeObject
{
    private readonly PyDict _attributes = new();
    /// <inheritdoc/>
    public override string TypeName => "Namespace";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name == "__dict__" ? _attributes :
        _attributes.TryGet(name, out var value) ? value ?? PyNone.Instance : throw Missing(name);
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value) => _attributes.Set(name, value);
    internal bool Contains(string name) => _attributes.TryGet(name, out _);
}
/// <summary>单个已登记参数的可观察基础元数据。</summary>
public sealed class PyArgparseAction : PyNativeObject
{
    internal string[] Flags = [];
    internal string Dest = "", Kind = "store";
    internal string? Help, Metavar;
    internal object? Default, Convert, Choices;
    internal bool Required, Positional;
    /// <inheritdoc/>
    public override string TypeName => "argparse.Action";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "dest" => Dest, "option_strings" => new PyList(new List<object?>(Flags)),
        "default" => Default ?? PyNone.Instance, "required" => Required, "type" => Convert ?? PyNone.Instance, "choices" => Choices ?? PyNone.Instance,
        "help" => (object?)Help ?? PyNone.Instance, "metavar" => (object?)Metavar ?? PyNone.Instance, "nargs" => Kind == "store" ? PyNone.Instance : (object)0L,
        _ => throw Missing(name),
    };
}
