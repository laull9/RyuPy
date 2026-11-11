using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>进程参数、标准流与受控系统模块；入口初始化与静态/动态print共享同一状态。</summary>
public static class PySystem
{
    private static PyModule? _sys;
    // 首次真实初始化绑定，后续Initialize不为被修改状态重新签发身份。
    private static Dictionary<string, object?>? originalAttributes;
    private static object? originalPath;
    internal static bool HasOriginalImportPath(PyModule system)
        => IsOriginalSystem(system) && originalPath is not null
            && originalAttributes is not null
            && originalAttributes.TryGetValue("path", out var path) && ReferenceEquals(path, originalPath)
            && !system.DynamicAttributes.ContainsKey("path")
            && !HasExplicitFinderConfiguration
            && !HasFinderHooks(originalAttributes) && !HasFinderHooks(system.DynamicAttributes);
    private static bool HasFinderHooks(Dictionary<string, object?> attributes)
        => attributes.ContainsKey("meta_path") || attributes.ContainsKey("path_hooks")
            || attributes.ContainsKey("path_importer_cache");
    // 候选冷激活不调用公开sys getter，配置写入时记录内部拒绝事实。
    internal static bool HasExplicitFinderConfiguration { get; private set; }
    internal static bool IsOriginalSystem(PyModule module) => ReferenceEquals(module, _sys);
    private static readonly PyStandardStream DefaultOutput = new("<stdout>", null, Builtins.DefaultOutput);
    private static readonly PyStandardStream DefaultError = new("<stderr>", null, Console.Error);
    private static readonly PyStandardStream DefaultInput = new("<stdin>", Console.In, null);
    // 产品 V1 仅接受本进程原始CLR输出流；用户/宿主自定义writer不在准入闭包。
    internal static bool UsesDefaultOutputStreams
        => ReferenceEquals(Stdout, DefaultOutput) && ReferenceEquals(Stderr, DefaultError);

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
            ["version_info"] = PySystemVersion.VersionInfo, ["version"] = PySystemVersion.Version,
            ["implementation"] = PySystemVersion.Implementation,
            ["byteorder"] = BitConverter.IsLittleEndian ? "little" : "big", ["maxsize"] = long.MaxValue,
            ["exit"] = PyNativeFunctions.Create("exit", ["status"], [PyNone.Instance], 0,
                values => throw new PySystemExit(values[0] is PyNone ? null : values[0])),
        };
        if (_sys is null)
        {
            bool freshSystem = !PyModules.TryCached("sys", out _);
            PyModules.RegisterNative("sys", attributes, (name, _) =>
            {
                if (name is "meta_path" or "path_hooks" or "path_importer_cache") HasExplicitFinderConfiguration = true;
                if (name is "modules" or "path" or "unraisablehook") throw new PyNotImplementedError("replacement of sys." + name + " is not supported");
            });
            _sys = PyModules.Import("sys");
            // 首次已有未知缓存对象时不认证它；普通初始化行为仍沿旧路径。
            if (freshSystem)
            {
                originalAttributes = attributes;
                originalPath = attributes["path"];
            }
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
        // V1 源码域禁止改绑输出流；实际默认writer的CLR失败每次由Guard创建新Python异常。
        // 因而保留首错误与Suppress后续错误不会把同一owner的事件混为一谈。
        if (PyTraceProduct.Enabled && !UsesDefaultOutputStreams)
            throw new InvalidOperationException("trace V1 output stream escaped its admitted domain");
        Exception? first = null;
        try { Builtins.Flush(); } catch (Exception error) { first = error; }
        var seen = new HashSet<object>(System.Collections.Generic.ReferenceEqualityComparer.Instance) { DefaultOutput };
        foreach (object? target in new[] { Stdout, Stderr })
        {
            if (target is null or PyNone || !seen.Add(target)) continue;
            try { Flush(target); }
            catch (Exception error)
            {
                if (first is null) first = error;
                else PyTraceProduct.Suppress(error);
            }
        }
        // 单条流失败不能阻止另一条流落盘；全部尝试后再报告首个失败。
        if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
    }

}
