// namedtuple阶段A只验内部载体和绑定：不把CLR派生关系当成Python协议已经完成。
// 使用测试侧反射进入internal入口，不为测试新增Python导出或生产反射路径。
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyNamedTupleLayoutTests
{
    private static readonly Type LayoutType = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyNamedTupleLayout")!;

    private static T Call<T>(string name, params object?[] args)
    {
        try
        {
            return (T)LayoutType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, args)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static PyClass Class(string name, string[] fields, object?[]? defaults = null) =>
        Call<PyClass>("CreateClass", name, fields, defaults ?? []);

    private static PyTuple Construct(PyClass cls, object?[] args, string[]? names = null, object?[]? values = null) =>
        Call<PyTuple>("Construct", cls, args, names ?? [], values ?? []);

    private static PyClass Owner(PyTuple value) => (PyClass)value.GetType()
        .GetProperty("Class", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(value)!;

    private static object Layout(PyClass cls) => typeof(PyClass)
        .GetProperty("NamedTupleLayout", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cls)!;

    [Fact]
    public void SameSpellingCreatesDistinctClassesAndNewInstances()
    {
        PyClass first = Class("Coordinate", ["x", "y"]);
        PyClass second = Class("Coordinate", ["x", "y"]);
        PyTuple left = Construct(first, [1L, 2L]);
        PyTuple right = Construct(second, [1L, 2L]);
        Assert.NotSame(first, second);
        Assert.NotSame(left, Construct(first, [1L, 2L]));
        Assert.Same(first, Owner(left));
        Assert.Same(second, Owner(right));
        Assert.Null(first.BuiltinBase);
        Assert.False(first.IsException);
        Assert.IsAssignableFrom<PyTuple>(left);
    }

    [Fact]
    public void EmptyAndSingleFieldInstancesKeepIndependentIdentity()
    {
        PyClass empty = Class("Empty", []);
        PyTuple first = Construct(empty, []);
        PyTuple second = Construct(empty, []);
        Assert.NotSame(first, second);
        Assert.NotSame(first.Items, second.Items);
        Assert.Equal(0, first.Length);
        Assert.Same(empty, Owner(first));
        PyClass one = Class("One", ["value"]);
        var item = new PyList();
        PyTuple single = Construct(one, [item]);
        Assert.Equal(1, single.Length);
        Assert.Same(item, single.GetItem(-1L));
    }

    [Fact]
    public void PositionalKeywordAndMixedCallsUseFieldOrder()
    {
        PyClass cls = Class("Packet", ["left", "middle", "right"]);
        Assert.Equal<object?>([1L, 2L, 3L], Construct(cls, [1L, 2L, 3L]).Items);
        Assert.Equal<object?>([1L, 2L, 3L], Construct(cls, [], ["right", "left", "middle"], [3L, 1L, 2L]).Items);
        Assert.Equal<object?>([1L, 2L, 3L], Construct(cls, [1L], ["right", "middle"], [3L, 2L]).Items);
    }

    [Fact]
    public void HiddenClassParameterDoesNotOccupyLegalFields()
    {
        PyClass cls = Class("Names", ["self", "cls", "count", "index"]);
        PyTuple value = Construct(cls, [], ["index", "cls", "self", "count"], [4L, 2L, 1L, 3L]);
        Assert.Equal<object?>([1L, 2L, 3L, 4L], value.Items);
        Assert.Equal(4, value.Length);
        Assert.Same(cls, Owner(value));
        Assert.Throws<PyTypeError>(() => Construct(cls, [1L, 2L, 3L, 4L], ["_cls"], [cls]));
    }

    [Fact]
    public void BindingFailuresRemainPythonTypeErrors()
    {
        PyClass cls = Class("Pair", ["x", "y"]);
        Assert.Throws<PyTypeError>(() => Construct(cls, [1L]));
        Assert.Throws<PyTypeError>(() => Construct(cls, [1L, 2L, 3L]));
        Assert.Throws<PyTypeError>(() => Construct(cls, [1L], ["x", "y"], [2L, 3L]));
        Assert.Throws<PyTypeError>(() => Construct(cls, [], ["x", "x", "y"], [1L, 2L, 3L]));
        Assert.Throws<PyTypeError>(() => Construct(cls, [1L, 2L], ["unknown"], [3L]));
        Assert.Throws<PyTypeError>(() => Construct(new PyClass("Ordinary"), []));
        Assert.Throws<PyTypeError>(() => Class("TooMany", ["x"], [1L, 2L]));
    }

    [Fact]
    public void DefaultsAlignRightAndPreserveSharedValues()
    {
        var shared = new PyList();
        object?[] defaults = [shared, PyNone.Instance];
        PyClass cls = Class("Defaults", ["required", "shared", "none"], defaults);
        PyTuple first = Construct(cls, [1L]);
        PyTuple second = Construct(cls, [2L]);
        Assert.Same(shared, first.Items[1]);
        Assert.Same(shared, second.Items[1]);
        Assert.Same(PyNone.Instance, first.Items[2]);
        Assert.NotSame(first.Items, second.Items);
        defaults[0] = new PyList();
        defaults[1] = 7L;
        PyTuple later = Construct(cls, [3L]);
        Assert.Same(shared, later.Items[1]);
        Assert.Same(PyNone.Instance, later.Items[2]);
        shared.Append(9L);
        Assert.Equal(1, ((PyList)later.Items[1]!).Count);
        PyTuple explicitNone = Construct(cls, [PyNone.Instance, PyNone.Instance, PyNone.Instance]);
        Assert.Same(PyNone.Instance, explicitNone.Items[0]);
        Assert.Same(PyNone.Instance, explicitNone.Items[1]);
        Assert.Throws<PyTypeError>(() => Construct(cls, []));
    }

    [Fact]
    public void EveryFieldCanHaveADefaultWithoutLosingNone()
    {
        PyClass cls = Class("Optional", ["x", "y"], [PyNone.Instance, 8L]);
        PyTuple result = Construct(cls, []);
        Assert.Same(PyNone.Instance, result.Items[0]);
        Assert.Equal(8L, result.Items[1]);
        Assert.Equal<object?>([4L, 8L], Construct(cls, [], ["x"], [4L]).Items);
    }

    [Fact]
    public void InputArraysCannotMutateStoredSchemaOrPayload()
    {
        string[] fields = ["x", "y"];
        PyClass cls = Class("Owned", fields);
        fields[0] = "changed";
        var shared = new PyList();
        object?[] positional = [shared, 2L];
        PyTuple first = Construct(cls, positional);
        Assert.NotSame(positional, first.Items);
        positional[0] = 99L;
        Assert.Same(shared, first.Items[0]);
        string[] keywords = ["x", "y"];
        object?[] keywordValues = [shared, 3L];
        PyTuple second = Construct(cls, [], keywords, keywordValues);
        keywordValues[0] = 100L;
        keywords[0] = "changed";
        Assert.Same(shared, second.Items[0]);
        Assert.NotSame(first.Items, second.Items);
        object layout = Layout(cls);
        Assert.Equal("x", LayoutType.GetMethod("FieldName", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(layout, [0]));
        object?[] lookup = ["x", 0];
        Assert.Equal(true, LayoutType.GetMethod("TryFieldIndex", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(layout, lookup));
        Assert.Equal(0, lookup[1]);
        Assert.Throws<PyTypeError>(() => Construct(cls, [], ["changed", "y"], [1L, 2L]));
    }

    [Fact]
    public void FactoryExportDoesNotChangeTupleStorage()
    {
        // 原内部阶段的未发布守门在 C3 升级为真实、稳定导出，不删除此处覆盖。
        object? factory = PyCollections.CreateAttributes()["namedtuple"];
        Assert.IsAssignableFrom<IPyNativeCallable>(factory);
        Assert.Same(factory, PyCollections.CreateAttributes()["namedtuple"]);
        Assert.False(typeof(PyTuple).IsSealed);
        Assert.False(typeof(PyTuple).GetProperty("Length")!.GetMethod!.IsVirtual);
        Assert.False(typeof(PyTuple).GetProperty("Items")!.GetMethod!.IsVirtual);
        Assert.False(typeof(PyTuple).GetMethod("GetItem", [typeof(long)])!.IsVirtual);
        Assert.False(typeof(PyTuple).GetMethod("GetItem", [typeof(object)])!.IsVirtual);
        Assert.False(typeof(PyTuple).GetMethod("Contains")!.IsVirtual);
        Assert.Single(typeof(PyTuple).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
        Assert.True(Construct(Class("Closed", []), []).GetType().IsSealed);
    }
}
