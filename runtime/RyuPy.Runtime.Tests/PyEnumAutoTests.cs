// Enum auto的赋值期、名单身份、marker缓存与警告原子性回归，不把最终值正确当完整协议。
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyEnumAutoTests
{
    private static object Auto(params object?[] args) => PyOps.CallValue(PyEnum.CreateAttributes()["auto"], args, [], [])!;
    private static PyClass Begin(object?[] names, string root = "Enum") =>
        (PyClass)PyEnum.Create("AutoValues", [PyEnum.CreateAttributes()[root]], names);
    private static object? Member(PyClass cls, string name) => PyObject.GetAttr(PyEnum.GetItem(cls, name), "value");
    private static object? Warnings(string name, params object?[] args) => PyOps.CallValue(PyWarnings.CreateAttributes()[name], args, [], []);
    private static void Capture(Action<PyList> body)
    {
        object? context = PyOps.CallValue(PyWarnings.CreateAttributes()["catch_warnings"], [], ["record"], [true]);
        var records = Assert.IsType<PyList>(PyOps.CallValue(PyObject.GetAttr(context, "__enter__"), [], [], []));
        PyModules.Register("enum_auto_test", "", "enum_auto_test.py", () => { }, _ => PyNone.Instance, (_, _, _) => { });
        long depth = PyTraceback.PushFrame("enum_auto_test.py", "enum_body");
        try { PyTraceback.SetLine(17); body(records); }
        finally
        {
            PyTraceback.PopTo(depth);
            PyOps.CallValue(PyObject.GetAttr(context, "__exit__"), [PyNone.Instance, PyNone.Instance, PyNone.Instance], [], []);
        }
    }
    [Fact]
    public void AssignmentResolvesBeforeReadsAndReusedMarkerSharesAcrossClasses()
    {
        var cls = Begin(["A", "B", "C"]); object marker = Auto();
        Assert.Equal("auto(_auto_null)", PyRepr.Repr(marker));
        Assert.Same(PyEnum.CreateAttributes()["auto"], PyObject.TypeOf(marker));
        cls.SetAttr("A", marker); Assert.Equal(1L, cls.Attributes["A"]);
        Assert.Equal(1L, PyObject.GetAttr(marker, "value"));
        cls.SetAttr("B", marker); cls.SetAttr("C", Auto());
        Assert.Equal(2L, cls.Attributes["C"]);
        PyEnum.Finalize(cls, ["A", "B", "C"]);
        Assert.Same(PyEnum.GetItem(cls, "A"), PyEnum.GetItem(cls, "B"));
        Assert.Equal(2L, Member(cls, "C"));
        var other = Begin(["Z", "A"]); other.SetAttr("Z", 99L); other.SetAttr("A", marker);
        PyEnum.Finalize(other, ["Z", "A"]); Assert.Equal(1L, Member(other, "A"));
    }
    [Fact]
    public void ExplicitValueBindingAndMutableScalarValueKeepCanonicalMarker()
    {
        object marker = Auto(7L); Assert.Equal("auto(7)", PyRepr.Repr(marker));
        PyObject.SetAttr(marker, "value", 11L); Assert.Equal(11L, PyObject.GetAttr(marker, "value"));
        var cls = Begin(["A", "B"]); cls.SetAttr("A", marker); cls.SetAttr("B", Auto());
        PyEnum.Finalize(cls, ["A", "B"]); Assert.Equal(12L, Member(cls, "B"));
        Assert.Throws<PyTypeError>(() => Auto(1L, 2L));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyEnum.CreateAttributes()["auto"], [], ["bad"], [1L]));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(marker, "value", marker));
        object? keyed = PyOps.CallValue(PyEnum.CreateAttributes()["auto"], [], ["value"], [9L]);
        Assert.Equal(9L, PyObject.GetAttr(keyed, "value"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyEnum.CreateAttributes()["auto"], [1L], ["value"], [2L]));
    }
    [Fact]
    public void DeclaredNamesAreCopiedAndFinalizationRequiresExactOrderAndCompletion()
    {
        object?[] names = ["A", "B"]; var cls = Begin(names); names[0] = "changed";
        cls.SetAttr("__module__", "enum_auto_test"); cls.SetAttr("ordinary_method", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => cls.SetAttr("B", Auto()));
        Assert.Throws<PyTypeError>(() => PyEnum.Finalize(cls, ["A", "B"]));
        cls.SetAttr("A", Auto()); cls.SetAttr("B", Auto());
        Assert.Throws<PyTypeError>(() => PyEnum.Finalize(cls, ["B", "A"]));
        PyEnum.Finalize(cls, ["A", "B"]); Assert.Equal(1L, Member(cls, "A"));
        Assert.Throws<PyTypeError>(() => Begin(["A", "A"]));
        Assert.Throws<PyTypeError>(() => Begin([1L]));
        Assert.Throws<PyTypeError>(() => Begin(["__module__"]));
        Assert.Throws<PyTypeError>(() => Begin(["_generate_next_value_"]));
        Assert.Throws<PyTypeError>(() => Begin([""]));
        Assert.Throws<PyAttributeError>(() => cls.SetAttr("A", Auto()));
    }
    [Fact]
    public void NonMonotoneIntegersUseRealWarningAndMaximumWithBigIntegers()
    {
        Capture(records =>
        {
            Warnings("simplefilter", "always");
            BigInteger huge = BigInteger.One << 100;
            var cls = Begin(["A", "B", "C"]); cls.SetAttr("A", huge); cls.SetAttr("B", -7L); cls.SetAttr("C", Auto());
            PyEnum.Finalize(cls, ["A", "B", "C"]); Assert.Equal(huge + 1, Member(cls, "C"));
            object warning = Assert.Single(records.Items)!;
            Assert.Same(PyType.Get("DeprecationWarning"), PyObject.GetAttr(warning, "category"));
            Assert.Equal("enum_auto_test.py", PyObject.GetAttr(warning, "filename"));
            Assert.Equal(17L, PyObject.GetAttr(warning, "lineno"));
        });
    }
    [Fact]
    public void WarningAsErrorDoesNotCacheMarkerOrConsumeMemberSlot()
    {
        Capture(_ =>
        {
            Warnings("simplefilter", "error");
            var cls = Begin(["A", "B", "C"]); cls.SetAttr("A", 5L); cls.SetAttr("B", 1L); object marker = Auto();
            object? unset = PyObject.GetAttr(marker, "value");
            Assert.Throws<PyDeprecationWarning>(() => cls.SetAttr("C", marker));
            Assert.False(cls.Attributes.ContainsKey("C")); Assert.Same(unset, PyObject.GetAttr(marker, "value"));
            Warnings("simplefilter", "ignore"); cls.SetAttr("C", marker);
            PyEnum.Finalize(cls, ["A", "B", "C"]); Assert.Equal(6L, Member(cls, "C"));
        });
    }
    [Fact]
    public void NonIntegerScalarHistoryWarnsAndMixedHistoryExplicitlyRejects()
    {
        Capture(records =>
        {
            Warnings("simplefilter", "always");
            var text = Begin(["A", "B"]); text.SetAttr("A", "str"); text.SetAttr("B", Auto());
            PyEnum.Finalize(text, ["A", "B"]); Assert.Equal(1L, Member(text, "B"));
            var mixed = Begin(["A", "B", "C"]); mixed.SetAttr("A", 5L); mixed.SetAttr("B", "str"); object marker = Auto();
            Assert.Throws<PyNotImplementedError>(() => mixed.SetAttr("C", marker));
            Assert.Equal("auto(_auto_null)", PyRepr.Repr(marker)); Assert.Equal(2, records.Count);
            Warnings("simplefilter", "error");
            Assert.Throws<PyDeprecationWarning>(() => mixed.SetAttr("C", marker));
        });
    }
    [Fact]
    public void FlagUsesNextBitAndKeepsNegativeGuardAndOldNullProtocol()
    {
        var flags = Begin(["ZERO", "A", "B", "BOTH", "C"], "Flag");
        flags.SetAttr("ZERO", 0L); flags.SetAttr("A", Auto()); flags.SetAttr("B", Auto());
        flags.SetAttr("BOTH", 3L); flags.SetAttr("C", Auto());
        PyEnum.Finalize(flags, ["ZERO", "A", "B", "BOTH", "C"]); Assert.Equal(4L, Member(flags, "C"));
        var negative = Begin(["A"], "Flag"); Assert.Throws<PyNotImplementedError>(() => negative.SetAttr("A", -1L));
        var old = (PyClass)PyEnum.Create("Old", [PyEnum.CreateAttributes()["Enum"]], PyNone.Instance);
        old.SetAttr("A", 1L); PyEnum.Finalize(old, ["A"]); Assert.Equal(1L, Member(old, "A"));
        var undeclared = (PyClass)PyEnum.Create("OldAuto", [PyEnum.CreateAttributes()["Enum"]], PyNone.Instance);
        undeclared.SetAttr("A", Auto());
        Assert.Throws<PyNotImplementedError>(() => PyEnum.Finalize(undeclared, ["A"]));
    }
}
