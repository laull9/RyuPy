using System;
using System.Globalization;
using System.IO;

namespace RyuPy.Runtime;

/// <summary>
/// Python 内建函数。编译器按参数的静态类型选择具体方法（见 MIR <c>RuntimeFn</c>）。
/// 方法签名一旦被编译器引用就视为 ABI，修改时必须同步 codegen 的引用表。
/// </summary>
/// <remarks>
/// <c>print(a, b)</c> 被编译为 <c>WriteI64(a); WriteSep(); WriteF64(b); WriteNewline();</c>。
/// 输出统一使用 <c>\n</c>（而非平台相关的 <c>Environment.NewLine</c>），保证与 CPython 逐字节一致。
/// </remarks>
public static class Builtins
{
    private static readonly TextWriter Out = Console.Out;

    /// <summary>写出 <c>int</c>。</summary>
    public static void WriteI64(long value) => Out.Write(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>写出 <c>float</c>，格式与 CPython <c>repr(float)</c> 一致。</summary>
    public static void WriteF64(double value) => Out.Write(PyFormat.FloatRepr(value));

    /// <summary>写出 <c>bool</c>：<c>True</c>/<c>False</c>。</summary>
    public static void WriteBool(bool value) => Out.Write(value ? "True" : "False");

    /// <summary>写出 <c>str</c>。</summary>
    public static void WriteStr(string value) => Out.Write(value);

    /// <summary>
    /// 写出动态值：等价于 <c>print(str(value))</c>。动态路径（M2.1）里
    /// 编译期不知道具体类型，统一走这里。
    /// </summary>
    public static void WriteObject(object? value) => Out.Write(PyRepr.Str(value));

    /// <summary><c>print</c> 参数之间的分隔符（默认 <c>sep=' '</c>）。</summary>
    public static void WriteSep() => Out.Write(' ');

    /// <summary><c>print</c> 的结尾（默认 <c>end='\n'</c>）。</summary>
    public static void WriteNewline() => Out.Write('\n');

    /// <summary><c>str + str</c>。</summary>
    public static string StrConcat(string a, string b) => string.Concat(a, b);
}
