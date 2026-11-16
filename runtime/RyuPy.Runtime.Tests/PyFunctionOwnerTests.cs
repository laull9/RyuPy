// 实例函数真实归属的CLR协议测试；不代替生成Python正文的端到端验收。
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyFunctionOwnerTests
{
    private static PySignature Signature() => PySignature.Create("f", [], [], []);
    private static PyModule Module(PyModuleExecute body)
    {
        string name = "owner_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "owner.py", 1, "/virtual/owner.py", false, body);
        return PyModuleInstances.Create(name);
    }
    private static PyFunction Function(object context, PyOwnedCall? body = null, object?[]? cells = null) =>
        PyModuleInstances.CreateOwnedFunction(PyModuleInstances.CaptureDefinitionContext(context),
            "f", Signature(), body ?? Read, [], false, cells);
    private static object? Read(object context, PyFunction self, object?[] args, string[] names, object?[] values) =>
        PyModuleInstances.LoadName(context, "answer");
    private static object? Invoke(PyFunction function) => function.Invoke([], [], []);
    private static PyDict Dict(PyModule module) => Assert.IsType<PyDict>(module.GetAttr("__dict__"));

    [Fact]
    public void GlobalsAndBuiltinsAreRealObjectsAndOldFunctionsKeepCapturedBuiltins()
    {
        PyFunction? current = null;
        var module = Module(context => current = Function(context));
        var first = new PyDict(); first.Set("answer", 1L);
        var second = new PyDict(); second.Set("answer", 2L);
        module.SetAttr("__builtins__", first); PyModuleInstances.Execute(module);
        var old = current!;
        module.SetAttr("__builtins__", second); PyModuleInstances.Execute(module);
        Assert.NotSame(old, current);
        Assert.Same(Dict(module), PyObject.GetAttr(old, "__globals__"));
        Assert.Same(Dict(module), PyObject.GetAttr(current!, "__globals__"));
        Assert.Same(first, PyObject.GetAttr(old, "__builtins__"));
        Assert.Same(second, PyObject.GetAttr(current!, "__builtins__"));
        Assert.Equal(1L, Invoke(old)); Assert.Equal(2L, Invoke(current!));
        first.Set("answer", 3L); Assert.Equal(3L, Invoke(old));
        module.SetAttr("answer", 4L); Assert.Equal(4L, Invoke(old)); Assert.Equal(4L, Invoke(current!));
        PyObject.SetAttr(old, "__module__", "elsewhere");
        Assert.Equal(4L, Invoke(old));
    }

    [Fact]
    public void DefinitionUsesCurrentGlobalsRatherThanOldCallerBuiltins()
    {
        object? saved = null; PyFunction? outer = null;
        var module = Module(context => { saved = context; outer = Function(context,
            (owner, self, args, names, values) => Function(owner)); });
        var first = new PyDict(); first.Set("answer", 1L);
        var second = new PyDict(); second.Set("answer", 2L);
        module.SetAttr("__builtins__", first); PyModuleInstances.Execute(module);
        module.SetAttr("__builtins__", second);
        Assert.Equal(1L, PyModuleInstances.LoadName(saved!, "answer"));
        var nested = Assert.IsType<PyFunction>(Invoke(outer!));
        Assert.Equal(2L, Invoke(nested)); Assert.Same(second, PyObject.GetAttr(nested, "__builtins__"));
    }

    [Fact]
    public void FactoryConsumesCaptureWithoutRecapturingAndPreservesCellsAndDefaults()
    {
        object? captured = null;
        var module = Module(context => captured = PyModuleInstances.CaptureDefinitionContext(context));
        var first = new PyDict(); first.Set("answer", 1L);
        module.SetAttr("__builtins__", first); PyModuleInstances.Execute(module);
        module.SetAttr("__builtins__", new PyDict());
        object?[] cells = [PyOps.CellNew()]; object?[] defaults = [new PyList([])];
        var signature = Signature();
        var function = PyModuleInstances.CreateOwnedFunction(captured!, "f", signature, Read, defaults, false, cells);
        Assert.Same(cells, function.Env); Assert.Same(defaults, function.Defaults);
        Assert.Same(signature, function.Signature); Assert.Equal(1L, Invoke(function));
        Assert.Same(first, PyObject.GetAttr(function, "__builtins__"));
        Assert.Throws<PyRuntimeError>(() => PyModuleInstances.CaptureDefinitionContext(new object()));
    }

    [Fact]
    public void OwnerAttributesAreReadonlyAndLegacyNeverManufacturesMirrors()
    {
        PyFunction? function = null; var module = Module(context => function = Function(context));
        PyModuleInstances.Execute(module);
        var legacy = new PyFunction("f", Signature(), (_, _, _, _) => PyNone.Instance, []);
        foreach (string name in new[] { "__globals__", "__builtins__" })
        {
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(function!, name, new PyDict()));
            Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(function!, name));
            Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(legacy, name));
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(legacy, name, new PyDict()));
            Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(legacy, name));
            var attrs = Assert.IsType<PyDict>(PyObject.GetAttr(function!, "__dict__"));
            attrs.Set(name, "fake"); Assert.IsType<PyDict>(PyObject.GetAttr(function!, name));
        }
    }

    [Fact]
    public void FailedExecutionAndRetryCannotRetargetEscapedFunction()
    {
        PyFunction? escaped = null; int attempt = 0;
        string name = "owner_retry_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterInstanceSourceLocated(name, "", "owner.py", 1, "/virtual/owner.py", false,
            context => { PyModuleInstances.StoreName(context, "answer", (long)++attempt);
                var fn = Function(context); PyModuleInstances.StoreName(context, "f", fn);
                if (attempt == 1) { escaped = fn; throw new PyValueError("failure"); } });
        Assert.Throws<PyValueError>(() => PyModules.Import(name));
        var fresh = PyModules.Import(name); var next = Assert.IsType<PyFunction>(fresh.GetAttr("f"));
        Assert.Equal(1L, Invoke(escaped!)); Assert.Equal(2L, Invoke(next));
        Assert.NotSame(PyObject.GetAttr(escaped!, "__globals__"), PyObject.GetAttr(next, "__globals__"));
    }

    [Fact]
    public void MissingBuiltinsInheritsCallerWithoutInsertingAKeyAndMissingModuleIsNone()
    {
        object? saved = null;
        var module = Module(context => saved = context);
        var builtins = new PyDict(); builtins.Set("answer", 17L);
        module.SetAttr("__builtins__", builtins); PyModuleInstances.Execute(module);
        Dict(module).Delete("__builtins__"); Dict(module).Delete("__name__");
        var function = Function(saved!);
        Assert.Equal(17L, Invoke(function));
        Assert.Same(builtins, PyObject.GetAttr(function, "__builtins__"));
        Assert.False(Dict(module).TryGet("__builtins__", out _));
        Assert.Same(PyNone.Instance, PyModuleInstances.DefinitionModule(saved!));
        builtins.Set("__name__", "not a module source");
        Assert.Same(PyNone.Instance, PyModuleInstances.DefinitionModule(saved!));
        var marker = new PyList([]); module.SetAttr("__name__", marker);
        Assert.Same(marker, PyModuleInstances.DefinitionModule(saved!));
    }

    [Fact]
    public void SameBuiltinsReusesImmutableContextAndRebindingCreatesANewCapture()
    {
        object? outer = null; var module = Module(context => outer = context);
        var builtins = new PyDict(); builtins.Set("answer", 31L);
        module.SetAttr("__builtins__", builtins); PyModuleInstances.Execute(module);
        Assert.Same(outer, PyModuleInstances.CaptureDefinitionContext(outer!));
        builtins.Set("answer", 32L);
        Assert.Same(outer, PyModuleInstances.CaptureDefinitionContext(outer!));
        Assert.Equal(32L, PyModuleInstances.LoadName(outer!, "answer"));
        Dict(module).Delete("__builtins__");
        Assert.Same(outer, PyModuleInstances.CaptureDefinitionContext(outer!));
        var different = new PyDict(); different.Set("answer", 33L);
        module.SetAttr("__builtins__", different);
        object fresh = PyModuleInstances.CaptureDefinitionContext(outer!);
        Assert.NotSame(outer, fresh);
        Assert.Equal(32L, PyModuleInstances.LoadName(outer!, "answer"));
        Assert.Equal(33L, PyModuleInstances.LoadName(fresh, "answer"));
    }

    [Fact]
    public void NonDictionaryBuiltinsRemainAnExplicitEarlyUnsupportedBoundary()
    {
        object? saved = null; var module = Module(context => saved = context);
        PyModuleInstances.Execute(module);
        foreach (object value in new object[] { PyNone.Instance, 1L, new PyList([]), Module(static _ => { }) })
        {
            module.SetAttr("__builtins__", value);
            Assert.Throws<PyNotImplementedError>(() => PyModuleInstances.CaptureDefinitionContext(saved!));
            Assert.Same(value, Dict(module).Get("__builtins__"));
        }
    }

    [Fact]
    public void BoundMethodsKeepFunctionOwnerWhenInstalledOnOtherClasses()
    {
        PyFunction? function = null; var module = Module(context => function = Function(context));
        module.SetAttr("answer", 19L); PyModuleInstances.Execute(module);
        var cls = new PyClass("Other");
        var bound = new PyBoundMethod(cls, PyNone.Instance, function);
        Assert.Equal(19L, bound.Invoke([], [], []));
        Assert.Same(function, PyObject.GetAttr(bound, "__func__"));
    }

    [Fact]
    public void GeneratorFrameRetainsCapturedOwnerAcrossSuspensionAndClearsItOnFinish()
    {
        PyFunction? function = null; object?[]? frame = null;
        var module = Module(context => function = Function(context,
            (owner, self, args, names, values) =>
            {
                frame = [0L, PyNone.Instance, null, null, owner];
                return new PyGenerator(state =>
                {
                    var slots = (object?[])state;
                    if ((long)slots[0]! == 0) { slots[0] = 1L; return PyModuleInstances.LoadName(slots[4]!, "answer"); }
                    slots[0] = -1L; return PyNone.Instance;
                }, frame, "owner.py", "f", self);
            }));
        var first = new PyDict(); first.Set("answer", 23L);
        module.SetAttr("__builtins__", first); PyModuleInstances.Execute(module);
        var generator = Assert.IsType<PyGenerator>(Invoke(function!));
        module.SetAttr("__builtins__", new PyDict());
        Assert.Equal(23L, generator.Next(null, false));
        Assert.Throws<PyStopIteration>(() => generator.Next(null, false));
        Assert.All(frame!, value => Assert.Null(value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] MakeUnrootedOwnerCycle()
    {
        // 静态注册代码不能捕获本次module/function，否则测试会主动制造永久强根。
        var module = Module(static context => PyModuleInstances.StoreName(context, "f", Function(context)));
        PyModuleInstances.Execute(module);
        var function = Assert.IsType<PyFunction>(module.GetAttr("f"));
        return [new WeakReference(module), new WeakReference(Dict(module)), new WeakReference(function)];
    }

    [Fact]
    public void RegisteredCodeDoesNotRootInstanceFunctionCycles()
    {
        var weak = MakeUnrootedOwnerCycle();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.All(weak, reference => Assert.False(reference.IsAlive));
    }

    [Fact]
    public void AbiAndExistingFunctionLayoutRemainExact()
    {
        Assert.Equal(new[] { typeof(object), typeof(PyFunction), typeof(object[]), typeof(string[]), typeof(object[]) },
            typeof(PyOwnedCall).GetMethod("Invoke")!.GetParameters().Select(p => p.ParameterType));
        Assert.Equal(typeof(object), typeof(PyOwnedCall).GetMethod("Invoke")!.ReturnType);
        var create = typeof(PyModuleInstances).GetMethod("CreateOwnedFunction")!;
        Assert.Equal(typeof(PyFunction), create.ReturnType);
        Assert.Equal(new[] { typeof(object), typeof(string), typeof(PySignature), typeof(PyOwnedCall),
            typeof(object[]), typeof(bool), typeof(object[]) }, create.GetParameters().Select(p => p.ParameterType));
        string[] expected = ["<Name>k__BackingField", "<Signature>k__BackingField", "<Body>k__BackingField",
            "<Defaults>k__BackingField", "<UsesSuper>k__BackingField", "<Env>k__BackingField",
            "_doc", "_annotations", "_attributes", "_module"];
        Assert.Equal(expected.Order(), typeof(PyFunction).GetFields(BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic).Select(f => f.Name).Order());
    }
}
