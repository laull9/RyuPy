using System;

namespace RyuPy.Runtime;

/// <summary>内建 map 的惰性游标：构造只取迭代器，每轮左到右消费并调用一次函数。</summary>
public sealed class PyMap : PyToolIterator
{
    private static readonly Lazy<PyNativeType> NativeType = new(() => new PyNativeType(
        "map", "builtins", Create, value => value is PyMap));
    private readonly object? _function;
    private readonly IPyIterator[] _iterators;
    private readonly object?[] _arguments;
    private bool _active;
    private object? _current;

    private PyMap(object? function, IPyIterator[] iterators)
    {
        _function = function;
        _iterators = iterators;
        _arguments = new object?[iterators.Length];
    }

    /// <summary>稳定的内建类型对象，供一等值、type 与 isinstance 共用。</summary>
    public static PyNativeType Type => NativeType.Value;

    /// <summary>Python 3.12 的纯位置构造入口；函数是否可调用延后至真正取得元素时判断。</summary>
    public static object Create(object?[] args, string[] names, object?[] values)
    {
        _ = Type;
        if (names.Length != 0) throw new PyTypeError("map() takes no keyword arguments");
        if (args.Length < 2) throw new PyTypeError("map() must have at least two arguments");
        var iterators = new IPyIterator[args.Length - 1];
        for (int i = 0; i < iterators.Length; i++) iterators[i] = PyIter.Create(args[i + 1]);
        return new PyMap(args[0], iterators);
    }

    /// <inheritdoc />
    public override string TypeName => "map";

    /// <inheritdoc />
    public override bool MoveNext()
    {
        try { return Advance(); }
        catch (PyStopIteration) { return false; }
    }

    /// <summary>显式 next 保留输入或函数抛出的 StopIteration.value，默认值仅在耗尽时使用。</summary>
    public object? Next(object? fallback = null, bool hasFallback = false)
    {
        try { return Advance() ? _current : throw new PyStopIteration(); }
        catch (PyStopIteration) when (hasFallback) { return fallback; }
    }

    /// <inheritdoc />
    public override object? GetAttr(string name) => name == "__next__"
        ? PyNativeFunctions.Create(name, [], [], 0, _ => Next())
        : base.GetAttr(name);

    private bool Advance()
    {
        // 常规迭代复用实参数组；用户 next/callable 可重入同一 map，此时单独建帧，
        // 避免覆盖仍在执行的外层参数。finally 清槽，不把已消费元素长期保留。
        bool wasActive = _active;
        object?[] arguments = wasActive ? new object?[_iterators.Length] : _arguments;
        _active = true;
        _current = null;
        try
        {
            for (int i = 0; i < _iterators.Length; i++)
            {
                // 用户适配器的 bool 路径会永久缓存耗尽并丢终值；map 与 yield from
                // 一样必须保留真实 next，允许非规范但可恢复的用户游标继续执行。
                arguments[i] = ReadNext(_iterators[i]);
            }
            _current = PyOps.CallValue(_function, arguments, [], []);
            return true;
        }
        finally
        {
            Array.Clear(arguments);
            _active = wasActive;
        }
    }

    private static object? ReadNext(IPyIterator iterator)
    {
        if (iterator is PyGenerator generator) return generator.Next(null, false);
        if (iterator is PyMap map) return map.Next();
        if (iterator is PyInstanceIterator user)
        {
            if (PyObject.TryMethod(user.Instance, "__next__", [], out object? value)) return value;
            throw new PyTypeError("object is not an iterator");
        }
        return iterator.MoveNext() ? iterator.Current() : throw new PyStopIteration();
    }

    /// <inheritdoc />
    public override object? Current() => _current;
}
