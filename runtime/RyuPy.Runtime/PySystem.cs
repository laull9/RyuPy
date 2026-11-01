using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>进程参数、标准流与受控系统模块；入口初始化与静态/动态print共享同一状态。</summary>
public static class PySystem
{
    private static PyModule? _sys;
    private static readonly PyStandardStream DefaultOutput = new("<stdout>", null, Builtins.DefaultOutput);
    private static readonly PyStandardStream DefaultError = new("<stderr>", null, Console.Error);
    private static readonly PyStandardStream DefaultInput = new("<stdin>", Console.In, null);

    /// <summary>由生成的Main调用；argv[0]使用编译入口源码标识而不是dotnet宿主路径。</summary>
    public static void Initialize(string sourceName, string[] args)
    {
        var argv = new List<object?> { sourceName };
        argv.AddRange(args);
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["argv"] = new PyList(argv), ["path"] = new PyTuple([System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(sourceName)) ?? ""]), ["stdin"] = DefaultInput, ["stdout"] = DefaultOutput,
            ["stderr"] = DefaultError, ["__stdin__"] = DefaultInput, ["__stdout__"] = DefaultOutput,
            ["__stderr__"] = DefaultError, ["modules"] = new PyModuleView(),
            ["platform"] = OperatingSystem.IsWindows() ? "win32" : OperatingSystem.IsMacOS() ? "darwin" : "linux",
            ["byteorder"] = BitConverter.IsLittleEndian ? "little" : "big", ["maxsize"] = long.MaxValue,
            ["exit"] = PyNativeFunctions.Create("exit", ["status"], [PyNone.Instance], 0,
                values => throw new PySystemExit(values[0] is PyNone ? null : values[0])),
        };
        if (_sys is null)
        {
            PyModules.RegisterNative("sys", attributes, (name, _) =>
            {
                if (name is "modules" or "path") throw new PyNotImplementedError("replacement of sys." + name + " is not supported");
            });
            _sys = PyModules.Import("sys");
        }
        else foreach (var pair in attributes) if (pair.Key is not ("modules" or "path")) _sys.SetAttr(pair.Key, pair.Value);
        PyNativeModules.Register();
    }

    /// <summary>读取当前sys.stdout；模块属性赋值立即对所有print路径生效。</summary>
    public static object? Stdout => _sys?.GetAttr("stdout") ?? DefaultOutput;

    /// <summary>当前标准输入，供input共享重绑定语义。</summary>
    public static object? Stdin => _sys?.GetAttr("stdin") ?? DefaultInput;

    /// <summary>当前标准错误流，用户重绑定同样对错误报告生效。</summary>
    public static object? Stderr => _sys?.GetAttr("stderr") ?? DefaultError;

    /// <summary>写入当前标准输出，保留自定义write方法副作用。</summary>
    public static void WriteStdout(string text) => Write(Stdout, text);

    /// <summary>写入已经选定的目标；None标准流按Python规则忽略输出。</summary>
    public static void Write(object? target, string text)
    {
        if (target is null or PyNone) return;
        if (target is PyStandardStream stream) stream.Write(text);
        else PyOps.CallValue(PyObject.GetAttr(target, "write"), [text], [], []);
    }

    /// <summary>刷新显式选择的流。</summary>
    public static void Flush(object? target)
    {
        if (target is null or PyNone) return;
        if (target is PyStandardStream stream) stream.Flush();
        else PyOps.CallValue(PyObject.GetAttr(target, "flush"), [], [], []);
    }
    /// <summary>退出前刷新原始缓冲及当前重绑定流；不关闭调用方拥有的流。</summary>
    public static void FlushOutputs()
    {
        Exception? first = null;
        try { Builtins.Flush(); } catch (Exception error) { first = error; }
        var seen = new HashSet<object>(System.Collections.Generic.ReferenceEqualityComparer.Instance) { DefaultOutput };
        foreach (object? target in new[] { Stdout, Stderr })
        {
            if (target is null or PyNone || !seen.Add(target)) continue;
            try { Flush(target); } catch (Exception error) { first ??= error; }
        }
        // 单条流失败不能阻止另一条流落盘；全部尝试后再报告首个失败。
        if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
    }

}
