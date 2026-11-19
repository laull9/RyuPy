using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>B3有限运算与真实类型闭合；显式比较保对象，不把它当全局比较表达式ABI验收。</summary>
[Collection("生成器帧栈")]
public class PyMappingProxyOperatorTests
{
    private static PyMappingProxy New(object mapping) => (PyMappingProxy)PyMappingProxy.Create([mapping], [], []);
    private static object? Call(object value, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);
    private static PyFunction Slot(string name, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, ["self", "other"], [null, null], 2, body);
    private static PyInstance Mapping(string name)
    {
        var cls = new PyClass(name);
        cls.SetAttr("__getitem__", Slot("__getitem__", _ => 1L));
        return new PyInstance(cls);
    }
    private static PyDict Dict(params object?[] pairs)
    {
        var result = new PyDict();
        for (int i = 0; i < pairs.Length; i += 2) result.Set(pairs[i], pairs[i + 1]);
        return result;
    }

    [Fact]
    public void ExactDictionaryUnionKeepsOrderAliasesAndOldInPlaceFailure()
    {
        var shared = new PyList(); var left = Dict("a", shared, "b", 2L, true, "old");
        var right = Dict("b", 7L, "c", 3L, new BigInteger(1), "new");
        var result = Assert.IsType<PyDict>(PyOps.Binary(PyOps.BitOr, left, right));
        Assert.Equal(new object?[] { "a", "b", true, "c" }, result.Keys.ToArray());
        Assert.Same(shared, result.Get("a")); Assert.Equal("new", result.Get(1.0));
        Assert.Equal(2L, left.Get("b")); Assert.Equal("old", left.Get(true));
        Assert.NotSame(left, result); Assert.NotSame(right, result);
        Assert.True(PyHash.Equal(result, PyOps.Binary(PyOps.BitOr, New(left), right)));
        Assert.True(PyHash.Equal(result, PyOps.Binary(PyOps.BitOr, left, New(right))));
        Assert.True(PyHash.Equal(result, PyOps.Binary(PyOps.BitOr, New(left), New(right))));
        Assert.Throws<PyTypeError>(() => PyOps.Binary(PyOps.BitOr | PyOps.InPlaceFlag, left, right));
        Assert.Equal(2L, left.Get("b"));
    }

    [Fact]
    public void UnionCallbacksKeepDirectionOneLayerAndOriginalErrors()
    {
        PyInstance mapping = Mapping("UnionMap"); var marker = new object(); var log = new List<object?>();
        mapping.Class.SetAttr("__or__", Slot("__or__", a => { log.Add(a[1]); return marker; }));
        mapping.Class.SetAttr("__ror__", Slot("__ror__", a => { log.Add(a[1]); return marker; }));
        PyMappingProxy proxy = New(mapping); var other = new PyDict();
        Assert.Same(marker, PyOps.Binary(PyOps.BitOr, proxy, other)); Assert.Same(other, log.Single()); log.Clear();
        Assert.Same(marker, PyOps.Binary(PyOps.BitOr, other, proxy)); Assert.Same(other, log.Single()); log.Clear();
        PyInstance second = Mapping("UnionOther");
        Assert.Same(marker, Call(proxy, "__or__", New(second))); Assert.Same(second, log.Single()); log.Clear();
        PyMappingProxy inner = New(second);
        Assert.Same(marker, Call(proxy, "__or__", New(inner))); Assert.Same(inner, log.Single());
        // 未轮到的右descriptor不能提前拒绝，左NI之后才读取最新右槽。
        PyInstance guarded = Mapping("UnionGuarded"); int rightCalls = 0;
        guarded.Class.SetAttr("__ror__", new PyProperty(Slot("descriptor", _ => throw new Exception("unused"))));
        Assert.Same(marker, Call(proxy, "__or__", guarded));
        mapping.Class.SetAttr("__or__", Slot("__or__", _ =>
        {
            guarded.Class.SetAttr("__ror__", Slot("__ror__", _ => { rightCalls++; return marker; }));
            return PyNotImplemented.Instance;
        }));
        Assert.Same(marker, Call(proxy, "__or__", guarded)); Assert.Equal(1, rightCalls);
        mapping.Class.SetAttr("__or__", Slot("__or__", _ =>
        {
            guarded.Class.SetAttr("__ror__", new PyProperty(null)); return PyNotImplemented.Instance;
        }));
        Assert.Throws<PyNotImplementedError>(() => Call(proxy, "__or__", guarded)); Assert.Equal(1, rightCalls);
        var exceptionOperand = new PyValueError("operand"); var exceptionClass = new PyClass("ExceptionOperand");
        exceptionOperand.BindPythonClass(exceptionClass);
        exceptionClass.SetAttr("__ror__", new PyProperty(null));
        mapping.Class.SetAttr("__or__", Slot("__or__", _ => PyNotImplemented.Instance));
        Assert.Throws<PyNotImplementedError>(() => Call(proxy, "__or__", exceptionOperand));
        int forwardCalls = 0; rightCalls = 0;
        mapping.Class.SetAttr("__or__", Slot("__or__", _ => { forwardCalls++; return PyNotImplemented.Instance; }));
        guarded.Class.SetAttr("__ror__", Slot("__ror__", _ => { rightCalls++; return PyNotImplemented.Instance; }));
        Assert.Throws<PyTypeError>(() => Call(proxy, "__or__", guarded));
        Assert.Equal(1, forwardCalls); Assert.Equal(1, rightCalls);
        guarded.Class.SetAttr("__ror__", new PyProperty(null));
        var original = new PyValueError("union callback"); var active = new PyRuntimeError("outer");
        object? previous = PyErrors.EnterHandler(active);
        mapping.Class.SetAttr("__or__", Slot("__or__", _ => { PyErrors.PrepareRaise(original); throw original; }));
        try { Assert.Same(original, Assert.Throws<PyValueError>(() => Call(proxy, "__or__", guarded))); Assert.Same(active, original.Context); }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void NewUnionRejectsCustomKeysAndUnsupportedDerivedSlotsWithoutCopyCallbacks()
    {
        int hashes = 0, equals = 0; PyInstance key = Mapping("UnionKey");
        key.Class.SetAttr("__hash__", PyNativeFunctions.Create("__hash__", ["self"], [null], 1, _ => { hashes++; return 5L; }));
        key.Class.SetAttr("__eq__", Slot("__eq__", _ => { equals++; return false; }));
        var source = Dict(key, 4L); hashes = 0; equals = 0;
        Assert.Throws<PyNotImplementedError>(() => PyOps.Binary(PyOps.BitOr, source, new PyDict()));
        Assert.Equal(0, hashes); Assert.Equal(0, equals);
        Assert.Equal(4L, source.Get(key)); Assert.True(hashes > 0);
        var copied = new PyDict(); copied.Update(source); Assert.Equal(4L, copied.Get(key));
        object defaults = PyOps.CallValue(PyCollections.CreateAttributes()["defaultdict"], [], [], [])!;
        Assert.Throws<PyNotImplementedError>(() => Call(New(defaults), "__or__", new PyDict()));
        object counter = PyOps.CallValue(PyCollections.CreateAttributes()["Counter"], [Dict("x", 1L)], [], [])!;
        Assert.IsType<PyCounter>(Call(New(counter), "__or__", counter));
        PyInstance parent = Mapping("UnionParent");
        var childClass = (PyClass)PyOps.MakeClass("UnionChild", [parent.Class], null);
        var child = new PyInstance(childClass);
        Assert.Throws<PyNotImplementedError>(() => Call(New(parent), "__or__", child));
        parent.Class.SetAttr("__or__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => Call(New(parent), "__or__", new PyDict()));
    }

    [Fact]
    public void InPlaceAlwaysRaisesWithoutReadingOrCallingUnderlyingOperators()
    {
        PyInstance mapping = Mapping("InPlaceMap"); int calls = 0;
        foreach (string name in new[] { "__or__", "__ior__", "__ror__" })
            mapping.Class.SetAttr(name, Slot(name, _ => { calls++; return mapping; }));
        PyMappingProxy proxy = New(mapping);
        Assert.Equal("'|=' is not supported by mappingproxy; use '|' instead",
            Assert.Throws<PyTypeError>(() => Call(proxy, "__ior__", mapping)).Message);
        Assert.Throws<PyTypeError>(() => PyOps.Binary(PyOps.BitOr | PyOps.InPlaceFlag, proxy, new PyDict()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ExplicitComparisonsReturnOriginalObjectsAndNeverPeelObservedRightProxy()
    {
        PyInstance mapping = Mapping("EqualityMap"); PyMappingProxy proxy = New(mapping);
        PyMappingProxy other = New(new PyDict()); var eq = new PyList(); var ne = new object(); int calls = 0;
        mapping.Class.SetAttr("__eq__", Slot("__eq__", a => { calls++; Assert.Same(other, a[1]); return eq; }));
        mapping.Class.SetAttr("__ne__", Slot("__ne__", a => { calls++; Assert.Same(other, a[1]); return ne; }));
        Assert.Same(eq, Call(proxy, "__eq__", other)); Assert.Same(ne, Call(proxy, "__ne__", other)); Assert.Equal(2, calls);
        mapping.Class.SetAttr("__eq__", Slot("__eq__", _ => PyNone.Instance));
        Assert.Same(PyNone.Instance, Call(proxy, "__eq__", other));
        mapping.Class.SetAttr("__ne__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => Call(proxy, "__ne__", other));
        var exceptionOperand = new PyValueError("comparison operand"); var exceptionClass = new PyClass("CompareException");
        exceptionOperand.BindPythonClass(exceptionClass); exceptionClass.SetAttr("__eq__", new PyProperty(null));
        mapping.Class.SetAttr("__eq__", Slot("__eq__", _ => PyNotImplemented.Instance));
        Assert.Throws<PyNotImplementedError>(() => Call(proxy, "__eq__", exceptionOperand));
        var original = new PyValueError("comparison callback");
        mapping.Class.SetAttr("__eq__", Slot("__eq__", _ => throw original));
        Assert.Same(original, Assert.Throws<PyValueError>(() => Call(proxy, "__eq__", other)));
    }

    [Fact]
    public void ComparisonFallbackHasOneCandidatePerSideAndFiniteProxyDescent()
    {
        PyInstance left = Mapping("CompareLeft"), right = Mapping("CompareRight");
        var log = new List<string>(); var marker = new object();
        left.Class.SetAttr("__eq__", Slot("__eq__", _ => { log.Add("left-eq"); return true; }));
        Assert.Equal(false, Call(New(left), "__ne__", right)); Assert.Equal(new[] { "left-eq" }, log); log.Clear();
        left.Class.SetAttr("__ne__", Slot("__ne__", _ => { log.Add("left-ne"); return PyNotImplemented.Instance; }));
        right.Class.SetAttr("__ne__", Slot("__ne__", _ => { log.Add("right-ne"); return marker; }));
        Assert.Same(marker, Call(New(left), "__ne__", right)); Assert.Equal(new[] { "left-ne", "right-ne" }, log); log.Clear();
        right.Class.SetAttr("__ne__", Slot("__ne__", _ => { log.Add("right-ne"); return PyNotImplemented.Instance; }));
        Assert.Equal(true, Call(New(left), "__ne__", right)); Assert.Equal(new[] { "left-ne", "right-ne" }, log); log.Clear();
        PyInstance bare = Mapping("CompareBare");
        bare.Class.SetAttr("__eq__", Slot("__eq__", _ => { log.Add("bare-eq"); return PyNotImplemented.Instance; }));
        Assert.Equal(true, Call(New(bare), "__ne__", right)); Assert.Equal(new[] { "bare-eq", "right-ne" }, log); log.Clear();
        Assert.Equal(true, Call(New(bare), "__eq__", bare)); Assert.Equal(new[] { "bare-eq", "bare-eq" }, log); log.Clear();
        var a = Dict("x", 1L); var b = Dict("x", 1L);
        PyMappingProxy nested = New(New(b));
        Assert.Equal(true, Call(New(a), "__eq__", nested)); Assert.Equal(false, Call(nested, "__ne__", a));
        Assert.True(PyHash.Equal(a, nested)); Assert.True(PyHash.Equal(nested, a));
        right.Class.SetAttr("__eq__", Slot("__eq__", a => { log.Add("right-eq"); Assert.Equal("abc", a[1]); return marker; }));
        Assert.Same(marker, Call(New("abc"), "__eq__", right)); Assert.Equal(new[] { "right-eq" }, log);
    }

    [Fact]
    public void EnumUsesOneCachedCarrierAndRealMappingAbcIdentity()
    {
        var cls = (PyClass)PyEnum.Create("OperatorEnum", [PyEnum.CreateAttributes()["Enum"]], PyNone.Instance);
        cls.SetAttr("ONE", 1L); cls.SetAttr("ALIAS", 1L); PyEnum.Finalize(cls, ["ONE", "ALIAS"]);
        Assert.True(PyEnum.TryGetClassAttr(cls, "__members__", out object? value));
        var proxy = Assert.IsType<PyMappingProxy>(value);
        Assert.True(PyEnum.TryGetClassAttr(cls, "__members__", out object? again)); Assert.Same(proxy, again);
        Assert.Same(PyMappingProxy.Type, PyObject.TypeOf(proxy));
        Assert.Same(PyOps.GetItem(proxy, "ONE"), PyOps.GetItem(proxy, "ALIAS"));
        var copy = Assert.IsType<PyDict>(Call(proxy, "copy")); Assert.Same(copy.Get("ONE"), PyOps.GetItem(proxy, "ONE"));
        foreach (string name in new[] { "Mapping", "Iterable", "Sized", "Container", "Collection" })
        {
            object type = PyAbc.CreateAttributes()[name]!;
            Assert.Equal(true, PyOps.CallBuiltin("isinstance", [proxy, type], [], []));
            Assert.Equal(true, PyOps.CallBuiltin("issubclass", [PyMappingProxy.Type, type], [], []));
        }
        object mutable = PyAbc.CreateAttributes()["MutableMapping"]!;
        Assert.Equal(false, PyOps.CallBuiltin("isinstance", [proxy, mutable], [], []));
        Assert.Equal(false, PyOps.CallBuiltin("issubclass", [PyMappingProxy.Type, mutable], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [new PyDict(), mutable], [], []));
        Assert.Equal(false, PyOps.CallBuiltin("isinstance", [proxy, PyType.Get("dict")], [], []));
        Assert.Equal(true, PyOps.CallBuiltin("issubclass", [PyMappingProxy.Type, PyType.Get("object")], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.SetItem(proxy, "ONE", 2L));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyJsonModule.CreateAttributes()["dumps"], [proxy], [], []));
        Assert.False((object)proxy is IPyNativeMapping);
    }

    [Fact]
    public void RealNativeTypeCannotBeBaseButSameNamedUserClassStillCan()
    {
        var active = new PyRuntimeError("base context"); object? previous = PyErrors.EnterHandler(active);
        long depth = PyTraceback.PushFrame("proxy-base.py", "caller");
        try
        {
            foreach (Action action in new Action[] {
                () => PyRuntimeBases.Validate(PyMappingProxy.Type),
                () => PyOps.MakeClass("Bad", [PyMappingProxy.Type], null) })
            {
                PyTypeError error = Assert.Throws<PyTypeError>(action);
                Assert.Equal("type 'mappingproxy' is not an acceptable base type", error.Message);
                Assert.Same(active, error.Context); Assert.Contains(error.Frames, frame => frame.File == "proxy-base.py");
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        var shadow = new PyClass("mappingproxy");
        Assert.Same(shadow, PyRuntimeBases.Validate(shadow));
        Assert.IsType<PyClass>(PyOps.MakeClass("Allowed", [shadow], null));
    }
}
