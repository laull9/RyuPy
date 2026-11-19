// delattr 的公开直调和保存函数入口：绑定、删除身份、失败原状态与异常帧。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyDelAttrTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static object? Delete(bool saved, object?[] args, object?[]? names = null, object?[]? values = null) =>
        saved ? PyOps.CallValue(PyOps.BuiltinFunction("delattr"), args, names ?? [], values ?? [])
            : PyOps.CallBuiltin("delattr", args, names ?? [], values ?? []);
    private static PyInstance Instance() => new(new PyClass("Box"));
    private static PyFunction Fn(string name, Func<object?[], object?> action) => PyNativeFunctions.CreateVariadic(name, action);

    [Fact]
    public void SavedBuiltinHasStableRealFunctionIdentity()
    {
        object saved = PyOps.BuiltinFunction("delattr");
        Assert.IsType<PyBuiltinFunction>(saved);
        Assert.Same(saved, PyOps.BuiltinFunction("delattr"));
        Assert.Equal("delattr", PyObject.GetAttr(saved, "__name__"));
        Assert.Equal(true, PyOps.CallBuiltin("callable", [saved], [], []));
    }

    [Fact]
    public void BothRoutesDeleteTheSameInstanceAndReturnNone()
    {
        foreach (bool saved in new[] { false, true })
        {
            var value = Instance(); var alias = value; var retained = new PyList();
            PyObject.SetAttr(value, "item", retained); PyObject.SetAttr(value, "keep", retained);
            Assert.Same(PyNone.Instance, Delete(saved, [value, "item"]));
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(alias, "item"));
            Assert.Same(retained, PyObject.GetAttr(alias, "keep"));
        }
    }

    [Fact]
    public void ExactArityIsCheckedBeforeDeletion()
    {
        foreach (bool saved in new[] { false, true })
        foreach (int count in new[] { 0, 1, 3, 4 })
        {
            var value = Instance(); PyObject.SetAttr(value, "item", 7L);
            object?[] args = count switch { 0 => [], 1 => [value], 3 => [value, "item", 1L], _ => [value, "item", 1L, 2L] };
            Assert.Equal($"delattr expected 2 arguments, got {count}", Assert.Throws<PyTypeError>(() => Delete(saved, args)).PyMessage);
            Assert.Equal(7L, PyObject.GetAttr(value, "item"));
        }
    }

    [Fact]
    public void KeywordsWinBeforeArityAndDoNotMutate()
    {
        foreach (bool saved in new[] { false, true })
        foreach (object?[] args in new object?[][] { [], [1L], [1L, 2L], [1L, 2L, 3L] })
            Assert.Equal("delattr() takes no keyword arguments", Assert.Throws<PyTypeError>(() => Delete(saved, args, ["name"], ["x"])).PyMessage);
    }

    [Fact]
    public void NonStringNamesFailWithoutCallingConversion()
    {
        int calls = 0; var cls = new PyClass("Name");
        cls.SetAttr("__str__", Fn("__str__", _ => { calls++; return "item"; }));
        foreach (bool saved in new[] { false, true })
        foreach (object? name in new object?[] { null, 1L, false, new PyInstance(cls) })
        {
            var value = Instance(); PyObject.SetAttr(value, "item", 7L);
            Assert.Equal("attribute name must be string, not '" + PyRepr.TypeName(name) + "'", Assert.Throws<PyTypeError>(() => Delete(saved, [value, name])).PyMessage);
            Assert.Equal(7L, PyObject.GetAttr(value, "item"));
        }
        Assert.Equal(0, calls);
    }

    [Fact]
    public void EmptyAndNonIdentifierStringsAreRealAttributeNames()
    {
        foreach (bool saved in new[] { false, true })
        foreach (string name in new[] { "", "not-an-identifier", "中文", "a\0b" })
        {
            var value = Instance(); PyObject.SetAttr(value, name, 1L);
            Assert.Same(PyNone.Instance, Delete(saved, [value, name]));
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, name));
        }
        var nameClass = (PyClass)PyOps.MakeClass("AttributeName", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(nameClass);
        object? subclassName = PyOps.CallValue(nameClass, ["item"], [], []);
        foreach (bool saved in new[] { false, true })
        {
            var value = Instance(); PyObject.SetAttr(value, "item", 7L);
            Assert.Same(PyNone.Instance, Delete(saved, [value, subclassName]));
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, "item"));
        }
    }

    [Fact]
    public void MissingAndRepeatedDeletesPreserveOtherAttributes()
    {
        foreach (bool saved in new[] { false, true })
        {
            var value = Instance(); var keep = new PyList(); PyObject.SetAttr(value, "keep", keep);
            Assert.Equal("'Box' object has no attribute 'missing'", Assert.Throws<PyAttributeError>(() => Delete(saved, [value, "missing"])).PyMessage);
            Assert.Same(keep, PyObject.GetAttr(value, "keep"));
            Delete(saved, [value, "keep"]); Assert.Throws<PyAttributeError>(() => Delete(saved, [value, "keep"]));
        }
    }

    [Fact]
    public void ClassDeletionIsVisibleThroughInstanceAndClassAliases()
    {
        foreach (bool saved in new[] { false, true })
        {
            var cls = new PyClass("Owner"); var alias = cls; var value = new PyInstance(cls);
            cls.SetAttr("item", 9L); Assert.Equal(9L, PyObject.GetAttr(value, "item"));
            Assert.Same(PyNone.Instance, Delete(saved, [alias, "item"]));
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(cls, "item"));
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, "item"));
        }
    }

    [Fact]
    public void ClassOnlyAttributeCannotBeDeletedFromInstance()
    {
        var cls = new PyClass("Owner"); cls.SetAttr("item", 9L); var value = new PyInstance(cls);
        Assert.Throws<PyAttributeError>(() => Delete(true, [value, "item"]));
        Assert.Equal(9L, PyObject.GetAttr(value, "item")); Assert.Equal(9L, PyObject.GetAttr(cls, "item"));
    }

    [Fact]
    public void PropertyDeleterReceivesIdentityAndItsReturnIsDiscarded()
    {
        var cls = new PyClass("Owner"); var value = new PyInstance(cls); int calls = 0;
        cls.SetAttr("item", new PyProperty(null, null, Fn("delete", args => { Assert.Same(value, args[0]); calls++; return 99L; })));
        Assert.Same(PyNone.Instance, Delete(false, [value, "item"]));
        Assert.Same(PyNone.Instance, Delete(true, [value, "item"])); Assert.Equal(2, calls);
    }

    [Fact]
    public void ReadOnlyPropertyDoesNotInvokeGetterOrLoseDescriptor()
    {
        int calls = 0; var cls = new PyClass("Owner"); var property = new PyProperty(Fn("get", _ => { calls++; return 1L; }));
        cls.SetAttr("item", property); var value = new PyInstance(cls);
        Assert.Equal("property 'item' of 'Owner' object has no deleter", Assert.Throws<PyAttributeError>(() => Delete(true, [value, "item"])).PyMessage);
        Assert.Same(property, PyObject.GetAttr(cls, "item")); Assert.Equal(0, calls);
    }

    [Fact]
    public void DeleterFailureKeepsExceptionIdentityAndEarlierEffects()
    {
        var marker = new PyLookupError("delete marker"); var cls = new PyClass("Owner"); var value = new PyInstance(cls);
        var events = new List<string>(); PyObject.SetAttr(value, "keep", 7L);
        cls.SetAttr("item", new PyProperty(null, null, Fn("delete", _ => { events.Add("delete"); throw marker; })));
        Assert.Same(marker, Assert.Throws<PyLookupError>(() => Delete(true, [value, "item"])));
        Assert.Equal(new[] { "delete" }, events); Assert.Equal(7L, PyObject.GetAttr(value, "keep"));
    }

    [Fact]
    public void FunctionDictionaryDeletionKeepsAliasesAndOtherFields()
    {
        var function = Fn("probe", _ => null); var alias = function; var keep = new PyList();
        PyObject.SetAttr(function, "item", keep); PyObject.SetAttr(function, "keep", keep);
        Assert.Same(PyNone.Instance, Delete(true, [function, "item"]));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(alias, "item")); Assert.Same(keep, PyObject.GetAttr(alias, "keep"));
        Assert.Throws<PyAttributeError>(() => Delete(false, [function, "item"]));
    }

    [Fact]
    public void FunctionModuleResetDoesNotDeleteDictionaryShadow()
    {
        var function = Fn("probe", _ => null); PyObject.SetAttr(function, "__module__", "owner");
        var dictionary = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__")); dictionary.Set("__module__", "shadow");
        Assert.Same(PyNone.Instance, Delete(false, [function, "__module__"]));
        Assert.Same(PyNone.Instance, Delete(true, [function, "__module__"]));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(function, "__module__")); Assert.Equal("shadow", dictionary.Get("__module__"));
    }

    [Fact]
    public void ReadOnlySliceAndFunctionFailuresLeaveStateIntact()
    {
        var slice = new PySlice(1L, 9L, 2L); var function = Fn("probe", _ => null);
        foreach (bool saved in new[] { false, true })
        {
            foreach (string name in new[] { "start", "stop", "step" }) Assert.Throws<PyAttributeError>(() => Delete(saved, [slice, name]));
            Assert.Throws<PyTypeError>(() => Delete(saved, [function, "__name__"]));
            Assert.Throws<PyTypeError>(() => Delete(saved, [function, "__dict__"]));
        }
        Assert.Equal("cannot set 'x' attribute of immutable type 'int'",
            Assert.Throws<PyTypeError>(() => Delete(true, [PyType.Get("int"), "x"])).PyMessage);
        Assert.Equal(1L, slice.Start); Assert.Equal(9L, slice.Stop); Assert.Equal(2L, slice.Step);
        Assert.Equal("probe", PyObject.GetAttr(function, "__name__"));
    }

    [Fact]
    public void BindingNameAndDeletionErrorsCaptureContextAndUserFrame()
    {
        PyTraceback.Reset(); var ambient = new PyRuntimeError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("delattr_public.py", "caller"); PyTraceback.SetLine(23);
            foreach (bool saved in new[] { false, true })
            {
                PyBaseException[] errors = [Assert.Throws<PyTypeError>(() => Delete(saved, [])),
                    Assert.Throws<PyTypeError>(() => Delete(saved, [Instance(), 1L])),
                    Assert.Throws<PyAttributeError>(() => Delete(saved, [Instance(), "missing"]))];
                foreach (var error in errors) { Assert.Same(ambient, error.Context); Assert.Equal("caller", Assert.Single(error.Frames).Function); Assert.Equal(23, error.Frames[0].Line); }
            }
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }
    [Fact]
    public void InstanceNamedHookIsIgnoredButClassHookIsExplicitlyUnsupported()
    {
        int calls = 0; var value = Instance();
        var hook = Fn("hook", _ => { calls++; return null; });
        PyObject.SetAttr(value, "__delattr__", hook); PyObject.SetAttr(value, "item", 7L);
        Assert.Same(PyNone.Instance, Delete(true, [value, "item"])); Assert.Equal(0, calls);
        value.Class.SetAttr("__delattr__", hook); PyObject.SetAttr(value, "item", 9L);
        Assert.Throws<PyNotImplementedError>(() => Delete(false, [value, "item"]));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "item"));
        Assert.Equal(9L, PyObject.GetAttr(value, "item")); Assert.Equal(0, calls);
    }

    [Fact]
    public void GeneralDescriptorsAndUnknownPropertyDeletersFailBeforeEffects()
    {
        int calls = 0;
        var descriptorClass = new PyClass("Descriptor");
        foreach (string slot in new[] { "__delete__", "__set__" })
            descriptorClass.SetAttr(slot, Fn(slot, _ => { calls++; return null; }));
        var owner = new PyClass("Owner"); var value = new PyInstance(owner);
        PyObject.SetAttr(value, "keep", 7L);
        var descriptor = new PyInstance(descriptorClass); owner.SetAttr("item", descriptor);
        Assert.Throws<PyNotImplementedError>(() => Delete(true, [value, "item"]));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "item"));
        owner.SetAttr("item", new PyProperty(null, null, 7L));
        Assert.Throws<PyNotImplementedError>(() => Delete(false, [value, "item"]));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "item"));
        Assert.Equal(7L, PyObject.GetAttr(value, "keep")); Assert.Equal(0, calls);
    }

    [Fact]
    public void FrozenDictionaryDeletionKeepsOriginalGuardPriority()
    {
        var cls = new PyClass("Frozen"); PyOps.DeclareDataclass(cls, ["item"], true); PyOps.ApplyDataclass(cls, true);
        object? value = PyOps.CallValue(cls, [7L], [], []);
        Assert.Throws<PyFrozenInstanceError>(() => Delete(true, [value, "__dict__"]));
        Assert.Throws<PyFrozenInstanceError>(() => PyObject.DelAttr(value, "__dict__"));
        Assert.Equal(7L, PyObject.GetAttr(value, "item"));
    }

    [Fact]
    public void ReservedNamesStillRunOrdinaryPropertyDeleters()
    {
        foreach (string name in new[] { "__dict__", "__class__" })
        {
            var cls = new PyClass("Reserved"); var value = new PyInstance(cls); int calls = 0;
            var property = new PyProperty(null, null, Fn("delete", args => { Assert.Same(value, args[0]); calls++; return 99L; }));
            cls.SetAttr(name, property);
            Assert.Same(PyNone.Instance, Delete(false, [value, name])); PyObject.DelAttr(value, name);
            Assert.Equal(2, calls); Assert.Same(property, cls.Attributes[name]);
        }
    }

    [Fact]
    public void NativeNonDataDescriptorAllowsShadowDeletionButTupleFieldDoesNot()
    {
        object? nonData = PyHtml.CreateAttributes()["escape"];
        Assert.IsAssignableFrom<IPyNativeDescriptor>(nonData);
        var cls = new PyClass("Owner"); var value = new PyInstance(cls); cls.SetAttr("item", nonData);
        PyObject.SetAttr(value, "item", 7L); Assert.Equal(7L, PyObject.GetAttr(value, "item"));
        Assert.Same(PyNone.Instance, Delete(true, [value, "item"]));
        PyObject.SetAttr(value, "item", 8L); PyObject.DelAttr(value, "item");
        Assert.Same(nonData, PyObject.GetAttr(cls, "item"));
        object? namedTuple = PyCollections.CreateAttributes()["namedtuple"];
        object? tupleClass = PyOps.CallValue(namedTuple, ["Point", "x"], ["module"], ["test"]);
        object? field = PyObject.GetAttr(tupleClass, "x"); Assert.IsAssignableFrom<IPyNativeDataDescriptor>(field);
        PyObject.SetAttr(value, "item", 9L); cls.SetAttr("item", field);
        Assert.Throws<PyNotImplementedError>(() => Delete(true, [value, "item"]));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "item"));
        cls.SetAttr("item", nonData); Assert.Equal(9L, PyObject.GetAttr(value, "item"));
    }

    [Fact]
    public void ExceptionDataDescriptorKeepsItsOwnAttributeOnFailure()
    {
        int calls = 0; var descriptorClass = new PyClass("Descriptor");
        descriptorClass.SetAttr("__delete__", Fn("delete", _ => { calls++; return null; }));
        var descriptor = new PyRuntimeError("descriptor"); descriptor.BindPythonClass(descriptorClass);
        var owner = new PyClass("ErrorOwner"); var value = new PyRuntimeError("value"); value.BindPythonClass(owner);
        PyObject.SetAttr(value, "item", 7L); owner.SetAttr("item", descriptor);
        Assert.Throws<PyNotImplementedError>(() => Delete(false, [value, "item"]));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "item"));
        owner.DeleteAttr("item"); Assert.Equal(7L, PyObject.GetAttr(value, "item")); Assert.Equal(0, calls);
    }

    [Fact]
    public void WarmPublicRoutesMeasureAllocationsWithoutCallerArrays()
    {
        var value = Instance(); object item = 7L; object?[] args = [value, "item"]; object?[] empty = [];
        object saved = PyOps.BuiltinFunction("delattr");
        long directAllocated = 0, savedAllocated = 0;
        foreach (bool useSaved in new[] { false, true })
        {
            for (int i = 0; i < 1000; i++)
            {
                PyObject.SetAttr(value, "item", item);
                if (useSaved) PyOps.CallValue(saved, args, empty, empty); else PyOps.CallBuiltin("delattr", args, empty, empty);
            }
            long before = GC.GetAllocatedBytesForCurrentThread(); bool allNone = true;
            for (int i = 0; i < 1000; i++)
            {
                PyObject.SetAttr(value, "item", item);
                object? result = useSaved ? PyOps.CallValue(saved, args, empty, empty) : PyOps.CallBuiltin("delattr", args, empty, empty);
                allNone &= ReferenceEquals(PyNone.Instance, result);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (useSaved) savedAllocated = allocated; else directAllocated = allocated;
            output.WriteLine($"delattr warm {(useSaved ? "saved" : "direct")}: 1000 set+delete, {allocated} bytes");
            Assert.True(allNone); Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, "item"));
        }
        // 对照同一已预热对象的共享写删入口，不把公开调用器成本归给删除守卫。
        for (int i = 0; i < 1000; i++) { PyObject.SetAttr(value, "item", item); PyObject.DelAttr(value, "item"); }
        long controlBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) { PyObject.SetAttr(value, "item", item); PyObject.DelAttr(value, "item"); }
        long controlAllocated = GC.GetAllocatedBytesForCurrentThread() - controlBefore;
        output.WriteLine($"delattr warm shared control: 1000 set+delete, {controlAllocated} bytes");
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(value, "item"));
        // 全部测量结束才创建证据目录和JSON，不让留证分配污染任一路线。
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"ryupy-delattr-allocation-{Environment.ProcessId}-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(root);
        string resultPath = System.IO.Path.Combine(root, "results.json");
        System.IO.File.WriteAllText(resultPath, System.Text.Json.JsonSerializer.Serialize(new
        {
            warmup_calls_per_route = 1000,
            measured_calls_per_route = 1000,
            operation = "每次先SetAttr再delete；公开路线还验证返回None，全部路线验证属性已删除",
            reused_inputs = "同一目标实例、属性名item、预装箱值7、两实参数组、空关键字数组及保存的builtin；初始化、输出、断言和留证均在测量外",
            allocated_bytes = new { direct = directAllocated, saved = savedAllocated, shared_control = controlAllocated },
            allocation_threshold = "无阈值；不把组合路线分配归因于单个删除步骤"
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        output.WriteLine($"delattr allocation evidence: {resultPath}");
    }

}
