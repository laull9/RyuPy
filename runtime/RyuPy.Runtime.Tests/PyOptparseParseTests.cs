// optparse阶段B公开解析、剩余参数、部分状态和两层异常上下文；不声称完整帮助已完成。
using System;
using System.Collections.Generic;
using System.IO;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyOptparseParseTests : IDisposable
{
    private readonly PyModule _sys;
    private readonly object? _stderr, _stdout;
    public PyOptparseParseTests()
    {
        PySystem.Initialize("optparse-tests.py", []); PyNativeModules.Register(); _sys = PyModules.Import("sys");
        _stderr = _sys.GetAttr("stderr"); _stdout = _sys.GetAttr("stdout");
    }
    public void Dispose() { _sys.SetAttr("stderr", _stderr); _sys.SetAttr("stdout", _stdout); }
    private sealed class Stream(Action<object?> write) : PyNativeObject
    {
        public override string TypeName => "test_stream";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, a => { write(a[0]); return null; })
            : throw Missing(name);
    }
    private static readonly Dictionary<string, object?> Module = PyOptparse.CreateAttributes();
    private static object New(string name, object?[]? args = null, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(Module[name], args ?? [], names ?? [], values ?? [])!;
    private static object? Call(object target, string name, object?[]? args = null, string[]? names = null, object?[]? values = null) =>
        PyOps.CallMethod(target, name, args ?? [], names ?? [], values ?? []);
    private static object? Get(object target, string name) => PyObject.GetAttr(target, name);
    private static object Parser() => New("OptionParser", [], ["prog", "add_help_option"], ["sample", false]);
    private static PyTuple Parse(object parser, params object?[] args) => Assert.IsType<PyTuple>(Call(parser, "parse_args", [new PyList(args)]));
    private static object? Item(object sequence, long index) => PyOps.GetItem(sequence, index);
    private static void Add(object parser, string flag, string action = "store", string? type = null) =>
        Call(parser, "add_option", [flag], type is null ? ["action"] : ["action", "type"], type is null ? [action] : [action, type]);
    private static PyModule QuietSystem()
    {
        PySystem.Initialize("optparse-tests.py", []);
        var sys = PyModules.Import("sys"); sys.SetAttr("stderr", new PyStandardStream("test", null, new StringWriter())); return sys;
    }
    [Fact]
    public void InterspersedOptionsReturnNewRemainingAndDoNotMutateInput()
    {
        var p = Parser(); Add(p, "--name"); var input = new PyList(["a", "--name", "x", "b"]);
        var result = Assert.IsType<PyTuple>(Call(p, "parse_args", [input]));
        Assert.Equal("x", Get(Item(result, 0)!, "name")); Assert.Equal("['a', 'b']", PyRepr.Repr(Item(result, 1)));
        Assert.Equal(4, input.Items.Count); Assert.NotSame(Get(p, "largs"), Item(result, 1)); Assert.Empty(Assert.IsType<PyList>(Get(p, "rargs")).Items);
    }
    [Fact]
    public void DoubleDashAndDisabledInterspersingKeepActualRemainingList()
    {
        var p = Parser(); Add(p, "-v", "store_true");
        var result = Parse(p, "-", "--", "-v"); Assert.Equal("['-', '-v']", PyRepr.Repr(Item(result, 1)));
        Assert.Equal("['-v']", PyRepr.Repr(Get(p, "rargs")));
        Call(p, "disable_interspersed_args"); result = Parse(p, "operand", "-v");
        Assert.Equal("['operand', '-v']", PyRepr.Repr(Get(p, "rargs"))); Assert.Empty(Assert.IsType<PyList>(Get(p, "largs")).Items);
        Call(p, "enable_interspersed_args"); Assert.Equal(true, Get(Item(Parse(p, "operand", "-v"), 0)!, "v"));
    }
    [Fact]
    public void ShortClustersAndAttachedValueUseOneValueOptionAndUnicodeCharacters()
    {
        var p = Parser(); Add(p, "-v", "store_true"); Add(p, "-q", "store_false"); Add(p, "-n", type: "int"); Add(p, "-😀", "store_true");
        var values = Item(Parse(p, "-vqn010", "-😀"), 0)!;
        Assert.Equal(true, Get(values, "v")); Assert.Equal(false, Get(values, "q")); Assert.Equal(8L, Get(values, "n")); Assert.Equal(true, Get(values, "😀"));
        Add(p, "-🚀"); values = Item(Parse(p, "-v🚀😀tail"), 0)!;
        Assert.Equal(true, Get(values, "v")); Assert.Equal("😀tail", Get(values, "🚀"));
    }
    [Fact]
    public void LongAbbreviationExactMatchAndDashLookingValueAreNotArgparseRules()
    {
        var p = Parser(); Add(p, "--name"); Add(p, "--names");
        Assert.Equal("--other", Get(Item(Parse(p, "--name", "--other"), 0)!, "name"));
        Assert.Equal("--", Get(Item(Parse(p, "--names", "--"), 0)!, "names"));
        var q = Parser(); Add(q, "--verbose", "store_true"); Assert.Equal(true, Get(Item(Parse(q, "--verb"), 0)!, "verbose"));
        Assert.Equal("", Get(Item(Parse(p, "--name="), 0)!, "name"));
    }
    [Fact]
    public void RepeatedParseCreatesValuesButProvidedValuesIdentitySkipsDefaults()
    {
        var p = Parser(); Call(p, "add_option", ["-n"], ["type", "default"], ["int", "7"]);
        object one = Item(Parse(p), 0)!, two = Item(Parse(p), 0)!; Assert.NotSame(one, two); Assert.Equal(7L, Get(two, "n"));
        var supplied = New("Values"); Call(p, "set_default", ["n", "bad"]);
        var result = Assert.IsType<PyTuple>(Call(p, "parse_args", [new PyList(), supplied])); Assert.Same(supplied, Item(result, 0));
        Assert.Throws<PyAttributeError>(() => Get(supplied, "n")); Assert.Same(supplied, Get(p, "values"));
    }
    [Fact]
    public void BadDefaultsFailBeforePublicParseStateAndBeforeHelp()
    {
        var p = Parser(); Call(p, "add_option", ["-n"], ["type", "default"], ["int", "bad"]);
        var error = Assert.Throws<PyOptionValueError>(() => Parse(p, "--help")); Assert.IsType<PyValueError>(error.Context);
        Assert.Same(PyNone.Instance, Get(p, "rargs"));
        Assert.Throws<PyTypeError>(() => Call(p, "parse_args", [42L])); Assert.Same(PyNone.Instance, Get(p, "values"));
    }
    [Fact]
    public void NonListBuiltinsRetainSliceAndFailureTimingInsteadOfIterableCoercion()
    {
        var p = Parser(); Add(p, "-v", "store_true");
        Assert.Throws<PyAttributeError>(() => Call(p, "parse_args", [new PyTuple(["-v"])]));
        Assert.IsType<PyTuple>(Get(p, "rargs")); Assert.Empty(Assert.IsType<PyList>(Get(p, "largs")).Items);
        Assert.Throws<PyTypeError>(() => Call(p, "parse_args", [new PyTuple(["operand"])]));
        Assert.Equal("['operand']", PyRepr.Repr(Get(p, "largs")));
        Assert.Throws<PyTypeError>(() => Call(p, "parse_args", [new PyTuple([])]));
        Assert.Throws<PyTypeError>(() => Call(p, "parse_args", ["x"])); Assert.Equal("['x']", PyRepr.Repr(Get(p, "largs")));
    }
    [Fact]
    public void ParseNoneUsesCurrentSysArgvSlice()
    {
        PySystem.Initialize("sample", ["-v", "tail"]); var p = Parser(); Add(p, "-v", "store_true");
        var result = Assert.IsType<PyTuple>(Call(p, "parse_args")); Assert.Equal(true, Get(Item(result, 0)!, "v"));
        Assert.Equal("['tail']", PyRepr.Repr(Item(result, 1))); Assert.Equal(3, Assert.IsType<PyList>(PyModules.Import("sys").GetAttr("argv")).Items.Count);
    }
    [Fact]
    public void UnknownAndAmbiguousErrorsKeepPartialStateAndOriginalErrorContext()
    {
        var sys = QuietSystem(); object? previousStream = sys.GetAttr("stderr");
        try
        {
            var p = Parser(); Add(p, "-n", type: "int"); Add(p, "--name"); Add(p, "--names");
            var exit = Assert.Throws<PySystemExit>(() => Parse(p, "-n", "4", "--nam=x", "tail"));
            Assert.Equal(2L, exit.Code); Assert.IsType<PyAmbiguousOptionError>(exit.Context); Assert.Equal(4L, Get(Get(p, "values")!, "n"));
            Assert.Equal("['x', 'tail']", PyRepr.Repr(Get(p, "rargs")));
            exit = Assert.Throws<PySystemExit>(() => Parse(p, "-z")); Assert.IsType<PyBadOptionError>(exit.Context);
            Add(p, "--u😀"); Add(p, "--u\ue000");
            exit = Assert.Throws<PySystemExit>(() => Parse(p, "--u"));
            var ambiguous = Assert.IsType<PyAmbiguousOptionError>(exit.Context);
            var possibilities = Assert.IsType<PyList>(PyObject.GetAttr(ambiguous, "possibilities"));
            Assert.Equal(new object?[] { "--u\ue000", "--u😀" }, possibilities.Items);
        }
        finally { sys.SetAttr("stderr", previousStream); }
    }
    [Fact]
    public void IntegerFailureHasTwoNestedContextsAndRestoresAmbientHandler()
    {
        var sys = QuietSystem(); var p = Parser(); Add(p, "-n", type: "int");
        var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var exit = Assert.Throws<PySystemExit>(() => Parse(p, "-n", "bad"));
            var option = Assert.IsType<PyOptionValueError>(exit.Context); var integer = Assert.IsType<PyValueError>(option.Context);
            Assert.Same(outer, integer.Context); Assert.Same(outer, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }
    [Fact]
    public void ChoiceHasOptionContextButMissingAndFlagValueDoNotInventIt()
    {
        QuietSystem(); var p = Parser(); Call(p, "add_option", ["--color"], ["choices"], [new PyList(["red"])]); Add(p, "-v", "store_true"); Add(p, "--verbose", "store_true");
        var exit = Assert.Throws<PySystemExit>(() => Parse(p, "--color", "blue")); Assert.IsType<PyOptionValueError>(exit.Context); Assert.Null(exit.Context!.Context);
        exit = Assert.Throws<PySystemExit>(() => Parse(p, "--color")); Assert.Null(exit.Context);
        exit = Assert.Throws<PySystemExit>(() => Parse(p, "--verbose=x")); Assert.Null(exit.Context); Assert.Equal("['x']", PyRepr.Repr(Get(p, "rargs")));
        exit = Assert.Throws<PySystemExit>(() => Parse(p, "-v", "--color")); Assert.Null(exit.Context); Assert.Equal(true, Get(Get(p, "values")!, "v"));
    }
    [Fact]
    public void UsageVersionExitPreserveOutputAndExitCode()
    {
        var sys = QuietSystem(); var p = Parser(); Assert.Equal("Usage: sample [options]\n", Call(p, "get_usage"));
        var writes = new List<object?>(); var stream = new Stream(writes.Add);
        Call(p, "print_usage", [stream]); Assert.Equal(new object?[] { "Usage: sample [options]\n", "\n" }, writes);
        writes.Clear(); sys.SetAttr("stderr", stream);
        var missing = Assert.Throws<PySystemExit>(() => Call(p, "error", ["bad"])); Assert.Equal(2L, missing.Code);
        Assert.Equal(new object?[] { "Usage: sample [options]\n", "\n", "sample: error: bad\n" }, writes);
        Call(p, "set_usage", [Module["SUPPRESS_USAGE"]]); Assert.Equal("", Call(p, "get_usage"));
        PyObject.SetAttr(p, "version", "%prog v1"); Assert.Equal("sample v1", Call(p, "get_version"));
        object code = new PyList(); Assert.Same(code, Assert.Throws<PySystemExit>(() => Call(p, "exit", [code])).Code);
    }
    [Fact]
    public void StreamErrorSeesActiveOptionErrorAndPropagatesOriginalException()
    {
        var sys = QuietSystem(); var p = Parser(); var raised = new PyRuntimeError("write");
        PyBaseException? active = null;
        sys.SetAttr("stderr", new Stream(_ => { active = PyErrors.Current; throw (PyRuntimeError)PyErrors.PrepareRaise(raised); }));
        var outer = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(outer);
        try
        {
            Assert.Same(raised, Assert.Throws<PyRuntimeError>(() => Parse(p, "--missing")));
            Assert.IsType<PyBadOptionError>(active); Assert.Same(active, raised.Context); Assert.Same(outer, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(token); }
    }
    [Fact]
    public void ParseBinderIsStrictAndDefaultHelpExitsSuccessfully()
    {
        var p = Parser(); Assert.Throws<PyTypeError>(() => Call(p, "parse_args", [new PyList(), null, 3L]));
        Assert.Throws<PyTypeError>(() => Call(p, "parse_args", [], ["unknown"], [1L]));
        Assert.Throws<PyTypeError>(() => Call(p, "parse_args", [new PyList()], ["args"], [new PyList()]));
        var help = New("OptionParser", [], ["prog"], ["sample"]);
        var writes = new List<object?>(); _sys.SetAttr("stdout", new Stream(writes.Add));
        Assert.Equal(0L, Assert.Throws<PySystemExit>(() => Parse(help, "--help")).Code);
        string output = Assert.IsType<string>(Assert.Single(writes));
        Assert.StartsWith("Usage: sample [options]\n\nOptions:\n", output);
        Assert.Contains("-h, --help", output);
    }
}
