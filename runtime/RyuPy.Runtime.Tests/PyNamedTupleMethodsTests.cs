// namedtuple阶段B2的调用捕获及容器拒绝边界；C#入口测试不冒充真实源码求值顺序验收。
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNamedTupleMethodsTests
{
    private static PyClass Class(string name, params string[] fields)
    {
        Type layout = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyNamedTupleLayout")!;
        try
        {
            return (PyClass)layout.GetMethod("CreateClass", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [name, fields, Array.Empty<object?>()])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static object Instance(PyClass cls, params object?[] args) => cls.Invoke(args, [], [])!;
    private static object? Call(object? function, params object?[] args) => PyOps.CallValue(function, args, [], []);
    private static object? Get(object? value, string name) => PyObject.GetAttr(value, name);

    [Fact]
    public void CaptureRecognizesOnlyModulesAndGeneratedTupleTypes()
    {
        PyClass cls = Class("Bounded", "x");
        object value = Instance(cls, 1L);
        Assert.True(PyOps.IsModule(cls));
        Assert.True(PyOps.IsModule(value));
        Assert.Equal(1L, PyOps.CaptureModuleTarget(value, "x"));
        foreach (object ordinary in new object[] { new PyTuple([1L]), new PyList([1L]), new PyClass("Ordinary") })
        {
            Assert.False(PyOps.IsModule(ordinary));
            Assert.Same(ordinary, PyOps.CaptureModuleTarget(ordinary, "missing"));
        }
        Assert.Throws<PyAttributeError>(() => PyOps.CaptureModuleTarget(value, "missing"));
        Assert.Throws<PyAttributeError>(() => PyOps.CaptureModuleTarget(cls, "missing"));
    }

    [Fact]
    public void DirectPreparedAndCapturedCallsRespectFieldShadowing()
    {
        var field = PyNativeFunctions.CreateVariadic("field", values => (long)values.Length);
        object value = Instance(Class("Shadow", "count", "index"), field, field);
        Assert.Same(field, PyOps.PrepareMethod(value, "count"));
        Assert.Same(field, PyOps.CaptureModuleTarget(value, "index"));
        Assert.Equal(2L, PyOps.CallMethod(value, "count", [1L, 2L], [], []));
        Assert.Equal(1L, Call(Get(value, "count"), 1L));
        object? target = PyOps.CaptureModuleTarget(value, "count");
        Assert.Equal(0L, PyPic.CallCapturedMethod0(3907, true, target, "count"));
        Assert.Equal(1L, PyPic.CallCapturedMethod1(3907, true, target, "count", 1L));
        Assert.Equal(2L, PyPic.CallCapturedMethod2(3907, true, target, "count", 1L, 2L));
        Assert.Equal(3L, PyPic.CallCapturedMethod3(3907, true, target, "count", 1L, 2L, 3L));
        object other = Instance(Class("IntegerShadow", "count"), 7L);
        Assert.Equal(7L, PyOps.PrepareMethod(other, "count"));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(other, "count", [], [], []));
    }

    [Fact]
    public void OneSiteAlternatesRealLayoutsWithoutSharedClrTypeCaching()
    {
        var first = PyNativeFunctions.Create("first", ["x"], [null], 1, args => args[0]);
        var second = PyNativeFunctions.Create("second", ["x"], [null], 1, _ => 99L);
        object left = Instance(Class("Left", "count", "other"), first, second);
        object right = Instance(Class("Right", "other", "count"), first, second);
        foreach (object value in new[] { left, right, left })
        {
            object? target = PyOps.CaptureModuleTarget(value, "count");
            Assert.Same(ReferenceEquals(value, right) ? second : first, target);
            Assert.Equal(ReferenceEquals(value, right) ? 99L : 5L,
                PyPic.CallCapturedMethod1(3908, PyOps.IsModule(value), target, "count", 5L));
        }
    }

    [Fact]
    public void MissingAttributeFailsBeforeStarExpansionAndCapturePrecedesArgumentCallback()
    {
        object value = Instance(Class("Missing", "x"), 1L);
        int reads = 0;
        Assert.Throws<PyAttributeError>(() => PyOps.CallMethod(value, "missing", [PyOps.StarArg(new Once(() => reads++))], [], []));
        Assert.Equal(0, reads);
        Assert.Throws<PyAttributeError>(() => CaptureThenArgument(value, "missing", () => { reads++; return 1L; }));
        Assert.Equal(0, reads);
        var field = PyNativeFunctions.Create("field", ["x"], [null], 1, args => args[0]);
        object callable = Instance(Class("Callable", "count"), field);
        Assert.Equal(7L, CaptureThenArgument(callable, "count", () => { reads++; return 7L; }));
        Assert.Equal(1, reads);
    }

    private static object? CaptureThenArgument(object receiver, string name, Func<object?> argument)
    {
        bool captured = PyOps.IsModule(receiver);
        object? target = PyOps.CaptureModuleTarget(receiver, name);
        return PyPic.CallCapturedMethod1(3909, captured, target, name, argument());
    }

    [Fact]
    public void KeywordAndStarCallsExpandOnceAndKeepUserExceptionIdentity()
    {
        var field = PyNativeFunctions.Create("field", ["x"], [null], 1, args => args[0]);
        object value = Instance(Class("Keyword", "count"), field);
        Assert.Equal(8L, PyOps.CallMethod(value, "count", [], ["x"], [8L]));
        int reads = 0;
        Assert.Equal(7L, PyOps.CallMethod(value, "count", [PyOps.StarArg(new Once(() => reads++))], [], []));
        Assert.Equal(1, reads);
        var original = new PyValueError("field body");
        var outer = new PyTypeError("ambient");
        var throwing = PyNativeFunctions.Create("throwing", [], [], 0, _ => throw (PyBaseException)PyErrors.PrepareRaise(original));
        object fail = Instance(Class("Fail", "count"), throwing);
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            Assert.Same(original, Assert.Throws<PyValueError>(() => PyOps.CallMethod(fail, "count", [], [], [])));
            Assert.Same(outer, original.Context);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void ClassesExposeRealTupleMethodsAndFieldsStillTakePriority()
    {
        PyClass cls = Class("Values", "a", "b");
        object value = Instance(cls, 1L, 1L);
        object? count = Get(cls, "count");
        Assert.Equal(2L, Call(count, value, 1L));
        Assert.Equal(1L, Call(count, new PyTuple([1L, 2L]), 1L));
        Assert.Equal(0L, Call(Get(cls, "index"), value, 1L));
        Assert.Equal(2L, Call(Get(cls, "__len__"), value));
        Assert.Same(PyType.Get("tuple"), Get(count, "__objclass__"));
        Assert.Equal("tuple.count", Get(Get(value, "count"), "__qualname__"));
        Assert.Same(PyType.Get("tuple"), Get(Get(value, "__len__"), "__objclass__"));
        Assert.True(PyHash.Equal(Get(value, "count"), Get(value, "count")));
        Assert.Throws<PyTypeError>(() => Call(count, new PyList([1L]), 1L));
        Assert.Throws<PyTypeError>(() => Call(count));
        Assert.Throws<PyTypeError>(() => Call(Get(cls, "index"), value, 1L, 0L));
        PyClass shadow = Class("Own", "count");
        Assert.Equal("_tuplegetter", PyRepr.TypeName(Get(shadow, "count")));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(count, "__code__", 1L));
    }

    [Fact]
    public void CopyRejectsTheRecordNodeBeforeLosingTypeOrVisitingFields()
    {
        object value = Instance(Class("Record", "field"), new PyList([new PyClass("Payload")]));
        object? shallow = PyCopy.CreateAttributes()["copy"];
        object? deep = PyCopy.CreateAttributes()["deepcopy"];
        Assert.Equal("copy protocol for 'Record' is not supported", Assert.Throws<PyNotImplementedError>(() => Call(shallow, value)).PyMessage);
        Assert.Equal("copy protocol for 'Record' is not supported", Assert.Throws<PyNotImplementedError>(() => Call(deep, value)).PyMessage);
        Assert.Equal("copy protocol for 'Record' is not supported", Assert.Throws<PyNotImplementedError>(() => Call(deep, new PyList([value]))).PyMessage);
        var exact = new PyTuple([1L]);
        Assert.Same(exact, Call(shallow, exact));
        object key = Instance(Class("Key", "x"), 1L);
        var dictionary = new PyDict();
        dictionary.Set(key, 2L);
        var copied = Assert.IsType<PyDict>(Call(shallow, dictionary));
        Assert.Same(key, Assert.Single(copied.Keys));
    }

    [Fact]
    public void GetoptKeepsExactSequenceBoundaryAndExistingValidationOrder()
    {
        object? getopt = PyGetopt.CreateAttributes()["getopt"];
        object arguments = Instance(Class("Arguments", "one"), "-a");
        object declarations = Instance(Class("Declarations", "one"), "alpha");
        Assert.Equal("getopt namedtuple argument sequence is not supported",
            Assert.Throws<PyNotImplementedError>(() => Call(getopt, arguments, "a")).PyMessage);
        Assert.Equal("getopt namedtuple longopts sequence is not supported",
            Assert.Throws<PyNotImplementedError>(() => Call(getopt, new PyList(), "", declarations)).PyMessage);
        Assert.Equal("getopt custom longopts iterable is not supported",
            Assert.Throws<PyNotImplementedError>(() => Call(getopt, arguments, "a", new object())).PyMessage);
        Assert.IsType<PyTuple>(Call(getopt, new PyTuple([]), ""));
    }

    private sealed class Once(Action read) : IPyIterator
    {
        private bool _done;
        public bool MoveNext()
        {
            if (_done) return false;
            _done = true;
            read();
            return true;
        }
        public object? Current() => 7L;
    }
}
