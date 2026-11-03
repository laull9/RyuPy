// 普通单继承构造协议：返回值决定init，特殊布局仍保专用路径，罕见槽历史冷拒。
using System;
using System.Collections.Generic;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyOrdinaryConstructionTests
{
    private static PyClass Make(string name, PyClass? parent = null) =>
        (PyClass)PyOps.MakeClass(name, parent is null ? [] : [parent], null);
    private static void Finish(PyClass cls) => PyOps.FinalizeOrdinaryClass(cls);
    private static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    private static object? Root(string name, params object?[] args) => Call(PyObject.GetAttr(PyBaseObject.Type, name), args);
    private static PyFunction Variadic(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static PyFunction Allocator() => Variadic("__new__", args => Root("__new__", args[0]));
    // 仅测试冷布局守卫；不为测试扩公开runtime API或Python属性。
    private static void MarkNativeLayout(PyClass cls)
    {
        PropertyInfo? marker = typeof(PyClass).GetProperty("IsNativeRuntimeClass", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(marker); marker.SetValue(cls, true);
    }

    [Fact]
    public void PlainAndCustomInitKeepOriginalAllocationAndArgumentIdentity()
    {
        var plain = Make("Plain"); Finish(plain);
        Assert.IsType<PyInstance>(plain.Invoke([], [], []));
        Assert.Throws<PyTypeError>(() => plain.Invoke([1L], [], []));
        var init = Make("Init"); object token = new PyList([]); object? seen = null;
        init.SetAttr("__init__", PyNativeFunctions.Create("__init__", ["self", "value"], [null, null], 2,
            args => { seen = args[1]; return PyNone.Instance; })); Finish(init);
        object value = init.Invoke([], ["value"], [token])!;
        Assert.Same(init, Assert.IsType<PyInstance>(value).Class); Assert.Same(token, seen);
    }

    [Fact]
    public void CustomNewRunsBeforeInitAndUsesOriginalKeywordObject()
    {
        var cls = Make("Factory"); object token = new PyList([]); var log = new List<string>();
        cls.SetAttr("__new__", PyNativeFunctions.Create("__new__", ["cls", "value"], [null, null], 2,
            args => { Assert.Same(cls, args[0]); Assert.Same(token, args[1]); log.Add("new"); return Root("__new__", cls); }));
        cls.SetAttr("__init__", PyNativeFunctions.Create("__init__", ["self", "value"], [null, null], 2,
            args => { Assert.Same(token, args[1]); log.Add("init"); return PyNone.Instance; })); Finish(cls);
        Assert.Same(cls, Assert.IsType<PyInstance>(cls.Invoke([], ["value"], [token])).Class);
        Assert.Equal(new[] { "new", "init" }, log);
    }

    [Fact]
    public void ForeignAndNoneSkipInitButCachedOwnInstanceDoesNot()
    {
        var cls = Make("Cached"); int calls = 0; object? result = new PyList([]);
        cls.SetAttr("__new__", Variadic("__new__", _ => result));
        cls.SetAttr("__init__", Variadic("__init__", _ => { calls++; return PyNone.Instance; })); Finish(cls);
        Assert.Same(result, cls.Invoke([], [], [])); Assert.Equal(0, calls);
        result = PyNone.Instance; Assert.Same(result, cls.Invoke([], [], [])); Assert.Equal(0, calls);
        result = Root("__new__", cls); Assert.Same(result, cls.Invoke([], [], []));
        Assert.Same(result, cls.Invoke([], [], [])); Assert.Equal(2, calls);
    }

    [Fact]
    public void ReturnedSubclassSelectsItsLatestInitAfterNew()
    {
        var parent = Make("Parent"); var child = Make("Child", parent); var log = new List<string>();
        parent.SetAttr("__new__", Variadic("__new__", args =>
        {
            Assert.Same(parent, args[0]);
            child.SetAttr("__init__", Variadic("late", a => { log.Add("late"); Assert.Same(child, ((PyInstance)a[0]!).Class); return PyNone.Instance; }));
            return Root("__new__", child);
        }));
        parent.SetAttr("__init__", Variadic("__init__", _ => throw new Exception("wrong requested init")));
        child.SetAttr("__init__", Variadic("__init__", _ => throw new Exception("stale init")));
        Finish(parent); Finish(child);
        Assert.Same(child, Assert.IsType<PyInstance>(parent.Invoke([7L], [], [])).Class);
        Assert.Equal(new[] { "late" }, log);
    }

    [Fact]
    public void InitialFourSlotCombinationsMatchRootExtraArgumentRules()
    {
        for (int bits = 0; bits != 4; bits++)
        {
            var cls = Make("C"); bool hasNew = (bits & 1) != 0, hasInit = (bits & 2) != 0;
            if (hasNew) cls.SetAttr("__new__", Allocator());
            if (hasInit) cls.SetAttr("__init__", Variadic("__init__", _ => PyNone.Instance));
            Finish(cls);
            if (bits == 0) Assert.Throws<PyTypeError>(() => cls.Invoke([1L], [], []));
            else Assert.IsType<PyInstance>(cls.Invoke([1L], [], []));
            if (!hasNew && hasInit) Assert.IsType<PyInstance>(Root("__new__", cls, 1L));
            else Assert.Throws<PyTypeError>(() => Root("__new__", cls, 1L));
            object instance = Root("__new__", cls)!;
            if (hasNew && !hasInit) Assert.Same(PyNone.Instance, Root("__init__", instance, 1L));
            else Assert.Throws<PyTypeError>(() => Root("__init__", instance, 1L));
        }
    }

    [Fact]
    public void DefinitionAndLateFunctionAssignmentsHaveDifferentBinding()
    {
        var defined = Make("Defined"); PyFunction first = Allocator(); defined.SetAttr("__new__", first); Finish(defined);
        Assert.IsType<PyStaticMethod>(defined.Attributes["__new__"]);
        Assert.Same(PyType.Get("staticmethod"), PyObject.TypeOf(defined.Attributes["__new__"]));
        object instance = Root("__new__", defined)!;
        Assert.Same(first, PyObject.GetAttr(instance, "__new__"));
        var late = Make("Late"); Finish(late); PyFunction second = Allocator(); late.SetAttr("__new__", second);
        Assert.Same(second, late.Attributes["__new__"]);
        Assert.IsType<PyBoundMethod>(PyObject.GetAttr(Root("__new__", late), "__new__"));
        Assert.Same(late, Assert.IsType<PyInstance>(late.Invoke([], [], [])).Class);
    }

    [Fact]
    public void StaticAndClassMethodSlotsPreserveTheirActualBinding()
    {
        var cls = Make("Wrapped");
        cls.SetAttr("__new__", new PyClassMethod(Variadic("__new__", a =>
        { Assert.Same(cls, a[0]); Assert.Same(cls, a[1]); return Root("__new__", cls); })));
        object token = new PyList([]);
        cls.SetAttr("__init__", new PyStaticMethod(Variadic("__init__", a =>
        { Assert.Single(a); Assert.Same(token, a[0]); return PyNone.Instance; }))); Finish(cls);
        Assert.IsType<PyInstance>(cls.Invoke([token], [], []));
        cls.SetAttr("__init__", new PyClassMethod(Variadic("__init__", a =>
        { Assert.Same(cls, a[0]); Assert.Same(token, a[1]); return PyNone.Instance; })));
        Assert.IsType<PyInstance>(cls.Invoke([token], [], []));
    }

    [Fact]
    public void InheritedNewRebindingInvalidatesAndSuperUsesDefinitionOwner()
    {
        var parent = Make("Parent"); var child = Make("Child", parent); int calls = 0;
        parent.SetAttr("__new__", Variadic("__new__", a =>
        {
            calls++; Assert.Same(child, a[0]);
            return Call(PyObject.GetAttr(PyMethodContext.MakeSuper(), "__new__"), child);
        })); Finish(parent); Finish(child);
        Assert.IsType<PyInstance>(child.Invoke([], [], [])); Assert.Equal(1, calls);
        parent.SetAttr("__new__", Variadic("replacement", a => { calls += 10; return Root("__new__", a[0]); }));
        Assert.IsType<PyInstance>(child.Invoke([], [], [])); Assert.Equal(11, calls);
    }

    [Fact]
    public void FinalizerValidatesBeforeWrappingAndLateUnsupportedMutationsAreAtomic()
    {
        var invalid = Make("Invalid"); PyFunction raw = Allocator(); invalid.SetAttr("__new__", raw);
        invalid.SetAttr("__init__", new PyProperty(null, null));
        long before = PyClass.GlobalVersion;
        Assert.Throws<PyNotImplementedError>(() => Finish(invalid));
        Assert.Same(raw, invalid.Attributes["__new__"]); Assert.Equal(before, PyClass.GlobalVersion);
        var cls = Make("C"); cls.SetAttr("__new__", Allocator()); Finish(cls);
        object? original = cls.Attributes["__new__"]; before = PyClass.GlobalVersion;
        Assert.Throws<PyNotImplementedError>(() => cls.SetAttr("__new__", PyObject.GetAttr(PyBaseObject.Type, "__new__")));
        Assert.Throws<PyNotImplementedError>(() => cls.DeleteAttr("__new__"));
        Assert.Same(original, cls.Attributes["__new__"]); Assert.Equal(before, PyClass.GlobalVersion);
        var plain = Make("Plain"); Finish(plain); Assert.False(plain.DeleteAttr("__new__"));
    }

    [Fact]
    public void NonCallableAndNonNoneInitAreTypeErrorsWithCallerContext()
    {
        var cls = Make("Bad"); cls.SetAttr("__new__", PyNone.Instance); Finish(cls);
        var active = new PyValueError("active"); object? old = PyErrors.EnterHandler(active);
        long depth = PyTraceback.PushFrame("construct.py", "caller");
        try
        {
            var error = Assert.Throws<PyTypeError>(() => cls.Invoke([], [], []));
            Assert.Same(active, error.Context); Assert.NotEmpty(error.Frames);
            cls.SetAttr("__new__", Allocator()); cls.SetAttr("__init__", PyNone.Instance);
            Assert.Throws<PyTypeError>(() => cls.Invoke([], [], []));
            cls.SetAttr("__init__", Variadic("__init__", _ => 7L));
            Assert.Throws<PyTypeError>(() => cls.Invoke([], [], []));
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(old); }
    }

    [Fact]
    public void DataclassActivationAndLateNativeAncestorInvalidateConstruction()
    {
        // 当前真实dataclass只支持无基类；先成功快计划，再在本类新增new后明确拒。
        var data = Make("Data"); Finish(data);
        PyOps.DeclareDataclass(data, [], false); PyOps.ApplyDataclass(data, false);
        Assert.IsType<PyInstance>(data.Invoke([], [], []));
        data.SetAttr("__new__", Allocator());
        Assert.Throws<PyNotImplementedError>(() => data.Invoke([], [], []));
        Assert.Throws<PyNotImplementedError>(() => Root("__new__", data));
        // 先缓存普通custom-new成功状态，再由真实装饰器激活special布局。
        var own = Make("Own"); own.SetAttr("__new__", Allocator()); Finish(own);
        Assert.IsType<PyInstance>(own.Invoke([], [], []));
        PyOps.DeclareDataclass(own, [], false); PyOps.ApplyDataclass(own, false);
        Assert.Throws<PyNotImplementedError>(() => own.Invoke([], [], []));
        // 祖先late-new使用实际已支持的TestCase单继承，不伪造dataclass继承或改共享基类。
        var parent = Make("CaseParent", PyUnittest.TestCase); Finish(parent);
        var child = Make("CaseChild", parent); Finish(child);
        Assert.IsType<PyInstance>(child.Invoke([], [], []));
        parent.SetAttr("__new__", Allocator());
        Assert.Throws<PyNotImplementedError>(() => child.Invoke([], [], []));
    }

    [Fact]
    public void SpecialLayoutAllocationIsNeverFakedByObjectNew()
    {
        var native = Make("Native"); MarkNativeLayout(native);
        Assert.Throws<PyNotImplementedError>(() => Root("__new__", native));
        Assert.Throws<PyTypeError>(() => Root("__new__", PyType.Get("int")));
        var parent = Make("Plain"); var specialChild = Make("SpecialChild", parent); MarkNativeLayout(specialChild);
        parent.SetAttr("__new__", Variadic("__new__", _ => new PyInstance(specialChild))); Finish(parent);
        Assert.Throws<PyNotImplementedError>(() => parent.Invoke([], [], []));
    }
}
