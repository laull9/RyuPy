// F0消费相位与明确未知域：CP合法原源留在unsupported用例，NI不是CP通过。
// 每次恢复typing全局，避免污染随后测试；热命中不得访问已替换helper。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingSpecialFormBoundariesTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Apply(object? root, object? key) => PyOps.GetItem(root, key);

    [Fact]
    public void SavedUnionRootSurvivesExportRebindingAndOptionalHitSkipsIt()
    {
        object? optional = Form("Optional"), union = Form("Union"), key = Variable("Warm");
        object? alias = Apply(optional, key);
        Typing["Union"] = "changed";
        try
        {
            Assert.Same(alias, Apply(optional, key));
            object? cold = Variable("Cold");
            object? result = Apply(union, new PyTuple([cold, PyType.Get("int")]));
            Assert.Same(union, Get(result, "__origin__"));
            Assert.Equal("string indices must be integers, not 'tuple'", Assert.Throws<PyTypeError>(() => Apply(optional, PyType.Get("bytes"))).Message);
        }
        finally { Typing["Union"] = union; }
        Assert.Same(alias, Apply(optional, key));
    }

    [Fact]
    public void HotOptionalSkipsReboundTypeCheckButColdUnknownHelperIsExplicit()
    {
        object? optional = Form("Optional"), key = Variable("WarmHelper");
        object? alias = Apply(optional, key);
        bool existed = Typing.TryGetValue("_type_check", out object? saved);
        int calls = 0;
        Typing["_type_check"] = PyNativeFunctions.CreateVariadic("changed", _ =>
        { calls++; return PyType.Get("float"); });
        try
        {
            Assert.Same(alias, Apply(optional, key));
            Assert.Equal(0, calls);
            Assert.Throws<PyNotImplementedError>(() => Apply(optional, Variable("ColdHelper")));
            Assert.Equal(0, calls);
        }
        finally
        {
            if (existed) Typing["_type_check"] = saved;
            else Typing.Remove("_type_check");
        }
    }

    [Fact]
    public void MutableAliasMetadataIsExplicitlyUnsupportedRatherThanReadOnly()
    {
        object? alias = Apply(Form("Optional"), Variable("Mutable"));
        foreach (var (name, value) in new (string, object?)[] {
            ("__args__", new PyTuple([PyType.Get("str"), PyType.Get("NoneType")])),
            ("__parameters__", new PyTuple([])), ("__origin__", PyType.Get("str")), ("__module__", "changed") })
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(alias, name, value));
    }

    [Fact]
    public void LegalForwardScalarAndCallbackKeysDoNotBecomeFalseTypeErrors()
    {
        object? optional = Form("Optional");
        Assert.Throws<PyNotImplementedError>(() => Apply(optional, "Thing"));
        Assert.Throws<PyNotImplementedError>(() => Apply(optional, 42L));
        Assert.Throws<PyNotImplementedError>(() => Apply(optional, new PyList([PyType.Get("int")])));
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("Key", [PyRuntimeBases.Validate(PyType.Get("object"))], null));
        int calls = 0;
        PyObject.SetAttr(cls, "__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ => { calls++; return 701L; }));
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.Throws<PyNotImplementedError>(() => Apply(optional, Call(cls)));
        Assert.Equal(0, calls);
    }
}
