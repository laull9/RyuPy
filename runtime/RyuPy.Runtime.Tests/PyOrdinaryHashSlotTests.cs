// 普通类 hash 槽的有限合同：真实完成接点、动态类链、根描述符、空表准入和 dataclass 联动。
// 不把 Hashable ABC、完整 hash 返回归约或空表有效用户 hash 次数旧债纳入本切。
using System;
using System.Linq;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyOrdinaryHashSlotTests
{
    private static PyClass Make(string name, PyClass? parent = null) =>
        (PyClass)PyOps.MakeClass(name, parent is null ? [] : [parent], null);
    private static void Finish(PyClass cls) => PyOps.FinalizeOrdinaryClass(cls);
    private static PyInstance New(PyClass cls) => Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    private static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    private static PyFunction Fn(string name, string[] parameters, Func<object?[], object?> body) =>
        new(name, PySignature.Create(name, parameters, parameters.Select(_ => "pos").ToArray(),
            parameters.Select(_ => "0").ToArray()), (_, args, _, _) => body(args), []);
    private static PyFunction Hash(long value) => Fn("__hash__", ["self"], _ => value);
    private static PyFunction Eq() => Fn("__eq__", ["self", "other"], _ =>
        throw new InvalidOperationException("hash 槽查找不得执行 eq"));
    private static PyInstance Disabled()
    {
        var cls = Make("Disabled"); cls.SetAttr("__eq__", Eq()); Finish(cls); return New(cls);
    }
    private static void Unhashable(Action operation, string name) =>
        Assert.Equal($"unhashable type: '{name}'", Assert.Throws<PyTypeError>(operation).Message);
    private static void Caller(Action<PyValueError> body)
    {
        long depth = PyTraceback.PushFrame("hash_slots.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try { PyTraceback.SetLine(37); body(outer); }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
    private static void Prepared(PyBaseException error, PyBaseException outer)
    {
        Assert.Same(outer, error.Context); Assert.Single(error.Frames);
        Assert.Equal(new PyStackFrame("hash_slots.py", "caller", 37), error.Frames[0]);
    }
    private static PyDict DictState(int state)
    {
        var dict = new PyDict();
        if (state != 0) dict.Set("kept", 1L);
        if (state == 2) dict.Clear();
        if (state == 3) dict.Delete("kept");
        return dict;
    }

    [Fact]
    public void FinalNamespaceMaterializesNoneOnlyForOwnEqualityWithoutOwnHash()
    {
        foreach (object eq in new object[] { Eq(), PyNone.Instance })
        {
            var cls = Make("Equality"); cls.SetAttr("__eq__", eq);
            Assert.False(cls.Attributes.ContainsKey("__hash__")); Finish(cls);
            Assert.Same(PyNone.Instance, cls.Attributes["__hash__"]);
            Unhashable(() => PyHash.Of(New(cls)), "Equality");
        }
        var ne = Make("NeOnly"); ne.SetAttr("__ne__", Eq()); Finish(ne);
        Assert.False(ne.Attributes.ContainsKey("__hash__"));
        var removed = Make("BodyDeleted"); removed.SetAttr("__eq__", Eq());
        Assert.True(removed.DeleteAttr("__eq__")); Finish(removed);
        Assert.False(removed.Attributes.ContainsKey("__hash__"));
        var explicitHash = Make("Explicit"); var hash = Hash(17);
        explicitHash.SetAttr("__eq__", Eq()); explicitHash.SetAttr("__hash__", hash); Finish(explicitHash);
        Assert.Same(hash, explicitHash.Attributes["__hash__"]); Assert.Equal(17L, PyHash.Of(New(explicitHash)));
    }

    [Fact]
    public void InheritedEqualityDoesNotSuppressButOwnEqualityOverridesInheritedHash()
    {
        var parent = Make("Parent"); parent.SetAttr("__eq__", Eq()); parent.SetAttr("__hash__", Hash(17)); Finish(parent);
        var child = Make("Child", parent); Finish(child);
        Assert.False(child.Attributes.ContainsKey("__hash__")); Assert.Equal(17L, PyHash.Of(New(child)));
        var disabled = Make("Override", parent); disabled.SetAttr("__eq__", Eq()); Finish(disabled);
        Assert.Same(PyNone.Instance, disabled.Attributes["__hash__"]);
        var descendant = Make("Descendant", disabled); Finish(descendant);
        Assert.False(descendant.Attributes.ContainsKey("__hash__"));
        Unhashable(() => PyHash.Of(New(descendant)), "Descendant");
        var restored = Make("Restored", parent); restored.SetAttr("__eq__", Eq());
        object? alias = PyObject.GetAttr(parent, "__hash__"); restored.SetAttr("__hash__", alias); Finish(restored);
        parent.SetAttr("__hash__", Hash(23));
        Assert.Same(alias, restored.Attributes["__hash__"]); Assert.Equal(17L, PyHash.Of(New(restored)));
        Assert.Equal(23L, PyHash.Of(New(child)));
    }

    [Fact]
    public void LateEqualityAndDeletionDoNotRecomputeMaterializedHash()
    {
        var plain = Make("Plain"); Finish(plain); var value = New(plain); long identity = PyHash.Of(value);
        plain.SetAttr("__eq__", Eq()); Assert.False(plain.Attributes.ContainsKey("__hash__"));
        Assert.Equal(identity, PyHash.Of(value)); plain.SetAttr("__hash__", PyNone.Instance);
        Unhashable(() => PyHash.Of(value), "Plain"); Assert.True(plain.DeleteAttr("__hash__"));
        Assert.Equal(identity, PyHash.Of(value)); Assert.True(plain.DeleteAttr("__eq__"));
        var disabled = Disabled(); Assert.True(disabled.Class.DeleteAttr("__eq__"));
        Assert.Same(PyNone.Instance, disabled.Class.Attributes["__hash__"]);
        Unhashable(() => PyHash.Of(disabled), "Disabled");
        Assert.True(disabled.Class.DeleteAttr("__hash__"));
        Assert.Equal(Call(PyObject.GetAttr(PyBaseObject.Type, "__hash__"), disabled), PyHash.Of(disabled));
    }

    [Fact]
    public void AncestorRebindingInvalidatesWarmLookupsAndDeletionRevealsCurrentParent()
    {
        var parent = Make("Parent"); parent.SetAttr("__hash__", Hash(17)); Finish(parent);
        var child = Make("Child", parent); Finish(child); var value = New(child);
        Assert.Equal(17L, PyHash.Of(value)); Assert.Equal(17L, PyHash.Of(value));
        parent.SetAttr("__hash__", Hash(23)); Assert.Equal(23L, PyHash.Of(value));
        parent.SetAttr("__hash__", PyNone.Instance); Unhashable(() => PyHash.Of(value), "Child");
        Assert.True(parent.DeleteAttr("__hash__"));
        Assert.Equal(Call(PyObject.GetAttr(PyBaseObject.Type, "__hash__"), value), PyHash.Of(value));
        child.SetAttr("__hash__", PyNone.Instance); parent.SetAttr("__hash__", Hash(29));
        Unhashable(() => PyHash.Of(value), "Child"); Assert.True(child.DeleteAttr("__hash__"));
        Assert.Equal(29L, PyHash.Of(value));
    }

    [Fact]
    public void RootHashAndExplicitAliasBindForClassInstanceAndSuper()
    {
        object? root = PyObject.GetAttr(PyBaseObject.Type, "__hash__");
        var plain = Make("Plain"); Finish(plain); var value = New(plain);
        Assert.Same(root, PyObject.GetAttr(plain, "__hash__"));
        Assert.Equal(PyHash.Of(value), Call(root, value));
        Assert.Equal(PyHash.Of(value), Call(PyObject.GetAttr(value, "__hash__")));
        var alias = Make("Alias"); alias.SetAttr("__eq__", Eq()); alias.SetAttr("__hash__", root); Finish(alias);
        var aliased = New(alias); Assert.Same(root, PyObject.GetAttr(alias, "__hash__"));
        Assert.Equal(PyHash.Of(aliased), Call(PyObject.GetAttr(aliased, "__hash__")));
        Assert.Equal(PyHash.Of(aliased), Call(PyObject.GetAttr(alias, "__hash__"), aliased));
        var derived = Make("Derived", plain);
        derived.SetAttr("__hash__", Fn("__hash__", ["self"], _ =>
            Call(PyObject.GetAttr(PyMethodContext.MakeSuper(), "__hash__")))); Finish(derived);
        var throughSuper = New(derived); Assert.Equal(Call(root, throughSuper), PyHash.Of(throughSuper));
    }

    [Fact]
    public void ImplicitHashIgnoresInstanceShadowButExplicitAccessStillSeesIt()
    {
        var cls = Make("Shadow"); cls.SetAttr("__hash__", Hash(17)); Finish(cls); var value = New(cls);
        PyObject.SetAttr(value, "__hash__", Fn("shadow", [], _ => 97L));
        Assert.Equal(97L, Call(PyObject.GetAttr(value, "__hash__"))); Assert.Equal(17L, PyHash.Of(value));
        cls.SetAttr("__hash__", PyNone.Instance); Unhashable(() => PyHash.Of(value), "Shadow");
        PyObject.DelAttr(value, "__hash__"); Assert.Same(PyNone.Instance, PyObject.GetAttr(value, "__hash__"));
    }

    [Fact]
    public void StaticAndClassHashKeepSupportedWrapperCalls()
    {
        var stat = Make("StaticHash"); int calls = 0;
        stat.SetAttr("__hash__", new PyStaticMethod(Fn("__hash__", [], args =>
        { Assert.Empty(args); calls++; return 31L; }))); Finish(stat);
        Assert.Equal(31L, PyHash.Of(New(stat))); Assert.Equal(1, calls);
        var parent = Make("ClassHash"); object? receiver = null;
        parent.SetAttr("__hash__", new PyClassMethod(Fn("__hash__", ["cls"], args =>
        { receiver = Assert.Single(args); return 37L; }))); Finish(parent);
        var child = Make("ChildClassHash", parent); Finish(child); var value = New(child);
        Assert.Equal(37L, PyHash.Of(value)); Assert.Same(child, receiver);
        // 显式属性读取沿现有 classmethod 绑定规则，绑定到实际派生类。
        Assert.Equal(37L, Call(PyObject.GetAttr(value, "__hash__"))); Assert.Same(child, receiver);
    }

    [Fact]
    public void DictAdmissionRejectsNoneForFreshPopulatedClearedAndDeletedTables()
    {
        var key = Disabled();
        for (int state = 0; state < 4; state++)
        {
            var dict = DictState(state); int count = dict.Count; long version = dict.Version;
            Action[] operations = [() => dict.Get(key), () => dict.GetOrDefault(key, "fallback"),
                () => dict.TryGet(key, out _), () => dict.Contains(key), () => dict.Delete(key),
                () => dict.SetDefault(key, 2L), () => dict.Set(key, 2L),
                () => new PyDictView(dict, PyDictViewKind.Keys).Contains(key),
                () => new PyDictView(dict, PyDictViewKind.Items).Contains(new PyTuple([key, 1L]))];
            foreach (Action operation in operations) Unhashable(operation, "Disabled");
            Assert.Equal(count, dict.Count); Assert.Equal(version, dict.Version);
            Assert.False(new PyDictView(dict, PyDictViewKind.Items).Contains(new PyTuple([key])));
            if (count != 0) Unhashable(() => dict.Pop(key, true, "fallback"), "Disabled");
        }
        // values 是线性比较，不因查找对象不可 hash 而拒绝。
        var values = new PyDict(); values.Set("key", key);
        key.Class.SetAttr("__eq__", Fn("__eq__", ["self", "other"], args => ReferenceEquals(args[0], args[1])));
        Assert.True(new PyDictView(values, PyDictViewKind.Values).Contains(key));
    }

    [Fact]
    public void EmptyPopSkipsHashInAllThreeEmptyHistoriesAndPreservesKeyIdentity()
    {
        var key = Disabled(); object fallback = new PyList();
        foreach (int state in new[] { 0, 2, 3 })
        {
            var dict = DictState(state); Assert.Equal(0, dict.Count);
            Assert.Same(fallback, dict.Pop(key, true, fallback));
            Assert.Same(PyNone.Instance, dict.Pop(key, true, PyNone.Instance));
            var error = Assert.Throws<PyKeyError>(() => dict.Pop(key, false, null));
            Assert.Same(key, Assert.Single(error.Args));
            // 非 None 但会抛异常的 hash 同样不能在空 pop 路径执行。
            key.Class.SetAttr("__hash__", Fn("__hash__", ["self"], _ => throw new InvalidOperationException("空 pop 调用了 hash")));
            Assert.Same(fallback, dict.Pop(key, true, fallback));
            Assert.Same(key, Assert.Single(Assert.Throws<PyKeyError>(() => dict.Pop(key, false, null)).Args));
            key.Class.SetAttr("__hash__", PyNone.Instance);
        }
    }

    [Fact]
    public void SetAndFrozenSetRejectNoneForEmptyAndPopulatedMembershipAndConstruction()
    {
        var key = Disabled();
        for (int state = 0; state < 4; state++)
        {
            var set = state == 0 ? new PySet() : new PySet([1L]);
            // set.clear 的 Python 方法入口仍未开放；用公开载体制造 CLR 清空历史。
            if (state == 2) set.Items.Clear();
            if (state == 3) set.Remove(1L);
            int count = set.Count;
            Unhashable(() => set.Contains(key), "Disabled"); Unhashable(() => set.Remove(key), "Disabled");
            Unhashable(() => set.Discard(key), "Disabled"); Unhashable(() => set.Add(key), "Disabled");
            Assert.Equal(count, set.Count);
        }
        foreach (var frozen in new[] { new PyFrozenSet([]), new PyFrozenSet([1L]) })
            Unhashable(() => PyOps.Contains(frozen, key), "Disabled");
        Unhashable(() => { _ = new PySet([key]); }, "Disabled");
        Unhashable(() => { _ = new PyFrozenSet([key]); }, "Disabled");
    }

    [Fact]
    public void OrdinaryKeysPreserveSingleHashIdentityAndNumericCrossLookup()
    {
        int calls = 0; var cls = Make("NumericKey");
        cls.SetAttr("__hash__", Fn("__hash__", ["self"], _ => { calls++; return 1L; }));
        cls.SetAttr("__eq__", Fn("__eq__", ["self", "other"], args =>
            args[1] is PyInstance other && ReferenceEquals(other.Class, cls) || PyHash.Equal(args[1], 1L)));
        Finish(cls); var key = New(cls); var equal = New(cls); var dict = new PyDict();
        dict.Set(key, "first"); Assert.Equal(1, calls); calls = 0;
        Assert.Equal("first", dict.Get(key)); Assert.Equal(1, calls); calls = 0;
        dict.Set(equal, "second"); Assert.Equal(1, calls); Assert.Same(key, Assert.Single(dict.Keys));
        foreach (object number in new object[] { true, 1L, 1.0 })
        {
            Assert.Equal("second", dict.Get(number));
            var builtin = new PyDict(); builtin.Set(number, "builtin"); Assert.Equal("builtin", builtin.Get(key));
            Assert.True(new PySet([number]).Contains(key)); Assert.True(new PySet([key]).Contains(number));
        }
        var set = new PySet(); calls = 0; set.Add(key); Assert.Equal(1, calls);
        calls = 0; Assert.True(set.Contains(key)); Assert.Equal(1, calls);
    }

    [Fact]
    public void FrozenDataclassDistinguishesOwnEqNoneFromExplicitNoneBeforeGeneratedEq()
    {
        for (int variant = 0; variant < 5; variant++)
        {
            var cls = Make("Frozen");
            if (variant is 1 or 2) cls.SetAttr("__eq__", Eq());
            if (variant is 2 or 3) cls.SetAttr("__hash__", PyNone.Instance);
            if (variant == 4) cls.SetAttr("__hash__", Hash(41));
            Finish(cls); PyOps.DeclareDataclass(cls, ["value"], true); PyOps.ApplyDataclass(cls, true);
            object value = cls.Invoke([4L], [], [])!;
            if (variant == 3) Unhashable(() => PyHash.Of(value), "Frozen");
            else Assert.Equal(variant == 4 ? 41L : PyHash.Of(new PyTuple([4L])), PyHash.Of(value));
        }
        var mutable = Make("Mutable"); Finish(mutable);
        PyOps.DeclareDataclass(mutable, ["value"], false); PyOps.ApplyDataclass(mutable, false);
        Unhashable(() => PyHash.Of(mutable.Invoke([4L], [], [])), "Mutable");
        // field占位先删除/替换，之后才判断显式hash；不能在字段整理之前快照。
        object? field = PyDataclasses.CreateAttributes()["field"];
        var removed = Make("HashFieldRemoved");
        removed.SetAttr("__hash__", PyOps.CallValue(field, [], ["init", "repr", "compare"], [false, false, false]));
        Finish(removed); PyOps.DeclareDataclass(removed, ["__hash__"], true); PyOps.ApplyDataclass(removed, true);
        Assert.Equal(PyHash.Of(new PyTuple([])), PyHash.Of(removed.Invoke([], [], [])));
        var replaced = Make("HashFieldNone");
        replaced.SetAttr("__hash__", PyOps.CallValue(field, [], ["default", "init", "repr", "compare"],
            [PyNone.Instance, false, false, false]));
        Finish(replaced); PyOps.DeclareDataclass(replaced, ["__hash__"], true); PyOps.ApplyDataclass(replaced, true);
        Unhashable(() => PyHash.Of(replaced.Invoke([], [], [])), "HashFieldNone");
    }

    [Fact]
    public void DataclassRejectsImplicitlyUnhashableDefaultWithoutCallingItsProtocols()
    {
        var defaultValue = Disabled();
        defaultValue.Class.SetAttr("__repr__", Fn("__repr__", ["self"], _ =>
            throw new InvalidOperationException("默认值验证不得调用 repr")));
        var cls = Make("BadDefault"); cls.SetAttr("value", defaultValue); Finish(cls);
        PyOps.DeclareDataclass(cls, ["value"], false);
        Assert.Throws<PyValueError>(() => PyOps.ApplyDataclass(cls, false));
        Assert.False(cls.Attributes.ContainsKey("__init__"));
    }

    [Fact]
    public void StringAndSpecialLayoutsAreNotOrdinaryHashSlots()
    {
        var text = (PyClass)PyOps.MakeClass("Text", [PyType.Get("str")], null); Finish(text);
        object value = text.Invoke(["payload"], [], [])!; Assert.Equal(PyHash.Of("payload"), PyHash.Of(value));
        Assert.Throws<PyNotImplementedError>(() => text.SetAttr("__hash__", PyNone.Instance));
        Assert.Throws<PyNotImplementedError>(() => text.SetAttr("__eq__", Eq()));
        Assert.Throws<PyNotImplementedError>(() => text.DeleteAttr("__hash__"));
        var bad = (PyClass)PyOps.MakeClass("BadText", [PyType.Get("str")], null);
        bad.SetAttr("__eq__", Eq()); Assert.Throws<PyNotImplementedError>(() => Finish(bad));
        Assert.False(bad.Attributes.ContainsKey("__hash__"));
        // 真实 TestCase 专用链，不反射或伪造内部布局标志。
        var special = Make("Special", PyUnittest.TestCase); special.SetAttr("__eq__", Eq()); Finish(special);
        Assert.False(special.Attributes.ContainsKey("__hash__"));
        special.SetAttr("__hash__", Hash(47)); Assert.Equal(47L, PyHash.Of(New(special)));
    }

    [Fact]
    public void UnfinalizedManualClassesRetainTheirExistingCustomHashCapability()
    {
        foreach (var cls in new[] { new PyClass("Manual"), Make("Unfinished") })
        {
            cls.SetAttr("__eq__", Eq()); var value = new PyInstance(cls);
            long identity = PyHash.Of(value); Assert.Equal(identity, PyHash.Of(value));
            Assert.False(cls.Attributes.ContainsKey("__hash__"));
            cls.SetAttr("__hash__", Hash(53)); Assert.Equal(53L, PyHash.Of(value));
            cls.SetAttr("__hash__", Hash(59)); Assert.Equal(59L, PyHash.Of(value));
        }
    }

    [Fact]
    public void NoneErrorsPrepareCallerAndContextWithoutChangingUserExceptionIdentity()
    {
        var value = Disabled();
        value.Class.SetAttr("__repr__", Fn("__repr__", ["self"], _ =>
            throw new InvalidOperationException("hash 类型名不得执行 repr")));
        Caller(outer =>
        {
            Prepared(Assert.Throws<PyTypeError>(() => PyHash.Of(value)), outer);
            Prepared(Assert.Throws<PyTypeError>(() => new PyDict().Get(value)), outer);
            Prepared(Assert.Throws<PyTypeError>(() => new PySet().Contains(value)), outer);
        });
        var failure = new PyLookupError("hash body"); Caller(_ => PyErrors.PrepareRaise(failure));
        var frames = failure.Frames; var context = failure.Context;
        value.Class.SetAttr("__hash__", Fn("__hash__", ["self"], _ => throw failure));
        Assert.Same(failure, Assert.Throws<PyLookupError>(() => PyHash.Of(value)));
        Assert.Same(frames, failure.Frames); Assert.Same(context, failure.Context);
        value.Class.SetAttr("__hash__", 7L);
        var noncallable = Assert.Throws<PyTypeError>(() => PyHash.Of(value));
        Assert.Contains("not callable", noncallable.Message);
        Assert.DoesNotContain("unhashable", noncallable.Message);
    }
}
