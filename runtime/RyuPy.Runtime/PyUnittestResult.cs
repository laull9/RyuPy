using System;

namespace RyuPy.Runtime;

/// <summary>结构化 TestResult：保存测试对象与安全格式化的错误文本，不输出不稳定的计时信息。</summary>
public sealed class PyUnittestResult : PyNativeObject
{
    private long _testsRun;
    private bool _shouldStop;
    private readonly PyList _failures = new();
    private readonly PyList _errors = new();
    /// <inheritdoc />
    public override string TypeName => "TestResult";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "testsRun" => _testsRun, "shouldStop" => _shouldStop,
        "failures" => _failures, "errors" => _errors,
        "wasSuccessful" => PyNativeFunctions.Create(name, [], [], 0, _ => _failures.Count == 0 && _errors.Count == 0),
        "stop" => PyNativeFunctions.Create(name, [], [], 0, _ => { _shouldStop = true; return PyNone.Instance; }),
        "startTest" => PyNativeFunctions.Create(name, ["test"], [null], 1, _ => { _testsRun++; return PyNone.Instance; }),
        "stopTest" or "addSuccess" => PyNativeFunctions.Create(name, ["test"], [null], 1, _ => PyNone.Instance),
        "startTestRun" or "stopTestRun" => PyNativeFunctions.Create(name, [], [], 0, _ => PyNone.Instance),
        "addFailure" or "addError" => PyNativeFunctions.Create(name, ["test", "err"], [null, null], 2,
            a => Add(name == "addFailure" ? _failures : _errors, a[0], a[1])),
        _ => throw Missing(name),
    };
    private static object Add(PyList target, object? test, object? errorInfo)
    {
        if (errorInfo is not PyTuple { Length: 3 } info || info.Items[1] is not PyBaseException error)
            throw new PyTypeError("err must be an exception info tuple");
        target.Append(new PyTuple([test, PyTraceback.Format(error)]));
        return PyNone.Instance;
    }
    /// <inheritdoc />
    public override void SetAttr(string name, object? value)
    {
        if (name == "shouldStop") { _shouldStop = PyOps.Truthy(value); return; }
        throw Missing(name);
    }
}
