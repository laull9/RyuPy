// list.pop 的宽索引、单次协议及失败纯性；不以共享下标的行为代替 pop 合同。
using System.Numerics;
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyListPopIndexTests
{
    private static readonly string[] Routes = ["direct", "prepare", "getattr", "invoke", "false", "true", "pic"];
    private static object? Pop(string route, PyList list, object? index) => route == "pic"
        ? PyPic.CallMethod1(3701, list, "pop", index)
        : ListBindingChecks.Call(route, list, "pop", [index], [], []);
    private static PyInstance Slot(Func<object?> body, string name = "__index__")
    {
        var cls = new PyClass("PopIndex");
        cls.SetAttr(name, PyNativeFunctions.Create(name, ["self"], [null], 1, _ => body()));
        return new PyInstance(cls);
    }
    private static object? Field(PyList list, string name) => typeof(PyList)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(list);

    [Fact]
    public void ExactAndBigIntegerValuesPreserveRemovedObjectIdentity()
    {
        foreach (string route in Routes)
        foreach (var (index, position) in new (object, int)[] {
            (0, 0), (1L, 1), (-1L, 2), (-3, 0), (false, 0), (true, 1),
            (BigInteger.Zero, 0), (new BigInteger(-1), 2) })
        {
            object?[] before = ListBindingChecks.Markers(); var list = new PyList(before);
            Assert.Same(before[position], Pop(route, list, index));
            ListBindingChecks.Unchanged(list, before.Where((_, i) => i != position).ToArray());
        }
    }

    [Fact]
    public void WideMachineIndicesNeverTruncateOrMaterializeOnFailure()
    {
        foreach (string route in Routes)
        foreach (object index in new object[] { 3L, -4L, 2147483648L, -2147483649L,
            4294967296L, -4294967296L, long.MaxValue, long.MinValue,
            new BigInteger(long.MaxValue), new BigInteger(long.MinValue) })
        {
            var list = (PyList)PyNumericListOps.Create();
            PyNumericListOps.AppendF64(list, 1.25); PyNumericListOps.AppendF64(list, 2.5);
            var numeric = Field(list, "_numeric");
            Assert.Equal("pop index out of range", Assert.Throws<PyIndexError>(() => Pop(route, list, index)).Message);
            Assert.Null(Field(list, "_items")); Assert.Same(numeric, Field(list, "_numeric"));
            Assert.Equal(2, list.Count); Assert.Equal(1.25, PyNumericListOps.ReadF64(list, 0L));
            Assert.Equal(2.5, PyNumericListOps.ReadF64(list, 1L));
            Assert.Equal("pop from empty list", Assert.Throws<PyIndexError>(() => Pop(route, new PyList(), index)).Message);
        }
    }

    [Fact]
    public void ConversionErrorsPrecedeEmptyCheckAndKeepOriginalContents()
    {
        foreach (string route in Routes)
        foreach (bool empty in new[] { false, true })
        {
            object?[] before = empty ? [] : ListBindingChecks.Markers(); var list = new PyList(before);
            foreach (object index in new object[] { (BigInteger)long.MaxValue + 1,
                (BigInteger)long.MinValue - 1, BigInteger.One << 100, -(BigInteger.One << 100) })
            {
                Assert.Equal("Python int too large to convert to C ssize_t",
                    Assert.Throws<PyOverflowError>(() => Pop(route, list, index)).Message);
                int calls = 0;
                Assert.Throws<PyOverflowError>(() => Pop(route, list, Slot(() => { calls++; return index; })));
                Assert.Equal(1, calls); ListBindingChecks.Unchanged(list, before);
            }
            foreach (var (index, type) in new (object?, string)[] { (null, "NoneType"), (PyNone.Instance, "NoneType"), ("bad", "str"), (1.5, "float") })
                Assert.Equal($"'{type}' object cannot be interpreted as an integer",
                    Assert.Throws<PyTypeError>(() => Pop(route, list, index)).Message);
            ListBindingChecks.Unchanged(list, before);
        }
        // 冷转换失败也不能先物化 numeric 背板，不只是 long 正文越界。
        foreach (string route in Routes)
        foreach (object? index in new object?[] { "bad", BigInteger.One << 100,
            Slot(() => "bad"), Slot(() => true), Slot(() => BigInteger.One << 100) })
        {
            var list = (PyList)PyNumericListOps.Create();
            PyNumericListOps.AppendF64(list, 1.25);
            object? backing = Field(list, "_numeric");
            Assert.IsAssignableFrom<PyBaseException>(Record.Exception(() => Pop(route, list, index)));
            Assert.Null(Field(list, "_items")); Assert.Same(backing, Field(list, "_numeric"));
            Assert.Equal(1, list.Count); Assert.Equal(1.25, PyNumericListOps.ReadF64(list, 0L));
        }
    }

    [Fact]
    public void ProtocolRunsOnceAndUsesCurrentListAfterCallback()
    {
        foreach (string route in Routes)
        foreach (bool empty in new[] { false, true })
        foreach (bool clear in new[] { false, true })
        {
            var original = new PyList(); var added = new PyList();
            var list = new PyList(empty ? [] : [original]); int calls = 0;
            var index = Slot(() => { calls++; if (clear) list.Clear(); else list.Append(added); return -1L; });
            if (clear) Assert.Equal("pop from empty list", Assert.Throws<PyIndexError>(() => Pop(route, list, index)).Message);
            else { Assert.Same(added, Pop(route, list, index)); Assert.Equal(empty ? 0 : 1, list.Count); }
            Assert.Equal(1, calls);
        }
    }

    [Fact]
    public void TypeSlotDoesNotFallbackRecurseOrAcceptDeprecatedBooleanResult()
    {
        foreach (string route in Routes)
        {
            var list = new PyList(["a", "b"]); int calls = 0;
            var intOnly = Slot(() => { calls++; return 0L; }, "__int__");
            Assert.Throws<PyTypeError>(() => Pop(route, list, intOnly)); Assert.Equal(0, calls);
            var nested = Slot(() => { calls++; return 0L; });
            Assert.Equal("__index__ returned non-int (type PopIndex)",
                Assert.Throws<PyTypeError>(() => Pop(route, list, Slot(() => nested))).Message);
            Assert.Equal(0, calls);
            foreach (object? result in new object?[] { null, PyNone.Instance, "bad", 1.5 })
                Assert.Equal($"__index__ returned non-int (type {PyRepr.TypeName(result)})",
                    Assert.Throws<PyTypeError>(() => Pop(route, list, Slot(() => result))).Message);
            foreach (bool result in new[] { false, true })
                Assert.Equal("list.pop(): __index__ returning bool requires DeprecationWarning support",
                    Assert.Throws<PyNotImplementedError>(() => Pop(route, list, Slot(() => result))).Message);
            var plain = new PyInstance(new PyClass("InstanceOnly"));
            PyObject.SetAttr(plain, "__index__", PyNativeFunctions.Create("shadow", [], [], 0, _ => { calls++; return 0L; }));
            Assert.Throws<PyTypeError>(() => Pop(route, list, plain)); Assert.Equal(0, calls);
            var actual = Slot(() => { calls++; return 0L; });
            PyObject.SetAttr(actual, "__index__", PyNone.Instance);
            Assert.Equal("a", Pop(route, list, actual)); Assert.Equal(1, calls);
            Assert.Equal("b", list.GetItem(0));
        }
    }

    [Fact]
    public void BinderRejectsBeforeCallingIndex()
    {
        foreach (string route in Routes.Where(route => route != "pic"))
        {
            var list = new PyList(["a"]); int calls = 0;
            var index = Slot(() => { calls++; return 0L; });
            Assert.Throws<PyTypeError>(() => ListBindingChecks.Call(route, list, "pop", [index, index], [], []));
            Assert.Throws<PyTypeError>(() => ListBindingChecks.Call(route, list, "pop", [index], ["index"], [index]));
            Assert.Equal(0, calls); Assert.Equal("a", list.GetItem(0));
        }
    }

    [Fact]
    public void OwnErrorsCaptureCallerAndContextButCallbackErrorIsNeverRewritten()
    {
        long frame = PyTraceback.PushFrame("pop_test.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            PyTraceback.SetLine(37);
            foreach (string route in Routes)
            {
                var list = new PyList(["a"]);
                PyBaseException[] errors = [
                    Assert.Throws<PyIndexError>(() => Pop(route, list, 4294967296L)),
                    Assert.Throws<PyIndexError>(() => Pop(route, new PyList(), 0L)),
                    Assert.Throws<PyOverflowError>(() => Pop(route, list, BigInteger.One << 100)),
                    Assert.Throws<PyTypeError>(() => Pop(route, list, "bad")),
                    Assert.Throws<PyTypeError>(() => Pop(route, list, Slot(() => "bad"))),
                    Assert.Throws<PyNotImplementedError>(() => Pop(route, list, Slot(() => true))) ];
                foreach (var error in errors)
                {
                    Assert.Same(outer, error.Context);
                    Assert.Null(error.Cause); Assert.False(error.SuppressContextFlag);
                    Assert.Equal(new PyStackFrame("pop_test.py", "caller", 37), Assert.Single(error.Frames));
                    Assert.Equal(error.Message, Assert.Single(error.Args));
                }
                var marker = new PyLookupError("callback");
                var cause = new PyValueError("explicit cause"); marker.WithCause(cause);
                PyErrors.PrepareRaise(marker); var frames = marker.Frames; var context = marker.Context;
                var index = Slot(() => { list.Append("side effect"); throw marker; });
                Assert.Same(marker, Assert.Throws<PyLookupError>(() => Pop(route, list, index)));
                Assert.Same(frames, marker.Frames); Assert.Same(context, marker.Context);
                Assert.Same(cause, marker.Cause); Assert.True(marker.SuppressContextFlag);
                Assert.Equal(2, list.Count); Assert.Equal("side effect", list.GetItem(1));
                Assert.Same(outer, PyErrors.Current); Assert.Equal(frame, PyTraceback.CaptureDepth());
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(frame); }
    }
}
