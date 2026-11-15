// 编译器实验入口的精确对象门与弱键缓存；这组合同不是已编译源码差分。
using System.Linq;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRecordsIncrementGuardTests
{
    [Fact]
    public void PublicGuardKeepsMaterializedIdentityAndCachedBox()
    {
        var dict = new PyDict(); var first = new PyTuple(["ann", "buy"]);
        Assert.True(PyRecordsIncrement.TryMaterialized(dict, first));
        Assert.Same(first, dict.Keys.Single()); Assert.Same(PyInt.Box(1L), dict.Get(first));
        var later = new PyTuple(["ann", "buy"]);
        Assert.True(PyRecordsIncrement.TryMaterialized(dict, later));
        Assert.Same(first, dict.Keys.Single()); Assert.Same(PyInt.Box(2L), dict.Get(first));
        dict.Delete(first); Assert.True(PyRecordsIncrement.TryMaterialized(dict, later));
        Assert.Same(later, dict.Keys.Single());
    }

    [Fact]
    public void RejectedGuardDoesNotRunUserProtocolsOrWrite()
    {
        int called = 0; var cls = new PyClass("Counter");
        cls.SetAttr("__add__", PyNativeFunctions.CreateVariadic("add", args => { called++; return PyInt.Box(9L); }));
        var old = new PyInstance(cls); var dict = new PyDict(); var key = new PyTuple(["ann", "buy"]);
        dict.Set(key, old); long version = dict.Version;
        Assert.False(PyRecordsIncrement.TryMaterialized(dict, key));
        Assert.Equal(0, called); Assert.Equal(version, dict.Version); Assert.Same(old, dict.Get(key));
        Assert.False(PyRecordsIncrement.TryMaterialized(old, key)); Assert.Equal(0, called);
        object result = PyOps.Binary(PyOps.Add, PyOps.CallMethod(dict, "get", [key, PyInt.Box(0L)], [], [])!, PyInt.Box(1L));
        PyOps.SetItem(dict, key, result);
        Assert.Equal(1, called); Assert.Equal(9L, dict.Get(key));
        dict.Set(key, PyInt.Box(long.MaxValue));
        Assert.False(PyRecordsIncrement.TryMaterialized(dict, key)); Assert.Equal(long.MaxValue, dict.Get(key));
    }

    [Fact]
    public void CachedDomainRejectsLargeExternalChangesAndRecoversAfterClear()
    {
        var dict = new PyDict(); var zero = new PyTuple(["0", "buy"]);
        Assert.True(PyRecordsIncrement.TryMaterialized(dict, zero));
        for (int i = 1; i < 300; i++)
            Assert.True(PyRecordsIncrement.TryMaterialized(dict, new PyTuple([i.ToString(), "buy"])));
        dict.Delete(zero); dict.Set(new PyTuple(["outside", "buy"]), PyInt.Box(0L));
        Assert.False(PyRecordsIncrement.TryMaterialized(dict, zero)); Assert.Equal(300, dict.Count);
        dict.Clear(); Assert.True(PyRecordsIncrement.TryMaterialized(dict, zero));
        Assert.Same(zero, dict.Keys.Single());
    }
}
