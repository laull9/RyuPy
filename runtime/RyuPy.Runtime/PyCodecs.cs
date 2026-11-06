// codecs 的公开 BOM 常量族：真实 bytes 值、别名身份与运行机器端序。
// 不注册编码器、registry 或占位函数，也不进入 str/bytes 编解码热路。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>只交付 CPython 3.12.13 的十四个公开 BOM 名称，不代表完整 codecs 模块。</summary>
public static class PyCodecs
{
    /// <summary>模块注册冷路创建常量；端序与 sys.byteorder 使用同一个运行时事实。</summary>
    public static Dictionary<string, object?> CreateAttributes() => CreateAttributes(BitConverter.IsLittleEndian);

    /// <summary>纯端序工厂，供两种架构分支验收；参数不会成为 Python 模块属性。</summary>
    /// <param name="littleEndian">目标运行机器是否为小端序。</param>
    public static Dictionary<string, object?> CreateAttributes(bool littleEndian)
    {
        // 五个不可变 bytes 对象；别名必须复用对象而非各自重新包装等值数组。
        var utf8 = new PyBytes([0xef, 0xbb, 0xbf]);
        var utf16Le = new PyBytes([0xff, 0xfe]);
        var utf16Be = new PyBytes([0xfe, 0xff]);
        var utf32Le = new PyBytes([0xff, 0xfe, 0x00, 0x00]);
        var utf32Be = new PyBytes([0x00, 0x00, 0xfe, 0xff]);
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["BOM_UTF8"] = utf8,
            ["BOM_LE"] = utf16Le, ["BOM_UTF16_LE"] = utf16Le, ["BOM32_LE"] = utf16Le,
            ["BOM_BE"] = utf16Be, ["BOM_UTF16_BE"] = utf16Be, ["BOM32_BE"] = utf16Be,
            ["BOM_UTF32_LE"] = utf32Le, ["BOM64_LE"] = utf32Le,
            ["BOM_UTF32_BE"] = utf32Be, ["BOM64_BE"] = utf32Be,
            ["BOM"] = littleEndian ? utf16Le : utf16Be,
            ["BOM_UTF16"] = littleEndian ? utf16Le : utf16Be,
            ["BOM_UTF32"] = littleEndian ? utf32Le : utf32Be,
        };
    }
}
