using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>固定Unicode15.0.0的有限属性API；支持所有码点含孤代理，不借用宿主Unicode分类。</summary>
public static class PyUnicodeData
{
    private static readonly string[] WidthNames = ["N", "Na", "A", "H", "W", "F"];
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(() => new(StringComparer.Ordinal)
    {
        ["unidata_version"] = "15.0.0",
        ["east_asian_width"] = PyNativeFunctions.Create("east_asian_width", ["chr"], [null], 1,
            args => WidthNames[Lookup(PyUnicodeDataTables.EastAsianWidth, Character(args[0], "east_asian_width"))], ["posonly"]),
        ["combining"] = PyNativeFunctions.Create("combining", ["chr"], [null], 1,
            args => PyInt.Box(Lookup(PyUnicodeDataTables.Combining, Character(args[0], "combining"))), ["posonly"]),
    });

    /// <summary>稳定的模块导出；未支持的UCD API不伪造占位实现。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;

    private static int Character(object? value, string function)
    {
        if (value is string text)
        {
            if (text.Length == 1) return text[0];
            if (text.Length == 2 && char.IsSurrogatePair(text, 0)) return char.ConvertToUtf32(text, 0);
        }
        throw new PyTypeError($"{function}() argument must be a unicode character, not {PyRepr.TypeName(value)}");
    }

    private static int Lookup(int[] ranges, int codepoint)
    {
        int low = 0, high = ranges.Length / 3 - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int offset = middle * 3;
            if (codepoint < ranges[offset]) high = middle - 1;
            else if (codepoint > ranges[offset + 1]) low = middle + 1;
            else return ranges[offset + 2];
        }
        return 0;
    }
}
