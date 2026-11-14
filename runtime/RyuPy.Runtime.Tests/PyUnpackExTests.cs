// 通用星号解包的双迭代、序列回退和精确容器合同；仅准备，尚未构建或执行。
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public partial class PyUnpackExTests
{
    private static PyTuple Unpack(object? source, long before = 1, long after = 1) =>
        Assert.IsType<PyTuple>(PyOps.UnpackEx(source, before, after));
    private static PyFunction Fn(string name, PyCall body) => new(
        name, PySignature.Create(name, ["self"], ["pos"], ["0"]), body, []);
    private static object Nullary(string name, Func<object?> body) =>
        PyNativeFunctions.Create(name, [], [], 0, _ => body());
    private static PyInstance User(Func<object?> next, Func<PyInstance, object?>? iter = null)
    {
        var cls = (PyClass)PyOps.MakeClass("StarCursor", [], null);
        cls.SetAttr("__iter__", Fn("__iter__", (_, args, _, _) =>
            iter is null ? args[0] : iter((PyInstance)args[0]!)));
        cls.SetAttr("__next__", Fn("__next__", (_, _, _, _) => next()));
        cls.SetAttr("close", Fn("close", (_, _, _, _) => throw new PyAssertionError("不得主动关闭")));
        return (PyInstance)cls.Invoke([], [], [])!;
    }
    private static PyInstance Values(params object?[] values)
    {
        int index = 0;
        return User(() => index < values.Length ? values[index++] : throw new PyStopIteration());
    }
    private static PyInstance Indexed(Func<long, object?> get)
    {
        var cls = (PyClass)PyOps.MakeClass("StarSequence", [], null);
        cls.SetAttr("__getitem__", new PyFunction("__getitem__",
            PySignature.Create("__getitem__", ["self", "index"], ["pos", "pos"], ["0", "0"]),
            (_, args, _, _) => get((long)args[1]!), []));
        return (PyInstance)cls.Invoke([], [], [])!;
    }
    private static void AssertParts(PyTuple result, object? first, object?[] middle, object? last)
    {
        Assert.Equal(3, result.Length);
        Assert.Equal(first, result.Items[0]);
        Assert.Equal(middle, Assert.IsType<PyList>(result.Items[1]).Items);
        Assert.Equal(last, result.Items[2]);
    }

    [Fact]
    public void SecondIteratorReadsReplacementButHintBelongsToFirst()
    {
        var events = new List<string>(); int firstIters = 0, firstNext = 0, tailNext = 0;
        var tail = User(() =>
        {
            events.Add($"I1.next:{tailNext}");
            return tailNext++ < 3 ? 60L + tailNext * 10 : throw new PyStopIteration();
        }, _ => throw new PyAssertionError("不得第三次获取I1"));
        tail.Class.SetAttr("__len__", Fn("__len__", (_, _, _, _) => throw new PyAssertionError("不能查询I1长度")));
        var first = User(() => { events.Add("I0.next"); firstNext++; return 10L; }, self =>
        { events.Add("I0.iter"); return ++firstIters == 1 ? self : tail; });
        first.Class.SetAttr("__length_hint__", Fn("__length_hint__", (_, _, _, _) =>
        { events.Add("I0.hint"); return 1L; }));
        AssertParts(Unpack(first), 10L, [70L, 80L], 90L);
        Assert.Equal(1, firstNext);
        Assert.Equal(new[] { "I0.iter", "I0.next", "I0.iter", "I0.hint", "I1.next:0", "I1.next:1", "I1.next:2", "I1.next:3" }, events);
    }

    [Fact]
    public void ExplicitInstanceIteratorIsUnwrappedAtInitialAcquisition()
    {
        int iters = 0, next = 0;
        var user = User(() => ++next <= 3 ? (object?)(long)next : throw new PyStopIteration(), self => { iters++; return self; });
        IPyIterator wrapper = PyIter.Create(user);
        Assert.Equal(1, iters);
        AssertParts(Unpack(wrapper), 1L, [2L], 3L);
        Assert.Equal(3, iters); Assert.Equal(4, next);
    }

    [Fact]
    public void DistinctSourceLengthAndInstanceShadowsAreIgnored()
    {
        var iterator = Values(1L, 2L, 3L); int hints = 0;
        iterator.Class.SetAttr("__length_hint__", Fn("__length_hint__", (_, _, _, _) => { hints++; return 0L; }));
        PyObject.SetAttr(iterator, "__length_hint__", Nullary("shadow", () => throw new PyAssertionError("实例影子")));
        var source = User(() => throw new PyAssertionError("源不是游标"), _ => iterator);
        source.Class.SetAttr("__len__", Fn("__len__", (_, _, _, _) => throw new PyAssertionError("不能查询源长度")));
        AssertParts(Unpack(source), 1L, [2L], 3L); Assert.Equal(1, hints);
    }

    [Fact]
    public void PrefixShortageSkipsSecondIterAndHintButSuffixShortageConsumesTail()
    {
        int iters = 0, calls = 0, hints = 0;
        var prefix = User(() => ++calls == 1 ? 1L : throw new PyStopIteration(), self => { iters++; return self; });
        prefix.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => { hints++; return 0L; }));
        Assert.Equal("not enough values to unpack (expected at least 3, got 1)", Assert.Throws<PyValueError>(() => Unpack(prefix, 2, 1)).PyMessage);
        Assert.Equal(1, iters); Assert.Equal(2, calls); Assert.Equal(0, hints);
        Assert.Equal("not enough values to unpack (expected at least 3, got 2)", Assert.Throws<PyValueError>(() => Unpack(Values(1L, 2L), 1, 2)).PyMessage);
        Assert.Empty(Assert.IsType<PyList>(Unpack(Values(), 0, 0).Items[0]).Items);
        Assert.Empty(Assert.IsType<PyList>(Unpack(Values(1L), 1, 0).Items[1]).Items);
    }

    [Fact]
    public void ExactListKeepsNumericBackingAndFreezesAllAliases()
    {
        var source = Assert.IsType<PyList>(PyNumericListOps.Create());
        foreach (double value in new[] { 1234.5, double.NaN, 9999.5 }) PyNumericListOps.AppendF64(source, value);
        object? first = source.GetItem(0), middleValue = source.GetItem(1), last = source.GetItem(2);
        var result = Unpack(source); var middle = Assert.IsType<PyList>(result.Items[1]);
        Assert.Null(typeof(PyList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source));
        Assert.NotSame(source, middle); Assert.Same(first, result.Items[0]);
        Assert.Same(middleValue, middle.GetItem(0)); Assert.Same(last, result.Items[2]);
        source.SetItem(2, 0L); middle.Items.Clear();
        Assert.Same(last, result.Items[2]);
        var alias = new PyList([]); var tuple = new PyTuple([alias, alias, alias]);
        var frozen = Unpack(tuple); var star = Assert.IsType<PyList>(frozen.Items[1]);
        Assert.Same(alias, frozen.Items[0]); Assert.Same(alias, star.GetItem(0)); Assert.Same(alias, frozen.Items[2]);
        Assert.NotSame(star, Unpack(tuple).Items[1]);
        AssertParts(Unpack("A😀B"), "A", ["😀"], "B");
        var textClass = (PyClass)PyOps.MakeClass("StarText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(textClass);
        object text = textClass.Invoke(["A😀B"], [], [])!;
        var textResult = Unpack(text);
        AssertParts(textResult, "A", ["😀"], "B");
        Assert.IsType<string>(textResult.Items[0]);
        Assert.IsType<string>(Assert.IsType<PyList>(textResult.Items[1]).GetItem(0));
        Assert.IsType<string>(textResult.Items[2]);
        // CLR计数器保持int；Python协议产生的数值明确用long，不能靠相等比较掩盖盒类型。
        var numeric = Unpack(Values(1L, 2L, 3L));
        Assert.IsType<long>(numeric.Items[0]);
        Assert.IsType<long>(Assert.IsType<PyList>(numeric.Items[1]).GetItem(0));
        Assert.IsType<long>(numeric.Items[2]);
    }

    [Fact]
    public void GetItemFallbackReadsSourceLenAndNeverSourceHint()
    {
        foreach (string mode in new[] { "missing", "valid", "type", "value" })
        {
            var events = new List<string>(); var failure = new PyValueError("len");
            var source = Indexed(index => { events.Add($"get:{index}"); return index < 3 ? index + 10 : throw new PyIndexError("end"); });
            source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => throw new PyAssertionError("不能调用源hint")));
            if (mode != "missing") source.Class.SetAttr("__len__", Fn("len", (_, _, _, _) =>
            { events.Add("len"); return mode == "type" ? throw new PyTypeError("ignored") : mode == "value" ? throw failure : 3L; }));
            if (mode == "value")
            { Assert.Same(failure, Assert.Throws<PyValueError>(() => Unpack(source))); Assert.Equal(new[] { "get:0", "len" }, events); }
            else
            { AssertParts(Unpack(source), 10L, [11L], 12L); Assert.Equal(mode == "missing" ? new[] { "get:0", "get:1", "get:2", "get:3" } : new[] { "get:0", "len", "get:1", "get:2", "get:3" }, events); }
        }
    }
}
