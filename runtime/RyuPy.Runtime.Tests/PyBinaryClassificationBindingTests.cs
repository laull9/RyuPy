// ASCII分类的direct/saved/root窄绑定、捕获身份、求值副作用和新自造错误帧；不扩通用descriptor框架。
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBinaryClassificationBindingTests
{
    private static readonly string[] Names = ["isalpha", "isalnum", "isdigit", "isspace", "isascii", "islower", "isupper", "istitle"];
    private static object Receiver(bool mutable) => mutable ? new PyByteArray([65]) : new PyBytes([65]);
    private static object? Route(int route, object receiver, string name, object?[] args, object?[] keywords, object?[] values)
    {
        object owner = PyObject.TypeOf(receiver);
        return route switch
        {
            0 => PyOps.CallMethod(receiver, name, args, keywords, values),
            1 => PyOps.CallValue(PyObject.GetAttr(receiver, name), args, keywords, values),
            2 => PyOps.CallMethod(owner, name, [receiver, .. args], keywords, values),
            _ => PyOps.CallValue(PyObject.GetAttr(owner, name), [receiver, .. args], keywords, values),
        };
    }

    [Fact]
    public void EveryClassificationUsesQualifiedMessagesAndKeywordsPrecedeArityOnAllRoutes()
    {
        foreach (bool mutable in new[] { false, true })
            foreach (string name in Names)
                for (int route = 0; route < 4; route++)
                {
                    object value = Receiver(mutable); string prefix = (mutable ? "bytearray." : "bytes.") + name + "()";
                    Assert.Equal(prefix + " takes no arguments (1 given)",
                        Assert.Throws<PyTypeError>(() => Route(route, value, name, [PyNone.Instance], [], [])).PyMessage);
                    Assert.Equal(prefix + " takes no arguments (2 given)",
                        Assert.Throws<PyTypeError>(() => Route(route, value, name, [1L, 2L], [], [])).PyMessage);
                    foreach (object?[] args in new object?[][] { [], [1L] })
                        Assert.Equal(prefix + " takes no keyword arguments",
                            Assert.Throws<PyTypeError>(() => Route(route, value, name, args, ["x"], [1L])).PyMessage);
                    if (value is PyByteArray array) { Assert.Equal(new byte[] { 65 }, array.Bytes); array.Append(66L); }
                }
    }

    [Fact]
    public void RootRequiresPositionalSelfBeforeKeywordsAndRejectsOtherCarriersWithoutCoercion()
    {
        int coercions = 0;
        var cls = new PyClass("Trap");
        foreach (string name in new[] { "__bytes__", "__iter__", "__bool__" })
            cls.SetAttr(name, PyNativeFunctions.Create(name, ["self"], [null], 1, _ => { coercions++; return true; }));
        var trap = new PyInstance(cls);
        var view = (PyMemoryView)PyMemoryView.Create([new PyBytes([65])], [], []);
        try
        {
            foreach (bool mutable in new[] { false, true })
                foreach (string name in Names)
                {
                    PyType owner = PyType.Get(mutable ? "bytearray" : "bytes");
                    object root = PyObject.GetAttr(owner, name)!;
                    foreach (object?[] keywords in new object?[][] { [], ["self"] })
                        Assert.Equal($"unbound method {owner.Name}.{name}() needs an argument",
                            Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [], keywords,
                                keywords.Length == 0 ? [] : [Receiver(mutable)])).PyMessage);
                    foreach (object wrong in new object[] { PyNone.Instance, "A", 65L, Receiver(!mutable), view, trap })
                    {
                        string message = $"descriptor '{name}' for '{owner.Name}' objects doesn't apply to a '{PyRepr.TypeName(wrong)}' object";
                        Assert.Equal(message, Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [wrong], [], [])).PyMessage);
                        Assert.Equal(message, Assert.Throws<PyTypeError>(() => PyOps.CallMethod(owner, name, [wrong, 1L], ["x"], [2L])).PyMessage);
                    }
                }
        }
        finally { view.Release(); }
        Assert.Equal(0, coercions);
    }

    [Fact]
    public void EmptyStarExpansionMutatesReceiverBeforeScanOnAllRoutes()
    {
        for (int route = 0; route < 4; route++)
        {
            var value = new PyByteArray([65]); int calls = 0;
            var iterator = new EmptyIterator(() => { calls++; value.SetItem(0L, 49L); value.Append(50L); });
            Assert.Equal(false, Route(route, value, "isalpha", [PyOps.StarArg(iterator)], [PyNone.Instance], [new PyDict()]));
            Assert.Equal(1, calls); value.Append(51L); Assert.Equal(new byte[] { 49, 50, 51 }, value.Bytes);
        }
    }

    [Fact]
    public void SavedBoundAndRootKeepOriginalObjectsAfterLocalRebinding()
    {
        var old = new PyByteArray([65]); object receiver = old;
        object bound = PyObject.GetAttr(receiver, "isalpha")!;
        receiver = new PyByteArray([49]);
        object owner = PyType.Get("bytes"); object root = PyObject.GetAttr(owner, "isalpha")!;
        owner = PyType.Get("bytearray");
        Assert.Same(old, PyObject.GetAttr(bound, "__self__"));
        Assert.Equal(true, PyOps.CallValue(bound, [], [], []));
        Assert.Equal(false, PyOps.CallMethod(receiver, "isalpha", [], [], []));
        Assert.Same(PyType.Get("bytes"), PyObject.GetAttr(root, "__objclass__"));
        Assert.Equal(true, PyOps.CallValue(root, [new PyBytes([65])], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [receiver], [], []));
        Assert.Equal(true, PyOps.CallMethod(owner, "isalpha", [old], [], []));
    }

    [Fact]
    public void SamePicSiteKeepsOrdinaryMethodsAndBinaryCallsSeparate()
    {
        var cls = new PyClass("Ordinary");
        cls.SetAttr("isalpha", PyNativeFunctions.Create("isalpha", ["self"], [null], 1, _ => "ordinary-body"));
        var ordinary = new PyInstance(cls);
        for (int pass = 0; pass < 4; pass++)
        {
            Assert.Equal("ordinary-body", PyPic.CallMethod0(1761, ordinary, "isalpha"));
            Assert.Equal(true, PyPic.CallMethod0(1761, new PyBytes([65]), "isalpha"));
            Assert.Equal(false, PyPic.CallMethod0(1761, new PyByteArray([49]), "isalpha"));
        }
        cls.SetAttr("isalpha", PyNativeFunctions.Create("isalpha", ["self"], [null], 1, _ => "updated-body"));
        Assert.Equal("updated-body", PyPic.CallMethod0(1761, ordinary, "isalpha"));
    }

    [Fact]
    public void NewOwnedFailuresHaveCallerFramesAndAmbientContext()
    {
        long depth = PyTraceback.PushFrame("binary_classification.py", "caller");
        var ambient = new PyRuntimeError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.SetLine(41);
            for (int route = 0; route < 4; route++)
            {
                var error = Assert.Throws<PyTypeError>(() => Route(route, Receiver(false), "isalpha", [1L], [], []));
                Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("binary_classification.py", "caller", 41), Assert.Single(error.Frames));
            }
            object root = PyObject.GetAttr(PyType.Get("bytes"), "isalpha")!;
            foreach (object?[] args in new object?[][] { [], [PyNone.Instance] })
            {
                var error = Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, args, ["x"], [1L]));
                Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("binary_classification.py", "caller", 41), Assert.Single(error.Frames));
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void StarExpansionUserErrorIsNotRewrappedOrPreparedByClassification()
    {
        long depth = PyTraceback.PushFrame("binary_classification.py", "argument");
        PyTraceback.SetLine(7);
        var marker = new PyValueError("argument-body"); var cause = new PyKeyError("cause");
        marker.WithCause(cause); PyErrors.PrepareRaise(marker); var frames = marker.Frames;
        var ambient = new PyRuntimeError("ambient"); object? previous = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.SetLine(41);
            for (int route = 0; route < 4; route++)
            {
                var value = new PyByteArray([65]);
                var iterator = new EmptyIterator(() => { value.SetItem(0L, 49L); throw marker; });
                Assert.Same(marker, Assert.Throws<PyValueError>(() => Route(route, value, "isalpha", [PyOps.StarArg(iterator)], [], [])));
                Assert.Same(frames, marker.Frames); Assert.Null(marker.Context);
                Assert.Same(cause, marker.Cause); Assert.True(marker.SuppressContextFlag); value.Append(50L);
            }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void NewRootSurfaceDoesNotOpenOtherMethodsUnicodeOrMemoryviews()
    {
        foreach (string owner in new[] { "bytes", "bytearray" })
            // decode已有独立四入口合同；其余未实现根方法仍必须拒绝。
            foreach (string name in new[] { "lower", "isdecimal", "isnumeric" })
                Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(PyType.Get(owner), name));
        object root = PyObject.GetAttr(PyType.Get("bytes"), "isalpha")!;
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(root, "__get__"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr("A", "isalpha"));
        var view = (PyMemoryView)PyMemoryView.Create([new PyBytes([65])], [], []);
        try { Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(view, "isalpha")); }
        finally { view.Release(); }
    }

    private sealed class EmptyIterator(Action action) : IPyIterator
    {
        private bool _done;
        public bool MoveNext()
        {
            if (_done) return false;
            _done = true; action(); return false;
        }
        public object? Current() => throw new InvalidOperationException("empty iterator has no current item");
    }
}
