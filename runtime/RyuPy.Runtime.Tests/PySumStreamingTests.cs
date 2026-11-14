// 通过真实sum入口验证流式消费、加法回调交错、异常早停和start身份。
using System;
using System.Collections.Generic;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PySumStreamingTests
{
    private sealed class TrackingIterator : IPyIterator
    {
        private readonly List<string> _events;
        private readonly object?[] _items;
        private int _index;
        private readonly int _failAtMoveNext;

        public TrackingIterator(List<string> events, object?[] items, int failAtMoveNext = -1)
        {
            _events = events;
            _items = items;
            _failAtMoveNext = failAtMoveNext;
        }

        public bool MoveNext()
        {
            _events.Add($"MoveNext:{_index}");
            if (_failAtMoveNext >= 0 && _index == _failAtMoveNext)
            {
                throw new PyRuntimeError($"iterator failed at {_index}");
            }
            if (_index >= _items.Length)
            {
                return false;
            }
            _index++;
            return true;
        }

        public object? Current()
        {
            _events.Add($"Current:{_index - 1}");
            return _items[_index - 1];
        }
    }

    private static PyInstance CreateItem(string label, long val, List<string> events, bool failOnAdd = false)
    {
        var cls = new PyClass($"TrackedItem_{label}");
        cls.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2, a =>
        {
            events.Add($"Add:{label}");
            if (failOnAdd)
            {
                throw new PyValueError($"error adding {label}");
            }
            return PyOps.Binary(PyOps.Add, a[1]!, val);
        }));
        return new PyInstance(cls);
    }

    [Fact]
    public void StreamingConsumptionIsStrictlyInterleaved()
    {
        var events = new List<string>();
        var itemA = CreateItem("A", 10L, events);
        var itemB = CreateItem("B", 20L, events);
        var itemC = CreateItem("C", 30L, events);

        var iter = new TrackingIterator(events, [itemA, itemB, itemC]);
        object? result = PyOps.CallBuiltin("sum", [iter], [], []);

        Assert.Equal(60L, result);
        Assert.Equal(
            [
                "MoveNext:0", "Current:0", "Add:A",
                "MoveNext:1", "Current:1", "Add:B",
                "MoveNext:2", "Current:2", "Add:C",
                "MoveNext:3"
            ],
            events
        );
    }

    [Fact]
    public void EarlyStoppingWhenAdditionThrowsException()
    {
        var events = new List<string>();
        var itemA = CreateItem("A", 10L, events);
        var itemB = CreateItem("B", 20L, events, failOnAdd: true);
        var itemC = CreateItem("C", 30L, events);

        var iter = new TrackingIterator(events, [itemA, itemB, itemC]);
        var error = Assert.Throws<PyValueError>(() => PyOps.CallBuiltin("sum", [iter], [], []));
        Assert.Equal("error adding B", error.Message);

        // 第B项加法抛错后，不能再调用第3项的MoveNext或Current。
        Assert.Equal(
            [
                "MoveNext:0", "Current:0", "Add:A",
                "MoveNext:1", "Current:1", "Add:B"
            ],
            events
        );
    }

    [Fact]
    public void EmptyIterablePreservesStartIdentity()
    {
        var customStart = new PyList([42L]);
        object? positional = PyOps.CallBuiltin("sum", [new PyList([]), customStart], [], []);
        Assert.Same(customStart, positional);

        object? keyword = PyOps.CallBuiltin("sum", [new PyList([])], ["start"], [customStart]);
        Assert.Same(customStart, keyword);

        object? defaultStart = PyOps.CallBuiltin("sum", [new PyList([])], [], []);
        Assert.Equal(0L, defaultStart);
    }

    [Fact]
    public void SumWithNumericStartPositionalAndKeyword()
    {
        var list = new PyList([1L, 2L, 3L]);
        object? positional = PyOps.CallBuiltin("sum", [list, 100L], [], []);
        Assert.Equal(106L, positional);

        object? keyword = PyOps.CallBuiltin("sum", [list], ["start"], [100L]);
        Assert.Equal(106L, keyword);
    }

    [Fact]
    public void LiveListMutationDuringStreamingSumVisitsAppendedItem()
    {
        var events = new List<string>();
        var list = new PyList();

        PyInstance item1 = null!;
        var cls1 = new PyClass("MutatorItem1");
        cls1.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2, a =>
        {
            events.Add("Add:1");
            var item3 = CreateItem("3", 3L, events);
            list.Items.Add(item3);
            return PyOps.Binary(PyOps.Add, a[1]!, 1L);
        }));
        item1 = new PyInstance(cls1);

        var item2 = CreateItem("2", 2L, events);
        list.Items.Add(item1);
        list.Items.Add(item2);

        object? total = PyOps.CallBuiltin("sum", [list], [], []);
        Assert.Equal(6L, total);
        Assert.Equal(3, list.Items.Count);
        Assert.Equal(["Add:1", "Add:2", "Add:3"], events);
    }

    [Fact]
    public void CustomStartUsesAddAndNeverIAdd()
    {
        int addCalls = 0;
        int iaddCalls = 0;

        var startClass = new PyClass("CustomStartType");
        startClass.SetAttr("__add__", PyNativeFunctions.Create("__add__", ["self", "other"], [null, null], 2, a =>
        {
            addCalls++;
            return PyOps.Binary(PyOps.Add, 10L, a[1]!);
        }));
        startClass.SetAttr("__iadd__", PyNativeFunctions.Create("__iadd__", ["self", "other"], [null, null], 2, a =>
        {
            iaddCalls++;
            return a[0];
        }));

        var startInstance = new PyInstance(startClass);
        var list = new PyList([5L]);
        object? res = PyOps.CallBuiltin("sum", [list, startInstance], [], []);

        Assert.Equal(15L, res);
        Assert.Equal(1, addCalls);
        Assert.Equal(0, iaddCalls);
    }

    [Fact]
    public void IteratorExceptionPropagatesImmediately()
    {
        var events = new List<string>();
        var itemA = CreateItem("A", 1L, events);
        var itemB = CreateItem("B", 2L, events);

        var iter = new TrackingIterator(events, [itemA, itemB], failAtMoveNext: 1);
        var err = Assert.Throws<PyRuntimeError>(() => PyOps.CallBuiltin("sum", [iter], [], []));
        Assert.Equal("iterator failed at 1", err.Message);

        Assert.Equal(["MoveNext:0", "Current:0", "Add:A", "MoveNext:1"], events);
    }

    [Fact]
    public void NumericSourcesAndInternalArrayContractRemainAvailable()
    {
        Assert.Equal(6L, PyOps.CallBuiltin("sum", [new object?[] { 1L, 2L, 3L }], [], []));
        Assert.Equal(30L, PyOps.CallBuiltin("sum", [new PyTuple([10L, 20L])], [], []));
        Assert.Equal(1.75, PyOps.CallBuiltin("sum", [new PyList([0.5, 1.25])], [], []));
        BigInteger large = BigInteger.One << 80;
        Assert.Equal(large + 2, PyOps.CallBuiltin("sum", [new PyList([large, 2L])], [], []));
    }

    [Fact]
    public void BuiltinSumParameterValidation()
    {
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("sum", [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("sum", [new PyList([]), 1L, 2L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("sum", [new PyList([])], ["invalid"], [1L]));
    }
}
