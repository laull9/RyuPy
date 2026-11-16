using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>mappingproxy 隔离 A 阶段八组内部合同；不注册 types，不冒充迭代/运算/消费者已闭合。</summary>
[Collection("生成器帧栈")]
public class PyMappingProxyCoreTests
{
    private static PyMappingProxy New(object? mapping) =>
        Assert.IsType<PyMappingProxy>(PyMappingProxy.Create([mapping], [], []));
    private static object? Method(object value, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);
    private static PyFunction Slot(string name, string[] parameters, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, new object?[parameters.Length], parameters.Length, body);

    [Fact]
    public void ConstructorChecksCountFirstAndNeverCallsUserSlots()
    {
        var dictionary = new PyDict();
        Assert.IsType<PyMappingProxy>(PyMappingProxy.Type.Invoke([], ["mapping"], [dictionary]));
        Assert.Equal("mappingproxy() missing required argument 'mapping' (pos 1)",
            Assert.Throws<PyTypeError>(() => PyMappingProxy.Create([], ["wrong"], [dictionary])).Message);
        Assert.Equal("mappingproxy() takes at most 1 argument (3 given)",
            Assert.Throws<PyTypeError>(() => PyMappingProxy.Create([dictionary, dictionary], ["wrong"], [1L])).Message);
        Assert.Throws<PyTypeError>(() => PyMappingProxy.Create([dictionary], ["mapping"], [dictionary]));
        Assert.Throws<PyTypeError>(() => New(new PyList()));
        Assert.Throws<PyTypeError>(() => New(new PyTuple([])));
        Assert.Equal("mappingproxy() argument must be a mapping, not object",
            Assert.Throws<PyTypeError>(() => New(PyBaseObject.Create([], []))).Message);
        Assert.Throws<PyNotImplementedError>(() => New(new PyRange(0, 2, 1)));
        int calls = 0;
        var cls = new PyClass("ProxyInput");
        cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], _ => { calls++; return 4L; }));
        PyMappingProxy proxy = New(new PyInstance(cls));
        Assert.Equal(0, calls); Assert.Equal(4L, PyOps.GetItem(proxy, "x")); Assert.Equal(1, calls);
        var noneSlot = new PyClass("ProxyNoneSlot"); noneSlot.SetAttr("__getitem__", PyNone.Instance);
        PyMappingProxy none = New(new PyInstance(noneSlot));
        Assert.Throws<PyTypeError>(() => PyOps.GetItem(none, "x"));
        var fake = new PyInstance(new PyClass("ProxyFake"));
        PyObject.SetAttr(fake, "__getitem__", Slot("fake", ["key"], _ => 9L));
        Assert.Throws<PyTypeError>(() => New(fake));
    }

    [Fact]
    public void TypeRegistrationKeepsUnifiedEnumProxyIdentityConsistent()
    {
        var cls = (PyClass)PyEnum.Create("ProxyEnum", [PyEnum.CreateAttributes()["Enum"]], PyNone.Instance);
        cls.SetAttr("ONE", 1L); PyEnum.Finalize(cls, ["ONE"]);
        Assert.True(PyEnum.TryGetClassAttr(cls, "__members__", out object? old));
        Assert.IsType<PyMappingProxy>(old);
        PyNativeType type = PyMappingProxy.Type;
        Assert.Same(type, PyObject.TypeOf(old)); Assert.True(type.Accepts(old));
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [old, type], [], []));
        PyMappingProxy proxy = New(new PyDict());
        Assert.Same(type, PyObject.TypeOf(proxy)); Assert.Same(type, PyObject.GetAttr(proxy, "__class__"));
        Assert.True(type.Accepts(proxy)); Assert.Equal("mappingproxy", type.GetAttr("__name__"));
        Assert.Equal("builtins", type.GetAttr("__module__")); Assert.Equal("<class 'mappingproxy'>", type.ToString());
        Assert.Equal(false, PyOps.CallBuiltin("isinstance", [proxy, PyType.Get("dict")], [], []));
        Assert.False((object)proxy is IPyNativeMapping);
        var external = new PyNativeType("ProxyExternal", "example", null, _ => false);
        Assert.Equal("<class 'example.ProxyExternal'>", external.ToString());
        // 统一真实载体后仍保只读行为，不能只改短名注册骗过类型检查。
        Assert.NotNull(PyOps.GetItem(old, "ONE"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(old, "update"));
    }

    [Fact]
    public void DictReadsViewsAndCopiesKeepLiveAndShallowAliases()
    {
        var dictionary = new PyDict(); var shared = new PyList(); dictionary.Set("a", shared);
        PyMappingProxy proxy = New(dictionary);
        var keys = Assert.IsType<PyDictView>(Method(proxy, "keys"));
        var values = Assert.IsType<PyDictView>(Method(proxy, "values"));
        var copy = Assert.IsType<PyDict>(Method(proxy, "copy"));
        Assert.Same(shared, copy.Get("a")); Assert.NotSame(dictionary, copy);
        dictionary.Set("b", 2L); Assert.Equal(2, keys.Count); Assert.Equal(2, values.Count);
        Assert.Equal(2L, PyOps.Len(proxy)); Assert.True(PyOps.Truthy(proxy));
        dictionary.Set("a", 3L); Assert.Equal(3L, PyOps.GetItem(proxy, "a"));
        Assert.Same(shared, copy.Get("a")); dictionary.Delete("b"); Assert.Equal(1, keys.Count);
        Assert.Throws<PyKeyError>(() => PyOps.GetItem(proxy, "missing"));
        Assert.Same(PyNone.Instance, Method(proxy, "get", "missing"));
        Assert.Equal(7L, Method(proxy, "get", "missing", 7L));
        Assert.Equal("{'a': 3}", proxy.PythonStr()); Assert.Equal("mappingproxy({'a': 3})", proxy.PythonRepr());
    }

    [Fact]
    public void ReadOnlySurfaceAndJsonDoNotMistakeProxyForDict()
    {
        PyMappingProxy proxy = New(new PyDict());
        Assert.Throws<PyTypeError>(() => PyOps.SetItem(proxy, "a", 1L));
        Assert.Throws<PyTypeError>(() => PyOps.DelItem(proxy, "a"));
        foreach (string name in new[] { "update", "pop", "popitem", "setdefault", "clear", "Mapping" })
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(proxy, name));
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(proxy, "x", 1L));
        Assert.Throws<PyAttributeError>(() => proxy.DelAttr("x"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyJsonModule.CreateAttributes()["dumps"], [proxy], [], []));
        Assert.IsAssignableFrom<IPyIterator>(Method(proxy, "__iter__"));
    }

    [Fact]
    public void DefaultDictReadsPreserveFactoryAndGetDifference()
    {
        int calls = 0;
        object factory = PyNativeFunctions.Create("factory", [], [], 0, _ => { calls++; return 8L; });
        object dictionary = PyOps.CallValue(PyCollections.CreateAttributes()["defaultdict"], [factory], [], [])!;
        PyMappingProxy proxy = New(dictionary);
        Assert.Same(PyNone.Instance, Method(proxy, "get", "missing")); Assert.Equal(0, calls);
        Assert.Equal(8L, PyOps.GetItem(proxy, "missing")); Assert.Equal(1, calls);
        Assert.Equal(8L, Method(proxy, "get", "missing")); Assert.Equal(1, calls);
        Assert.IsType<PyDefaultDict>(Method(proxy, "copy"));
        object counter = PyOps.CallValue(PyCollections.CreateAttributes()["Counter"], [], [], [])!;
        Assert.Equal(0L, PyOps.GetItem(New(counter), "missing"));
    }

    [Fact]
    public void UserMethodsReturnOriginalValuesAndAreLookedUpEachTime()
    {
        var marker = new object(); var cls = new PyClass("ProxyMethods");
        cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], _ => 1L));
        int calls = 0;
        cls.SetAttr("get", Slot("get", ["self", "key", "fallback"], args =>
        {
            calls++; Assert.Equal("k", args[1]); Assert.Same(PyNone.Instance, args[2]); return marker;
        }));
        foreach (string name in new[] { "keys", "values", "items", "copy" })
            cls.SetAttr(name, Slot(name, ["self"], _ => marker));
        var instance = new PyInstance(cls); PyMappingProxy proxy = New(instance);
        Assert.Same(marker, Method(proxy, "get", "k")); Assert.Equal(1, calls);
        foreach (string name in new[] { "keys", "values", "items", "copy" })
            Assert.Same(marker, Method(proxy, name));
        cls.SetAttr("get", Slot("get", ["self", "key", "fallback"], args => args[2]));
        Assert.Equal(19L, Method(proxy, "get", "k", 19L)); Assert.Equal(1, calls);
        PyMappingProxy text = New("a😀");
        Assert.Equal(2L, PyOps.Len(text)); Assert.Equal("😀", PyOps.GetItem(text, 1L));
        Assert.Throws<PyAttributeError>(() => Method(text, "get", 0L));
    }

    [Fact]
    public void CallbackErrorsKeepOriginalIdentityFramesAndContext()
    {
        var original = new PyValueError("user failure"); var active = new PyRuntimeError("outer");
        object? previous = PyErrors.EnterHandler(active);
        long depth = PyTraceback.PushFrame("proxy-test.py", "caller");
        PyStackFrame[]? frames = null;
        var cls = new PyClass("ProxyRaises");
        object? Fail(object?[] _)
        {
            long inner = PyTraceback.PushFrame("callback.py", "reader");
            try { PyErrors.PrepareRaise(original); frames = original.Frames; throw original; }
            finally { PyTraceback.PopTo(inner); }
        }
        cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], Fail));
        cls.SetAttr("get", Slot("get", ["self", "key", "fallback"], Fail));
        try
        {
            PyMappingProxy proxy = New(new PyInstance(cls));
            Assert.Same(original, Assert.Throws<PyValueError>(() => PyOps.GetItem(proxy, "k")));
            Assert.Same(frames, original.Frames); Assert.Same(active, original.Context);
            Assert.Same(original, Assert.Throws<PyValueError>(() => Method(proxy, "get", "k")));
            Assert.Same(frames, original.Frames); Assert.Same(active, original.Context);
            Assert.Contains(original.Frames, frame => frame.File == "callback.py");
            PyTypeError own = Assert.Throws<PyTypeError>(() => PyMappingProxy.Create([], [], []));
            Assert.Same(active, own.Context); Assert.Contains(own.Frames, frame => frame.File == "proxy-test.py");
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void HashDelegatesWithoutCachingAndNestedProxyKeepsItsLayer()
    {
        int calls = 0; var cls = new PyClass("ProxyHash");
        cls.SetAttr("__getitem__", Slot("__getitem__", ["self", "key"], _ => 5L));
        cls.SetAttr("__hash__", Slot("__hash__", ["self"], _ => { calls++; return 123L; }));
        PyMappingProxy proxy = New(new PyInstance(cls));
        Assert.Equal(123L, PyHash.Of(proxy)); Assert.Equal(123L, PyHash.Of(proxy)); Assert.Equal(2, calls);
        var dictionary = new PyDict(); dictionary.Set("a", 1L);
        PyMappingProxy inner = New(dictionary); PyMappingProxy outer = New(inner);
        Assert.NotSame(inner, outer); Assert.Same(PyMappingProxy.Type, PyObject.TypeOf(outer));
        Assert.Equal("mappingproxy(mappingproxy({'a': 1}))", outer.PythonRepr());
        dictionary.Set("a", 2L); Assert.Equal(2L, PyOps.GetItem(outer, "a"));
        Assert.Equal("unhashable type: 'dict'", Assert.Throws<PyTypeError>(() => PyHash.Of(outer)).Message);
        Assert.Equal(PyHash.Of("abc"), PyHash.Of(New("abc")));
    }
}
