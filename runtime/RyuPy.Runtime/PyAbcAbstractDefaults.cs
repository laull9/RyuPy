// ABC抽象方法也有可供super调用的默认体；不能以禁止抽象类构造掩盖这些语义。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;
public static partial class PyAbc
{
    private static void InstallAbstract(PyClass cls, string name)
    {
        string[] parameters = name switch
        {
            "__contains__" => ["self", "x"], "__getitem__" or "__delitem__" => ["self", cls.Name.Contains("Mapping", StringComparison.Ordinal) ? "key" : "index"],
            "__setitem__" => ["self", cls.Name.Contains("Mapping", StringComparison.Ordinal) ? "key" : "index", "value"],
            "insert" => ["self", "index", "value"], "add" or "discard" => ["self", "value"],
            "send" => ["self", "value"], "throw" => ["self", "typ", "val", "tb"],
            "__call__" => ["self", "args", "kwds"], _ => ["self"],
        };
        var function = Method(cls, name, parameters, a => name switch
        {
            "__iter__" or "__reversed__" => new PyAbcIterator(Empty().GetEnumerator()),
            "__next__" or "send" => throw new PyStopIteration(),
            "__len__" => 0L, "__contains__" or "__call__" => false,
            "throw" => ThrowGenerator(a),
            "add" or "discard" => throw EmptyError(new PyNotImplementedError("")),
            _ when cls.Name.Contains("Mapping", StringComparison.Ordinal) => throw EmptyError(new PyKeyError("")),
            _ => throw EmptyError(new PyIndexError("")),
        }, name == "throw" ? [PyNone.Instance, PyNone.Instance] : null,
            name == "__call__" ? ["pos", "varpos", "varkw"] : null);
        PyObject.SetAttr(function, "__isabstractmethod__", true); AbstractFunctions.Add(function);
    }
    private static PyBaseException EmptyError(PyBaseException error)
    {
        error.InitializePositionalArguments([]); return error;
    }
    private static IEnumerable<object?> Empty() { yield break; }
    private static object? ThrowGenerator(object?[] a)
    {
        if (a[3] is not null and not PyNone) throw new PyNotImplementedError("Generator.throw traceback is not supported");
        object? exception = a[2] is null or PyNone ? a[1] : a[2];
        throw PyErrors.NormalizeRaise(exception!);
    }
    private static void InstallDefaults(PyClass cls)
    {
        if (cls.Name == "Iterator") Method(cls, "__iter__", ["self"], a => a[0]);
        if (cls.Name != "Generator") return;
        Method(cls, "__next__", ["self"], a => Call(a[0], "send", PyNone.Instance));
        Method(cls, "close", ["self"], a =>
        {
            try { Call(a[0], "throw", PyType.Get("GeneratorExit")); }
            catch (PyBaseException error) when (error.PyTypeName is "GeneratorExit" or "StopIteration") { return PyNone.Instance; }
            throw new PyRuntimeError("generator ignored GeneratorExit");
        });
    }
}
// 包装惰性正文；正常源耗尽与用户回调泄漏StopIteration必须区分。
internal sealed class PyAbcIterator(IEnumerator<object?> source) : PyToolIterator
{
    private bool ended;
    public override string TypeName => "generator";
    public override object? GetAttr(string name) => name is "send" or "throw" or "close"
        ? throw PyAbcIteration.UnsupportedControl(name) : base.GetAttr(name);
    public override bool MoveNext()
    {
        if (ended) return false;
        try { if (source.MoveNext()) return true; ended = true; source.Dispose(); return false; }
        catch (PyStopIteration error) { ended = true; source.Dispose(); throw PyAbcIteration.EscapingStop(error); }
        catch { ended = true; source.Dispose(); throw; }
    }
    public override object? Current() => source.Current;
}
