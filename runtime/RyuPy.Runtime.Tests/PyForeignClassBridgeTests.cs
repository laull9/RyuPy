// 跨域类继承的 Runtime 中性协议单元测试：不依赖 libpython，通过模拟 IPyForeignClassBridge
// 检验动态基类守卫、单/多继承终结、实例与属性委托、super() 转发以及异常原子性。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>跨域类继承中性协议单元测试。</summary>
[Collection("生成器帧栈")]
public class PyForeignClassBridgeTests
{
    private sealed class MockForeignObject : PyNativeObject, IPyForeignObject
    {
        private readonly string _typeName;
        private readonly object? _foreignType;

        public MockForeignObject(string typeName, object? foreignType = null, bool isType = false)
        {
            _typeName = typeName;
            _foreignType = foreignType;
            IsTypeFlag = isType;
        }

        public Dictionary<string, object?> Dict { get; } = new(StringComparer.Ordinal);
        public Func<object?[], string[], object?[], object?>? OnInvoke { get; init; }
        public Func<object, bool>? OnAccepts { get; init; }
        public bool IsTypeFlag { get; }

        public override string TypeName => _typeName;
        public object ForeignType => _foreignType ?? PyType.Get(IsTypeFlag ? "type" : _typeName);
        public bool IsExceptionClass => false;

        public override object? GetAttr(string name) =>
            Dict.TryGetValue(name, out object? value) ? value : throw Missing(name);

        public override void SetAttr(string name, object? value) => Dict[name] = value;

        public override void DelAttr(string name)
        {
            if (!Dict.Remove(name)) throw Missing(name);
        }

        public object? Invoke(object?[] args, string[] names, object?[] values) =>
            OnInvoke is not null ? OnInvoke(args, names, values) : new MockForeignObject(_typeName, this);

        public object ReadScalar(string kind) => throw new PyTypeError("not a scalar");
        public object? Item(object? key) => Dict.TryGetValue($"[{key}]", out object? v) ? v : throw new PyKeyError($"{key}");
        public void WriteItem(object? key, object? value, bool deleting)
        {
            string slot = $"[{key}]";
            if (deleting)
            {
                if (!Dict.Remove(slot)) throw new PyKeyError($"{key}");
                return;
            }
            Dict[slot] = value;
        }

        public IPyIterator Iterate() => throw new PyTypeError("not iterable");
        public long Length() => Dict.Count;
        public bool Truth() => true;
        public long Hash() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        public object Binary(int operation, object? other, bool reverse) => PyNotImplemented.Instance;
        public bool Compare(int operation, object? other, bool reverse) => false;
        public object RichCompare(int operation, object? other, bool reverse) => PyNotImplemented.Instance;
        public object Unary(int operation) => throw new PyTypeError("unsupported unary");
        public bool IsInstanceOf(object type) => ReferenceEquals(type, ForeignType);
        public bool Accepts(object? value) => OnAccepts is not null && value is not null && OnAccepts(value);
        public bool MatchesException(PyBaseException error) => false;
        public PyStackFrame[] TracebackFrames() => [];
        public void SynchronizeException(PyBaseException error, bool write) { }
    }

    private sealed class MockForeignClassBridge : IPyForeignClassBridge
    {
        public string? LastSuperAttrName { get; private set; }
        public bool ShouldFailCreate { get; set; }
        public bool ReturnRawSuper { get; set; }
        public List<MockForeignObject> CreatedClasses { get; } = [];
        public Dictionary<PyClass, MockForeignObject> Mirrors { get; } = [];

        public bool IsType(IPyForeignObject value) => value is MockForeignObject { IsTypeFlag: true };
        public bool IsPlainType(object? typeObject) => ReferenceEquals(typeObject, PyType.Get("type"));
        public bool IsSubclass(object? child, object? parent)
        {
            if (child is PyClass c)
            {
                if (ReferenceEquals(c, parent)) return true;
                foreach (object? item in PyForeignClassBridge.GetDirectBases(c))
                    if (ReferenceEquals(item, parent) || item is PyClass && IsSubclass(item, parent)) return true;
                return false;
            }
            if (child is MockForeignObject fc)
            {
                foreach (KeyValuePair<PyClass, MockForeignObject> pair in Mirrors)
                {
                    if (ReferenceEquals(pair.Value, fc)) return IsSubclass(pair.Key, parent);
                }
            }
            return false;
        }

        public IPyForeignObject CreateDerivedClass(
            PyClass compiledClass,
            string name,
            object?[] bases,
            IReadOnlyDictionary<string, object?> attributes
        )
        {
            if (ShouldFailCreate) throw new PyTypeError("simulated foreign type creation failure");
            MockForeignObject? foreignTypeObj = null;
            foreignTypeObj = new MockForeignObject(
                "type",
                PyType.Get("type"),
                isType: true
            )
            {
                OnInvoke = (args, kwNames, kwValues) =>
                {
                    var fi = new MockForeignObject(name, foreignTypeObj);
                    var instance = new PyInstance(compiledClass, fi);
                    if (attributes.TryGetValue("__init__", out object? init) && init is not null)
                    {
                        PyObject.InvokeBound(init, compiledClass, instance, args, kwNames, kwValues);
                    }
                    return instance;
                },
                OnAccepts = val => val is PyInstance inst && inst.Class.IsSubclassOf(compiledClass),
            };
            foreach (KeyValuePair<string, object?> entry in attributes)
            {
                foreignTypeObj.Dict[entry.Key] = entry.Value;
            }
            foreignTypeObj.Dict["__bases__"] = new PyTuple(bases);
            foreignTypeObj.Dict["__mro__"] = new PyTuple([compiledClass, .. bases, PyType.Get("object")]);
            foreignTypeObj.Dict["__module__"] = "test_mod";
            CreatedClasses.Add(foreignTypeObj);
            Mirrors[compiledClass] = foreignTypeObj;
            return foreignTypeObj;
        }

        public void SetClassAttribute(IPyForeignObject foreignClass, PyClass ownerClass, string name, object? value) =>
            ((MockForeignObject)foreignClass).SetAttr(name, value);

        public void DeleteClassAttribute(IPyForeignObject foreignClass, string name) =>
            ((MockForeignObject)foreignClass).DelAttr(name);

        public object? AllocateFromBaseObject(IPyForeignObject foreignClass, object?[] extraArgs, string[] kwNames, object?[] kwValues)
        {
            var fc = (MockForeignObject)foreignClass;
            foreach (KeyValuePair<PyClass, MockForeignObject> pair in Mirrors)
            {
                if (ReferenceEquals(pair.Value, fc))
                {
                    return new PyInstance(pair.Key, new MockForeignObject(pair.Key.Name, fc));
                }
            }
            return new MockForeignObject(fc.TypeName, fc);
        }

        public void InitializeFromBaseObject(IPyForeignObject foreignInstance, object?[] extraArgs, string[] kwNames, object?[] kwValues) { }
        public void SetAttrFromBaseObject(IPyForeignObject foreignInstance, string name, object? value) =>
            ((MockForeignObject)foreignInstance).SetAttr(name, value);
        public void DelAttrFromBaseObject(IPyForeignObject foreignInstance, string name) =>
            ((MockForeignObject)foreignInstance).DelAttr(name);

        public object? GetSuperAttribute(object start, object? receiver, string name)
        {
            LastSuperAttrName = name;
            return new MockForeignObject("method") { OnInvoke = (_, _, _) => $"super:{name}" };
        }

        public object? CreateSuper(object start, object? receiver)
        {
            var proxy = new MockForeignObject("super");
            proxy.Dict["legacy_marker"] = 17L;
            if (ReturnRawSuper) return proxy;
            return PySuper.FromForeign(proxy, proxy.GetAttr);
        }
        public bool Contains(IPyForeignObject foreignInstance, object? value) => ((MockForeignObject)foreignInstance).Dict.ContainsKey($"[{value}]");
        public object? Dir(IPyForeignObject foreignObject) => new PyList(["title", "withdraw"]);
    }

    private sealed class MockProviderWithBridge(MockForeignClassBridge bridge) : IPyModuleProvider, IPyForeignClassBridge
    {
        public bool Owns(string name) => false;
        public object? SystemAttribute(string name) => null;
        public PyModule Import(string name) => throw new PyModuleNotFoundError(name);
        public void Registered(string name) { }
        public void Initializing(PyModule module) { }
        public void Failed(string name) { }

        public bool IsType(IPyForeignObject value) => bridge.IsType(value);
        public bool IsPlainType(object? typeObject) => bridge.IsPlainType(typeObject);
        public bool IsSubclass(object? child, object? parent) => bridge.IsSubclass(child, parent);
        public IPyForeignObject CreateDerivedClass(PyClass compiledClass, string name, object?[] bases, IReadOnlyDictionary<string, object?> attributes) =>
            bridge.CreateDerivedClass(compiledClass, name, bases, attributes);
        public void SetClassAttribute(IPyForeignObject foreignClass, PyClass ownerClass, string name, object? value) =>
            bridge.SetClassAttribute(foreignClass, ownerClass, name, value);
        public void DeleteClassAttribute(IPyForeignObject foreignClass, string name) =>
            bridge.DeleteClassAttribute(foreignClass, name);
        public object? AllocateFromBaseObject(IPyForeignObject foreignClass, object?[] extraArgs, string[] kwNames, object?[] kwValues) =>
            bridge.AllocateFromBaseObject(foreignClass, extraArgs, kwNames, kwValues);
        public void InitializeFromBaseObject(IPyForeignObject foreignInstance, object?[] extraArgs, string[] kwNames, object?[] kwValues) =>
            bridge.InitializeFromBaseObject(foreignInstance, extraArgs, kwNames, kwValues);
        public void SetAttrFromBaseObject(IPyForeignObject foreignInstance, string name, object? value) =>
            bridge.SetAttrFromBaseObject(foreignInstance, name, value);
        public void DelAttrFromBaseObject(IPyForeignObject foreignInstance, string name) =>
            bridge.DelAttrFromBaseObject(foreignInstance, name);
        public object? GetSuperAttribute(object start, object? receiver, string name) =>
            bridge.GetSuperAttribute(start, receiver, name);
        public object? CreateSuper(object start, object? receiver) =>
            bridge.CreateSuper(start, receiver);
        public bool Contains(IPyForeignObject foreignInstance, object? value) =>
            bridge.Contains(foreignInstance, value);
        public object? Dir(IPyForeignObject foreignObject) =>
            bridge.Dir(foreignObject);
    }

    [Fact]
    public void LegacySuperAbiRetainsObjectReturnAndCanonicalizesForeignProxy()
    {
        // 旧provider二进制仍实现object返回签名，Runtime在冷边界统一super身份。
        Assert.Equal(typeof(object), typeof(IPyForeignClassBridge).GetMethod("CreateSuper")!.ReturnType);
        var bridge = new MockForeignClassBridge { ReturnRawSuper = true };
        PyForeignClassBridge.Install(bridge);
        try
        {
            var start = new MockForeignObject("Base", isType: true);
            object? proxy = PyOps.CallValue(PyLexicalSuper.Type, [start], [], []);
            Assert.IsType<PySuper>(proxy);
            Assert.Same(PyLexicalSuper.Type, PyObject.TypeOf(proxy));
            Assert.Same(PyLexicalSuper.Type, PyObject.GetAttr(proxy, "__class__"));
            Assert.Equal(17L, PyObject.GetAttr(proxy, "legacy_marker"));
        }
        finally { PyForeignClassBridge.Remove(bridge); }
    }

    [Fact]
    public void ValidateForeignBase_AcceptsForeignTypeAndRejectsPlainForeignObject()
    {
        var bridge = new MockForeignClassBridge();
        PyForeignClassBridge.Install(bridge);
        try
        {
            var tkBase = new MockForeignObject("Tk", isType: true);
            var nonType = new MockForeignObject("TkInstance", tkBase, isType: false);

            Assert.Same(tkBase, PyRuntimeBases.Validate(tkBase));
            Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(nonType));
        }
        finally
        {
            PyForeignClassBridge.Remove(bridge);
        }
    }

    [Fact]
    public void SingleInheritance_FinalizesCreatesInstanceForwardsAttrsAndSuper()
    {
        var bridge = new MockForeignClassBridge();
        PyForeignClassBridge.Install(bridge);
        try
        {
            var tkBase = new MockForeignObject("Tk", isType: true);
            var derived = (PyClass)PyOps.PrepareRuntimeDerivedClass("App", [tkBase]);
            var initFn = new PyFunction(
                "__init__",
                PySignature.Create("__init__", ["self"], ["pos"], ["0"]),
                (_, args, _, _) =>
                {
                    object? self = args[0];
                    _ = PyOps.CallValue(PyObject.GetAttr(PyMethodContext.MakeSuper(), "__init__"), [], [], []);
                    PyObject.SetAttr(self, "title_text", "SanBudget");
                    return PyNone.Instance;
                },
                [],
                usesSuper: true
            );
            derived.SetAttr("__init__", initFn);
            PyOps.FinalizeOrdinaryClass(derived);

            Assert.NotNull(derived.ForeignClass);
            Assert.True((bool)PyOps.CallBuiltin("issubclass", [derived, tkBase], [], [])!);
            Assert.Equal(PyType.Get("type"), PyObject.TypeOf(derived));

            object? instanceObj = PyOps.CallValue(derived, [], [], []);
            var instance = Assert.IsType<PyInstance>(instanceObj);
            Assert.True(PyObject.IsInstance(instance, derived));
            Assert.Equal("SanBudget", PyObject.GetAttr(instance, "title_text"));
            Assert.Equal("__init__", bridge.LastSuperAttrName);

            PyObject.DelAttr(instance, "title_text");
            Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(instance, "title_text"));

            PyObject.SetAttr(instance, "x", 42L);
            Assert.Equal(42L, PyPic.GetAttr(0, instance, "x"));

            var dirList = Assert.IsType<PyList>(PyDir.Call([instance], [], []));
            Assert.Contains("title", dirList.Items);
        }
        finally
        {
            PyForeignClassBridge.Remove(bridge);
        }
    }

    [Fact]
    public void MultipleInheritance_WithNativeMixin_FinalizesThroughBridge()
    {
        var bridge = new MockForeignClassBridge();
        PyForeignClassBridge.Install(bridge);
        try
        {
            var tkBase = new MockForeignObject("Tk", isType: true);
            var mixin = (PyClass)PyOps.MakeClass("Mixin", [], null);
            mixin.SetAttr("tag", "mix");
            PyOps.FinalizeOrdinaryClass(mixin);

            object frame = PyLexicalClassFactory.PrepareForMode("Combined", "Combined", "test_mod", [mixin, tkBase], false);
            var derived = (PyClass)PyLexicalClassFactory.Finish(frame);

            Assert.True(derived.HasForeignHierarchy);
            Assert.NotNull(derived.ForeignClass);
            Assert.True(derived.IsSubclassOf(mixin));
            Assert.True((bool)PyOps.CallBuiltin("issubclass", [derived, tkBase], [], [])!);
            var directBases = derived.DirectBasesTuple();
            Assert.Equal(2, directBases.Length);
        }
        finally
        {
            PyForeignClassBridge.Remove(bridge);
        }
    }

    [Fact]
    public void FailedForeignClassCreation_LeavesClassUnfinalizedAtomically()
    {
        var bridge = new MockForeignClassBridge { ShouldFailCreate = true };
        PyForeignClassBridge.Install(bridge);
        try
        {
            var nonInheritable = new MockForeignObject("bool", isType: true);
            var bad = (PyClass)PyOps.PrepareRuntimeDerivedClass("SubBool", [nonInheritable]);
            Assert.Throws<PyTypeError>(() => PyOps.FinalizeOrdinaryClass(bad));
            Assert.Null(bad.ForeignClass);
        }
        finally
        {
            PyForeignClassBridge.Remove(bridge);
        }
    }

    [Fact]
    public void PyProviderInstallAndRemove_HooksBridgeAutomatically()
    {
        Assert.Null(PyProvider.Current);
        Assert.Null(PyForeignClassBridge.Current);
        var provider = new MockProviderWithBridge(new MockForeignClassBridge());
        PyProvider.Install(provider);
        try
        {
            Assert.Same(provider, PyForeignClassBridge.Current);
        }
        finally
        {
            PyProvider.Remove(provider);
        }
        Assert.Null(PyForeignClassBridge.Current);
    }
}
