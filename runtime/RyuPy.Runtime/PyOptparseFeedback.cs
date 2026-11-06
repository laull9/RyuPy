// 解析错误所需的默认usage/version/退出桥；完整帮助排版由阶段C提供。
namespace RyuPy.Runtime;

public sealed partial class PyOptparseParser
{
    private string GetUsage() => PyOps.Truthy(Data["usage"]) ? "Usage: " + PyOptparse.Text(ExpandProg(Data["usage"])) + "\n" : "";
    private object? GetVersion() => PyOps.Truthy(Data["version"]) ? ExpandProg(Data["version"]) : "";
    private void PrintUsage(object? file)
    {
        if (!PyOps.Truthy(Data["usage"])) return;
        PrintLine(GetUsage(), file);
    }
    private void PrintVersion(object? file)
    {
        if (!PyOps.Truthy(Data["version"])) return;
        PrintLine(PyOptparse.Text(GetVersion()), file);
    }
    private static void PrintLine(string text, object? file)
    {
        object? target = PyOptparse.None(file) ? PySystem.Stdout : file;
        // print保留文本和换行两次write，不合并用户流可观察的回调。
        PySystem.Write(target, text); PySystem.Write(target, "\n");
    }
    private void Exit(object? status, object? message)
    {
        if (PyOps.Truthy(message)) PyOps.CallMethod(PySystem.Stderr, "write", [message], [], []);
        throw PyOptparse.Own(new PySystemExit(status));
    }
    private void Error(object? message)
    {
        PrintUsage(PySystem.Stderr);
        Exit(2L, ProgramName() + ": error: " + PyRepr.Str(message) + "\n");
    }
}
