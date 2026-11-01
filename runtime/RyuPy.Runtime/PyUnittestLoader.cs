using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>按测试方法名称发现用户 TestCase；读取普通 MRO，子类覆盖同名测试而不重复执行。</summary>
public sealed class PyUnittestLoader : PyNativeObject
{
    /// <inheritdoc />
    public override string TypeName => "TestLoader";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "loadTestsFromTestCase" => PyNativeFunctions.Create(name, ["testCaseClass"], [null], 1, a => Load(a[0])),
        "getTestCaseNames" => PyNativeFunctions.Create(name, ["testCaseClass"], [null], 1, a => new PyList(Names(Class(a[0])))),
        _ => throw Missing(name),
    };
    private static PyClass Class(object? value)
    {
        if (value is not PyClass cls || !cls.IsSubclassOf(PyUnittest.TestCase))
            throw new PyTypeError("testCaseClass must be a subclass of TestCase");
        return cls;
    }
    private static List<object?> Names(PyClass cls)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        for (PyClass? current = cls; current is not null; current = current.BaseClass)
            foreach (string name in current.Attributes.Keys) found.Add(name);
        var result = new List<object?>();
        foreach (string name in found)
            if (name.StartsWith("test", StringComparison.Ordinal) && PyUnittest.Callable(PyObject.GetAttr(cls, name))) result.Add(name);
        // Python 字符串比较按码点；不用 CLR ordinal 排序补充平面字符。
        return new List<object?>(PyOps.ToSequence(PyOps.CallBuiltin("sorted", [new PyList(result)], [], [])));
    }
    private static object Load(object? value)
    {
        PyClass cls = Class(value);
        if (cls.TryLookup("setUpClass", out _, out _) || cls.TryLookup("tearDownClass", out _, out _))
            throw new PyNotImplementedError("unittest class fixtures are not supported");
        List<object?> names = Names(cls);
        if (names.Count == 0 && cls.TryLookup("runTest", out _, out _)) names.Add("runTest");
        var tests = new PyList();
        foreach (object? name in names) tests.Append(cls.Invoke([name], [], []));
        return new PyUnittestSuite(tests);
    }
}

/// <summary>TestSuite 顺序执行并清除已运行的槽；重复执行不得悄悄重跑已消耗测试。</summary>
public sealed class PyUnittestSuite : PyNativeObject, IPyNativeCallable
{
    private readonly PyList _tests = new();
    private long _removed;
    /// <summary>构造时消费测试序列，保存其对象身份。</summary>
    public PyUnittestSuite(object? tests)
    {
        IPyIterator iterator = PyIter.Create(tests);
        while (iterator.MoveNext()) Add(iterator.Current());
    }
    /// <inheritdoc />
    public override string TypeName => "TestSuite";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "run" or "__call__" => PyNativeFunctions.Create(name, ["result", "debug"], [null, false], 1,
            a => PyOps.Truthy(a[1]) ? throw new PyNotImplementedError("TestSuite debug mode is not supported") : Run(a[0]!)),
        "countTestCases" => PyNativeFunctions.Create(name, [], [], 0, _ => Count()),
        "addTest" => PyNativeFunctions.Create(name, ["test"], [null], 1, a => { Add(a[0]); return PyNone.Instance; }),
        "addTests" => PyNativeFunctions.Create(name, ["tests"], [null], 1, a =>
        {
            IPyIterator iterator = PyIter.Create(a[0]); while (iterator.MoveNext()) Add(iterator.Current());
            return PyNone.Instance;
        }),
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => PyIter.Create(_tests)),
        _ => throw Missing(name),
    };
    /// <summary>嵌套 suite 与 TestCase 一样通过真实可调用协议执行。</summary>
    public object? Invoke(object?[] args, string[] names, object?[] values) =>
        PyOps.CallValue(GetAttr("run"), args, names, values);
    private void Add(object? test)
    {
        if (!PyUnittest.Callable(test)) throw new PyTypeError("test is not callable");
        if (test is not PyUnittestSuite && (test is not PyInstance instance || !instance.Class.IsSubclassOf(PyUnittest.TestCase)))
            throw new PyNotImplementedError("TestSuite supports TestCase and TestSuite objects only");
        _tests.Append(test);
    }
    private long Count()
    {
        long total = _removed;
        foreach (object? test in _tests.Items) if (test is not null and not PyNone) total += PyCast.ToInt64(PyUnittest.Call(test, "countTestCases"));
        return total;
    }
    private object Run(object result)
    {
        for (int i = 0; i < _tests.Count; i++)
        {
            if (PyOps.Truthy(PyObject.GetAttr(result, "shouldStop"))) break;
            object? test = _tests.Items[i];
            if (test is null or PyNone) throw new PyTypeError("'NoneType' object is not callable");
            if (test is PyInstance instance &&
                (instance.Class.TryLookup("setUpClass", out _, out _) || instance.Class.TryLookup("tearDownClass", out _, out _)))
                throw new PyNotImplementedError("unittest class fixtures are not supported");
            PyOps.CallValue(test, [result], [], []);
            _removed += PyCast.ToInt64(PyUnittest.Call(test, "countTestCases"));
            _tests.Items[i] = PyNone.Instance;
        }
        return result;
    }
}
