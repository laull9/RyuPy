// 参数kind保留五个稳定枚举成员；通过原生数字协议与int键相等和同哈希。
using System;
using System.Numerics;

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
        long number = value switch { PyInspectKind kind => kind.Value, long n => n, int n => n, bool b => b ? 1 : 0, BigInteger n when n >= 0 && n <= 4 => (long)n, double n when n >= 0 && n <= 4 && n == Math.Truncate(n) => (long)n, _ => -1 };
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
        "__int__" or "__index__" or "__hash__" or "__trunc__" or "__floor__" or "__ceil__" => Unary(name, () => (long)Value),
        "__bool__" => Unary(name, () => Value != 0), "__float__" => Unary(name, () => (double)Value),
        "__neg__" => Unary(name, () => -(long)Value), "__pos__" => Unary(name, () => (long)Value),
        "__invert__" => Unary(name, () => ~(long)Value),
        "__add__" or "__radd__" or "__sub__" or "__rsub__" or "__mul__" or "__rmul__"
            or "__truediv__" or "__rtruediv__" or "__floordiv__" or "__rfloordiv__"
            or "__mod__" or "__rmod__" or "__pow__" or "__rpow__" or "__and__" or "__rand__"
            or "__or__" or "__ror__" or "__xor__" or "__rxor__" or "__lshift__" or "__rlshift__"
            or "__rshift__" or "__rrshift__" => Binary(name),
        "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__" => PyInspectModule.Method(name, (a, n, v) =>
        {
            var x = PyInspectModule.Arguments(name, ["other"], ["posonly"], [null], 1, a, n, v)[0];
            if (x is not (long or int or bool or double or BigInteger or PyInspectKind)) return PyNotImplemented.Instance;
            object other = x is PyInspectKind k ? (long)k.Value : x;
            int operation = name switch { "__eq__" => PyOps.Eq, "__ne__" => PyOps.Ne, "__lt__" => PyOps.Lt,
                "__le__" => PyOps.Le, "__gt__" => PyOps.Gt, _ => PyOps.Ge };
            return PyOps.Compare(operation, (long)Value, other);
        }),
        _ => PyObject.GetAttr((long)Value, name),
    };
    private object Binary(string name) => PyInspectModule.Method(name, (a, n, v) =>
    {
        object? other = PyInspectModule.Arguments(name, ["other"], ["posonly"], [null], 1, a, n, v)[0];
        if (other is PyInspectKind kind) other = (long)kind.Value;
        if (other is not (long or int or bool or double or BigInteger or string)) return PyNotImplemented.Instance;
        bool reverse = name.StartsWith("__r", StringComparison.Ordinal) && name != "__rshift__";
        string operation = name[(reverse ? 3 : 2)..^2];
        int code = operation switch { "add" => PyOps.Add, "sub" => PyOps.Sub, "mul" => PyOps.Mul,
            "truediv" => PyOps.TrueDiv, "floordiv" => PyOps.FloorDiv, "mod" => PyOps.Mod, "pow" => PyOps.Pow,
            "and" => PyOps.BitAnd, "or" => PyOps.BitOr, "xor" => PyOps.BitXor, "lshift" => PyOps.LShift, _ => PyOps.RShift };
        return reverse ? PyOps.Binary(code, other, (long)Value) : PyOps.Binary(code, (long)Value, other);
    });
    private static object Unary(string name, Func<object?> f) => PyInspectModule.Method(name, (a, n, v) =>
    { PyInspectModule.Arguments(name, [], [], [], 0, a, n, v); return f(); });
    /// <inheritdoc/>
    public override string PythonStr() => Names[Value];
    /// <inheritdoc/>
    public override string PythonRepr() => $"<_ParameterKind.{Names[Value]}: {Value}>";
}
