// G1真实Generic根公开入口合同：CP原源已先冻结，不依赖新生产类的内部表示。
// 每项安装真实注册模块帧；这里仅写测试，统一构建与差分由父任务执行。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingGenericRootTests : IDisposable
{
    private readonly long depth;
    public PyTypingGenericRootTests()
    {
        PyNativeModules.Register();
        string key = "generic_root_" + Guid.NewGuid().ToString("N");
        PyModules.Register(key, "", "generic_root.py", () => { }, _ => "generic_contract", (_, _, _) => { });
        depth = PyTraceback.CaptureDepth();
        PyTraceback.PushFrameWithOrigin("generic_root.py", "<module>", PyTraceback.CreateModuleExecutionSource(key));
    }
    public void Dispose() => PyTraceback.PopTo(depth + 1);
    internal static Dictionary<string, object?> Typing => PyTyping.CreateAttributes();
    internal static object? Form(string name) => Typing[name];
    internal static object? Get(object? value, string name) => PyObject.GetAttr(value, name);
    internal static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    internal static object? Builtin(string name, params object?[] args) => PyOps.CallBuiltin(name, args, [], []);
    internal static object? Variable(string name) => Call(Form("TypeVar"), name);
    internal static object? Subscribe(object? key) => PyOps.GetItem(Form("Generic"), key);

    [Fact]
    public void CanonicalRootIsARealClassWithObjectMroAndNoInventedSlots()
    {
        object? root = Form("Generic");
        Assert.IsType<PyClass>(root);
        Assert.Same(PyType.Get("type"), PyObject.TypeOf(root));
        Assert.Same(root, Form("Generic"));
        Assert.Equal("typing", Get(root, "__module__"));
        Assert.Equal("Generic", Get(root, "__name__"));
        Assert.Equal("Generic", Get(root, "__qualname__"));
        Assert.Same(PyType.Get("object"), Get(root, "__base__"));
        Assert.Equal(new object?[] { PyType.Get("object") }, Assert.IsType<PyTuple>(Get(root, "__bases__")).Items);
        Assert.Equal(new object?[] { root, PyType.Get("object") }, Assert.IsType<PyTuple>(Get(root, "__mro__")).Items);
        Assert.Same(Get(root, "__bases__"), Get(root, "__bases__"));
        Assert.Same(Get(root, "__mro__"), Get(root, "__mro__"));
        Assert.Equal("[<class 'typing.Generic'>, <class 'object'>]", PyRepr.Repr(Call(Get(root, "mro"))));
        Assert.Same(root, Call(Form("get_origin"), root));
        Assert.Empty(Assert.IsType<PyTuple>(Call(Form("get_args"), root)).Items);
        foreach (string name in new[] { "__slots__", "__parameters__", "__orig_bases__" })
            Assert.Throws<PyAttributeError>(() => Get(root, name));
        var sameName = new PyClass("Generic");
        Assert.Equal(false, Builtin("isinstance", new PyInstance(sameName), root));
    }

    [Fact]
    public void RootAndAliasCallsProduceRealRootInstancesWithoutWritableFields()
    {
        object? root = Form("Generic"), alias = Subscribe(Variable("T"));
        object? objectNew = Get(PyType.Get("object"), "__new__");
        foreach (object? value in new[] { Call(root), Call(alias), Call(objectNew, root) })
        {
            Assert.IsAssignableFrom<PyInstance>(value);
            Assert.Same(root, PyObject.TypeOf(value));
            Assert.Same(root, Get(value, "__class__"));
            Assert.Equal(true, Builtin("isinstance", value, root));
            foreach (string name in new[] { "__dict__", "__orig_class__", "__weakref__" })
                Assert.Throws<PyAttributeError>(() => Get(value, name));
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, "x", 1L));
            Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(value, "x"));
            Assert.Throws<PyTypeError>(() => PyObject.SetAttr(value, "__class__", PyType.Get("object")));
        }
    }

    [Fact]
    public void AliasPreservesRawSubscriptionShapeAndTypeVariableIdentity()
    {
        object? t = Variable("T"), u = Variable("U"), other = Variable("T");
        object? first = Subscribe(t), tuple = Subscribe(new PyTuple([t]));
        object? pair = Subscribe(new PyTuple([t, u]));
        Assert.Same(first, Subscribe(t));
        Assert.Same(tuple, Subscribe(new PyTuple([t])));
        Assert.NotSame(first, tuple);
        Assert.True(PyHash.Equal(first, tuple));
        Assert.Equal(PyHash.Of(first), PyHash.Of(tuple));
        Assert.False(PyHash.Equal(first, Subscribe(other)));
        Assert.False(PyHash.Equal(first, pair));
        Assert.Same(PyObject.TypeOf(PyOps.GetItem(Form("List"), t)), PyObject.TypeOf(first));
        Assert.Equal(false, Builtin("isinstance", first, PyType.Get("type")));
        Assert.Same(Form("Generic"), Get(first, "__origin__"));
        Assert.Equal(new[] { t, u }, Assert.IsType<PyTuple>(Get(pair, "__args__")).Items);
        Assert.Equal(new[] { t, u }, Assert.IsType<PyTuple>(Get(pair, "__parameters__")).Items);
        Assert.NotSame(Get(first, "__args__"), Get(first, "__parameters__"));
        Assert.Same(Get(first, "__args__"), Get(first, "__args__"));
        Assert.Same(Get(first, "__parameters__"), Get(first, "__parameters__"));
        Assert.Same(Get(first, "__args__"), Call(Form("get_args"), first));
        Assert.Same(Form("Generic"), Call(Form("get_origin"), first));
        Assert.Same(t, Assert.IsType<PyTuple>(Call(Form("get_args"), first)).Items[0]);
        Assert.Equal("typing", Get(first, "__module__"));
        Assert.Equal("Generic", Get(first, "__name__"));
        Assert.Equal("Generic", Get(first, "__qualname__"));
        Assert.Equal("typing.Generic[~T]", PyRepr.Repr(first));
        Assert.Equal(true, Builtin("callable", first));
    }

    [Fact]
    public void LimitedMroEntriesAcceptsEqualNonidenticalAliasAndKeywordBinder()
    {
        object? t = Variable("T"), first = Subscribe(t), tuple = Subscribe(new PyTuple([t]));
        object? method = Get(first, "__mro_entries__");
        foreach (object? candidate in new[] { first, tuple })
            Assert.Same(Form("Generic"), Assert.IsType<PyTuple>(Call(method, new PyTuple([candidate]))).Items[0]);
        Assert.Same(Form("Generic"), Assert.IsType<PyTuple>(
            PyOps.CallValue(method, [], ["bases"], [new PyTuple([first])])).Items[0]);
        Assert.Throws<PyTypeError>(() => Call(method));
        Assert.Throws<PyTypeError>(() => Call(method, new PyTuple([first]), new PyTuple([first])));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(method, [], ["wrong"], [new PyTuple([first])]));
    }

    [Fact]
    public void KnownInvalidSubscriptionAndConstructionUseCpythonErrors()
    {
        object? t = Variable("T"), alias = Subscribe(t);
        Assert.Equal("Parameter list to Generic[...] cannot be empty",
            Assert.Throws<PyTypeError>(() => Subscribe(new PyTuple([]))).PyMessage);
        foreach (object? value in new object?[] { PyType.Get("int"), PyNone.Instance, 3L, "T", new PyList([t]), new PyTuple([new PyTuple([t])]) })
            Assert.Equal("Parameters to Generic[...] must all be type variables or parameter specification variables.",
                Assert.Throws<PyTypeError>(() => Subscribe(value)).PyMessage);
        Assert.Equal("Parameters to Generic[...] must all be unique",
            Assert.Throws<PyTypeError>(() => Subscribe(new PyTuple([t, t]))).PyMessage);
        foreach (object? target in new[] { Form("Generic"), alias })
        {
            Assert.Equal("typing.Generic() takes no arguments", Assert.Throws<PyTypeError>(() => Call(target, 1L)).PyMessage);
            Assert.Equal("typing.Generic() takes no arguments", Assert.Throws<PyTypeError>(
                () => PyOps.CallValue(target, [], ["x"], [1L])).PyMessage);
        }
        Assert.Equal("Cannot subscript already-subscripted typing.Generic[~T]",
            Assert.Throws<PyTypeError>(() => PyOps.GetItem(alias, PyType.Get("int"))).PyMessage);
        foreach (string builtin in new[] { "isinstance", "issubclass" })
            Assert.Equal("Subscripted generics cannot be used with class and instance checks",
                Assert.Throws<PyTypeError>(() => Builtin(builtin, 1L, alias)).PyMessage);
    }

    [Fact]
    public void IoUnknownGenericAncestryIsExplicitButTupleChecksShortCircuit()
    {
        object? root = Form("Generic"), alias = Subscribe(Variable("T"));
        foreach (string name in new[] { "IO", "BinaryIO", "TextIO" })
        {
            object? cls = Form(name), value = Call(cls);
            Assert.Throws<PyNotImplementedError>(() => Builtin("issubclass", cls, root));
            Assert.Throws<PyNotImplementedError>(() => Builtin("isinstance", value, root));
            Assert.Equal(true, Builtin("issubclass", cls, new PyTuple([cls, root])));
            Assert.Equal(true, Builtin("isinstance", value, new PyTuple([cls, root])));
            Assert.Equal(true, Builtin("issubclass", cls, new PyTuple([cls, alias])));
            Assert.Equal(true, Builtin("isinstance", value, new PyTuple([cls, alias])));
        }
    }
}
