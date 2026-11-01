using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>string 模块的固定 ASCII 分类常量；不把模板/Unicode 标题化假实现为字符串拼接。</summary>
public static class PyStringModule
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Punctuation = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
    private const string Whitespace = " \t\n\r\v\f";

    /// <summary>常量内容不依赖宿主文化或区域设置。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["ascii_lowercase"] = Lower,
        ["ascii_uppercase"] = Upper,
        ["ascii_letters"] = Lower + Upper,
        ["digits"] = Digits,
        ["hexdigits"] = Digits + "abcdefABCDEF",
        ["octdigits"] = "01234567",
        ["punctuation"] = Punctuation,
        ["whitespace"] = Whitespace,
        ["printable"] = Digits + Lower + Upper + Punctuation + Whitespace,
    };
}
