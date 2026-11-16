// 新API一律经真实Import；普通专项恢复所有重绑，首次状态另由fresh进程专项覆盖。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextDedentTests
{
    private static PyModule Module()
    {
        PySystem.Initialize("textwrap-dedent-tests.py", []);
        return PyModules.Import("textwrap");
    }
    private static object? Call(object? callable, params object?[] args) => PyOps.CallValue(callable, args, [], []);
    private static object? Dedent(object? subject) => Call(Module().GetAttr("dedent"), subject);
    private static PyFunction Raw(PyCall body) => new("spy", PySignature.Create("spy", [], [], []), body, [], false);

    [Fact]
    public void FixedOracleMatrixAndNoChangeIdentity()
    {
        // 直接复制冻结oracle的24输入，不重新计算期待或宽化空白定义。
        (string Text, string Expected, bool Identity)[] cases =
        [
            ("", "", true),
            ("plain payload", "plain payload", true),
            ("   ", "", false),
            ("\t\t\n", "\n", false),
            ("  alpha\n   ", "alpha\n", false),
            ("  alpha\n \t \n    beta\n", "alpha\n\n  beta\n", false),
            ("  a\n    b\n  c", "a\n  b\nc", false),
            ("\ta\n\t\tb", "a\n\tb", false),
            (" a\n\tb", " a\n\tb", true),
            (" \ta\n  b", "\ta\n b", false),
            ("  \r\n  x", "\r\nx", false),
            ("\r\n  x", "\r\n  x", true),
            ("  a\r  b", "a\r  b", false),
            ("\u00a0a\n  b", "\u00a0a\n  b", true),
            (" \u00a0a\n b", "\u00a0a\nb", false),
            ("\u3000a\n  b", "\u3000a\n  b", true),
            ("  \u000b\n  x", "\u000b\nx", false),
            ("  \f\n  x", "\f\nx", false),
            ("  a\u0085  b", "a\u0085  b", false),
            ("  a\u2028  b", "a\u2028  b", false),
            ("  a\u2029  b", "a\u2029  b", false),
            ("  \ud83d\ude00\n    \u4e2d", "\ud83d\ude00\n  \u4e2d", false),
            ("x\n \t", "x\n", false),
            ("  \r", "\r", false),
        ];
        foreach (var row in cases)
        {
            string result = Assert.IsType<string>(Dedent(row.Text));
            Assert.Equal(row.Expected, result);
            if (row.Identity) Assert.Same(row.Text, result);
        }
    }

    [Fact]
    public void RawSubclassDoesNotCallOverridesAndWrongSubjectsDoNotConvert()
    {
        var cls = (PyClass)PyOps.MakeClass("Raw", [PyType.Get("str")], null);
        int callbacks = 0;
        foreach (string name in new[] { "__str__", "strip", "splitlines", "expandtabs" })
            cls.SetAttr(name, Raw((_, _, _, _) => { callbacks++; throw new PyValueError("unexpected override"); }));
        PyOps.FinalizeOrdinaryClass(cls);
        foreach (var row in new[] { ("", ""), ("plain", "plain"), ("  x\n    y", "x\n  y"), (" \t\n", "\n") })
        {
            object? subject = cls.Invoke([row.Item1], [], []);
            string result = Assert.IsType<string>(Dedent(subject));
            Assert.Equal(row.Item2, result); Assert.NotSame(subject, result);
        }
        var other = new PyClass("Stringable");
        other.SetAttr("__str__", Raw((_, _, _, _) => { callbacks++; return "not used"; }));
        foreach (object subject in new object[] { new PyInstance(other), PyNone.Instance, 3L,
            new PyBytes([120]), new PyByteArray([120]), PyMemoryView.Create([new PyBytes([120])], [], []) })
            Assert.Throws<PyTypeError>(() => Dedent(subject));
        Assert.Equal(0, callbacks);
    }

    [Fact]
    public void BinderAndSavedExportRemainOrdinaryFunctions()
    {
        PyModule module = Module(); object? saved = module.GetAttr("dedent"), originalRe = module.GetAttr("re");
        Assert.Equal("x", PyOps.CallValue(saved, [], ["text"], ["  x"]));
        Assert.Throws<PyTypeError>(() => Call(saved));
        Assert.Throws<PyTypeError>(() => Call(saved, "x", "y"));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(saved, ["x"], ["text"], ["y"]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(saved, [], ["other"], ["x"]));
        var token = new PyList([]); var replacement = Raw((_, _, _, _) => token);
        try
        {
            module.SetAttr("dedent", replacement);
            Assert.Same(replacement, module.GetAttr("dedent"));
            Assert.Same(token, Call(module.GetAttr("dedent"), "x"));
            Assert.Equal("x", Call(saved, "  x"));
            module.SetAttr("re", token);
            Assert.Equal("plain", Call(saved, "plain"));
            Assert.Throws<PyNotImplementedError>(() => Call(saved, "  x"));
        }
        finally { module.SetAttr("dedent", saved); module.SetAttr("re", originalRe); }
    }

    [Fact]
    public void CurrentSubReceivesThreePositionalNormalizedArgumentsAndKeepsIdentity()
    {
        PyModule module = Module(), re = PyModules.Import("re"); object? sub = re.GetAttr("sub");
        object token = new PyList([]); var error = new PyRuntimeError("callback");
        int calls = 0;
        try
        {
            re.SetAttr("sub", Raw((_, args, names, values) =>
            {
                calls++; Assert.Empty(names); Assert.Empty(values);
                Assert.Equal(new object?[] { "(?m)^  ", "", "  x\n\n    y" }, args);
                return token;
            }));
            Assert.Same(token, Call(module.GetAttr("dedent"), "  x\n \t \n    y"));
            Assert.Equal(1, calls);
            re.SetAttr("sub", Raw((_, _, _, _) => throw error));
            Assert.Same(error, Assert.Throws<PyRuntimeError>(() => Dedent("  x")));
            re.SetAttr("sub", PyNone.Instance);
            Assert.Throws<PyTypeError>(() => Dedent("  x"));
        }
        finally { re.SetAttr("sub", sub); }
    }

    [Fact]
    public void NoMarginNeverLooksUpCurrentReOrSub()
    {
        PyModule module = Module(); object? re = module.GetAttr("re"); var trap = new LookupTrap();
        try
        {
            module.SetAttr("re", trap); Assert.Same(trap, module.GetAttr("re"));
            Assert.Equal("plain", Dedent("plain"));
            Assert.Equal("\n", Dedent(" \t\n"));
            Assert.Equal("x\n", Dedent("x\n \t"));
            Assert.Equal(0, trap.Lookups);
            Assert.Throws<PyNotImplementedError>(() => Dedent("  x"));
            Assert.Equal(0, trap.Lookups);
        }
        finally { module.SetAttr("re", re); }
    }

    [Fact]
    public void PrivatePatternWritesSucceedButConsumptionRetainsStageOrder()
    {
        PyModule module = Module(); object? white = module.GetAttr("_whitespace_only_re"), leading = module.GetAttr("_leading_whitespace_re");
        Assert.IsType<PyRegexPattern>(white); Assert.IsType<PyRegexPattern>(leading);
        Assert.Equal("^[ \t]+$", PyObject.GetAttr(white, "pattern"));
        Assert.Equal("(^[ \t]*)(?:[^ \t\n])", PyObject.GetAttr(leading, "pattern"));
        Assert.Equal(40L, PyObject.GetAttr(white, "flags"));
        Assert.Equal(40L, PyObject.GetAttr(leading, "flags"));
        object token = new PyList([]);
        try
        {
            module.SetAttr("_leading_whitespace_re", token);
            Assert.Same(token, module.GetAttr("_leading_whitespace_re"));
            Assert.Throws<PyTypeError>(() => Dedent(PyNone.Instance));
            Assert.Throws<PyNotImplementedError>(() => Dedent("plain"));
            module.SetAttr("_whitespace_only_re", token);
            Assert.Same(token, module.GetAttr("_whitespace_only_re"));
            Assert.Throws<PyNotImplementedError>(() => Dedent(PyNone.Instance));
            Assert.Throws<PyNotImplementedError>(() => Dedent(""));
            Assert.Equal("one\ntwo", Call(module.GetAttr("fill"), "one two", 4L));
        }
        finally
        {
            module.SetAttr("_whitespace_only_re", white); module.SetAttr("_leading_whitespace_re", leading);
        }
    }

    [Fact]
    public void OldHostDictionaryAndPythonFactoryHaveOneAuthority()
    {
        PyModule module = Module(); var old = PyTextWrap.CreateAttributes(); object? factory = old["TextWrapper"];
        foreach (string name in new[] { "dedent", "re", "_whitespace_only_re", "_leading_whitespace_re" })
            Assert.False(old.ContainsKey(name));
        var product = new PyClass("Product");
        product.SetAttr("wrap", PyNativeFunctions.Create("wrap", ["self", "text"], [null, null], 2, a => new PyList([a[1]])));
        product.SetAttr("fill", PyNativeFunctions.Create("fill", ["self", "text"], [null, null], 2, a => "fill:" + a[1]));
        object instance = new PyInstance(product); int calls = 0;
        var replacement = Raw((_, _, _, _) => { calls++; return instance; });
        try
        {
            module.SetAttr("TextWrapper", replacement);
            Assert.Same(replacement, old["TextWrapper"]); Assert.Same(replacement, module.GetAttr("TextWrapper"));
            Assert.Equal("fill:x", Call(module.GetAttr("fill"), "x"));
            old["TextWrapper"] = factory;
            Assert.Same(factory, module.GetAttr("TextWrapper"));
            Assert.Equal("one\ntwo", Call(module.GetAttr("fill"), "one two", 4L));
            old["TextWrapper"] = replacement;
            Assert.Equal(new object?[] { "x" }, Assert.IsType<PyList>(Call(module.GetAttr("wrap"), "x")).Items);
            Assert.Equal(2, calls);
        }
        finally { old["TextWrapper"] = factory; }
    }

    [Fact]
    public void BoundPatternsSurviveLaterPublicCompileAndFlagChanges()
    {
        PyModule module = Module(), re = PyModules.Import("re");
        object? compile = re.GetAttr("compile"), flags = re.GetAttr("MULTILINE");
        try
        {
            re.SetAttr("compile", Raw((_, _, _, _) => throw new PyValueError("must not compile again")));
            re.SetAttr("MULTILINE", new PyList([]));
            Assert.Equal("x", Call(module.GetAttr("dedent"), "  x"));
        }
        finally { re.SetAttr("compile", compile); re.SetAttr("MULTILINE", flags); }
    }

    [Fact]
    public void LargeMarginRespectsCurrentSubAndStockLimit()
    {
        // 小正例不宣称最大值；原pattern长度和译后体积是两个不同的既有资源门。
        Assert.Equal("x", Dedent(new string(' ', 64) + "x"));
        Assert.Equal("re: unsupported translated pattern size limit",
            Assert.Throws<PyNotImplementedError>(() => Dedent(new string(' ', 16379) + "x")).PyMessage);
        Assert.Equal("re: pattern length exceeds supported resource limit",
            Assert.Throws<PyNotImplementedError>(() => Dedent(new string(' ', 16380) + "x")).PyMessage);
        PyModule re = PyModules.Import("re"); object? sub = re.GetAttr("sub"); object token = new PyList([]);
        string margin = new(' ', 16380), input = margin + "x";
        try
        {
            re.SetAttr("sub", Raw((_, args, names, values) =>
            { Assert.Equal(new object?[] { "(?m)^" + margin, "", input }, args); Assert.Empty(names); Assert.Empty(values); return token; }));
            Assert.Same(token, Dedent(input));
        }
        finally { re.SetAttr("sub", sub); }
    }

    [Fact]
    public void LargeWhitespaceNormalizationDoesNotRescanRemainingSuffixes()
    {
        string input = "x\n" + string.Concat(Enumerable.Repeat(" \t\n", 100000));
        string expected = "x\n" + new string('\n', 100000);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Assert.Equal(expected, Dedent(input));
        // 宽松防退化门，不是性能基准或整个dedent的2秒保证。
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10));
    }

    private sealed class LookupTrap : PyNativeObject
    {
        internal int Lookups;
        public override string TypeName => "LookupTrap";
        public override object? GetAttr(string name) { Lookups++; throw new PyValueError("unexpected lookup"); }
    }
}
