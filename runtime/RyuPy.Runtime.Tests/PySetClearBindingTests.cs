// set.clear 的入口、绑定失败不变性和保存方法合同；只使用已冻结的精确 set 通过域。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PySetClearBindingTests
{
    private static readonly string[] Routes = ["direct", "prepare", "getattr", "invoke", "pic", "saved-pic"];

    private static object? Call(string route, PySet set, object?[] args, string[] names, object?[] values)
    {
        if (route == "direct") return PyOps.CallMethod(set, "clear", args, names, values);
        if (route == "pic") return PyPic.CallMethod(3925, set, "clear", args, names, values);
        object? method = route == "prepare" ? PyOps.PrepareMethod(set, "clear") : PyObject.GetAttr(set, "clear");
        if (route == "invoke") return Assert.IsType<PyBuiltinMethod>(method).Invoke(args, names, values);
        if (route == "saved-pic") return PyPic.CallValue(3926, method, args, names, values);
        return PyOps.CallValue(method, args, names, values);
    }

    private static PyTypeError BindingError(Action call, string message)
    {
        var error = Assert.Throws<PyTypeError>(call);
        Assert.Equal(message, error.PyMessage);
        Assert.Equal(message, Assert.Single(error.Args));
        return error;
    }

    [Fact]
    public void EveryEntryClearsInPlaceReturnsNoneAndAllowsEmptyRepetition()
    {
        foreach (string route in Routes)
        foreach (object?[] values in new object?[][] { [], [1L], [1L, 2L, PyNone.Instance] })
        {
            var set = new PySet(values); PySet alias = set; var storage = set.Items;
            Assert.Same(PyNone.Instance, Call(route, set, [], [], []));
            Assert.Same(set, alias); Assert.Same(storage, set.Items); Assert.Equal(0, alias.Count);
            Assert.Same(PyNone.Instance, Call(route, set, [], [], []));
            set.Add(9L);
            Assert.Same(PyNone.Instance, Call(route, set, [], [], []));
            Assert.Equal(0, alias.Count); Assert.Same(storage, set.Items);
        }
    }

    [Fact]
    public void KeywordsWinOverArityAndAllBindingFailuresPreserveSetAndIterator()
    {
        foreach (string route in Routes)
        foreach (int count in new[] { 0, 1, 2, 3 })
        foreach (bool keyword in new[] { false, true })
        {
            if (!keyword && count == 0) continue;
            var set = new PySet([1L, 2L]); var storage = set.Items;
            var before = set.Items.ToArray(); IPyIterator iterator = PyIter.Create(set);
            object?[] args = Enumerable.Repeat<object?>(new PyList(), count).ToArray();
            BindingError(() => Call(route, set, args, keyword ? ["bad"] : [], keyword ? [0L] : []),
                keyword ? "set.clear() takes no keyword arguments" : $"set.clear() takes no arguments ({count} given)");
            Assert.Same(storage, set.Items); Assert.Equal(before, set.Items.ToArray());
            foreach (object item in before)
            {
                Assert.True(iterator.MoveNext()); Assert.Same(item, iterator.Current());
            }
            Assert.False(iterator.MoveNext());
        }
    }

    [Fact]
    public void SavedAndPreparedMethodsCaptureReceiverWithoutCallingOrCheckingArity()
    {
        foreach (bool prepare in new[] { false, true })
        {
            var first = new PySet([1L]); var second = new PySet([2L]); object receiver = first;
            object? method = prepare ? PyOps.PrepareMethod(receiver, "clear") : PyObject.GetAttr(receiver, "clear");
            Assert.Equal(1, first.Count); receiver = second;
            Assert.Same(first, PyObject.GetAttr(method, "__self__"));
            Assert.Equal("clear", PyObject.GetAttr(method, "__name__"));
            Assert.Equal("set.clear", PyObject.GetAttr(method, "__qualname__"));
            Assert.Same(PyNone.Instance, PyObject.GetAttr(method, "__module__"));
            BindingError(() => PyOps.CallValue(method, [0L], [], []), "set.clear() takes no arguments (1 given)");
            Assert.Equal(1, first.Count); Assert.Same(second, receiver);
            Assert.Same(PyNone.Instance, PyOps.CallValue(method, [], [], []));
            Assert.Equal(0, first.Count); Assert.Equal(1, second.Count);
            first.Add(3L);
            Assert.Same(PyNone.Instance, PyOps.CallValue(method, [], [], []));
            Assert.Equal(0, first.Count); Assert.Equal(1, second.Count);
        }
    }

    [Fact]
    public void DirectAndSavedPicFixedArityAndArraySitesPreserveFailureState()
    {
        foreach (int site in new[] { 3927, -1 })
        foreach (bool saved in new[] { false, true })
        foreach (int count in new[] { 0, 1, 2, 3, 4, 0 })
        {
            var set = new PySet([1L]); object? target = saved ? PyObject.GetAttr(set, "clear") : set;
            object? Invoke() => (saved, count) switch
            {
                (false, 0) => PyPic.CallMethod0(site, target, "clear"),
                (false, 1) => PyPic.CallMethod1(site, target, "clear", 0L),
                (false, 2) => PyPic.CallMethod2(site, target, "clear", 0L, 0L),
                (false, 3) => PyPic.CallMethod3(site, target, "clear", 0L, 0L, 0L),
                (false, _) => PyPic.CallMethod(site, target, "clear", [0L, 0L, 0L, 0L], [], []),
                (true, 0) => PyPic.CallValue0(site, target),
                (true, 1) => PyPic.CallValue1(site, target, 0L),
                (true, 2) => PyPic.CallValue2(site, target, 0L, 0L),
                (true, 3) => PyPic.CallValue3(site, target, 0L, 0L, 0L),
                _ => PyPic.CallValue(site, target, [0L, 0L, 0L, 0L], [], []),
            };
            if (count == 0)
            {
                Assert.Same(PyNone.Instance, Invoke()); Assert.Equal(0, set.Count);
            }
            else
            {
                BindingError(() => Invoke(), $"set.clear() takes no arguments ({count} given)");
                Assert.Equal(1, set.Count); Assert.True(set.Contains(1L));
            }
        }
    }

    [Fact]
    public void ClearDoesNotCallStoredKeysHashOrEquality()
    {
        foreach (string route in Routes)
        {
            int hashes = 0, equalities = 0; bool reject = false;
            var cls = new PyClass("ClearKey");
            cls.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ =>
            {
                hashes++; if (reject) throw new PyValueError("clear must not hash"); return 17L;
            }));
            cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("__eq__", _ =>
            {
                equalities++; throw new PyValueError("clear must not compare");
            }));
            var key = new PyInstance(cls); var set = new PySet([key]); reject = true;
            int before = hashes;
            Assert.Same(PyNone.Instance, Call(route, set, [], [], []));
            Assert.Equal(before, hashes); Assert.Equal(0, equalities); Assert.Equal(0, set.Count);
            GC.KeepAlive(key);
        }
    }

    [Fact]
    public void ExistingSuccessfulStarAndDictionaryExpansionUseTheSameBinder()
    {
        foreach (bool saved in new[] { false, true })
        {
            var set = new PySet([1L]); object? method = PyObject.GetAttr(set, "clear");
            object? Invoke(object?[] args, object?[] names, object?[] values) => saved
                ? PyOps.CallValue(method, args, names, values) : PyOps.CallMethod(set, "clear", args, names, values);
            var keywords = new PyDict(); keywords.Set("bad", 0L);
            BindingError(() => Invoke([PyOps.StarArg(new PyList([0L]))], [null], [keywords]),
                "set.clear() takes no keyword arguments");
            BindingError(() => Invoke([PyOps.StarArg(new PyList([0L, 1L]))], [], []),
                "set.clear() takes no arguments (2 given)");
            Assert.Equal(1, set.Count);
            Assert.Same(PyNone.Instance, Invoke([PyOps.StarArg(new PyList())], [null], [new PyDict()]));
            Assert.Equal(0, set.Count);
        }
    }

    [Fact]
    public void StarExpansionFailureKeepsUserExceptionIdentityAndSetState()
    {
        foreach (bool saved in new[] { false, true })
        {
            var set = new PySet([1L]); int calls = 0;
            var failure = new PyValueError("用户展开失败");
            var outer = new PyValueError("原上下文"); object? previous = PyErrors.EnterHandler(outer);
            long depth = PyTraceback.PushFrame("set_star.py", "user_iterator");
            try
            {
                PyTraceback.SetLine(17); PyErrors.PrepareRaise(failure);
                var frames = failure.Frames; var context = failure.Context; var cause = failure.Cause;
                bool suppress = failure.SuppressContextFlag;
                var cls = new PyClass("BrokenClearStar");
                cls.SetAttr("__iter__", PyNativeFunctions.CreateVariadic("__iter__", _ =>
                {
                    calls++; set.Add(2L); throw failure;
                }));
                object star = PyOps.StarArg(new PyInstance(cls));
                object? method = PyObject.GetAttr(set, "clear");
                PyTraceback.SetLine(26);
                var caught = Assert.Throws<PyValueError>(() =>
                {
                    if (saved) PyOps.CallValue(method, [star], ["bad"], [0L]);
                    else PyOps.CallMethod(set, "clear", [star], ["bad"], [0L]);
                });
                Assert.Same(failure, caught); Assert.Same(frames, caught.Frames);
                Assert.Same(context, caught.Context); Assert.Same(cause, caught.Cause);
                Assert.Equal(suppress, caught.SuppressContextFlag); Assert.Equal(1, calls);
                Assert.Equal(2, set.Count); Assert.True(set.Contains(1L)); Assert.True(set.Contains(2L));
            }
            finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        }
    }

    [Fact]
    public void BinderErrorsHaveCurrentFramesAndContextWithoutReplacingAmbientException()
    {
        var outer = new PyValueError("正在处理"); object? previous = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("set_clear.py", "caller");
        try
        {
            PyTraceback.SetLine(31);
            foreach (string route in Routes)
            {
                var set = new PySet([1L]);
                var first = BindingError(() => Call(route, set, [0L], [], []), "set.clear() takes no arguments (1 given)");
                var second = BindingError(() => Call(route, set, [0L], ["bad"], [0L]), "set.clear() takes no keyword arguments");
                Assert.NotSame(first, second);
                foreach (var error in new[] { first, second })
                {
                    Assert.Same(outer, error.Context); Assert.Null(error.Cause); Assert.False(error.SuppressContextFlag);
                    Assert.Contains(new PyStackFrame("set_clear.py", "caller", 31), error.Frames);
                }
                Assert.Same(PyNone.Instance, Call(route, set, [], [], []));
                Assert.Same(outer, PyErrors.Current); Assert.Equal(depth, PyTraceback.CaptureDepth());
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }
}
