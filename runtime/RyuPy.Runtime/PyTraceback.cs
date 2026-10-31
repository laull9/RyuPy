using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 运行时栈帧记录、未捕获异常报告与 traceback 文本。
/// </summary>
/// <remarks>
/// # 栈帧是怎么来的
///
/// RyuPy 没有解释器循环，栈帧只能靠编译器插入的钩子维护：
/// <list type="bullet">
/// <item>函数入口调用 <see cref="PushFrame"/> 取得自己的深度；</item>
/// <item>每条语句前调用 <see cref="SetLine"/> 更新当前行号（写一个静态字段，成本可忽略）；</item>
/// <item>正常返回前调用 <see cref="PopTo"/> 弹栈；异常被捕获时也调用 <see cref="PopTo"/>
/// 把被展开的帧丢掉；</item>
/// <item>抛出异常前调用 <see cref="Attach"/> 把当前栈快照挂到异常对象上。</item>
/// </list>
///
/// 快照取的是每个帧**当前所在的行**：对最内层就是出错行，对调用者就是调用点所在行，
/// 与 CPython 的 traceback 一致。栈本身是 <see cref="List{T}"/>，只在 push/pop 时变化。
///
/// # 未捕获异常
///
/// 入口包装器的 <c>catch (PyBaseException)</c> 调用 <see cref="Report"/>：把 traceback 写 stderr、
/// 退出码 1，与 CPython 相同。<c>SystemExit</c> 单独处理：<c>sys.exit(3)</c> 应当返回 3
/// 而不是打印 traceback。
/// </remarks>
public static class PyTraceback
{
    private const string Header = "Traceback (most recent call last):";
    private const string CauseSeparator =
        "\nThe above exception was the direct cause of the following exception:\n";
    private const string ContextSeparator =
        "\nDuring handling of the above exception, another exception occurred:\n";

    private sealed class FrameSlot
    {
        public string Function = "";
        public int Line;
    }

    private static readonly List<FrameSlot> Stack = [];

    /// <summary>源文件名；入口包装器启动时设置一次，供所有帧共用。</summary>
    private static string _file = "<ryupy>";

    /// <summary>设置源文件名（traceback 里 <c>File "..."</c> 的内容）。</summary>
    /// <param name="file">文件名。</param>
    public static void SetFile(string file) => _file = file;

    /// <summary>
    /// Python 递归深度上限（默认 1000，可用环境变量 <c>RYUPY_RECURSION_LIMIT</c> 覆盖）。
    /// </summary>
    /// <remarks>
    /// 用「帧数」而不是「CLR 栈字节数」做守卫：CLR 的栈溢出无法被捕获，进程会直接终止；
    /// 而 Python 承诺抛出可捕获的 <c>RecursionError</c>。默认值比 CPython 保守一些，
    /// 因为一层 Python 调用在 CLR 上会占好几层栈帧。
    /// </remarks>
    private static readonly int RecursionLimit = ReadRecursionLimit();

    private static int ReadRecursionLimit()
    {
        string? raw = Environment.GetEnvironmentVariable("RYUPY_RECURSION_LIMIT");
        if (raw is not null && int.TryParse(raw, out int parsed) && parsed > 0)
        {
            return parsed;
        }
        return 1000;
    }

    /// <summary>压入一帧，返回该帧的深度（1 起）。</summary>
    /// <param name="function">函数名；模块顶层传 <c>&lt;module&gt;</c>。</param>
    public static long PushFrame(string function)
    {
        if (Stack.Count >= RecursionLimit)
        {
            throw new PyRecursionError("maximum recursion depth exceeded");
        }
        Stack.Add(new FrameSlot { Function = function, Line = 0 });
        return Stack.Count;
    }

    /// <summary>更新最内层帧的当前行号。</summary>
    /// <param name="line">行号（1 起）。</param>
    public static void SetLine(int line)
    {
        if (Stack.Count > 0)
        {
            Stack[^1].Line = line;
        }
    }

    /// <summary>弹栈到指定深度：保留前 <paramref name="depth"/> 帧。</summary>
    /// <param name="depth"><see cref="PushFrame"/> 返回的深度。</param>
    public static void PopTo(long depth)
    {
        if (depth >= 1 && depth <= Stack.Count)
        {
            Stack.RemoveRange((int)depth - 1, Stack.Count - (int)(depth - 1));
        }
    }

    /// <summary>把当前栈快照挂到异常对象上（已经有快照时不覆盖）。</summary>
    /// <param name="exception">即将抛出的异常。</param>
    public static void Attach(object exception)
    {
        if (exception is not PyBaseException ex || ex.Frames.Length > 0)
        {
            return;
        }
        var frames = new PyStackFrame[Stack.Count];
        // 最内层在前：倒序保存，格式化时再反过来输出
        for (int i = 0; i < Stack.Count; i++)
        {
            FrameSlot slot = Stack[Stack.Count - 1 - i];
            frames[i] = new PyStackFrame(_file, slot.Function, slot.Line);
        }
        ex.Frames = frames;
    }

    /// <summary>把异常（含原因链与上下文链）渲染成 CPython 风格的多段文本。</summary>
    /// <param name="exception">异常对象。</param>
    public static string Format(PyBaseException exception)
    {
        var builder = new StringBuilder();
        AppendChain(builder, exception, 0);
        return builder.ToString();
    }

    private static void AppendChain(StringBuilder builder, PyBaseException exception, int depth)
    {
        // 异常链可能是环（异常对象允许被重新抛出并且互相挂接），限制递归深度
        if (depth > 16)
        {
            AppendSingle(builder, exception);
            return;
        }

        if (exception.Cause is not null)
        {
            AppendChain(builder, exception.Cause, depth + 1);
            builder.Append(CauseSeparator);
        }
        else if (exception.Context is not null)
        {
            AppendChain(builder, exception.Context, depth + 1);
            builder.Append(ContextSeparator);
        }
        AppendSingle(builder, exception);
    }

    private static void AppendSingle(StringBuilder builder, PyBaseException exception)
    {
        PyStackFrame[] frames = exception.Frames;
        if (frames.Length > 0)
        {
            builder.Append(Header).Append('\n');
            // frames 最内层在前，输出时从最外层开始
            for (int i = frames.Length - 1; i >= 0; i--)
            {
                PyStackFrame frame = frames[i];
                builder.Append("  File \"").Append(frame.File).Append("\", line ");
                builder.Append(frame.Line).Append(", in ").Append(frame.Function).Append('\n');
            }
        }
        builder.Append(exception.Render()).Append('\n');
    }

    /// <summary>把未捕获异常写到 stderr（格式与 CPython 一致）。</summary>
    /// <param name="exception">未被捕获的异常。</param>
    public static void Report(PyBaseException exception)
    {
        TextWriter error = Console.Error;
        error.Write(Format(exception));
        error.Flush();
    }

    /// <summary>
    /// 把 <c>SystemExit</c> 转成进程退出码：<c>None</c> → 0，整数 → 该值，
    /// 其它值 → 打印 <c>str(code)</c> 到 stderr 并退出 1。
    /// </summary>
    /// <param name="exit">退出请求。</param>
    public static int SystemExitCode(PySystemExit exit)
    {
        switch (exit.Code)
        {
            case null:
            case PyNone:
                return 0;
            case bool b:
                return b ? 1 : 0;
            case long l:
                return unchecked((int)l);
            case System.Numerics.BigInteger big:
                return unchecked((int)(long)big);
            default:
                Console.Error.Write(PyRepr.Str(exit.Code));
                Console.Error.Write('\n');
                Console.Error.Flush();
                return 1;
        }
    }

    /// <summary>清空帧栈；仅用于同一进程内反复执行多个模块的宿主场景（测试）。</summary>
    public static void Reset() => Stack.Clear();
}
