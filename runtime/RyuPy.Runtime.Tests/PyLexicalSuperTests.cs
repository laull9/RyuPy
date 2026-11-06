// 词法super的独立运行时合同：不以TLS伪造frame，并覆盖身份、惰性消费和冷能力边界。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyLexicalSuperTests
{
    private static PyClass Child(PyClass parent) => (PyClass)PyOps.MakeClass("Child", [parent], null);
    private static object Cell(object? value)
    {
        object cell = PyOps.CellNew();
        PyOps.CellSet(cell, value);
        return cell;
    }
    private static object? Super(object? cls, object? receiver, long kind = 1) =>
        PyOps.CallZeroWithFrame(1700, PyLexicalSuper.Type, new object?[] { Cell(cls) }, 0, receiver, kind);
    private static PyFunction Function(string name, PyCall body) =>
        new(name, PySignature.Create(name, [], [], []), body, []);

    [Fact]
    public void CanonicalTypeIdentityAndMinimalMetadataAgree()
    {
        var cls = new PyClass("C");
        var proxy = Assert.IsType<PySuper>(Super(cls, new PyInstance(cls)));
        Assert.Same(PyLexicalSuper.Type, PyOps.BuiltinFunction("super"));
        Assert.Same(PyOps.BuiltinFunction("super"), PyOps.BuiltinFunction("super"));
        Assert.Same(PyLexicalSuper.Type, PyObject.TypeOf(proxy));
        Assert.Same(PyLexicalSuper.Type, PyObject.GetAttr(proxy, "__class__"));
        Assert.Same(PyType.Get("type"), PyObject.TypeOf(PyLexicalSuper.Type));
        Assert.Equal("super", PyObject.GetAttr(PyLexicalSuper.Type, "__name__"));
        Assert.Equal("super", PyObject.GetAttr(PyLexicalSuper.Type, "__qualname__"));
        Assert.Equal("builtins", PyObject.GetAttr(PyLexicalSuper.Type, "__module__"));
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [proxy, PyLexicalSuper.Type], [], []));
        Assert.Equal(false, PyOps.CallBuiltin("isinstance", [cls, PyLexicalSuper.Type], [], []));
    }

    [Fact]
    public void ShadowCallableNeverConsumesMalformedMissingOrUnsupportedFrame()
    {
        int calls = 0;
        var shadow = Function("super", (_, args, names, _) =>
        {
            Assert.Empty(args); Assert.Empty(names); calls++; return 42L;
        });
        Assert.Equal(42L, PyOps.CallZeroWithFrame(1700, shadow, "not an env", 99, "not a cell", 2));
        Assert.Equal(42L, PyOps.CallZeroWithFrame(1700, shadow, null, -1, null, 0));
        Assert.Equal(42L, PyOps.CallZeroWithFrame(1700, shadow, null, 0, null, -1));
        Assert.Equal(3, calls);
        Assert.Throws<PyTypeError>(() => PyOps.CallZeroWithFrame(1700, 12L, null, 0, null, 0));
    }

    [Theory]
    [InlineData(1720)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void OneSiteAlternatesShadowCanonicalAndAnotherShadowWithoutReadingUnusedFrame(int site)
    {
        int firstCalls = 0, secondCalls = 0;
        var first = Function("first", (_, _, _, _) => { firstCalls++; return 11L; });
        var second = Function("second", (_, _, _, _) => { secondCalls++; return 22L; });
        var cls = new PyClass("C"); var receiver = new PyInstance(cls);
        object?[] environment = [Cell(cls)];
        Assert.Equal(11L, PyOps.CallZeroWithFrame(site, first, "bad env", 99, "bad cell", 2));
        var proxy = Assert.IsType<PySuper>(PyOps.CallZeroWithFrame(site, PyLexicalSuper.Type, environment, 0, receiver, 1));
        Assert.Same(cls, proxy.Owner); Assert.Same(receiver, proxy.Self);
        Assert.Equal(22L, PyOps.CallZeroWithFrame(site, second, null, -1, null, 0));
        Assert.Equal(11L, PyOps.CallZeroWithFrame(site, first, null, 0, null, -1));
        Assert.Equal("super(): empty __class__ cell", Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallZeroWithFrame(site, PyLexicalSuper.Type, new object?[] { PyOps.CellNew() }, 0, receiver, 1)).Message);
        Assert.Equal(22L, PyOps.CallZeroWithFrame(site, second, null, -1, null, 0));
        Assert.Equal(2, firstCalls); Assert.Equal(2, secondCalls);
    }

    [Fact]
    public void ErrorPriorityUsesRealEmptyCellsAndDeletedSentinel()
    {
        var canonical = PyLexicalSuper.Type;
        object?[] emptyEnv = [PyOps.CellNew()];
        Assert.Equal("super(): no arguments", Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallZeroWithFrame(1700, canonical, emptyEnv, 0, PyUnbound.Instance, 0)).Message);
        Assert.Equal("super(): arg[0] deleted", Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallZeroWithFrame(1700, canonical, null, -1, PyUnbound.Instance, 1)).Message);
        Assert.Equal("super(): arg[0] deleted", Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallZeroWithFrame(1700, canonical, emptyEnv, 0, PyOps.CellNew(), 2)).Message);
        Assert.Equal("super(): __class__ cell not found", Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallZeroWithFrame(1700, canonical, null, -1, PyNone.Instance, 1)).Message);
        Assert.Equal("super(): empty __class__ cell", Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallZeroWithFrame(1700, canonical, emptyEnv, 0, PyNone.Instance, 1)).Message);
        Assert.Contains("__class__ is not a type", Assert.Throws<PyRuntimeError>(() =>
            Super(PyNone.Instance, PyNone.Instance)).Message);
        Assert.Throws<PyNotImplementedError>(() => Super(new PyClass("C"), PyNone.Instance));
        Assert.Throws<PyTypeError>(() => Super(new PyClass("C"), 12L));
    }

    [Fact]
    public void EnvironmentAndArgumentCellsReadCurrentContentsWithoutSnapshot()
    {
        var first = new PyClass("First"); var second = new PyClass("Second");
        var initial = new PyInstance(first); var current = new PyInstance(second);
        object classCell = Cell(first); object argCell = Cell(initial);
        object?[] environment = [Cell(123L), classCell];
        PyOps.CellSet(classCell, second); PyOps.CellSet(argCell, current);
        var proxy = Assert.IsType<PySuper>(PyOps.CallZeroWithFrame(1700, PyLexicalSuper.Type, environment, 1, argCell, 2));
        Assert.Same(second, proxy.Owner); Assert.Same(current, proxy.Self);
        Assert.Same(current, Assert.IsType<PySuper>(Super(second, current)).Self);
        PyOps.CellSet(argCell, PyUnbound.Instance);
        Assert.Equal("super(): arg[0] deleted", Assert.Throws<PyRuntimeError>(() =>
            PyOps.CallZeroWithFrame(1700, PyLexicalSuper.Type, environment, 1, argCell, 2)).Message);
    }

    [Fact]
    public void LexicalClassMustOccurInActualReceiverChain()
    {
        var parent = new PyClass("Parent"); var defined = Child(parent); var derived = Child(defined);
        var receiver = new PyInstance(derived);
        var proxy = Assert.IsType<PySuper>(Super(defined, receiver));
        Assert.Same(defined, proxy.Owner); Assert.Same(receiver, proxy.Self);
        var unrelatedSameName = new PyClass(defined.Name);
        Assert.Throws<PyTypeError>(() => Super(defined, new PyInstance(unrelatedSameName)));
        // 重执行所得的新同名类不能冒充旧函数cell保存的原类。
        Assert.Throws<PyTypeError>(() => Super(defined, new PyInstance(Child(parent))));
    }

    [Fact]
    public void ProxyClassFallbackIsRejectedWithoutEvaluatingUserProperty()
    {
        var start = new PyClass("Start"); var proxyClass = new PyClass("Proxy");
        proxyClass.SetAttr("__class__", new PyProperty(Function("get", (_, _, _, _) =>
            throw new Exception("must not run"))));
        Assert.Throws<PyNotImplementedError>(() => Super(start, new PyInstance(proxyClass)));
    }

    [Fact]
    public void NativeAndClassReceiverBoundariesAreExplicit()
    {
        var cls = new PyClass("C");
        Assert.Throws<PyNotImplementedError>(() => Super(cls, cls));
        Assert.Throws<PyNotImplementedError>(() => Super(PyType.Get("object"), new PyInstance(cls)));
        var listChild = (PyClass)PyOps.MakeClass("L", [PyType.Get("list")], null);
        Assert.Throws<PyNotImplementedError>(() => Super(listChild, new PyInstance(listChild)));
        Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("S", [PyLexicalSuper.Type], null));
    }

    [Fact]
    public void UnsupportedAndMalformedFramesDoNotFallBackToTls()
    {
        Assert.Throws<PyNotImplementedError>(() =>
            PyOps.CallZeroWithFrame(1700, PyLexicalSuper.Type, null, -1, null, -1));
        Assert.Throws<PyNotImplementedError>(() =>
            PyOps.CallZeroWithFrame(1700, PyLexicalSuper.Type, "bad env", 0, PyNone.Instance, 1));
        Assert.Throws<PyNotImplementedError>(() =>
            PyOps.CallZeroWithFrame(1700, PyLexicalSuper.Type, new object?[] { 1L }, 0, PyNone.Instance, 1));
        Assert.Throws<PyNotImplementedError>(() => Super(new PyClass("C"), new object?[2], 2));
    }

    [Fact]
    public void NoFrameFactoryRejectsZeroExplicitAndKeywordCalls()
    {
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyLexicalSuper.Type, [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyLexicalSuper.Type, [1L], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyLexicalSuper.Type, [1L, 2L], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyLexicalSuper.Type, [], ["x"], [1L]));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(new PyStaticMethod(PyLexicalSuper.Type), [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("super", [1L, 2L], [], []));
    }

    [Fact]
    public void LegacyBuiltinRejectsKeywordsBeforeExplicitArgumentsAndTls()
    {
        // 与原WithKeywords一致：先报首个关键词，再判显式实参，不提前进入TLS。
        object?[][] arguments = [[], [1L], [1L, 2L]];
        foreach (object?[] args in arguments)
            Assert.Equal("super() got an unexpected keyword argument 'x'", Assert.Throws<PyTypeError>(() =>
                PyOps.CallBuiltin("super", args, ["x", "y"], [3L, 4L])).Message);
        foreach (object?[] args in arguments[1..])
            Assert.Equal("explicit super arguments are not supported", Assert.Throws<PyNotImplementedError>(() =>
                PyOps.CallBuiltin("super", args, [], [])).Message);
    }

    [Fact]
    public void ExistingMethodTlsCannotSupplyCanonicalFrame()
    {
        var cls = new PyClass("C"); var receiver = new PyInstance(cls);
        var method = Function("method", (_, _, _, _) =>
        {
            // 旧ABI在已绑定方法内仍可工作，而一等canonical调用必须独立拒绝。
            Assert.IsType<PySuper>(PyOps.CallBuiltin("super", [], [], []));
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyLexicalSuper.Type, [], [], []));
            Assert.Equal("super(): no arguments", Assert.Throws<PyRuntimeError>(() =>
                PyOps.CallZeroWithFrame(1700, PyLexicalSuper.Type, null, -1, null, 0)).Message);
            return PyNone.Instance;
        });
        cls.SetAttr("method", method);
        PyOps.CallValue(PyObject.GetAttr(receiver, "method"), [], [], []);
    }

    [Fact]
    public void MethodsPropertiesAndKnownWrappersUseActualReceiver()
    {
        var parent = new PyClass("Parent"); var cls = Child(parent); var derived = Child(cls);
        var receiver = new PyInstance(derived);
        parent.SetAttr("method", Function("method", (_, args, _, _) => args[0]));
        parent.SetAttr("__init__", Function("__init__", (_, args, _, _) => args[0]));
        parent.SetAttr("property", new PyProperty(Function("get", (_, args, _, _) => args[0])));
        parent.SetAttr("static", new PyStaticMethod(Function("static", (_, args, _, _) => (long)args.Length)));
        parent.SetAttr("class_method", new PyClassMethod(Function("class_method", (_, args, _, _) => args[0])));
        object? proxy = Super(cls, receiver);
        Assert.Same(receiver, PyOps.CallValue(PyObject.GetAttr(proxy, "method"), [], [], []));
        Assert.Same(receiver, PyPic.CallMethod0(1711, proxy, "method"));
        Assert.Same(receiver, PyPic.CallMethod0(1711, proxy, "method"));
        Assert.Same(receiver, PyOps.CallValue(PyObject.GetAttr(proxy, "__init__"), [], [], []));
        Assert.Same(receiver, PyObject.GetAttr(proxy, "property"));
        Assert.Equal(0L, PyOps.CallValue(PyObject.GetAttr(proxy, "static"), [], [], []));
        Assert.Same(derived, PyOps.CallValue(PyObject.GetAttr(proxy, "class_method"), [], [], []));
    }

    [Fact]
    public void CanonicalProxyPreservesRootNewAndInheritedNewAttributeLookup()
    {
        var parent = (PyClass)PyOps.MakeClass("Parent", [], null);
        var cls = Child(parent);
        PyOps.FinalizeOrdinaryClass(parent); PyOps.FinalizeOrdinaryClass(cls);
        object? proxy = Super(cls, new PyInstance(cls));
        // 代理上的__new__是既有基类构造属性，不是super类型自身的反射入口。
        var allocated = Assert.IsType<PyInstance>(PyOps.CallValue(PyObject.GetAttr(proxy, "__new__"), [cls], [], []));
        Assert.Same(cls, allocated.Class);
        Assert.Same(cls, Assert.IsType<PyInstance>(PyPic.CallMethod1(1714, proxy, "__new__", cls)).Class);
        parent.SetAttr("__new__", new PyStaticMethod(Function("__new__", (_, args, _, _) => args[0])));
        Assert.Same(cls, PyOps.CallValue(PyObject.GetAttr(proxy, "__new__"), [cls], [], []));
        Assert.Same(cls, PyPic.CallMethod1(1714, proxy, "__new__", cls));
    }

    [Theory]
    [InlineData("__thisclass__")]
    [InlineData("__self__")]
    [InlineData("__self_class__")]
    [InlineData("__get__")]
    [InlineData("__mro__")]
    [InlineData("__dict__")]
    public void ProxyComplexReflectionRemainsExplicitlyUnsupported(string name)
    {
        var parent = new PyClass("Parent"); var cls = Child(parent);
        parent.SetAttr(name, Function(name, (_, _, _, _) => 123L));
        object? proxy = Super(cls, new PyInstance(cls));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(proxy, name));
        Assert.Throws<PyNotImplementedError>(() => PyPic.CallMethod0(1712, proxy, name));
    }

    [Theory]
    [InlineData("__new__")]
    [InlineData("__init__")]
    [InlineData("__get__")]
    [InlineData("__mro__")]
    [InlineData("__dict__")]
    public void CanonicalTypeComplexReflectionIsExplicitlyUnsupported(string name) =>
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(PyLexicalSuper.Type, name));

    [Fact]
    public void CustomDescriptorTypeChainIsRejectedWithoutInvokingIt()
    {
        var descriptorRoot = new PyClass("DescriptorRoot");
        descriptorRoot.SetAttr("__get__", Function("__get__", (_, _, _, _) => throw new Exception("must not run")));
        var parent = new PyClass("Parent"); var cls = Child(parent);
        parent.SetAttr("value", new PyInstance(Child(descriptorRoot)));
        object? proxy = Super(cls, new PyInstance(cls));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(proxy, "value"));
        Assert.Throws<PyNotImplementedError>(() => PyPic.CallMethod0(1713, proxy, "value"));
        // 实例字典伪造的__get__不是类型协议，不应把普通值误判为描述符。
        var ordinary = new PyInstance(new PyClass("Ordinary"));
        PyObject.SetAttr(ordinary, "__get__", 12L); parent.SetAttr("value", ordinary);
        Assert.Same(ordinary, PyObject.GetAttr(proxy, "value"));
    }
}
