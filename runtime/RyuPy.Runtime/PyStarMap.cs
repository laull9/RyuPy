// starmap 每轮只取一个实参行，再解包调用；失败已消费该行，但不永久关闭源游标。
// 显式 next 保留用户 StopIteration.value，循环入口仅把该异常解释为本轮耗尽。
using System;

namespace RyuPy.Runtime;

internal sealed class PyStarMap : PyToolIterator
{
    private static readonly Lazy<PyNativeType> NativeType = new(() => new PyNativeType(
        "starmap", "itertools", Create, value => value is PyStarMap));
    private readonly object? function;
    private readonly IPyIterator source;
    private object? current;

    private PyStarMap(object? function, IPyIterator source)
    { this.function = function; this.source = source; }

    internal static PyNativeType Type => NativeType.Value;
    public override string TypeName => "itertools.starmap";

    private static object Create(object?[] args, string[] names, object?[] values)
    {
        if (names.Length != 0) throw new PyTypeError("starmap() takes no keyword arguments");
        if (args.Length != 2) throw new PyTypeError($"starmap expected 2 arguments, got {args.Length}");
        // callable 验证必须延后；空输入允许不可调用的第一个实参。
        return new PyStarMap(args[0], PyIter.Create(args[1]));
    }

    internal object? Next(object? fallback = null, bool hasFallback = false)
    {
        try
        {
            object? row = ReadNext(source);
            // 每轮独立数组，用户解包或 callable 重入不覆盖外层实参。
            object?[] arguments = PyOps.ToSequence(row);
            current = PyOps.CallValue(function, arguments, [], []);
            return current;
        }
        catch (PyStopIteration) when (hasFallback) { return fallback; }
    }

    private static object? ReadNext(IPyIterator iterator)
    {
        if (iterator is PyGenerator generator) return generator.Next(null, false);
        if (iterator is PyMap map) return map.Next();
        if (iterator is PyStarMap starMap) return starMap.Next();
        if (iterator is PyInstanceIterator user)
        {
            if (PyObject.TryMethod(user.Instance, "__next__", [], out object? value)) return value;
            throw new PyTypeError("object is not an iterator");
        }
        return iterator.MoveNext() ? iterator.Current() : throw new PyStopIteration();
    }

    public override bool MoveNext()
    {
        try { Next(); return true; }
        catch (PyStopIteration) { return false; }
    }

    public override object? Current() => current;
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "__next__" => PyNativeFunctions.Create(name, [], [], 0, _ => Next()),
        _ => base.GetAttr(name),
    };
}
