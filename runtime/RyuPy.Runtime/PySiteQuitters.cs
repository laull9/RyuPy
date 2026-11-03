// 默认 site-enabled 启动的真实 Quitter 对象；不实现 site 模块或 no-site 启动模式。
using System;

namespace RyuPy.Runtime;

internal static class PySiteQuitters
{
    private sealed record State(PyInstance Exit, PyInstance Quit);
    private static readonly Lazy<State> Values = new(Create);
    internal static PyInstance Get(string name) => name == "exit" ? Values.Value.Exit : Values.Value.Quit;

    private static State Create()
    {
        var type = new PyClass("Quitter") { NativeDefinitionModule = "_sitebuiltins" };
        type.SetAttr("__module__", "_sitebuiltins");
        type.SetAttr("__init__", Method("__init__", ["self", "name", "eof"], [null, null, null], 3, Initialize));
        type.SetAttr("__repr__", Method("__repr__", ["self"], [null], 1, Represent));
        type.SetAttr("__call__", Method("__call__", ["self", "code"], [null, PyNone.Instance], 1, Call));
        string eof = OperatingSystem.IsWindows() ? "Ctrl-Z plus Return" : "Ctrl-D (i.e. EOF)";
        var exit = new PyInstance(type);
        var quit = new PyInstance(type);
        Initialize([exit, "exit", eof]);
        Initialize([quit, "quit", eof]);
        return new State(exit, quit);
    }

    // 函数名和绑定错误里的限定名分开；绑定失败尚未触碰 stdin，不建立伪 Python 源帧。
    private static PyFunction Method(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body)
    {
        var kinds = new string[names.Length]; Array.Fill(kinds, "pos");
        var flags = new string[names.Length];
        for (int i = 0; i < flags.Length; i++) flags[i] = i < required ? "0" : "1";
        PySignature signature = PySignature.Create("Quitter." + name, names, kinds, flags);
        return new PyFunction(name, signature, (function, args, keywords, values) =>
        {
            object?[] bound;
            try { bound = PyBinder.Bind(signature, args, keywords, values, function.Defaults); }
            catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
            return body(bound);
        }, defaults, false);
    }

    private static object Initialize(object?[] args)
    {
        try
        {
            PyObject.SetAttr(args[0], "name", args[1]);
            PyObject.SetAttr(args[0], "eof", args[2]);
            return PyNone.Instance;
        }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
    }

    private static object Represent(object?[] args)
    {
        try
        {
            // 先取两个属性，再按 Python %s 的顺序转换，不约束可写字段只能为 string。
            object? name = PyObject.GetAttr(args[0], "name");
            object? eof = PyObject.GetAttr(args[0], "eof");
            return "Use " + PyRepr.Str(name) + "() or " + PyRepr.Str(eof) + " to exit";
        }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
    }

    private static object Call(object?[] args)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try { PyOps.CallValue(PyObject.GetAttr(PySystem.Stdin, "close"), [], [], []); }
        catch (PyBaseException) { /* Python 裸 except 吞下 close 的 BaseException；不承诺吞宿主致命错误。 */ }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
        // 必须在吞错处理结束后抛出，保外层活动异常，而不是被吞 close 异常。
        // 默认参数是实际 PyNone，故 SystemExit.args 为 (None,)，不能转成 CLR null。
        throw (PySystemExit)PyErrors.PrepareRaise(new PySystemExit(args[1]));
    }
}
