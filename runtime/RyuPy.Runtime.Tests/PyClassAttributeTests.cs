// 通用 __class__ 只读合同：真实类身份、用户覆盖、字段优先级及 PIC 重复读。
// 字段夹具直接写内部槽位，对应 CP 的 obj.__dict__ 布置；绝不冒称动态改类已实现。
using System;
using System.Reflection;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyClassAttributeTests
{
    private const string Name = "__class__";
    private static PyFunction Fn(Func<object?[], object?> body) =>
        PyNativeFunctions.CreateVariadic("class_attribute_test", body);
    private static PyClass Class(string name, PyClass? parent = null) =>
        (PyClass)PyOps.MakeClass(name, parent is null ? [] : [parent], null);
    private static object? Read(object obj, bool pic) => pic
        ? PyPic.GetAttr(907, obj, Name) : PyObject.GetAttr(obj, Name);
    private static void Field(PyInstance obj, object value)
    {
        // 仅测试反射绕过写协议，保留原只读参考的合法字段状态，不经 PyObject.SetAttr 改类。
        int slot = obj.Class.GetOrAddInstanceSlot(Name);
        typeof(PyInstance).GetMethod("SetSlot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(obj, [slot, value]);
    }

    [Fact]
    public void DefaultIdentityDoesNotInternNamesAndSurvivesInheritance()
    {
        PyClass root = Class("Same");
        PyClass same = Class("Same");
        PyClass child = Class("Child", root);
        PyClass grandchild = Class("Grandchild", child);
        PyClass fakeList = Class("list");
        Assert.NotSame(root, same);
        Assert.NotSame(fakeList, PyType.Get("list"));
        foreach (PyClass cls in new[] { root, same, child, grandchild, fakeList })
        {
            var first = new PyInstance(cls); var second = new PyInstance(cls);
            foreach (bool pic in new[] { false, true })
                for (int i = 0; i < 12; i++)
                {
                    Assert.Same(cls, Read(first, pic));
                    Assert.Same(cls, Read(second, pic));
                    Assert.Same(PyObject.TypeOf(first), Read(first, pic));
                }
        }
    }

    [Fact]
    public void RootDataDescriptorBeatsPresentAndLaterFieldsAcrossVersionChanges()
    {
        PyClass cls = Class("Plain");
        var first = new PyInstance(cls); var other = new PyInstance(cls);
        Field(first, "present before cache");
        foreach (bool pic in new[] { false, true })
        {
            for (int i = 0; i < 12; i++) Assert.Same(cls, Read(first, pic));
            Assert.Same(cls, Read(other, pic));
            Field(other, "added after cache");
            for (int i = 0; i < 12; i++)
            {
                // 合法普通类属性变化使全局版本失效，不对 __class__ 本身赋值。
                cls.SetAttr("unrelated", (long)i);
                Assert.Same(cls, Read(first, pic));
                Assert.Same(cls, Read(other, pic));
            }
        }
        Assert.True(cls.TryGetInstanceSlot(Name, out int slot));
        Assert.True(first.TryGetSlot(slot, out object? stored));
        Assert.Equal("present before cache", stored);
    }

    [Fact]
    public void DefaultReadDoesNotInvokeFallback()
    {
        PyClass cls = Class("Fallback"); int calls = 0;
        cls.SetAttr("__getattr__", Fn(_ => { calls++; throw new PyValueError("must not run"); }));
        var obj = new PyInstance(cls);
        foreach (bool pic in new[] { false, true })
            for (int i = 0; i < 12; i++) Assert.Same(cls, Read(obj, pic));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void OrdinaryAndNoneClassItemsPermitInstanceFields()
    {
        foreach (object raw in new object[] { new object(), PyNone.Instance })
        {
            PyClass parent = Class("Value"); parent.SetAttr(Name, raw);
            PyClass child = Class("Child", parent);
            foreach (PyClass cls in new[] { parent, child })
            {
                var obj = new PyInstance(cls);
                foreach (bool pic in new[] { false, true }) Assert.Same(raw, Read(obj, pic));
                object field = new object(); Field(obj, field);
                foreach (bool pic in new[] { false, true })
                    for (int i = 0; i < 12; i++) Assert.Same(field, Read(obj, pic));
                Assert.Same(cls, PyObject.TypeOf(obj));
            }
        }
    }

    [Fact]
    public void PropertyWinsOverFieldAndExecutesOnEveryReadButNotTypeOf()
    {
        PyClass parent = Class("Property"); int calls = 0; object token = new object();
        parent.SetAttr(Name, new PyProperty(Fn(_ => { calls++; return token; })));
        PyClass child = Class("Child", parent);
        foreach (PyClass cls in new[] { parent, child })
        {
            var obj = new PyInstance(cls); Field(obj, "hidden");
            int before = calls;
            Assert.Same(cls, PyObject.TypeOf(obj));
            Assert.Same(PyType.Get("type"), PyObject.GetAttr(cls, Name));
            Assert.Equal(before, calls);
            foreach (bool pic in new[] { false, true })
                for (int i = 0; i < 12; i++) Assert.Same(token, Read(obj, pic));
            Assert.Equal(before + 24, calls);
        }
    }

    [Fact]
    public void NearestClassItemDecidesWhetherTheFieldWins()
    {
        PyClass parent = Class("Property"); int calls = 0;
        parent.SetAttr(Name, new PyProperty(Fn(_ => { calls++; return "parent"; })));
        PyClass child = Class("Ordinary", parent); child.SetAttr(Name, "child");
        var obj = new PyInstance(child);
        foreach (bool pic in new[] { false, true }) Assert.Equal("child", Read(obj, pic));
        Field(obj, "field");
        foreach (bool pic in new[] { false, true }) Assert.Equal("field", Read(obj, pic));
        Assert.Equal(0, calls);
        PyClass grandchild = Class("PropertyAgain", child);
        grandchild.SetAttr(Name, new PyProperty(Fn(_ => "grandchild")));
        var other = new PyInstance(grandchild); Field(other, "hidden");
        foreach (bool pic in new[] { false, true }) Assert.Equal("grandchild", Read(other, pic));
    }

    [Fact]
    public void FunctionsBindWithoutRunningAndNonDataItemsAllowFields()
    {
        foreach (string kind in new[] { "method", "static", "class" })
        {
            PyClass parent = Class("Methods"); int calls = 0;
            PyFunction function = Fn(args => { calls++; return args.Length == 0 ? "static" : args[0]; });
            object raw = kind == "static" ? new PyStaticMethod(function)
                : kind == "class" ? new PyClassMethod(function) : function;
            parent.SetAttr(Name, raw);
            PyClass child = Class("Child", parent); var obj = new PyInstance(child);
            foreach (bool pic in new[] { false, true })
            {
                int before = calls; object? saved = Read(obj, pic);
                Assert.Equal(before, calls);
                object? result = PyOps.CallValue(saved, [], [], []);
                if (kind == "static") Assert.Equal("static", result);
                else Assert.Same(kind == "class" ? (object)child : obj, result);
                Assert.Equal(before + 1, calls);
            }
            object field = new object(); Field(obj, field);
            foreach (bool pic in new[] { false, true }) Assert.Same(field, Read(obj, pic));
        }
    }

    [Fact]
    public void GetterFailuresAndMissingGetterNeverUseTheFieldOrActualClass()
    {
        foreach (bool pic in new[] { false, true })
        {
            PyClass cls = Class("Broken"); var obj = new PyInstance(cls); Field(obj, "hidden");
            int calls = 0; var error = new PyValueError("class getter");
            cls.SetAttr(Name, new PyProperty(Fn(_ => { calls++; throw error; })));
            for (int i = 0; i < 8; i++) Assert.Same(error, Assert.Throws<PyValueError>(() => Read(obj, pic)));
            Assert.Equal(8, calls);
            PyClass missing = Class("NoGetter"); missing.SetAttr(Name, new PyProperty(null));
            var noGetter = new PyInstance(missing); Field(noGetter, "hidden");
            Assert.Contains("has no getter", Assert.Throws<PyAttributeError>(() => Read(noGetter, pic)).PyMessage);
        }
    }

    [Fact]
    public void WarmPropertyCachePropagatesLaterErrorsWithoutClassVersionChange()
    {
        PyClass cls = Class("WarmGetter"); var obj = new PyInstance(cls);
        Field(obj, "hidden");
        int calls = 0; bool fail = false; object token = new object();
        var error = new PyValueError("warm getter failure");
        cls.SetAttr(Name, new PyProperty(Fn(_ =>
        {
            calls++;
            if (fail) throw error;
            return token;
        })));
        // 成功慢路先填充 getter PIC；仅改变闭包状态，不使类版本失效。
        long version = PyClass.GlobalVersion;
        for (int i = 0; i < 8; i++) Assert.Same(token, Read(obj, true));
        Assert.Equal(8, calls);
        fail = true;
        for (int i = 0; i < 8; i++)
            Assert.Same(error, Assert.Throws<PyValueError>(() => Read(obj, true)));
        Assert.Equal(16, calls);
        Assert.Equal(version, PyClass.GlobalVersion);
        fail = false;
        Assert.Same(token, Read(obj, true));
        Assert.Equal(17, calls);
        Assert.Equal(version, PyClass.GlobalVersion);
    }

    [Fact]
    public void PresentNullClassItemIsNotMistakenForMissingLookup()
    {
        // CLR null 是运行时 API 可表达的类字典值；存在性必须由 TryLookup 的 bool 判断。
        PyClass parent = Class("NullItem"); parent.SetAttr(Name, null);
        PyClass child = Class("NullChild", parent);
        foreach (PyClass cls in new[] { parent, child })
        {
            var obj = new PyInstance(cls);
            foreach (bool pic in new[] { false, true })
                for (int i = 0; i < 8; i++) Assert.Null(Read(obj, pic));
            Assert.Same(cls, PyObject.TypeOf(obj));
            Assert.Same(PyType.Get("type"), PyObject.GetAttr(cls, Name));
            object field = new object(); Field(obj, field);
            foreach (bool pic in new[] { false, true })
                for (int i = 0; i < 8; i++) Assert.Same(field, Read(obj, pic));
        }
    }

    [Fact]
    public void ClassObjectAlwaysReadsItsActualMetaclassWithoutBindingOwnItem()
    {
        int calls = 0; PyFunction function = Fn(_ => { calls++; return "bad"; });
        object[] items = [new object(), PyNone.Instance, function, new PyProperty(function),
            new PyStaticMethod(function), new PyClassMethod(function)];
        foreach (object raw in items)
        {
            PyClass parent = Class("Parent"); parent.SetAttr(Name, raw);
            PyClass child = Class("Child", parent);
            foreach (PyClass cls in new[] { parent, child })
                for (int i = 0; i < 12; i++)
                    Assert.Same(PyObject.TypeOf(cls), PyObject.GetAttr(cls, Name));
        }
        foreach (string name in new[] { "type", "object", "int", "str", "list", "dict" })
            Assert.Same(PyType.Get("type"), PyObject.GetAttr(PyType.Get(name), Name));
        Assert.Equal(0, calls);
    }
}
