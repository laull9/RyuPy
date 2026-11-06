// 默认帮助公开入口专测；长尾formatter协议不计入支持范围。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyOptparseHelpTests : IDisposable
{
    private readonly object _environment;
    private readonly object? _oldColumns;
    public PyOptparseHelpTests()
    {
        PySystem.Initialize("optparse-help-tests.py", []); PyNativeModules.Register();
        _environment = PyModules.Import("os").GetAttr("environ")!;
        _oldColumns = PyOps.CallMethod(_environment, "get", ["COLUMNS"], [], []);
        Columns("80");
    }
    public void Dispose()
    {
        if (_oldColumns is null or PyNone) PyOps.CallMethod(_environment, "__delitem__", ["COLUMNS"], [], []);
        else Columns((string)_oldColumns);
    }
    private void Columns(string value) => PyOps.CallMethod(_environment, "__setitem__", ["COLUMNS", value], [], []);
    private static readonly Dictionary<string, object?> Module = PyOptparse.CreateAttributes();
    private static object Parser(string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(Module["OptionParser"], [], names ?? ["prog", "add_help_option"], values ?? ["demo", false])!;
    private static object? Call(object target, string name, object?[]? args = null, string[]? names = null, object?[]? values = null) =>
        PyOps.CallMethod(target, name, args ?? [], names ?? [], values ?? []);
    private static object Add(object parser, object?[] flags, string[] names, object?[] values) => Call(parser, "add_option", flags, names, values)!;
    private static string Help(object parser) => Assert.IsType<string>(Call(parser, "format_help"));
    private sealed class Stream(Func<object?, object?> write) : PyNativeObject
    {
        public override string TypeName => "help_test_stream";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, a => write(a[0])) : throw Missing(name);
    }

    private sealed class Mapping(Func<object?> read) : PyNativeObject
    {
        public override string TypeName => "help_test_mapping";
        public override object? GetAttr(string name) => name == "__getitem__"
            ? PyNativeFunctions.Create(name, ["key"], [null], 1, _ => read()) : throw Missing(name);
    }
    [Fact]
    public void DefaultHelpAndVersionOrderUseExactPadding()
    {
        object parser = Parser(["prog", "version"], ["demo", "%prog 1"]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  --version   show program's version number and exit\n  -h, --help  show this help message and exit\n", Help(parser));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(parser, "formatter"));
    }
    [Fact]
    public void SmallWidthWrapsLongWordsAndHyphensAtCodepoints()
    {
        Columns("26"); object parser = Parser();
        Add(parser, ["-x"], ["help"], ["alpha-beta-gamma abcdefghijklmnopqrstuvwxyz"]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X\n    alpha-beta-gamma abc\n    defghijklmnopqrstuvw\n    xyz\n", Help(parser));
    }
    [Fact]
    public void UnicodeMetavarAndDescriptionUseRealWrapWhileImplicitUpperIsExplicitBoundary()
    {
        object parser = Parser(["prog", "add_help_option", "description", "epilog"], ["demo", false, "Desc %prog\t😀 text\nsecond line long-long-word", "End %prog\nnext"]);
        Add(parser, ["-x", "--example-long"], ["metavar", "default", "help"], ["值😀", 7L, "help %prog %default\tword-word\nnext and abcdefghijklmnopqrstuvwxyz"]);
        Add(parser, ["-z"], ["action", "help"], ["store_true", ""]);
        Assert.Equal("Usage: demo [options]\n\nDesc demo       😀 text second line long-long-word\n\nOptions:\n  -x 值😀, --example-long=值😀\n                        help %prog 7    word-word next and\n                        abcdefghijklmnopqrstuvwxyz\n  -z                    \n\nEnd %prog next\n", Help(parser));
        object unicode = Parser(); object option = Add(unicode, ["--straße"], ["help"], ["value"]);
        Assert.Throws<PyNotImplementedError>(() => Help(unicode));
        PyObject.SetAttr(option, "metavar", "STRASSE"); Assert.Contains("--straße=STRASSE", Help(unicode));
    }
    [Fact]
    public void SuppressedOptionsStillDetermineColumnsAndKeepHeading()
    {
        object parser = Parser();
        Add(parser, ["--very-long-hidden-name"], ["help"], [Module["SUPPRESS_HELP"]]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n", Help(parser));
        Add(parser, ["-x"], ["help"], ["visible"]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X                  visible\n", Help(parser));
    }
    [Fact]
    public void EmptyHelpRetainsPaddingAndTruthyWhitespaceRaisesIndexError()
    {
        object parser = Parser(); object option = Add(parser, ["-x"], ["help"], [null]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X  \n", Help(parser));
        PyObject.SetAttr(option, "help", ""); Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X  \n", Help(parser));
        PyObject.SetAttr(option, "help", "   "); Assert.Throws<PyIndexError>(() => Help(parser));
        object suppressed = Parser(["usage", "prog", "add_help_option"], [Module["SUPPRESS_USAGE"], "demo", false]);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(suppressed, "usage")); Assert.Equal("", Help(suppressed));
    }
    [Fact]
    public void ColumnsAreSnapshotFromPythonMappingNotLaterProcessEnvironment()
    {
        Columns("26"); object parser = Parser(); Add(parser, ["-x"], ["help"], ["abcdefghijklmnopqrstuvwxyz"]);
        string narrow = Help(parser); Columns("80"); Assert.Equal(narrow, Help(parser));
        Columns("bad"); object fallback = Parser(); Add(fallback, ["-x"], ["help"], ["abcdefghijklmnopqrstuvwxyz"]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X  abcdefghijklmnopqrstuvwxyz\n", Help(fallback));
        Columns("0"); object tiny = Parser(); Add(tiny, ["-x"], ["help"], ["abcdefghijklm"]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X\n    abcdefghijk\n    lm\n", Help(tiny));
        var os = PyModules.Import("os"); object? original = os.GetAttr("environ");
        var ambient = new PyRuntimeError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.CaptureDepth();
        try
        {
            var missing = new PyKeyError("COLUMNS");
            os.SetAttr("environ", new Mapping(() => { PyTraceback.PushFrame("columns.py", "getitem"); throw missing; }));
            Assert.Equal("Usage: demo [options]\n\n", Help(Parser()));
            Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
            Assert.Contains("getitem", PyTraceback.Format(missing));
            var invalid = new PyValueError("invalid columns");
            var conversion = (PyClass)PyOps.MakeClass("Columns", [], null);
            conversion.SetAttr("__int__", new PyFunction("int", PySignature.Create("int", ["self"], ["pos"], ["0"]),
                (_, _, _, _) => { PyTraceback.PushFrame("columns.py", "convert"); throw invalid; }, []));
            os.SetAttr("environ", new Mapping(() => conversion.Invoke([], [], [])));
            Assert.Equal("Usage: demo [options]\n\n", Help(Parser()));
            Assert.Equal(depth, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
            Assert.Contains("convert", PyTraceback.Format(invalid));
        }
        finally { os.SetAttr("environ", original); PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(token); }

    }
    [Fact]
    public void DefaultsAreLiveAndConversionRunsWithoutPlaceholderBeforeLaterOptions()
    {
        object parser = Parser(); object first = Add(parser, ["-x"], ["help"], ["plain"]);
        object second = Add(parser, ["-y"], ["help", "default"], ["%default", "old"]);
        int calls = 0; var cls = (PyClass)PyOps.MakeClass("Default", [], null);
        cls.SetAttr("__str__", new PyFunction("str", PySignature.Create("str", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { calls++; Call(parser, "set_default", ["y", "new"]); PyObject.SetAttr(second, "metavar", "LATER"); return "unused"; }, []));
        Call(parser, "set_default", ["x", cls.Invoke([], [], [])]);
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X  plain\n  -y Y  new\n", Help(parser)); Assert.Equal(1, calls);
        PyObject.SetAttr(first, "default", "not-live");
        Assert.Contains("-y LATER", Help(parser)); Assert.Equal(2, calls);
        var options = Assert.IsType<PyList>(PyObject.GetAttr(parser, "option_list"));
        options.Items.Remove(second);
        cls.SetAttr("__str__", new PyFunction("str", PySignature.Create("str", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { options.Items.Add(second); return "unused"; }, []));
        Assert.Equal("Usage: demo [options]\n\nOptions:\n  -x X  plain\n  -y LATER\n        new\n", Help(parser));

        cls.SetAttr("__str__", new PyFunction("str", PySignature.Create("str", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => Help(parser), []));
        Assert.Throws<PyNotImplementedError>(() => Help(parser));
        Call(parser, "set_default", ["x", "safe"]);
        Assert.Contains("plain", Help(parser));

    }
    [Fact]
    public void PrintHelpWritesOnceAndIgnoresReturnWithoutClosing()
    {
        object parser = Parser(); var writes = new List<object?>();
        var stream = new Stream(value => { writes.Add(value); return 999L; });
        Call(parser, "print_help", [stream]); Assert.Single(writes); Assert.Equal(Help(parser), writes[0]);
        var failure = new PyValueError("write failed");
        Assert.Same(failure, Assert.Throws<PyValueError>(() => Call(parser, "print_help", [new Stream(_ => throw failure)])));
    }
    [Fact]
    public void ProgExpansionAndCurrentStdoutDoNotAddNewlines()
    {
        object parser = Parser(["prog", "add_help_option", "usage", "description", "epilog"], ["demo", false, "%prog X", "D %prog", "E %prog"]);
        Add(parser, ["-x"], ["help"], ["H %prog %default"]);
        string expected = "Usage: demo X\n\nD demo\n\nOptions:\n  -x X  H %prog none\n\nE %prog\n";
        Assert.Equal(expected, Help(parser));
        var sys = PyModules.Import("sys"); object? previous = sys.GetAttr("stdout"); var writes = new List<object?>();
        try { sys.SetAttr("stdout", new Stream(value => { writes.Add(value); return null; })); Call(parser, "print_help"); }
        finally { sys.SetAttr("stdout", previous); }
        Assert.Single(writes); Assert.Equal(expected, writes[0]);
    }
}
