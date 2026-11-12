// canonical身份与真实虚拟消费同时验证；不以导入成功替代时区行为。
using System;
using System.Collections.Generic;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyDateTimeTzInfoTests
{
    private static object New(string type, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyDateTimeModule.CreateAttributes()[type], args, names ?? [], values ?? [])!;
    private static object? Call(object value, string method, params object?[] args) => PyOps.CallMethod(value, method, args, [], []);
    private static PyClass Class(string name, Func<object?[], object?> offset)
    {
        var cls = (PyClass)PyOps.PrepareRuntimeDerivedClassForMode(name, [PyDateTimeTzInfo.Root], false);
        cls.SetAttr("utcoffset", PyNativeFunctions.Create("utcoffset", ["self", "dt"], [null, null], 2, offset));
        cls.SetAttr("dst", PyNativeFunctions.Create("dst", ["self", "dt"], [null, null], 2, _ => new PyDateTimeDelta(0)));
        cls.SetAttr("tzname", PyNativeFunctions.Create("tzname", ["self", "dt"], [null, null], 2, _ => "custom"));
        PyOps.FinalizeOrdinaryClass(cls); return cls;
    }
    [Fact]
    public void RootIsCanonicalImmutableInstantiableAndNativeTimezoneHasRealParent()
    {
        var first = PyDateTimeModule.CreateAttributes(); var second = PyDateTimeModule.CreateAttributes();
        var root = Assert.IsType<PyClass>(first["tzinfo"]); Assert.Same(root, second["tzinfo"]);
        Assert.False(root.HasMultipleInheritance); Assert.False(root.IsNativeRuntimeClass);
        Assert.Throws<PyTypeError>(() => root.SetAttr("arbitrary", 1L));
        Assert.Throws<PyTypeError>(() => root.DeleteAttr("utcoffset"));
        var instance = root.Invoke([], [], []);
        Assert.Throws<PyNotImplementedError>(() => Call(instance!, "utcoffset", PyNone.Instance));
        Assert.Same(root, PyObject.GetAttr(first["timezone"], "__base__"));
        Assert.True(((PyNativeType)first["timezone"]!).IsIdentitySubclassOf(root));
        first["tzinfo"] = 42L; Assert.Same(root, PyDateTimeModule.CreateAttributes()["tzinfo"]);
        var child = Class("Child", _ => null); child.SetAttr("mutable", 3L); Assert.Equal(3L, PyObject.GetAttr(child, "mutable"));
    }
    [Fact]
    public void ActualZoneAndDatetimeIdentityAreKeptAndClockPassesNone()
    {
        object? seen = null;
        var cls = Class("Identity", args => { seen = args[1]; return new PyDateTimeDelta(3600000000L); });
        var zone = cls.Invoke([], [], []); var dt = New("datetime", [2024L, 1L, 2L, 3L], ["tzinfo"], [zone]);
        Assert.Null(seen); Assert.Same(zone, PyObject.GetAttr(dt, "tzinfo"));
        Assert.Equal("2024-01-02T03:00:00+01:00", Call(dt, "isoformat")); Assert.Same(dt, seen);
        var clock = New("time", [3L], ["tzinfo"], [zone]); Call(clock, "utcoffset"); Assert.True(seen is null or PyNone);
        Assert.Same(zone, PyObject.GetAttr(Call(dt, "timetz"), "tzinfo"));
    }
    [Fact]
    public void SameZoneSkipsCallbacksAndHashUsesFoldZeroOnce()
    {
        var calls = new List<long>();
        var cls = Class("Fold", args => { long fold = (long)PyObject.GetAttr(args[1], "fold")!; calls.Add(fold); return new PyDateTimeDelta((fold + 1) * 3600000000L); });
        var zone = cls.Invoke([], [], []);
        var a = New("datetime", [2024L, 1L, 2L, 3L], ["tzinfo", "fold"], [zone, 0L]);
        var b = New("datetime", [2024L, 1L, 2L, 3L], ["tzinfo", "fold"], [zone, 1L]);
        Assert.True(PyHash.Equal(a, b)); Assert.Equal("0:00:00", PyRepr.Str(Call(b, "__sub__", a))); Assert.Empty(calls);
        Assert.Equal(PyHash.Of(a), PyHash.Of(b)); Assert.Equal(new long[] { 0L, 0L }, calls);
        calls.Clear(); Assert.Equal(PyHash.Of(a), PyHash.Of(b)); Assert.Empty(calls);
    }
    [Fact]
    public void InvalidCallbacksAreValidatedAtConsumptionNotConstruction()
    {
        var cls = Class("Invalid", _ => 3L); var zone = cls.Invoke([], [], []);
        var dt = New("datetime", [2024L, 1L, 2L], ["tzinfo"], [zone]);
        Assert.Throws<PyTypeError>(() => Call(dt, "utcoffset"));
        cls.SetAttr("utcoffset", PyNativeFunctions.Create("utcoffset", ["self", "dt"], [null, null], 2, _ => new PyDateTimeDelta(PyDateTimeModule.Day)));
        Assert.Throws<PyValueError>(() => Call(dt, "isoformat"));
        Assert.Throws<PyTypeError>(() => New("datetime", [2024L, 1L, 2L], ["tzinfo"], [new object()]));
    }
    [Fact]
    public void AstimezoneInvokesTargetFromutcAndKeepsItsActualReturn()
    {
        var cls = Class("Target", _ => new PyDateTimeDelta(3600000000L));
        object? seen = null;
        cls.SetAttr("fromutc", PyNativeFunctions.Create("fromutc", ["self", "dt"], [null, null], 2, args => { seen = args[1]; return "override-result"; }));
        var target = cls.Invoke([], [], []);
        var dt = New("datetime", [2024L, 1L, 2L], ["tzinfo"], [PyDateTimeModule.Utc]);
        Assert.Equal("override-result", Call(dt, "astimezone", target)); Assert.Same(target, PyObject.GetAttr(seen, "tzinfo"));
        var local = New("datetime", [2024L, 1L, 2L], ["tzinfo"], [target]); Assert.Same(local, Call(local, "astimezone", target));
    }
    [Fact]
    public void TznameAcceptsStringSubclassWithoutErasingIdentity()
    {
        var text = (PyClass)PyOps.MakeClass("Text", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(text); var name = text.Invoke(["custom"], [], []);
        var cls = Class("Named", _ => new PyDateTimeDelta(0));
        cls.SetAttr("tzname", PyNativeFunctions.Create("tzname", ["self", "dt"], [null, null], 2, _ => name));
        var dt = New("datetime", [2024L, 1L, 2L], ["tzinfo"], [cls.Invoke([], [], [])]);
        Assert.Same(name, Call(dt, "tzname"));
    }

}
