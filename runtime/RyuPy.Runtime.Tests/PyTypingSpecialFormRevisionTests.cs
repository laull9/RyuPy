// F0第二轮负样本回归：保留第一次失败证据，独立验证通用错误和原生别名暖命中。
// 预期来自固定CP双跑；Unicode长度按码点而非UTF16单元计算。
// CLR直接构造字符串的通过不等于Python孤代理字面量/全局repr端到端已兼容。
using System;
using System.Linq;
using Xunit;
using static RyuPy.Runtime.Tests.PyTypingGenericRootTests;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTypingSpecialFormRevisionTests : IDisposable
{
    private readonly PyTypingGenericRootTests frame = new();
    public void Dispose() => frame.Dispose();

    [Fact]
    public void InvalidStringKeysHaveGeneralPythonTypeErrorMessages()
    {
        foreach (var (key, typeName) in new (object?, string)[] {
            (new PyTuple([PyType.Get("int")]), "tuple"), ("key", "str"),
            (1.5, "float"), (PyNone.Instance, "NoneType"), (new PyList([]), "list") })
            Assert.Equal($"string indices must be integers, not '{typeName}'", Assert.Throws<PyTypeError>(() => PyOps.GetItem("abc", key)).Message);
    }

    [Fact]
    public void DirectClrStringOptionalTupleErrorsUseOneHundredCodePointRepr()
    {
        const string prefix = "typing.Optional requires a single type. Got ";
        string emoji = string.Concat(Enumerable.Repeat("😀", 110));
        string emojiExpected = prefix + "('" + string.Concat(Enumerable.Repeat("😀", 98)) + ".";
        string surrogate = new('\ud800', 110);
        string surrogateExpected = prefix + "('" + string.Concat(Enumerable.Repeat("\\ud800", 16)) + "\\u.";
        string ascii = new('a', 110);
        string asciiExpected = prefix + "('" + new string('a', 98) + ".";
        foreach (var (text, expected) in new[] { (emoji, emojiExpected), (surrogate, surrogateExpected), (ascii, asciiExpected) })
            Assert.Equal(expected, Assert.Throws<PyTypeError>(() => PyOps.GetItem(Form("Optional"), new PyTuple([text]))).Message);
    }

    [Fact]
    public void EquivalentNativeAliasWarmKeyDoesNotReadTypingGenericAliasHelper()
    {
        object? a = PyOps.GetItem(PyType.Get("list"), PyType.Get("int"));
        object? old = PyOps.GetItem(Form("Optional"), a);
        // 旧native Intern独立于SpecialForm，压力使b不再和a同一。
        for (long i = 0; i < 130; i++) PyOps.GetItem(Form("Literal"), i);
        object? b = PyOps.GetItem(PyType.Get("list"), PyType.Get("int"));
        Assert.NotSame(a, b); Assert.True(PyHash.Equal(a, b));
        bool existed = Typing.TryGetValue("_GenericAlias", out object? saved);
        Typing["_GenericAlias"] = PyNone.Instance;
        try { Assert.Same(old, PyOps.GetItem(Form("Optional"), b)); }
        finally
        {
            if (existed) Typing["_GenericAlias"] = saved;
            else Typing.Remove("_GenericAlias");
        }
    }
}
