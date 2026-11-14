// _thread 首个有限 API：当前实际 CLR 线程身份，不声明 Python 线程创建或 GIL 能力。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyThreadIdentity
{
    // Python 只承诺非零身份在存活线程间唯一；不将平台相关值伪装成 OS native id。
    internal static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["get_ident"] = PyNativeFunctions.Create("get_ident", [], [], 0,
            _ => (long)Environment.CurrentManagedThreadId),
    };
}
