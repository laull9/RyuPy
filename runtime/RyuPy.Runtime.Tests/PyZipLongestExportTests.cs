// zip_longest 阶段 B：真实 itertools 类型、实例公开入口、别名身份和准确有限反射边界。
using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyZipLongestExportTests
{
    private static PyNativeType Type => Assert.IsType<PyNativeType>(PyItertools.CreateAttributes()["zip_longest"]);
    private static object New(params object?[] args) => PyOps.CallValue(Type, args, [], [])!;
    private static object? Builtin(string name, params object?[] args) => PyOps.CallBuiltin(name, args, [], []);

    [Fact]
    public void PublicExportIsAStableRealTypeAndIteratorReturnsItself()
    {
        PyNativeType type = Type; object value = New(new PyTuple([1L]));
        Assert.Same(type, Type); Assert.Same(type, PyObject.TypeOf(value)); Assert.Same(type, PyObject.GetAttr(value, "__class__"));
        Assert.Equal("zip_longest", type.GetAttr("__name__")); Assert.Equal("itertools", type.GetAttr("__module__"));
        Assert.Equal(true, Builtin("isinstance", value, type)); Assert.False(type.Accepts(new Pretender()));
        Assert.Same(value, Builtin("iter", value)); Assert.Same(value, PyOps.IterStart(value));
        object? iter = PyObject.GetAttr(value, "__iter__"); Assert.Same(value, PyOps.CallValue(iter, [], [], []));
        Assert.Equal<object?>([1L], Assert.IsType<PyTuple>(Builtin("next", value)).Items);
        Assert.Throws<PyStopIteration>(() => Builtin("next", value));
        var fallback = new PyList(); Assert.Same(fallback, Builtin("next", value, fallback));
    }

    [Fact]
    public void ExplicitNextListTupleAndLoopEntrypointsShareTheSameStateMachine()
    {
        object value = New(new PyTuple([1L, 2L]), new PyTuple([3L])); object? next = PyObject.GetAttr(value, "__next__");
        PyTuple first = Assert.IsType<PyTuple>(PyOps.CallValue(next, [], [], []));
        PyTuple second = Assert.IsType<PyTuple>(PyOps.CallValue(next, [], [], []));
        Assert.Equal<object?>([1L, 3L], first.Items); Assert.Equal<object?>([2L, PyNone.Instance], second.Items); Assert.NotSame(first, second);
        Assert.Throws<PyStopIteration>(() => PyOps.CallValue(next, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(next, [1L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(next, [], ["bad"], [1L]));
        Assert.Equal(2, Assert.IsType<PyList>(Builtin("list", New(new PyTuple([1L, 2L])))).Count);
        Assert.Equal(2, Assert.IsType<PyTuple>(Builtin("tuple", New(new PyTuple([1L, 2L])))).Length);
        object loop = PyOps.IterStart(New(new PyTuple([5L])));
        Assert.True(PyOps.IterNextOk(loop)); Assert.Equal<object?>([5L], Assert.IsType<PyTuple>(PyOps.IterValue(loop)).Items); Assert.False(PyOps.IterNextOk(loop));
    }

    [Fact]
    public void ModuleRebindingDoesNotChangeSavedTypeOrExistingInstanceIdentity()
    {
        PyNativeModules.Register(); PyModule module = PyModules.Import("itertools"); object? original = module.GetAttr("zip_longest");
        PyNativeType alias = Assert.IsType<PyNativeType>(original); object value = PyOps.CallValue(alias, [], [], [])!;
        try
        {
            module.SetAttr("zip_longest", 7L); Assert.Equal(7L, module.GetAttr("zip_longest"));
            Assert.Same(alias, PyObject.TypeOf(value)); Assert.Same(alias, PyObject.GetAttr(value, "__class__"));
            Assert.Equal(true, Builtin("isinstance", value, alias));
            object later = PyOps.CallValue(alias, [new PyTuple([9L])], [], [])!;
            Assert.Same(alias, PyObject.TypeOf(later)); Assert.Equal<object?>([9L], Assert.IsType<PyTuple>(Builtin("next", later)).Items);
        }
        finally { module.SetAttr("zip_longest", original); }
    }

    [Fact]
    public void FillAttributeIsAbsentAndUnsupportedStateMutationCannotPretendToWork()
    {
        object value = New(); const string message = "'itertools.zip_longest' object has no attribute 'fillvalue'";
        Assert.Equal(message, Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, "fillvalue")).PyMessage);
        Assert.Equal(message, Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, "fillvalue", 1L)).PyMessage);
        Assert.Equal(message, Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(value, "fillvalue")).PyMessage);
        foreach (string name in new[] { "__reduce__", "__reduce_ex__", "__setstate__" })
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(value, name));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(value, "__class__", PyType.Get("object")));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "__next__"));
        Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(Type));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyCopy.CreateAttributes()["copy"], [value], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyCopy.CreateAttributes()["deepcopy"], [value], [], []));
        Assert.Same(Type, PyObject.GetAttr(value, "__class__"));
    }

    [Fact]
    public void PublicErrorsPrepareTheCallerWithoutAddingAnItertoolsFrame()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("zip_export.py", "caller"); PyTraceback.SetLine(31);
            PyTypeError error = Assert.Throws<PyTypeError>(() => PyOps.CallValue(Type, [], ["bad"], [1L]));
            Assert.Same(ambient, error.Context); Assert.Equal(new PyStackFrame("zip_export.py", "caller", 31), Assert.Single(error.Frames));
            PyAttributeError missing = Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(New(), "fillvalue"));
            Assert.Same(ambient, missing.Context); Assert.Equal(new PyStackFrame("zip_export.py", "caller", 31), Assert.Single(missing.Frames));
            Assert.Equal(1L, PyTraceback.CaptureDepth()); Assert.Same(ambient, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    private sealed class Pretender : PyNativeObject
    {
        public override string TypeName => "zip_longest";
        public override object? GetAttr(string name) => throw Missing(name);
    }
}
