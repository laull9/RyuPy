// C1 MI 后代回归：单继承不丢外部 MRO，CPython 再派生的 opaque 类型/实例消费已缓存镜像。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    private static PyClass FinishedMixin(string name)
    {
        var cls = (PyClass)PyOps.MakeClass(name, [], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }

    private static PyClass FinishedForeign(string name, object?[] bases)
    {
        var cls = (PyClass)PyLexicalClassFactory.Finish(PyLexicalClassFactory.Prepare(name, name, "test_mod", bases));
        return cls;
    }

    [Fact]
    public void ReleasedForeignInstance_RejectsRelationQueryBeforeUnrelatedMirrorLookup()
    {
        RequireHost();
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        object value = PyOps.CallValue(userDict, [], [], [])!;
        var foreign = Assert.IsAssignableFrom<IPyForeignObject>(value);
        var unrelated = FinishedMixin("ReleasedInstanceUnrelated");
        Assert.False(PyObject.IsInstance(value, unrelated));
        Assert.IsAssignableFrom<IDisposable>(value).Dispose();
        Assert.Throws<ObjectDisposedException>(() => PyObject.IsInstance(foreign, unrelated));
    }

    [Fact]
    public void ForeignMiSingleDescendants_PreserveExplicitAndLexicalSuperWithInstanceAndClassReceivers()
    {
        RequireHost();
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        PyClass mixin = FinishedMixin("DescendantMixin"), right = FinishedMixin("DescendantRight");
        foreach (bool foreignFirst in new[] { false, true })
        {
            PyClass parent = FinishedForeign("DescendantParent" + foreignFirst,
                foreignFirst ? [userDict, mixin, right] : [mixin, right, userDict]);
            PyClass child = FinishedForeign("DescendantChild" + foreignFirst, [parent]);
            object instance = SuperCall(child)!;
            foreach (PyClass start in new[] { mixin, right })
                foreach (object receiver in new[] { instance, child })
                {
                    object explicitProxy = SuperCall(PyLexicalSuper.Type, start, receiver)!;
                    Assert.Same(start, PyObject.GetAttr(explicitProxy, "__thisclass__"));
                    object?[] environment = [new object?[] { start }];
                    object lexicalProxy = PyOps.CallZeroWithFrame(3591, PyLexicalSuper.Type, environment, 0, receiver, 1)!;
                    Assert.Same(start, PyObject.GetAttr(lexicalProxy, "__thisclass__"));
                }
        }
    }

    [Fact]
    public void OpaqueCpSubclass_RelationsUseExistingMixinMirrorsAndRejectUnrelatedValues()
    {
        RequireHost();
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        PyClass mixin = FinishedMixin("OpaqueMixin"), right = FinishedMixin("OpaqueRight");
        PyClass unrelated = FinishedMixin("OpaqueUnrelated");
        object make = Bootstrap.Host.Import("bridge_fixture.super_contract").GetAttr("make_subclass")!;
        foreach (bool foreignFirst in new[] { false, true })
        {
            PyClass parent = FinishedForeign("OpaqueParent" + foreignFirst,
                foreignFirst ? [userDict, mixin, right] : [mixin, right, userDict]);
            PyClass child = FinishedForeign("OpaqueChild" + foreignFirst, [parent]);
            foreach (PyClass known in new[] { parent, child })
            {
                object opaque = SuperCall(make, known)!;
                Assert.IsAssignableFrom<IPyForeignObject>(opaque);
                object instance = SuperCall(opaque)!;
                Assert.IsAssignableFrom<IPyForeignObject>(instance);
                foreach (PyClass target in new[] { mixin, right })
                {
                    Assert.True((bool)PyOps.CallBuiltin("issubclass", [opaque, target], [], [])!);
                    Assert.True(PyObject.IsInstance(instance, target));
                }
                Assert.False((bool)PyOps.CallBuiltin("issubclass", [opaque, unrelated], [], [])!);
                Assert.False(PyObject.IsInstance(instance, unrelated));
                Assert.False(PyObject.IsInstance(opaque, mixin));
                Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("issubclass", [instance, mixin], [], []));
                Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("issubclass", [instance, unrelated], [], []));
            }
        }
        Assert.False((bool)PyOps.CallBuiltin("issubclass", [mixin, userDict], [], [])!);
        Assert.False((bool)PyOps.CallBuiltin("issubclass", [unrelated, userDict], [], [])!);
    }
}
