// typing.IO 有限族的布局、完成态与冷反射边界；只构造 Python 可达状态，不污染 CLR 私有槽。
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingIoTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingIoLayoutTests
{
    private static object? ObjectNew(PyClass cls) => Call(Get(PyBaseObject.Type, "__new__"), cls);
    private static void Initialize(PyInstance instance, string name, object? value) =>
        Internal(typeof(PyObject), "InitializeDataclassField", instance, name, value);
    private static PyFunction Fn(string name, string[] parameters, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, new object?[parameters.Length], parameters.Length, body);
    private static void Unchanged(PyClass cls, Dictionary<string, object?> before, int slots, long version)
    {
        Assert.Equal(slots, cls.InstanceSlotCount);
        Assert.Equal(version, cls.Version);
        Assert.Equal(before.Keys, cls.Attributes.Keys);
        foreach (var (name, value) in before) Assert.Same(value, cls.Attributes[name]);
    }

    [Fact]
    public void ExactRootsNeverAcquireSlotsThroughOrdinaryPicOrInitializerWrites()
    {
        foreach (string name in new[] { "IO", "BinaryIO", "TextIO" })
        {
            PyClass cls = Root(name);
            PyInstance instance = Instance(cls);
            Assert.Equal(0, cls.InstanceSlotCount);
            long version = cls.Version;
            for (int round = 0; round < 3; round++)
            {
                string missing = $"'{name}' object has no attribute 'extra'";
                string readOnly = $"'{name}' object attribute 'read' is read-only";
                Assert.Equal(missing, Assert.Throws<PyAttributeError>(() => Get(instance, "extra")).PyMessage);
                Assert.Equal(missing, Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "extra", round)).PyMessage);
                Assert.Equal(missing, Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(instance, "extra")).PyMessage);
                Assert.Equal(readOnly, Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "read", round)).PyMessage);
                Assert.Equal(readOnly, Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(instance, "read")).PyMessage);
                Assert.Equal(missing, Assert.Throws<PyAttributeError>(() => Initialize(instance, "extra", round)).PyMessage);
                Assert.Equal(readOnly, Assert.Throws<PyAttributeError>(() => Initialize(instance, "read", round)).PyMessage);
                Assert.Equal(missing, Assert.Throws<PyAttributeError>(() => cls.GetOrAddInstanceSlot("extra")).PyMessage);
                Assert.Equal(readOnly, Assert.Throws<PyAttributeError>(() => cls.GetOrAddInstanceSlot("read")).PyMessage);
                // 同站点固定属性名，避免把 PIC 调用点约束之外的 C# 输入当作反例。
                Assert.Equal(missing, Assert.Throws<PyAttributeError>(() => PyPic.SetAttr(3101, instance, "extra", round)).PyMessage);
                Assert.Equal(readOnly, Assert.Throws<PyAttributeError>(() => PyPic.SetAttr(3102, instance, "read", round)).PyMessage);
            }
            Assert.IsType<PyBoundMethod>(Get(instance, "read"));
            Assert.False(cls.TryGetInstanceSlot("extra", out _));
            Assert.False(cls.TryGetInstanceSlot("read", out _));
            Assert.Equal(0, cls.InstanceSlotCount);
            Assert.Equal(version, cls.Version);
            Assert.Equal($"'{name}' object has no attribute '__dict__'",
                Assert.Throws<PyAttributeError>(() => Get(instance, "__dict__")).PyMessage);
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "__dict__", new PyDict()));
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(instance, "__class__", cls));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(instance, "__class__"));
            Assert.Equal(0, cls.InstanceSlotCount);
        }
    }

    [Fact]
    public void OriginalPropertiesRejectWritesAndDeletesBeforeAllocatingSlots()
    {
        foreach (string root in new[] { "IO", "BinaryIO", "TextIO" })
        {
            PyClass cls = Root(root);
            PyInstance instance = Instance(cls);
            string[] names = root == "TextIO"
                ? ["mode", "name", "closed", "buffer", "encoding", "errors", "line_buffering", "newlines"]
                : ["mode", "name", "closed"];
            foreach (string name in names)
            {
                Assert.Same(PyNone.Instance, Get(instance, name));
                Assert.Equal($"property '{name}' of '{root}' object has no setter",
                    Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, name, 1L)).PyMessage);
                Assert.Equal($"property '{name}' of '{root}' object has no deleter",
                    Assert.Throws<PyAttributeError>(() => PyObject.DelAttr(instance, name)).PyMessage);
                Assert.Throws<PyAttributeError>(() => Initialize(instance, name, 1L));
                Assert.Equal(0, cls.InstanceSlotCount);
            }
        }
    }

    [Fact]
    public void RootPropertyReplacementKeepsSetterAndDeleterAheadOfEmptyLayoutGuard()
    {
        PyClass cls = Root("IO");
        PyInstance instance = Instance(cls);
        object? original = cls.Attributes["mode"];
        object token = new PyList(); object? seen = null; int deleted = 0;
        var setter = Fn("set_mode", ["self", "value"], args =>
        { Assert.Same(instance, args[0]); seen = args[1]; return PyNone.Instance; });
        var deleter = Fn("del_mode", ["self"], args =>
        { Assert.Same(instance, args[0]); deleted++; return PyNone.Instance; });
        try
        {
            var first = Assert.IsType<PyProperty>(original);
            var withSetter = Assert.IsType<PyProperty>(Call(Get(first, "setter"), setter));
            var replacement = Assert.IsType<PyProperty>(Call(Get(withSetter, "deleter"), deleter));
            Assert.Null(first.Setter); Assert.Null(first.Deleter);
            Assert.Same(first.Getter, replacement.Getter);
            cls.SetAttr("mode", replacement);
            PyObject.SetAttr(instance, "mode", token); Assert.Same(token, seen);
            Initialize(instance, "mode", 12L); Assert.Equal(12L, seen);
            PyPic.SetAttr(3103, instance, "mode", token); Assert.Same(token, seen);
            PyObject.DelAttr(instance, "mode"); Assert.Equal(1, deleted);
            Assert.Equal(0, cls.InstanceSlotCount);
            cls.SetAttr("mode", new PyProperty(first.Getter,
                Fn("write_forbidden", ["self", "value"], args =>
                { PyObject.SetAttr(args[0], "extra", args[1]); return PyNone.Instance; })));
            Assert.Throws<PyAttributeError>(() => PyObject.SetAttr(instance, "mode", token));
            Assert.Equal(0, cls.InstanceSlotCount);
        }
        finally { cls.SetAttr("mode", original); }
        Assert.Same(original, Get(cls, "mode"));
        Assert.Same(PyNone.Instance, Get(instance, "mode"));
    }

    [Fact]
    public void UnslottedChildrenRemainWritableAndPicInvalidatesWhenPropertyReplacesSlot()
    {
        PyClass cls = Child("Writable", Root("IO"));
        PyInstance instance = Instance(cls);
        PyClass grandchild = Child("WritableGrandchild", cls);
        PyInstance second = Instance(grandchild);
        Assert.Empty(Assert.IsType<PyTuple>(Get(cls, "__slots__")).Items);
        foreach (PyInstance value in new[] { instance, second })
        {
            PyObject.SetAttr(value, "extra", 1L);
            PyObject.SetAttr(value, "read", 2L);
            Assert.Equal(1L, Get(value, "extra")); Assert.Equal(2L, Get(value, "read"));
            PyObject.DelAttr(value, "read"); Assert.IsType<PyBoundMethod>(Get(value, "read"));
            PyObject.DelAttr(value, "extra"); Assert.Throws<PyAttributeError>(() => Get(value, "extra"));
            // 类元数据禁写表不应错误套用到普通派生实例的普通字段。
            PyObject.SetAttr(value, "__name__", "instance-name");
            PyObject.SetAttr(value, "__bases__", 9L);
            Assert.Equal("instance-name", Get(value, "__name__")); Assert.Equal(9L, Get(value, "__bases__"));
            Assert.Throws<PyNotImplementedError>(() => Get(value, "__dict__"));
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(value, "__dict__", new PyDict()));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(value, "__dict__"));
        }
        for (int index = 0; index < 3; index++) PyPic.SetAttr(3101, instance, "extra", (long)index);
        Assert.Equal(2L, Get(instance, "extra"));
        cls.SetAttr("extra", new PyProperty(Fn("get_extra", ["self"], args => Get(args[0], "_value")),
            Fn("set_extra", ["self", "value"], args =>
            { PyObject.SetAttr(args[0], "_value", args[1]); return PyNone.Instance; }),
            Fn("del_extra", ["self"], args => { PyObject.DelAttr(args[0], "_value"); return PyNone.Instance; })));
        PyPic.SetAttr(3101, instance, "extra", 7L);
        Assert.Equal(7L, Get(instance, "extra")); Assert.Equal(7L, Get(instance, "_value"));
        PyObject.DelAttr(instance, "extra"); Assert.Throws<PyAttributeError>(() => Get(instance, "_value"));
        cls.DeleteAttr("extra");
        PyPic.SetAttr(3101, instance, "extra", 8L); Assert.Equal(8L, Get(instance, "extra"));
        Assert.Throws<PyAttributeError>(() => PyPic.SetAttr(3101, Instance(Root("IO")), "extra", 9L));
        Assert.Equal(0, Root("IO").InstanceSlotCount);
    }

    [Fact]
    public void FinalizationAndObjectNewShareTheSameReadinessBoundary()
    {
        PyClass cls = Child("NotReady", Root("IO"), finish: false);
        Assert.Throws<PyNotImplementedError>(() => Instance(cls));
        Assert.Throws<PyNotImplementedError>(() => ObjectNew(cls));
        Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("TooSoon", [cls], null));
        PyOps.FinalizeOrdinaryClass(cls);
        Assert.Same(cls, Instance(cls).Class);
        Assert.Same(cls, Assert.IsType<PyInstance>(ObjectNew(cls)).Class);
        foreach (string name in new[] { "IO", "BinaryIO", "TextIO" })
            Assert.Same(Root(name), Assert.IsType<PyInstance>(ObjectNew(Root(name))).Class);
        PyClass child = Child("AfterReady", cls);
        Assert.Same(child, Instance(child).Class);
    }

    [Fact]
    public void ForbiddenDefinitionsRejectBeforeWrappingNewAndNeverBecomeConstructible()
    {
        // 定义期qualname已在Runtime metadata合同中闭合；late reserved仍由下一项全覆盖。
        foreach (string name in new[] { "__slots__", "__init_subclass__", "__class_getitem__", "__mro_entries__", "__prepare__",
            "__name__" })
        {
            int hookCalls = 0;
            PyClass cls = Child("InvalidDefinition", Root("IO"), finish: false);
            PyFunction allocator = Fn("__new__", ["cls"], args => ObjectNew((PyClass)args[0]!));
            cls.SetAttr("__new__", allocator);
            cls.SetAttr(name, name is "__name__" or "__qualname__" ? "FalseName" : name == "__slots__" ? new PyTuple([])
                : Fn(name, ["self"], _ => { hookCalls++; return PyNone.Instance; }));
            var before = new Dictionary<string, object?>(cls.Attributes); long version = cls.Version;
            Assert.Throws<PyNotImplementedError>(() => PyOps.FinalizeOrdinaryClass(cls));
            Unchanged(cls, before, 0, version);
            Assert.Same(allocator, cls.Attributes["__new__"]);
            Assert.Throws<PyNotImplementedError>(() => Instance(cls));
            Assert.Throws<PyNotImplementedError>(() => ObjectNew(cls));
            Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("InvalidChild", [cls], null));
            Assert.Equal(0, hookCalls);
        }
    }

    [Fact]
    public void ReservedLateClassMutationsFailBeforeChangingAttributesOrVersion()
    {
        foreach (PyClass cls in new[] { Root("IO"), Root("BinaryIO"), Root("TextIO"), Child("Reserved", Root("TextIO")) })
        {
            var before = new Dictionary<string, object?>(cls.Attributes); long version = cls.Version;
            foreach (string name in new[] { "__base__", "__bases__", "__mro__", "mro", "__orig_bases__", "__parameters__",
                "__slots__", "__class__", "__dict__", "__init_subclass__", "__class_getitem__", "__mro_entries__", "__prepare__",
                "__name__", "__qualname__" })
            {
                Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(cls, name, PyNone.Instance));
                Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(cls, name));
            }
            Assert.Equal($"cannot delete '__module__' attribute of type '{cls.Name}'",
                Assert.Throws<PyTypeError>(() => PyObject.DelAttr(cls, "__module__")).PyMessage);
            Unchanged(cls, before, 0, version);
        }
    }

    [Fact]
    public void OrdinaryAnnotationsCanBeDeclaredButDataclassApplicationIsAtomicAndUnsupported()
    {
        PyClass cls = Child("Annotated", Root("IO"), finish: false);
        var annotations = new PyDict(); annotations.Set("value", PyType.Get("int"));
        cls.SetAttr("__annotations__", annotations); cls.SetAttr("value", 11L);
        PyOps.DeclareDataclass(cls, ["value"], true);
        PyOps.FinalizeOrdinaryClass(cls);
        PyInstance instance = Instance(cls);
        // Declare 仅记录注解，不应把普通派生类冻结或启用数据类快路径。
        PyObject.SetAttr(instance, "value", 12L); Assert.Equal(12L, Get(instance, "value"));
        Initialize(instance, "extra", 13L); Assert.Equal(13L, Get(instance, "extra"));
        foreach (PyClass target in new[] { Root("IO"), Root("BinaryIO"), Root("TextIO"), cls })
        {
            var before = new Dictionary<string, object?>(target.Attributes);
            int slots = target.InstanceSlotCount; long version = target.Version;
            Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(target, false));
            Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(target, true));
            Unchanged(target, before, slots, version);
            Assert.False(target.Attributes.ContainsKey("__dataclass_fields__"));
            Assert.Throws<PyTypeError>(() => target.Invoke([1L], [], []));
            Assert.Same(target, Instance(target).Class);
        }
        PyObject.SetAttr(instance, "value", 14L); Assert.Equal(14L, Get(instance, "value"));
        // field() 会在通用 Apply 中被删除或替换；族拒绝必须发生在那一步之前。
        PyClass fieldClass = Child("DeclaredField", Root("IO"));
        object? field = Call(PyDataclasses.CreateAttributes()["field"]);
        fieldClass.SetAttr("value", field); PyOps.DeclareDataclass(fieldClass, ["value"], false);
        var snapshot = new Dictionary<string, object?>(fieldClass.Attributes); long fieldVersion = fieldClass.Version;
        Assert.Throws<PyNotImplementedError>(() => PyOps.ApplyDataclass(fieldClass, false));
        Unchanged(fieldClass, snapshot, 0, fieldVersion);
        Assert.Same(field, fieldClass.Attributes["value"]);
    }

    [Fact]
    public void ReflectionDoesNotInventGenericAncestorsAndBuiltinFallbacksDoNotHideNi()
    {
        PyClass io = Root("IO");
        PyClass child = Child("ReflectionChild", Root("BinaryIO"));
        foreach (PyClass cls in new[] { Root("BinaryIO"), Root("TextIO"), child })
        {
            Assert.Same(cls.BaseClass, Get(cls, "__base__"));
            object? bases = Get(cls, "__bases__");
            Assert.Equal<object?>([cls.BaseClass], Assert.IsType<PyTuple>(bases).Items);
            Assert.Same(bases, Get(cls, "__bases__"));
        }
        foreach (string name in new[] { "__base__", "__bases__", "__parameters__" })
            Assert.Throws<PyNotImplementedError>(() => Get(io, name));
        foreach (string root in new[] { "BinaryIO", "TextIO" })
        {
            object? parameters = Get(Root(root), "__parameters__");
            Assert.Empty(Assert.IsType<PyTuple>(parameters).Items);
            Assert.Same(parameters, Get(Root(root), "__parameters__"));
        }
        Assert.Throws<PyNotImplementedError>(() => Get(child, "__parameters__"));
        foreach (PyClass cls in new[] { io, Root("BinaryIO"), Root("TextIO"), child })
        {
            PyInstance instance = Instance(cls);
            foreach (string name in new[] { "__mro__", "mro", "__orig_bases__", "__dict__", "__init_subclass__", "__class_getitem__" })
            {
                Assert.Throws<PyNotImplementedError>(() => Get(cls, name));
                Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("getattr", [cls, name, PyNone.Instance], [], []));
                Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("hasattr", [cls, name], [], []));
            }
            foreach (string name in new[] { "__orig_bases__", "__init_subclass__", "__class_getitem__" })
                Assert.Throws<PyNotImplementedError>(() => Get(instance, name));
            foreach (string name in new[] { "__base__", "__bases__", "__mro__", "__name__", "__qualname__" })
                Assert.Throws<PyAttributeError>(() => Get(instance, name));
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("dir", [cls], [], []));
            Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("dir", [instance], [], []));
        }
        var super = new PySuper(child, Instance(child));
        foreach (string name in new[] { "__dict__", "__base__", "__bases__", "__mro__", "mro", "__orig_bases__",
            "__parameters__", "__init_subclass__", "__class_getitem__" })
            Assert.Throws<PyNotImplementedError>(() => Get(super, name));
        child.SetAttr("__dir__", Fn("__dir__", ["self"], _ => new PyList(["z", "a"])));
        Assert.Equal<object?>(["a", "z"], Assert.IsType<PyList>(PyOps.CallBuiltin("dir", [Instance(child)], [], [])).Items);
    }

    [Fact]
    public void OrdinaryInstanceFieldsCanShadowInheritedGenericMetadataWithoutChangingClassBoundary()
    {
        PyClass cls = Child("MetadataFields", Root("IO"));
        PyInstance instance = Instance(cls);
        foreach (string name in new[] { "__parameters__", "__orig_bases__", "__init_subclass__", "__class_getitem__" })
        {
            Assert.Throws<PyNotImplementedError>(() => Get(instance, name));
            object token = new PyList();
            PyObject.SetAttr(instance, name, token);
            Assert.Same(token, Get(instance, name));
            Assert.Throws<PyNotImplementedError>(() => Get(cls, name));
            Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(cls, name, token));
            Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(cls, name));
            PyObject.DelAttr(instance, name);
            Assert.Throws<PyNotImplementedError>(() => Get(instance, name));
        }
    }

    [Fact]
    public void OrdinaryInitOverridesAndSuperCallsRemainAvailableToCompletedChildren()
    {
        PyClass cls = Child("CustomInit", Root("BinaryIO"), finish: false);
        object token = new PyList();
        cls.SetAttr("__init__", Fn("__init__", ["self", "value"], args =>
        { PyObject.SetAttr(args[0], "value", args[1]); return PyNone.Instance; }));
        cls.SetAttr("read", Fn("read", ["self"], args =>
        {
            Assert.Same(PyNone.Instance, Call(Get(new PySuper(cls, args[0]), "read")));
            return Get(args[0], "value");
        }));
        PyOps.FinalizeOrdinaryClass(cls);
        var instance = Assert.IsType<PyInstance>(cls.Invoke([], ["value"], [token]));
        Assert.Same(token, Call(Get(instance, "read")));
        cls.SetAttr("read", Fn("replacement", ["self"], _ => 9L));
        Assert.Equal(9L, Call(Get(instance, "read")));
        cls.DeleteAttr("read");
        Assert.Same(PyNone.Instance, Call(Get(instance, "read")));
        Assert.Same(PyNone.Instance, Call(Get(instance, "__enter__")));
        Assert.Same(PyNone.Instance, Call(Get(instance, "__exit__"), PyNone.Instance, PyNone.Instance, PyNone.Instance));
    }

    [Fact]
    public void LateRootModuleValuesAreRealAndSharedByClassInstanceAndTypingDisplay()
    {
        foreach (string name in new[] { "IO", "BinaryIO", "TextIO" })
        {
            PyClass cls = Root(name); PyInstance instance = Instance(cls);
            object? original = Get(cls, "__module__");
            object? alias = PyOps.GetItem(PyTyping.CreateAttributes()["List"], cls);
            try
            {
                foreach (object module in new object[] { "changed.module", "builtins", PyNone.Instance, false, new PyList() })
                {
                    PyObject.SetAttr(cls, "__module__", module);
                    Assert.Same(module, Get(cls, "__module__"));
                    Assert.Same(module, Get(instance, "__module__"));
                    string display = module is string text && text != "builtins" ? text + "." + name : name;
                    Assert.Equal($"<class '{display}'>", PyRepr.Repr(cls));
                    Assert.StartsWith($"<{display} object at 0x", PyRepr.Repr(instance));
                    Assert.Equal($"typing.List[{display}]", PyRepr.Repr(alias));
                }
            }
            finally { PyObject.SetAttr(cls, "__module__", original); }
            Assert.Equal($"<class 'typing.{name}'>", PyRepr.Repr(cls));
        }
    }

    [Fact]
    public void UnknownClassSourceOnlyBlocksModuleDependentMetadata()
    {
        long depth = PyTraceback.PushFrame("unknown_typing_io.py", "static");
        try
        {
            var cls = (PyClass)PyOps.MakeClass("UnknownOrigin", [Root("IO")], null);
            PyOps.FinalizeOrdinaryClass(cls);
            PyInstance instance = Instance(cls);
            Assert.Same(cls, PyObject.TypeOf(instance));
            Assert.Same(cls, Get(instance, "__class__"));
            Assert.True(PyObject.IsInstance(instance, Root("IO")));
            PyObject.SetAttr(instance, "value", 3L); Assert.Equal(3L, Get(instance, "value"));
            Assert.Equal(PyHash.Of(instance), PyHash.Of(instance));
            Assert.Throws<PyNotImplementedError>(() => Get(cls, "__module__"));
            Assert.Throws<PyNotImplementedError>(() => Get(instance, "__module__"));
            Assert.Throws<PyNotImplementedError>(() => PyRepr.Repr(cls));
            Assert.Throws<PyNotImplementedError>(() => PyRepr.Repr(instance));
            cls.SetAttr("__module__", "explicit.module");
            Assert.Equal("explicit.module", Get(instance, "__module__"));
            Assert.Equal("<class 'explicit.module.UnknownOrigin'>", PyRepr.Repr(cls));
            Assert.StartsWith("<explicit.module.UnknownOrigin object at 0x", PyRepr.Repr(instance));
        }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void SubscriptionKeepsUnsupportedGenericSeparateFromClosedRootTypeErrors()
    {
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(Root("IO"), PyType.Get("str")));
        Assert.Throws<PyNotImplementedError>(() => PyOps.GetItem(Child("NotGeneric", Root("IO")), PyType.Get("str")));
        foreach (string name in new[] { "BinaryIO", "TextIO" })
            Assert.Equal($"<class 'typing.{name}'> is not a generic class",
                Assert.Throws<PyTypeError>(() => PyOps.GetItem(Root(name), PyType.Get("str"))).PyMessage);
    }
}
