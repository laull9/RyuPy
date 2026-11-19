// C1 审查回归：外部真实 C3、纯Mixin镜像关系与各入口返回的统一 super 类型身份。
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public partial class CPythonClassBridgeTests
{
    private static PyFunction SuperFunction(string name, PyCall body, bool usesSuper = false) =>
        new(name, PySignature.Create(name, ["self"], ["pos"], ["0"]), body, [], usesSuper);
    private static object? SuperCall(object? callable, params object?[] args) => PyOps.CallValue(callable, args, [], []);

    [Fact]
    public void ForeignC3_UsesSiblingMixinBeforeMixinBaseAndPreservesMetadataShadow()
    {
        RequireHost();
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        var root = (PyClass)PyOps.MakeClass("SuperMixinBase", [], null);
        root.SetAttr("chain", SuperFunction("chain", (_, _, _, _) => "MixinBase"));
        PyOps.FinalizeOrdinaryClass(root);
        var mixin = (PyClass)PyOps.MakeClass("SuperMixin", [root], null);
        mixin.SetAttr("chain", SuperFunction("chain", (_, _, _, _) =>
            "Mixin:" + PyRepr.Str(SuperCall(PyObject.GetAttr(PyMethodContext.MakeSuper(), "chain"))), usesSuper: true));
        PyOps.FinalizeOrdinaryClass(mixin);
        var right = (PyClass)PyOps.MakeClass("SuperRightMixin", [root], null);
        right.SetAttr("chain", SuperFunction("chain", (_, _, _, _) => "RightMixin"));
        right.SetAttr("__self__", "right-shadow");
        PyOps.FinalizeOrdinaryClass(right);
        var unrelated = (PyClass)PyOps.MakeClass("SuperUnrelated", [], null);
        PyOps.FinalizeOrdinaryClass(unrelated);
        foreach (object?[] bases in new object?[][] { [mixin, right, userDict], [userDict, mixin, right] })
        {
            var cls = (PyClass)PyLexicalClassFactory.Finish(PyLexicalClassFactory.Prepare(
                "SuperCombined" + (ReferenceEquals(bases[0], mixin) ? "First" : "Last"), "SuperCombined", "test_mod", bases));
            object instance = SuperCall(cls)!;
            Assert.Equal("Mixin:RightMixin", SuperCall(PyObject.GetAttr(instance, "chain")));
            Assert.True(cls.IsSubclassOf(mixin)); Assert.True(cls.IsSubclassOf(right));
            Assert.True(PyObject.IsInstance(instance, mixin)); Assert.True(PyObject.IsInstance(instance, right));
            Assert.False(cls.IsSubclassOf(unrelated));
            object proxy = SuperCall(PyLexicalSuper.Type, mixin, instance)!;
            Assert.Equal("right-shadow", PyObject.GetAttr(proxy, "__self__"));
            Assert.Same(mixin, PyObject.GetAttr(proxy, "__thisclass__"));
            right.SetAttr("__self__", "changed-shadow");
            Assert.Equal("changed-shadow", PyObject.GetAttr(proxy, "__self__"));
            Assert.True(right.DeleteAttr("__self__"));
            Assert.Same(instance, PyObject.GetAttr(proxy, "__self__"));
            right.SetAttr("__self__", "right-shadow");
            right.SetAttr("chain", SuperFunction("chain", (_, _, _, _) => "ChangedRight"));
            Assert.Equal("Mixin:ChangedRight", SuperCall(PyObject.GetAttr(instance, "chain")));
            Assert.True(right.DeleteAttr("chain"));
            Assert.Equal("Mixin:MixinBase", SuperCall(PyObject.GetAttr(instance, "chain")));
            right.SetAttr("chain", SuperFunction("chain", (_, _, _, _) => "RightMixin"));
            Assert.Throws<PyTypeError>(() => SuperCall(PyLexicalSuper.Type, unrelated, instance));
        }
    }

    [Fact]
    public void ForeignSuper_ConstructIncomingRebindAndRoundtripKeepCanonicalTypeAndIdentity()
    {
        RequireHost();
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        var cls = (PyClass)PyOps.PrepareRuntimeDerivedClass("SuperIdentityDict", [userDict]);
        PyOps.FinalizeOrdinaryClass(cls);
        object instance = SuperCall(cls)!;
        PyModule helper = Bootstrap.Host.Import("bridge_fixture.super_contract");
        object make = helper.GetAttr("make_super")!, held = helper.GetAttr("get_held")!;
        object check = helper.GetAttr("check_super")!, rebind = helper.GetAttr("rebind")!;
        var local = (PySuper)SuperCall(PyLexicalSuper.Type, cls, instance)!;
        Assert.Null(local.ForeignObject);
        Assert.Same(local, SuperCall(check, local));
        Assert.NotNull(local.ForeignObject);
        Assert.Same(cls, local.Owner); Assert.Same(instance, local.Self);
        Assert.Same(local.ForeignObject, PyForeignClassBridge.GetForeignSuper(local));
        foreach (object proxy in new[] {
            SuperCall(PyLexicalSuper.Type, cls)!,
            SuperCall(PyLexicalSuper.Type, userDict, instance)!,
            SuperCall(make, cls, instance)!,
        })
        {
            Assert.IsType<PySuper>(proxy);
            Assert.Same(PyLexicalSuper.Type, PyObject.TypeOf(proxy));
            Assert.Same(PyLexicalSuper.Type, PyObject.GetAttr(proxy, "__class__"));
            Assert.True(PyLexicalSuper.Type.Accepts(proxy));
            Assert.Same(proxy, SuperCall(check, proxy));
        }
        object incoming = SuperCall(make, cls)!;
        Assert.Same(incoming, SuperCall(held));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(incoming, "__self__"));
        object rebound = SuperCall(PyObject.GetAttr(incoming, "__get__"), instance, cls)!;
        Assert.IsType<PySuper>(rebound);
        Assert.Same(instance, PyObject.GetAttr(rebound, "__self__"));
        Assert.Same(cls, PyObject.GetAttr(rebound, "__thisclass__"));
        Assert.Same(PyLexicalSuper.Type, PyObject.TypeOf(SuperCall(rebind, incoming, instance, cls)));
        Assert.Same(PyLexicalSuper.Type, Bootstrap.Host.Import("builtins").GetAttr("super"));
        // 普通外部对象仍保留真实类型，不能通过中性代理泛化成 super。
        Assert.False(PyLexicalSuper.Type.Accepts(userDict));
        Assert.Throws<PyTypeError>(() => SuperCall(PyLexicalSuper.Type, userDict, 1L));
    }

    [Fact]
    public void ForeignSuperSubclass_PreservesItsOwnTypeAndCanonicalSuperRelations()
    {
        RequireHost();
        object? userDict = PyModules.Import("collections").GetAttr("UserDict");
        var leaf = (PyClass)PyOps.PrepareRuntimeDerivedClass("SuperSubclassReceiver", [userDict]);
        PyOps.FinalizeOrdinaryClass(leaf);
        object receiver = SuperCall(leaf)!;
        object make = Bootstrap.Host.Import("bridge_fixture.super_contract").GetAttr("make_super_subclass")!;
        object pair = SuperCall(make, leaf, receiver)!;
        object foreignClass = PyOps.GetItem(pair, 0L)!, foreignValue = PyOps.GetItem(pair, 1L)!;
        Assert.IsAssignableFrom<IPyForeignObject>(foreignClass);
        Assert.IsAssignableFrom<IPyForeignObject>(foreignValue);
        Assert.Same(foreignClass, PyObject.TypeOf(foreignValue));
        Assert.True(PyLexicalSuper.Type.Accepts(foreignValue));
        Assert.True((bool)PyOps.CallBuiltin("issubclass", [foreignClass, PyLexicalSuper.Type], [], [])!);

        // 外部 S(super) 进入已有真实类型派生域，编译子类实例继续保留自身类型。
        var compiled = (PyClass)PyOps.PrepareRuntimeDerivedClass("CompiledSuperSubclass", [foreignClass]);
        PyOps.FinalizeOrdinaryClass(compiled);
        object compiledValue = SuperCall(compiled, leaf, receiver)!;
        Assert.Same(compiled, PyObject.TypeOf(compiledValue));
        Assert.True(PyLexicalSuper.Type.Accepts(compiledValue));
        Assert.True((bool)PyOps.CallBuiltin("issubclass", [compiled, PyLexicalSuper.Type], [], [])!);
        Assert.False(PyLexicalSuper.Type.Accepts(foreignClass));
        Assert.False(PyLexicalSuper.Type.Accepts(compiled));
        Assert.False(PyLexicalSuper.Type.Accepts(receiver));
        Assert.False(PyLexicalSuper.Type.Accepts(userDict));
        Assert.False(PyLexicalSuper.Type.Accepts(SuperCall(userDict)));
        Assert.False((bool)PyOps.CallBuiltin("issubclass", [leaf, PyLexicalSuper.Type], [], [])!);
        Assert.False((bool)PyOps.CallBuiltin("issubclass", [FinishedMixin("UnrelatedSuperType"), PyLexicalSuper.Type], [], [])!);
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("issubclass", [foreignValue, PyLexicalSuper.Type], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("issubclass", [foreignClass, PyMappingProxy.Type], [], []));
    }
}
