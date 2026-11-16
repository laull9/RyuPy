// 参数kind保留五个稳定枚举成员；通过原生数字协议与int键相等和同哈希。
using System;

namespace RyuPy.Runtime;

/// <summary>inspect._ParameterKind的稳定值对象。</summary>
public sealed class PyInspectKind : PyNativeObject
{
    /// <summary>显式int祖先用于isinstance，无需改变普通整数的热路径。</summary>
    public static readonly PyNativeType Type = new("_ParameterKind", "inspect", Construct, x => x is PyInspectKind, "int");
    private static readonly string[] Names = ["POSITIONAL_ONLY", "POSITIONAL_OR_KEYWORD", "VAR_POSITIONAL", "KEYWORD_ONLY", "VAR_KEYWORD"];
    private static readonly string[] Descriptions = ["positional-only", "positional or keyword", "variadic positional", "keyword-only", "variadic keyword"];
    /// <summary>五个成员。</summary>
    public static readonly PyInspectKind[] Values = [new(0), new(1), new(2), new(3), new(4)];
    /// <summary>稳定整数判别值。</summary>
    public int Value { get; }
    private PyInspectKind(int value) => Value = value;
    static PyInspectKind()
    { PyInspectModule.RegisterIdentity(Type, 14); foreach (var kind in Values)
        { Type.SetAttr(Names[kind.Value], kind); PyInspectModule.RegisterIdentity(kind, kind.Value + 2); } }
    /// <summary>只接受五种合法kind。</summary>
    public static PyInspectKind Of(object? value)
    {
        long number = value switch { PyInspectKind kind => kind.Value, long n => n, int n => n, bool b => b ? 1 : 0, _ => -1 };
        return number >= 0 && number < 5 ? Values[number] : throw new PyValueError($"value {PyRepr.Repr(value)} is not a valid parameter kind");
    }
    private static object Construct(object?[] a, string[] n, object?[] v) => Of(PyInspectModule.Arguments("_ParameterKind", ["value"], ["pos"], [null], 1, a, n, v)[0]);
    /// <inheritdoc/>
    public override string TypeName => "_ParameterKind";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type, "name" or "_name_" => Names[Value], "value" or "_value_" => (long)Value,
        "description" => Descriptions[Value],
        "__int__" or "__index__" or "__hash__" => Unary(name, () => (long)Value),
        "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__" => PyInspectModule.Method(name, (a, n, v) =>
        {
            var x = PyInspectModule.Arguments(name, ["other"], ["posonly"], [null], 1, a, n, v)[0];
            if (x is not (long or int or bool or PyInspectKind)) return name == "__ne__";
            long other = OfNumber(x);
            return name switch { "__eq__" => Value == other, "__ne__" => Value != other, "__lt__" => Value < other,
                "__le__" => Value <= other, "__gt__" => Value > other, _ => Value >= other };
        }),
        _ => throw Missing(name),
    };
    private static long OfNumber(object? x) => x switch { PyInspectKind k => k.Value, long n => n, int n => n, bool b => b ? 1 : 0, _ => 0 };
    private static object Unary(string name, Func<object?> f) => PyInspectModule.Method(name, (a, n, v) =>
    { PyInspectModule.Arguments(name, [], [], [], 0, a, n, v); return f(); });
    /// <inheritdoc/>
    public override string PythonStr() => Names[Value];
    /// <inheritdoc/>
    public override string PythonRepr() => $"<_ParameterKind.{Names[Value]}: {Value}>";
}
