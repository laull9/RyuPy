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
/// 输出逻辑换行 <c>\n</c>，由标准文本流执行宿主平台翻译；用户替换的输出流仍按其 write 协议接收原文本。
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
public static partial class Builtins
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

    /// <summary>默认标准输出包装复用此缓冲器，避免第二套缓冲造成顺序颠倒。</summary>
    internal static TextWriter DefaultOutput => Out;

    /// <summary>把缓冲区内容写到标准输出。进程退出前与写错误报告前调用。</summary>
    public static void Flush()
    {
        if (PyTraceProduct.Enabled)
            PyIO.Guard(() => { Out.Flush(); return PyNone.Instance; });
        else Out.Flush();
    }

    /// <summary>写出 <c>int</c>。</summary>
    public static void WriteI64(long value) =>
        PySystem.WriteStdout(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>写出 <c>float</c>，格式与 CPython <c>repr(float)</c> 一致。</summary>
    public static void WriteF64(double value) => PySystem.WriteStdout(PyFormat.FloatRepr(value));

    /// <summary>写出 <c>bool</c>：<c>True</c>/<c>False</c>。</summary>
    public static void WriteBool(bool value) => PySystem.WriteStdout(value ? "True" : "False");

    /// <summary>写出 <c>str</c>。</summary>
    public static void WriteStr(string value) => PySystem.WriteStdout(value);

    /// <summary>
    /// 写出动态值：等价于 <c>print(str(value))</c>。动态路径（M2.1）里
    /// 编译期不知道具体类型，统一走这里。
    /// </summary>
    public static void WriteObject(object? value) => PySystem.WriteStdout(PyRepr.Str(value));

    /// <summary><c>print</c> 参数之间的分隔符（默认 <c>sep=' '</c>）。</summary>
    public static void WriteSep() => PySystem.WriteStdout(" ");

    /// <summary><c>print</c> 的结尾（默认 <c>end='\n'</c>）。</summary>
    public static void WriteNewline() => PySystem.WriteStdout("\n");

    /// <summary><c>str + str</c>。</summary>
    public static string StrConcat(string a, string b) => string.Concat(a, b);}
