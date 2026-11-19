// collections.abc真实类根工厂与冻结抽象元数据；canonical身份不随模块导出重绑改变。
using System;
using System.Collections.Generic;
using System.Linq;
namespace RyuPy.Runtime;

public static partial class PyAbc
{
    private static readonly HashSet<PyFunction> AbstractFunctions = new(ReferenceEqualityComparer.Instance);
    private static Dictionary<string, object?> Build()
    {
        Root("Iterable", [], ["__iter__"]);
        Root("Iterator", ["Iterable"], ["__next__"]);
        Root("Reversible", ["Iterable"], ["__reversed__"]);
        Root("Generator", ["Iterator"], ["send", "throw"]);
        Root("Sized", [], ["__len__"]);
        Root("Container", [], ["__contains__"]);
        Root("Callable", [], ["__call__"]);
        Root("Collection", ["Sized", "Iterable", "Container"], []);
        Root("Sequence", ["Reversible", "Collection"], ["__getitem__"]);
        Root("MutableSequence", ["Sequence"], ["__setitem__", "__delitem__", "insert"]);
        Root("Set", ["Collection"], []);
        Root("MutableSet", ["Set"], ["add", "discard"]);
        Root("Mapping", ["Collection"], ["__getitem__"]);
        Root("MutableMapping", ["Mapping"], ["__setitem__", "__delitem__"]);
        Root("MappingView", ["Sized"], []);
        Root("KeysView", ["MappingView", "Set"], []);
        Root("ItemsView", ["MappingView", "Set"], []);
        Root("ValuesView", ["MappingView", "Collection"], []);
        return Roots.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal);
    }
    private static void Root(string name, string[] parents, string[] abstractNames)
    {
        var cls = new PyClass(name) { NativeDefinitionModule = "collections.abc" };
        Roots.Add(name, cls); RootIdentities.Add(cls);
        cls.PrepareMultipleBases(parents.Length == 0 ? [PyBaseObject.Type] : parents.Select(p => (object?)Roots[p]).ToArray());
        cls.SetAttr("__module__", "collections.abc"); cls.SetAttr("__slots__", new PyTuple([]));
        if (name == "MappingView") cls.ConfigureNativeSlots(["_mapping"]);
        foreach (string member in abstractNames) InstallAbstract(cls, member);
        InstallDefaults(cls); InstallSequence(cls); InstallSet(cls); InstallMapping(cls);
        cls.FinalizeOrdinaryConstruction();
    }
    internal static PyFunction Method(PyClass cls, string name, string[] names, Func<object?[], object?> body,
        object?[]? defaults = null, string[]? kinds = null)
    {
        defaults ??= []; int required = names.Length - defaults.Length;
        var allDefaults = new object?[names.Length]; Array.Copy(defaults, 0, allDefaults, required, defaults.Length);
        var flags = names.Select((_, i) => i < required ? "0" : "1").ToArray();
        kinds ??= Enumerable.Repeat("pos", names.Length).ToArray();
        var sig = PySignature.Create(cls.Name + "." + name, names, kinds, flags);
        var function = new PyFunction(name, sig, (self, args, keys, values) =>
            body(PyBinder.Bind(sig, args, keys, values, self.Defaults)), allDefaults, false);
        PyOps.FunctionModule(function, "collections.abc"); cls.SetAttr(name, function); return function;
    }
    internal static object? Call(object? value, string name, params object?[] args) => PyOps.CallMethod(value, name, args, [], []);
    private static bool AbstractMember(object? value)
    {
        value = value switch { PyStaticMethod s => s.Value, PyClassMethod c => c.Value, _ => value };
        if (value is not PyFunction function) return false;
        object? marker;
        try { marker = PyObject.GetAttr(function, "__isabstractmethod__"); }
        catch (PyAttributeError) { marker = null; }
        if (AbstractFunctions.Contains(function))
        {
            if (marker is not true) throw new PyNotImplementedError("mutating abstract method markers is not supported");
            return true;
        }
        if (marker is not null) throw new PyNotImplementedError("custom abstract method markers are not supported");
        return false;
    }
    internal static void FinalizeAbstractMethods(PyClass cls)
    {
        var bases = cls.DirectBasesTuple().Items.OfType<PyClass>().ToArray();
        if (!IsAbc(cls) && !bases.Any(b => b.AbcAbstractNames is not null)) return;
        if (cls.Attributes.ContainsKey("__abstractmethods__")) throw new PyNotImplementedError("explicit __abstractmethods__ is not supported");
        var names = new HashSet<string>(StringComparer.Ordinal);
        // 只继承直接父类已冻结集合；不能因祖先后来重绑而复活已具体化方法。
        foreach (var parent in bases) if (parent.AbcAbstractNames is { } inherited) names.UnionWith(inherited);
        foreach (var pair in cls.Attributes) if (AbstractMember(pair.Value)) names.Add(pair.Key);
        names.RemoveWhere(name => !cls.TryLookup(name, out _, out object? value) || !AbstractMember(value));
        cls.AbcAbstractNames = names.Order(StringComparer.Ordinal).ToArray();
        cls.Attributes["__abstractmethods__"] = new PyFrozenSet(cls.AbcAbstractNames.Cast<object?>().ToArray());
    }
    internal static void RequireConcrete(PyClass cls)
    {
        if (cls.AbcAbstractNames is { Length: > 0 } names)
            throw new PyTypeError($"Can't instantiate abstract class {cls.Name} with abstract methods {string.Join(", ", names)}");
    }
    internal static void GuardAbstractMutation(PyClass cls, string name)
    {
        if (name == "__abstractmethods__" && cls.AbcAbstractNames is not null)
            throw new PyNotImplementedError("updating frozen abstract methods is not supported");
    }
}
public sealed partial class PyClass
{
    internal string[]? AbcAbstractNames { get; set; }
}
