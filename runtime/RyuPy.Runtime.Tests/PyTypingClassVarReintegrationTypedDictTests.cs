// C1 TypedDict旁路合同：复用 typed-dict-cp-001 的72个双跑场景，不另造旧 NI 期望。
// Begin/Finish只验证运行时阶段；C#的赋值与求值顺序不能代替完整Python类语句差分。
using System;
using System.Collections.Generic;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingClassVarReintegrationTypedDictTests : IDisposable
{
    private readonly long savedDepth;
    public PyTypingClassVarReintegrationTypedDictTests()
    {
        PyNativeModules.Register();
        // Factory按帧filename查唯一模块；仅key唯一而复用generic_root.py会形成歧义。
        string key = "classvar_typeddict_" + Guid.NewGuid().ToString("N");
        string file = key + ".py";
        PyModules.Register(key, "", file, () => { },
            name => name == "__name__" ? key : null, (_, _, _) => { });
        savedDepth = PyTraceback.CaptureDepth();
        PyTraceback.PushFrameWithOrigin(file, "<module>", PyTraceback.CreateModuleExecutionSource(key));
    }
    public void Dispose()
    {
        // PopTo接受Push后的深度；savedDepth+1恰好移除本fixture帧并保留原调用者。
        PyTraceback.PopTo(savedDepth + 1);
        Assert.Equal(savedDepth, PyTraceback.CaptureDepth());
    }
    private static object? Apply(string name, object? key) => PyOps.GetItem(Form(name), key);
    private static void WithClassVar(object? value, Action body)
    {
        bool existed = Typing.TryGetValue("ClassVar", out object? saved);
        Typing["ClassVar"] = value;
        try { body(); }
        finally { if (existed) Typing["ClassVar"] = saved; else Typing.Remove("ClassVar"); }
    }
    private static object Define(bool classStyle, PyDict fields)
    {
        if (!classStyle) return Call(Form("TypedDict"), "C1Typed", fields)!;
        object shell = PyTypedDict.Begin("C1Typed", PyTypedDict.Factory, true, "c1_reintegration");
        PyObject.SetAttr(shell, "__annotations__", fields);
        return PyTypedDict.Finish(shell);
    }
    private static PyDict Fields(object? value)
    {
        var fields = new PyDict();
        fields.Set("before", PyType.Get("int")); fields.Set("subject", value); fields.Set("after", PyType.Get("str"));
        return fields;
    }
    private static void AssertAccepted(bool classStyle, object? value)
    {
        var input = Fields(value);
        object result = Define(classStyle, input);
        var annotations = Assert.IsType<PyDict>(Get(result, "__annotations__"));
        Assert.NotSame(input, annotations);
        Assert.Equal(new object?[] { "before", "subject", "after" }, annotations.Keys);
        Assert.Same(value is null or PyNone ? PyType.Get("NoneType") : value, annotations.Get("subject"));
        Assert.Same(PyType.Get("int"), annotations.Get("before"));
        Assert.Same(PyType.Get("str"), annotations.Get("after"));
        Assert.Equal(true, Call(Form("is_typeddict"), result));
        Assert.Equal(true, Get(result, "__total__"));
        Assert.True(Assert.IsType<PyFrozenSet>(Get(result, "__required_keys__")).SetEquals(new PyFrozenSet(["before", "subject", "after"])));
        Assert.True(Assert.IsType<PyFrozenSet>(Get(result, "__optional_keys__")).SetEquals(new PyFrozenSet([])));
        Assert.Equal(new object?[] { PyType.Get("dict") }, Assert.IsType<PyTuple>(Get(result, "__bases__")).Items);
    }

    [Fact]
    public void TypedDictCanonicalExportRejectsClassVarRootAndAliasInBothStyles()
    {
        object? root = Form("ClassVar"), alias = Apply("ClassVar", PyType.Get("int"));
        foreach (bool classStyle in new[] { false, true })
        {
            Assert.Equal("Plain typing.ClassVar is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Define(classStyle, Fields(root))).PyMessage);
            Assert.Equal("typing.ClassVar[int] is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Define(classStyle, Fields(alias))).PyMessage);
            foreach (object? ordinary in new object?[] { PyType.Get("int"), PyNone.Instance, PyType.Get("NoneType") })
                AssertAccepted(classStyle, ordinary);
        }
    }

    [Fact]
    public void TypedDictListReplacementRejectsTypingListButAcceptsSavedClassVarAndNativeAliases()
    {
        object? root = Form("ClassVar"), alias = Apply("ClassVar", PyType.Get("int")), list = Apply("List", PyType.Get("int"));
        object? native = PyOps.GetItem(PyType.Get("list"), PyType.Get("int"));
        object? nativeRoot = PyOps.GetItem(PyType.Get("list"), root), nativeAlias = PyOps.GetItem(PyType.Get("list"), alias);
        WithClassVar(PyType.Get("list"), () =>
        {
            foreach (bool classStyle in new[] { false, true })
            {
                Assert.Equal("typing.List[int] is not valid as type argument",
                    Assert.Throws<PyTypeError>(() => Define(classStyle, Fields(list))).PyMessage);
                Assert.Equal("Plain typing.ClassVar is not valid as type argument",
                    Assert.Throws<PyTypeError>(() => Define(classStyle, Fields(root))).PyMessage);
                foreach (object? accepted in new[] { alias, native, nativeRoot, nativeAlias, PyType.Get("int"), PyNone.Instance })
                    AssertAccepted(classStyle, accepted);
            }
        });
    }

    [Fact]
    public void TypedDictStringAndNoneReplacementsAllowOldAliasesButNeverBareClassVar()
    {
        object? root = Form("ClassVar"), alias = Apply("ClassVar", PyType.Get("int")), list = Apply("List", PyType.Get("int"));
        foreach (object? replacement in new object?[] { "rebound-classvar", PyNone.Instance })
            WithClassVar(replacement, () =>
            {
                foreach (bool classStyle in new[] { false, true })
                {
                    AssertAccepted(classStyle, alias); AssertAccepted(classStyle, list);
                    Assert.Equal("Plain typing.ClassVar is not valid as type argument",
                        Assert.Throws<PyTypeError>(() => Define(classStyle, Fields(root))).PyMessage);
                }
            });
    }

    [Fact]
    public void TypedDictChecksAfterAllFieldValuesAndKeepsOldBindingOnFailure()
    {
        object? root = Form("ClassVar"), alias = Apply("ClassVar", PyType.Get("int")), list = Apply("List", PyType.Get("int"));
        foreach (var (replacement, invalid, expected) in new (object?, object?, string)[] {
            (root, root, "Plain typing.ClassVar is not valid as type argument"),
            (root, alias, "typing.ClassVar[int] is not valid as type argument"),
            (PyType.Get("list"), list, "typing.List[int] is not valid as type argument") })
            WithClassVar(replacement, () =>
            {
                foreach (bool classStyle in new[] { false, true })
                {
                    var events = new List<string>();
                    object? Mark(string label, object? value) { events.Add(label); return value; }
                    object oldBinding = new(), binding = oldBinding;
                    var fields = new PyDict();
                    fields.Set("before", Mark("before", PyType.Get("int")));
                    fields.Set("subject", Mark("subject", invalid));
                    fields.Set("after", Mark("after", PyType.Get("str")));
                    Assert.Equal(expected, Assert.Throws<PyTypeError>(() => binding = Define(classStyle, fields)).PyMessage);
                    Assert.Same(oldBinding, binding);
                    Assert.Equal(new[] { "before", "subject", "after" }, events);
                    Assert.Equal(new object?[] { "before", "subject", "after" }, fields.Keys);
                    Assert.Same(invalid, fields.Get("subject"));
                }
            });
    }
}
