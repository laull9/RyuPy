// inspect冷元数据和绑定的行为门：身份、live参数字典及严格错误不依赖输出格式。
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class PyInspectTests
{
    [Fact]
    public void ReflectionReadsCurrentMetadataAndPreservesOldSnapshotIdentity()
    {
        object first = new PyList(), second = new PyList();
        var shape = PySignature.Create("f", ["x", "option"], ["pos", "kwonly"], ["1", "1"]);
        var f = new PyFunction("f", shape, (_, _, _, _) => throw new Xunit.Sdk.XunitException("inspect executed body"), [first, 1L]);
        Assert.True(PyOps.FunctionDefaultsUnobserved(f, f));
        var old = PyInspectCallable.Signature(f);
        Assert.False(PyOps.FunctionDefaultsUnobserved(f, f));
        PyObject.SetAttr(f, "__defaults__", new PyTuple([second]));
        var keywords = Assert.IsType<PyDict>(PyObject.GetAttr(f, "__kwdefaults__")); keywords.Set("option", second);
        var annotations = Assert.IsType<PyDict>(PyObject.GetAttr(f, "__annotations__")); annotations.Set("x", PyType.Get("int"));
        var current = PyInspectCallable.Signature(f);
        Assert.Same(first, old.Parameters[0].Default); Assert.Same(second, current.Parameters[0].Default);
        Assert.Same(second, current.Parameters[1].Default); Assert.Same(PyType.Get("int"), current.Parameters[0].Annotation);
        Assert.Same(PyInspectModule.Empty, old.Parameters[0].Annotation);
        PyObject.SetAttr(f, "__signature__", old); Assert.Same(old, PyInspectCallable.Signature(f));
    }

    [Fact]
    public void BindingPreservesPositionalOnlyKeywordInVariadicDictionaryAndLiveReplacement()
    {
        object shared = new PyList();
        var s = new PyInspectSignature([
            new("a", PyInspectKind.Values[0], PyInspectModule.Empty, PyInspectModule.Empty),
            new("b", PyInspectKind.Values[1], shared, PyInspectModule.Empty),
            new("args", PyInspectKind.Values[2], PyInspectModule.Empty, PyInspectModule.Empty),
            new("c", PyInspectKind.Values[3], PyInspectModule.Empty, PyInspectModule.Empty),
            new("kwargs", PyInspectKind.Values[4], PyInspectModule.Empty, PyInspectModule.Empty)], PyInspectModule.Empty);
        var bound = s.Bind([1L], ["c", "a"], [3L, 6L]);
        Assert.Equal(3, bound.Arguments.Count);
        Assert.Equal(6L, Assert.IsType<PyDict>(bound.Arguments.Get("kwargs")).Get("a"));
        bound.ApplyDefaults(); Assert.Same(shared, bound.Arguments.Get("b"));
        var replacement = new PyDict(); replacement.Set("a", 7L); replacement.Set("c", 9L);
        bound.SetAttr("arguments", replacement); Assert.Same(replacement, bound.Arguments);
        Assert.Equal(7L, Assert.IsType<PyTuple>(bound.GetAttr("args")).Items[0]);
        Assert.Equal(9L, Assert.IsType<PyDict>(bound.GetAttr("kwargs")).Get("c"));
        Assert.Throws<PyTypeError>(() => s.Bind([], [], []));
        Assert.Throws<PyTypeError>(() => s.Bind([1L, 2L], ["b", "c"], [4L, 3L]));
    }

    [Fact]
    public void KindKeysMatchIntegersAndKeywordOrderDoesNotChangeSignatureEquality()
    {
        var dict = new PyDict(); dict.Set(PyInspectKind.Values[3], "value"); Assert.Equal("value", dict.Get(3L));
        var a = new PyInspectParameter("a", PyInspectKind.Values[3], PyInspectModule.Empty, PyInspectModule.Empty);
        var b = new PyInspectParameter("b", PyInspectKind.Values[3], PyInspectModule.Empty, PyInspectModule.Empty);
        var left = new PyInspectSignature([a, b], PyInspectModule.Empty);
        var right = new PyInspectSignature([b, a], PyInspectModule.Empty);
        Assert.True(PyHash.Equal(left, right)); Assert.Equal(PyHash.Of(left), PyHash.Of(right));
        Assert.Throws<PyAttributeError>(() => a.SetAttr("name", "new"));
        Assert.Throws<PyValueError>(() => new PyInspectSignature([a, a], PyInspectModule.Empty));
    }
    [Fact]
    public void TrustedKindsAreCanonicalAndDoNotOpenRuntimeCodeReflection()
    {
        var shape = PySignature.Create("f", [], [], []);
        var ordinary = new PyFunction("f", shape, (_, _, _, _) => PyNone.Instance, []);
        var unknown = new PyFunction("f", shape, ordinary.Body, []);
        PyOps.FunctionIntrospectionKind(ordinary, 0);
        Assert.True(PyInspectFunctionKind.IsOrdinaryModule(ordinary));
        Assert.False(PyInspectFunctionKind.IsOrdinaryModule(unknown));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(ordinary, "__code__"));
        for (long kind = 1; kind <= 4; kind++)
        {
            var function = new PyFunction("f", shape, ordinary.Body, []);
            PyOps.FunctionIntrospectionKind(function, kind);
            Assert.False(PyInspectFunctionKind.IsOrdinaryModule(function));
        }
        Assert.Throws<PyRuntimeError>(() => PyOps.FunctionIntrospectionKind(ordinary, 0));
        Assert.Throws<PyValueError>(() => PyOps.FunctionIntrospectionKind(unknown, 9));
    }

    [Fact]
    public void ParameterReprUsesFixedOuterQuotesAndPreservesInnerText()
    {
        var plain = new PyInspectParameter("x", PyInspectKind.Values[1], PyInspectModule.Empty, PyInspectModule.Empty);
        var quoted = new PyInspectParameter("x", PyInspectKind.Values[1], "\"", PyInspectModule.Empty);
        Assert.Equal("<Parameter \"x\">", PyRepr.Repr(plain));
        Assert.Equal("<Parameter \"x='\"'\">", PyRepr.Repr(quoted));
    }

}
