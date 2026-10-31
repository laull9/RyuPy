using System;
using System.IO;

namespace RyuPy.Runtime;

/// <summary>
/// 未捕获异常的顶层报告与 traceback 文本。
/// </summary>
/// <remarks>
/// CPython 在进程退出时把 traceback 写到 stderr，最后一行是 <c>Type: message</c>，退出码 1。
/// RyuPy 的入口方法是一个 CIL 包装器：<c>try { __main(); return 0; } catch (PyBaseException) { ... return 1; }</c>，
/// 捕获后调用这里格式化输出。
///
/// 当前没有逐帧行号（MIR 尚未携带源码位置），所以 traceback 只有头部与最内层异常；
/// M2.2 的后续竖切会给 MIR 加行号，把 <c>File "x.py", line N, in fn</c> 补全。异常链
/// （<c>raise ... from ...</c> 的 <c>Cause</c>）已经按 CPython 的措辞完整输出。
/// </remarks>
public static class PyTraceback
{
    private const string Header = "Traceback (most recent call last):";
    private const string CauseSeparator =
        "\nThe above exception was the direct cause of the following exception:\n";
    private const string ContextSeparator =
        "\nDuring handling of the above exception, another exception occurred:\n";

    /// <summary>把异常（含显式原因链）渲染成 CPython 风格的多段文本。</summary>
    public static string Format(PyBaseException exception)
    {
        var builder = new System.Text.StringBuilder();
        if (exception.Cause is not null)
        {
            builder.Append(Format(exception.Cause));
            builder.Append(CauseSeparator);
            AppendSingle(builder, exception);
        }
        else if (exception is PyException && exception.InnerException is PyBaseException context)
        {
            // .NET 的 InnerException 由 raise ... from ... 设置；没有显式原因时，
            // 这里不再额外输出 context，避免和 Cause 重复。
            builder.Append(Format(context));
            builder.Append(ContextSeparator);
            AppendSingle(builder, exception);
        }
        else
        {
            AppendSingle(builder, exception);
        }
        return builder.ToString();
    }

    private static void AppendSingle(System.Text.StringBuilder builder, PyBaseException exception)
    {
        builder.Append(Header).Append('\n');
        builder.Append("  File \"<ryupy>\", in <module>\n");
        builder.Append(exception.Render()).Append('\n');
    }

    /// <summary>把未捕获异常写到 stderr（格式与 CPython 一致）。</summary>
    public static void Report(PyBaseException exception)
    {
        TextWriter error = Console.Error;
        error.Write(Format(exception));
        error.Flush();
    }
}
