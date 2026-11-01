using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>有限 typing 元数据的身份、参数快照和错误边界；不把别名当运行时实参验证器。</summary>
public class PyTypingTests
{
    public PyTypingTests() => PyNativeModules.Register();
    private static object? Form(string name) => PyTyping.CreateAttributes()[name];
    private static object? Alias(string name, object? args) => PyOps.GetItem(Form(name), args);
    private static object? Call(string name, params object?[] args) => PyOps.CallValue(Form(name), args, [], []);

    [Fact]
    public void AliasesCacheIdentityAndUnionUsesOrderIndependentEquality()
    {
        object? integer = PyType.Get("int"), text = PyType.Get("str");
        Assert.Same(Alias("List", integer), Alias("List", integer));
        object? left = Alias("Union", new PyTuple([integer, text]));
        object? right = Alias("Union", new PyTuple([text, integer]));
        Assert.True(PyHash.Equal(left, right));
        Assert.Equal(PyHash.Of(left), PyHash.Of(right));
        Assert.Same(PyType.Get("list"), Call("get_origin", Alias("List", integer)));
        Assert.Same(PyNone.Instance, Call("get_origin", Form("Union")));
        var value = new PyList([]);
        Assert.Same(value, Call("cast", integer, value));
    }

    [Fact]
    public void CallableArgumentsAreFreshPublicListsAndForwardRefsAreExplicit()
    {
        object? alias = Alias("Callable", new PyTuple([new PyList([PyType.Get("int")]), PyType.Get("str")]));
        var first = Assert.IsType<PyTuple>(Call("get_args", alias));
        var second = Assert.IsType<PyTuple>(Call("get_args", alias));
        Assert.IsType<PyList>(first.Items[0]);
        Assert.NotSame(first.Items[0], second.Items[0]);
        Assert.Throws<PyNotImplementedError>(() => Alias("List", "Forward"));
        Assert.Throws<PyTypeError>(() => Alias("Dict", PyType.Get("int")));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(Alias("List", PyType.Get("int")), [], [], []));
    }
}
