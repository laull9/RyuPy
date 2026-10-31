using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 动态值的 <c>str()</c> / <c>repr()</c> 与类型名。
/// </summary>
/// <remarks>
/// M2.1 的动态值表示是「装箱的 CLR 值」：Python <c>int</c> 是 <see cref="long"/> 或
/// <see cref="BigInteger"/>，<c>float</c> 是 <see cref="double"/>，<c>bool</c> 是
/// <see cref="bool"/>，<c>str</c> 是 <see cref="string"/>，<c>None</c> 是
/// <see cref="PyNone"/> 单例。类型判定因此是 CLR 类型判定，取反例时要先判断
/// <c>bool</c> 再判断数值——<c>bool</c> 在 CLR 里不继承 <c>long</c>，但 Python 里
/// <c>True == 1</c> 成立，这一差别只在比较与算术里显式处理。
/// </remarks>
public static class PyRepr
{
    /// <summary>Python 类型名，用于错误消息（<c>'int' and 'str'</c>）。</summary>
    public static string TypeName(object? value) => value switch
    {
        null => "NoneType",
        PyNone => "NoneType",
        bool => "bool",
        long or int or BigInteger => "int",
        double or float => "float",
        string => "str",
        _ => value.GetType().Name,
    };

    /// <summary><c>str(value)</c>。</summary>
    public static string Str(object? value) => value switch
    {
        null => "None",
        PyNone => "None",
        bool b => b ? "True" : "False",
        long l => l.ToString(CultureInfo.InvariantCulture),
        BigInteger big => big.ToString(CultureInfo.InvariantCulture),
        double d => PyFormat.FloatRepr(d),
        string s => s,
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>
    /// <c>repr(value)</c>。字符串用单引号、转义反斜杠与控制字符；非 ASCII 字符保持原样
    /// （与 CPython 一致，<c>repr</c> 不把可打印 Unicode 转成 <c>\uXXXX</c>）。
    /// </summary>
    public static string Repr(object? value) => value switch
    {
        string s => Quote(s),
        _ => Str(value),
    };

    /// <summary>字符串的 Python <c>repr</c>：优先单引号，内部单引号且无单引号时切换双引号。</summary>
    public static string Quote(string text)
    {
        bool hasSingle = text.Contains('\'');
        bool hasDouble = text.Contains('"');
        char quote = hasSingle && !hasDouble ? '"' : '\'';
        var builder = new StringBuilder(text.Length + 2);
        builder.Append(quote);
        foreach (char ch in text)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (ch == quote)
                    {
                        builder.Append('\\');
                    }
                    builder.Append(ch);
                    break;
            }
        }
        builder.Append(quote);
        return builder.ToString();
    }
}

/// <summary>
/// Python <c>None</c> 的单例。用独立类型而不是 <c>null</c>：<c>null</c> 在 CIL 里表示
/// 「未初始化引用」，会让 <c>None</c> 与「变量尚未赋值」混淆，也无法参与 <c>repr</c>
/// 与容器元素。
/// </summary>
public sealed class PyNone
{
    private PyNone() { }

    /// <summary>唯一的 <c>None</c> 实例。</summary>
    public static readonly PyNone Instance = new();

    /// <summary>与 <c>str(None)</c> 一致。</summary>
    public override string ToString() => "None";
}
