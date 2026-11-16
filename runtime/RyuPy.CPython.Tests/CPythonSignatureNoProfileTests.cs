// 无inspect profile的旧代理合同：普通属性读取不建立反射state，原调用与CodeType边界保留。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    [Fact]
    public void NoProfileSignatureProbeDoesNotObserveDefaultsOrChangeOrdinaryCallback()
    {
        Assert.True(fixture.Available);
        var shape = PySignature.Create("untouched", ["value"], ["pos"], ["1"]);
        int calls = 0;
        var function = new PyFunction("untouched", shape, (self, a, n, v) =>
        { calls++; return PyBinder.Bind(shape, a, n, v, self.Defaults)[0]; }, [2L], false);
        object hasattr = PyModules.Import("builtins").GetAttr("hasattr")!;
        object getattr = PyModules.Import("builtins").GetAttr("getattr")!;
        object setattr = PyModules.Import("builtins").GetAttr("setattr")!;
        Assert.True(PyOps.FunctionDefaultsUnobserved(function, function));
        Assert.Equal(false, PyOps.CallValue(hasattr, [function, "__signature__"], [], []));
        Assert.True(PyOps.FunctionDefaultsUnobserved(function, function));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(getattr, [function, "__code__"], [], []));
        Assert.True(PyOps.FunctionDefaultsUnobserved(function, function));
        object call = PyModules.Import("bridge_fixture").GetAttr("call")!;
        Assert.Equal(4L, PyOps.CallValue(call, [function, 4L], [], []));
        Assert.Equal(1, calls);
        Assert.True(PyOps.FunctionDefaultsUnobserved(function, function));
        PyOps.CallValue(setattr, [function, "__signature__", PyNone.Instance], [], []);
        Assert.Equal(true, PyOps.CallValue(hasattr, [function, "__signature__"], [], []));
        Assert.Same(PyNone.Instance, PyOps.CallValue(getattr, [function, "__signature__"], [], []));
        Assert.True(PyOps.FunctionDefaultsUnobserved(function, function));
    }
}
