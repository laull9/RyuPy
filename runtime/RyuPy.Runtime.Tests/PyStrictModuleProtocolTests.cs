// 严格导入能力的中性行为门：缓存三态、任意替换值及真实调用者globals必须穿透旧模块返回边界。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyStrictModuleProtocolTests
{
    [Fact]
    public void SysModulesDistinguishesMissingNoneAndArbitraryObjectWithoutModuleWrapping()
    {
        var provider = new StrictProvider(); PyProvider.Install(provider);
        try
        {
            Assert.False(PyModules.TryGetSysModuleEntry("sample", out _));
            provider.Entries["sample"] = PyNone.Instance;
            Assert.True(PyModules.TryGetSysModuleEntry("sample", out var blocked)); Assert.Same(PyNone.Instance, blocked);
            Assert.Throws<PyModuleNotFoundError>(() => PyModules.ImportLevel("sample", PyNone.Instance, [], 0));
            var replacement = new PyList([1L]); provider.Entries["sample"] = replacement;
            Assert.True(PyModules.TryGetSysModuleEntry("sample", out var entry)); Assert.Same(replacement, entry);
            Assert.Same(replacement, PyModules.ImportLevel("sample", PyNone.Instance, [], 0));
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void BuiltinAndRelativeImportsForwardCurrentGlobalsLevelAndFromlist()
    {
        var provider = new StrictProvider(); PyProvider.Install(provider);
        try
        {
            object? context = null;
            var globals = new PyForeignModuleStateTests.FakeNamespace();
            globals.External["__builtins__"] = PyModuleInstances.CreateCanonicalBuiltins();
            string name = "strict_context_" + Guid.NewGuid().ToString("N");
            PyModules.RegisterInstanceSourceLocated(name, "", "strict.py", 1, "/virtual/strict.py", false, state => context = state);
            PyModuleInstances.ExecuteBound(PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), globals, new PyForeignModuleStateTests.ForeignModule()));
            var value = new PyList([7L]); provider.Entries["leaf"] = value;
            var builtins = (PyDict)globals.External["__builtins__"]!;
            Assert.Same(value, PyOps.CallValue(builtins.Get("__import__"), ["leaf", globals, PyNone.Instance, new PyTuple(["member"]), 2L], [], []));
            Assert.Same(globals, provider.Globals); Assert.Equal(2L, provider.Level); Assert.Equal(new object?[] { "member" }, provider.Fromlist);
            object?[] fromlist = ["selected"];
            Assert.Same(value, PyModuleInstances.RelativeImport(context!, "leaf", 1, fromlist));
            Assert.Same(globals, provider.Globals); Assert.Equal(1L, provider.Level); Assert.Same(fromlist, provider.Fromlist);
            Assert.NotNull(PyModuleInstances.RelativeImportAttr(context!, value, "append"));
            var parent = PyProvider.Module("parent", key => key == "__name__" ? "parent"
                : throw new PyAttributeError(key), (_, _, _) => { });
            provider.Entries["parent.selected"] = value;
            Assert.Same(value, PyModuleInstances.RelativeImportAttr(context!, parent, "selected"));
            provider.Entries["parent.selected"] = PyNone.Instance;
            Assert.Same(PyNone.Instance, PyModuleInstances.RelativeImportAttr(context!, parent, "selected"));
            Assert.Equal(0, provider.LegacyImports);
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void ExplicitNoneFromlistIsEmptyWithoutOpeningArbitraryIterables()
    {
        var provider = new StrictProvider(); PyProvider.Install(provider);
        try
        {
            var value = new PyList([]); provider.Entries["os"] = value;
            object? importer = PyModuleInstances.CreateCanonicalBuiltins().Get("__import__");
            foreach (object? fromlist in new object?[] { null, PyNone.Instance })
            {
                Assert.Same(value, PyOps.CallValue(importer, ["os", PyNone.Instance, PyNone.Instance, fromlist], [], []));
                Assert.Empty(provider.Fromlist!);
            }
            object?[] previous = provider.Fromlist!;
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(importer,
                ["os", PyNone.Instance, PyNone.Instance, new PySet()], [], []));
            Assert.Same(previous, provider.Fromlist);
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void PlainModuleNoneFallbackAndPackageFromlistBlockingAreDifferentPhases()
    {
        var provider = new StrictProvider(); PyProvider.Install(provider);
        try
        {
            object? context = null; int attributeReads = 0;
            var globals = new PyForeignModuleStateTests.FakeNamespace();
            globals.External["__builtins__"] = PyModuleInstances.CreateCanonicalBuiltins();
            string name = "phase_context_" + Guid.NewGuid().ToString("N");
            PyModules.RegisterInstanceSourceLocated(name, "", "phase.py", 1, "/virtual/phase.py", false, state => context = state);
            PyModuleInstances.ExecuteBound(PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), globals, new PyForeignModuleStateTests.ForeignModule()));
            PyModule Module(string key, bool package) => PyProvider.Module(key, member =>
            {
                if (member == "__name__") return key;
                if (member == "__path__" && package) return new PyList([]);
                if (member == "x") attributeReads++;
                throw new PyAttributeError(member);
            }, (_, _, _) => { });
            provider.Entries["plain"] = Module("plain", false); provider.Entries["plain.x"] = PyNone.Instance;
            var plain = PyModuleInstances.RelativeImport(context!, "plain", 0, ["x"]);
            Assert.Same(PyNone.Instance, PyModuleInstances.RelativeImportAttr(context!, plain, "x"));
            Assert.Equal(1, attributeReads);
            provider.Entries["package"] = Module("package", true); provider.Entries["package.x"] = PyNone.Instance;
            // kernel负责包fromlist阶段的阻断；Runtime必须直接传播，不能先IMPORT_FROM或读取子缓存。
            var blocked = new PyModuleNotFoundError("import of package.x halted; None in sys.modules");
            provider.ImportFailure = blocked;
            Assert.Same(blocked, Assert.Throws<PyModuleNotFoundError>(() => PyModuleInstances.RelativeImport(context!, "package", 0, ["x"])));
            Assert.Equal(1, attributeReads); Assert.Equal(new object?[] { "x" }, provider.Fromlist);
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void CanonicalSnapshotPreservesIdentityAndNeverSharesMutableDictionary()
    {
        var first = PyModuleInstances.CreateCanonicalBuiltins(); var second = PyModuleInstances.CreateCanonicalBuiltins();
        Assert.NotSame(first, second);
        foreach (string name in new[] { "globals", "__import__", "__build_class__", "dict", "int", "ValueError" })
            Assert.Same(first.Get(name), second.Get(name));
        first.Set("__import__", 3L);
        Assert.IsType<PyFunction>(second.Get("__import__"));
        Assert.Same(second.Get("__import__"), PyModuleInstances.CreateCanonicalBuiltins().Get("__import__"));
    }

    [Fact]
    public void RealViewWithoutNamespaceInterfaceIsAdaptedAndReboundWithoutCopying()
    {
        var provider = new StrictProvider(); PyProvider.Install(provider);
        try
        {
            object? context = null;
            var first = new ForeignDict(); first.Values["answer"] = 4L;
            var second = new ForeignDict(); second.Values["answer"] = 5L;
            var globals = new PyForeignModuleStateTests.FakeNamespace(); globals.External["__builtins__"] = first;
            string name = "foreign_builtins_" + Guid.NewGuid().ToString("N");
            PyModules.RegisterInstanceSourceLocated(name, "", "dict.py", 1, "/virtual/dict.py", false, state => context = state);
            var module = PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), globals, new PyForeignModuleStateTests.ForeignModule());
            PyModuleInstances.ExecuteBound(module);
            PyFunction Define() => PyModuleInstances.CreateOwnedFunction(PyModuleInstances.CaptureDefinitionContext(context!),
                "read", PySignature.Create("read", [], [], []), (state, _, _, _, _) => PyModuleInstances.LoadName(state, "answer"), [], false, null);
            var old = Define(); globals.External["__builtins__"] = second; var fresh = Define();
            Assert.Same(first, PyObject.GetAttr(old, "__builtins__")); Assert.Same(second, PyObject.GetAttr(fresh, "__builtins__"));
            first.Values["answer"] = 14L;
            Assert.Equal(14L, PyOps.CallValue(old, [], [], [])); Assert.Equal(5L, PyOps.CallValue(fresh, [], [], []));
            globals.External["__builtins__"] = new PyForeignModuleStateTests.ForeignModule();
            Assert.Throws<PyNotImplementedError>(() => Define());
            globals.External["__builtins__"] = new PyList([]);
            Assert.Throws<PyNotImplementedError>(() => Define());
            provider.WrongIdentity = true; globals.External["__builtins__"] = second;
            Assert.Throws<PyRuntimeError>(() => Define());
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void BuiltinsModuleKeepsOriginalKeyWhileFunctionCapturesItsActualDictionary()
    {
        var provider = new StrictProvider(); PyProvider.Install(provider);
        try
        {
            object? context = null;
            var source = new BuiltinsModule(); source.Dictionary.Values["answer"] = 21L;
            var globals = new PyForeignModuleStateTests.FakeNamespace(); globals.External["__builtins__"] = source;
            string name = "module_builtins_" + Guid.NewGuid().ToString("N");
            PyModules.RegisterInstanceSourceLocated(name, "", "module.py", 1, "/virtual/module.py", false, state => context = state);
            var module = PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), globals, new PyForeignModuleStateTests.ForeignModule());
            PyModuleInstances.ExecuteBound(module);
            PyFunction Define() => PyModuleInstances.CreateOwnedFunction(PyModuleInstances.CaptureDefinitionContext(context!),
                "read", PySignature.Create("read", [], [], []), (state, _, _, _, _) => PyModuleInstances.LoadName(state, "answer"), [], false, null);
            var old = Define();
            Assert.Same(source, globals.External["__builtins__"]);
            Assert.Same(source.Dictionary, PyObject.GetAttr(old, "__builtins__"));
            source.Dictionary.Values["answer"] = 22L; Assert.Equal(22L, PyOps.CallValue(old, [], [], []));
            var second = new BuiltinsModule(); second.Dictionary.Values["answer"] = 30L;
            globals.External["__builtins__"] = second; var fresh = Define();
            Assert.Same(second.Dictionary, PyObject.GetAttr(fresh, "__builtins__"));
            Assert.Equal(22L, PyOps.CallValue(old, [], [], [])); Assert.Equal(30L, PyOps.CallValue(fresh, [], [], []));
            globals.External.Remove("__builtins__");
            Assert.Same(source.Dictionary, PyObject.GetAttr(Define(), "__builtins__"));
            Assert.False(globals.External.ContainsKey("__builtins__"));
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void MissingForeignBuiltinsFailsBeforeBodyAndDoesNotInjectManagedDictionary()
    {
        bool called = false;
        string name = "missing_builtins_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "missing.py", 1, "/virtual/missing.py", false, _ => called = true);
        var globals = new PyForeignModuleStateTests.FakeNamespace();
        var module = PyModuleInstances.BindForeign(PyModuleInstances.CaptureRegisteredCode(name), globals, new PyForeignModuleStateTests.ForeignModule());
        Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.ExecuteBound(module));
        Assert.False(called); Assert.False(globals.External.ContainsKey("__builtins__"));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.ExecuteBound(module));
    }

    private sealed class BuiltinsModule : PyForeignModuleStateTests.ForeignModule
    {
        internal readonly ForeignDict Dictionary = new();
    }
    private sealed class ForeignDict : PyForeignModuleStateTests.ForeignModule
    {
        internal readonly Dictionary<string, object?> Values = new(StringComparer.Ordinal);
        public override object ForeignType => PyType.Get("dict");
    }
    private sealed class DictAdapter(ForeignDict dictionary, bool wrong = false) : IPyModuleNamespace
    {
        public object ViewObject => wrong ? new object() : dictionary;
        public bool TryGet(string name, out object? value) => dictionary.Values.TryGetValue(name, out value);
        public void Set(string name, object? value) => dictionary.Values[name] = value;
        public bool DeleteIfPresent(string name) => dictionary.Values.Remove(name);
    }

    private sealed class StrictProvider : IPyModuleProvider, IPyStrictImportProvider
    {
        internal readonly Dictionary<string, object?> Entries = new(StringComparer.Ordinal);
        internal bool WrongIdentity;
        internal PyBaseException? ImportFailure;
        internal object? Globals; internal object?[]? Fromlist; internal long Level; internal int LegacyImports;
        public bool TryGetNamespace(object value, out IPyModuleNamespace? result)
        {
            result = value is ForeignDict dict ? new DictAdapter(dict, WrongIdentity) : null;
            return result is not null;
        }
        public bool TryGetBuiltinsNamespace(object value, out IPyModuleNamespace? result)
        {
            if (value is BuiltinsModule module) value = module.Dictionary;
            return TryGetNamespace(value, out result);
        }
        public bool TryGetSysModuleEntry(string name, out object? entry) => Entries.TryGetValue(name, out entry);
        public object ImportLevel(string name, object? globals, object?[] fromlist, long level)
        {
            Globals = globals; Fromlist = fromlist; Level = level;
            if (ImportFailure is not null) throw ImportFailure;
            if (!Entries.TryGetValue(name, out var entry) || entry is PyNone)
                throw new PyModuleNotFoundError("blocked or missing");
            return entry!;
        }
        public PyModule BindExistingMainModule(object codeToken) => throw new NotSupportedException();
        public bool Owns(string name) => true;
        public object? SystemAttribute(string name) => throw new NotSupportedException();
        public PyModule Import(string name) { LegacyImports++; throw new NotSupportedException(); }
        public void Registered(string name) { }
        public void Initializing(PyModule module) { }
        public void Failed(string name) { }
    }
}
