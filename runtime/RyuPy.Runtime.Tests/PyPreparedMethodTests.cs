using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyPreparedMethodTests
{
    private static PyFunction Fn(string name, Func<object?[], object?> body) =>
        new(name, PySignature.Create(name, ["self"], ["pos"], ["0"]),
            (_, args, _, _) => body(args), []);

    [Fact]
    public void BindingCapturesOldMethodAndPropertyRunsImmediately()
    {
        var cls = (PyClass)PyOps.MakeClass("Receiver", [], null);
        cls.SetAttr("method", Fn("method", _ => "old"));
        object instance = cls.Invoke([], [], [])!;
        object? method = PyOps.PrepareMethod(instance, "method");
        cls.SetAttr("method", Fn("method", _ => "new"));
        Assert.Equal("old", PyOps.CallValue(method, [], [], []));
        Assert.Equal("new", PyOps.CallMethod(instance, "method", [], [], []));
        int reads = 0;
        cls.SetAttr("value", new PyProperty(Fn("value", _ => { reads++; return 42L; })));
        object? value = PyOps.PrepareMethod(instance, "value");
        Assert.Equal(1, reads);
        Assert.Equal(42L, value);
        // 属性读取不检查可调用性；只有参数求值结束后的 CallValue 才报错。
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(value, [], [], []));
        Assert.Equal(1, reads);
        Assert.Throws<PyAttributeError>(() => PyOps.PrepareMethod(instance, "missing"));
    }

    [Fact]
    public void BuiltinBindingKeepsReceiverAndDefersArgumentValidation()
    {
        var first = new PyList();
        var second = new PyList();
        object? append = PyOps.PrepareMethod(first, "append");
        Assert.Equal(0, first.Count);
        PyOps.CallValue(append, [42L], [], []);
        Assert.Equal(1, first.Count);
        Assert.Equal(0, second.Count);
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(append, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(append, [], ["value"], [1L]));
        Assert.Throws<PyAttributeError>(() => PyOps.PrepareMethod(first, "missing"));
        var values = new PyList([1L, 3L, 2L]);
        object? sort = PyOps.PrepareMethod(values, "sort");
        PyOps.CallValue(sort, [], ["reverse"], [true]);
        Assert.Equal("[3, 2, 1]", PyRepr.Repr(values));
    }

    [Fact]
    public void GeneratorSendCanBePreparedWithoutResuming()
    {
        int calls = 0;
        var generator = new PyGenerator(raw =>
        {
            calls++;
            ((object?[])raw)[0] = 1L;
            return 7L;
        }, new object?[] { 0L, null, null, null }, "prepare.py", "values");
        object? send = PyOps.PrepareMethod(generator, "send");
        Assert.Equal(0, calls);
        Assert.Equal(7L, PyOps.CallValue(send, [PyNone.Instance], [], []));
        Assert.Equal(1, calls);
        object? close = PyOps.PrepareMethod(generator, "close");
        // 此手写恢复入口没有处理注入异常，会再次 yield，close 必须拒绝并保留暂停。
        Assert.Throws<PyRuntimeError>(() => PyOps.CallValue(close, [], [], []));
    }

    [Fact]
    public void ArityErrorsCannotModifyReceiversOrEscapeAsClrErrors()
    {
        var list = new PyList([1L, 2L]);
        foreach (string name in new[] { "clear", "reverse", "copy", "sort" })
        {
            object? method = PyOps.PrepareMethod(list, name);
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(method, [true], [], []));
            Assert.Equal("[1, 2]", PyRepr.Repr(list));
        }
        object? pop = PyOps.PrepareMethod(list, "pop");
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(pop, [0L, 1L], [], []));
        Assert.Equal("[1, 2]", PyRepr.Repr(list));
        object? upper = PyOps.PrepareMethod("text", "upper");
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(upper, [1L], [], []));
        object? hex = PyOps.PrepareMethod(new PyBytes([1]), "hex");
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(hex, [1L], [], []));
        object? get = PyOps.PrepareMethod(new PyDict(), "get");
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(get, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(get, [1L, 2L, 3L], [], []));
    }

    [Fact]
    public void SupportedBuiltinNamesBindWithoutExecutingAndMissingNamesFail()
    {
        // 与各类型 XxxMethod 分派的支持集同步；新增方法时必须扩充本表与准备入口。
        (object receiver, string names)[] cases =
        [
            (new PyList(), "append extend insert pop remove index count clear reverse copy sort"),
            (new PyTuple([]), "count index"),
            (new PyDict(), "get keys values items update pop setdefault clear copy"),
            (new PySet(), "add remove discard union intersection difference"),
            (new PyByteArray([]), "append extend decode"),
            (new PyBytes([]), "decode hex"),
            ("", "encode upper lower strip lstrip rstrip split join replace find rfind index rindex count startswith endswith"),
            (new PyGenerator(_ => throw new Exception("不得执行"), new object?[] { 0L, null, null, null },
                "prepare.py", "never"), "__iter__ __next__ send throw close"),
        ];
        foreach (var (receiver, names) in cases)
        {
            foreach (string name in names.Split(' '))
            {
                var method = Assert.IsType<PyBuiltinMethod>(PyOps.PrepareMethod(receiver, name));
                // 每个已绑定入口都必须在执行副作用之前拒绝过量实参。
                Assert.Throws<PyTypeError>(() => method.Invoke(new object?[10], [], []));
            }
            Assert.Throws<PyAttributeError>(() => PyOps.PrepareMethod(receiver, "missing"));
        }
    }
}
