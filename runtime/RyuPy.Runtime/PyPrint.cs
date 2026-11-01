namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>动态print先校验选项，固定本次输出目标；每个write保留用户副作用顺序。</summary>
    private static object? BuiltinPrint(object?[] args, string[] names, object?[] values)
    {
        RejectUnknownKeywords("print", names, "sep", "end", "file", "flush");
        bool flush = Truthy(KeywordValue(names, values, "flush"));
        object? requested = KeywordValue(names, values, "file");
        object? target = requested is null or PyNone ? PySystem.Stdout : requested;
        if (target is null or PyNone) return PyNone.Instance;
        object? sep = KeywordValue(names, values, "sep");
        object? end = KeywordValue(names, values, "end");
        string separator = sep is null or PyNone ? " " : PyCast.ToStr(sep);
        string terminator = end is null or PyNone ? "\n" : PyCast.ToStr(end);
        for (int index = 0; index < args.Length; index++)
        {
            if (index > 0) PySystem.Write(target, separator);
            PySystem.Write(target, PyRepr.Str(args[index]));
        }
        PySystem.Write(target, terminator);
        if (flush) PySystem.Flush(target);
        return PyNone.Instance;
    }
    /// <summary>input读取当前标准输入的一行；EOF与空行不同，输出提示后先刷新。</summary>
    private static object BuiltinInput(object?[] args, string[] names)
    {
        RejectUnknownKeywords("input", names);
        if (args.Length > 1) throw new PyTypeError("input expected at most 1 argument");
        if (args.Length > 0) PySystem.WriteStdout(PyRepr.Str(args[0]));
        PySystem.Flush(PySystem.Stdout);
        if (PySystem.Stdin is null or PyNone) throw new PyRuntimeError("input(): lost sys.stdin");
        object? result = CallValue(PyObject.GetAttr(PySystem.Stdin, "readline"), [], [], []);
        string line = PyCast.ToStr(result);
        if (line.Length == 0) throw new PyEOFError("EOF when reading a line");
        if (line.EndsWith("\n", System.StringComparison.Ordinal)) line = line[..^1];
        return line;
    }

}
