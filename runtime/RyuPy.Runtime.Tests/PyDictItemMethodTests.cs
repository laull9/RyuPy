// 精确dict的保存下标方法/根描述符：同次CP参数差分、任意键、hash异常身份及空表重入。
using System.Diagnostics;
using System.Text.Json;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictItemMethodTests
{
    private static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    private static object Bound(PyDict value) => PyObject.GetAttr(value, "__getitem__")!;
    private static object Root => PyObject.GetAttr(PyType.Get("dict"), "__getitem__")!;

    [Fact]
    public void SavedBoundAndRootMethodsKeepReceiverAndLiveValues()
    {
        var dictionary = new PyDict(); var marker = new PyList();
        dictionary.Set("key", marker);
        object saved = Bound(dictionary);
        Assert.Same(dictionary, PyObject.GetAttr(saved, "__self__"));
        Assert.Equal("dict.__getitem__", PyObject.GetAttr(saved, "__qualname__"));
        Assert.Same(marker, Call(saved, "key"));
        dictionary.Set("key", 9L);
        Assert.Equal(9L, Call(saved, "key"));
        Assert.Equal(9L, Call(Root, dictionary, "key"));
        Assert.Equal(9L, PyOps.CallMethod(dictionary, "__getitem__", ["key"], [], []));
        var tuple = new PyTuple([1L, "tuple"]);
        dictionary.Set(tuple, marker);
        Assert.Same(marker, Call(saved, new PyTuple([1L, "tuple"])));
    }

    [Fact]
    public void BoundAndDescriptorErrorsMatchFreshPinnedCPython()
    {
        var dictionary = new PyDict(); dictionary.Set("x", 1L);
        object bound = Bound(dictionary), root = Root;
        Action[] cases = [
            () => Call(bound), () => Call(bound, "x", "extra"),
            () => PyOps.CallValue(bound, [], ["key"], ["x"]),
            () => Call(root), () => Call(root, 1L), () => Call(root, dictionary),
            () => Call(root, dictionary, "x", "extra"), () => Call(bound, "missing"),
            () => Call(bound, new PyList()), () => Call(Bound(new PyDict()), new PyList()),
        ];
        string[] actual = cases.Select(action => {
            var error = Assert.ThrowsAny<PyBaseException>(action);
            return error.PyTypeName + "|" + error.Message;
        }).ToArray();
        Assert.Equal(ReadOracle(), actual);
    }

    [Fact]
    public void EmptyLookupPreservesHashExceptionIdentity()
    {
        var expected = new PyValueError("hash failed"); int calls = 0;
        var key = Key(_ => { calls++; throw expected; });
        Assert.Same(expected, Assert.Throws<PyValueError>(() => Call(Bound(new PyDict()), key)));
        Assert.Equal(1, calls);
        Assert.Equal(ReadOracle("hash-error")[0], calls + "|True");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyHashReentryUsesCurrentStorageAndSingleOuterHash(bool sameKey)
    {
        var dictionary = new PyDict(); var marker = new PyList(); int calls = 0;
        var key = Key(self => {
            calls++;
            if (calls == 1)
            {
                // 迫使底层表扩容，不能依赖初始空表或旧bucket/entries的快照。
                for (int i = 0; i < 20; i++) dictionary.Set("other" + i, (long)i);
                if (sameKey) dictionary.Set(self, marker);
            }
            return 7L;
        });
        bool identity;
        if (sameKey) identity = ReferenceEquals(marker, Call(Bound(dictionary), key));
        else identity = ReferenceEquals(key, Assert.Throws<PyKeyError>(() => Call(Bound(dictionary), key)).Args[0]);
        Assert.True(identity);
        Assert.Equal(sameKey ? 2 : 1, calls);
        Assert.Equal(19L, dictionary.Get("other19"));
        string actual = (sameKey ? "hit" : "missing") + "|" + calls + "|19|" + identity;
        Assert.Equal(ReadOracle("hash")[sameKey ? 1 : 0], actual);
    }

    private static PyInstance Key(Func<object?, object?> hash)
    {
        var type = (PyClass)PyOps.MakeClass("ItemKey", [], null);
        type.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("__hash__", args => hash(args[0])));
        PyOps.FinalizeOrdinaryClass(type);
        return Assert.IsAssignableFrom<PyInstance>(type.Invoke([], [], []));
    }

    private static string[] ReadOracle(string mode = "errors")
    {
        string python = Environment.GetEnvironmentVariable("PYTHON")
            ?? throw new InvalidOperationException("请指定固定CPython3.12.13的PYTHON绝对路径");
        var start = new ProcessStartInfo(python) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in new[] { "-I", "-B", "-S", "-c", """
import json, sys
assert sys.version_info[:3] == (3, 12, 13)
d = {'x': 1}
b, r = d.__getitem__, dict.__getitem__
cases = [lambda: b(), lambda: b('x', 'extra'), lambda: b(key='x'),
         lambda: r(), lambda: r(1), lambda: r(d), lambda: r(d, 'x', 'extra'),
         lambda: b('missing'), lambda: b([]), lambda: {}.__getitem__([])]
out = []
for case in cases:
    try: case()
    except Exception as error: out.append(type(error).__name__ + '|' + str(error))
def hash_case(same):
    table, marker, calls = {}, [], 0
    class Key:
        def __hash__(self):
            nonlocal calls
            calls += 1
            if calls == 1:
                table.update(('other' + str(i), i) for i in range(20))
                if same: table[self] = marker
            return 7
    key = Key()
    try: value = table.__getitem__(key)
    except KeyError as error: tag, identity = 'missing', error.args[0] is key
    else: tag, identity = 'hit', value is marker
    return f'{tag}|{calls}|{table["other19"]}|{identity}'
def hash_error():
    expected, calls = ValueError('hash failed'), 0
    class Key:
        def __hash__(self):
            nonlocal calls
            calls += 1
            raise expected
    try: {}.__getitem__(Key())
    except ValueError as actual: return f'{calls}|{actual is expected}'
print(json.dumps({'errors': out, 'hash': [hash_case(False), hash_case(True)], 'hash-error': [hash_error()]}[sys.argv[1]]))
""" }) start.ArgumentList.Add(arg);
        start.ArgumentList.Add(mode);
        start.Environment.Remove("PYTHONHOME"); start.Environment.Remove("PYTHONPATH");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("CPython启动失败");
        string output = process.StandardOutput.ReadToEnd(), error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("CPython超时"); }
        Assert.Equal(0, process.ExitCode); Assert.Empty(error);
        return JsonSerializer.Deserialize<string[]>(output)!;
    }
}
