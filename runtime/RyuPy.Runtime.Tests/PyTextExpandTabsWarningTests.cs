// expandtabs 的 bool-index 警告保留既有过滤、调用帧、stderr 重入和异常身份；真实源码位置另验。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextExpandTabsWarningTests
{
    private const string Message = "__index__ returned non-int (type bool).  The ability to return an instance of a strict subclass of int is deprecated, and may be removed in a future version of Python.";
    private static PyFunction Fn(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("expandtabs_warning", body);
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("BoolIndex"); cls.SetAttr("__index__", Fn(_ => body())); return new PyInstance(cls);
    }
    private static object? Expand(string text, object? size) => PyOps.CallMethod(text, "expandtabs", [size], [], []);
    private static object? WarnCall(string name, object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyWarnings.CreateAttributes()[name], args, names ?? [], values ?? []);
    private static object? Method(object? value, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);

    [Fact]
    public void BoolReturnUsesActualFiltersAndDirectBoolNeverWarns()
    {
        const string file = "expandtabs_warning_frame.py";
        PyModules.Register("expandtabs_warning_frame", "", file, () => { }, _ => PyNone.Instance, (_, _, _) => { });
        long depth = PyTraceback.PushFrame(file, "caller"); PyTraceback.SetLine(23);
        object? context = WarnCall("catch_warnings", [], ["record"], [true]);
        var records = Assert.IsType<PyList>(Method(context, "__enter__"));
        try
        {
            int count = 0; var size = Index(() => { count++; return true; });
            WarnCall("simplefilter", ["always"]);
            Assert.Equal("a ", Expand("a\t", size)); Assert.Equal(1, count); Assert.Single(records.Items);
            object? record = records.Items[0];
            Assert.Same(PyType.Get("DeprecationWarning"), PyObject.GetAttr(record, "category"));
            Assert.Equal(Message, Assert.IsType<PyDeprecationWarning>(PyObject.GetAttr(record, "message")).PyMessage);
            Assert.Equal(file, PyObject.GetAttr(record, "filename")); Assert.Equal(23L, PyObject.GetAttr(record, "lineno"));
            Assert.Equal("a ", Expand("a\t", true)); Assert.Equal("a", Expand("a\t", false)); Assert.Single(records.Items);
            WarnCall("simplefilter", ["ignore"]); Assert.Equal("a ", Expand("a\t", size)); Assert.Single(records.Items);
            WarnCall("simplefilter", ["error"]);
            Assert.Equal(Message, Assert.Throws<PyDeprecationWarning>(() => Expand("", size)).PyMessage);
            Assert.Equal(Message, Assert.Throws<PyDeprecationWarning>(() => Expand("plain", size)).PyMessage);
            Assert.Equal(4, count); Assert.Single(records.Items);
        }
        finally { Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void RealStderrReentryKeepsOuterTextAndTabsizeAndWriteErrorIdentity()
    {
        const int site = 7392;
        PySystem.Initialize("expandtabs_stderr.py", []);
        var sys = PyModules.Import("sys"); object? previous = sys.GetAttr("stderr");
        object? context = WarnCall("catch_warnings", [], ["action"], ["always"]); Method(context, "__enter__");
        int calls = 0; var size = Index(() => { calls++; return true; });
        string output = "";
        var sink = new Sink(text =>
        {
            output = text; Assert.Equal("inner   ", PyPic.CallMethod1(site, "inner\t", "expandtabs", 4L));
        });
        try
        {
            sys.SetAttr("stderr", sink);
            Assert.Equal("outer ", PyPic.CallMethod1(site, "outer\t", "expandtabs", size)); Assert.Equal(1, calls);
            Assert.Contains("DeprecationWarning: " + Message + "\n", output);
            var marker = new PyValueError("stderr marker"); PyErrors.PrepareRaise(marker); var frames = marker.Frames;
            sink.Write = _ => throw marker;
            Assert.Same(marker, Assert.Throws<PyValueError>(() => Expand("a\t", size))); Assert.Equal(2, calls);
            Assert.Same(frames, marker.Frames);
        }
        finally { sys.SetAttr("stderr", previous); Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); }
    }

    private sealed class Sink(Action<string> write) : PyNativeObject
    {
        internal Action<string> Write = write;
        public override string TypeName => "ExpandTabsSink";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, args =>
            {
                string text = (string)args[0]!; Write(text); return (long)text.Length;
            }) : throw Missing(name);
    }
}
