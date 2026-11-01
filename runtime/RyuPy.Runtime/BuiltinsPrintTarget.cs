using System.Globalization;

namespace RyuPy.Runtime;

public static partial class Builtins
{
    /// <summary>在一次print的实参全部求值后固定目标，避免write副作用改变后续输出去向。</summary>
    public static object? PrintTarget() => PySystem.Stdout;
    /// <summary>静态整数无装箱输出到已固定目标。</summary>
    public static void WriteI64(object? target, long value) => PySystem.Write(target, value.ToString(CultureInfo.InvariantCulture));
    /// <summary>静态浮点输出到已固定目标。</summary>
    public static void WriteF64(object? target, double value) => PySystem.Write(target, PyFormat.FloatRepr(value));
    /// <summary>静态布尔输出到已固定目标。</summary>
    public static void WriteBool(object? target, bool value) => PySystem.Write(target, value ? "True" : "False");
    /// <summary>字符串输出到已固定目标。</summary>
    public static void WriteStr(object? target, string value) => PySystem.Write(target, value);
    /// <summary>动态值的str转换不会改变本次print固定的目标。</summary>
    public static void WriteObject(object? target, object? value)
    { if (target is not (null or PyNone)) PySystem.Write(target, PyRepr.Str(value)); }
    /// <summary>默认分隔符。</summary>
    public static void WriteSep(object? target) => PySystem.Write(target, " ");
    /// <summary>默认行尾。</summary>
    public static void WriteNewline(object? target) => PySystem.Write(target, "\n");
    /// <summary>所有退出路径刷新标准流；失败转Python报告和退出码120，不从Main泄漏CLR异常。</summary>
    public static int FlushForExit(int exitCode)
    {
        try { PySystem.FlushOutputs(); return exitCode; }
        catch (System.Exception error)
        {
            PyBaseException python = error as PyBaseException ?? PyIO.FileSystemError(error);
            // 报告走宿主stderr兜底，不能再次调用失败的用户flush形成递归。
            try { System.Console.Error.WriteLine("Exception ignored while flushing standard streams:");
                System.Console.Error.Write(PyTraceback.Format(python)); System.Console.Error.Flush(); }
            catch (System.Exception) { /* 报告通道也不可写时仍保持确定退出码。 */ }
            return 120;
        }
    }

}
