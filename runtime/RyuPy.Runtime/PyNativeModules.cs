using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>原生标准库的显式注册表：只公开已实现API，缺失属性正常抛AttributeError。</summary>
public static class PyNativeModules
{
    private static bool _registered;

    /// <summary>系统入口初始化时登记；不运行任何第三方或用户代码。</summary>
    public static void Register()
    {
        if (_registered) return;
        _registered = true;
        PyModules.RegisterNative("io", new Dictionary<string, object?>
        {
            ["StringIO"] = new PyNativeType("StringIO", "_io", PyIO.CreateStringIO,
                value => value is PyIOTextMemory, "TextIOBase", "IOBase"),
            ["BytesIO"] = new PyNativeType("BytesIO", "_io", PyIO.CreateBytesIO,
                value => value is PyIOBinary binary && binary.TypeName == "BytesIO", "BufferedIOBase", "IOBase"),
            ["IOBase"] = new PyNativeType("IOBase", "io", null, value => value is PyIOStream or PyStandardStream),
            ["TextIOBase"] = new PyNativeType("TextIOBase", "io", null,
                value => value is PyIOTextMemory or PyIOTextFile or PyStandardStream, "IOBase"),
            ["BufferedIOBase"] = new PyNativeType("BufferedIOBase", "io", null,
                value => value is PyIOBinary binary && binary.TypeName != "FileIO", "IOBase"),
            ["TextIOWrapper"] = new PyNativeType("TextIOWrapper", "_io", null,
                value => value is PyIOTextFile or PyStandardStream, "TextIOBase", "IOBase"),
            ["open"] = Forward("open", PyIO.Open),
        });
        PyModules.RegisterNative("math", PyMathModule.CreateAttributes());
        PyModules.RegisterNative("time", PyTimeModule.CreateAttributes());
        PyModules.RegisterNative("json", PyJsonModule.CreateAttributes(), (key, _) =>
        {
            if (key == "JSONDecodeError")
                throw new PyNotImplementedError("rebinding json.JSONDecodeError is not supported");
        });
        PyModules.RegisterNative("re", PyRegex.CreateAttributes(), (key, _) =>
        {
            if (key == "error")
                throw new PyNotImplementedError("rebinding re.error is not supported");
        });
        var os = PyOs.CreateAttributes();
        os["environ"] = new PyEnvironment();
        os["getenv"] = PyNativeFunctions.Create("getenv", ["key", "default"], [null, PyNone.Instance], 1,
            args => PyEnvironment.Get(args[0], args[1]));
        os["putenv"] = PyNativeFunctions.Create("putenv", ["key", "value"], [null, null], 2, args =>
        { PyEnvironment.Set(args[0], args[1]); return PyNone.Instance; });
        os["unsetenv"] = PyNativeFunctions.Create("unsetenv", ["key"], [null], 1, args =>
        { PyEnvironment.Unset(args[0]); return PyNone.Instance; });
        PyModules.RegisterNative("os", os);
        PyModules.RegisterNative("os.path", PyOsPath.CreateAttributes());
        // os.path同时是os的公开属性；与import os.path返回同一模块对象。
        PyModules.Import("os.path");
        PyModules.RegisterNative("pathlib", PyPaths.CreateAttributes());
    }

    private static PyFunction Forward(string name, Func<object?[], string[], object?[], object?> call) =>
        new(name, PySignature.Create(name, [], [], []), (_, args, names, values) => call(args, names, values), [], false);
}
