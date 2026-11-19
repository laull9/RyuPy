// translate的有限buffer合同：公共分派、全值域、别名活读、精确错误与无泄漏；CP3.12.13原流另存验证目录。
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBinaryTranslateTests
{
    private static PyBytes B(string text) => new(Encoding.Latin1.GetBytes(text));
    private static byte[] All() => Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
    private static object Receiver(bool mutable, byte[] data) => mutable ? new PyByteArray(data) : new PyBytes(data);
    private static byte[] Data(object? value) => value switch
    {
        PyBytes bytes => bytes.Bytes, PyByteArray array => array.Bytes.ToArray(),
        _ => throw new InvalidOperationException("unexpected carrier"),
    };
    private static object? Route(int route, object value, object?[] args, object?[] names, object?[] values)
    {
        object owner = PyObject.TypeOf(value);
        return route switch
        {
            0 => PyOps.CallMethod(value, "translate", args, names, values),
            1 => PyOps.CallValue(PyObject.GetAttr(value, "translate"), args, names, values),
            2 => PyOps.CallMethod(owner, "translate", [value, .. args], names, values),
            _ => PyOps.CallValue(PyObject.GetAttr(owner, "translate"), [value, .. args], names, values),
        };
    }
    private static object? Call(object value, object? table, object? delete) => Route(0, value, [table, delete], [], []);
    private static PyMemoryView View(object owner) => (PyMemoryView)PyMemoryView.Create([owner], [], []);

    [Fact]
    public void All256ValuesMapAndDeleteOriginalBytesAcrossBothCarriersAndFourRoutes()
    {
        var table = new PyBytes(All().Reverse().ToArray());
        var delete = new PyBytes(All().Where(value => value % 3 == 0).ToArray());
        byte[] expected = All().Where(value => value % 3 != 0).Select(value => (byte)(255 - value)).ToArray();
        foreach (bool mutable in new[] { false, true })
            for (int route = 0; route < 4; route++)
            {
                object source = Receiver(mutable, All());
                Assert.Equal(expected, Data(Route(route, source, [table, delete], [], [])));
                Assert.Equal(All(), Data(source));
                Assert.Equal(expected, Data(Route(route, source, [table], ["delete"], [delete])));
            }
    }

    [Fact]
    public void DeletedOriginalBytesAreNotConfusedWithMappedValuesOrNul()
    {
        var table = new PyByteArray(All()); table.SetItem(97L, 98L); table.SetItem(99L, 0L);
        foreach (bool mutable in new[] { false, true })
        {
            object source = Receiver(mutable, B("abcab").Bytes);
            Assert.Equal(new byte[] { 98, 0, 98 }, Data(Call(source, table, B("bb"))));
            Assert.Equal(B("b").Bytes, Data(Call(Receiver(mutable, B("abc").Bytes), PyNone.Instance, B("ac"))));
        }
    }

    [Fact]
    public void BytesWithoutActualChangesKeepIdentityEvenForEmptyOrSingleByte()
    {
        foreach (string text in new[] { "", "a", "abc\0\xff" })
            foreach (object table in new object[] { PyNone.Instance, new PyBytes(All()), new PyByteArray(All()) })
                for (int route = 0; route < 4; route++)
                {
                    var value = B(text);
                    Assert.Same(value, Route(route, value, [table], [], []));
                    Assert.Same(value, Route(route, value, [table], ["delete"], [B("XYZ")]));
                }
    }

    [Fact]
    public void BytearrayAlwaysReturnsFreshIndependentStorageIncludingEmpty()
    {
        foreach (string text in new[] { "", "a", "abc" })
        {
            var source = new PyByteArray(B(text).Bytes);
            var first = Assert.IsType<PyByteArray>(Call(source, PyNone.Instance, B("")));
            var second = Assert.IsType<PyByteArray>(Call(source, new PyBytes(All()), B("")));
            Assert.NotSame(source, first); Assert.NotSame(first, second);
            first.Append(90L); source.Append(88L);
            Assert.Equal(B(text).Bytes, second.Bytes);
            Assert.Equal(B(text + "Z").Bytes, first.Bytes);
            Assert.Equal(B(text + "X").Bytes, source.Bytes);
        }
    }

    [Fact]
    public void ChangedBytesDoNotReuseSourceOrShareMutableTableStorage()
    {
        var source = B("a"); var table = new PyByteArray(All()); table.SetItem(97L, 98L);
        var result = Assert.IsType<PyBytes>(Call(source, table, B("")));
        Assert.NotSame(source, result); Assert.Equal(B("b").Bytes, result.Bytes);
        table.SetItem(97L, 99L); Assert.Equal(B("b").Bytes, result.Bytes);
        Assert.Equal(B("c").Bytes, Data(Call(source, table, B(""))));
    }

    [Fact]
    public void SourceTableAndDeleteMayAllAliasWithoutMutation()
    {
        foreach (bool mutable in new[] { false, true })
        {
            object source = Receiver(mutable, All());
            Assert.Equal(All(), Data(Call(source, source, B(""))));
            Assert.Empty(Data(Call(source, source, source)));
            Assert.Equal(All(), Data(source));
            if (source is PyByteArray array) array.Append(7L);
        }
    }

    [Fact]
    public void SameLengthTableAndDeleteEditsAreReadOnEverySavedCall()
    {
        var source = new PyByteArray(B("abc").Bytes); var table = new PyByteArray(All());
        var deleted = new PyByteArray(B("c").Bytes); object saved = PyObject.GetAttr(source, "translate")!;
        Assert.Equal(B("ab").Bytes, Data(PyOps.CallValue(saved, [table, deleted], [], [])));
        table.SetItem(97L, 90L); deleted.SetItem(0L, 98L);
        Assert.Equal(B("Zc").Bytes, Data(PyOps.CallValue(saved, [table, deleted], [], [])));
        source.Append(97L);
        Assert.Equal(B("ZcZ").Bytes, Data(PyOps.CallValue(saved, [table, deleted], [], [])));
    }

    [Fact]
    public void ContiguousOffsetAndCharacterViewsUseRawBytesWithoutCopiesOrIteration()
    {
        var tableOwner = new PyByteArray([9, .. All(), 8]); var all = View(tableOwner);
        var table = (PyMemoryView)all.GetItem(new PySlice(1L, 257L, 1L));
        var character = (PyMemoryView)PyOps.CallMethod(table, "cast", ["c"], [], [])!;
        var deleteOwner = new PyByteArray(B("!b?").Bytes); var deletionAll = View(deleteOwner);
        var delete = (PyMemoryView)deletionAll.GetItem(new PySlice(1L, 2L, 1L));
        try
        {
            tableOwner.SetItem(98L, 90L);
            Assert.Equal(B("Zc").Bytes, Data(Call(B("abc"), character, delete)));
            deleteOwner.SetItem(1L, 99L);
            Assert.Equal(B("Zb").Bytes, Data(Call(B("abc"), character, delete)));
        }
        finally { character.Release(); table.Release(); all.Release(); delete.Release(); deletionAll.Release(); }
        tableOwner.Append(3L); deleteOwner.Append(4L);
    }

    [Fact]
    public void ActiveSourceViewStaysLiveAndTranslateDoesNotOwnExtraExports()
    {
        var source = new PyByteArray(B("abc").Bytes); var view = View(source);
        var result = Assert.IsType<PyByteArray>(Call(source, PyNone.Instance, view));
        Assert.Empty(result.Bytes); Assert.Equal(B("abc").Bytes, source.Bytes);
        Assert.Throws<PyBufferError>(() => source.Append(1L));
        view.SetItem(0L, 90L); Assert.Equal(B("Zbc").Bytes, source.Bytes);
        view.Release(); source.Append(1L);
    }

    [Fact]
    public void BadLengthTableWinsBeforeDeleteAndEmptySourceDoesNotSkipValidation()
    {
        foreach (bool mutable in new[] { false, true })
            for (int route = 0; route < 4; route++)
                foreach (int length in new[] { 0, 255, 257 })
                    Assert.Equal("translation table must be 256 characters long",
                        Assert.Throws<PyValueError>(() => Route(route, Receiver(mutable, []),
                            [new PyBytes(new byte[length]), PyNone.Instance], [], [])).PyMessage);
    }

    [Fact]
    public void ReleasedTableAndDeleteKeepValueErrorAndDoNotLeakOtherViews()
    {
        var owner = new PyByteArray(All()); var valid = View(owner); var released = View(B("")); released.Release();
        try
        {
            Assert.Equal("operation forbidden on released memoryview object",
                Assert.Throws<PyValueError>(() => Call(B(""), released, 1L)).PyMessage);
            Assert.Equal("operation forbidden on released memoryview object",
                Assert.Throws<PyValueError>(() => Call(B(""), valid, released)).PyMessage);
            Assert.Equal("translation table must be 256 characters long",
                Assert.Throws<PyValueError>(() => Call(B(""), B("x"), released)).PyMessage);
        }
        finally { valid.Release(); }
        owner.Append(0L);
    }

    [Fact]
    public void NoneTableFastPathStillValidatesEmptyDeleteViewAndDoesNotRetainIt()
    {
        var source = new PyBytes(new byte[1024 * 1024]); var owner = new PyByteArray([]); var view = View(owner);
        Assert.Same(source, Call(source, PyNone.Instance, view));
        view.Release(); owner.Append(1L);
        Assert.Equal("operation forbidden on released memoryview object",
            Assert.Throws<PyValueError>(() => Call(source, PyNone.Instance, view)).PyMessage);
        Assert.Throws<PyTypeError>(() => Call(source, PyNone.Instance, PyNone.Instance));
    }

    [Fact]
    public void BinderPreservesReferencePrecedenceAndDoesNotTreatExplicitNoneAsOmitted()
    {
        foreach (bool mutable in new[] { false, true })
            for (int route = 0; route < 4; route++)
            {
                object source = Receiver(mutable, []);
                void Error(string expected, object?[] args, object?[] names, object?[] values) =>
                    Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Route(route, source, args, names, values)).PyMessage);
                Error("translate() takes at least 1 positional argument (0 given)", [], [], []);
                Error("translate() takes at least 1 positional argument (0 given)", [], ["table"], [PyNone.Instance]);
                Error("translate() takes at most 2 keyword arguments (3 given)", [], ["a", "b", "c"], [1L, 2L, 3L]);
                Error("translate() takes at most 2 arguments (3 given)", [PyNone.Instance, B("")], ["delete"], [B("")]);
                Error("'table' is an invalid keyword argument for translate()", [PyNone.Instance], ["table"], [1L]);
                Error("'other' is an invalid keyword argument for translate()", [B("")], ["other"], [1L]);
                Error("a bytes-like object is required, not 'NoneType'", [PyNone.Instance, PyNone.Instance], [], []);
                Error("a bytes-like object is required, not 'int'", [1L, PyNone.Instance], [], []);
                Error("a bytes-like object is required, not 'str'", [PyNone.Instance], ["delete"], [""]);
            }
    }

    [Fact]
    public void RootRequiresRealPositionalSelfBeforeBinderWithoutCoercingBuffers()
    {
        foreach (bool mutable in new[] { false, true })
        {
            var owner = PyType.Get(mutable ? "bytearray" : "bytes"); object root = PyObject.GetAttr(owner, "translate")!;
            Assert.Equal($"unbound method {owner.Name}.translate() needs an argument",
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [], ["self", "x", "y"], [B(""), 1L, 2L])).PyMessage);
            foreach (object wrong in new object[] { PyNone.Instance, "a", 1L, Receiver(!mutable, []) })
                Assert.Equal($"descriptor 'translate' for '{owner.Name}' objects doesn't apply to a '{PyRepr.TypeName(wrong)}' object",
                    Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [wrong, 1L, 2L, 3L], [], [])).PyMessage);
        }
    }

    [Fact]
    public void ConversionProtocolsAreNotBufferAndUserBufferProviderIsExplicitlyUnsupported()
    {
        int calls = 0; var cls = new PyClass("Trap");
        foreach (string name in new[] { "__bytes__", "__iter__", "__index__", "__len__", "__getitem__" })
            cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, _ => { calls++; return B("x"); }));
        var trap = new PyInstance(cls);
        Assert.Equal("a bytes-like object is required, not 'Trap'", Assert.Throws<PyTypeError>(() => Call(B("a"), trap, B(""))).PyMessage);
        Assert.Throws<PyTypeError>(() => Call(B("a"), PyNone.Instance, trap));
        cls.SetAttr("__buffer__", PyNativeFunctions.CreateVariadic("__buffer__", _ => { calls++; return B("x"); }));
        Assert.Equal("translate does not support user-defined buffer providers", Assert.Throws<PyNotImplementedError>(() => Call(B("a"), trap, B(""))).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => Call(B("a"), PyNone.Instance, trap));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void MethodMetadataAndStableRootMatchBuiltinCarrier()
    {
        foreach (bool mutable in new[] { false, true })
        {
            object source = Receiver(mutable, []); object owner = PyObject.TypeOf(source);
            object root = PyObject.GetAttr(owner, "translate")!, bound = PyObject.GetAttr(source, "translate")!;
            Assert.Same(root, PyObject.GetAttr(owner, "translate")); Assert.Same(owner, PyObject.GetAttr(root, "__objclass__"));
            Assert.Equal("translate", PyObject.GetAttr(root, "__name__"));
            Assert.Equal((mutable ? "bytearray" : "bytes") + ".translate", PyObject.GetAttr(root, "__qualname__"));
            Assert.Equal("method_descriptor", PyRepr.TypeName(root));
            Assert.Equal("builtin_function_or_method", PyRepr.TypeName(bound));
            Assert.Same(source, PyObject.GetAttr(bound, "__self__"));
            Assert.True(PyHash.Equal(bound, PyObject.GetAttr(source, "translate")));
        }
    }

    [Fact]
    public void PicFallbackDoesNotConfuseOrdinaryTranslateOrHoldOldContents()
    {
        var cls = new PyClass("Ordinary");
        cls.SetAttr("translate", PyNativeFunctions.Create("translate", ["self", "table"], [null, null], 2, _ => "ordinary"));
        var instance = new PyInstance(cls); var source = new PyByteArray(B("abc").Bytes);
        for (int pass = 0; pass < 3; pass++)
        {
            Assert.Equal("ordinary", PyPic.CallMethod1(1841, instance, "translate", PyNone.Instance));
            Assert.Equal(Data(source), Data(PyPic.CallMethod1(1841, source, "translate", PyNone.Instance)));
            Assert.Equal(B("ac").Bytes, Data(PyPic.CallMethod2(1842, B("abc"), "translate", PyNone.Instance, B("b"))));
            source.Append(90L);
        }
    }

    [Fact]
    public void BufferAndBindingFailuresKeepCallerFramesAndAmbientContext()
    {
        long depth = PyTraceback.PushFrame("binary_translate.py", "caller");
        var ambient = new PyRuntimeError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.SetLine(41);
            for (int route = 0; route < 4; route++)
            {
                var error = Assert.Throws<PyTypeError>(() => Route(route, B(""), [PyNone.Instance, PyNone.Instance], [], []));
                Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("binary_translate.py", "caller", 41), Assert.Single(error.Frames));
            }
            object root = PyObject.GetAttr(PyType.Get("bytes"), "translate")!;
            var missing = Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [], [], []));
            Assert.Same(ambient, missing.Context); Assert.Single(missing.Frames);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
}
