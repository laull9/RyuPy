// 别名图边界：逐一覆盖宿主登记、from/finder 冷拒绝、异常链与原普通导入行为。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public partial class PyModuleCacheAliasTests
{
    private static void RegisterBy(string kind, string name, Action body)
    {
        PyModuleInit init = () => body(); PyModuleGet get = _ => throw new PyAttributeError("missing");
        switch (kind)
        {
            case "legacy": PyModules.Register(name, "", "", init, get, PyModules.SetDynamic); break;
            case "native": PyModules.RegisterNative(name, new Dictionary<string, object?>(), (_, _) => body()); break;
            case "located": PyModules.RegisterLocated(name, "", "source.py", 1, "/virtual/source.py", true, init, get, PyModules.SetDynamic); break;
            case "resource": PyModules.RegisterResourceLocated(name, "", "source.py", 1, "/virtual/source.py", true, false, init, get, PyModules.SetDynamic); break;
            case "source": PyModules.RegisterSourceLocated(name, "", "source.py", 1, "/virtual/source.py", true, false, init, get, PyModules.SetDynamic); break;
            case "instance": PyModules.RegisterInstanceSourceLocated(name, "", "source.py", 1, "/virtual/source.py", false, _ => body()); break;
            case "declared": PyModules.RegisterDeclaredBinding(name, init, get, PyModules.SetDynamic); break;
            default: throw new InvalidOperationException(kind);
        }
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("native")]
    [InlineData("located")]
    [InlineData("resource")]
    [InlineData("source")]
    [InlineData("instance")]
    [InlineData("declared")]
    public void EveryRegistrationEntryRejectsAliasNamesAndDescendants(string kind)
    {
        var source = Empty(); string alias = Add(source); int calls = 0;
        foreach (string name in new[] { alias, alias + ".child", alias + ".child.deep" })
        {
            var error = Assert.Throws<PyNotImplementedError>(() => RegisterBy(kind, name, () => calls++));
            Assert.Contains("registration", error.Message); Assert.Same(source, PyModules.Import(alias));
        }
        Assert.Equal(0, calls); Assert.Throws<PyNotImplementedError>(() => PyModules.Import(alias + ".child"));
        string independent = alias + "_suffix"; RegisterBy(kind, independent, () => calls++);
        var unrelated = PyModules.Import(independent); Assert.NotSame(source, unrelated);
        if (kind == "native") unrelated.SetAttr("trigger", 1L);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void DeclarationPreflightRejectsDescendantsBeforeGeneratedSlotWrites()
    {
        string alias = Add(Empty());
        foreach (string name in new[] { alias, alias + ".child", alias + ".child.deep" })
            Assert.Throws<PyNotImplementedError>(() => PyModules.EnsureDeclaredBindingAvailable(name));
        string available = Unique(); PyModules.EnsureDeclaredBindingAvailable(available);
        PyModules.RegisterDeclaredBinding(available, () => { }, _ => 1L, PyModules.SetDynamic);
        Assert.Equal(1L, PyModules.Import(available).GetAttr("value"));
    }

    [Theory]
    [InlineData("located")]
    [InlineData("resource")]
    [InlineData("source")]
    [InlineData("instance")]
    public void RegistrationAliasGuardPrecedesLocationValidation(string kind)
    {
        string alias = Add(Empty());
        Action action = kind switch
        {
            "located" => () => PyModules.RegisterLocated(alias, "", "", 99, "invalid", false, () => { }, _ => null, PyModules.SetDynamic),
            "resource" => () => PyModules.RegisterResourceLocated(alias, "", "", 99, "invalid", false, false, () => { }, _ => null, PyModules.SetDynamic),
            "source" => () => PyModules.RegisterSourceLocated(alias, "", "", 99, "invalid", false, false, () => { }, _ => null, PyModules.SetDynamic),
            _ => () => PyModules.RegisterInstanceSourceLocated(alias, "", "", 99, "invalid", false, _ => { }),
        };
        Assert.Contains("registration", Assert.Throws<PyNotImplementedError>(action).Message);
    }

    [Fact]
    public void ExistingPackageChildAttributeWorksButAliasSubmoduleImportDoesNot()
    {
        string root = Unique(), child = root + ".child";
        PyModules.Register(root, root, "", () => { }, _ => throw new PyAttributeError("missing"), PyModules.SetDynamic);
        PyModules.Register(child, root, "", () => { }, _ => 7L, PyModules.SetDynamic);
        var originalChild = PyModules.Import(child); var parent = PyModules.Import(root); string alias = Add(parent);
        Assert.Same(originalChild, PyModules.ImportAttr(alias, "child"));
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(alias + ".child"));
        Assert.Throws<PyNotImplementedError>(() => PyModules.Import(alias + ".child.deep"));
        Assert.Same(originalChild, parent.GetAttr("child")); Assert.False(PyOps.Contains(View(), alias + ".child"));
    }

    [Fact]
    public void FinderRejectsAliasDescendantsBeforeParentPathGetterCallbacks()
    {
        string name = Unique(); int calls = 0;
        PyModules.RegisterSourceLocated(name, name, "__init__.py", 1, "/virtual/__init__.py", false, true,
            () => { }, key => { calls++; throw new PyAttributeError(key); }, PyModules.SetDynamic);
        PyModule source = PyModules.Import(name); string alias = Add(source);
        Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => Find(alias + ".child"));
        Assert.Throws<PyNotImplementedError>(() => Find(alias + ".child.deep"));
        Assert.Equal(0, calls); Assert.Same(source, PyModules.Import(alias));
    }

    [Fact]
    public void FromMissingAttributePreservesOriginalMissingAndOuterContext()
    {
        var missing = new PyAttributeError("original missing"); int calls = 0;
        var module = Native(get: _ => { calls++; throw missing; }); string alias = Add(module);
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("alias_test.py", "caller");
        try
        {
            PyTraceback.SetLine(31);
            var error = Assert.Throws<PyNotImplementedError>(() => PyModules.ImportAttr(alias, "absent"));
            Assert.Same(missing, error.Context); Assert.Same(outer, missing.Context); Assert.Equal(1, calls);
            Assert.Equal(new PyStackFrame("alias_test.py", "caller", 31), error.Frames[0]);
            Assert.False(error.SuppressContextFlag); Assert.Null(error.Cause);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        Assert.Same(module, PyModules.Import(alias)); Assert.False(PyOps.Contains(View(), alias + ".absent"));
    }

    [Fact]
    public void ExistingGetterFailuresRemainOriginalAndNonAliasMissingImportIsUnchanged()
    {
        var failure = new PyValueError("getter failure"); var source = Native(get: _ => throw failure); string alias = Add(source);
        Assert.Same(failure, Assert.Throws<PyValueError>(() => PyModules.ImportAttr(alias, "value")));
        var ordinary = Native();
        Assert.Equal($"cannot import name 'absent' from '{ordinary.Name}'",
            Assert.Throws<PyImportError>(() => PyModules.ImportAttr(ordinary.Name, "absent")).Message);
        string unknown = Unique();
        Assert.Equal($"No module named '{unknown}'", Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(unknown)).Message);
    }

    [Fact]
    public void FindSpecReadsTheCurrentOriginalObjectAndPreservesMissingSemantics()
    {
        PyModule source = Empty(); string alias = Add(source); object? original = source.GetAttr("__spec__");
        Assert.Same(original, Find(alias));
        var marker = new PyList([]); source.SetAttr("__spec__", marker); Assert.Same(marker, Find(alias));
        source.SetAttr("__spec__", PyNone.Instance);
        Assert.Equal(alias + ".__spec__ is None", Assert.Throws<PyValueError>(() => Find(alias)).Message);
        PyObject.DelAttr(source, "__spec__");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var error = Assert.Throws<PyValueError>(() => Find(alias));
            Assert.Equal(alias + ".__spec__ is not set", error.Message); Assert.True(error.SuppressContextFlag);
            var missing = Assert.IsType<PyAttributeError>(error.Context); Assert.Same(outer, missing.Context);
        }
        finally { PyErrors.LeaveHandler(previous); }
        Assert.Same(source, PyModules.Import(alias));
    }

    [Fact]
    public void AssignmentAndCacheHitNeverProbeModuleAttributes()
    {
        int calls = 0; var source = Native(get: _ => { calls++; throw new PyAttributeError("missing"); });
        string alias = Add(source); Assert.Same(source, PyModules.Import(alias)); Assert.Equal(0, calls);
        Assert.Throws<PyNotImplementedError>(() => PyOps.SetItem(View(), alias, source)); Assert.Equal(0, calls);
    }

    [Fact]
    public void AllNewRejectionsCaptureCallerFramesAndActiveException()
    {
        string alias = Add(Empty()); var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("cache_boundary.py", "caller");
        try
        {
            PyTraceback.SetLine(22);
            Action[] actions = [() => PyOps.SetItem(View(), alias, Empty()), () => PyOps.DelItem(View(), alias),
                () => RegisterBy("legacy", alias, () => { }), () => PyModules.Import(alias + ".child"),
                () => Find(alias + ".child"), () => PyModules.EnsureDeclaredBindingAvailable(alias + ".child")];
            foreach (Action action in actions)
            {
                var error = Assert.Throws<PyNotImplementedError>(action); Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("cache_boundary.py", "caller", 22), error.Frames[0]);
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void NormalRegistrationReplacementAndInitializationFailureRetryRemainAvailable()
    {
        string name = Unique(); int calls = 0;
        PyModules.RegisterNative(name, new Dictionary<string, object?> { ["value"] = "old" });
        PyModules.RegisterNative(name, new Dictionary<string, object?> { ["value"] = "new" });
        var original = PyModules.Import(name); Assert.Equal("new", original.GetAttr("value"));
        PyModules.Register(name, "", "", () => calls++, _ => "later", PyModules.SetDynamic);
        Assert.Same(original, PyModules.Import(name)); Assert.Equal(0, calls);
        string failName = Unique();
        PyModules.Register(failName, "", "", () => { if (++calls == 1) throw new PyValueError("first"); }, _ => calls, PyModules.SetDynamic);
        Assert.Throws<PyValueError>(() => PyModules.Import(failName)); Assert.False(PyOps.Contains(View(), failName));
        Assert.Equal(2, PyModules.Import(failName).GetAttr("runs"));
    }
}

// 新合法写入不能沿旧通用binder意外接受dict槽不接受的关键字；绑定失败必须先于挂接。
public partial class PyModuleCacheAliasTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void SetterKeywordArgumentsFailBeforePublishingOrReservingAlias(bool saved, int mode)
    {
        var view = View(); var module = Empty(); string alias = Unique(); object setter = view.GetAttr("__setitem__")!;
        object?[] args = mode == 0 ? [] : mode == 1 ? [alias] : [alias, module];
        string[] names = mode == 0 ? ["key", "value"] : mode == 1 ? ["value"] : ["key"];
        object?[] values = mode == 0 ? [alias, module] : mode == 1 ? [module] : [alias];
        var error = Assert.Throws<PyTypeError>(() => saved
            ? PyOps.CallValue(setter, args, names, values)
            : PyOps.CallMethod(view, "__setitem__", args, names, values));
        Assert.Contains("positional-only", error.Message);
        Assert.False(PyOps.Contains(view, alias));
        Assert.Same(PyNone.Instance, PyOps.CallValue(setter, [alias, module], [], []));
        Assert.Same(module, PyModules.Import(alias));
    }
}
