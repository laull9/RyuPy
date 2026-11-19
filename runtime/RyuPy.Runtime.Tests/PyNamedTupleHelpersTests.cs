// namedtuple C2 六个真实 helper：绑定、闭包、实际接收者、别名与拒绝边界；公开导出另由 C3 验证。
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyNamedTupleHelpersTests
{
    private static PyClass Class(string name, string fields, object? defaults = null)
    {
        Type factory = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyNamedTupleFactory")!;
        try
        {
            return (PyClass)factory.GetMethod("Invoke", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [new object?[] { name, fields }, new[] { "defaults", "module" }, new object?[] { defaults ?? PyNone.Instance, "helpers.test" }])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw;
        }
    }
    private static object? Get(object value, string name) => PyObject.GetAttr(value, name);
    private static object Instance(PyClass cls, params object?[] args) => cls.Invoke(args, [], [])!;
    private static object? Call(object? callable, params object?[] args) => PyOps.CallValue(callable, args, [], []);
    private static object? Keywords(object? callable, object?[] args, string[] names, object?[] values) => PyOps.CallValue(callable, args, names, values);
    private static object? Method(object receiver, string name, params object?[] args) => Call(Get(receiver, name), args);
    private static PyTuple Tuple(object? value) => Assert.IsAssignableFrom<PyTuple>(value);

    [Fact]
    public void HelpersBindAsOrdinaryStaticAndClassMethodsWithStableMetadata()
    {
        PyClass cls = Class("Pair", "left right", new PyTuple([9L])); object pair = Instance(cls, 1L);
        object make = Get(cls, "_make")!; object fromInstance = Get(pair, "_make")!;
        Assert.Same(cls, Get(make, "__self__")); Assert.Same(cls, Get(fromInstance, "__self__"));
        Assert.Same(Get(make, "__func__"), Get(fromInstance, "__func__"));
        Assert.Same(Get(cls, "__new__"), Get(pair, "__new__"));
        foreach (string name in new[] { "_replace", "_asdict", "__getnewargs__", "__repr__" })
        {
            object function = Get(cls, name)!; object bound = Get(pair, name)!;
            Assert.Same(pair, Get(bound, "__self__")); Assert.Same(function, Get(bound, "__func__"));
            Assert.Equal(name, Get(function, "__name__")); Assert.Equal("Pair." + name, Get(bound, "__qualname__"));
            Assert.Equal("collections", Get(function, "__module__")); Assert.Equal("function", PyRepr.TypeName(function));
            Assert.Equal("method", PyRepr.TypeName(bound)); Assert.Same(PyNone.Instance, Get(function, "__defaults__"));
        }
        Assert.Equal("namedtuple_Pair", Get(Get(cls, "__new__")!, "__module__"));
        Assert.Equal("Create new instance of Pair(left, right)", Get(Get(cls, "__new__")!, "__doc__"));
        foreach (string name in new[] { "__new__", "_replace", "__repr__", "_asdict", "__getnewargs__" })
            foreach (string metadata in new[] { "__qualname__", "__module__", "__doc__" })
                Assert.Same(Get(Get(cls, name)!, metadata), Get(Get(cls, name)!, metadata));
        foreach (string metadata in new[] { "__qualname__", "__module__", "__doc__" })
            Assert.Same(Get(make, metadata), Get(fromInstance, metadata));
        // 原内部阶段的未发布守门在 C3 升级为真实、稳定导出，不删除此处覆盖。
        object? factory = PyCollections.CreateAttributes()["namedtuple"];
        Assert.IsAssignableFrom<IPyNativeCallable>(factory);
        Assert.Same(factory, PyCollections.CreateAttributes()["namedtuple"]);
    }

    [Fact]
    public void ConstructorFunctionKeepsDefaultsIdentityAndOnlyAllocatesItsOwnClass()
    {
        var shared = new PyList(); var defaults = new PyTuple([shared]);
        PyClass cls = Class("Box", "value", defaults); object constructor = Get(cls, "__new__")!;
        Assert.Same(defaults, Get(constructor, "__defaults__")); Assert.Same(Get(constructor, "__defaults__"), Get(constructor, "__defaults__"));
        object value = Call(constructor, cls)!; Assert.Same(cls, PyObject.TypeOf(value)); Assert.Same(shared, Get(value, "value"));
        object byKeyword = Keywords(constructor, [], ["_cls", "value"], [cls, 7L])!; Assert.Equal(7L, Get(byKeyword, "value"));
        PyClass other = Class("Other", "a b");
        Assert.Throws<PyNotImplementedError>(() => Call(constructor, other));
        Assert.Throws<PyNotImplementedError>(() => Call(constructor, PyType.Get("tuple")));
        Assert.Equal("Box.__new__() got an unexpected keyword argument 'bad'", Assert.Throws<PyTypeError>(() => Keywords(constructor, [other], ["bad"], [1L])).PyMessage);
        Assert.Same(PyNone.Instance, Get(Get(Class("NoneDefaults", "x"), "__new__")!, "__defaults__"));
        var empty = new PyTuple([]); Assert.Same(empty, Get(Get(Class("EmptyDefaults", "x", empty), "__new__")!, "__defaults__"));
        PyClass listDefaults = Class("FromList", "x", new PyList()); object read = Get(Get(listDefaults, "__new__")!, "__defaults__")!;
        Assert.Empty(Assert.IsType<PyTuple>(read).Items); Assert.Same(read, Get(Get(listDefaults, "__new__")!, "__defaults__"));
    }

    [Fact]
    public void MakeConsumesAllInputAndNeverFillsConstructorDefaults()
    {
        PyClass cls = Class("Pair", "left right", new PyTuple([9L])); object make = Get(cls, "_make")!;
        var shared = new PyList(); var input = new PyTuple([1L, shared]);
        object first = Call(make, input)!; object second = Keywords(make, [], ["iterable"], [input])!;
        Assert.Same(cls, PyObject.TypeOf(first)); Assert.NotSame(first, second); Assert.NotSame(input.Items, Tuple(first).Items);
        Assert.Same(shared, Get(first, "right"));
        var log = new List<int>();
        Assert.Equal("Expected 2 arguments, got 1", Assert.Throws<PyTypeError>(() => Call(make, new Stream([1L], log))).PyMessage);
        Assert.Equal(new[] { 0, 1 }, log); log.Clear();
        Assert.Equal("Expected 2 arguments, got 4", Assert.Throws<PyTypeError>(() => Call(make, new Stream([1L, 2L, 3L, 4L], log))).PyMessage);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, log); log.Clear();
        var original = new PyRuntimeError("tail");
        Assert.Same(original, Assert.Throws<PyRuntimeError>(() => Call(make, new Stream([1L, 2L, 3L], log, original))));
        object raw = Get(make, "__func__")!; Assert.Same(cls, PyObject.TypeOf(Call(raw, cls, input)));
        Assert.Throws<PyNotImplementedError>(() => Call(raw, Class("Other", "a b"), input));
        Assert.Equal("Pair._make() missing 1 required positional argument: 'iterable'", Assert.Throws<PyTypeError>(() => Call(make)).PyMessage);
    }

    [Fact]
    public void ReplaceCreatesFreshSameClassAndPreservesAliasesAndSelfField()
    {
        var shared = new PyList(); PyClass cls = Class("Pair", "left right"); object pair = Instance(cls, 1L, shared);
        object copy = Method(pair, "_replace")!; Assert.NotSame(pair, copy); Assert.Same(cls, PyObject.TypeOf(copy)); Assert.Same(shared, Get(copy, "right"));
        object changed = Keywords(Get(pair, "_replace"), [], ["left"], [9L])!;
        Assert.Equal(9L, Get(changed, "left")); Assert.Equal(1L, Get(pair, "left")); Assert.Same(shared, Get(changed, "right"));
        Assert.Equal("Got unexpected field names: ['z', 'a']", Assert.Throws<PyValueError>(() => Keywords(Get(pair, "_replace"), [], ["z", "a"], [1L, 2L])).PyMessage);
        PyClass selfClass = Class("Self", "self cls"); object self = Instance(selfClass, 1L, 2L);
        object replaced = Keywords(Get(self, "_replace"), [], ["self"], [3L])!; Assert.Equal(3L, Get(replaced, "self"));
        Assert.Equal("Self._replace() missing 1 required positional argument: 'self'",
            Assert.Throws<PyTypeError>(() => Keywords(Get(selfClass, "_replace"), [], ["self"], [3L])).PyMessage);
        Assert.Throws<PyTypeError>(() => Method(pair, "_replace", 1L));
    }

    [Fact]
    public void UnboundHelpersKeepDefiningFieldsButUseTheActualReceiverClass()
    {
        PyClass pairClass = Class("Pair", "left right"); PyClass peerClass = Class("Peer", "alpha beta");
        PyClass shortClass = Class("Short", "only"); PyClass longClass = Class("Long", "a b c", new PyTuple([99L]));
        object peer = Instance(peerClass, 1L, 2L); object shorter = Instance(shortClass, 3L); object longer = Instance(longClass, 4L, 5L, 6L);
        object replace = Get(pairClass, "_replace")!;
        object changed = Keywords(replace, [peer], ["left"], [9L])!;
        Assert.Same(peerClass, PyObject.TypeOf(changed)); Assert.Equal<object?>([9L, 2L], Tuple(changed).Items);
        object small = Keywords(replace, [shorter], ["left"], [8L])!;
        Assert.Same(shortClass, PyObject.TypeOf(small)); Assert.Equal<object?>([8L], Tuple(small).Items);
        Assert.Equal("Got unexpected field names: ['right']", Assert.Throws<PyValueError>(() => Keywords(replace, [shorter], ["right"], [9L])).PyMessage);
        Assert.Equal("Expected 3 arguments, got 2", Assert.Throws<PyTypeError>(() => Keywords(replace, [longer], ["unknown"], [7L])).PyMessage);
        Assert.Equal<object?>([4L, 5L, 6L], Assert.IsType<PyTuple>(Call(Get(pairClass, "__getnewargs__"), longer)).Items);
        PyDict dict = Assert.IsType<PyDict>(Call(Get(pairClass, "_asdict"), peer)); Assert.Equal(1L, dict.Get("alpha")); Assert.Equal(2L, dict.Get("beta"));
    }

    [Fact]
    public void ReprUsesDefiningLabelsAndPreservesConversionBeforeLengthErrors()
    {
        var log = new List<string>(); var elementClass = new PyClass("Element");
        elementClass.SetAttr("__repr__", PyNativeFunctions.CreateVariadic("repr", _ => { log.Add("repr"); return "item"; }));
        object element = new PyInstance(elementClass); PyClass pair = Class("Pair", "left right");
        object repr = Get(pair, "__repr__")!;
        object peer = Instance(Class("Peer", "a b"), element, 2L);
        Assert.Equal("Peer(left=item, right=2)", Call(repr, peer)); Assert.Single(log); log.Clear();
        Assert.Equal("not enough arguments for format string", Assert.Throws<PyTypeError>(() => Call(repr, Instance(Class("Short", "a"), element))).PyMessage);
        Assert.Single(log); log.Clear();
        Assert.Equal("not all arguments converted during string formatting", Assert.Throws<PyTypeError>(() => Call(repr, Instance(Class("Long", "a b c"), element, 2L, 3L))).PyMessage);
        Assert.Single(log);
        var original = new PyRuntimeError("repr-error"); elementClass.SetAttr("__repr__", PyNativeFunctions.CreateVariadic("repr", _ => throw original));
        Assert.Same(original, Assert.Throws<PyRuntimeError>(() => Call(repr, peer)));
    }

    [Fact]
    public void AsdictAndGetnewargsReturnFreshPlainContainersWithoutCopyingElements()
    {
        var shared = new PyList(); PyClass cls = Class("Pair", "left right"); object pair = Instance(cls, 1L, shared);
        PyDict first = Assert.IsType<PyDict>(Method(pair, "_asdict")); PyDict second = Assert.IsType<PyDict>(Method(pair, "_asdict"));
        Assert.NotSame(first, second); Assert.Same(shared, first.Get("right")); first.Set("left", 9L); Assert.Equal(1L, Get(pair, "left"));
        PyTuple args = Assert.IsType<PyTuple>(Method(pair, "__getnewargs__")); Assert.NotSame(pair, args); Assert.Same(shared, args.Items[1]);
        Assert.NotSame(args, Method(pair, "__getnewargs__")); Assert.Equal("Pair(left=1, right=[])", Method(pair, "__repr__"));
        object empty = Instance(Class("Empty", "")); Assert.Empty(Assert.IsType<PyTuple>(Method(empty, "__getnewargs__")).Items);
        Assert.Empty(Assert.IsType<PyDict>(Method(empty, "_asdict")).Keys); Assert.Equal("Empty()", Method(empty, "__repr__"));
        Assert.Throws<PyNotImplementedError>(() => Call(PyCopy.CreateAttributes()["copy"], pair)); Assert.Throws<PyNotImplementedError>(() => Call(PyCopy.CreateAttributes()["deepcopy"], pair));
    }

    [Fact]
    public void ExplicitFunctionDescriptorBindsReceiverWithoutRequiringSameClass()
    {
        PyClass cls = Class("Pair", "a b"); object function = Get(cls, "_asdict")!;
        object other = Instance(Class("Other", "x y"), 3L, 4L); object getter = Get(function, "__get__")!;
        Assert.Same(function, Call(getter, PyNone.Instance, new object()));
        Assert.Equal("__get__(None, None) is invalid", Assert.Throws<PyTypeError>(() => Call(getter, PyNone.Instance)).PyMessage);
        Assert.Equal("__get__(None, None) is invalid", Assert.Throws<PyTypeError>(() => Call(getter, PyNone.Instance, PyNone.Instance)).PyMessage);
        object bound = Call(getter, other, cls)!; Assert.Same(other, Get(bound, "__self__")); Assert.Same(function, Get(bound, "__func__"));
        Assert.Equal(3L, Assert.IsType<PyDict>(Call(bound)).Get("x"));
        Assert.Throws<PyNotImplementedError>(() => Call(function, new PyTuple([1L, 2L])));
        Assert.Throws<PyTypeError>(() => Keywords(function, [other], ["self"], [other]));
    }

    [Fact]
    public void BoundMethodEqualityAndHashRespectFunctionAndReceiverSemantics()
    {
        PyClass cls = Class("Pair", "a b"); object first = Instance(cls, 1L, 2L); object equal = Instance(cls, 1L, 2L); object unequal = Instance(cls, 1L, 3L);
        object a = Get(first, "_asdict")!; object b = Get(equal, "_asdict")!;
        Assert.True(PyHash.Equal(a, Get(first, "_asdict"))); Assert.True(PyHash.Equal(a, b)); Assert.Equal(PyHash.Of(a), PyHash.Of(b));
        Assert.False(PyHash.Equal(a, Get(unequal, "_asdict"))); Assert.False(PyHash.Equal(a, Get(first, "__repr__")));
        Assert.False(PyHash.Equal(a, Get(Instance(Class("Pair", "a b"), 1L, 2L), "_asdict")));
        Assert.True(PyHash.Equal(Get(cls, "_make"), Get(first, "_make")));
        Assert.Throws<PyTypeError>(() => PyHash.Of(Get(Instance(cls, 1L, new PyList()), "_asdict")));
    }

    [Fact]
    public void ProtectedMetadataAndLocalErrorsKeepTheExistingSafetyBoundaries()
    {
        PyClass cls = Class("Pair", "a b"); object pair = Instance(cls, 1L, 2L); object function = Get(cls, "_asdict")!; object bound = Get(pair, "_asdict")!;
        foreach (object target in new[] { function, bound, Get(cls, "__new__")!, Get(Get(cls, "_make")!, "__func__")!, Get(function, "__get__")! })
        {
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(target, "__defaults__", new PyTuple([])));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(target, "__code__"));
            Assert.Throws<PyNotImplementedError>(() => Get(target, "__dict__"));
        }
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("helper_probe.py", "caller"); PyTraceback.SetLine(19);
            PyTypeError error = Assert.Throws<PyTypeError>(() => Call(function));
            Assert.Same(ambient, error.Context); Assert.Equal(new PyStackFrame("helper_probe.py", "caller", 19), error.Frames[0]);
            PyValueError fields = Assert.Throws<PyValueError>(() => Keywords(Get(pair, "_replace"), [], ["bad"], [1L]));
            Assert.Same(ambient, fields.Context); Assert.Equal(new PyStackFrame("helper_probe.py", "caller", 19), fields.Frames[0]);
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    private sealed class Stream(object?[] values, List<int> log, PyBaseException? tail = null) : IPyIterator
    {
        private int _index = -1;
        public bool MoveNext()
        {
            log.Add(++_index); if (_index < values.Length) return true;
            if (tail is not null) throw tail; return false;
        }
        public object? Current() => values[_index];
    }
}
