// 搜索bool-index的实际过滤与完整记录；源码输出合同仍由独立pending fixture验收。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextSearchWarningTests
{
    private const string Message = "__index__ returned non-int (type bool).  The ability to return an instance of a strict subclass of int is deprecated, and may be removed in a future version of Python.";
    private static readonly string[] Methods = ["count", "find", "rfind", "index", "rindex"];
    private static PyFunction Fn(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("search_warning", body);
    private static PyInstance Index(Func<object?> body)
    {
        var cls = new PyClass("BoolIndex"); cls.SetAttr("__index__", Fn(_ => body())); return new PyInstance(cls);
    }
    private static object? Warn(string name, object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyWarnings.CreateAttributes()[name], args, names ?? [], values ?? []);
    private static object? Method(object? value, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);

    [Fact]
    public void StartAndEndBoolReturnsKeepFullWarningRecordAndErrorStopsEnd()
    {
        const string file = "text_search_warning_frame.py";
        PyModules.Register("text_search_warning_frame", "", file, () => { }, _ => PyNone.Instance, (_, _, _) => { });
        long depth = PyTraceback.PushFrame(file, "caller"); PyTraceback.SetLine(23);
        object? context = Warn("catch_warnings", [], ["record"], [true]);
        var records = Assert.IsType<PyList>(Method(context, "__enter__"));
        try
        {
            var events = new List<string>();
            var start = Index(() => { events.Add("start"); return true; });
            var end = Index(() => { events.Add("end"); return true; });
            foreach (string name in Methods)
            {
                records.Items.Clear(); events.Clear(); Warn("simplefilter", ["always"]);
                Assert.Equal(1L, PyOps.CallMethod("aa", name, ["a", start, 2L], [], []));
                Assert.Equal(name == "count" ? 1L : 0L, PyOps.CallMethod("aa", name, ["a", 0L, end], [], []));
                Assert.Equal(new[] { "start", "end" }, events); Assert.Equal(2, records.Items.Count);
                foreach (object? record in records.Items)
                {
                    Assert.Same(PyType.Get("DeprecationWarning"), PyObject.GetAttr(record, "category"));
                    Assert.Equal(Message, Assert.IsType<PyDeprecationWarning>(PyObject.GetAttr(record, "message")).PyMessage);
                    Assert.Equal(file, PyObject.GetAttr(record, "filename")); Assert.Equal(23L, PyObject.GetAttr(record, "lineno"));
                }
                Assert.Equal(1L, PyOps.CallMethod("aa", name, ["a", true, 2L], [], [])); Assert.Equal(2, records.Items.Count);
                Warn("simplefilter", ["ignore"]);
                Assert.Equal(1L, PyOps.CallMethod("aa", name, ["a", start, 2L], [], [])); Assert.Equal(2, records.Items.Count);
                events.Clear(); Warn("simplefilter", ["error"]);
                Assert.Equal(Message, Assert.Throws<PyDeprecationWarning>(() => PyOps.CallMethod("aa", name, ["a", start, end], [], [])).PyMessage);
                Assert.Equal(new[] { "start" }, events); Assert.Equal(2, records.Items.Count);
            }
        }
        finally { Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void StderrReentryKeepsOuterArgumentsAndWriteExceptionIdentity()
    {
        const string file = "text_search_stderr_frame.py";
        PySystem.Initialize(file, []);
        PyModules.Register("text_search_stderr_frame", "", file, () => { }, _ => PyNone.Instance, (_, _, _) => { });
        long depth = PyTraceback.PushFrame(file, "caller"); PyTraceback.SetLine(31);
        var sys = PyModules.Import("sys"); object? previous = sys.GetAttr("stderr");
        object? context = Warn("catch_warnings", [], ["action"], ["always"]); Method(context, "__enter__");
        try
        {
            for (int i = 0; i < Methods.Length; i++)
            {
                int site = 3880 + i; string name = Methods[i]; var events = new List<string>();
                var start = Index(() => { events.Add("start"); return true; });
                var end = Index(() => { events.Add("end"); return 3L; });
                var marker = new PyValueError("stderr marker"); PyErrors.PrepareRaise(marker); var frames = marker.Frames;
                bool fail = false;
                var sink = new Sink(text =>
                {
                    Assert.Equal(file + ":31: DeprecationWarning: " + Message + "\n", text);
                    events.Add("write"); Assert.Equal(1L, PyPic.CallMethod3(site, "zz", name, "z", 1L, 2L));
                    if (fail) throw marker;
                });
                sys.SetAttr("stderr", sink);
                Assert.Equal(name == "count" ? 2L : name is "rfind" or "rindex" ? 2L : 1L,
                    PyPic.CallMethod3(site, "aaa", name, "a", start, end));
                Assert.Equal(new[] { "start", "write", "end" }, events);
                events.Clear(); fail = true;
                Assert.Same(marker, Assert.Throws<PyValueError>(() => PyPic.CallMethod3(site, "aaa", name, "a", start, end)));
                Assert.Equal(new[] { "start", "write" }, events); Assert.Same(frames, marker.Frames);
            }
        }
        finally
        {
            sys.SetAttr("stderr", previous); Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance);
            PyTraceback.PopTo(depth);
        }
    }

    private sealed class Sink(Action<string> write) : PyNativeObject
    {
        public override string TypeName => "SearchWarningSink";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, args =>
            {
                string text = (string)args[0]!; write(text); return (long)text.Length;
            }) : throw Missing(name);
    }
}
