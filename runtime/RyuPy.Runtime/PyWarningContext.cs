using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>catch_warnings的单次上下文；进入时复制过滤规则，退出即使遇异常也恢复外层。</summary>
internal sealed class PyWarningContext : PyNativeObject
{
    private readonly object?[] _options;
    private bool _entered;
    private List<PyWarningFilters.Rule>? _previous;
    private PyList? _previousRecords;
    internal PyWarningContext(object?[] options)
    {
        if (options[1] is not (null or PyNone))
            throw new PyNotImplementedError("catch_warnings module override is not supported");
        _options = (object?[])options.Clone();
    }
    /// <inheritdoc />
    public override string TypeName => "catch_warnings";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__enter__" => PyNativeFunctions.Create(name, [], [], 0, _ => Enter()),
        "__exit__" => PyNativeFunctions.Create(name, ["type", "value", "traceback"], [null, null, null], 3,
            _ => Exit(), ["posonly", "posonly", "posonly"]),
        _ => throw Missing(name),
    };
    private object? Enter()
    {
        if (_entered) throw new PyRuntimeError("Cannot enter catch_warnings twice");
        _entered = true;
        _previous = PyWarningFilters.Enter();
        _previousRecords = PyWarnings.Records;
        if (_options[2] is not (null or PyNone))
            PyWarningFilters.Simple(_options[2], _options[3], _options[4], _options[5]);
        if (!PyOps.Truthy(_options[0])) return PyNone.Instance;
        var records = new PyList();
        PyWarnings.Records = records;
        return records;
    }
    private bool Exit()
    {
        if (!_entered || _previous is null) throw new PyRuntimeError("Cannot exit catch_warnings without entering first");
        PyWarningFilters.Restore(_previous);
        PyWarnings.Records = _previousRecords;
        return false;
    }
}

/// <summary>捕获的WarningMessage只保存本次位置、真实异常对象与类别身份。</summary>
public sealed class PyWarningMessage : PyNativeObject
{
    internal object Category { get; }
    internal string Filename { get; }
    internal int LineNumber { get; }
    internal PyBaseException Message { get; }
    private readonly object? _source;
    internal PyWarningMessage(PyBaseException message, object category, string filename, int line, object? source)
    { Message = message; Category = category; Filename = filename; LineNumber = line; _source = source; }
    /// <inheritdoc />
    public override string TypeName => "WarningMessage";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "message" => Message, "category" => Category, "filename" => Filename,
        "lineno" => (long)LineNumber, "line" => PyNone.Instance, "source" => _source ?? PyNone.Instance,
        _ => throw Missing(name),
    };
}
