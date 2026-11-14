// textwrap的手写矩阵与小字母表逐结果差分：固定CPython当次生成预期，不保留基线或摘要。
using System.Text.Json;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>独立固定CPython3.12.13黑盒矩阵：Unicode/空白/连字符/长词和参数错误。</summary>
public class PyTextWrapTests
{
    private static object? Invoke(string name, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyTextWrap.CreateAttributes()[name], args, names ?? [], values ?? []);

    [Fact]
    public void FixedBaselineMatrixMatchesAllSupportedOptions()
    {
        using var data = TextWrapOracle.Read("matrix");
        Assert.Equal(1200, data.RootElement.GetArrayLength());
        foreach (JsonElement row in data.RootElement.EnumerateArray()) CheckRow(row);
    }

    [Fact]
    public void ExhaustiveSmallAlphabetPreservesBoundaryDecisions()
    {
        using var data = TextWrapOracle.Read("exhaustive");
        Assert.Equal(87360, data.RootElement.GetArrayLength());
        foreach (JsonElement row in data.RootElement.EnumerateArray()) CheckRow(row);
    }

    private static void CheckRow(JsonElement row)
    {
        var names = new List<string>(); var values = new List<object?>();
        foreach (var option in row.GetProperty("options").EnumerateObject())
        {
            names.Add(option.Name);
            values.Add(option.Value.ValueKind switch
            {
                JsonValueKind.String => option.Value.GetString(),
                JsonValueKind.Number => option.Value.GetInt64(),
                _ => option.Value.GetBoolean(),
            });
        }
        string text = row.GetProperty("text").GetString()!;
        long width = row.GetProperty("width").GetInt64();
        var result = Assert.IsType<PyList>(Invoke("wrap", [text, width], names.ToArray(), values.ToArray()));
        string[] expected = row.GetProperty("expected").EnumerateArray().Select(value => value.GetString()!).ToArray();
        Assert.True(expected.SequenceEqual(result.Items.Cast<string>()),
            $"text={PyRepr.Repr(text)} width={width} options={row.GetProperty("options")} actual={PyRepr.Repr(result)}");
    }

    [Fact]
    public void LongHyphenRunsRemainLinearAndKeepFixedBaselineOutput()
    {
        string separator = new('-', 100000);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        foreach (string text in new[] { separator, "a" + separator, separator + "a", "a" + separator + "b" })
        {
            var broken = Assert.IsType<PyList>(Invoke("wrap", [text, 70L]));
            Assert.Equal(1429, broken.Count);
            Assert.Equal(text, string.Concat(broken.Items.Cast<string>()));
            Assert.All(broken.Items.Cast<string>(), line => Assert.InRange(line.Length, 1, 70));
            var whole = Assert.IsType<PyList>(Invoke("wrap", [text, 70L], ["break_long_words"], [false]));
            Assert.Equal(text == "a" + separator + "b" ? 3 : 1, whole.Count);
            Assert.Equal(text, string.Concat(whole.Items.Cast<string>()));
        }
        // 宽松防退化界限而非速度承诺：旧逐位置重扫会对8次输入执行约400亿比较。
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "long hyphen runs regressed to superlinear scanning");
    }

    [Fact]
    public void FillAndInvalidWidthPreserveErrorOrder()
    {
        Assert.Equal("one two\nthree\nfour", Invoke("fill", ["one two three four", 7L]));
        foreach (string text in new[] { "", "   ", "abc" })
            Assert.Throws<PyValueError>(() => Invoke("wrap", [text, 0L]));
        Assert.Throws<PyAttributeError>(() => Invoke("wrap", [PyNone.Instance, 0L]));
        Assert.Throws<PyAttributeError>(() => Invoke("wrap", [1L, 0L]));
        Assert.Throws<PyTypeError>(() => Invoke("wrap", ["abc"], ["unknown"], [true]));
        Assert.Throws<PyTypeError>(() => Invoke("wrap", ["abc", 4L, "indent"]));
    }

    [Fact]
    public void UnimplementedModesFailExplicitly()
    {
        Assert.Throws<PyNotImplementedError>(() => Invoke("wrap", ["abc"], ["fix_sentence_endings"], [true]));
        Assert.Throws<PyNotImplementedError>(() => Invoke("wrap", ["abc"], ["max_lines"], [1L]));
        Assert.Equal(new object?[] { "abc" }, Assert.IsType<PyList>(Invoke("wrap", ["abc"], ["placeholder"], ["..."])).Items);
        Assert.Throws<PyNotImplementedError>(() => Invoke("wrap", ["abc", 1.5]));
    }
}
