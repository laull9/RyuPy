using System.Collections.Concurrent;

namespace RyuPy.Runtime;

/// <summary>一等内建函数；同名值共享身份，调用仍使用现有内建参数校验。</summary>
public sealed class PyBuiltinFunction
{
    /// <summary>只读 Python 函数名。</summary>
    public string Name { get; }

    internal PyBuiltinFunction(string name) => Name = name;

    /// <summary>调用已经展开实参的一等内建函数。</summary>
    /// <param name="args">位置参数。</param>
    /// <param name="names">关键字名称。</param>
    /// <param name="values">关键字值。</param>
    public object? Invoke(object?[] args, string[] names, object?[] values) =>
        PyOps.CallBuiltin(Name, args, names, values);

    internal object GetMetadata(string name) => name == "__name__" ? Name
        : throw new PyAttributeError($"'builtin_function_or_method' object has no attribute '{name}'");

    /// <inheritdoc />
    public override string ToString() => $"<built-in function {Name}>";
}

public static partial class PyOps
{
    private static readonly ConcurrentDictionary<string, PyBuiltinFunction> BuiltinFunctions = new();

    /// <summary>读取按名称缓存的一等内建函数对象；不更改类型对象构造路径。</summary>
    /// <param name="name">编译器识别的内建函数名。</param>
    public static object BuiltinFunction(string name) =>
        BuiltinFunctions.GetOrAdd(name, static value => new PyBuiltinFunction(value));
}
