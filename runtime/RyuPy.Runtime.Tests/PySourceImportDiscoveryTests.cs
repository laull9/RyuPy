// find_spec/相对fromlist借用的父导入只消费一次实际path；共同初始化不得偷偷重走ordinary守卫。
using System;
using RyuPy.Runtime;
using Xunit;
using Fixture = RyuPy.Runtime.Tests.SourceImportFixture;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PySourceImportDiscoveryTests
{
    [Fact]
    public void FindingThroughUncachedIntermediateParentsReadsEachPathOnce()
    {
        string root = Fixture.Unique(), middle = root + ".middle", child = middle + ".child";
        int middleCalls = 0, childCalls = 0;
        var source = new Fixture(root, true); var intermediate = new Fixture(middle, true, () => middleCalls++);
        _ = new Fixture(child, false, () => childCalls++);
        Assert.NotNull(Fixture.Find(child));
        Assert.Equal(new[] { "__path__" }, source.Reads);
        Assert.Equal(new[] { "__path__" }, intermediate.Reads);
        Assert.Equal(1, middleCalls); Assert.Equal(0, childCalls); Assert.False(Fixture.Cached(child));
    }

    [Fact]
    public void IntermediateParentChecksAreNotRepeatedWhenTheSecondGetterWouldFail()
    {
        string root = Fixture.Unique(), middle = root + ".middle", child = middle + ".child";
        var source = new Fixture(root, true); _ = new Fixture(middle, true); _ = new Fixture(child, false);
        int reads = 0;
        source.Getter = key => {
            if (key == "__path__" && ++reads > 1) throw new PyRuntimeError("duplicate path read");
            return source.Read(key);
        };
        Assert.NotNull(Fixture.Find(child)); Assert.Equal(1, reads);
    }

    [Fact]
    public void GetterImportedIntermediateParentIsReusedByTheFinalCacheCheck()
    {
        string root = Fixture.Unique(), middle = root + ".middle", child = middle + ".child";
        var source = new Fixture(root, true); int calls = 0; bool inside = false; PyModule? imported = null;
        _ = new Fixture(middle, true, () => calls++); _ = new Fixture(child, false);
        source.Getter = key => {
            if (key == "__path__" && !inside)
            {
                inside = true; imported = PyModules.Import(middle);
            }
            return source.Read(key);
        };
        Assert.NotNull(Fixture.Find(child)); Assert.Equal(1, calls);
        Assert.Same(imported, PyModules.Import(middle));
        Assert.Equal(new[] { "__path__", "__path__" }, source.Reads);
    }

    [Fact]
    public void DiscoveryKeepsItsOwnStrictPathDomainAndMessage()
    {
        string root = Fixture.Unique(), middle = root + ".middle", child = middle + ".child";
        var source = new Fixture(root, true); _ = new Fixture(middle, true); _ = new Fixture(child, false);
        PyModules.Import(root); source.Fields["__path__"] = new PyList([source.Directory]);
        var error = Assert.Throws<PyNotImplementedError>(() => Fixture.Find(child));
        Assert.Equal("find_spec(): modified or unsupported parent search paths are not implemented", error.Message);
        Assert.Equal(new[] { "__path__" }, source.Reads); Assert.False(Fixture.Cached(middle));
        // ordinary正域可以接受同值新list；这不等于扩大find_spec支持域。
        Assert.NotNull(PyModules.Import(middle));
    }

    [Fact]
    public void DiscoveryParentGetterFailureKeepsIdentityAndDoesNotExecuteIntermediate()
    {
        string root = Fixture.Unique(), middle = root + ".middle", child = middle + ".child";
        var source = new Fixture(root, true); int calls = 0;
        _ = new Fixture(middle, true, () => calls++); _ = new Fixture(child, false);
        var failure = new PyRuntimeError("discovery parent getter"); source.Getter = _ => throw failure;
        Assert.Same(failure, Assert.Throws<PyRuntimeError>(() => Fixture.Find(child)));
        Assert.Equal(new[] { "__path__" }, source.Reads); Assert.Equal(0, calls);
    }

    [Fact]
    public void RelativeFromlistKeepsTwoPhasesWithoutDoublingChildPathReads()
    {
        string root = Fixture.Unique(), contextName = root + ".context";
        var source = new Fixture(root, true); var order = new System.Collections.Generic.List<string>();
        _ = new Fixture(root + ".first", false, () => order.Add("first"));
        _ = new Fixture(root + ".second", false, () => {
            order.Add("second"); PyModules.Import(root).SetAttr("first", "changed");
        });
        object? context = null;
        PyModules.RegisterInstanceSourceLocated(contextName, root, "context.py", 1,
            Fixture.Location(contextName, false), false, state => context = state);
        PyModule contextModule = PyModuleInstances.Create(contextName);
        PyModuleInstances.Execute(contextModule);
        object imported = PyModuleInstances.RelativeImport(context!, "", 1, ["first", "second"]);
        Assert.Equal(new[] { "first", "second" }, order);
        Assert.Equal(3, source.Reads.FindAll(key => key == "__path__").Count);
        Assert.Equal("changed", PyModuleInstances.RelativeImportAttr(context!, imported, "first"));
    }
}
