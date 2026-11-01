using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>受支持re模块入口；64项FIFO缓存有界，复杂或未实现语法明确拒绝，不直接透传.NET方言。</summary>
public static class PyRegex
{
    private static readonly Dictionary<(string, int), PyRegexPattern> Cache = [];
    private static readonly Queue<(string, int)> Order = [];
    private static readonly object CacheLock = new();
    private static readonly PyFunction ErrorConstructor = Function("error", ["msg", "pattern", "pos"],
        [null, PyNone.Instance, PyNone.Instance], 1, args => PyRegexError.FromArguments(args[0], args[1], args[2]));
    /// <summary>re.error真实异常构造器；统一binder处理必填、重复与未知关键字。</summary>
    public static PyRegexError ConstructError(object?[] args, string[] names, object?[] values) =>
        (PyRegexError)ErrorConstructor.Invoke(args, names, values)!;

    /// <summary>供统一原生模块注册器公开；未知API不会伪装完整re。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var result = new Dictionary<string, object?>
        {
            ["I"] = 2L, ["IGNORECASE"] = 2L, ["L"] = 4L, ["LOCALE"] = 4L,
            ["M"] = 8L, ["MULTILINE"] = 8L, ["S"] = 16L, ["DOTALL"] = 16L,
            ["U"] = 32L, ["UNICODE"] = 32L, ["X"] = 64L, ["VERBOSE"] = 64L,
            ["A"] = 256L, ["ASCII"] = 256L, ["NOFLAG"] = 0L,
            ["compile"] = Function("compile", ["pattern", "flags"], [null, 0L], 1, args => Compile(args[0], args[1])),
            ["escape"] = Function("escape", ["pattern"], [null], 1, args => Escape(Text(args[0]))),
            ["purge"] = Function("purge", [], [], 0, _ => { lock (CacheLock) { Cache.Clear(); Order.Clear(); } return PyNone.Instance; }),
            ["Pattern"] = new PyNativeType("Pattern", "re", null, value => value is PyRegexPattern),
            ["Match"] = new PyNativeType("Match", "re", null, value => value is PyRegexMatch),
            ["error"] = PyType.Get("error"),
        };
        foreach (string method in new[] { "match", "search", "fullmatch", "findall", "finditer" })
        {
            string captured = method;
            result[method] = Function(method, ["pattern", "string", "flags"], [null, null, 0L], 2,
                args => Compile(args[0], args[2]).Apply(captured, Text(args[1]), 0, int.MaxValue));
        }
        foreach (string method in new[] { "sub", "subn" })
        {
            bool tuple = method == "subn";
            result[method] = Function(method, ["pattern", "repl", "string", "count", "flags"], [null, null, null, 0L, 0L], 3,
                args => Compile(args[0], args[4]).Substitute(args[1], Text(args[2]), Number(args[3]), tuple));
        }
        return result;
    }
    internal static PyFunction Function(string name, string[] names, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, defaults, required, body);
    /// <summary>编译已验证的Python子集，已编译模式禁止另传flags。</summary>
    public static PyRegexPattern Compile(object? value, object? flagValue = null)
    {
        int flags = flagValue is null ? 0 : Number(flagValue);
        if (value is PyRegexPattern compiled)
        {
            if (flags != 0) throw new PyValueError("cannot process flags argument with a compiled pattern");
            return compiled;
        }
        string pattern = Text(value); CheckFlags(flags);
        lock (CacheLock)
        {
            if (Cache.TryGetValue((pattern, flags), out var cached)) return cached;
            var result = new PyRegexPattern(pattern, flags);
            if (Cache.Count >= 64) Cache.Remove(Order.Dequeue());
            Cache[(pattern, flags)] = result; Order.Enqueue((pattern, flags)); return result;
        }
    }
    internal static void CheckFlags(int flags)
    {
        if ((flags & 4) != 0) throw new PyValueError("cannot use LOCALE flag with a str pattern");
        if ((flags & (256 | 32)) == (256 | 32)) throw new PyValueError("ASCII and UNICODE flags are incompatible");
        if ((flags & ~(2 | 8 | 16 | 32 | 64 | 256)) != 0) throw new PyNotImplementedError("re: unsupported flags");
    }
    internal static string Text(object? value) => value switch
    {
        string text => text,
        PyBytes or PyByteArray => throw new PyNotImplementedError("re: bytes patterns and subjects are not supported"),
        _ => throw new PyTypeError("expected string or compiled pattern"),
    };
    internal static int Number(object? value)
    {
        BigInteger number;
        if (!Integer(value, out number))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out object? indexed) || !Integer(indexed, out number))
                throw new PyTypeError("integer argument expected");
        }
        if (number < int.MinValue || number > int.MaxValue) throw new PyOverflowError("Python int too large to convert to C int");
        return (int)number;
    }
    private static bool Integer(object? value, out BigInteger number)
    {
        switch (value)
        {
            case long n: number = n; return true;
            case int n: number = n; return true;
            case bool n: number = n ? 1 : 0; return true;
            case BigInteger n: number = n; return true;
            default: number = 0; return false;
        }
    }
    private static string Escape(string value)
    {
        var result = new StringBuilder();
        foreach (char c in value) { if ("()[]{}?*+-|^$\\.&~# \t\n\r\v\f".Contains(c)) result.Append('\\'); result.Append(c); }
        return result.ToString();
    }
}

/// <summary>Python re.error；错误文本与原模式/码点位置分离，允许用户构造时省略位置。</summary>
public sealed class PyRegexError : PyException
{
    private sealed record Details(object? Message, object? Pattern, object? Position, object? Line, object? Column, object? Display);
    private readonly object? _display;
    /// <summary>未附加位置的原始Python错误消息。</summary>
    public object? MessageText { get; }
    /// <summary>原Python模式或替换模板，可为None。</summary>
    public object? Pattern { get; }
    /// <summary>原文本中的零基码点位置，可为None。</summary>
    public object? Position { get; }
    /// <summary>原文本中的一基行号，可为None。</summary>
    public object? LineNumber { get; }
    /// <summary>原文本中的一基列号，可为None。</summary>
    public object? ColumnNumber { get; }
    /// <summary>编译器内部按UTF16偏移构造，先换算成原模式的Python码点位置。</summary>
    public PyRegexError(string message, string pattern, int offset)
        : this(Build(message, pattern, (long)new PyRegexText(pattern[..Math.Clamp(offset, 0, pattern.Length)]).Length)) { }
    private PyRegexError(Details details) : base("error", "", [details.Display])
    {
        MessageText = details.Message; Pattern = details.Pattern; Position = details.Position;
        LineNumber = details.Line; ColumnNumber = details.Column; _display = details.Display;
    }
    /// <summary>无位置时不提前格式化用户msg对象，保留与普通异常一致的延迟str。</summary>
    public override string PyMessage => PyRepr.Str(_display);
    internal static PyRegexError FromArguments(object? message, object? pattern, object? position) => new(Build(message, pattern, position));
    private static Details Build(object? message, object? pattern, object? position)
    {
        pattern ??= PyNone.Instance; position ??= PyNone.Instance;
        if (pattern is PyNone || position is PyNone)
            return new(message, pattern, position, PyNone.Instance, PyNone.Instance, message);
        string source = PyRegex.Text(pattern);
        if (message is not string text) throw new PyTypeError("error message must be str when position is specified");
        int point = PyRegex.Number(position); var mapping = new PyRegexText(source);
        int stop = point < 0 ? Math.Max(0, mapping.Length + point) : Math.Min(mapping.Length, point);
        string prefix = source[..mapping.Offsets[stop]]; long line = 1;
        foreach (char c in prefix) if (c == '\n') line++;
        int newline = prefix.LastIndexOf('\n');
        long column = point - (newline < 0 ? -1 : new PyRegexText(prefix[..newline]).Length);
        string display = text + " at position " + PyRepr.Str(position);
        if (source.Contains('\n')) display += $" (line {line}, column {column})";
        return new(message, pattern, position, line, column, display);
    }
}
