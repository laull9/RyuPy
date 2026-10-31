using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// Python 内建函数。编译器按参数的静态类型选择具体方法（见 MIR <c>RuntimeFn</c>）。
/// 方法签名一旦被编译器引用就视为 ABI，修改时必须同步 codegen 的引用表。
/// </summary>
/// <remarks>
/// <c>print(a, b)</c> 被编译为 <c>WriteI64(a); WriteSep(); WriteF64(b); WriteNewline();</c>。
/// 输出统一使用 <c>\n</c>（而非平台相关的 <c>Environment.NewLine</c>），保证与 CPython 逐字节一致。
///
/// # 为什么要自己缓冲
///
/// <c>Console.Out</c> 是同步的、逐次写底层流的，且每次写都要经过编码器。循环里
/// <c>print(i)</c> 每秒几百万次时，系统调用会占掉绝大部分时间。这里套一层
/// <see cref="StreamWriter"/> 并关闭自动刷新：格式化仍逐次进行，但真正写入管道
/// 按 64 KiB 批量发生。
///
/// 代价是**必须显式刷新**：入口包装器在退出前调用 <see cref="Flush"/>，
/// 未捕获异常路径也会先刷新 stdout 再写 stderr（否则缓冲区里的内容会丢失或乱序）。
/// </remarks>
public static class Builtins
{
    private const int OutputBufferSize = 64 * 1024;

    /// <summary>标准输出；不自动刷新，由入口包装器统一 <see cref="Flush"/>。</summary>
    private static readonly StreamWriter Out = new(
        Console.OpenStandardOutput(),
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        OutputBufferSize
    )
    {
        AutoFlush = false,
    };

    /// <summary>把缓冲区内容写到标准输出。进程退出前与写错误报告前调用。</summary>
    public static void Flush() => Out.Flush();

    /// <summary>写出 <c>int</c>。</summary>
    public static void WriteI64(long value) =>
        Out.Write(value.ToString(CultureInfo.InvariantCulture));

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
    public static string StrConcat(string a, string b) => string.Concat(a, b);}
