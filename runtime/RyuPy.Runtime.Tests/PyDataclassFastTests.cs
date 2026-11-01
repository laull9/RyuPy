using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>构造计划的语义守卫：快路径预热后修改类，必须即时回到通用调用协议。</summary>
public class PyDataclassFastTests
{
    private static PyClass Record(bool frozen = false)
    {
        var cls = new PyClass("Record");
        PyOps.DeclareDataclass(cls, ["x", "y"], frozen);
        PyOps.ApplyDataclass(cls, frozen);
        cls.Invoke([1L, 2L], [], []);
        return cls;
    }

    private static PyFunction Fn(string name, string[] parameters, Func<object?[], object?> body)
    {
        var kinds = Enumerable.Repeat("pos", parameters.Length).ToArray();
        var flags = Enumerable.Repeat("0", parameters.Length).ToArray();
        var signature = PySignature.Create(name, parameters, kinds, flags);
        return new PyFunction(name, signature,
            (self, args, names, values) => body(PyBinder.Bind(signature, args, names, values, self.Defaults)), [], false);
    }

    [Fact]
    public void ReboundInitAndRestoredInitAreObserved()
    {
        var cls = Record();
        object? original = cls.Attributes["__init__"];
        cls.SetAttr("__init__", Fn("__init__", ["self", "x", "y"], args =>
        {
            PyObject.SetAttr(args[0], "x", 99L);
            return PyNone.Instance;
        }));
        Assert.Equal(99L, PyObject.GetAttr(cls.Invoke([3L, 4L], [], []), "x"));
        cls.SetAttr("__init__", original);
        Assert.Equal(3L, PyObject.GetAttr(cls.Invoke([3L, 4L], [], []), "x"));
    }

    [Fact]
    public void LatePostInitIsIgnoredButPropertyInvalidatesPlan()
    {
        var cls = Record();
        int calls = 0;
        cls.SetAttr("__post_init__", Fn("__post_init__", ["self"], args =>
        {
            calls++;
            PyObject.SetAttr(args[0], "x", 12L);
            return PyNone.Instance;
        }));
        Assert.Equal(3L, PyObject.GetAttr(cls.Invoke([3L, 4L], [], []), "x"));
        Assert.Equal(0, calls);
        cls.DeleteAttr("__post_init__");
        cls.SetAttr("x", new PyProperty(Fn("x", ["self"], _ => 77L), null));
        Assert.Throws<PyAttributeError>(() => cls.Invoke([3L, 4L], [], []));
        cls.DeleteAttr("x");
        Assert.Equal(3L, PyObject.GetAttr(cls.Invoke([3L, 4L], [], []), "x"));
    }

    [Fact]
    public void ExistingPostInitIsLookedUpAtEveryConstruction()
    {
        var cls = new PyClass("WithPostInit");
        cls.SetAttr("__post_init__", Fn("__post_init__", ["self"], args =>
        {
            PyObject.SetAttr(args[0], "x", 8L);
            return PyNone.Instance;
        }));
        PyOps.DeclareDataclass(cls, ["x"], false);
        PyOps.ApplyDataclass(cls, false);
        Assert.Equal(8L, PyObject.GetAttr(cls.Invoke([3L], [], []), "x"));
        cls.SetAttr("__post_init__", Fn("__post_init__", ["self"], args =>
        {
            PyObject.SetAttr(args[0], "x", 9L);
            return PyNone.Instance;
        }));
        Assert.Equal(9L, PyObject.GetAttr(cls.Invoke([3L], [], []), "x"));
        cls.DeleteAttr("__post_init__");
        Assert.Throws<PyAttributeError>(() => cls.Invoke([3L], [], []));
    }

    [Fact]
    public void DefaultsAndFactoriesRetainBindingAndFreshness()
    {
        var defaults = new PyClass("Defaults");
        defaults.SetAttr("y", 8L);
        PyOps.DeclareDataclass(defaults, ["x", "y"], false);
        PyOps.ApplyDataclass(defaults, false);
        defaults.Invoke([1L, 2L], [], []);
        Assert.Equal(8L, PyObject.GetAttr(defaults.Invoke([3L], [], []), "y"));
        int calls = 0;
        var factory = Fn("factory", [], _ => { calls++; return new PyList(); });
        object? field = PyOps.CallValue(PyDataclasses.CreateAttributes()["field"], [],
            ["default_factory"], [factory]);
        var factories = new PyClass("Factories");
        factories.SetAttr("x", field);
        PyOps.DeclareDataclass(factories, ["x"], false);
        PyOps.ApplyDataclass(factories, false);
        var explicitValue = new PyList();
        Assert.Same(explicitValue, PyObject.GetAttr(factories.Invoke([explicitValue], [], []), "x"));
        object? first = PyObject.GetAttr(factories.Invoke([], [], []), "x");
        object? second = PyObject.GetAttr(factories.Invoke([], [], []), "x");
        Assert.NotSame(first, second);
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("deque")]
    [InlineData("defaultdict")]
    [InlineData("Counter")]
    public void NativeMutableCollectionsCannotBeSharedDefaults(string typeName)
    {
        object? shared = PyOps.CallValue(PyCollections.CreateAttributes()[typeName], [], [], []);
        var cls = new PyClass("MutableDefault");
        cls.SetAttr("items", shared);
        PyOps.DeclareDataclass(cls, ["items"], false);
        Assert.Throws<PyValueError>(() => PyOps.ApplyDataclass(cls, false));
        Assert.False(cls.Attributes.ContainsKey("__init__"));
    }

    [Fact]
    public void FrozenIdentityAndArgumentErrorsRemainUnchanged()
    {
        var cls = Record(true);
        object first = cls.Invoke([3L, 4L], [], [])!;
        object second = cls.Invoke([3L, 4L], [], [])!;
        Assert.NotSame(first, second);
        Assert.Same(cls, PyObject.TypeOf(first));
        Assert.Throws<PyFrozenInstanceError>(() => PyObject.SetAttr(first, "x", 9L));
        Assert.Throws<PyTypeError>(() => cls.Invoke([1L], [], []));
        Assert.Throws<PyTypeError>(() => cls.Invoke([1L, 2L, 3L], [], []));
        Assert.Equal(4L, PyObject.GetAttr(cls.Invoke([3L], ["y"], [4L]), "y"));
    }
}
