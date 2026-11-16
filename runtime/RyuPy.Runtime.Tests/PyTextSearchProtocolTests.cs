// 搜索冷槽协议保护；共用槽抽取另由原expandtabs完整矩阵保护，搜索保自己的转换与错误。
using System.Numerics;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextSearchProtocolTests
{
    private static PyFunction Fn(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("search_slot", body);
    private static PyInstance Slot(object? raw)
    {
        var cls = new PyClass("SearchIndex"); cls.SetAttr("__index__", raw); return new PyInstance(cls);
    }
    private static object? Search(object? start) => PyOps.CallMethod("abc", "find", ["a", start], [], []);

    [Fact]
    public void TypeLookupIgnoresInstanceAndCallsOrdinaryStaticClassAndPropertyOnce()
    {
        int calls = 0; var value = Slot(Fn(_ => { calls++; return 0L; }));
        PyObject.SetAttr(value, "__index__", Fn(_ => throw new PyValueError("instance")));
        Assert.Equal(0L, Search(value)); Assert.Equal(1, calls);
        value.Class.SetAttr("__index__", new PyStaticMethod(Fn(args => { Assert.Empty(args); calls++; return 0L; })));
        Assert.Equal(0L, Search(value)); Assert.Equal(2, calls);
        value.Class.SetAttr("__index__", new PyClassMethod(Fn(args =>
        {
            Assert.Single(args); Assert.Same(value.Class, args[0]); calls++; return 0L;
        })));
        Assert.Equal(0L, Search(value)); Assert.Equal(3, calls);
        int gets = 0;
        value.Class.SetAttr("__index__", new PyProperty(Fn(args =>
        {
            Assert.Single(args); Assert.Same(value, args[0]); gets++;
            return Fn(call => { Assert.Empty(call); calls++; return 0L; });
        })));
        Assert.Equal(0L, Search(value)); Assert.Equal(4, calls); Assert.Equal(1, gets);
    }

    [Fact]
    public void RawDescriptorReceivesThreeArgumentsAndActualClassWithoutSecondBinding()
    {
        var descriptorClass = new PyClass("Descriptor"); var descriptor = new PyInstance(descriptorClass);
        var parent = new PyClass("Parent"); parent.SetAttr("__index__", descriptor);
        var child = (PyClass)PyOps.MakeClass("Child", [parent], null); var value = new PyInstance(child);
        int gets = 0, calls = 0;
        PyFunction get = Fn(args =>
        {
            Assert.Equal(3, args.Length); Assert.Same(descriptor, args[0]); Assert.Same(value, args[1]); Assert.Same(child, args[2]);
            gets++; return Fn(call => { Assert.Empty(call); calls++; return 0L; });
        });
        descriptorClass.SetAttr("__get__", get);
        PyObject.SetAttr(descriptor, "__get__", Fn(_ => throw new PyValueError("instance get")));
        Assert.Equal(0L, Search(value)); Assert.Equal(1, gets); Assert.Equal(1, calls);
        descriptorClass.SetAttr("__get__", new PyStaticMethod(get));
        Assert.Equal(0L, Search(value)); Assert.Equal(2, gets); Assert.Equal(2, calls);
        descriptorClass.SetAttr("__get__", new PyClassMethod(get));
        Assert.Equal("'classmethod' object is not callable", Assert.Throws<PyTypeError>(() => Search(value)).PyMessage);
        Assert.Equal(2, gets); Assert.Equal(2, calls);
    }

    [Fact]
    public void MissingNonCallableAndBadReturnAreDistinctAndWideIntegersSaturate()
    {
        Assert.Equal("slice indices must be integers or None or have an __index__ method",
            Assert.Throws<PyTypeError>(() => Search(new PyInstance(new PyClass("Missing")))).PyMessage);
        Assert.Equal("'NoneType' object is not callable", Assert.Throws<PyTypeError>(() => Search(Slot(PyNone.Instance))).PyMessage);
        Assert.Equal("'int' object is not callable", Assert.Throws<PyTypeError>(() => Search(Slot(0L))).PyMessage);
        Assert.Equal("__index__ returned non-int (type float)", Assert.Throws<PyTypeError>(() => Search(Slot(Fn(_ => 1.5)))).PyMessage);
        int calls = 0; BigInteger huge = BigInteger.One << 100;
        Assert.Equal(-1L, Search(Slot(Fn(_ => { calls++; return huge; }))));
        Assert.Equal(0L, Search(Slot(Fn(_ => { calls++; return -huge; }))));
        Assert.Equal(2, calls);
        // 同一槽返回值仍由expandtabs按C-int溢出检查，搜索不放宽旧API。
        Assert.Equal("Python int too large to convert to C int",
            Assert.Throws<PyOverflowError>(() => PyOps.CallMethod("", "expandtabs", [Slot(Fn(_ => huge))], [], [])).PyMessage);
    }

    [Fact]
    public void GetterAndIndexExceptionsKeepIdentityFramesCauseAndContext()
    {
        long depth = PyTraceback.PushFrame("search_identity.py", "caller");
        var marker = new PyValueError("marker"); var cause = new PyKeyError("cause"); marker.WithCause(cause);
        PyTraceback.SetLine(7); PyErrors.PrepareRaise(marker); var frames = marker.Frames;
        var outer = new PyRuntimeError("outer"); object? token = PyErrors.EnterHandler(outer);
        try
        {
            PyTraceback.SetLine(41);
            var descriptorClass = new PyClass("ThrowDescriptor"); var descriptor = new PyInstance(descriptorClass);
            descriptorClass.SetAttr("__get__", Fn(_ => throw marker));
            foreach (object value in new object[] { Slot(Fn(_ => throw marker)), Slot(new PyProperty(Fn(_ => throw marker))), Slot(descriptor) })
                Assert.Same(marker, Assert.Throws<PyValueError>(() => Search(value)));
            Assert.Same(frames, marker.Frames); Assert.Same(cause, marker.Cause); Assert.Null(marker.Context); Assert.True(marker.SuppressContextFlag);
            var owned = Assert.Throws<PyTypeError>(() => Search(Slot(Fn(_ => "bad"))));
            Assert.Same(outer, owned.Context); Assert.Equal(new PyStackFrame("search_identity.py", "caller", 41), owned.Frames[0]);
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void NativeAndPythonExceptionCarriersKeepTheirExistingSlotDomain()
    {
        var native = new NativeIndex(); Assert.Equal(0L, Search(native)); Assert.Equal(1, native.Lookups); Assert.Equal(1, native.Calls);
        var cls = new PyClass("ExceptionIndex"); var exception = new PyValueError("payload"); exception.BindPythonClass(cls);
        cls.SetAttr("__index__", Fn(args => { Assert.Single(args); Assert.Same(exception, args[0]); return 0L; }));
        Assert.Equal(0L, Search(exception));
    }

    private sealed class NativeIndex : PyNativeObject
    {
        internal int Lookups, Calls;
        public override string TypeName => "NativeIndex";
        public override object? GetAttr(string name)
        {
            if (name != "__index__") throw Missing(name);
            Lookups++; return Fn(args => { Assert.Empty(args); Calls++; return 0L; });
        }
    }
}
