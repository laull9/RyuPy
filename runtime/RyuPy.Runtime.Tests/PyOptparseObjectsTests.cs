// optparse对象/声明及模块接线测试；保留阶段A身份断言并验证后续真实入口。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyOptparseObjectsTests
{
    public PyOptparseObjectsTests() { PySystem.Initialize("optparse-objects.py", []); PyNativeModules.Register(); }
    private sealed class HelpOutput : PyNativeObject
    {
        internal readonly List<object?> Writes = new();
        public override string TypeName => "help_output";
        public override object? GetAttr(string name) => name == "write"
            ? PyNativeFunctions.Create("write", ["text"], [null], 1, a => { Writes.Add(a[0]); return null; }) : throw Missing(name);
    }
    private static readonly Dictionary<string, object?> Module = PyOptparse.CreateAttributes();
    private static object New(string name, object?[]? args = null, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(Module[name], args ?? [], names ?? [], values ?? [])!;
    private static object? Call(object target, string name, object?[]? args = null, string[]? names = null, object?[]? values = null) =>
        PyOps.CallMethod(target, name, args ?? [], names ?? [], values ?? []);
    private static object? Get(object target, string name) => PyObject.GetAttr(target, name);
    private static object Parser() => New("OptionParser", [], ["prog", "add_help_option"], ["demo", false]);

    [Fact]
    public void ObjectsHaveDistinctRealTypesAndReprIdentity()
    {
        var parser = Parser(); var option = New("Option", ["--x"]); var values = New("Values");
        var module = PyModules.Import("optparse"); Assert.Same(Module["OptionParser"], module.GetAttr("OptionParser"));
        foreach (string name in new[] { "OptParseError", "OptionError", "OptionConflictError", "OptionValueError", "BadOptionError", "AmbiguousOptionError" })
        { Assert.Same(PyType.Get(name), module.GetAttr(name)); Assert.Throws<PyNotImplementedError>(() => module.SetAttr(name, null)); }
        Assert.Throws<PyAttributeError>(() => module.GetAttr("OptionGroup"));
        Assert.Same(Module["OptionParser"], Get(parser, "__class__"));
        Assert.Same(Module["Option"], Get(option, "__class__")); Assert.Same(Module["Values"], Get(values, "__class__"));
        Assert.NotSame(Module["Option"], Module["Values"]);
        Assert.StartsWith("<Option at 0x", PyRepr.Repr(option)); Assert.Contains(": --x>", PyRepr.Repr(option));
        Assert.StartsWith("<Values at 0x", PyRepr.Repr(values));
    }
    [Fact]
    public void OmittedDefaultIsThePublicSentinelAndExplicitNoneIsDifferent()
    {
        object option = New("Option", ["-x", "--long-name"]);
        Assert.Same(Module["NO_DEFAULT"], Get(option, "default"));
        Assert.Equal("long_name", Get(option, "dest")); Assert.Equal("string", Get(option, "type")); Assert.Equal(1L, Get(option, "nargs"));
        Assert.Equal("-x/--long-name", PyRepr.Str(option)); Assert.Equal("--long-name", Call(option, "get_opt_string"));
        object explicitNone = New("Option", ["--n"], ["default"], [PyNone.Instance]);
        Assert.Same(PyNone.Instance, Get(explicitNone, "default"));
        object equalTuple = new PyTuple(["NO", "DEFAULT"]), parser = Parser();
        Call(parser, "add_option", ["--tuple"], ["default"], [equalTuple]);
        Assert.Same(equalTuple, Get(Call(parser, "get_default_values")!, "tuple"));
    }
    [Fact]
    public void BooleanDefaultsRemainNoneAndSupplementaryShortFlagIsOneCharacter()
    {
        object parser = Parser();
        Call(parser, "add_option", ["-v"], ["action"], ["store_true"]);
        Call(parser, "add_option", ["-q"], ["action"], ["store_false"]);
        object values = Call(parser, "get_default_values")!;
        Assert.Same(PyNone.Instance, Get(values, "v")); Assert.Same(PyNone.Instance, Get(values, "q"));
        object option = New("Option", ["-😀"]); Assert.Equal("😀", Get(option, "dest"));
    }
    [Fact]
    public void DefinitionErrorsHaveActualTypesAndDoNotBecomeUnsupported()
    {
        Assert.Throws<PyTypeError>(() => New("Option"));
        Assert.Throws<PyOptionError>(() => New("Option", ["---x"]));
        Assert.Throws<PyOptionError>(() => New("Option", ["--x"], ["action"], ["bogus"]));
        Assert.Throws<PyOptionError>(() => New("Option", ["--x"], ["type"], ["bogus"]));
        Assert.Throws<PyOptionError>(() => New("Option", ["--x"], ["action", "type"], ["store_true", "int"]));
        Assert.Throws<PyOptionError>(() => New("Option", ["--x"], ["type"], ["choice"]));
        Assert.Throws<PyNotImplementedError>(() => New("Option", ["--x"], ["action"], ["append"]));
    }
    [Fact]
    public void UnknownAttributesPrecedeActionChecksAndChoicesImplyType()
    {
        var error = Assert.Throws<PyOptionError>(() => New("Option", ["--x"], ["z", "action"], [1L, "bogus"]));
        Assert.Equal("option --x: invalid keyword arguments: z", PyRepr.Str(error));
        var choices = new PyList(["a", "b"]);
        object option = New("Option", ["--x"], ["choices"], [choices]);
        Assert.Same(choices, Get(option, "choices")); Assert.Equal("choice", Get(option, "type"));
        object typed = New("Option", ["--n"], ["type"], [PyType.Get("int")]); Assert.Equal("int", Get(typed, "type"));
    }
    [Fact]
    public void ConflictFailsBeforePublishingOptionOrChangingDefaults()
    {
        object parser = Parser(); object first = Call(parser, "add_option", ["-x", "--x"], ["default"], ["old"])!;
        var list = Assert.IsType<PyList>(Get(parser, "option_list"));
        var error = Assert.Throws<PyOptionConflictError>(() => Call(parser, "add_option", ["--x"], ["default"], ["new"]));
        Assert.Contains("conflicting option string(s): --x", PyRepr.Str(error));
        Assert.Single(list.Items); Assert.Same(first, list.Items[0]);
        Assert.Equal("old", Get(Call(parser, "get_default_values")!, "x"));
    }
    [Fact]
    public void ExistingOptionIsReturnedAndContainerIsActualParser()
    {
        object parser = Parser(), option = New("Option", ["--x"]);
        Assert.Same(option, Call(parser, "add_option", [option])); Assert.Same(parser, Get(option, "container"));
        Assert.Throws<PyIndexError>(() => Call(parser, "add_option"));
        Assert.Throws<PyTypeError>(() => Call(parser, "add_option", [1L]));
    }
    [Fact]
    public void DefaultsAreLiveButOptionDefaultMutationDoesNotRewriteThem()
    {
        object parser = Parser(); var marker = new PyList();
        object option = Call(parser, "add_option", ["--x"], ["default"], [marker])!;
        var defaults = Assert.IsType<PyDict>(Get(parser, "defaults")); Assert.Same(defaults, Get(parser, "defaults"));
        PyObject.SetAttr(option, "default", "changed");
        object a = Call(parser, "get_default_values")!, b = Call(parser, "get_default_values")!;
        Assert.NotSame(a, b); Assert.Same(marker, Get(a, "x")); Assert.Same(marker, Get(b, "x"));
        defaults.Set("x", 7L); Assert.Equal(7L, Get(Call(parser, "get_default_values")!, "x"));
        Call(parser, "set_defaults", [], ["x", "extra"], [8L, marker]);
        Assert.Same(marker, Get(Call(parser, "get_default_values")!, "extra"));
    }
    [Fact]
    public void ValuesCopyTheMappingButKeepValueAliasesAndSupportDeletion()
    {
        var dict = new PyDict(); var marker = new PyList(); dict.Set("x", marker);
        object values = New("Values", [dict]); var own = Assert.IsType<PyDict>(Get(values, "__dict__"));
        Assert.NotSame(dict, own); Assert.Same(marker, Get(values, "x"));
        PyObject.SetAttr(values, "extra", 2L); Assert.True(own.TryGet("extra", out _));
        PyObject.DelAttr(values, "extra"); Assert.Throws<PyAttributeError>(() => Get(values, "extra"));
        own.Set("direct", marker); Assert.Same(marker, Get(values, "direct"));
        Assert.True(PyOps.Compare(PyOps.Eq, values, own));
        own.Set("__class__", 42L); Assert.Same(Module["Values"], Get(values, "__class__"));
        own.Set("__eq__", 42L);
        Assert.Throws<PyNotImplementedError>(() => PyOps.Compare(PyOps.Eq, values, new PyDict()));
    }
    [Fact]
    public void DefaultIntegerConversionUsesOptparseRadixAndLeavesOriginalDefaults()
    {
        object parser = Parser(); Call(parser, "add_option", ["--n"], ["type", "default"], ["int", "010"]);
        Assert.Equal(8L, Get(Call(parser, "get_default_values")!, "n"));
        Assert.True(Assert.IsType<PyDict>(Get(parser, "defaults")).TryGet("n", out var raw)); Assert.Equal("010", raw);
        foreach (var pair in new[] { ("-010", -10L), ("+010", 10L), ("0b", 0L), ("0o10", 8L), ("0x10", 16L), ("1_0", 10L) })
        { Call(parser, "set_default", ["n", pair.Item1]); Assert.Equal(pair.Item2, Get(Call(parser, "get_default_values")!, "n")); }
    }
    [Fact]
    public void ConversionErrorKeepsInnerValueErrorAndOriginalCallerContext()
    {
        object parser = Parser(); Call(parser, "add_option", ["-n"], ["type", "default"], ["int", "bad"]);
        var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var error = Assert.Throws<PyOptionValueError>(() => Call(parser, "get_default_values"));
            var inner = Assert.IsType<PyValueError>(error.Context); Assert.Same(outer, inner.Context);
            Assert.Same(outer, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }
    [Fact]
    public void ChoiceDefaultsAreCheckedAndDisabledConversionPreservesRawValue()
    {
        object parser = Parser(); var choices = new PyList(["a"]);
        Call(parser, "add_option", ["--x"], ["choices", "default"], [choices, "b"]);
        Assert.Throws<PyOptionValueError>(() => Call(parser, "get_default_values"));
        choices.Items.Add("b"); Assert.Equal("b", Get(Call(parser, "get_default_values")!, "x"));
        Call(parser, "add_option", ["--n"], ["type", "default"], ["int", "bad"]);
        Call(parser, "set_process_default_values", [false]); Assert.Equal("bad", Get(Call(parser, "get_default_values")!, "n"));
    }
    [Fact]
    public void ConstructorBindsArgumentsAndPublishesAutomaticOptionsInOrder()
    {
        object parser = New("OptionParser", [], ["version", "prog"], ["v1", "demo"]);
        var options = Assert.IsType<PyList>(Get(parser, "option_list")); Assert.Equal(2, options.Count);
        Assert.Equal("--version", PyRepr.Str(options.Items[0])); Assert.Equal("-h/--help", PyRepr.Str(options.Items[1]));
        Assert.Throws<PyTypeError>(() => New("OptionParser", [], ["unknown"], [1L]));
        Assert.Throws<PyTypeError>(() => New("OptionParser", ["x"], ["usage"], ["y"]));
        Assert.Throws<PyNotImplementedError>(() => New("OptionParser", [], ["conflict_handler"], ["resolve"]));
        Assert.Throws<PyValueError>(() => New("OptionParser", [], ["conflict_handler"], ["bogus"]));
        Assert.Throws<PyNotImplementedError>(() => New("OptionParser", [], ["formatter"], [1L]));
    }
    [Fact]
    public void PublicMetadataIsLiveAndUnsupportedFormatterRemainsExplicit()
    {
        object parser = Parser(); Assert.Same(PyNone.Instance, Get(parser, "rargs"));
        Assert.Throws<PyAttributeError>(() => Call(parser, "get_description"));
        Call(parser, "set_usage", [Module["SUPPRESS_USAGE"]]); Assert.Same(PyNone.Instance, Get(parser, "usage"));
        string equalUsage = new string("SUPPRESSUSAGE".ToCharArray());
        Call(parser, "set_usage", [equalUsage]); Assert.Same(equalUsage, Get(parser, "usage"));
        Call(parser, "set_usage", ["Usage: %prog tail"]); Assert.Equal("%prog tail", Get(parser, "usage"));
        Call(parser, "set_description", ["hello %prog"]); Assert.Equal("hello demo", Call(parser, "get_description"));
        Call(parser, "disable_interspersed_args"); Assert.Equal(false, Get(parser, "allow_interspersed_args"));
        Call(parser, "enable_interspersed_args"); Assert.Equal(true, Get(parser, "allow_interspersed_args"));
        Assert.IsType<PyTuple>(Call(parser, "parse_args", [new PyList()]));
        const string expectedHelp = "Usage: demo tail\n\nhello demo\n\n";
        Assert.Equal(expectedHelp, Call(parser, "format_help"));
        var output = new HelpOutput(); Call(parser, "print_help", [output]); Assert.Equal(expectedHelp, Assert.Single(output.Writes));
        Assert.Throws<PyNotImplementedError>(() => Get(parser, "formatter"));
        Assert.Throws<PyNotImplementedError>(() => Call(parser, "format_help", [new PyDict()]));
        Assert.Throws<PyAttributeError>(() => Get(parser, "format_usage"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(parser, "parse_args", null));
        var option = New("Option", ["--x"]); PyObject.SetAttr(option, "help", "new"); Assert.Equal("new", Get(option, "help"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(option, "action", "append"));
    }
}
