// 全局模块注册表不并行修改；唯一测试名隔离成功缓存，不重置其它模块或静态 Lazy。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNativeModuleFactoryTests
{
    private static string Name() => "lazy_native_test_" + Guid.NewGuid().ToString("N");

    [Fact]
    public void RegistrationDefersFactoryAndSuccessUsesOriginalTable()
    {
        string name = Name(); int calls = 0;
        var marker = new object(); var table = new Dictionary<string, object?> { ["value"] = marker };
        PyModules.RegisterNativeFactory(name, () => { calls++; return table; });
        Assert.Equal(0, calls);
        var module = PyModules.Import(name);
        Assert.Equal(1, calls); Assert.Same(module, PyModules.Import(name));
        Assert.Same(marker, module.GetAttr("value"));
        var replacement = new object(); module.SetAttr("value", replacement);
        Assert.Same(replacement, table["value"]);
        table["direct"] = marker;
        Assert.Same(marker, module.GetAttr("direct"));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void CircularImportSeesSameModuleAndMetadataButCannotWriteIncompleteTable()
    {
        string name = Name(); PyModule? inside = null; int calls = 0, guards = 0;
        PyModules.RegisterNativeFactory(name, () =>
        {
            calls++; inside = PyModules.Import(name);
            Assert.Equal(name, inside.GetAttr("__name__"));
            Assert.Equal("", inside.GetAttr("__package__"));
            Assert.Throws<PyAttributeError>(() => inside.GetAttr("later"));
            Assert.Throws<PyNotImplementedError>(() => inside.SetAttr("later", 1L));
            Assert.Equal(0, guards);
            return new() { ["later"] = 2L };
        }, (_, _) => guards++);
        var result = PyModules.Import(name);
        Assert.Same(inside, result); Assert.Equal(1, calls);
        Assert.Equal(2L, result.GetAttr("later"));
        result.SetAttr("later", 3L); Assert.Equal(1, guards);
    }

    [Fact]
    public void FailedAttemptRetriesWithoutRetargetingLeakedOldModule()
    {
        string name = Name(); PyModule? failed = null; int calls = 0;
        var failure = new PyValueError("factory failed");
        PyModules.RegisterNativeFactory(name, () =>
        {
            calls++;
            if (calls == 1) { failed = PyModules.Import(name); throw failure; }
            return new() { ["ready"] = true };
        });
        Assert.Same(failure, Assert.Throws<PyValueError>(() => PyModules.Import(name)));
        var ready = PyModules.Import(name);
        Assert.Equal(2, calls); Assert.NotSame(failed, ready);
        Assert.Equal(true, ready.GetAttr("ready"));
        Assert.Throws<PyAttributeError>(() => failed!.GetAttr("ready"));
        Assert.Throws<PyNotImplementedError>(() => failed!.SetAttr("ready", false));
        Assert.Same(ready, PyModules.Import(name)); Assert.Equal(2, calls);
    }

    [Fact]
    public void FailedChildDoesNotPublishAndSuccessfulDependencyRemainsCached()
    {
        string parentName = Name(), dependencyName = Name(); string childName = parentName + ".child";
        PyModules.RegisterNative(parentName, new());
        int dependencies = 0, attempts = 0;
        PyModules.RegisterNativeFactory(dependencyName, () => { dependencies++; return new(); });
        PyModule? dependency = null;
        PyModules.RegisterNativeFactory(childName, () =>
        {
            dependency = PyModules.Import(dependencyName);
            if (++attempts == 1) throw new PyValueError("child failed");
            return new();
        });
        Assert.Throws<PyValueError>(() => PyModules.Import(childName));
        var parent = PyModules.Import(parentName);
        Assert.Throws<PyAttributeError>(() => parent.GetAttr("child"));
        Assert.Same(dependency, PyModules.Import(dependencyName));
        var child = PyModules.Import(childName);
        Assert.Same(child, parent.GetAttr("child"));
        Assert.Equal(1, dependencies); Assert.Equal(2, attempts);
    }

    [Fact]
    public void SuccessfulWritesRunProtectionBeforeMutatingOriginalTable()
    {
        string name = Name(); var original = new object();
        var table = new Dictionary<string, object?> { ["protected"] = original };
        int guards = 0;
        PyModules.RegisterNativeFactory(name, () => table, (key, _) =>
        { guards++; if (key == "protected") throw new PyNotImplementedError("protected"); });
        var module = PyModules.Import(name);
        Assert.Throws<PyNotImplementedError>(() => module.SetAttr("protected", new object()));
        Assert.Same(original, table["protected"]); Assert.Equal(1, guards);
        module.SetAttr("new", original); Assert.Equal(2, guards);
        Assert.Same(original, table["new"]);
    }

    [Fact]
    public void MutualImportCompletesWithoutLazyRecursionAndPreservesIdentity()
    {
        string a = Name(), b = Name(); PyModule? partialA = null;
        PyModules.RegisterNativeFactory(a, () => new() { ["b"] = PyModules.Import(b) });
        PyModules.RegisterNativeFactory(b, () =>
        { partialA = PyModules.Import(a); return new() { ["a"] = partialA }; });
        var first = PyModules.Import(a); var second = PyModules.Import(b);
        Assert.Same(first, partialA); Assert.Same(second, first.GetAttr("b"));
        Assert.Same(first, second.GetAttr("a"));
    }
}
