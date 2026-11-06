// typing.IO 的公共稳定身份与真实方法表；公开导出必须复用已完成的内部工厂。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingIoTests
{
    // 与既有 typing 专项一致，测试宿主须显式登记原生模块，不能依赖其它测试先运行。
    public PyTypingIoTests() => PyNativeModules.Register();

    // 只通过测试反射进入内部工厂，不添加公开 API；真实 Python 异常必须原样抛出。
    internal static object? Internal(Type owner, string name, params object?[] arguments)
    {
        try
        {
            return owner.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static PyClass Root(string name) => (PyClass)Internal(
        typeof(PyClass).Assembly.GetType("RyuPy.Runtime.PyTypingIo")!, "Root", name)!;
    internal static object? Get(object? value, string name) => PyObject.GetAttr(value, name);
    internal static object? Call(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);
    internal static PyInstance Instance(PyClass cls) => Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    internal static PyClass Child(string name, PyClass parent, bool finish = true)
    {
        var cls = (PyClass)PyOps.MakeClass(name, [parent], null);
        cls.SetAttr("__module__", "__main__");
        if (finish) PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static bool Builtin(string name, params object?[] args) => (bool)PyOps.CallBuiltin(name, args, [], [])!;

    private sealed record MethodSpec(string Owner, string Name, string[] Parameters, int Required,
        object?[] Defaults, bool Property = false);

    // 独立列出 CP 固定方法表，不能从待测签名反向生成预期值。
    private static IEnumerable<MethodSpec> Methods()
    {
        foreach (string name in new[] { "mode", "name", "closed" })
            yield return new("IO", name, ["self"], 1, [], true);
        foreach (string name in new[] { "close", "fileno", "flush", "isatty", "readable", "seekable",
            "tell", "writable", "__enter__" })
            yield return new("IO", name, ["self"], 1, []);
        yield return new("IO", "read", ["self", "n"], 1, [-1L]);
        yield return new("IO", "readline", ["self", "limit"], 1, [-1L]);
        yield return new("IO", "readlines", ["self", "hint"], 1, [-1L]);
        yield return new("IO", "seek", ["self", "offset", "whence"], 2, [0L]);
        yield return new("IO", "truncate", ["self", "size"], 1, [PyNone.Instance]);
        yield return new("IO", "write", ["self", "s"], 2, []);
        yield return new("IO", "writelines", ["self", "lines"], 2, []);
        yield return new("IO", "__exit__", ["self", "type", "value", "traceback"], 4, []);
        yield return new("BinaryIO", "write", ["self", "s"], 2, []);
        yield return new("BinaryIO", "__enter__", ["self"], 1, []);
        foreach (string name in new[] { "buffer", "encoding", "errors", "line_buffering", "newlines" })
            yield return new("TextIO", name, ["self"], 1, [], true);
        yield return new("TextIO", "__enter__", ["self"], 1, []);
    }

    private static PyFunction Function(MethodSpec spec)
    {
        object? own = Root(spec.Owner).Attributes[spec.Name];
        return spec.Property ? Assert.IsType<PyFunction>(Assert.IsType<PyProperty>(own).Getter)
            : Assert.IsType<PyFunction>(own);
    }

    [Fact]
    public void StableRootsArePublishedWithRealTypeIdentity()
    {
        foreach (string name in new[] { "IO", "BinaryIO", "TextIO" })
        {
            PyClass cls = Root(name);
            PyInstance instance = Instance(cls);
            Assert.Same(cls, Root(name));
            Assert.Same(cls, PyTyping.CreateAttributes()[name]);
            Assert.Same(cls, Get(PyModules.Import("typing"), name));
            Assert.Same(PyType.Get("type"), PyObject.TypeOf(cls));
            Assert.Same(PyType.Get("type"), Get(cls, "__class__"));
            Assert.Same(cls, PyObject.TypeOf(instance));
            Assert.Same(cls, Get(instance, "__class__"));
            Assert.Equal(name, Get(cls, "__name__"));
            Assert.Equal(name, Get(cls, "__qualname__"));
            Assert.Equal("typing", Get(cls, "__module__"));
            Assert.Equal("typing", Get(instance, "__module__"));
            Assert.Equal($"<class 'typing.{name}'>", PyRepr.Repr(cls));
            Assert.StartsWith($"<typing.{name} object at 0x", PyRepr.Repr(instance));
            Assert.EndsWith(">", PyRepr.Repr(instance));
            Assert.True(Builtin("isinstance", cls, PyType.Get("type")));
            Assert.True(Builtin("isinstance", instance, PyBaseObject.Type));
            Assert.True(Builtin("issubclass", cls, PyBaseObject.Type));
            Assert.True(Builtin("callable", cls));
            Assert.False(Builtin("callable", instance));
            Assert.Equal($"{name}() takes no arguments",
                Assert.Throws<PyTypeError>(() => cls.Invoke([1L], [], [])).PyMessage);
            Assert.Throws<PyTypeError>(() => cls.Invoke([], ["extra"], [1L]));
            object? slots = Get(cls, "__slots__");
            Assert.Empty(Assert.IsType<PyTuple>(slots).Items);
            Assert.Same(slots, Get(cls, "__slots__"));
            Assert.Same(slots, Get(instance, "__slots__"));
            Assert.Same(PyNone.Instance, Call(PyTyping.CreateAttributes()["get_origin"], cls));
            object? args = Call(PyTyping.CreateAttributes()["get_args"], cls);
            Assert.Empty(Assert.IsType<PyTuple>(args).Items);
            Assert.Same(args, Call(PyTyping.CreateAttributes()["get_args"], cls));
            object? alias = PyOps.GetItem(PyTyping.CreateAttributes()["List"], cls);
            Assert.Equal($"typing.List[typing.{name}]", PyRepr.Repr(alias));
        }
    }

    [Fact]
    public void NominalRelationsRejectSameNamesDuckTypesAndRealIoStreams()
    {
        PyClass io = Root("IO"), binary = Root("BinaryIO"), text = Root("TextIO");
        PyClass child = Child("BinaryChild", binary), grandchild = Child("BinaryGrandchild", child);
        foreach (PyClass cls in new[] { io, binary, text, child, grandchild })
        {
            Assert.True(Builtin("issubclass", cls, io));
            Assert.True(Builtin("isinstance", Instance(cls), io));
        }
        Assert.False(Builtin("issubclass", io, binary));
        Assert.False(Builtin("issubclass", binary, text));
        Assert.False(Builtin("issubclass", text, binary));
        Assert.True(Builtin("issubclass", grandchild, binary));
        Assert.False(Builtin("isinstance", Instance(text), binary));
        Assert.False(Builtin("isinstance", Instance(binary), text));
        var sameName = (PyClass)PyOps.MakeClass("IO", [], null);
        sameName.SetAttr("read", Get(io, "read"));
        sameName.SetAttr("write", Get(io, "write"));
        PyOps.FinalizeOrdinaryClass(sameName);
        object? bytes = PyIO.CreateBytesIO([], [], []), chars = PyIO.CreateStringIO([], [], []);
        try
        {
            foreach (PyClass root in new[] { io, binary, text })
            {
                foreach (object? value in new object?[] { Instance(sameName), new IO(), bytes, chars, 1L, PyNone.Instance })
                    Assert.False(Builtin("isinstance", value, root));
                foreach (object type in new[] { (object)sameName, PyType.Get("int"), PyObject.TypeOf(bytes), PyObject.TypeOf(chars), PyBaseObject.Type })
                    Assert.False(Builtin("issubclass", type, root));
                Assert.Throws<PyTypeError>(() => Builtin("issubclass", 1L, root));
            }
        }
        finally { Call(Get(bytes, "close")); Call(Get(chars, "close")); }
        // 同名 CLR 对象正是旧名称兜底会误认的真实输入，不伪造实例布局。
    }

    private sealed class IO { }

    [Fact]
    public void FamilyCallabilityUsesTypeSlotExistenceRatherThanInstanceAttributeOrSlotValue()
    {
        foreach (string name in new[] { "IO", "BinaryIO", "TextIO" })
            Assert.False(Builtin("callable", Instance(Root(name))));
        PyClass parent = Child("CallableParent", Root("IO"));
        PyClass child = Child("CallableChild", parent);
        PyInstance first = Instance(parent), second = Instance(child);
        PyFunction function = PyNativeFunctions.Create("__call__", ["self"], [null], 1, _ => PyNone.Instance);
        PyObject.SetAttr(first, "__call__", function);
        PyObject.SetAttr(second, "__call__", function);
        Assert.False(Builtin("callable", first));
        Assert.False(Builtin("callable", second));
        // CP 的 callable 只检查类型槽存在；None 和整数不在此处执行或验证可调用性。
        foreach (object value in new object[] { PyNone.Instance, 7L, function })
        {
            parent.SetAttr("__call__", value);
            Assert.True(Builtin("callable", first));
            Assert.True(Builtin("callable", second));
        }
        child.SetAttr("__call__", PyNone.Instance);
        parent.DeleteAttr("__call__");
        Assert.False(Builtin("callable", first));
        Assert.True(Builtin("callable", second));
        child.DeleteAttr("__call__");
        Assert.False(Builtin("callable", second));
        var ordinary = (PyClass)PyOps.MakeClass("OrdinaryCallability", [], null);
        PyOps.FinalizeOrdinaryClass(ordinary);
        // 仅锁定非族已有行为未被本切改动；此 True 不是 CP 兼容承诺。
        Assert.True(Builtin("callable", Instance(ordinary)));
    }

    [Fact]
    public void OwnMethodAndPropertyCountsAndInheritedIdentitiesAreExact()
    {
        var unique = new HashSet<PyFunction>(ReferenceEqualityComparer.Instance);
        foreach (var (name, methods, properties) in new[] { ("IO", 17, 3), ("BinaryIO", 2, 0), ("TextIO", 1, 5) })
        {
            PyClass cls = Root(name);
            Assert.Equal(methods, cls.Attributes.Values.Count(value => value is PyFunction));
            Assert.Equal(properties, cls.Attributes.Values.Count(value => value is PyProperty));
            foreach (MethodSpec spec in Methods().Where(spec => spec.Owner == name)) Assert.True(unique.Add(Function(spec)));
        }
        Assert.Equal(28, unique.Count);
        Assert.Equal(8, Methods().Count(spec => spec.Property));
        Assert.Same(Get(Root("IO"), "read"), Get(Root("BinaryIO"), "read"));
        Assert.Same(Get(Root("IO"), "write"), Get(Root("TextIO"), "write"));
        Assert.False(Root("BinaryIO").Attributes.ContainsKey("read"));
        Assert.False(Root("TextIO").Attributes.ContainsKey("write"));
        Assert.NotSame(Get(Root("IO"), "write"), Get(Root("BinaryIO"), "write"));
        Assert.NotSame(Get(Root("IO"), "__enter__"), Get(Root("BinaryIO"), "__enter__"));
        Assert.NotSame(Get(Root("IO"), "__enter__"), Get(Root("TextIO"), "__enter__"));
    }

    [Fact]
    public void EveryFunctionHasExactSignatureDefaultsAndSupportedMetadata()
    {
        foreach (MethodSpec spec in Methods())
        {
            PyFunction function = Function(spec);
            Assert.Equal(spec.Parameters, function.Signature.Names);
            Assert.Equal(spec.Required, function.Signature.RequiredCount);
            Assert.All(function.Signature.Kinds, kind => Assert.Equal(PyParamKind.PositionalOrKeyword, kind));
            Assert.Equal(spec.Parameters.Select((_, index) => index >= spec.Required), function.Signature.HasDefault);
            Assert.Equal(spec.Owner + "." + spec.Name, function.Signature.Name);
            Assert.Equal(spec.Name, Get(function, "__name__"));
            Assert.Equal(spec.Owner + "." + spec.Name, Get(function, "__qualname__"));
            Assert.Equal("typing", Get(function, "__module__"));
            Assert.Same(PyType.Get("function"), Get(function, "__class__"));
            Assert.Same(PyType.Get("function"), PyObject.TypeOf(function));
            Assert.Equal(true, Get(function, "__isabstractmethod__"));
            Assert.Same(PyNone.Instance, Get(function, "__doc__"));
            Assert.Same(PyNone.Instance, Get(function, "__kwdefaults__"));
            object? defaults = Get(function, "__defaults__");
            if (spec.Defaults.Length == 0) Assert.Same(PyNone.Instance, defaults);
            else Assert.Equal(spec.Defaults, Assert.IsType<PyTuple>(defaults).Items);
            Assert.Same(defaults, Get(function, "__defaults__"));
            Assert.Equal(spec.Defaults, function.Defaults[spec.Required..]);
            if (spec.Property)
            {
                object? property = Get(Root(spec.Owner), spec.Name);
                Assert.Same(function, Get(property, "fget"));
                Assert.Same(PyNone.Instance, Get(property, "fset"));
                Assert.Same(PyNone.Instance, Get(property, "fdel"));
            }
        }
    }

    [Fact]
    public void AllLegalCallShapesReturnNoneWithoutInspectingForeignSelfOrArguments()
    {
        var poison = new Poison();
        foreach (MethodSpec spec in Methods())
        {
            PyFunction function = Function(spec);
            object?[] all = Enumerable.Repeat<object?>(poison, spec.Parameters.Length).ToArray();
            for (int length = spec.Required; length <= all.Length; length++)
                Assert.Same(PyNone.Instance, function.Invoke(all[..length], [], []));
            Assert.Same(PyNone.Instance, function.Invoke([], spec.Parameters, all));
            Assert.Same(PyNone.Instance, function.Invoke(all[..1], spec.Parameters[1..], all[1..]));
            PyInstance instance = Instance(Root(spec.Owner));
            if (spec.Property) Assert.Same(PyNone.Instance, Get(instance, spec.Name));
            else
            {
                object? bound = Get(instance, spec.Name);
                Assert.Same(PyNone.Instance, Call(bound, all[1..spec.Required]));
                Assert.Same(PyNone.Instance, PyOps.CallValue(bound, [], spec.Parameters[1..], all[1..]));
            }
        }
        Assert.Equal(0, poison.Reads);
    }

    [Fact]
    public void BoundMethodsExposeRealReceiverFunctionOwnerAndDeclarationMetadata()
    {
        foreach (MethodSpec spec in Methods().Where(spec => !spec.Property))
        {
            PyClass owner = Root(spec.Owner), child = Child("BoundChild", owner);
            PyInstance instance = Instance(child);
            var method = Assert.IsType<PyBoundMethod>(Get(instance, spec.Name));
            Assert.Same(owner, method.Owner);
            Assert.Same(instance, Get(method, "__self__"));
            Assert.Same(Function(spec), Get(method, "__func__"));
            Assert.Same(PyType.Get("method"), Get(method, "__class__"));
            Assert.Same(PyType.Get("method"), PyObject.TypeOf(method));
            Assert.Equal("typing", Get(method, "__module__"));
            Assert.Equal(spec.Owner + "." + spec.Name, Get(method, "__qualname__"));
            Assert.Same(Function(spec), Get(child, spec.Name));
            var inherited = Assert.IsType<PyBoundMethod>(Get(new PySuper(child, instance), spec.Name));
            Assert.Same(owner, inherited.Owner);
            Assert.Same(Function(spec), inherited.Target);
            Assert.Same(instance, inherited.Self);
        }
    }

    [Fact]
    public void EverySignatureRejectsMissingExtraUnknownAndDuplicateArguments()
    {
        foreach (MethodSpec spec in Methods())
        {
            PyFunction function = Function(spec);
            object?[] all = Enumerable.Repeat<object?>(PyNone.Instance, spec.Parameters.Length).ToArray();
            Assert.Throws<PyTypeError>(() => function.Invoke([], [], []));
            Assert.Throws<PyTypeError>(() => function.Invoke([.. all, PyNone.Instance], [], []));
            Assert.Equal($"{spec.Owner}.{spec.Name}() got an unexpected keyword argument 'unknown'",
                Assert.Throws<PyTypeError>(() => function.Invoke(all, ["unknown"], [1L])).PyMessage);
            Assert.Equal($"{spec.Owner}.{spec.Name}() got multiple values for argument 'self'",
                Assert.Throws<PyTypeError>(() => function.Invoke(all, ["self"], [1L])).PyMessage);
            Assert.Equal($"{spec.Owner}.{spec.Name}() got multiple values for argument 'self'",
                Assert.Throws<PyTypeError>(() => function.Invoke([], ["self", "self"], [1L, 2L])).PyMessage);
        }
        object? read = Get(Instance(Root("BinaryIO")), "read");
        Assert.Equal("IO.read() takes from 1 to 2 positional arguments but 3 were given",
            Assert.Throws<PyTypeError>(() => Call(read, 1L, 2L)).PyMessage);
        Assert.Equal("IO.seek() takes from 2 to 3 positional arguments but 4 were given",
            Assert.Throws<PyTypeError>(() => Call(Get(Root("IO"), "seek"), 1L, 2L, 3L, 4L)).PyMessage);
        Assert.Equal("IO.close() takes 1 positional argument but 2 were given",
            Assert.Throws<PyTypeError>(() => Call(Get(Root("IO"), "close"), 1L, 2L)).PyMessage);
        object? exit = Get(Instance(Root("TextIO")), "__exit__");
        Assert.Equal("IO.__exit__() missing 3 required positional arguments: 'type', 'value', and 'traceback'",
            Assert.Throws<PyTypeError>(() => Call(exit)).PyMessage);
        Assert.Equal("IO.__exit__() missing 2 required positional arguments: 'value' and 'traceback'",
            Assert.Throws<PyTypeError>(() => Call(exit, PyNone.Instance)).PyMessage);
        Assert.Equal("IO.__exit__() missing 1 required positional argument: 'traceback'",
            Assert.Throws<PyTypeError>(() => Call(exit, PyNone.Instance, PyNone.Instance)).PyMessage);
        Assert.Equal("IO.__exit__() missing 4 required positional arguments: 'self', 'type', 'value', and 'traceback'",
            Assert.Throws<PyTypeError>(() => Call(Get(Root("IO"), "__exit__"))).PyMessage);
        Assert.Equal("BinaryIO.write() missing 1 required positional argument: 's'",
            Assert.Throws<PyTypeError>(() => Call(Get(Instance(Root("BinaryIO")), "write"))).PyMessage);
    }

    [Fact]
    public void FunctionReflectionAndReservedMutationStayExplicitlyUnsupported()
    {
        foreach (MethodSpec spec in Methods())
        {
            PyFunction function = Function(spec);
            foreach (string name in new[] { "__annotations__", "__dict__", "__code__", "__closure__", "__get__", "__reduce__", "__reduce_ex__" })
                Assert.Throws<PyNotImplementedError>(() => Get(function, name));
            foreach (string name in new[] { "__annotations__", "__dict__", "__code__", "__closure__", "__get__",
                "__reduce__", "__reduce_ex__", "__defaults__", "__kwdefaults__", "__qualname__", "__class__", "__isabstractmethod__" })
            {
                Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(function, name, PyNone.Instance));
                Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(function, name));
            }
            Assert.Equal(spec.Owner + "." + spec.Name, Get(function, "__qualname__"));
        }
    }

    private sealed class Poison : PyNativeObject
    {
        internal int Reads;
        public override string TypeName => "TypingIoArgumentPoison";
        private Exception Touched() { Reads++; return new Exception("pass 方法不得读取或转换参数"); }
        public override object? GetAttr(string name) => throw Touched();
        public override string PythonRepr() => throw Touched();
        public override string PythonStr() => throw Touched();
        public override int GetHashCode() => throw Touched();
    }
}
