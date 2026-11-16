// traceback 的当前异常窄接口：复用已有帧/原因链，不造 frame、不借终端报告接收异常。
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTracebackModule
{
    internal static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["format_exc"] = PyNativeFunctions.Create("format_exc", ["limit", "chain"],
            [PyNone.Instance, true], 0, a => string.Concat(Parts(a[0], a[1]))),
        ["print_exc"] = PyNativeFunctions.Create("print_exc", ["limit", "file", "chain"],
            [PyNone.Instance, PyNone.Instance, true], 0, a =>
            {
                object? file = a[1] is null or PyNone ? PySystem.Stderr : a[1];
                foreach (string part in Parts(a[0], a[2]))
                {
                    PySystem.Write(file, part);
                    // CPython print(line, end="") 仍调用 write("")，保留 writer 副作用。
                    PySystem.Write(file, "");
                }
                return PyNone.Instance;
            }),
    };

    private static string[] Parts(object? limit, object? chain)
    {
        // V1 尚未完成可观察 traceback API 准入；不能调用 Terminal 改变异常传播状态。
        if (PyTraceProduct.Enabled)
            throw new PyNotImplementedError("traceback observation is not supported in trace V1");
        PyBaseException? current = PyErrors.Current;
        // 即使没有当前异常，也先校验 limit，再求值 chain 一次；失败不能跳过用户副作用顺序。
        long? count = limit is null or PyNone ? null : Limit(limit);
        bool includeChain = PyOps.Truthy(chain);
        if (current is null) return ["NoneType: None\n"];
        return PyTraceback.FormatParts(current, count, includeChain);
    }

    private static long Limit(object? value)
    {
        // 只接整数/布尔参数；保持 CPython islice/deque 对巨大范围的错误，不静默裁切。
        if (value is double number && number >= 0)
            throw new PyValueError("Stop argument for islice() must be None or an integer: 0 <= x <= sys.maxsize.");
        if (!PyInt.IsInt(value)) throw new PyTypeError("traceback limit must be an integer or None");
        if (value is BigInteger big)
        {
            if (big > long.MaxValue) throw new PyValueError("Stop argument for islice() exceeds sys.maxsize");
            if (big <= long.MinValue) throw new PyOverflowError("Python int too large to convert to C ssize_t");
            return (long)big;
        }
        long count = PyCast.ToInt64(value);
        if (count == long.MinValue) throw new PyOverflowError("Python int too large to convert to C ssize_t");
        return count;
    }
}
