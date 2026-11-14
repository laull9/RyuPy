// shutil 的有限流复制：保存普通绑定方法，原样转交块与长度，不取得流所有权。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>真实 copyfileobj 协议；不冒充文件复制、权限或链接处理接口。</summary>
public static class PyShutil
{
    /// <summary>函数闭包与模块读写共用属性表，保存的函数仍观察 COPY_BUFSIZE 的后续重绑。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["COPY_BUFSIZE"] = OperatingSystem.IsWindows() ? 1048576L : 65536L,
        };
        attributes["copyfileobj"] = PyNativeFunctions.Create("copyfileobj",
            ["fsrc", "fdst", "length"], [null, null, 0L], 2,
            args => Copy(attributes, args[0], args[1], args[2]));
        return attributes;
    }

    private static object Copy(Dictionary<string, object?> attributes, object? source, object? destination, object? length)
    {
        // 只对传入长度求真值一次；模块默认值不再次验证或转换，交给 read 自身处理。
        if (!PyOps.Truthy(length)) length = attributes["COPY_BUFSIZE"];
        object? read = PyObject.GetAttr(source, "read");
        object? write = PyObject.GetAttr(destination, "write");
        while (true)
        {
            object? buffer = PyOps.CallValue(read, [length], [], []);
            if (!PyOps.Truthy(buffer)) return PyNone.Instance;
            // 写入结果并非结束信号；短读、负长度及自定义块均保持同一个循环。
            _ = PyOps.CallValue(write, [buffer], [], []);
        }
    }
}
