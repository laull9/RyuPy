using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>四个列表方法的公开调用入口、精确绑定消息、捕获目标与实参展开合同。</summary>
[Collection("生成器帧栈")]
public class PyListMethodBindingEntryTests
{
    [Fact]
    public void DirectArityErrorsPreserveReceiversAndPrecedeBodyErrors()
    {
        foreach (string name in ListBindingChecks.Names)
        foreach (int count in name == "pop" ? new[] { 2, 3 } : new[] { 1, 2 })
        foreach (bool empty in new[] { false, true })
        {
            object?[] before = empty ? [] : ListBindingChecks.Markers();
            var list = new PyList(before);
            object?[] args = Enumerable.Repeat<object?>("bad", count).ToArray();
            ListBindingChecks.Error(() => PyOps.CallMethod(list, name, args, [], []),
                ListBindingChecks.Arity(name, count));
            ListBindingChecks.Unchanged(list, before);
        }
    }

    [Fact]
    public void KeywordsWinOverArityAndIndexErrorsAcrossDirectAndBoundEntrypoints()
    {
        foreach (string name in ListBindingChecks.Names)
        foreach (string route in new[] { "direct", "prepare", "getattr", "invoke" })
        foreach (int count in new[] { 0, 1, 2 })
        {
            object?[] before = ListBindingChecks.Markers();
            var list = new PyList(before);
            object?[] args = Enumerable.Repeat<object?>("bad", count).ToArray();
            ListBindingChecks.Error(() => ListBindingChecks.Call(route, list, name, args, ["index"], [0L]),
                $"list.{name}() takes no keyword arguments");
            ListBindingChecks.Unchanged(list, before);
        }
    }

    [Fact]
    public void MethodPicFixedAndArraySitesAlternateSuccessAndFailure()
    {
        // 同一站点交错方法和参数个数；不修改缓存或依赖站点内部布局。
        foreach (int site in new[] { 3950, -1 })
        for (int repeat = 0; repeat < 2; repeat++)
        foreach (string name in ListBindingChecks.Names)
        foreach (int count in new[] { 0, 2, 1, 3, 4, 0 })
        {
            object?[] before = ListBindingChecks.Markers();
            var list = new PyList(before);
            object?[] args = Enumerable.Repeat<object?>(0L, count).ToArray();
            Func<object?> call = () => count switch
            {
                0 => PyPic.CallMethod0(site, list, name),
                1 => PyPic.CallMethod1(site, list, name, args[0]),
                2 => PyPic.CallMethod2(site, list, name, args[0], args[1]),
                3 => PyPic.CallMethod3(site, list, name, args[0], args[1], args[2]),
                _ => PyPic.CallMethod(site, list, name, args, [], []),
            };
            ListBindingChecks.CheckCall(list, before, name, count, call);
        }
    }

    [Fact]
    public void PreparedAndGetAttrMethodsCaptureOldReceiverWithoutExecuting()
    {
        foreach (string name in ListBindingChecks.Names)
        foreach (bool getattr in new[] { false, true })
        {
            object?[] before = ListBindingChecks.Markers();
            var original = new PyList(before);
            var replacement = new PyList(ListBindingChecks.Markers());
            object? receiver = original;
            var method = Assert.IsType<PyBuiltinMethod>(getattr
                ? PyObject.GetAttr(receiver, name) : PyOps.PrepareMethod(receiver, name));
            ListBindingChecks.Unchanged(original, before);
            receiver = replacement;
            int count = name == "pop" ? 2 : 1;
            object?[] args = Enumerable.Repeat<object?>(0L, count).ToArray();
            ListBindingChecks.Error(() => PyOps.CallValue(method, args, [], []),
                ListBindingChecks.Arity(name, count));
            ListBindingChecks.Error(() => method.Invoke(args, [], []),
                ListBindingChecks.Arity(name, count));
            ListBindingChecks.Unchanged(original, before);
            ListBindingChecks.CheckCall(original, before, name, 0,
                () => PyOps.CallValue(method, [], [], []));
            Assert.Same(replacement, receiver);
            Assert.Equal(3, replacement.Count);
        }
    }

    [Fact]
    public void SavedValuePicCoversEveryFixedArityAndArrayFallback()
    {
        foreach (int site in new[] { 3951, -1 })
        for (int repeat = 0; repeat < 2; repeat++)
        foreach (string name in ListBindingChecks.Names)
        foreach (int count in new[] { 0, 2, 1, 3, 4 })
        {
            object?[] before = ListBindingChecks.Markers();
            var list = new PyList(before);
            object? method = PyObject.GetAttr(list, name);
            object?[] args = Enumerable.Repeat<object?>(0L, count).ToArray();
            Func<object?> call = () => count switch
            {
                0 => PyPic.CallValue0(site, method),
                1 => PyPic.CallValue1(site, method, args[0]),
                2 => PyPic.CallValue2(site, method, args[0], args[1]),
                3 => PyPic.CallValue3(site, method, args[0], args[1], args[2]),
                _ => PyPic.CallValue(site, method, args, [], []),
            };
            ListBindingChecks.CheckCall(list, before, name, count, call);
        }
    }

    [Fact]
    public void CapturedFlagsUseActualModuleExportsAndKeepOldBoundReceiver()
    {
        foreach (bool export in new[] { false, true })
        foreach (string name in ListBindingChecks.Names)
        foreach (int count in new[] { 0, 1, 2, 3, 4 })
        {
            object?[] before = ListBindingChecks.Markers();
            var list = new PyList(before);
            object?[] replacementItems = ListBindingChecks.Markers();
            var replacement = new PyList(replacementItems);
            object receiver = export ? ListBindingChecks.Module(PyOps.PrepareMethod(list, name)) : list;
            bool captured = PyOps.IsModule(receiver);
            string attribute = export ? "f" : name;
            object? target = PyOps.CaptureModuleTarget(receiver, attribute);
            Assert.Equal(export, captured);
            if (receiver is PyModule module)
                module.SetAttr("f", PyOps.PrepareMethod(replacement, name));
            else
                Assert.Same(list, target);
            Func<object?> call = () => count switch
            {
                0 => PyPic.CallCapturedMethod0(3952, captured, target, attribute),
                1 => PyPic.CallCapturedMethod1(3952, captured, target, attribute, 0L),
                2 => PyPic.CallCapturedMethod2(3952, captured, target, attribute, 0L, 0L),
                3 => PyPic.CallCapturedMethod3(3952, captured, target, attribute, 0L, 0L, 0L),
                _ => PyOps.CallCapturedMethod(captured, target, attribute, [0L, 0L, 0L, 0L], [], []),
            };
            ListBindingChecks.CheckCall(list, before, name, count, call);
            ListBindingChecks.Unchanged(replacement, replacementItems);
            ListBindingChecks.Error(() => PyOps.CallCapturedMethod(captured, target, attribute,
                ["bad", 0L], ["index"], [0L]), $"list.{name}() takes no keyword arguments");
        }
    }

    [Fact]
    public void StarAndEmptyMappingExpandOnceBeforeBindingAndPreserveExpansionFailure()
    {
        foreach (string route in new[] { "direct", "prepare", "getattr", "false", "true" })
        foreach (string name in ListBindingChecks.Names)
        {
            foreach (int count in new[] { 0, 1, 2, 3 })
            {
                object?[] before = ListBindingChecks.Markers();
                var list = new PyList(before);
                var star = new CountingIterator(Enumerable.Repeat<object?>(0L, count).ToArray());
                Func<object?> call = () => ListBindingChecks.Call(route, list, name,
                    [PyOps.StarArg(star)], [null], [new PyDict()]);
                ListBindingChecks.CheckCall(list, before, name, count, call);
                Assert.Equal(count + 1, star.Moves);
                Assert.Equal(count, star.Reads);
            }
            object?[] untouched = ListBindingChecks.Markers();
            var receiver = new PyList(untouched);
            var keywords = new PyDict();
            keywords.Set("index", 0L);
            var mixed = new CountingIterator([0L, 0L]);
            ListBindingChecks.Error(() => ListBindingChecks.Call(route, receiver, name,
                [PyOps.StarArg(mixed)], [null], [keywords]), $"list.{name}() takes no keyword arguments");
            Assert.Equal(3, mixed.Moves);
            Assert.Equal(2, mixed.Reads);
            var sentinel = new PyValueError("展开失败");
            var failing = new CountingIterator([0L], sentinel);
            Assert.Same(sentinel, Assert.Throws<PyValueError>(() => ListBindingChecks.Call(route,
                receiver, name, [0L, 0L, PyOps.StarArg(failing)], [null], [keywords])));
            Assert.Equal(1, failing.Moves);
            Assert.Equal(0, failing.Reads);
            ListBindingChecks.Unchanged(receiver, untouched);
        }
    }

    // 每次推进及取值均记数；第二次展开已耗尽迭代器也会增加推进数，不能漏检。
    private sealed class CountingIterator(object?[] values, PyBaseException? failure = null) : IPyIterator
    {
        private int position = -1;
        public int Moves { get; private set; }
        public int Reads { get; private set; }
        public bool MoveNext()
        {
            Moves++;
            if (failure is not null) throw failure;
            return ++position < values.Length;
        }
        public object? Current()
        {
            Reads++;
            return values[position];
        }
    }
}

/// <summary>仅测试侧复用消息和身份断言，不依赖新生产入口或私有绑定实现。</summary>
internal static class ListBindingChecks
{
    internal static readonly string[] Names = ["clear", "reverse", "copy", "pop"];
    internal static object?[] Markers() => [new PyList(), new PyList(), new PyList()];
    internal static string Arity(string name, int count) => name == "pop"
        ? $"pop expected at most 1 argument, got {count}"
        : $"list.{name}() takes no arguments ({count} given)";

    internal static PyTypeError Error(Func<object?> call, string message)
    {
        var error = Assert.Throws<PyTypeError>(() => { call(); });
        Assert.Equal(message, error.PyMessage);
        Assert.Equal(message, Assert.IsType<string>(Assert.Single(error.Args)));
        return error;
    }

    internal static void Unchanged(PyList list, object?[] items)
    {
        Assert.Equal(items.Length, list.Count);
        for (int i = 0; i < items.Length; i++) Assert.Same(items[i], list.GetItem(i));
    }

    internal static void CheckCall(PyList list, object?[] before, string name, int count, Func<object?> call)
    {
        if (count > (name == "pop" ? 1 : 0))
        {
            Error(call, Arity(name, count));
            Unchanged(list, before);
            return;
        }
        object? result = call();
        switch (name)
        {
            case "clear":
                Assert.Same(PyNone.Instance, result);
                Assert.Equal(0, list.Count);
                break;
            case "reverse":
                Assert.Same(PyNone.Instance, result);
                Unchanged(list, before.Reverse().ToArray());
                break;
            case "copy":
                var copy = Assert.IsType<PyList>(result);
                Assert.NotSame(list, copy);
                Unchanged(copy, before);
                Unchanged(list, before);
                break;
            case "pop":
                // 入口矩阵中的单参数固定为零；其它合法索引由状态测试单独覆盖。
                int removed = count == 0 ? before.Length - 1 : 0;
                Assert.Same(before[removed], result);
                Unchanged(list, before.Where((_, index) => index != removed).ToArray());
                break;
        }
    }

    internal static PyModule Module(object? value)
    {
        string name = "list_binding_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterNative(name, new Dictionary<string, object?> { ["f"] = value });
        return PyModules.Import(name);
    }

    internal static object? Call(string route, PyList list, string name,
        object?[] args, object?[] names, object?[] values) => route switch
    {
        "direct" => PyOps.CallMethod(list, name, args, names, values),
        "prepare" => PyOps.CallValue(PyOps.PrepareMethod(list, name), args, names, values),
        "getattr" => PyOps.CallValue(PyObject.GetAttr(list, name), args, names, values),
        "invoke" => Assert.IsType<PyBuiltinMethod>(PyOps.PrepareMethod(list, name))
            .Invoke(args, names.Cast<string>().ToArray(), values),
        "false" => PyOps.CallCapturedMethod(PyOps.IsModule(list),
            PyOps.CaptureModuleTarget(list, name), name, args, names, values),
        "true" => CallExport(list, name, args, names, values),
        _ => throw new InvalidOperationException("未知测试入口"),
    };

    private static object? CallExport(PyList list, string name, object?[] args, object?[] names, object?[] values)
    {
        PyModule module = Module(PyOps.PrepareMethod(list, name));
        return PyOps.CallCapturedMethod(PyOps.IsModule(module),
            PyOps.CaptureModuleTarget(module, "f"), "f", args, names, values);
    }
}
