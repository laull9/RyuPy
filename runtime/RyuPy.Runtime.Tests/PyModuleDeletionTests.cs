// 模块裸名删除的 CLR 机制门禁：区分槽位绑定与字典键，并验证真实 builtins 回退及实例隔离。
using System;
using System.Linq;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyModuleDeletionTests
{
    private static string Register(PyModuleExecute execute)
    {
        string name = "module_deletion_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "deletion.py", 1,
            "/virtual/deletion.py", false, execute);
        return name;
    }

    private static PyDict Globals(PyModule module) => Assert.IsType<PyDict>(module.GetAttr("__dict__"));

    private static PyDict DefaultBuiltins() => Assert.IsType<PyDict>(
        PyModuleInstances.Create(Register(_ => { })).GetAttr("__builtins__"));

    private static void Missing(string name, Action action) => Assert.Equal(
        $"name '{name}' is not defined", Assert.Throws<PyNameError>(action).PyMessage);

    // 各种零值、引用、单例和容器都是合法绑定，不能用 truthiness 或类型判断替代绑定位。
    private static object[] BoundValues() =>
    [
        0L, long.MinValue, 0.0, double.NaN, false, true, "", "value",
        BigInteger.One << 100, PyNone.Instance, PyEllipsis.Instance, PyNotImplemented.Instance,
        new PyList([]), new PyDict(), new PyTuple([]), new PySet(), new PyFrozenSet([]),
        new PyBytes([]), new PyByteArray([]), new PySlice(null, null, null), new PyRange(0, 1, 1),
        new PyClass("DeletionValue"), new PyInstance(new PyClass("DeletionInstance")),
        new PyFunction("saved", PySignature.Create("saved", [], [], []), (_, _, _, _) => PyNone.Instance, []),
        PyModuleInstances.Create(Register(_ => { })), new PyValueError("saved"),
        PyOps.BuiltinFunction("len"), PyOps.TypeObject("int"), new object(),
    ];

    [Theory]
    [InlineData("missing")]
    [InlineData("len")]
    [InlineData("Ellipsis")]
    [InlineData("eval")]
    public void TypedDeleteChecksOnlyTheExplicitBinding(string name)
    {
        PyOps.CheckModuleDelete(true, name);
        Missing(name, () => PyOps.CheckModuleDelete(false, name));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("len")]
    [InlineData("Ellipsis")]
    [InlineData("eval")]
    public void ValueDeleteRejectsOnlyUnboundSlotsWithoutBuiltinFallback(string name)
    {
        Missing(name, () => PyOps.CheckModuleDeleteValue(null, name));
        Missing(name, () => PyOps.CheckModuleDeleteValue(PyErrors.Unbound(), name));
        foreach (object value in BoundValues()) PyOps.CheckModuleDeleteValue(value, name);
    }

    [Fact]
    public void BoundReadsPreserveEveryValueAndReference()
    {
        foreach (object value in BoundValues())
        {
            Assert.Same(value, PyOps.LoadDeletedModuleName(value, "len"));
            Assert.Same(value, PyOps.LoadDeletedModuleName(value, "eval"));
            Assert.Same(value, PyOps.LoadDeletedModuleName(value, "ordinary"));
        }
    }

    [Fact]
    public void DynamicDeletionKeepsSavedAliasesAndRepeatedDeleteFails()
    {
        var saved = new PyList([1L]);
        object slot = saved;
        PyOps.CheckModuleDeleteValue(slot, "items");
        slot = PyErrors.Unbound();
        saved.Append(2L);
        Assert.Equal(new object?[] { 1L, 2L }, saved.Items);
        Missing("items", () => PyOps.CheckModuleDeleteValue(slot, "items"));
        Missing("items", () => PyOps.LoadDeletedModuleName(slot, "items"));
        slot = PyNone.Instance;
        PyOps.CheckModuleDeleteValue(slot, "items");
        Assert.Same(PyNone.Instance, PyOps.LoadDeletedModuleName(slot, "items"));
    }

    [Fact]
    public void TypedDeletionCanClearOnlyTheFlagAndRebindZero()
    {
        long slot = 0;
        bool bound = true;
        PyOps.CheckModuleDelete(bound, "number");
        bound = false;
        Assert.Equal(0, slot);
        Missing("number", () => PyOps.CheckModuleDelete(bound, "number"));
        bound = true;
        PyOps.CheckModuleDelete(bound, "number");
        Assert.Equal(0, slot);
    }

    [Fact]
    public void DeletedReadsUseActualDefaultBuiltinsWithoutInsertingOrDeleting()
    {
        var builtins = DefaultBuiltins();
        long version = builtins.Version;
        int count = builtins.Count;
        foreach (string name in new[] { "len", "int", "ValueError", "Ellipsis", "NotImplemented" })
        {
            object? expected = builtins.Get(name);
            Assert.Same(expected, PyOps.LoadDeletedModuleName(null, name));
            Assert.Same(expected, PyOps.LoadDeletedModuleName(PyErrors.Unbound(), name));
            Missing(name, () => PyOps.CheckModuleDelete(false, name));
            Missing(name, () => PyOps.CheckModuleDeleteValue(null, name));
            Assert.Same(expected, builtins.Get(name));
        }
        Assert.Equal(version, builtins.Version);
        Assert.Equal(count, builtins.Count);
    }

    [Fact]
    public void DefaultBuiltinMutationsAreObservedByReferenceIncludingNullValues()
    {
        var builtins = DefaultBuiltins();
        string name = "deletion_builtin_" + Guid.NewGuid().ToString("N");
        var saved = new PyList([]);
        try
        {
            builtins.Set(name, saved);
            Assert.Same(saved, PyOps.LoadDeletedModuleName(null, name));
            builtins.Set(name, PyNone.Instance);
            Assert.Same(PyNone.Instance, PyOps.LoadDeletedModuleName(PyErrors.Unbound(), name));
            // 宿主可写入 null；字典键存在时保持已有 LoadName 的原值约定。
            builtins.Set(name, null);
            Assert.Null(PyOps.LoadDeletedModuleName(null, name));
            Missing(name, () => PyOps.CheckModuleDeleteValue(null, name));
            Assert.True(builtins.Contains(name));
        }
        finally { builtins.Delete(name); }
        Missing(name, () => PyOps.LoadDeletedModuleName(null, name));
    }

    [Fact]
    public void MissingReadRetainsKnownUnsupportedBuiltinClassification()
    {
        foreach (object? unbound in new object?[] { null, PyErrors.Unbound() })
        {
            Missing("undefined_deletion_name", () => PyOps.LoadDeletedModuleName(unbound, "undefined_deletion_name"));
            var error = Assert.Throws<PyNotImplementedError>(() => PyOps.LoadDeletedModuleName(unbound, "eval"));
            Assert.Equal("builtin fallback for unbound module name 'eval' is not supported", error.PyMessage);
        }
    }

    [Fact]
    public void ExistingReadHelpersKeepTheirOriginalContract()
    {
        Assert.Throws<PyNotImplementedError>(() => PyOps.CheckGlobalBound(false, "len"));
        Assert.Throws<PyNotImplementedError>(() => PyErrors.LoadName(null, "len"));
        Assert.Same(PyEllipsis.Instance, PyErrors.LoadName(null, "Ellipsis"));
        Assert.Same(PyNone.Instance, PyErrors.LoadName(PyNone.Instance, "len"));
    }

    [Fact]
    public void InstanceDeletionRemovesOneRealKeyAndKeepsSavedReferences()
    {
        object? context = null;
        var module = PyModuleInstances.Create(Register(s => context = s));
        PyModuleInstances.Execute(module);
        var globals = Globals(module);
        foreach (object? value in BoundValues().Concat(new object?[] { null, PyErrors.Unbound() }))
        {
            PyModuleInstances.StoreName(context!, "value", value);
            Assert.Same(value, globals.Get("value"));
            long version = globals.Version;
            PyModuleInstances.DeleteName(context!, "value");
            Assert.False(globals.Contains("value"));
            Assert.Same(globals, Globals(module));
            Assert.Equal(version + 1, globals.Version);
            Missing("value", () => PyModuleInstances.DeleteName(context!, "value"));
            Assert.Equal(version + 1, globals.Version);
            Assert.Throws<PyAttributeError>(() => module.GetAttr("value"));
        }
        var saved = new PyList([1L]);
        module.SetAttr("items", saved);
        PyModuleInstances.DeleteName(context!, "items");
        saved.Append(2L);
        Assert.Equal(new object?[] { 1L, 2L }, saved.Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InstanceDeletionNeverFallsBackToCapturedBuiltins(bool custom)
    {
        object? context = null;
        var module = PyModuleInstances.Create(Register(s => context = s));
        var builtins = custom ? new PyDict() : Assert.IsType<PyDict>(module.GetAttr("__builtins__"));
        if (custom) { builtins.Set("len", new PyList([])); module.SetAttr("__builtins__", builtins); }
        PyModuleInstances.Execute(module);
        object? original = builtins.Get("len");
        long builtinVersion = builtins.Version;
        int builtinCount = builtins.Count;
        Missing("len", () => PyModuleInstances.DeleteName(context!, "len"));
        Assert.False(Globals(module).Contains("len"));
        module.SetAttr("len", PyNone.Instance);
        PyModuleInstances.DeleteName(context!, "len");
        Assert.Same(original, PyModuleInstances.LoadName(context!, "len"));
        Missing("len", () => PyModuleInstances.DeleteName(context!, "len"));
        Missing("eval", () => PyModuleInstances.DeleteName(context!, "eval"));
        Missing("undefined_deletion_name", () => PyModuleInstances.DeleteName(context!, "undefined_deletion_name"));
        Assert.Same(original, builtins.Get("len"));
        Assert.Equal(builtinVersion, builtins.Version);
        Assert.Equal(builtinCount, builtins.Count);
    }

    [Fact]
    public void DeletingBuiltinMetadataDoesNotReinsertItOrChangeCapturedEnvironment()
    {
        object? context = null;
        var module = PyModuleInstances.Create(Register(s => context = s));
        var custom = new PyDict();
        var answer = new PyList([]);
        custom.Set("answer", answer);
        module.SetAttr("__builtins__", custom);
        PyModuleInstances.Execute(module);
        object captured = context!;
        PyModuleInstances.DeleteName(captured, "__builtins__");
        Assert.False(Globals(module).Contains("__builtins__"));
        Assert.Same(answer, PyModuleInstances.LoadName(captured, "answer"));
        Missing("__builtins__", () => PyModuleInstances.DeleteName(captured, "__builtins__"));
        Assert.False(Globals(module).Contains("__builtins__"));
        PyModuleInstances.Execute(module);
        Assert.Same(DefaultBuiltins(), module.GetAttr("__builtins__"));
        Assert.Same(answer, PyModuleInstances.LoadName(captured, "answer"));
    }

    [Fact]
    public void InstanceNamesUseDictionaryKeysRatherThanModuleAttributeSpecialCases()
    {
        object? context = null;
        var module = PyModuleInstances.Create(Register(s => context = s));
        PyModuleInstances.Execute(module);
        var globals = Globals(module);
        globals.Set("__dict__", PyNone.Instance);
        PyModuleInstances.DeleteName(context!, "__dict__");
        Assert.Same(globals, module.GetAttr("__dict__"));
        Missing("__dict__", () => PyModuleInstances.DeleteName(context!, "__dict__"));
        PyModuleInstances.DeleteName(context!, "__name__");
        Missing("__name__", () => PyModuleInstances.DeleteName(context!, "__name__"));
    }

    [Fact]
    public void CapturedContextDoesNotRetargetAnotherInstanceOrReplacementRegistration()
    {
        object? context = null;
        string name = Register(s => context = s);
        var a = PyModuleInstances.Create(name);
        var b = PyModuleInstances.Create(name);
        a.SetAttr("value", 1L); b.SetAttr("value", 2L);
        PyModuleInstances.Execute(a);
        object aContext = context!;
        PyModuleInstances.Execute(b);
        object bContext = context!;
        a.SetAttr("__name__", "renamed");
        PyModules.RegisterInstanceSourceLocated(name, "", "replacement.py", 1,
            "/virtual/replacement.py", false, _ => { });
        var replacement = PyModuleInstances.Create(name);
        replacement.SetAttr("value", 3L);
        PyModuleInstances.DeleteName(aContext, "value");
        Assert.False(Globals(a).Contains("value"));
        Assert.Equal(2L, b.GetAttr("value"));
        Assert.Equal(3L, replacement.GetAttr("value"));
        PyModuleInstances.DeleteName(bContext, "value");
        Assert.False(Globals(b).Contains("value"));
        Assert.Equal(3L, replacement.GetAttr("value"));
    }

    [Fact]
    public void InvalidContextIdentityFailsBeforeAnyDeletion()
    {
        var module = PyModuleInstances.Create(Register(_ => { }));
        module.SetAttr("value", 42L);
        var globals = Globals(module);
        long version = globals.Version;
        foreach (object? invalid in new object?[] { null, new object(), module, globals, module.GetAttr("__builtins__") })
        {
            var error = Assert.Throws<PyRuntimeError>(() => PyModuleInstances.DeleteName(invalid!, "value"));
            Assert.Equal("invalid compiled module execution context", error.PyMessage);
            Assert.Equal(42L, module.GetAttr("value"));
            Assert.Equal(version, globals.Version);
        }
    }
}
