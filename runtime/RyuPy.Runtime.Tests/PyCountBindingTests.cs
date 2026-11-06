// count 公开构造合同：从模块工厂进入，保绑定先序与有限资格边界，不靠内部构造器绕过公开 ABI。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyCountBindingTests
{
    private static object Make(object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyItertools.CreateAttributes()["count"], args, names ?? [], values ?? [])!;
    private static object? Next(object value) => PyOps.CallBuiltin("next", [value], [], []);
    private static void Error(string message, object?[] args, object?[]? names = null, object?[]? values = null) =>
        Assert.Equal(message, Assert.Throws<PyTypeError>(() => Make(args, names, values)).PyMessage);

    [Fact]
    public void DefaultPositionalKeywordAndExpandedCallsReachTheSameFactory()
    {
        Assert.Equal(0L, Next(Make([])));
        Assert.Equal(5L, Next(Make([5L])));
        object value = Make([], ["step"], [2L]);
        Assert.Equal(0L, Next(value)); Assert.Equal(2L, Next(value));
        value = Make([], ["step", "start"], [-2L, 5L]);
        Assert.Equal(5L, Next(value)); Assert.Equal(3L, Next(value));
        value = Make([5L], ["step"], [0L]);
        Assert.Equal(5L, Next(value)); Assert.Equal(5L, Next(value));
        var keywords = new PyDict(); keywords.Set("step", 3L);
        value = Make([PyOps.StarArg(new PyList([7L]))], [PyNone.Instance], [keywords]);
        Assert.Equal(7L, Next(value)); Assert.Equal(10L, Next(value));
    }

    [Fact]
    public void TotalArityWinsOverBindingAndNumberErrors()
    {
        Error("count() takes at most 2 arguments (3 given)", [1L, 2L, 3L]);
        Error("count() takes at most 2 arguments (4 given)", [1L, 2L, 3L], ["bad"], [4L]);
        Error("count() takes at most 2 arguments (3 given)", [1L], ["start", "bad"], [2L, 3L]);
        Error("count() takes at most 2 arguments (3 given)", [PyNone.Instance, 1L], ["step"], [2L]);
        Error("count() takes at most 2 keyword arguments (3 given)", [], ["bad", "start", "step"], [1L, null, null]);
        Error("count() takes at most 2 keyword arguments (3 given)", [], ["step", "start", "bad"], [null, null, 1L]);
        Error("count() takes at most 2 keyword arguments (4 given)", [], ["bad", "worse", "start", "step"], [1L, 2L, null, null]);
        Error("count() takes at most 2 arguments (4 given)", [1L], ["bad", "start", "step"], [2L, null, null]);
    }

    [Fact]
    public void DuplicatePositionAndUnknownNamesWinBeforeNumericChecks()
    {
        Error("argument for count() given by name ('start') and position (1)", [1L], ["start"], [2L]);
        Error("argument for count() given by name ('start') and position (1)", [PyNone.Instance], ["start"], [2L]);
        Error("'bad' is an invalid keyword argument for count()", [PyNone.Instance], ["bad"], [3L]);
        Error("'bad' is an invalid keyword argument for count()", [], ["step", "bad"], [PyNone.Instance, 3L]);
        Error("'bad' is an invalid keyword argument for count()", [], ["bad", "step"], [3L, PyNone.Instance]);
        Error("'bad' is an invalid keyword argument for count()", [], ["bad", "worse"], [3L, 4L]);
        Error("'worse' is an invalid keyword argument for count()", [], ["worse", "bad"], [4L, 3L]);
    }

    [Fact]
    public void ExactKnownNonnumbersRejectBothSlotsWithoutCoercion()
    {
        object?[] invalid = [null, PyNone.Instance, "4", new PyBytes([52]), new PyByteArray([52]),
            new PyList([]), new PyTuple([]), new PyDict(), new PySet([]), new PyFrozenSet([]),
            new PySlice(null, 3L, null), new PyRange(0, 3, 1), PyBaseObject.Create([], []), PyType.Get("int")];
        foreach (object? value in invalid)
        {
            Error("a number is required", [value]);
            Error("a number is required", [0L, value]);
        }
        Assert.Equal(42L, Next(Make([new BigInteger(42), BigInteger.One])));
        Assert.Equal(42L, Next(Make([42, 1])));
    }

    [Fact]
    public void UnknownUsersAndFakeNativeNamesAreExplicitlyUnsupportedWithoutCallbacks()
    {
        int calls = 0;
        var cls = new PyClass("NumberCandidate");
        foreach (string slot in new[] { "__int__", "__index__", "__float__", "__add__", "__repr__", "__bool__" })
            cls.SetAttr(slot, PyNativeFunctions.CreateVariadic(slot, _ => { calls++; throw new InvalidOperationException("用户协议不应执行"); }));
        var descriptor = new PyClass("Descriptor");
        descriptor.SetAttr("__get__", PyNativeFunctions.CreateVariadic("__get__", _ => { calls++; throw new InvalidOperationException("descriptor 不应执行"); }));
        var customDescriptor = new PyClass("WithDescriptor"); customDescriptor.SetAttr("__index__", new PyInstance(descriptor));
        object[] unknown = [new PyInstance(cls), new PyInstance(new PyClass("Plain")),
            new PyInstance(customDescriptor), new NativePretender(), new TuplePretender()];
        foreach (object value in unknown)
        {
            Assert.Equal("count: custom numeric protocol is not supported",
                Assert.Throws<PyNotImplementedError>(() => Make([value])).PyMessage);
            Assert.Throws<PyNotImplementedError>(() => Make([0L, value]));
            Error("'bad' is an invalid keyword argument for count()", [value], ["bad"], [1L]);
        }
        Assert.Equal(0, calls);
    }

    private sealed class NativePretender : PyNativeObject
    {
        public override string TypeName => "int";
        public override object? GetAttr(string name) => throw new InvalidOperationException("不得按名字探测数值协议");
    }
    private sealed class TuplePretender() : PyTuple([]) { }
}
