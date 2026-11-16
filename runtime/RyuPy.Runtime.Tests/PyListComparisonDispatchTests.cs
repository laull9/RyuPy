// list派生表达式eq/ne与外部对象的事件序列；bool ABI边界不可吞掉用户返回身份。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyListComparisonDispatchTests
{
    private static PyInstance List()
    {
        var cls = (PyClass)PyOps.MakeClass("L", [PyType.Get("list")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return Assert.IsAssignableFrom<PyInstance>(cls.Invoke([new PyList([1L])], [], []));
    }
    private static void Slot(PyClass cls, string name, Func<object?> body) =>
        cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, _ => body()));

    [Fact]
    public void ExplicitNeRunsOnceInBothDirectionsAndNeverRunsEq()
    {
        var list = List(); var foreignClass = new PyClass("Foreign"); var events = new List<string>();
        Slot(foreignClass, "__eq__", () => { events.Add("eq"); return false; });
        Slot(foreignClass, "__ne__", () => { events.Add("ne"); return false; });
        var foreign = new PyInstance(foreignClass);
        Assert.False(PyOps.Compare(PyOps.Ne, list, foreign));
        Assert.False(PyOps.Compare(PyOps.Ne, foreign, list));
        Assert.Equal(new[] { "ne", "ne" }, events);
        events.Clear(); Slot(foreignClass, "__ne__", () => { events.Add("NI"); return PyNotImplemented.Instance; });
        Assert.True(PyOps.Compare(PyOps.Ne, list, foreign)); Assert.True(PyOps.Compare(PyOps.Ne, foreign, list));
        Assert.Equal(new[] { "NI", "NI" }, events);
    }

    [Fact]
    public void MissingNeUsesObjectNegationButExplicitNonboolNeverCallsBool()
    {
        var list = List(); var foreignClass = new PyClass("Foreign"); var resultClass = new PyClass("Result");
        int eq = 0, truth = 0;
        Slot(resultClass, "__bool__", () => { truth++; return false; });
        var result = new PyInstance(resultClass);
        Slot(foreignClass, "__eq__", () => { eq++; return result; });
        var foreign = new PyInstance(foreignClass);
        Assert.True(PyOps.Compare(PyOps.Ne, list, foreign)); Assert.True(PyOps.Compare(PyOps.Ne, foreign, list));
        Assert.Equal(2, eq); Assert.Equal(2, truth);
        truth = 0;
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, list, foreign));
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, foreign, list));
        Slot(foreignClass, "__ne__", () => result);
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Ne, list, foreign));
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Ne, foreign, list));
        Assert.Equal(0, truth);
        // 容器内部比较仍允许非bool eq结果的真值协议。
        Assert.False(PyHash.Equal(list, foreign)); Assert.Equal(1, truth);
    }

    [Fact]
    public void MissingNeWithNotImplementedEqFallsBackWithoutTruthy()
    {
        var list = List(); var foreignClass = new PyClass("Foreign"); int calls = 0;
        Slot(foreignClass, "__eq__", () => { calls++; return PyNotImplemented.Instance; });
        var foreign = new PyInstance(foreignClass);
        Assert.True(PyOps.Compare(PyOps.Ne, list, foreign)); Assert.True(PyOps.Compare(PyOps.Ne, foreign, list));
        Assert.False(PyOps.Compare(PyOps.Eq, list, foreign)); Assert.False(PyOps.Compare(PyOps.Eq, foreign, list));
        Assert.Equal(4, calls);
    }

    [Fact]
    public void IteratorAbcIncludesInheritedListRootIter()
    {
        var list = List(); Slot(list.Class, "__next__", () => 7L);
        object? abc = PyAbc.CreateAttributes()["Iterator"];
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [list, abc], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("issubclass", [list.Class, abc], [], []));
        Assert.Equal(7L, PyOps.CallValue(PyObject.GetAttr(list, "__next__"), [], [], []));
        Slot(list.Class, "__call__", () => PyNone.Instance);
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [list, PyAbc.CreateAttributes()["Callable"]], [], []));
    }

    [Fact]
    public void BothListLayoutsUseActualNeAndEqProtocols()
    {
        var list = List(); var exact = new PyList([1L]);
        Assert.True(PyOps.Compare(PyOps.Eq, list, exact)); Assert.True(PyOps.Compare(PyOps.Eq, exact, list));
        Assert.False(PyOps.Compare(PyOps.Ne, list, exact)); Assert.False(PyOps.Compare(PyOps.Ne, exact, list));
    }
}
