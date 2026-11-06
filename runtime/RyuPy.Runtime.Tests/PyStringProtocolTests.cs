// 通过公开入口锁真实字符串载体、默认协议与根方法；不靠反射读取私有payload。
using System;
using System.Linq;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringProtocolTests
{
    private static PyClass Make(string name = "Text")
    {
        var cls = (PyClass)PyOps.MakeClass(name, [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static object New(PyClass cls, string value) => cls.Invoke([value], [], [])!;
    private static PyFunction Method(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static object? Call(object target, string name, params object?[] args) => PyOps.CallMethod(target, name, args, [], []);
    private static object? Root(string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), name), args, [], []);
    private static object? Builtin(string name, params object?[] args) => PyOps.CallBuiltin(name, args, [], []);

    [Fact]
    public void DefaultSequenceUsesCodePointsAndReturnsBaseText()
    {
        var cls = Make(); object value = New(cls, "a😀b");
        Assert.Equal(3L, PyOps.Len(value)); Assert.True(PyOps.Truthy(value)); Assert.False(PyOps.Truthy(New(cls, "")));
        Assert.Equal("😀", PyOps.GetItem(value, 1L)); Assert.Equal("b", PyOps.GetItem(value, -1L));
        Assert.IsType<string>(PyOps.GetItem(value, new PySlice(0L, 2L, PyNone.Instance)));
        Assert.Equal("a😀", PyOps.GetItem(value, new PySlice(0L, 2L, PyNone.Instance)));
        var iterator = PyIter.Create(value);
        foreach (string expected in new[] { "a", "😀", "b" }) { Assert.True(iterator.MoveNext()); Assert.Equal(expected, iterator.Current()); }
        Assert.False(iterator.MoveNext()); Assert.Throws<PyIndexError>(() => PyOps.GetItem(value, 3L));
    }

    [Fact]
    public void MixedOperatorsUseRawPayloadWithoutStringConversion()
    {
        var cls = Make(); int calls = 0;
        cls.SetAttr("__str__", Method("str", _ => { calls++; return "wrong"; }));
        object value = New(cls, "ab"); object needle = New(cls, "b");
        Assert.True(PyOps.Contains(value, needle)); Assert.True(PyOps.Contains("abc", value));
        Assert.Equal("abc", PyOps.Binary(PyOps.Add, value, "c")); Assert.Equal("cab", PyOps.Binary(PyOps.Add, "c", value));
        Assert.Equal("abab", PyOps.Binary(PyOps.Mul, value, 2L)); Assert.Equal("abab", PyOps.Binary(PyOps.Mul, 2L, value));
        Assert.Equal("", PyOps.Binary(PyOps.Mul, value, -1L)); Assert.Equal(0, calls);
        long depth = PyTraceback.PushFrame("string_test.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            PyTraceback.SetLine(37);
            PyBaseException[] errors = [Assert.Throws<PyTypeError>(() => PyOps.Contains(value, 1L)),
                Assert.Throws<PyIndexError>(() => PyOps.GetItem(value, 7L)),
                Assert.Throws<PyTypeError>(() => PyOps.Binary(PyOps.Mul, value, 1.5))];
            foreach (var error in errors)
            {
                Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("string_test.py", "caller", 37), error.Frames[0]);
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void EqualTextSharesDictionaryAndSetKeySemantics()
    {
        object first = New(Make("First"), "same"); object second = New(Make("Second"), "same");
        Assert.True(PyOps.Compare(PyOps.Eq, first, "same")); Assert.True(PyOps.Compare(PyOps.Eq, "same", second));
        Assert.Equal(Builtin("hash", "same"), Builtin("hash", first)); Assert.Equal(Builtin("hash", first), Builtin("hash", second));
        var dict = new PyDict(); dict.Set(first, 1L); dict.Set("same", 2L); dict.Set(second, 3L);
        Assert.Equal(1, dict.Count); Assert.Equal(3L, dict.Get("same")); Assert.Same(first, dict.Keys.Single());
        Assert.Equal(1L, PyOps.Len(PyOps.MakeSet([first, "same", second])));
        Assert.True(PyOps.Compare(PyOps.Lt, New(Make(), "a"), "b"));
        // 只锁表示一致性，不把现UTF16旧序误称CP码点排序通过。
        object bmp = New(Make(), "\uE000"), astral = New(Make(), "😀");
        foreach (int op in new[] { PyOps.Lt, PyOps.Le, PyOps.Gt, PyOps.Ge })
        {
            bool exact = PyOps.Compare(op, "\uE000", "😀");
            Assert.Equal(exact, PyOps.Compare(op, bmp, "😀"));
            Assert.Equal(exact, PyOps.Compare(op, "\uE000", astral));
            Assert.Equal(exact, PyOps.Compare(op, bmp, astral));
        }
        Assert.Equal(PyOps.Compare(PyOps.Lt, "\uE000", "😀"), Root("__lt__", bmp, "😀"));
        object[] triple = ["😁", "\uE000", astral];
        for (int a = 0; a < 3; a++)
            for (int b = 0; b < 3; b++)
                for (int c = 0; c < 3; c++)
                    Assert.False(PyOps.Compare(PyOps.Lt, triple[a], triple[b])
                        && PyOps.Compare(PyOps.Lt, triple[b], triple[c])
                        && PyOps.Compare(PyOps.Lt, triple[c], triple[a]));
    }

    [Fact]
    public void OrdinaryMethodsAcceptRawSubtypeParametersAndBaseResults()
    {
        var cls = Make(); int calls = 0; cls.SetAttr("__str__", Method("str", _ => { calls++; return "wrong"; }));
        object text = New(cls, "a,b,a"), comma = New(cls, ",");
        Assert.Equal(new object?[] { "a", "b", "a" }, Assert.IsType<PyList>(Call(text, "split", comma)).Items);
        Assert.Equal("a-b", Assert.IsType<string>(Call(New(cls, "-"), "join", new PyList([New(cls, "a"), New(cls, "b")]))));
        Assert.Equal("a,b", Call(",", "join", new PyList([New(cls, "a"), New(cls, "b")])));
        Assert.Equal("X,b,X", Call(text, "replace", New(cls, "a"), New(cls, "X")));
        Assert.Equal(true, Call(text, "startswith", New(cls, "a"))); Assert.Equal(0, calls);
    }

    [Fact]
    public void RootAndSavedMethodsBypassLaterOrdinaryOverrides()
    {
        var cls = Make(); object value = New(cls, "ab"); object? saved = PyObject.GetAttr(value, "upper");
        Assert.Same(value, PyObject.GetAttr(saved, "__self__"));
        for (int i = 0; i < 3; i++) Assert.Equal("AB", PyPic.CallMethod0(3881, value, "upper"));
        cls.SetAttr("upper", Method("upper", _ => "changed"));
        Assert.Equal("changed", PyPic.CallMethod0(3881, value, "upper")); Assert.Equal("AB", PyOps.CallValue(saved, [], [], []));
        Assert.Equal("AB", Root("upper", value));
        Assert.Equal("AB", PyPic.CallMethod1(3882, PyType.Get("str"), "upper", value));
        Assert.Equal("AB", Call(PyType.Get("str"), "upper", value));
        object other = New(Make("Other"), "cd"); Assert.Equal("CD", PyPic.CallMethod0(3881, other, "upper"));
        Assert.Equal("changed", PyPic.CallMethod0(3881, value, "upper"));
    }

    [Fact]
    public void PropertyAndInstanceOverridesDoNotAlterExplicitRoots()
    {
        var cls = Make(); object value = New(cls, "ab");
        PyObject.SetAttr(value, "upper", Method("instance", _ => "instance"));
        Assert.Equal("instance", Call(value, "upper")); Assert.Equal("AB", Root("upper", value));
        cls.SetAttr("upper", new PyProperty(Method("property", _ => Method("result", _ => "property"))));
        Assert.Equal("property", Call(value, "upper")); Assert.Equal("AB", Root("upper", value));
        Assert.Equal("AB", Call(new PySuper(cls, value), "upper"));
    }

    [Fact]
    public void RootDescriptorsValidateReceiverAndArguments()
    {
        object value = New(Make(), "ab");
        Assert.Equal("ab", Root("__str__", value)); Assert.Equal("'ab'", Root("__repr__", value));
        Assert.Equal("b", Root("__getitem__", value, 1L)); Assert.Equal("abab", Root("__mul__", value, 2L));
        Assert.Same(PyNotImplemented.Instance, Root("__eq__", value, 1L));
        Assert.Throws<PyTypeError>(() => Root("upper", 1L)); Assert.Throws<PyTypeError>(() => Root("upper"));
        Assert.Throws<PyTypeError>(() => Root("upper", value, 1L));
    }

    [Fact]
    public void RootIndexCallbacksRunOnceAndDoNotUseTextConversion()
    {
        var indexClass = new PyClass("Index"); int calls = 0;
        indexClass.SetAttr("__index__", Method("index", _ => { calls++; return 1L; }));
        object index = new PyInstance(indexClass), value = New(Make(), "ab");
        Assert.Equal("b", Root("__getitem__", value, index)); Assert.Equal(1, calls);
        Assert.Equal("ab", Root("__mul__", value, index)); Assert.Equal(2, calls);
        indexClass.SetAttr("__index__", Method("index", _ => true));
        Assert.Throws<PyNotImplementedError>(() => Root("__getitem__", value, index));
        Assert.Throws<PyNotImplementedError>(() => Root("__mul__", value, index));
        var failure = new PyValueError("index body"); PyErrors.PrepareRaise(failure);
        var frames = failure.Frames; var context = failure.Context;
        indexClass.SetAttr("__index__", Method("index", _ => throw failure));
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Root("__getitem__", value, index)));
        Assert.Same(frames, failure.Frames); Assert.Same(context, failure.Context);
    }

    [Fact]
    public void NumericConsumersRespectSlotsAndExplicitBaseReadsRaw()
    {
        var cls = Make(); int integers = 0, floats = 0;
        cls.SetAttr("__int__", Method("int", _ => { integers++; return 7L; }));
        cls.SetAttr("__float__", Method("float", _ => { floats++; return 1.25; }));
        object value = New(cls, "10");
        Assert.Equal(7L, PyIntConstructor.Invoke([value], [], [])); Assert.Equal(16L, PyIntConstructor.Invoke([value, 16L], [], []));
        Assert.Equal(1.25, PyFloatConstructor.Invoke([value], [], [])); Assert.Equal(1, integers); Assert.Equal(1, floats);
        Assert.Equal(65L, Builtin("ord", New(cls, "A"))); Assert.Equal(1, integers);
        var error = new PyValueError("same"); cls.SetAttr("__int__", Method("int", _ => throw error));
        Assert.Same(error, Assert.Throws<PyValueError>(() => PyIntConstructor.Invoke([value], [], [])));
    }
}
