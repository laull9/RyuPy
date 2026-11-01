using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>映射百分号格式保留slot资格、逐键转换顺序与未消费参数边界。</summary>
public class PyPercentMappingTests
{
    private static PyFunction Function(string name, string[] names, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, new object?[names.Length], names.Length, body);

    [Fact]
    public void UnusedMappingDoesNotInvokeGetItemAndNullSlotStillCounts()
    {
        var cls = new PyClass("Mapping");
        cls.SetAttr("__getitem__", Function("__getitem__", ["self", "key"], _ => throw new PyAssertionError("不应取键")));
        var value = new PyInstance(cls);
        Assert.Equal("plain%", PyPercentFormat.Format("plain%%", value));
        cls.SetAttr("__getitem__", PyNone.Instance);
        Assert.Equal("plain", PyPercentFormat.Format("plain", value));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%(x)s", value));
        object? deque = PyOps.CallValue(PyCollections.CreateAttributes()["deque"], [], [], []);
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("plain", deque));
    }

    [Fact]
    public void EachKeyIsReloadedAfterPreviousConversionMutatesMapping()
    {
        var events = new List<string>();
        var mappingClass = new PyClass("Mapping");
        var resultClass = new PyClass("Result");
        long current = 1;
        resultClass.SetAttr("__str__", Function("__str__", ["self"], _ => { events.Add("str"); current = 9; return "first"; }));
        mappingClass.SetAttr("__getitem__", Function("__getitem__", ["self", "key"], args =>
        {
            string key = Assert.IsType<string>(args[1]); events.Add(key);
            return key == "first" ? new PyInstance(resultClass) : current;
        }));
        Assert.Equal("first/9/9", PyPercentFormat.Format("%(first)s/%(next)d/%(next)d", new PyInstance(mappingClass)));
        Assert.Equal(new[] { "first", "str", "next", "next" }, events);
    }

    [Fact]
    public void CompleteKeyIsReadBeforeConversionErrorsButIncompleteKeyIsNotRead()
    {
        var events = new List<string>();
        var cls = new PyClass("Mapping");
        cls.SetAttr("__getitem__", Function("__getitem__", ["self", "key"], args =>
        {
            string key = Assert.IsType<string>(args[1]); events.Add(key);
            if (key == "missing") throw new PyKeyError("missing");
            return 4L;
        }));
        var value = new PyInstance(cls);
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%(unfinished", value));
        Assert.Empty(events);
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%(x)", value));
        Assert.Throws<PyValueError>(() => PyPercentFormat.Format("%(x)q", value));
        Assert.Throws<PyKeyError>(() => PyPercentFormat.Format("%(missing)q", value));
        Assert.Equal(new[] { "x", "x", "missing" }, events);
        Assert.Equal("4/4", PyPercentFormat.Format("%((nested))d/%()d", value));
        Assert.Equal("(nested)", events[^2]); Assert.Equal("", events[^1]);
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%(", 1L));
    }

    [Fact]
    public void MappingSlotEligibilityIsCapturedBeforeStringConversion()
    {
        var cls = new PyClass("InitiallyNotMapping");
        cls.SetAttr("__str__", Function("__str__", ["self"], _ =>
        {
            cls.SetAttr("__getitem__", Function("__getitem__", ["self", "key"], _ => 9L));
            return "value";
        }));
        var value = new PyInstance(cls);
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%s/%(x)s", value));
        // 下一次调用入口能看到新slot，但同一调用中不可追溯改变资格。
        Assert.Equal("value/9", PyPercentFormat.Format("%s/%(x)s", value));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%(x)s/%s", value));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%(x)s", new PyTuple([value])));
    }

    private sealed class UnknownNative : PyNativeObject
    {
        public override string TypeName => "unknown_native";
        public override object? GetAttr(string name) => throw new PyAssertionError("不能靠执行属性探测mapping slot");
        public override string PythonStr() => "native";
    }

    [Fact]
    public void UnknownNativeSlotDoesNotBroadenMappingOrBreakExistingStringConversion()
    {
        var value = new UnknownNative();
        Assert.Equal("native", PyPercentFormat.Format("%s", value));
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format("plain", value));
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.Format("%(x)s", value));
    }
}
