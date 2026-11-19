// B1仅验证内部私壳、准备/提交与分配封口；不注册typing根，不模拟Python注解回调成功域。
// 反射仅用于测试internal API，源码审不等于已执行这些测试。
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingNamedTupleShellTests
{
    private static readonly Type ShellType = typeof(PyClass).Assembly.GetType("RyuPy.Runtime.PyTypingNamedTupleShell")!;
    private static readonly Type LayoutType = typeof(PyClass).Assembly.GetType("RyuPy.Runtime.PyNamedTupleLayout")!;
    private static object? Call(Type type, string method, object? target, params object?[] args)
    {
        try
        {
            return type.GetMethod(method, BindingFlags.NonPublic | (target is null ? BindingFlags.Static : BindingFlags.Instance))!
                .Invoke(target, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static PyClass Begin(string name = "Record") => (PyClass)Call(ShellType, "Begin", null, name, "origin")!;
    private static object Prepare(PyClass cls, string[] fields, object?[] defaults) =>
        Call(ShellType, "Prepare", null, cls, fields, defaults, "origin")!;
    private static void Commit(PyClass cls, object prepared) => Call(typeof(PyClass), "CommitNamedTuple", cls, prepared);
    private static object? Layout(PyClass cls) => typeof(PyClass).GetProperty("NamedTupleLayout", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cls);
    private static object? ObjectNew(PyClass cls) => PyOps.CallValue(PyObject.GetAttr(PyBaseObject.Type, "__new__"), [cls], [], []);
    private static object? Method(object receiver, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(receiver, name), args, [], []);

    [Fact]
    public void PendingNamespaceIsWritableButCannotAllocateOrBecomeABase()
    {
        PyClass shell = Begin();
        shell.SetAttr("x", 4L);
        Assert.Equal(4L, PyOps.ClassNamespaceGet(shell, "x"));
        Assert.Null(Layout(shell));
        Assert.Throws<PyNotImplementedError>(() => shell.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => ObjectNew(shell));
        Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("Child", [shell], null));
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(shell));
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(shell));
        Assert.Null(Layout(shell));
    }

    [Fact]
    public void PreparationDoesNotPublishLayoutOrReplaceNamespace()
    {
        PyClass shell = Begin();
        shell.SetAttr("x", 4L);
        object? annotations = PyOps.ClassNamespaceGet(shell, "__annotations__");
        object prepared = Prepare(shell, ["x"], []);
        Assert.Null(Layout(shell));
        Assert.Equal(4L, PyOps.ClassNamespaceGet(shell, "x"));
        Assert.Same(annotations, PyOps.ClassNamespaceGet(shell, "__annotations__"));
        Assert.False(shell.Attributes.ContainsKey("_make"));
        Assert.Throws<PyNotImplementedError>(() => shell.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => Prepare(shell, ["x"], []));
        Commit(shell, prepared);
        Assert.NotNull(Layout(shell));
    }

    [Fact]
    public void CommitKeepsOwnerAndInvalidatesPositiveNegativeAndConstructionCaches()
    {
        PyClass shell = Begin();
        shell.SetAttr("x", 4L);
        Assert.True(shell.TryLookup("x", out _, out object? old));
        Assert.Equal(4L, old);
        Assert.False(shell.TryLookup("_make", out _, out _));
        // 填充Special construction缓存；只验证内部状态，不声称Python能取得shell。
        Assert.Throws<PyNotImplementedError>(() => shell.Invoke([], [], []));
        long before = PyClass.GlobalVersion;
        object prepared = Prepare(shell, ["x"], []);
        Commit(shell, prepared);
        Assert.True(PyClass.GlobalVersion > before);
        Assert.True(shell.TryLookup("x", out PyClass fieldOwner, out object? descriptor));
        Assert.Same(shell, fieldOwner);
        Assert.NotEqual(old, descriptor);
        Assert.True(shell.TryLookup("_make", out PyClass helperOwner, out _));
        Assert.Same(shell, helperOwner);
        var value = Assert.IsAssignableFrom<PyTuple>(shell.Invoke([9L], [], []));
        Assert.Same(shell, PyObject.GetAttr(value, "__class__"));
        Assert.Equal(9L, PyObject.GetAttr(value, "x"));
        Assert.Same(shell, PyObject.GetAttr(PyObject.GetAttr(shell, "_make"), "__self__"));
        var explicitNew = Assert.IsAssignableFrom<PyTuple>(Method(shell, "__new__", shell, 8L));
        Assert.Same(shell, PyObject.GetAttr(explicitNew, "__class__"));
        Assert.Equal(8L, explicitNew.GetItem(0L));
        Assert.Throws<PyNotImplementedError>(() => Commit(shell, prepared));
        Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(shell));
    }

    [Fact]
    public void FailedPreparationCannotRetryOrFallBackToOrdinaryAllocation()
    {
        PyClass shell = Begin();
        shell.SetAttr("sentinel", 6L);
        Assert.Throws<PyTypeError>(() => Prepare(shell, [], [1L]));
        Assert.Equal(6L, PyOps.ClassNamespaceGet(shell, "sentinel"));
        Assert.Null(Layout(shell));
        Assert.Throws<PyNotImplementedError>(() => Prepare(shell, [], []));
        Assert.Throws<PyNotImplementedError>(() => shell.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => ObjectNew(shell));
    }

    [Fact]
    public void AbortingPreparedNamespacePreventsLaterCommit()
    {
        PyClass shell = Begin();
        object prepared = Prepare(shell, [], []);
        Call(ShellType, "Fail", null, shell);
        Assert.Throws<PyNotImplementedError>(() => Commit(shell, prepared));
        Assert.Throws<PyNotImplementedError>(() => shell.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => ObjectNew(shell));
    }

    [Fact]
    public void PreparedLayoutCannotBeAttachedToAnotherOwnerOrDefinedOrdinaryClass()
    {
        PyClass shell = Begin();
        object prepared = Prepare(shell, ["x"], []);
        PyClass other = Begin("Other");
        Assert.Throws<PyNotImplementedError>(() => Commit(other, prepared));
        Assert.Null(Layout(other));
        var ordinary = new PyClass("Ordinary");
        PyOps.FinalizeOrdinaryClass(ordinary);
        object forged = Call(LayoutType, "PrepareClass", null, ordinary, Array.Empty<string>(), Array.Empty<object?>(), "origin")!;
        Assert.Throws<PyNotImplementedError>(() => Commit(ordinary, forged));
        Assert.IsType<PyInstance>(ordinary.Invoke([], [], []));
    }

    [Fact]
    public void BothNamedTupleOriginsRejectUnsafeObjectAllocationButKeepHelpers()
    {
        PyClass typed = Begin();
        var shared = new PyList();
        Commit(typed, Prepare(typed, ["x"], [shared]));
        var collection = (PyClass)Call(LayoutType, "CreateClass", null, "Collection", new[] { "x" }, new object?[] { shared })!;
        foreach (PyClass cls in new[] { typed, collection })
        {
            var value = Assert.IsAssignableFrom<PyTuple>(cls.Invoke([], [], []));
            Assert.Same(shared, PyObject.GetAttr(value, "x"));
            var made = Assert.IsAssignableFrom<PyTuple>(Method(cls, "_make", new PyTuple([7L])));
            Assert.Equal(7L, made.GetItem(0L));
            Assert.Equal($"object.__new__({cls.Name}) is not safe, use {cls.Name}.__new__()",
                Assert.Throws<PyTypeError>(() => ObjectNew(cls)).Message);
        }
    }

    [Fact]
    public void NativeMarkerWithoutBuilderKeepsExistingLegacyConstruction()
    {
        var cls = new PyClass("Native");
        typeof(PyClass).GetProperty("IsNativeRuntimeClass", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cls, true);
        Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }
}
