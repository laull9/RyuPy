// 只验收 codecs 的 BOM 常量合同；不把编码器缺失或模拟端序当作完整模块/跨平台通过。
using System;
using System.Collections.Generic;
using System.Linq;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyCodecsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BothEndianBranchesPreserveAllValuesAndAliases(bool littleEndian)
    {
        var attributes = PyCodecs.CreateAttributes(littleEndian);
        string[] names = ["BOM", "BOM_BE", "BOM_LE", "BOM32_BE", "BOM32_LE", "BOM64_BE", "BOM64_LE",
            "BOM_UTF8", "BOM_UTF16", "BOM_UTF16_LE", "BOM_UTF16_BE", "BOM_UTF32", "BOM_UTF32_LE", "BOM_UTF32_BE"];
        Assert.Equal(names.OrderBy(name => name), attributes.Keys.OrderBy(name => name));
        Assert.Equal(5, attributes.Values.Distinct(ReferenceEqualityComparer.Instance).Count());
        Bytes(attributes, "BOM_UTF8", [0xef, 0xbb, 0xbf]);
        Bytes(attributes, "BOM_UTF16_LE", [0xff, 0xfe]);
        Bytes(attributes, "BOM_UTF16_BE", [0xfe, 0xff]);
        Bytes(attributes, "BOM_UTF32_LE", [0xff, 0xfe, 0x00, 0x00]);
        Bytes(attributes, "BOM_UTF32_BE", [0x00, 0x00, 0xfe, 0xff]);
        foreach (string suffix in new[] { "LE", "BE" })
        {
            Assert.Same(attributes["BOM_UTF16_" + suffix], attributes["BOM_" + suffix]);
            Assert.Same(attributes["BOM_UTF16_" + suffix], attributes["BOM32_" + suffix]);
            Assert.Same(attributes["BOM_UTF32_" + suffix], attributes["BOM64_" + suffix]);
        }
        string native = littleEndian ? "LE" : "BE";
        Assert.Same(attributes["BOM_UTF16_" + native], attributes["BOM"]);
        Assert.Same(attributes["BOM"], attributes["BOM_UTF16"]);
        Assert.Same(attributes["BOM_UTF32_" + native], attributes["BOM_UTF32"]);
    }

    [Fact]
    public void DefaultFactoryUsesRuntimeByteOrder()
    {
        var attributes = PyCodecs.CreateAttributes();
        string suffix = BitConverter.IsLittleEndian ? "LE" : "BE";
        Assert.Same(attributes["BOM_UTF16_" + suffix], attributes["BOM"]);
        Assert.Same(attributes["BOM_UTF32_" + suffix], attributes["BOM_UTF32"]);
    }

    [Fact]
    public void ModuleRebindingDoesNotChangeAliasesOrImportedSnapshot()
    {
        // 独立名字避免污染真实标准库及其他用例共享的模块缓存。
        string name = "codecs_bom_contract_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterNative(name, PyCodecs.CreateAttributes());
        PyModule module = PyModules.Import(name);
        Assert.Same(module, PyModules.Import(name));
        object? snapshot = PyModules.ImportAttr(name, "BOM_LE");
        var replacement = new PyBytes([1, 2]);
        module.SetAttr("BOM_LE", replacement);
        Assert.Same(replacement, module.GetAttr("BOM_LE"));
        Assert.Same(replacement, PyModules.ImportAttr(name, "BOM_LE"));
        Assert.Same(snapshot, module.GetAttr("BOM_UTF16_LE"));
        Assert.Same(snapshot, module.GetAttr("BOM32_LE"));
        foreach (string missing in new[] { "encode", "decode", "lookup", "register", "__all__", "not_an_api" })
        {
            Assert.Throws<PyAttributeError>(() => module.GetAttr(missing));
            Assert.Throws<PyImportError>(() => PyModules.ImportAttr(name, missing));
        }
    }

    private static void Bytes(Dictionary<string, object?> attributes, string name, byte[] expected) =>
        Assert.Equal(expected, Assert.IsType<PyBytes>(attributes[name]).Bytes);
}
