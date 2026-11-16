// format独立句法与接收类型校验；先判非法，再拒绝合法未实现，不复用旧f-string宽松parser。
using System;

namespace RyuPy.Runtime;

internal static partial class PyBuiltinFormat
{
    private struct Spec
    {
        public Spec() { }
        internal string Fill = " ";
        internal char Align, Sign, Group;
        internal bool ExplicitFill, Zero, Alternate, CoerceZero;
        internal long Width, Precision = -1;
        internal string Type = "";
    }

    private static Spec Parse(string text, string typeName)
    {
        var spec = new Spec(); int i = 0;
        // 一个Python码点可以是代理对，也可以是孤立代理；不经Rune替换损坏原文本。
        int first = Unit(text, 0);
        if (first > 0 && first < text.Length && Align(text[first]))
        { spec.Fill = text[..first]; spec.ExplicitFill = true; spec.Align = text[first]; i = first + 1; }
        else if (first == 1 && Align(text[0])) { spec.Align = text[0]; i = 1; }
        if (i < text.Length && text[i] is '+' or '-' or ' ') spec.Sign = text[i++];
        if (i < text.Length && text[i] == 'z') { spec.CoerceZero = true; i++; }
        if (i < text.Length && text[i] == '#') { spec.Alternate = true; i++; }
        if (i < text.Length && text[i] == '0') { spec.Zero = true; i++; }
        spec.Width = Decimal(text, ref i, out _);
        if (i < text.Length && text[i] is ',' or '_') spec.Group = text[i++];
        if (i < text.Length && text[i] == '.')
        {
            i++; spec.Precision = Decimal(text, ref i, out bool found);
            if (!found) throw Own(new PyValueError("Format specifier missing precision"));
        }
        if (i < text.Length)
        {
            int size = Unit(text, i); spec.Type = text.Substring(i, size); i += size;
        }
        if (i != text.Length)
            throw Own(new PyValueError($"Invalid format specifier '{text}' for object of type '{typeName}'"));
        return spec;
    }

    private static long Decimal(string text, ref int i, out bool found)
    {
        long number = 0; found = false;
        while (i < text.Length)
        {
            int scalar = Unit(text, i) == 2 ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
            int digit = DecimalDigit(scalar);
            if (digit < 0) break;
            found = true;
            if (number > (long.MaxValue - digit) / 10)
                throw Own(new PyValueError("Too many decimal digits in format string"));
            number = number * 10 + digit; i += Unit(text, i);
        }
        return number;
    }

    private static void Validate(Spec spec, string typeName)
    {
        bool text = typeName == "str", floating = typeName == "float";
        string type = spec.Type;
        bool floatPresentation = type.Length == 1 && "eEfFgG%".Contains(type[0]);
        bool allowed = type.Length == 0 || (type.Length == 1 &&
            (text ? type == "s" : floating ? "eEfFgGn%".Contains(type[0]) : "bcdoxXeEfFgGn%".Contains(type[0])));
        if (!allowed) throw Own(new PyValueError($"Unknown format code '{type}' for object of type '{typeName}'"));
        if (text)
        {
            if (spec.Sign != '\0') throw Own(new PyValueError("Sign not allowed in string format specifier"));
            if (spec.CoerceZero) throw Own(new PyValueError("Negative zero coercion (z) not allowed in string format specifier"));
            if (spec.Alternate) throw Own(new PyValueError("Alternate form (#) not allowed in string format specifier"));
            if (spec.Align == '=') throw Own(new PyValueError("'=' alignment not allowed in string format specifier"));
        }
        else if (!floating && !floatPresentation)
        {
            if (spec.Precision >= 0) throw Own(new PyValueError("Precision not allowed in integer format specifier"));
            if (spec.CoerceZero) throw Own(new PyValueError("Negative zero coercion (z) not allowed in integer format specifier"));
            if (type == "c" && spec.Sign != '\0') throw Own(new PyValueError("Sign not allowed with integer format specifier 'c'"));
            if (type == "c" && spec.Alternate) throw Own(new PyValueError("Alternate form (#) not allowed with integer format specifier 'c'"));
        }
        if (spec.Group != '\0')
        {
            string effective = type.Length == 0 ? (text ? "s" : floating ? "g" : "d") : type;
            if (text || effective is "n" or "c" || (spec.Group == ',' && effective is "b" or "o" or "x" or "X"))
                throw Own(new PyValueError($"Cannot specify '{spec.Group}' with '{effective}'."));
        }
    }

    // .NET10 String.MaxLength真实容量；全结果UTF-16检查，不设任意业务上限。
    // https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/String.cs
    private const int ManagedStringMaxLength = 0x3FFFFFDF;
    private static int Width(Spec spec)
    {
        if (spec.Width > ManagedStringMaxLength)
            throw Own(new PyNotImplementedError("format: result exceeds managed string capacity"));
        return (int)spec.Width;
    }
    private static void Capacity(long length)
    {
        if (length > ManagedStringMaxLength)
            throw Own(new PyNotImplementedError("format: result exceeds managed string capacity"));
    }
    private static bool Align(char c) => c is '<' or '>' or '^' or '=';
    private static int Unit(string text, int i) => i >= text.Length ? 0
        : char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
}
