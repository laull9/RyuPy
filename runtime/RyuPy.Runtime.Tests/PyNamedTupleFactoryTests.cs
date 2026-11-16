// namedtuple C1 内部工厂专项：保留内部入口细测，不将内部通过冒充 Python 应用验收。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyNamedTupleFactoryTests
{
    private static PyClass Invoke(object?[] args, string[] names, object?[] values)
    {
        Type factory = typeof(PyTuple).Assembly.GetType("RyuPy.Runtime.PyNamedTupleFactory")!;
        try
        {
            return (PyClass)factory.GetMethod("Invoke", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [args, names, values])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw;
        }
    }
    private static PyClass Make(object? name, object? fields, string[]? names = null, object?[]? values = null) =>
        Invoke([name, fields], [.. names ?? [], "module"], [.. values ?? [], "tests.namedtuple"]);
    private static object? Get(object value, string name) => PyObject.GetAttr(value, name);
    private static object PublicDefaults(PyClass cls)
    {
        object layout = typeof(PyClass).GetProperty("NamedTupleLayout", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cls)!;
        return layout.GetType().GetProperty("PublicDefaults", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(layout)!;
    }
    private static PyInstance Object(string name, params (string Name, Func<object?[], object?> Body)[] methods)
    {
        var cls = new PyClass(name);
        foreach (var method in methods) cls.SetAttr(method.Name, PyNativeFunctions.CreateVariadic(method.Name, method.Body));
        return new PyInstance(cls);
    }
    private static PyInstance Text(List<string> log, string label, string value) =>
        Object("Text", ("__str__", _ => { log.Add("str:" + label); return value; }));
    private static PyInstance Finite(List<string> log, string label, object?[] items, PyBaseException? tail = null) =>
        Object("Finite", ("__iter__", _ => { log.Add("iter:" + label); return new Steps(log, label, items, tail); }));

    [Fact]
    public void AsciiValidationRenamingAndFieldsUseTheRealClass()
    {
        PyClass cls = Make("Point", " x, y\tz\n");
        Assert.Equal<object?>(["x", "y", "z"], Assert.IsType<PyTuple>(Get(cls, "_fields")).Items);
        Assert.NotSame(cls, Make("Point", "x y z"));
        PyClass names = Make(new string("Twin".ToCharArray()), new PyTuple([new string("Twin".ToCharArray())]));
        Assert.Same(Get(names, "__name__"), Assert.IsType<PyTuple>(Get(names, "_fields")).Items[0]);
        PyClass otherNames = Make(new string("Twin".ToCharArray()), new PyTuple([new string("Twin".ToCharArray())]));
        Assert.NotSame(names, otherNames); Assert.Equal(Get(names, "__name__"), Get(otherNames, "__name__"));
        Assert.Equal(Assert.IsType<PyTuple>(Get(names, "_fields")).Items, Assert.IsType<PyTuple>(Get(otherNames, "_fields")).Items);
        // 跨类字符串 is 是已登记 pending，不能把 CLR 字符串驻留偶然性当 CP 证明。
        Assert.Equal("Point(x=1, y=2, z=3)", PyRepr.Repr(cls.Invoke([1L, 2L, 3L], [], [])));
        Assert.Empty(Assert.IsType<PyTuple>(Get(Make("Empty", ""), "_fields")).Items);
        Assert.Equal<object?>(["self", "cls", "count", "index", "match", "case", "type"],
            Assert.IsType<PyTuple>(Get(Make("Soft", "self cls count index match case type"), "_fields")).Items);
        PyClass renamed = Make("Renamed", new PyTuple(["x", "x", "_1", "x"]), ["rename"], [true]);
        Assert.Equal<object?>(["x", "_1", "_2", "_3"], Assert.IsType<PyTuple>(Get(renamed, "_fields")).Items);
        foreach (string bad in new[] { "", "1x", "x-y" })
            Assert.Equal($"Type names and field names must be valid identifiers: {PyRepr.Repr(bad)}",
                Assert.Throws<PyValueError>(() => Make("P", new PyTuple([bad]))).PyMessage);
        Assert.Equal("Type names and field names cannot be a keyword: 'class'", Assert.Throws<PyValueError>(() => Make("P", "class")).PyMessage);
        Assert.Equal("Encountered duplicate field name: 'x'", Assert.Throws<PyValueError>(() => Make("P", "x x")).PyMessage);
        Assert.Equal("Field names cannot start with an underscore: '_x'", Assert.Throws<PyValueError>(() => Make("P", "_x")).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => Make("P", "名字"));
        // 原内部阶段的未发布守门在 C3 升级为真实、稳定导出，不删除此处覆盖。
        object? factory = PyCollections.CreateAttributes()["namedtuple"];
        Assert.IsAssignableFrom<IPyNativeCallable>(factory);
        Assert.Same(factory, PyCollections.CreateAttributes()["namedtuple"]);
    }

    [Fact]
    public void FactoryAndConstructorBindingRejectBeforeAnyInputProtocol()
    {
        int calls = 0;
        object poison = Object("Poison", ("__str__", _ => { calls++; throw new PyRuntimeError("called"); }));
        Assert.Equal("namedtuple() got an unexpected keyword argument 'unknown'",
            Assert.Throws<PyTypeError>(() => Invoke([poison, poison, 3L], ["unknown"], [true])).PyMessage);
        Assert.Equal("namedtuple() got multiple values for argument 'typename'",
            Assert.Throws<PyTypeError>(() => Invoke([poison, poison, 3L], ["typename"], [poison])).PyMessage);
        Assert.Equal("namedtuple() takes 2 positional arguments but 3 positional arguments (and 1 keyword-only argument) were given",
            Assert.Throws<PyTypeError>(() => Invoke([poison, poison, 3L], ["rename"], [true])).PyMessage);
        Assert.Equal("namedtuple() missing 2 required positional arguments: 'typename' and 'field_names'",
            Assert.Throws<PyTypeError>(() => Invoke([], [], [])).PyMessage);
        Assert.Equal(0, calls);
        PyClass cls = Make("Pair", "left right", ["defaults"], [new PyTuple([9L])]);
        Assert.Equal("Pair.__new__() got an unexpected keyword argument 'unknown'",
            Assert.Throws<PyTypeError>(() => cls.Invoke([1L, 2L, 3L], ["unknown"], [4L])).PyMessage);
        Assert.Equal("Pair.__new__() got multiple values for argument 'left'",
            Assert.Throws<PyTypeError>(() => cls.Invoke([1L, 2L, 3L], ["left"], [4L])).PyMessage);
        Assert.Equal("Pair.__new__() takes from 2 to 3 positional arguments but 4 were given",
            Assert.Throws<PyTypeError>(() => cls.Invoke([1L, 2L, 3L], [], [])).PyMessage);
    }

    [Fact]
    public void FieldsInterleaveNextAndStrBeforeTypenameRenameAndDefaults()
    {
        var log = new List<string>();
        object fields = Finite(log, "fields", [Text(log, "x", "x"), Text(log, "y", "y")]);
        object rename = Object("Rename", ("__bool__", _ => { log.Add("bool:rename"); return false; }));
        PyClass cls = Make(Text(log, "typename", "Pair"), fields, ["rename", "defaults"], [rename, Finite(log, "defaults", [7L])]);
        Assert.Equal(new[] { "iter:fields", "yield:fields:0", "str:x", "yield:fields:1", "str:y", "end:fields", "str:typename", "bool:rename", "iter:defaults", "yield:defaults:0", "end:defaults" }, log);
        Assert.Equal("Pair(x=1, y=7)", PyRepr.Repr(cls.Invoke([1L], [], [])));
        log.Clear();
        Assert.Throws<PyValueError>(() => Make("Bad", Finite(log, "fields", ["bad-name"]), ["defaults"], [Finite(log, "defaults", [1L])]));
        Assert.Equal(new[] { "iter:fields", "yield:fields:0", "end:fields" }, log);
    }

    [Fact]
    public void RenameTruthIsRepeatedAtEachFinalUnderscoreField()
    {
        int calls = 0;
        object rename = Object("Rename", ("__bool__", _ => { calls++; return true; }));
        PyClass cls = Make("P", new PyTuple(["x", "x", "_1", "x"]), ["rename"], [rename]);
        Assert.Equal(4, calls);
        Assert.Equal<object?>(["x", "_1", "_2", "_3"], Assert.IsType<PyTuple>(Get(cls, "_fields")).Items);
        calls = 0;
        object changing = Object("Changing", ("__bool__", _ => ++calls == 1));
        Assert.Equal("Field names cannot start with an underscore: '_0'",
            Assert.Throws<PyValueError>(() => Make("P", new PyTuple(["bad-name"]), ["rename"], [changing])).PyMessage);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void DefaultsAreFullyConsumedDetachedAndKeepTheirPublicTupleIdentity()
    {
        var shared = new PyList(); var tuple = new PyTuple([shared]);
        PyClass cls = Make("Box", "value", ["defaults"], [tuple]);
        Assert.Same(tuple, PublicDefaults(cls)); Assert.Same(PublicDefaults(cls), PublicDefaults(cls));
        Assert.Same(shared, Get(cls.Invoke([], [], [])!, "value"));
        Assert.IsType<PyDict>(Get(cls, "_field_defaults")).Set("value", 10L);
        Assert.Same(shared, Get(cls.Invoke([], [], [])!, "value"));
        Assert.Same(PyNone.Instance, PublicDefaults(Make("No", "x")));
        var empty = new PyTuple([]); Assert.Same(empty, PublicDefaults(Make("Empty", "x", ["defaults"], [empty])));
        var log = new List<string>();
        Assert.Equal("Got more default values than field names", Assert.Throws<PyTypeError>(() =>
            Make("P", "x", ["defaults"], [Finite(log, "defaults", [1L, 2L, 3L])])).PyMessage);
        Assert.Equal(new[] { "iter:defaults", "yield:defaults:0", "yield:defaults:1", "yield:defaults:2", "end:defaults" }, log);
        var stop = new PyRuntimeError("tail"); log.Clear();
        Assert.Same(stop, Assert.Throws<PyRuntimeError>(() => Make("P", "x", ["defaults"], [Finite(log, "defaults", [1L, 2L], stop)])));
    }

    [Fact]
    public void ExplicitModuleBypassesUnknownSourceAndAllTruthChecks()
    {
        PyTraceback.Reset();
        try
        {
            int calls = 0;
            object poison = Object("Poison", ("__bool__", _ => { calls++; throw new PyRuntimeError("truth"); }));
            foreach (object module in new object[] { "", 0L, false, new PyList(), poison })
                Assert.Same(module, Get(Invoke(["P", "x"], ["module"], [module]), "__module__"));
            Assert.Equal(0, calls);
            Assert.Throws<PyNotImplementedError>(() => Invoke(["P", "x"], [], []));
            Assert.Throws<PyNotImplementedError>(() => Invoke(["P", "x"], ["module"], [PyNone.Instance]));
            Assert.Throws<PyValueError>(() => Invoke(["P", "bad-name"], [], []));
            Assert.Equal("Got more default values than field names", Assert.Throws<PyTypeError>(() =>
                Invoke(["P", "x"], ["defaults"], [new PyTuple([1L, 2L])])).PyMessage);
        }
        finally { PyTraceback.Reset(); }
    }

    [Fact]
    public void DefaultModuleReadsOnlyCurrentOwnerAfterAllValidation()
    {
        PyTraceback.Reset();
        try
        {
            int calls = 0;
            object module = Object("Module", ("__bool__", _ => { calls++; return true; }));
            var function = PyNativeFunctions.CreateVariadic("caller", _ => PyNone.Instance);
            PyOps.FunctionModule(function, module);
            long outer = PyTraceback.PushFrameWithOrigin("owner.py", "caller", function);
            Assert.Same(module, Get(Invoke(["P", "x"], [], []), "__module__")); Assert.Equal(1, calls);
            Assert.Throws<PyValueError>(() => Invoke(["P", "bad-name"], [], [])); Assert.Equal(1, calls);
            long inner = PyTraceback.PushFrame("static.py", "unknown");
            Assert.Throws<PyNotImplementedError>(() => Invoke(["P", "x"], [], [])); Assert.Equal(1, calls);
            Assert.Equal("explicit", Get(Invoke(["P", "x"], ["module"], ["explicit"]), "__module__"));
            PyTraceback.PopTo(inner);
            PyOps.FunctionModule(function, "");
            Assert.Equal("__main__", Get(Invoke(["P", "x"], [], []), "__module__"));
            PyTraceback.PopTo(outer);
            PyTraceback.PushFrameWithOrigin("root.py", "<module>", PyTraceback.CreateModuleExecutionSource(37L));
            Assert.Equal(37L, Get(Invoke(["P", "x"], [], []), "__module__"));
        }
        finally { PyTraceback.Reset(); }
    }

    [Fact]
    public void TupleMaterializationChecksHintAfterIterBeforeAnyNext()
    {
        var log = new List<string>();
        PyInstance source = Object("Hinted",
            ("__iter__", _ => { log.Add("iter"); return new Steps(log, "values", [7L]); }),
            ("__len__", _ => { log.Add("len"); throw new PyTypeError("use hint"); }),
            ("__length_hint__", _ => { log.Add("hint"); return 1L; }));
        Make("P", "x", ["defaults"], [source]);
        Assert.Equal(new[] { "iter", "len", "hint", "yield:values:0", "end:values" }, log);
        foreach (object bad in new object[] { "no", -1L, BigInteger.One << 80 })
        {
            log.Clear(); source.Class.SetAttr("__length_hint__", PyNativeFunctions.CreateVariadic("hint", _ => bad));
            Assert.ThrowsAny<PyBaseException>(() => Make("P", "x", ["defaults"], [source]));
            Assert.Equal(new[] { "iter", "len" }, log);
        }
        source.Class.SetAttr("__length_hint__", PyNativeFunctions.CreateVariadic("hint", _ => PyNotImplemented.Instance));
        Make("P", "x", ["defaults"], [source]);
        source.Class.SetAttr("__length_hint__", PyNativeFunctions.CreateVariadic("hint", _ => throw new PyTypeError("ignore hint")));
        Make("P", "x", ["defaults"], [source]);
    }

    [Fact]
    public void StringSubclassReplaceResultKeepsItsOwnSplitAndInternBoundary()
    {
        var log = new List<string>();
        object split = Object("Split", ("split", _ => { log.Add("split"); return new PyTuple(["x"]); }));
        PyClass textClass = (PyClass)PyOps.MakeClass("SubStr", [PyType.Get("str")], null);
        textClass.SetAttr("replace", PyNativeFunctions.CreateVariadic("replace", _ => { log.Add("replace"); return split; }));
        PyOps.FinalizeOrdinaryClass(textClass);
        object text = textClass.Invoke(["ignored"], [], [])!;
        Make("P", text); Assert.Equal(new[] { "replace", "split" }, log);
        object returnsSubclass = Object("Text", ("__str__", _ => text));
        int truth = 0; object rename = Object("Rename", ("__bool__", _ => { truth++; return true; }));
        Assert.Equal("can't intern SubStr", Assert.Throws<PyTypeError>(() => Make(returnsSubclass, "x", ["rename"], [rename])).PyMessage);
        Assert.Equal(0, truth);
        Assert.Equal("Type names and field names must be strings", Assert.Throws<PyTypeError>(() => Make("P", new PyTuple([returnsSubclass]))).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => Make("P", new PyTuple([returnsSubclass]), ["rename"], [rename]));
        Assert.Equal(1, truth);
    }

    [Fact]
    public void OwnErrorsCaptureCallerButUserProtocolErrorsKeepTheirIdentity()
    {
        PyTraceback.Reset(); object? token = PyErrors.EnterHandler(new PyValueError("ambient"));
        try
        {
            PyTraceback.PushFrame("factory_probe.py", "caller"); PyTraceback.SetLine(31);
            PyTypeError own = Assert.Throws<PyTypeError>(() => Invoke([], [], []));
            Assert.Same(PyErrors.Current, own.Context);
            Assert.Equal(new PyStackFrame("factory_probe.py", "caller", 31), own.Frames[0]);
            Assert.NotEmpty(Assert.Throws<PyTypeError>(() => Make("P", 42L)).Frames);
            var original = new PyRuntimeError("original");
            object text = Object("Text", ("__str__", _ => throw original));
            Assert.Same(original, Assert.Throws<PyRuntimeError>(() => Make(text, "x")));
            Assert.Empty(original.Frames); Assert.Null(original.Context); // 本地不擅自重包/Prepare 用户正文。
            object truth = Object("Module", ("__bool__", _ => throw original));
            PyTraceback.Reset(); PyTraceback.PushFrameWithOrigin("factory_probe.py", "caller", PyTraceback.CreateModuleExecutionSource(truth));
            Assert.Same(original, Assert.Throws<PyRuntimeError>(() => Invoke(["P", "x"], [], [])));
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void PropertySlotsDistinguishLookupErrorsAndRestoreStopState()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("tuple_protocol.py", "caller");
            var cls = new PyClass("PropertyIterator"); var source = new PyInstance(cls); int index = 0;
            cls.SetAttr("__iter__", new PyProperty(PyNativeFunctions.CreateVariadic("get_iter", _ =>
                PyNativeFunctions.CreateVariadic("iter_body", _ => source))));
            cls.SetAttr("__next__", new PyProperty(PyNativeFunctions.CreateVariadic("get_next", _ =>
                PyNativeFunctions.CreateVariadic("next_body", _ =>
                {
                    if (index++ == 0) return 7L;
                    PyTraceback.PushFrame("tuple_protocol.py", "stopping_next");
                    var stop = new PyStopIteration(); PyErrors.EnterHandler(stop); throw stop;
                }))));
            PyClass result = Make("P", "x", ["defaults"], [source]);
            Assert.Equal(7L, Get(result.Invoke([], [], [])!, "x"));
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
            var lookupError = new PyTypeError("hint getter");
            cls.SetAttr("__length_hint__", new PyProperty(PyNativeFunctions.CreateVariadic("get_hint", _ => throw lookupError)));
            index = 0;
            Assert.Same(lookupError, Assert.Throws<PyTypeError>(() => Make("P", "x", ["defaults"], [source])));
            Assert.Equal(0, index); Assert.Same(ambient, lookupError.Context); Assert.NotEmpty(lookupError.Frames);
            cls.SetAttr("__length_hint__", new PyProperty(PyNativeFunctions.CreateVariadic("get_hint", _ =>
                PyNativeFunctions.CreateVariadic("hint_body", _ => throw new PyTypeError("ignored call")))));
            Make("P", "x", ["defaults"], [source]); Assert.Same(ambient, PyErrors.Current);
            object descriptor = Object("Descriptor", ("__get__", _ => PyNone.Instance));
            cls.SetAttr("__iter__", descriptor);
            Assert.Throws<PyNotImplementedError>(() => Make("P", "x", ["defaults"], [source]));
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    private sealed class Steps(List<string> log, string label, object?[] items, PyBaseException? tail = null) : IPyIterator
    {
        private int _index = -1;
        public bool MoveNext()
        {
            _index++;
            if (_index < items.Length) { log.Add($"yield:{label}:{_index}"); return true; }
            if (tail is not null) throw tail;
            log.Add("end:" + label); return false;
        }
        public object? Current() => items[_index];
    }
}
