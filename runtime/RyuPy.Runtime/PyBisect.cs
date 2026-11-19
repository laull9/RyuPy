// bisect 的通用二分搜索：只按指定方向调用 <，不排序、不复制输入、不缓存用户 key。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyBisect
{
    internal static Dictionary<string, object?> CreateAttributes()
    {
        var left = Function("bisect_left", false, false);
        var right = Function("bisect_right", true, false);
        var insertLeft = Function("insort_left", false, true);
        var insertRight = Function("insort_right", true, true);
        return new(StringComparer.Ordinal)
        {
            ["bisect_left"] = left, ["bisect_right"] = right, ["bisect"] = right,
            ["insort_left"] = insertLeft, ["insort_right"] = insertRight, ["insort"] = insertRight,
        };
    }

    private static PyFunction Function(string name, bool right, bool insert) =>
        PyNativeFunctions.Create(name, ["a", "x", "lo", "hi", "key"],
            [null, null, 0L, PyNone.Instance, PyNone.Instance], 2,
            a => Invoke(a, right, insert), ["pos", "pos", "pos", "pos", "kwonly"]);

    private static object? Invoke(object?[] args, bool right, bool insert)
    {
        // 转换顺序与 CPython 的 C 参数解析一致：先 lo/hi，再 insort 的 key(x)。
        long lo = Index(args[2]);
        long hi = args[3] is null or PyNone ? -1 : Index(args[3]);
        object? key = args[4];
        bool keyed = key is not null and not PyNone;
        object? x = insert && keyed ? PyOps.CallValue(key, [args[1]], [], []) : args[1];
        if (lo < 0) throw new PyValueError("lo must be non-negative");
        // -1 是 CPython 加速器公开行为中的长度哨兵，其余负 hi 保留为空区间。
        if (hi == -1) hi = PyOps.Len(args[0]);
        while (lo < hi)
        {
            long mid = lo + (hi - lo) / 2;
            object? item = PyOps.GetItem(args[0], mid);
            if (keyed) item = PyOps.CallValue(key, [item], [], []);
            bool before = right
                ? PyOps.Compare(PyOps.Lt, x ?? PyNone.Instance, item ?? PyNone.Instance)
                : PyOps.Compare(PyOps.Lt, item ?? PyNone.Instance, x ?? PyNone.Instance);
            if (right ? before : !before) hi = mid;
            else lo = mid + 1;
        }
        if (!insert) return lo;
        // 不直接修改底层容器；派生类和用户序列的 insert 重载必须收到原始 x。
        PyOps.CallMethod(args[0], "insert", [lo, args[1]], [], []);
        return PyNone.Instance;
    }

    private static long Index(object? value)
    {
        if (!PyInt.IsInt(value))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError("object cannot be interpreted as an integer");
            if (!PyInt.IsInt(value)) throw new PyTypeError("__index__ returned non-int");
        }
        BigInteger index = PyInt.ToBigInteger(value!);
        if (index < long.MinValue || index > long.MaxValue)
            throw new PyOverflowError("Python int too large to convert to C ssize_t");
        return (long)index;
    }
}
