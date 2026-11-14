// bool索引返回值复用真实警告过滤与stderr；重入或抛错不重复求值外层索引。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringIndexWarningTests
{
    private static object? Warnings(string name, object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyWarnings.CreateAttributes()[name], args, names ?? [], values ?? []);
    private static object? Method(object? value, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(value, name), args, [], []);

    [Fact]
    public void WarningReentryAndFailurePreserveOuterIndexAndExceptionIdentity()
    {
        PySystem.Initialize("string_index_warning.py", []);
        var sys = PyModules.Import("sys"); object? previous = sys.GetAttr("stderr");
        object? context = Warnings("catch_warnings", [], ["action"], ["always"]); Method(context, "__enter__");
        int calls = 0;
        var cls = new PyClass("BoolIndex");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("__index__", _ => { calls++; return true; }));
        var key = new PyInstance(cls); string output = "";
        var sink = new Sink(text => { output = text; Assert.Equal("n", PyOps.GetItem("inner", 1L)); });
        try
        {
            sys.SetAttr("stderr", sink);
            Assert.Equal("u", PyOps.GetItem("outer", key)); Assert.Equal(1, calls);
            Assert.Contains("DeprecationWarning: __index__ returned non-int (type bool)", output);
            var marker = new PyValueError("stderr marker"); PyErrors.PrepareRaise(marker); var frames = marker.Frames;
            sink.Write = _ => throw marker;
            Assert.Same(marker, Assert.Throws<PyValueError>(() => PyOps.GetItem("", key)));
            Assert.Equal(2, calls); Assert.Same(frames, marker.Frames);
        }
        finally { sys.SetAttr("stderr", previous); Method(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance); }
    }

    private sealed class Sink(Action<string> write) : PyNativeObject
    {
        internal Action<string> Write = write;
        public override string TypeName => "StringIndexSink";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, args =>
            { string text = (string)args[0]!; Write(text); return (long)text.Length; }) : throw Missing(name);
    }
}
