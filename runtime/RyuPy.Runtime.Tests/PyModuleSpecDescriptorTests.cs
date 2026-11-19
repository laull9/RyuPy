// ModuleSpec公开property/方法的重复读取和空format协议，不用局部缓存吞用户描述符副作用。
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyModuleSpecDescriptorTests
{
    private static object Type()
    {
        PyNativeModules.Register(); return PyModules.ImportAttr("importlib.machinery", "ModuleSpec")!;
    }
    private static PyFunction Unary(string name, System.Func<object?, object?> body) =>
        PyNativeFunctions.Create(name, ["self"], [null], 1, a => body(a[0]));

    [Fact]
    public void CachedReadsOriginTwiceAndAlwaysReadsFinalCachedValue()
    {
        var trace = new List<string>(); int reads = 0; object? cache = PyNone.Instance;
        var cls = new PyClass("Observed");
        cls.SetAttr("origin", new PyProperty(Unary("origin", _ => {
            trace.Add("origin-get"); return ++reads == 1 ? "/virtual/first.py" : "/virtual/second.py";
        })));
        cls.SetAttr("_cached", new PyProperty(Unary("_cached", _ => { trace.Add("cached-get"); return cache; }),
            PyNativeFunctions.Create("_cached", ["self", "value"], [null, null], 2,
                a => { trace.Add("cached-set"); cache = a[1]; return PyNone.Instance; })));
        object view = PyOps.CallValue(cls, [], [], [])!; PyObject.SetAttr(view, "_set_fileattr", true);
        var getter = ((PyProperty)PyObject.GetAttr(Type(), "cached")!).Getter;
        object? Read() => PyOps.CallValue(getter, [view], [], []);
        object? first = Read(); Assert.Equal("/virtual/__pycache__/second.cpython-312.pyc", first);
        Assert.Equal(new[] { "cached-get", "origin-get", "origin-get", "cached-set", "cached-get" }, trace);
        trace.Clear(); Assert.Same(first, Read()); Assert.Equal(new[] { "cached-get", "cached-get" }, trace);
        cache = PyNone.Instance; PyObject.SetAttr(view, "_set_fileattr", false); trace.Clear();
        Assert.Same(PyNone.Instance, Read()); Assert.Equal(new[] { "cached-get", "origin-get", "cached-get" }, trace);
    }

    [Fact]
    public void ReprFormatsLocationsAndDynamicClassNameInsteadOfCallingStr()
    {
        var trace = new List<string>();
        object Formattable(string prefix, string result)
        {
            var cls = new PyClass("Formattable");
            cls.SetAttr("__str__", Unary("__str__", _ => { trace.Add(prefix + "-str"); return "wrong"; }));
            cls.SetAttr("__format__", PyNativeFunctions.Create("__format__", ["self", "spec"], [null, null], 2,
                a => { trace.Add(prefix + "-format:" + (string)a[1]!); return result; }));
            return PyOps.CallValue(cls, [], [], [])!;
        }
        object spec = PyOps.CallValue(Type(), ["example", PyNone.Instance], [], [])!;
        PyObject.SetAttr(spec, "submodule_search_locations", Formattable("locations", "formatted-locations"));
        Assert.Equal("ModuleSpec(name='example', loader=None, submodule_search_locations=formatted-locations)", PyRepr.Repr(spec));
        Assert.Equal(new[] { "locations-format:" }, trace);
        var classView = new PyClass("ClassView"); classView.SetAttr("__name__", Formattable("name", "FormattedName"));
        object nameView = PyOps.CallValue(classView, [], [], [])!;
        var viewClass = new PyClass("SpecView");
        viewClass.SetAttr("name", "view"); viewClass.SetAttr("loader", PyNone.Instance);
        viewClass.SetAttr("origin", PyNone.Instance); viewClass.SetAttr("submodule_search_locations", PyNone.Instance);
        viewClass.SetAttr("__class__", new PyProperty(Unary("__class__", _ => nameView)));
        object view = PyOps.CallValue(viewClass, [], [], [])!; trace.Clear();
        object? result = PyOps.CallValue(PyObject.GetAttr(Type(), "__repr__"), [view], [], []);
        Assert.Equal("FormattedName(name='view', loader=None)", result); Assert.Equal(new[] { "name-format:" }, trace);
    }
}
