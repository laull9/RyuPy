using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>map 的惰性消费、终值传播和重入实参隔离；不依赖编译器名称登记。</summary>
public class PyMapTests
{
    private static PyMap New(object? function, params object?[] inputs)
    {
        var args = new object?[inputs.Length + 1];
        args[0] = function; Array.Copy(inputs, 0, args, 1, inputs.Length);
        return (PyMap)PyMap.Create(args, [], []);
    }

    [Fact]
    public void ConstructorOnlyCreatesIteratorsAndDefersCallableValidation()
    {
        var source = new CountingIterator(3);
        PyMap map = New(PyNone.Instance, source);
        Assert.Equal(0, source.Calls);
        Assert.Throws<PyTypeError>(() => map.Next());
        Assert.Equal(1, source.Calls);
        Assert.Throws<PyTypeError>(() => map.Next());
        Assert.Equal(2, source.Calls);
        Assert.False(New(PyNone.Instance, new PyTuple([])).MoveNext());
        Assert.Throws<PyTypeError>(() => PyMap.Create([PyNone.Instance], [], []));
        Assert.Throws<PyTypeError>(() => PyMap.Create([PyNone.Instance, source], ["strict"], [true]));
        Assert.Same(PyMap.Type, PyNativeType.LookupByName("map"));
    }

    [Fact]
    public void ShortestInputStopsAfterEarlierInputsWereConsumed()
    {
        var left = new CountingIterator(3); var right = new CountingIterator(1);
        object function = PyNativeFunctions.Create("sum", ["a", "b"], [null, null], 2,
            args => (long)args[0]! + (long)args[1]!);
        PyMap map = New(function, left, right);
        Assert.Equal(2L, map.Next());
        Assert.False(map.MoveNext());
        Assert.Equal(2, left.Calls); Assert.Equal(2, right.Calls);
        Assert.False(map.MoveNext()); Assert.Equal(3, left.Calls);
    }

    [Fact]
    public void CallableStopValueSurvivesAndNextCallResumes()
    {
        object function = PyNativeFunctions.Create("f", ["x"], [null], 1,
            args => (long)args[0]! == 1 ? throw new PyStopIteration("pause") : args[0]);
        PyMap map = New(function, new CountingIterator(3));
        Assert.Equal("pause", Assert.Throws<PyStopIteration>(() => map.Next()).Value);
        Assert.Equal(2L, map.Next()); Assert.Equal(3L, map.Next());
        Assert.Equal("end", map.Next("end", true));
    }

    [Fact]
    public void ReentryDoesNotOverwriteOuterArgumentFrame()
    {
        PyMap? map = null;
        // 使用原生函数委托直接检查传入帧；Python thunk 也可能在绑定前重入。
        var function = new PyFunction("f", PySignature.Create("f", ["x"], ["pos"], ["0"]),
            (_, args, names, values) =>
            {
                object? old = args[0];
                if ((long)old! == 1) { Assert.Equal(2L, map!.Next()); Assert.Same(old, args[0]); }
                return old;
            }, [], false);
        map = New(function, new CountingIterator(3));
        Assert.Equal(1L, map.Next()); Assert.Equal(3L, map.Next());
    }

    private sealed class CountingIterator(int count) : IPyIterator
    {
        public int Calls { get; private set; }
        public bool MoveNext() => ++Calls <= count;
        public object Current() => (long)Calls;
    }
}
