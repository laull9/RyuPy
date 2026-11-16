using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>四方法绑定失败的背板纯性、成功身份、原方法体错误与新错误的帧及上下文。</summary>
[Collection("生成器帧栈")]
public class PyListMethodBindingStateTests
{
    [Fact]
    public void InvalidBindingsDoNotMaterializeOrReplaceNumericBacking()
    {
        foreach (string route in new[] { "direct", "prepare", "getattr", "invoke", "false", "true" })
        foreach (string name in ListBindingChecks.Names)
        {
            var list = Assert.IsType<PyList>(PyNumericListOps.Create());
            PyNumericListOps.AppendF64(list, 1.25);
            PyNumericListOps.AppendF64(list, double.NaN);
            var before = Storage(list);
            Assert.Null(before.Items);
            Assert.NotNull(before.Numeric);
            foreach (bool keywords in new[] { false, true })
            {
                // 首参数刻意非法，必须先拒绝绑定，不能进入索引转换或 Items。
                ListBindingChecks.Error(() => ListBindingChecks.Call(route, list, name, ["bad", 0L],
                    keywords ? ["index"] : [], keywords ? [0L] : []), keywords
                        ? $"list.{name}() takes no keyword arguments" : ListBindingChecks.Arity(name, 2));
                var after = Storage(list);
                Assert.Same(before.Items, after.Items);
                Assert.Same(before.Numeric, after.Numeric);
                Assert.Equal(2, list.Count);
                Assert.Equal(1.25, PyNumericListOps.ReadF64(list, 0L));
                Assert.True(double.IsNaN(PyNumericListOps.ReadF64(list, 1L)));
            }
        }
    }

    [Fact]
    public void ClearReverseAndCopyPreserveReturnAliasAndShallowElementContracts()
    {
        foreach (string route in new[] { "direct", "prepare", "getattr", "invoke" })
        {
            object?[] before = ListBindingChecks.Markers();
            var list = new PyList(before);
            PyList alias = list;
            Assert.Same(PyNone.Instance, ListBindingChecks.Call(route, list, "reverse", [], [], []));
            ListBindingChecks.Unchanged(alias, before.Reverse().ToArray());
            Assert.Same(PyNone.Instance, ListBindingChecks.Call(route, list, "clear", [], [], []));
            Assert.Equal(0, alias.Count);

            var mutable = new PyList();
            object nan = double.NaN;
            object box = 9876.5;
            object?[] shared = [mutable, nan, box, box];
            var original = new PyList(shared);
            var copy = Assert.IsType<PyList>(ListBindingChecks.Call(route, original, "copy", [], [], []));
            Assert.NotSame(original, copy);
            ListBindingChecks.Unchanged(original, shared);
            ListBindingChecks.Unchanged(copy, shared);
            mutable.Append("共享修改");
            Assert.Same(mutable, copy.GetItem(0));
            Assert.Equal("共享修改", Assert.IsType<PyList>(copy.GetItem(0)).GetItem(0));
            copy.SetItem(0L, PyNone.Instance);
            copy.Append("仅副本结构");
            ListBindingChecks.Unchanged(original, shared);
            Assert.Equal(5, copy.Count);
            Assert.Same(nan, copy.GetItem(1));
            Assert.Same(box, copy.GetItem(2));
            Assert.Same(box, copy.GetItem(3));
        }
    }

    [Fact]
    public void PopKeepsDefaultZeroNegativeAndBooleanIndexIdentity()
    {
        (object?[] Args, int Removed)[] cases =
        [
            ([], 2), ([0L], 0), ([-1L], 2), ([false], 0), ([true], 1),
        ];
        foreach (string route in new[] { "direct", "prepare", "getattr", "invoke" })
        foreach (var item in cases)
        {
            object?[] before = ListBindingChecks.Markers();
            var list = new PyList(before);
            PyList alias = list;
            object? result = ListBindingChecks.Call(route, list, "pop", item.Args, [], []);
            Assert.Same(before[item.Removed], result);
            ListBindingChecks.Unchanged(alias, before.Where((_, i) => i != item.Removed).ToArray());
        }
    }

    [Fact]
    public void PopBodyErrorsPrepareTheirOwnCallerContext()
    {
        var outer = new PyValueError("当前处理异常");
        object? previous = PyErrors.EnterHandler(outer);
        long frame = PyTraceback.PushFrame("list_body.py", "caller");
        try
        {
            PyTraceback.SetLine(29);
            foreach (string route in new[] { "direct", "prepare", "getattr", "invoke" })
            foreach (bool empty in new[] { false, true })
            {
                var list = new PyList(empty ? [] : ListBindingChecks.Markers());
                object?[] args = empty ? [] : [99L];
                // 与现有容器原语比较，而非把历史 traceback 缺口硬编码成新规范。
                var body = Assert.Throws<PyIndexError>(() => list.Pop(empty ? -1 : 99));
                var called = Assert.Throws<PyIndexError>(() => ListBindingChecks.Call(route,
                    list, "pop", args, [], []));
                Assert.Equal(empty ? "pop from empty list" : "pop index out of range", called.PyMessage);
                Assert.Equal(body.Args, called.Args);
                Assert.Same(body.Context, called.Context);
                Assert.Equal(body.Frames, called.Frames);
                Assert.Equal(empty ? 0 : 3, list.Count);
                Assert.Same(outer, PyErrors.Current);
                Assert.Equal(frame, PyTraceback.CaptureDepth());
            }
            foreach (string route in new[] { "direct", "prepare", "getattr", "invoke" })
            {
                object?[] before = ListBindingChecks.Markers();
                var list = new PyList(before);
                ListBindingChecks.Error(() => ListBindingChecks.Call(route, list, "pop", ["bad"], [], []),
                    "'str' object cannot be interpreted as an integer");
                ListBindingChecks.Unchanged(list, before);
            }
        }
        finally
        {
            PyTraceback.PopTo(frame);
            PyErrors.LeaveHandler(previous);
        }
    }

    [Fact]
    public void NewBinderErrorsCaptureAmbientContextAndCurrentFrameWithoutChangingState()
    {
        var outer = new PyValueError("正在处理");
        object? previous = PyErrors.EnterHandler(outer);
        long frame = PyTraceback.PushFrame("list_binding.py", "caller");
        try
        {
            PyTraceback.SetLine(47);
            var snapshot = new PyValueError("仅作帧快照");
            PyTraceback.Attach(snapshot);
            foreach (string name in ListBindingChecks.Names)
            foreach (string route in new[] { "direct", "prepare", "getattr", "invoke", "false", "true" })
            foreach (bool keywords in new[] { false, true })
            {
                var list = new PyList(ListBindingChecks.Markers());
                var error = ListBindingChecks.Error(() => ListBindingChecks.Call(route, list, name,
                    ["bad", 0L], keywords ? ["index"] : [], keywords ? [0L] : []), keywords
                        ? $"list.{name}() takes no keyword arguments" : ListBindingChecks.Arity(name, 2));
                Assert.Same(outer, error.Context);
                Assert.Null(error.Cause);
                Assert.False(error.SuppressContextFlag);
                Assert.Equal(snapshot.Frames, error.Frames);
                Assert.Equal(new PyStackFrame("list_binding.py", "caller", 47), error.Frames[0]);
                Assert.Same(outer, PyErrors.Current);
                Assert.Equal(frame, PyTraceback.CaptureDepth());
                // 成功操作也不能替换正在处理的异常或增减 Python 帧。
                ListBindingChecks.Call(route, list, "copy", [], [], []);
                Assert.Same(outer, PyErrors.Current);
                Assert.Equal(frame, PyTraceback.CaptureDepth());
            }
        }
        finally
        {
            PyTraceback.PopTo(frame);
            PyErrors.LeaveHandler(previous);
        }
    }

    [Fact]
    public void NewErrorsWithoutAmbientHandlerAreDistinctAndUnchained()
    {
        object? previous = PyErrors.EnterHandler(null);
        PyErrors.LeaveHandler(null);
        try
        {
            foreach (string name in ListBindingChecks.Names)
            foreach (string route in new[] { "direct", "prepare", "getattr", "invoke" })
            {
                var list = new PyList(ListBindingChecks.Markers());
                string message = ListBindingChecks.Arity(name, 2);
                var first = ListBindingChecks.Error(() => ListBindingChecks.Call(route, list, name,
                    [0L, 0L], [], []), message);
                var second = ListBindingChecks.Error(() => ListBindingChecks.Call(route, list, name,
                    [0L, 0L], [], []), message);
                Assert.NotSame(first, second);
                foreach (var error in new[] { first, second })
                {
                    Assert.Null(error.Context);
                    Assert.Null(error.Cause);
                    Assert.False(error.SuppressContextFlag);
                }
                Assert.Null(PyErrors.Current);
            }
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void UserAndDictionaryNamesSortAndAppendRemainOutsideTheNewBinder()
    {
        var marker = new PyList();
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("ListBindingGuard", [], null));
        foreach (string name in ListBindingChecks.Names)
            cls.SetAttr(name, PyNativeFunctions.CreateVariadic(name, _ => marker));
        object instance = cls.Invoke([], [], [])!;
        foreach (string name in ListBindingChecks.Names)
        {
            Assert.Same(marker, PyOps.CallMethod(instance, name, [1L, 2L, 3L], [], []));
            Assert.Same(marker, PyOps.CallValue(PyObject.GetAttr(instance, name), [1L, 2L, 3L], [], []));
        }
        foreach (bool saved in new[] { false, true })
        {
            var dictionary = new PyDict();
            dictionary.Set("key", marker);
            object? Call(object receiver, string name, object?[] args, object?[] names, object?[] values) => saved
                ? PyOps.CallValue(PyOps.PrepareMethod(receiver, name), args, names, values)
                : PyOps.CallMethod(receiver, name, args, names, values);
            var copied = Assert.IsType<PyDict>(Call(dictionary, "copy", [], [], []));
            Assert.NotSame(dictionary, copied);
            Assert.Same(marker, Call(copied, "pop", ["key"], [], []));
            Assert.Same(PyNone.Instance, Call(dictionary, "clear", [], [], []));
            Assert.Equal(0, dictionary.Count);
            var list = new PyList([1L, 3L, 2L]);
            Assert.Same(PyNone.Instance, Call(list, "sort", [], ["reverse"], [true]));
            Assert.Equal(new object?[] { 3L, 2L, 1L }, list.Items);
            Assert.Same(PyNone.Instance, Call(list, "append", [marker], [], []));
            Assert.Same(marker, list.GetItem(3));
        }
    }

    // 反射只用于测试读取原背板；禁止用 Items、Repr 或枚举先替被测代码物化列表。
    private static (object? Items, object? Numeric) Storage(PyList list) => (
        typeof(PyList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list),
        typeof(PyList).GetField("_numeric", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list));
}
