using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>textwrap有限常用API：独立码点分段与贪心换行，不复制CPython实现或分词正则。</summary>
public static class PyTextWrap
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>稳定wrap/fill导出；其余API不提供伪造占位。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    private static Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (string name in new[] { "wrap", "fill" })
        {
            bool fill = name == "fill";
            result[name] = PyNativeFunctions.Create(name,
                ["text", "width", "initial_indent", "subsequent_indent", "expand_tabs", "replace_whitespace",
                 "fix_sentence_endings", "break_long_words", "drop_whitespace", "break_on_hyphens", "tabsize", "max_lines", "placeholder"],
                [null, 70L, "", "", true, true, false, true, true, true, 8L, PyNone.Instance, " [...]"], 1,
                args => Run(args, fill), ["pos", "pos", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly",
                    "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly"]);
        }
        return result;
    }

    private static object Run(object?[] args, bool fill)
    {
        bool expand = Flag(args[4]), replace = Flag(args[5]);
        if (Flag(args[6]) || args[11] is not (null or PyNone) || args[12] is not string placeholder || placeholder != " [...]")
            throw new PyNotImplementedError("textwrap sentence fixing, max_lines and custom placeholder are not supported");
        if (args[0] is not string text)
        {
            if (args[0] is PyInstance or PyNativeObject)
                throw new PyNotImplementedError("textwrap custom text protocols are not supported");
            if (args[0] is PyBytes or PyByteArray)
                throw new PyTypeError("textwrap requires str, not bytes");
            if (expand || replace)
                throw new PyAttributeError($"'{PyRepr.TypeName(args[0])}' object has no attribute '{(expand ? "expandtabs" : "translate")}'");
            throw new PyTypeError("expected string or bytes-like object");
        }
        // 先处理文本再检查width，保持默认参数下text错误优先于非正width。
        long tabsize = Integer(args[10], "tabsize");
        string normalized = PyTextWrapEngine.Normalize(text, expand, replace, tabsize);
        long width = Integer(args[1], "width");
        if (width <= 0) throw new PyValueError($"invalid width {width} (must be > 0)");
        if (args[2] is not string first || args[3] is not string rest)
            throw new PyNotImplementedError("textwrap indents must be actual strings");
        List<string> lines = PyTextWrapEngine.Wrap(normalized, width, first, rest,
            Flag(args[7]), Flag(args[8]), Flag(args[9]));
        if (fill) return string.Join('\n', lines);
        var values = new object?[lines.Count];
        for (int index = 0; index < lines.Count; index++) values[index] = lines[index];
        return new PyList(values);
    }

    private static bool Flag(object? value) => value is bool flag ? flag :
        throw new PyNotImplementedError("textwrap flags currently require actual bool values");

    private static long Integer(object? value, string name)
    {
        if (!PyInt.IsInt(value))
        {
            if (value is double or float) throw new PyNotImplementedError("textwrap non-integer " + name + " is not supported");
            throw new PyTypeError("textwrap " + name + " must be an integer");
        }
        BigInteger number = PyInt.ToBigInteger(value!);
        if (number < long.MinValue || number > long.MaxValue)
            throw new PyNotImplementedError("textwrap integer exceeds the supported resource range");
        return (long)number;
    }
}
