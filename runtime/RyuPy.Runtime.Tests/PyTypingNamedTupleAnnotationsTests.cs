// B0仅验冷注解转换与实际全局消费守卫；不把内部方法测试当NamedTuple公开或项目通过。
// 反射只用于测试进入internal，不修改A测试、不为生产增加反射或假typing特殊值。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingNamedTupleAnnotationsTests
{
    private static readonly Type Implementation = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyTypingNamedTupleAnnotations")!;
    private static Dictionary<string, object?> Attributes => PyTyping.CreateAttributes();
    private static object Normalize(object? value, string field = "value")
    {
        try
        {
            return Implementation.GetMethod("NormalizeAnnotation", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [value, field])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
    private static void With(string name, object? value, Action body)
    {
        bool existed = Attributes.TryGetValue(name, out object? old);
        Attributes[name] = value;
        try { body(); }
        finally { if (existed) Attributes[name] = old; else Attributes.Remove(name); }
    }

    [Fact]
    public void NoneAndStringsNormalizeWhileExistingForwardRefsKeepIdentity()
    {
        Assert.Same(PyType.Get("NoneType"), Normalize(null));
        Assert.Same(PyType.Get("NoneType"), Normalize(PyNone.Instance));
        object first = Normalize("Missing"), second = Normalize("Missing");
        Assert.NotSame(first, second);
        Assert.True(PyHash.Equal(first, second));
        Assert.Same(first, Normalize(first));
        Assert.Equal("Missing", PyObject.GetAttr(first, "__forward_arg__"));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(first, "__forward_module__"));
    }

    [Fact]
    public void RealClassesAndNativeTypesKeepIdentityWithoutInstanceEqualityCalls()
    {
        var cls = new PyClass("Ordinary");
        int calls = 0;
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => { calls++; return true; }));
        Assert.Same(cls, Normalize(cls));
        Assert.Equal(0, calls);
        Assert.Same(PyType.Get("int"), Normalize(PyType.Get("int")));
        object nativeType = PyObject.TypeOf(Normalize("Missing"));
        Assert.IsType<PyNativeType>(nativeType);
        Assert.Same(nativeType, Normalize(nativeType));
    }

    [Fact]
    public void LegitimateFormsAndAliasesAreNotCollapsedOrReinterned()
    {
        foreach (string name in new[] { "Any", "List", "Dict", "Tuple", "Set", "FrozenSet", "Callable",
            "Iterable", "Iterator", "Sequence", "Mapping", "MutableMapping", "Collection", "Container" })
            Assert.Same(Attributes[name], Normalize(Attributes[name]));
        object? list = PyOps.GetItem(Attributes["List"], PyType.Get("int"));
        object? union = PyOps.GetItem(Attributes["Union"], new PyTuple([PyType.Get("int"), PyType.Get("str")]));
        object? literal = PyOps.GetItem(Attributes["Literal"], new PyTuple([1L, "x"]));
        foreach (object? alias in new[] { list, union, literal }) Assert.Same(alias, Normalize(alias));
        Assert.Throws<PyNotImplementedError>(() => Normalize(new PyTypingForm("Any")));
        Assert.Throws<PyNotImplementedError>(() => Normalize(new PyTypingForm("Unknown")));
    }

    [Fact]
    public void BareSpecialFormsUseTheirRealTypeErrors()
    {
        foreach (string name in new[] { "Optional", "Union", "Literal" })
            Assert.Equal($"Plain typing.{name} is not valid as type argument",
                Assert.Throws<PyTypeError>(() => Normalize(Attributes[name])).PyMessage);
    }

    [Fact]
    public void ScalarAnnotationsRemainActualValuesRatherThanBeingRequiredTypes()
    {
        foreach (object value in new object[] { true, false, 1L, 2, BigInteger.Parse("123456789012345678901234567890"), 3.25, double.NaN })
            Assert.Same(value, Normalize(value));
    }

    [Fact]
    public void UnknownInstancesStrSubclassesAndTupleErrorsDoNotRunApproximateProtocols()
    {
        var unknown = new ComparisonProbe();
        Assert.Throws<PyNotImplementedError>(() => Normalize(unknown));
        Assert.Equal(0, unknown.Lookups);
        Assert.Throws<PyNotImplementedError>(() => Normalize(new PyTuple([unknown])));
        Assert.Equal(0, unknown.Lookups);
        Assert.Throws<PyNotImplementedError>(() => Normalize(new PyList()));
        var cls = (PyClass)PyOps.MakeClass("AnnotationString", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object value = cls.Invoke(["Missing"], [], [])!;
        Assert.Throws<PyNotImplementedError>(() => Normalize(value));
    }

    [Fact]
    public void AliasCrossKindComparisonDoesNotWalkArgumentsButRejectsUnsafeOrigin()
    {
        var probe = new ComparisonProbe();
        var safe = new PyTypingAlias("List", PyType.Get("list"), [probe]);
        Assert.Same(safe, Normalize(safe));
        Assert.Equal(0, probe.Lookups);
        Assert.Throws<PyNotImplementedError>(() => Normalize(new PyTypingAlias("List", probe, [PyType.Get("int")])));
        Assert.Equal(0, probe.Lookups);
    }

    [Fact]
    public void RebindingAnyThroughTheActualModuleIsDetectedAndRestoringWorks()
    {
        // RegisterNative的setter更新同一字典；原identity不能从本次已改绑公开值初始化。
        PyModules.RegisterNative("typing", Attributes);
        PyModule module = PyModules.Import("typing");
        object? original = Attributes["Any"];
        var replacement = new ComparisonProbe();
        try
        {
            module.SetAttr("Any", replacement);
            Assert.Contains("typing.Any", Assert.Throws<PyNotImplementedError>(() => Normalize(PyType.Get("int"))).PyMessage);
            Assert.Throws<PyNotImplementedError>(() => Normalize(replacement));
            Assert.Equal(0, replacement.Lookups);
        }
        finally { module.SetAttr("Any", original); }
        Assert.Same(original, Normalize(original));
        Assert.Same(PyType.Get("int"), Normalize(PyType.Get("int")));
    }

    [Fact]
    public void AddedConsumedGlobalsRejectButUnrelatedModuleMetadataDoesNot()
    {
        foreach (string name in new[] { "_type_check", "_type_convert", "Generic", "Protocol", "Final",
            "isinstance", "str", "_GenericAlias", "LiteralString", "Never", "Self", "TypeAlias", "_SpecialForm", "type" })
            With(name, PyNone.Instance, () =>
                Assert.Contains("typing." + name, Assert.Throws<PyNotImplementedError>(() => Normalize(PyType.Get("int"))).PyMessage));
        With("__name__", "renamed", () => With("unrelated", new object(), () =>
            Assert.Same(PyType.Get("int"), Normalize(PyType.Get("int")))));
        With("ForwardRef", PyNone.Instance, () =>
        {
            Assert.Same(PyType.Get("int"), Normalize(PyType.Get("int")));
            Assert.Contains("typing.ForwardRef", Assert.Throws<PyNotImplementedError>(() => Normalize("Missing")).PyMessage);
        });
    }

    // C1仅迁移ClassVar读取相位；上面的其它helper NI合同逐项保留。
    [Fact]
    public void ClassVarNoneExportAllowsOrdinaryColdNamedTupleAnnotation()
    {
        With("ClassVar", PyNone.Instance, () =>
            Assert.Same(PyType.Get("int"), Normalize(PyType.Get("int"))));
    }

    [Fact]
    public void GuardsFollowTheActualConversionAndEarlyReturnBranches()
    {
        With("type", PyNone.Instance, () =>
        {
            Assert.Throws<PyNotImplementedError>(() => Normalize(PyNone.Instance));
            Assert.Same(Attributes["Any"], Normalize(Attributes["Any"]));
            Assert.Throws<PyTypeError>(() => Normalize(Attributes["Union"]));
            Assert.Throws<PyNotImplementedError>(() => Normalize(PyType.Get("int")));
        });
        With("_SpecialForm", PyNone.Instance, () =>
        {
            Assert.Same(Attributes["Any"], Normalize(Attributes["Any"]));
            Assert.Throws<PyNotImplementedError>(() => Normalize(PyType.Get("int")));
        });
        With("str", PyNone.Instance, () => Assert.Same(PyType.Get("NoneType"), Normalize(PyNone.Instance)));
        With("Any", PyNone.Instance, () =>
            Assert.Contains("ASCII type-reference syntax", Assert.Throws<PyNotImplementedError>(() => Normalize("")).PyMessage));
        With("Generic", PyNone.Instance, () =>
            Assert.Contains("typing.Generic", Assert.Throws<PyNotImplementedError>(() => Normalize("")).PyMessage));
    }

    [Fact]
    public void RejectedLaterFieldDoesNotRebuildOrInvalidateEarlierConvertedValue()
    {
        object first = Normalize("Missing", "first");
        Assert.Contains("second", Assert.Throws<PyNotImplementedError>(() => Normalize(new ComparisonProbe(), "second")).PyMessage);
        Assert.Same(first, Normalize(first, "first"));
        Assert.Equal("Missing", PyObject.GetAttr(first, "__forward_arg__"));
        Assert.IsType<PyFunction>(Attributes["NamedTuple"]);
        Assert.False(Attributes.ContainsKey("ForwardRef"));
    }

    [Fact]
    public void OwnAndForwardRefErrorsRetainCallerAndAmbientContext()
    {
        var ambient = new PyValueError("ambient");
        object? previous = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.PushFrame("annotation_probe.py", "caller");
        PyTraceback.SetLine(42);
        try
        {
            foreach (object? value in new object?[] { Attributes["Union"], new ComparisonProbe(), "f()", new PyTuple([]) })
            {
                PyBaseException error = Assert.ThrowsAny<PyBaseException>(() => Normalize(value));
                Assert.Same(ambient, error.Context);
                Assert.Equal(new PyStackFrame("annotation_probe.py", "caller", 42), error.Frames[0]);
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }

    private sealed class ComparisonProbe : PyNativeObject
    {
        internal int Lookups;
        public override string TypeName => "ComparisonProbe";
        public override object? GetAttr(string name) { Lookups++; throw new PyValueError("unexpected user protocol"); }
        public override string PythonRepr() { Lookups++; throw new PyValueError("unexpected repr"); }
    }
}
