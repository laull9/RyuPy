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

    /// <summary>读取稳定的一等内建对象；map/memoryview保留真实类型身份，其余已登记函数继续使用函数缓存。</summary>
    /// <param name="name">编译器识别的内建函数名。</param>
    public static object BuiltinFunction(string name) => name == "NotImplemented" ? PyNotImplemented.Instance
        : name == "Ellipsis" ? PyEllipsis.Instance
        : name == "map" ? PyMap.Type
        : name == "memoryview" ? PyMemoryView.Type
        : name is "exit" or "quit" ? PySiteQuitters.Get(name)
        : BuiltinFunctions.GetOrAdd(name, static value => new PyBuiltinFunction(value));
}
