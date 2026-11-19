// TypeVar 公开入口合同：固定 CP 应用先冻结；本文件不依赖生产类内部表示。
using System;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingTypeVarTests : IDisposable
{
    private readonly long savedDepth;
    public PyTypingTypeVarTests()
    {
        PyNativeModules.Register();
        savedDepth = PyTraceback.CaptureDepth();
        PyTraceback.PushFrameWithOrigin("typevar_foundation.py", "<module>",
            PyTraceback.CreateModuleExecutionSource("__main__"));
    }
    public void Dispose() => PyTraceback.PopTo(savedDepth + 1);
    private static object? Form(string name) => PyTyping.CreateAttributes()[name];
    private static object? Make(object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(Form("TypeVar"), args, names ?? [], values ?? []);
    private static object? Get(object? value, string name) => PyObject.GetAttr(value, name);
    private static object? Alias(string name, params object?[] args) =>
        PyOps.GetItem(Form(name), args.Length == 1 ? args[0] : new PyTuple(args));

    [Fact]
    public void RealTypeAndFreshIdentitySurviveEqualNamesAndMetadataWrites()
    {
        object? first = Make(["T"]), second = Make(["T"]);
        Assert.IsType<PyNativeType>(Form("TypeVar"));
        Assert.Same(Form("TypeVar"), PyObject.TypeOf(first));
        Assert.Same(Form("TypeVar"), Get(first, "__class__"));
        Assert.NotSame(first, second);
        Assert.True(PyHash.Equal(first, first));
        Assert.False(PyHash.Equal(first, second));
        long hash = PyHash.Of(first);
        Assert.Equal(hash, PyHash.Of(first));
        Assert.True(PyOps.Truthy(first));
        Assert.Equal("~T", PyRepr.Repr(first));
        var mutable = new PyList([]);
        PyObject.SetAttr(first, "__module__", mutable);
        PyObject.SetAttr(first, "extra", mutable);
        Assert.Same(mutable, Get(first, "__module__"));
        Assert.Same(mutable, Get(first, "extra"));
        Assert.Equal(hash, PyHash.Of(first));
        PyObject.DelAttr(first, "extra");
        Assert.Throws<PyAttributeError>(() => Get(first, "extra"));
        PyObject.DelAttr(first, "__module__");
        Assert.Equal("typing", Get(first, "__module__"));
        Assert.Throws<PyAttributeError>(() => Get(first, "__dict__"));
    }

    [Fact]
    public void ClassMetadataCannotBeReplacedOrDeleted()
    {
        object? value = Make(["T"]);
        foreach (object? replacement in new[] { Form("TypeVar"), PyType.Get("int") })
            Assert.Equal("__class__ assignment only supported for mutable types or ModuleType subclasses",
                Assert.Throws<PyTypeError>(() => PyObject.SetAttr(value, "__class__", replacement)).PyMessage);
        Assert.Equal("can't delete __class__ attribute",
            Assert.Throws<PyTypeError>(() => PyObject.DelAttr(value, "__class__")).PyMessage);
        Assert.Same(Form("TypeVar"), Get(value, "__class__"));
    }

    [Fact]
    public void CallerModuleReadsRealFunctionMetadataAndRejectsUnknownNativeOrigins()
    {
        long saved = PyTraceback.CaptureDepth();
        var function = PyNativeFunctions.CreateVariadic("helper", _ => PyNone.Instance);
        try
        {
            PyTraceback.PushFrameWithOrigin("helper.py", "helper", function);
            foreach (object value in new object[] { "other_module", PyNone.Instance, false, 0L, new PyList([]), new PyDict() })
            {
                PyObject.SetAttr(function, "__module__", value);
                Assert.Same(value, Get(Make(["T"]), "__module__"));
            }
            PyObject.DelAttr(function, "__module__");
            Assert.Same(PyNone.Instance, Get(Make(["T"]), "__module__"));
            PyTraceback.PushFrame("unknown.cs", "native");
            Assert.Throws<PyNotImplementedError>(() => Make(["Unknown"]));
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void ConstraintsKeepRawReferencesAndBoundKeepsSupportedValues()
    {
        var shared = new PyList([]);
        object? integer = PyType.Get("int");
        object? value = Make(["T", PyNone.Instance, "X", 7L, shared, integer, integer]);
        var constraints = Assert.IsType<PyTuple>(Get(value, "__constraints__"));
        Assert.Same(constraints, Get(value, "__constraints__"));
        Assert.Same(shared, constraints.Items[3]);
        Assert.Same(integer, constraints.Items[4]);
        Assert.Same(integer, constraints.Items[5]);
        foreach (object? bound in new object?[] { PyNone.Instance, integer, 1L, shared, value, Alias("List", value) })
            Assert.Same(bound, Get(Make(["B"], ["bound"], [bound]), "__bound__"));
        shared.Append(8L);
        Assert.Equal("[8]", PyRepr.Repr(constraints.Items[3]));
        object? forward = Get(Make(["F"], ["bound"], ["Missing"]), "__bound__");
        Assert.Equal("Missing", Get(forward, "__forward_arg__"));
        Assert.Same(forward, Get(Make(["F2"], ["bound"], [forward]), "__bound__"));
    }

    [Theory]
    [InlineData("__name__")]
    [InlineData("__bound__")]
    [InlineData("__constraints__")]
    [InlineData("__covariant__")]
    [InlineData("__contravariant__")]
    [InlineData("__infer_variance__")]
    public void CoreMetadataRejectsBothMutationPaths(string name)
    {
        object? value = Make(["T"]);
        object? before = Get(value, name);
        Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(value, name, PyNone.Instance));
        Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(value, name));
        Assert.Equal(before, Get(value, name));
    }

    [Fact]
    public void ConstructorErrorsAndVarianceConflictHaveNativeOrdering()
    {
        Assert.Throws<PyTypeError>(() => Make([]));
        Assert.Throws<PyTypeError>(() => Make([1L]));
        Assert.Throws<PyTypeError>(() => Make(["T"], ["name"], ["Other"]));
        Assert.Throws<PyTypeError>(() => Make(["T"], ["other"], [true]));
        Assert.Equal("'spam' is an invalid keyword argument for typevar()", Assert.Throws<PyTypeError>(
            () => Make([1L], ["spam"], [true])).PyMessage);
        Assert.Equal("typevar() missing required argument 'name' (pos 1)", Assert.Throws<PyTypeError>(
            () => Make([], ["spam"], [true])).PyMessage);
        Assert.Equal("'spam' is an invalid keyword argument for typevar()", Assert.Throws<PyTypeError>(
            () => Make(["T"], ["name", "spam"], ["U", true])).PyMessage);
        Assert.Equal("A single constraint is not allowed", Assert.Throws<PyTypeError>(
            () => Make(["T", PyType.Get("int")], ["bound"], [PyType.Get("int")])).PyMessage);
        Assert.Equal("Bivariant types are not supported.", Assert.Throws<PyValueError>(
            () => Make(["T"], ["bound", "covariant", "contravariant", "infer_variance"], ["[", true, true, true])).PyMessage);
        Assert.Equal("+T", PyRepr.Repr(Make(["T"], ["covariant"], [new PyList([1L])])));
        Assert.Equal("-T", PyRepr.Repr(Make(["T"], ["contravariant"], [true])));
        Assert.Equal("T", PyRepr.Repr(Make(["T"], ["infer_variance"], [true])));
    }

    [Fact]
    public void AliasParametersAreRecursiveOrderedAndIdentityBased()
    {
        object? first = Make(["T"]), second = Make(["T"]), other = Make(["U"]);
        object? nested = Alias("Dict", other, Alias("Tuple", Alias("List", first), other, second));
        var parameters = Assert.IsType<PyTuple>(Get(nested, "__parameters__"));
        Assert.Equal(3, parameters.Items.Length);
        Assert.Same(other, parameters.Items[0]);
        Assert.Same(first, parameters.Items[1]);
        Assert.Same(second, parameters.Items[2]);
        Assert.Same(first, Alias("Union", first, first));
        Assert.NotSame(Alias("List", first), Alias("List", second));
        Assert.Same(Alias("List", first), Alias("List", first));
        object? forward = Alias("Union", first, second), reverse = Alias("Union", second, first);
        Assert.True(PyHash.Equal(forward, reverse));
        Assert.Equal(PyHash.Of(forward), PyHash.Of(reverse));
        Assert.Empty(Assert.IsType<PyTuple>(Get(Alias("List", PyType.Get("int")), "__parameters__")).Items);
        Assert.Same(first, Assert.IsType<PyTuple>(Get(Alias("Literal", first), "__parameters__")).Items[0]);
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(Alias("List", first), PyType.Get("int")));
    }

    [Fact]
    public void UnsupportedUserBoundIsExplicitAndDoesNotInventComparisonResults()
    {
        int calls = 0;
        var cls = new PyClass("Bound");
        cls.SetAttr("__eq__", PyRuntimeDataclassTests.Fn("__eq__", ["self", "other"], _ => { calls++; return false; }));
        Assert.Throws<PyNotImplementedError>(() => Make(["T"], ["bound"], [new PyInstance(cls)]));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void IsInstanceInvalidArgumentUsesCommonMessageAndTupleShortCircuit()
    {
        // 固定CP探针覆盖普通int值/list/TypeVar，避免为新类型定制通用错误。
        const string message = "isinstance() arg 2 must be a type, a tuple of types, or a union";
        foreach (object? invalid in new object?[] { 1L, new PyList([]), Make(["T"]) })
        {
            Assert.Equal(message, Assert.Throws<PyTypeError>(
                () => PyOps.CallBuiltin("isinstance", [1L, invalid], [], [])).PyMessage);
            var candidates = new PyTuple([PyType.Get("int"), invalid]);
            Assert.Equal(true, PyOps.CallBuiltin("isinstance", [1L, candidates], [], []));
            Assert.Equal(message, Assert.Throws<PyTypeError>(
                () => PyOps.CallBuiltin("isinstance", ["text", candidates], [], [])).PyMessage);
        }
    }

    [Fact]
    public void InstanceSpecialFieldsAreReadableButNeverSupplyImplicitProtocols()
    {
        object? value = Make(["T"]), other = Make(["T"]);
        int calls = 0;
        var poison = PyNativeFunctions.CreateVariadic("poison", _ =>
        {
            calls++;
            throw new PyValueError("instance field called");
        });
        foreach (string name in new[] { "__hash__", "__eq__", "__getitem__", "__bool__", "__iter__", "__int__",
            "__trunc__", "__index__", "__length_hint__", "__call__", "__len__", "__float__", "__repr__", "__str__" })
        {
            PyObject.SetAttr(value, name, poison);
            Assert.Same(poison, Get(value, name));
            Assert.Throws<PyValueError>(() => PyOps.CallValue(Get(value, name), [], [], []));
        }
        Assert.Equal(14, calls);
        calls = 0;
        Assert.Equal(PyHash.Of(value), PyHash.Of(value));
        Assert.False(PyHash.Equal(value, other));
        Assert.True(PyOps.Truthy(value));
        Assert.Equal("~T", PyRepr.Repr(value));
        Assert.Equal("~T", PyRepr.Str(value));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(value, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.GetItem(value, PyType.Get("int")));
        foreach (string builtin in new[] { "iter", "int", "len", "float" })
            Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin(builtin, [value], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("isinstance", [1L, value], [], []));
        // 只测现有内部协议入口，不为了TypeVar测试新增operator模块或公开API。
        var hint = typeof(PyOps).GetMethod("ListExtendHint", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.Equal(8L, hint.Invoke(null, [value]));
        var indexOwner = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyItertools")!;
        var index = indexOwner.GetMethod("Index", BindingFlags.NonPublic | BindingFlags.Static)!;
        var wrapped = Assert.Throws<TargetInvocationException>(() => index.Invoke(null, [value]));
        Assert.IsType<PyTypeError>(wrapped.InnerException);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void PublicNativeConsumersRejectInstanceProtocolsWithoutChangingPrefixes()
    {
        // 独立review v2公开API合同：不复制临时harness，也不借内部表示绕过入口。
        object? variable = Make(["T"]);
        int calls = 0;
        var poison = PyNativeFunctions.CreateVariadic("poison", _ =>
        { calls++; throw new PyValueError("poison"); });
        foreach (string name in new[] { "__iter__", "__next__", "__getitem__", "__len__", "__length_hint__" })
        {
            PyObject.SetAttr(variable, name, poison);
            Assert.Same(poison, Get(variable, name));
        }
        const string unpack = "cannot unpack non-iterable typing.TypeVar object";
        const string iterable = "'typing.TypeVar' object is not iterable";
        Assert.Equal(unpack, Assert.Throws<PyTypeError>(() => PyOps.UnpackEx(variable, 1L, 0L)).PyMessage);
        Assert.Equal(unpack, Assert.Throws<PyTypeError>(() => PyOps.UnpackSequence(variable, 2L)).PyMessage);
        object collections = PyModules.Import("collections");
        object? factory = Get(collections, "namedtuple");
        object? pair = PyOps.CallValue(factory, ["Pair", "left right"], [], []);
        Assert.Equal(iterable, Assert.Throws<PyTypeError>(() =>
            PyOps.CallValue(Get(pair, "_make"), [variable], [], [])).PyMessage);
        Assert.Equal(iterable, Assert.Throws<PyTypeError>(() =>
            PyOps.CallValue(factory, ["NewPair", "left right"], ["defaults"], [variable])).PyMessage);
        var list = new PyList([1L]);
        Assert.Equal(iterable, Assert.Throws<PyTypeError>(() =>
            PyOps.CallValue(Get(list, "extend"), [variable], [], [])).PyMessage);
        Assert.Equal(0, calls);
        Assert.Equal(1, list.Count);
        Assert.Equal("[1]", PyRepr.Repr(list));
    }

}
