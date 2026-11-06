// C1 冷消费与范围外边界：完整 CP 正例仍 pending，明确 NI 不冒充 CP TypeError。
// 每项恢复所改模块槽；新增 ClassVar 导出不能破坏旧 Optional/Union、TypeVar 与 NamedTuple。
using System;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingClassVarBoundaryTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();
    private static object? Cv(object? key) => PyOps.GetItem(Form("ClassVar"), key);
    private static object? Apply(object? root, object? key) => PyOps.GetItem(root, key);
    private static object? Bound(object? key) => PyOps.CallValue(Form("TypeVar"), ["Bound"], ["bound"], [key]);
    private static object? Named(object? key) => Call(Form("NamedTuple"), "Record", new PyList([new PyTuple(["x", key])]));

    [Fact]
    public void SavedRootHandlesReboundDeletedAndRestoredExportsAtColdConsumption()
    {
        object? root = Form("ClassVar"), warmKey = Variable("Warm"), coldKey = Variable("Cold");
        object? warm = Apply(root, warmKey), inner = Apply(root, Variable("Inner"));
        object? nested;
        try
        {
            Typing["ClassVar"] = "changed";
            Assert.Same(warm, Apply(root, warmKey));
            Assert.Same(root, Get(Apply(root, coldKey), "__origin__"));
            nested = Apply(root, inner);
            Assert.Equal(new[] { inner }, Assert.IsType<PyTuple>(Get(nested, "__args__")).Items);
            Typing["ClassVar"] = root;
            Assert.Same(nested, Apply(root, inner));
            Assert.Throws<PyTypeError>(() => Apply(root, Apply(root, Variable("NewInner"))));
            Typing.Remove("ClassVar");
            Assert.Same(warm, Apply(root, warmKey));
            Assert.Equal("name 'ClassVar' is not defined",
                Assert.Throws<PyNameError>(() => Apply(root, Variable("DeletedCold"))).PyMessage);
        }
        finally { Typing["ClassVar"] = root; }
        Assert.Same(root, Get(Apply(root, Variable("Restored")), "__origin__"));
    }

    [Fact]
    public void RebindingClassVarToListDistinguishesTypingAndNativeAliases()
    {
        object? root = Form("ClassVar"), t = Variable("ReboundList");
        object? typingKey = PyOps.GetItem(Form("List"), t), nativeKey = PyOps.GetItem(PyType.Get("list"), t);
        Typing["ClassVar"] = PyType.Get("list");
        try
        {
            Assert.Equal("typing.List[~ReboundList] is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Apply(root, typingKey)).PyMessage);
            Assert.Equal(new[] { nativeKey }, Assert.IsType<PyTuple>(Get(Apply(root, nativeKey), "__args__")).Items);
        }
        finally { Typing["ClassVar"] = root; }
        Assert.Equal(new[] { typingKey }, Assert.IsType<PyTuple>(Get(Apply(root, typingKey), "__args__")).Items);
    }

    [Fact]
    public void WarmEntriesSkipHelpersWhileUnknownColdCallbacksAreExplicit()
    {
        foreach (string name in new[] { "_type_check", "_GenericAlias", "_collect_parameters" })
        {
            object? key = Variable("WarmHelper"), cold = Variable("ColdHelper"), alias = Cv(key);
            object? args = Get(alias, "__args__"), parameters = Get(alias, "__parameters__");
            bool had = Typing.TryGetValue(name, out object? saved);
            int calls = 0;
            Typing[name] = PyNativeFunctions.CreateVariadic("changed", _ => { calls++; return PyType.Get("float"); });
            try
            {
                Assert.Same(alias, Cv(key));
                Assert.Same(args, Get(alias, "__args__"));
                Assert.Same(parameters, Get(alias, "__parameters__"));
                Assert.Throws<PyNotImplementedError>(() => Cv(cold));
                Assert.Equal(0, calls);
            }
            finally { if (had) Typing[name] = saved; else Typing.Remove(name); }
        }
    }

    [Fact]
    public void LegalDeferredDomainsRemainNotImplementedInsteadOfFakeTypeErrors()
    {
        object? root = Form("ClassVar"), t = Variable("Deferred"), alias = Cv(t);
        foreach (object? key in new object?[] { 42L, true, 2.5, PyEllipsis.Instance, "Thing", new PyList([PyType.Get("int")]), new PyDict() })
            Assert.Throws<PyNotImplementedError>(() => Cv(key));
        Assert.Throws<PyNotImplementedError>(() => Apply(alias, PyType.Get("int")));
        foreach (var (name, value) in new (string, object?)[] { ("__args__", new PyTuple([PyType.Get("str")])),
            ("__parameters__", new PyTuple([])), ("__origin__", PyType.Get("list")), ("__module__", "changed") })
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(alias, name, value));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(root, "_name", "Renamed"));
        // 仅手造 CLR 载体测试 C1 冷收集的防线；公开 native Generic 构造仍在完整 CP pending 源中。
        // 此断言不声称 Python list[Generic[T]] 已支持，也不替代端到端原源。
        object? indirect = new PyTypingAlias("list", PyType.Get("list"), [Subscribe(t)]);
        Assert.Throws<PyNotImplementedError>(() => Cv(indirect));
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("Key", [PyRuntimeBases.Validate(PyType.Get("object"))], null));
        int calls = 0;
        PyObject.SetAttr(cls, "__hash__", PyNativeFunctions.CreateVariadic("__hash__", _ => { calls++; return 701L; }));
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.Throws<PyNotImplementedError>(() => Cv(Call(cls)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void NewExportKeepsOldColdConsumersWorkingAndNativeRulesSeparate()
    {
        object? root = Form("ClassVar"), integer = PyType.Get("int"), t = Variable("OldCold");
        Assert.Same(integer, Get(Bound(integer), "__bound__"));
        object? constrained = Call(Form("TypeVar"), "Choices", root, PyType.Get("str"));
        Assert.Equal(new[] { root, PyType.Get("str") }, Assert.IsType<PyTuple>(Get(constrained, "__constraints__")).Items);
        Assert.IsAssignableFrom<PyTuple>(Call(Named(integer), 7L));
        object? optional = PyOps.GetItem(Form("Optional"), t);
        Assert.Same(optional, PyOps.GetItem(Form("Union"), new PyTuple([t, PyType.Get("NoneType")])));
        foreach (object? value in new[] { root, Cv(integer) })
        {
            string expected = ReferenceEquals(root, value) ? "Plain typing.ClassVar is not valid as type argument"
                : "typing.ClassVar[int] is not valid as type argument";
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Bound(value)).PyMessage);
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => Named(value)).PyMessage);
            Assert.Equal(new[] { value }, Assert.IsType<PyTuple>(Get(PyOps.GetItem(PyType.Get("list"), value), "__args__")).Items);
        }
        object? nested = Cv(integer);
        Assert.Equal("typing.ClassVar[int] is not valid as type argument", Assert.Throws<PyTypeError>(() => PyOps.GetItem(Form("List"), nested)).PyMessage);
        Assert.Equal("typing.ClassVar[int] is not valid as type argument", Assert.Throws<PyTypeError>(() => PyOps.GetItem(Form("Optional"), nested)).PyMessage);
        Assert.Equal("typing.ClassVar[int] is not valid as type argument", Assert.Throws<PyTypeError>(() => PyOps.GetItem(Form("Union"), new PyTuple([nested, integer]))).PyMessage);
    }
}
