using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>固定Unicode15属性、全码点摘要、代理与参数错误；不把.NET当前版本当预期。</summary>
public class PyUnicodeDataTests
{
    private static object? Call(string name, params object?[] args) =>
        PyOps.CallValue(PyUnicodeData.CreateAttributes()[name], args, [], []);

    [Fact]
    public void AllCodepointsMatchFixedPython31213Digest()
    {
        string[] widths = ["N", "Na", "A", "H", "W", "F"];
        ulong hash = 14695981039346656037UL;
        for (int point = 0; point <= 0x10ffff; point++)
        {
            string text = point <= 0xffff ? new string((char)point, 1) : char.ConvertFromUtf32(point);
            int width = Array.IndexOf(widths, Assert.IsType<string>(Call("east_asian_width", text)));
            long combining = Assert.IsType<long>(Call("combining", text));
            Assert.InRange(width, 0, 5);
            hash = unchecked((hash ^ (ulong)width) * 1099511628211UL);
            hash = unchecked((hash ^ (ulong)combining) * 1099511628211UL);
        }
        Assert.Equal(17127505844844960952UL, hash);
    }

    [Fact]
    public void SurrogatesUnassignedAndSupplementaryCharactersKeepTheirProperties()
    {
        Assert.Equal("15.0.0", PyUnicodeData.CreateAttributes()["unidata_version"]);
        foreach (string text in new[] { "\ud800", "\udfff", "\u0378", char.ConvertFromUtf32(0x10ffff) })
        {
            Assert.Equal("N", Call("east_asian_width", text));
            Assert.Equal(0L, Call("combining", text));
        }
        Assert.Equal("W", Call("east_asian_width", char.ConvertFromUtf32(0x1f600)));
        Assert.Equal("A", Call("east_asian_width", "\u0301"));
        Assert.Equal(230L, Call("combining", "\u0301"));
        Assert.Equal(216L, Call("combining", char.ConvertFromUtf32(0x1d165)));
    }

    [Fact]
    public void ParametersMustBeOnePythonCharacterAndPositionalOnly()
    {
        foreach (string name in new[] { "east_asian_width", "combining" })
        {
            foreach (object? value in new object?[] { "", "ab", "e\u0301", 1L, PyNone.Instance, new PyBytes([97]) })
                Assert.Throws<PyTypeError>(() => Call(name, value));
            Assert.Throws<PyTypeError>(() => Call(name));
            Assert.Throws<PyTypeError>(() => Call(name, "a", "b"));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyUnicodeData.CreateAttributes()[name], [], ["chr"], ["a"]));
        }
    }
}
