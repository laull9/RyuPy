// 冷伴生分派以真实函数为键；普通Invoke、签名共享、活默认值与包装转发分别受保护。
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyGenericFunctionCoreTests
{
    private static PySignature Signature() => PySignature.Create("value", ["value"], ["pos"], ["0"]);
    [Fact]
    public void GenericRegistrationUsesActualFunctionAndKeepsOrdinaryBody()
    {
        var signature = Signature();
        PyFunction first = new("value", signature, (_, _, _, _) => "typed", [null]);
        PyFunction second = new("value", signature, (_, _, _, _) => "different", [null]);
        object value = new PyList();
        PyOps.RegisterGenericFunction(first, (owner, args, names, values) =>
        {
            Assert.Same(first, owner);
            return PyBinder.Bind(owner.Signature, args, names, values, owner.Defaults)[0];
        });
        Assert.Equal("typed", first.Invoke([7L], [], []));
        Assert.Equal("different", PyOps.CallValueGeneric(second, [], [], []));
        var defaults = new PyTuple([value]);
        PyObject.SetAttr(first, "__defaults__", defaults);
        Assert.Same(defaults, PyObject.GetAttr(first, "__defaults__"));
        Assert.Same(value, PyOps.CallValueGeneric(first, [], [], []));
        Assert.Same(value, PyOps.CallValueGeneric(first, [value], [], []));
        PyObject.SetAttr(first, "__defaults__", new PyTuple(["updated"]));
        Assert.Equal("updated", PyOps.CallValueGeneric(first, [], [], []));
        PyObject.DelAttr(first, "__defaults__");
        Assert.Equal("value() missing 1 required positional argument: 'value'",
            Assert.Throws<PyTypeError>(() => PyOps.CallValueGeneric(first, [], [], [])).Message);
    }
    [Fact]
    public void PartialAndStaticWrapperForwardTheSameObjectThroughGenericDomain()
    {
        PyFunction target = new("value", Signature(), (_, _, _, _) => "typed", [null]);
        PyOps.RegisterGenericFunction(target, (owner, args, names, values) =>
            PyBinder.Bind(owner.Signature, args, names, values, owner.Defaults)[0]);
        object value = new PyDict();
        var partial = new PyPartial(new PyStaticMethod(target), [value], [], []);
        Assert.Same(value, PyOps.CallValueGeneric(partial, [], [], []));
        Assert.Equal("typed", partial.Invoke([], [], []));
    }
    [Fact]
    public void BoundFallbackKeepsMethodContextAndRegisteredTargetKeepsActualOwner()
    {
        var owner = new PyClass("Owner"); object receiver = new PyList();
        var previousOwner = PyMethodContext.CurrentOwner;
        var previousSelf = PyMethodContext.CurrentSelf;
        PyFunction ordinary = new("value", Signature(), (_, args, _, _) =>
        {
            Assert.Same(owner, PyMethodContext.CurrentOwner);
            Assert.Same(receiver, PyMethodContext.CurrentSelf);
            return args[0];
        }, [null], usesSuper: true);
        Assert.Same(receiver, PyOps.CallValueGeneric(new PyBoundMethod(owner, receiver, ordinary), [], [], []));
        Assert.Same(previousOwner, PyMethodContext.CurrentOwner);
        Assert.Same(previousSelf, PyMethodContext.CurrentSelf);
        PyFunction registered = new("value", Signature(), (_, _, _, _) => "typed", [null], usesSuper: false);
        PyOps.RegisterGenericFunction(registered, (actual, args, names, values) =>
        {
            Assert.Same(registered, actual);
            return PyBinder.Bind(actual.Signature, args, names, values, actual.Defaults)[0];
        });
        Assert.Same(receiver, PyOps.CallValueGeneric(new PyBoundMethod(owner, receiver, registered), [], [], []));
    }

}
