// Decimal有限值竖切：真实不可变精确系数/指数，不借double或.NET decimal冒充数值。
// Context、信号/特殊值、算术和扩展格式未准入；通过已有native特殊槽消费，避免侵入数值热路径。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace RyuPy.Runtime;

internal sealed class PyDecimal : PyNativeObject
{
    internal static readonly PyNativeType Type = new("Decimal", "decimal", Invoke,
        static value => value is PyDecimal);
    static PyDecimal() { }
    internal bool Negative { get; }
    internal BigInteger Coefficient { get; }
    internal long Exponent { get; }
    internal string Digits { get; }

    // 仅由已验证文本、真实整数或有界IEEE内部展开构造；负零必须独立于BigInteger保存。
    internal PyDecimal(bool negative, string digits, long exponent)
    {
        Negative = negative; Digits = digits; Exponent = exponent;
        Coefficient = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
    }
    internal static PyDecimal FromInteger(BigInteger value) =>
        new(value.Sign < 0, BigInteger.Abs(value).ToString(CultureInfo.InvariantCulture), 0);

    internal static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["Decimal"] = Type,
        ["getcontext"] = PyNativeFunctions.Create("getcontext", [], [], 0,
            static _ => throw Unsupported("context access")),
        ["setcontext"] = PyNativeFunctions.CreateUnary("setcontext",
            static _ => throw Unsupported("context mutation")),
        ["localcontext"] = PyNativeFunctions.Create("localcontext", ["ctx"], [PyNone.Instance], 0,
            static _ => throw Unsupported("context managers")),
    };

    private static object Invoke(object?[] args, string[] names, object?[] values)
    {
        if (args.Length > 2) throw Own(new PyTypeError($"function takes at most 2 arguments ({args.Length} given)"));
        object? value = args.Length == 0 ? 0L : args[0];
        bool supplied = args.Length != 0;
        if (args.Length > 1) throw Unsupported("explicit context argument");
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == "context") throw Unsupported("explicit context argument");
            if (names[i] != "value") throw Own(new PyTypeError($"'{names[i]}' is an invalid keyword argument for this function"));
            if (supplied) throw Own(new PyTypeError("argument for function given by name ('value') and position (1)"));
            supplied = true; value = values[i];
        }
        if (value is PyDecimal decimalValue) return decimalValue;
        if (PyInt.IsInt(value!)) return FromInteger(PyInt.ToBigInteger(value!));
        if (PyStringLayout.TryGet(value, out string text)) return PyDecimalText.Parse(text);
        if (value is double or float) throw Unsupported("float construction; use an exact finite string");
        if (value is PyTuple) throw Unsupported("tuple construction");
        throw Own(new PyTypeError($"conversion from {PyRepr.TypeName(value)} to Decimal is not supported"));
    }

    public override string TypeName => "Decimal";
    public override string PythonStr() => PyDecimalText.Format(this);
    public override string PythonRepr() => "Decimal('" + PythonStr() + "')";
    internal long Hash() => PyFiniteNumericHash.Decimal(Coefficient, Exponent, Negative);
    internal string Format(string spec) => spec.Length == 0 ? PythonStr()
        : throw Unsupported("nonempty format specifications");

    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "__str__" or "__repr__" or "__hash__" or "__bool__" or "__format__"
            or "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__" => new Method(this, name),
        // 已存在但本竖切未实现的Decimal操作明确拒绝；不能静默继承object身份算术或舍入。
        "__add__" or "__radd__" or "__iadd__" or "__sub__" or "__rsub__" or "__isub__"
            or "__mul__" or "__rmul__" or "__imul__" or "__truediv__" or "__rtruediv__" or "__itruediv__"
            or "__floordiv__" or "__rfloordiv__" or "__ifloordiv__" or "__mod__" or "__rmod__" or "__imod__"
            or "__pow__" or "__rpow__" or "__ipow__" or "__divmod__" or "__rdivmod__"
            or "__neg__" or "__pos__" or "__abs__" or "__int__" or "__float__" or "__round__"
            or "__trunc__" or "__floor__" or "__ceil__" or "__reduce__" or "__reduce_ex__"
            or "quantize" or "normalize" or "sqrt" or "exp" or "ln" or "log10" or "fma"
            or "to_integral" or "to_integral_value" or "to_integral_exact" or "as_tuple" or "as_integer_ratio"
            or "copy_abs" or "copy_negate" or "copy_sign" or "is_finite" or "is_infinite" or "is_nan"
            or "is_snan" or "is_qnan" or "is_signed" or "is_zero" or "is_normal" or "is_subnormal"
            or "adjusted" or "compare" or "compare_signal" or "compare_total" or "compare_total_mag"
            or "max" or "min" or "max_mag" or "min_mag" or "next_minus" or "next_plus" or "next_toward"
            or "number_class" or "radix" or "remainder_near" or "rotate" or "same_quantum" or "scaleb"
            or "shift" or "to_eng_string" or "logical_and" or "logical_or" or "logical_xor" or "logical_invert"
            => new Method(this, name, unsupported: true),
        _ => throw Missing(name),
    };
    public override void SetAttr(string name, object? value) => throw Missing(name);
    public override void DelAttr(string name) => throw Missing(name);

    internal static PyNotImplementedError Unsupported(string operation) =>
        Own(new PyNotImplementedError("decimal finite-value scope does not support " + operation));
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);

    private sealed class Method(PyDecimal owner, string name, bool unsupported = false) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "builtin_function_or_method";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__self__" => owner, "__name__" => name, "__qualname__" => "Decimal." + name,
            _ => throw Missing(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (unsupported) throw Unsupported(name);
            if (names.Length != 0) throw Own(new PyTypeError($"{name}() takes no keyword arguments"));
            bool unary = name is "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__" or "__format__";
            int count = unary ? 1 : 0;
            if (args.Length != count) throw Own(new PyTypeError($"{name}() takes exactly {count} arguments ({args.Length} given)"));
            return name switch
            {
                "__str__" => owner.PythonStr(), "__repr__" => owner.PythonRepr(),
                "__hash__" => owner.Hash(), "__bool__" => !owner.Coefficient.IsZero,
                "__format__" => PyStringLayout.TryGet(args[0], out string spec) ? owner.Format(spec)
                    : throw Own(new PyTypeError("__format__() argument must be str")),
                _ => PyDecimalComparison.Compare(owner, args[0], name),
            };
        }
    }
}
