// TextWrapper真实类、绑定、可变配置与模块构造合同；每例独立工厂避免污染模块驻留对象。
using System;
using System.Collections.Generic;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextWrapperClassTests
{
    // 只测试私有冷工厂，不扩运行时公开API；生产仍由Lazy仅发布一次。
    private static Dictionary<string, object?> Module() => (Dictionary<string, object?>)
        typeof(PyTextWrap).GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
    private static PyClass Root(Dictionary<string, object?> module) => Assert.IsType<PyClass>(module["TextWrapper"]);
    private static object? Call(object? callable, params object?[] args) => PyOps.CallValue(callable, args, [], []);
    private static object? Method(object? self, string name, params object?[] args) => Call(PyObject.GetAttr(self, name), args);
    private static PyFunction Fn(string name, string[] names, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, new object?[names.Length], names.Length, body);
    private static PyClass Child(PyClass root)
    {
        var cls = (PyClass)PyOps.MakeClass("Child", [PyRuntimeBases.Validate(root)], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static string[] Lines(object? value) => Assert.IsType<PyList>(value).Items.ConvertAll(x => Assert.IsType<string>(x)).ToArray();

    // 只反射已知私有计划及其构造/调用签名；不新增生产可见的调试入口。
    private static object Plan(Dictionary<string, object?> module)
    {
        PyClass root = Root(module);
        var init = Assert.IsType<PyFunction>(PyObject.GetAttr(root, "__init__"));
        var slots = new int[12];
        for (int index = 0; index < slots.Length; index++)
            Assert.True(root.TryGetInstanceSlot(init.Signature.Names[index + 1], out slots[index]));
        Type type = typeof(PyTextWrap).Assembly.GetType("RyuPy.Runtime.PyTextWrapper+FastPlan", true)!;
        ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(PyClass), typeof(Dictionary<string, object?>), typeof(int[])], null)!;
        return constructor.Invoke([root, module, slots]);
    }

    private static bool Eligible(object plan, object? instance)
    {
        MethodInfo method = plan.GetType().GetMethod("TryWrap", BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(object), typeof(object), typeof(object).MakeByRefType()], null)!;
        object?[] arguments = [instance, "one two", null];
        return Assert.IsType<bool>(method.Invoke(plan, arguments));
    }

    [Fact]
    public void OrdinaryIdentityAndInheritedConstructionAreReal()
    {
        var module = Module(); PyClass root = Root(module), child = Child(root), grandchild = Child(child);
        var instance = Assert.IsType<PyInstance>(grandchild.Invoke([4L], [], []));
        Assert.Same(grandchild, PyObject.TypeOf(instance));
        Assert.True(PyObject.IsInstance(instance, root));
        Assert.True(grandchild.IsSubclassOf(root));
        Assert.Same(root, PyRuntimeBases.Validate(root));
        Assert.Equal("textwrap", PyObject.GetAttr(root, "__module__"));
        Assert.Equal(new[] { "one", "two" }, Lines(Method(instance, "wrap", "one two")));
    }

    [Fact]
    public void InitKeepsRawValuesAndOnlyChecksSignature()
    {
        PyClass root = Root(Module()); object token = new PyList([]);
        var instance = Assert.IsType<PyInstance>(root.Invoke([token], ["placeholder"], [token]));
        Assert.Same(token, PyObject.GetAttr(instance, "width"));
        Assert.Same(token, PyObject.GetAttr(instance, "placeholder"));
        Assert.Throws<PyTypeError>(() => root.Invoke([2L], ["width"], [3L]));
        Assert.Throws<PyTypeError>(() => root.Invoke([], ["extra"], [true]));
        Assert.Throws<PyTypeError>(() => root.Invoke([1L, "", "", true, true, false, true, true, true, 8L, 1L], [], []));
        Assert.IsType<PyInstance>(root.Invoke([1L, "", "", true, true, false, true, true, true, 8L], [], []));
    }

    [Fact]
    public void ExplicitInitSupportsDuckAndReinitializesOnlyConfig()
    {
        PyClass root = Root(Module()); var duckClass = (PyClass)PyOps.MakeClass("Duck", [], null);
        var duck = new PyInstance(duckClass);
        PyObject.SetAttr(duck, "extra", 41L);
        object? init = PyObject.GetAttr(root, "__init__");
        Assert.Same(PyNone.Instance, Call(init, duck, 2L));
        Assert.Equal(2L, PyObject.GetAttr(duck, "width"));
        Call(init, duck);
        Assert.Equal(70L, PyObject.GetAttr(duck, "width"));
        Assert.Equal(41L, PyObject.GetAttr(duck, "extra"));
    }

    [Fact]
    public void SetterFailurePreservesEarlierAssignmentsAndOriginalException()
    {
        PyClass root = Root(Module()), child = Child(root); var error = new PyValueError("setter");
        var events = new List<string>();
        child.SetAttr("subsequent_indent", new PyProperty(null, Fn("setter", ["self", "value"], _ =>
        { events.Add("setter"); throw error; })));
        var instance = new PyInstance(child);
        Assert.Same(error, Assert.Throws<PyValueError>(() => Call(PyObject.GetAttr(root, "__init__"), instance, 9L, ">")));
        Assert.Equal(9L, PyObject.GetAttr(instance, "width"));
        Assert.Equal(">", PyObject.GetAttr(instance, "initial_indent"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "expand_tabs"));
        Assert.Equal(new[] { "setter" }, events);
    }

    [Fact]
    public void WrapUsesDynamicHooksAndPreservesReturnIdentity()
    {
        PyClass root = Root(Module()), child = Child(root); var events = new List<string>();
        var chunks = new PyList(["chunk"]); object token = new PyList(["result"]);
        child.SetAttr("_split_chunks", Fn("split", ["self", "text"], a => { events.Add("split"); return chunks; }));
        child.SetAttr("_fix_sentence_endings", Fn("fix", ["self", "chunks"], a =>
        { events.Add("fix"); Assert.Same(chunks, a[1]); return PyNone.Instance; }));
        child.SetAttr("_wrap_chunks", Fn("wrap", ["self", "chunks"], a =>
        { events.Add("wrap"); Assert.Same(chunks, a[1]); return token; }));
        object? instance = child.Invoke([], ["fix_sentence_endings", "width", "max_lines"], [true, new PyList([]), 3L]);
        Assert.Same(token, Method(instance, "wrap", new PyList([])));
        Assert.Equal(new[] { "split", "fix", "wrap" }, events);
    }

    [Fact]
    public void ExplicitRootWrapBypassesPublicOverrideButRootFillDoesNot()
    {
        PyClass root = Root(Module()), child = Child(root);
        child.SetAttr("wrap", Fn("wrap", ["self", "text"], _ => new PyList(["public"])));
        child.SetAttr("_wrap_chunks", Fn("chunks", ["self", "chunks"], _ => new PyList(["internal"])));
        object? instance = child.Invoke([], [], []);
        Assert.Equal(new[] { "internal" }, Lines(Call(PyObject.GetAttr(root, "wrap"), instance, "abc")));
        Assert.Equal("public", Call(PyObject.GetAttr(root, "fill"), instance, "abc"));
    }

    [Fact]
    public void InstanceHooksAndLateClassChangesAreObserved()
    {
        PyClass root = Root(Module()), child = Child(root); object? instance = child.Invoke([], [], []);
        Assert.Equal(new[] { "abc" }, Lines(Method(instance, "wrap", "abc")));
        child.SetAttr("_wrap_chunks", Fn("late", ["self", "chunks"], _ => new PyList(["class"])));
        Assert.Equal(new[] { "class" }, Lines(Method(instance, "wrap", "abc")));
        PyObject.SetAttr(instance, "_wrap_chunks", Fn("instance", ["chunks"], _ => new PyList(["instance"])));
        Assert.Equal(new[] { "instance" }, Lines(Method(instance, "wrap", "abc")));
    }

    [Fact]
    public void ExistingConfigSlotsAreReadAgainAndInstancesAreIndependent()
    {
        PyClass root = Root(Module()); object? first = root.Invoke([4L], [], []), second = root.Invoke([9L], [], []);
        Assert.Equal(new[] { "one", "two" }, Lines(Method(first, "wrap", "one two")));
        PyObject.SetAttr(first, "width", 9L);
        Assert.Equal(new[] { "one two" }, Lines(Method(first, "wrap", "one two")));
        PyObject.SetAttr(first, "width", 1L);
        Assert.Equal(9L, PyObject.GetAttr(second, "width"));
    }

    [Fact]
    public void RootMutationAndInstanceShadowDisableFastPath()
    {
        PyClass root = Root(Module()); object? instance = root.Invoke([], [], []);
        Method(instance, "wrap", "warm");
        PyObject.SetAttr(instance, "_wrap_chunks", Fn("shadow", ["chunks"], _ => new PyList(["shadow"])));
        Assert.Equal(new[] { "shadow" }, Lines(Method(instance, "wrap", "text")));
        PyObject.DelAttr(instance, "_wrap_chunks");
        root.SetAttr("_wrap_chunks", Fn("late", ["self", "chunks"], _ => new PyList(["late"])));
        Assert.Equal(new[] { "late" }, Lines(Method(instance, "wrap", "text")));
    }

    [Fact]
    public void ModuleReadsCurrentFactoryAndForwardsUnknownKeywords()
    {
        var module = Module(); var targetClass = (PyClass)PyOps.MakeClass("Product", [], null);
        targetClass.SetAttr("wrap", Fn("wrap", ["self", "text"], a => new PyList([a[1]])));
        targetClass.SetAttr("fill", Fn("fill", ["self", "text"], a => "fill:" + a[1]));
        object? product = targetClass.Invoke([], [], []); var seen = new List<object?>();
        module["TextWrapper"] = PyNativeFunctions.Create("factory", ["width", "extra"], [null, null], 2, a =>
        { seen.Add(a[0]); seen.Add(a[1]); return product; }, ["kwonly", "kwonly"]);
        Assert.Equal(new[] { "abc" }, Lines(PyOps.CallValue(module["wrap"], ["abc", 3L], ["extra"], ["x"])));
        Assert.Equal("fill:def", PyOps.CallValue(module["fill"], ["def"], ["extra"], ["y"]));
        Assert.Equal(new object?[] { 3L, "x", 70L, "y" }, seen);
    }

    [Fact]
    public void ModuleObservesRootMethodRebindingAndRejectsExtraPositionals()
    {
        var module = Module(); PyClass root = Root(module);
        root.SetAttr("wrap", Fn("wrap", ["self", "text"], _ => new PyList(["changed"])));
        Assert.Equal(new[] { "changed" }, Lines(Call(module["wrap"], "abc")));
        Assert.Equal("changed", Call(module["fill"], "abc"));
        Assert.Throws<PyTypeError>(() => Call(module["wrap"], "abc", 2L, "bad"));
    }

    [Fact]
    public void RealDefaultsContentAffectsConstructionWithoutMetadataShortcut()
    {
        var module = Module(); PyClass root = Root(module);
        var init = Assert.IsType<PyFunction>(PyObject.GetAttr(root, "__init__"));
        // 精确定位现私有计划类型，不扫描编译器闭包字段；生产闭包接线另由源审确认。
        object plan = Plan(module);
        object? instance = root.Invoke([], [], []);
        Assert.True(Eligible(plan, instance));
        var otherModule = Module(); object isolatedPlan = Plan(otherModule);
        object? isolatedInstance = Root(otherModule).Invoke([], [], []);
        Assert.True(Eligible(isolatedPlan, isolatedInstance));
        init.Defaults[1] = 3L;
        Assert.False(Eligible(plan, instance));
        Assert.True(Eligible(isolatedPlan, isolatedInstance));
        Assert.Equal(3L, PyObject.GetAttr(root.Invoke([], [], []), "width"));
        // module显式width=70覆盖类默认值；不假装Python.__defaults__元数据已实现。
        Assert.Equal(new[] { "one two" }, Lines(Call(module["wrap"], "one two")));
    }

    [Fact]
    public void FastGuardFallbackDoesNotValidateUnusedValues()
    {
        PyClass root = Root(Module()); object token = new PyList([]);
        object? instance = root.Invoke([4L], ["expand_tabs", "tabsize", "placeholder"], [false, token, token]);
        Assert.Equal(new[] { "one", "two" }, Lines(Method(instance, "wrap", "one two")));
        PyObject.SetAttr(instance, "width", 0L);
        Assert.Throws<PyAttributeError>(() => Method(instance, "wrap", PyNone.Instance));
    }

    [Fact]
    public void CallbackExceptionKeepsIdentityAndAmbientContext()
    {
        PyClass root = Root(Module()), child = Child(root);
        PyValueError error = new PyValueError("hook"), outer = new PyValueError("outer");
        child.SetAttr("_split_chunks", Fn("split", ["self", "text"], _ => throw error));
        object? previous = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("textwrap_class_test.py", "caller");
        try
        {
            PyTraceback.SetLine(17);
            Assert.Same(error, Assert.Throws<PyValueError>(() => Method(child.Invoke([], [], []), "wrap", "abc")));
            Assert.Same(outer, error.Context);
            Assert.Equal(17, error.Frames[0].Line);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void PreparedCallbackErrorKeepsItsExistingFramesAndContext()
    {
        PyClass root = Root(Module()), child = Child(root);
        PyValueError error = new PyValueError("saved"), original = new PyValueError("original"), later = new PyValueError("later");
        object? previous = PyErrors.EnterHandler(original);
        long depth = PyTraceback.PushFrame("original.py", "original");
        try { PyTraceback.SetLine(6); PyErrors.PrepareRaise(error); }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        var frames = error.Frames;
        child.SetAttr("_split_chunks", Fn("split", ["self", "text"], _ => throw error));
        previous = PyErrors.EnterHandler(later);
        depth = PyTraceback.PushFrame("later.py", "later");
        try
        {
            PyTraceback.SetLine(99);
            Assert.Same(error, Assert.Throws<PyValueError>(() => Method(child.Invoke([], [], []), "wrap", "abc")));
            Assert.Same(original, error.Context); Assert.Same(frames, error.Frames);
            Assert.Equal(6, error.Frames[0].Line);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void ExcessInitPositionalsKeepRangeAndKeywordErrorPriority()
    {
        PyClass root = Root(Module());
        object?[] values = [1L, "", "", true, true, false, true, true, true, 8L, 1L];
        Assert.Equal("TextWrapper.__init__() takes from 1 to 11 positional arguments but 12 were given",
            Assert.Throws<PyTypeError>(() => root.Invoke(values, [], [])).PyMessage);
        Assert.Equal("TextWrapper.__init__() got an unexpected keyword argument 'unknown'",
            Assert.Throws<PyTypeError>(() => root.Invoke(values, ["unknown"], [1L])).PyMessage);
        Assert.Equal("TextWrapper.__init__() got multiple values for argument 'width'",
            Assert.Throws<PyTypeError>(() => root.Invoke(values, ["width"], [1L])).PyMessage);
    }

}
