// 固定码点宽度后顾的冷路径：与主解析器共享token消费，结束整份语法后才验证宽度。
// 正文只准入无捕获、无引用、无显式断言的纯字符语言，避免宿主反向求值改变可观察状态。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

internal sealed partial class PyRegexParser
{
    // 普通解析器仅增加一个空引用；宽度值与验证列表都留在首次后顾才创建的状态中。
    private LookbehindState? _lookbehind;
    private bool _inLookbehind => _lookbehind?.InBody == true;
    private Width _groupWidth
    {
        get => _lookbehind!.GroupWidth;
        set => _lookbehind!.GroupWidth = value;
    }
    private sealed class LookbehindState
    {
        internal bool InBody, LargeRepeat;
        internal Width GroupWidth;
        internal readonly List<(Width Width, bool LargeRepeat)> Widths = [];
    }

    // BigInteger只用于后顾编译冷路径，连接和嵌套重复不会发生CLR整数溢出。
    // Max=null表示无上界；与任意有限巨大值明确区分，零次重复仍准确归零。
    private readonly record struct Width(BigInteger Min, BigInteger? Max)
    {
        internal static Width One => new(BigInteger.One, BigInteger.One);
        internal static Width Zero => new(BigInteger.Zero, BigInteger.Zero);
        internal static Width Add(Width a, Width b) => new(a.Min + b.Min,
            a.Max is { } x && b.Max is { } y ? x + y : null);
        internal static Width Choice(Width a, Width b) => new(BigInteger.Min(a.Min, b.Min),
            a.Max is { } x && b.Max is { } y ? BigInteger.Max(x, y) : null);
        internal Width Repeat(BigInteger min, BigInteger? max) => new(Min * min,
            Max == BigInteger.Zero || max == BigInteger.Zero ? BigInteger.Zero :
            Max is { } x && max is { } y ? x * y : null);
    }

    // 尚未归并的最后原子允许量词修改；分支只在遇到“|”或组尾时归并。
    private sealed class WidthSequence
    {
        private Width _prefix = Width.Zero, _atom = Width.Zero;
        private Width? _alternatives;
        internal void Atom(Width atom) { _prefix = Width.Add(_prefix, _atom); _atom = atom; }
        internal void Repeat(BigInteger min, BigInteger? max) => _atom = _atom.Repeat(min, max);
        internal void Alternate()
        {
            Width branch = Width.Add(_prefix, _atom);
            _alternatives = _alternatives is { } old ? Width.Choice(old, branch) : branch;
            _prefix = _atom = Width.Zero;
        }
        internal Width Finish()
        {
            Width branch = Width.Add(_prefix, _atom);
            return _alternatives is { } old ? Width.Choice(old, branch) : branch;
        }
    }

    private void RejectLookbehindNode(string feature)
    {
        if (_inLookbehind) throw Unsupported("lookbehind " + feature);
    }

    private string Lookbehind(int start)
    {
        RejectLookbehindNode("nested assertion");
        char kind = _pattern[_position + 1];
        _position += 2;
        if (++_depth > 256) throw new PyRecursionError("re: supported group nesting limit exceeded (256)");
        var state = _lookbehind ??= new LookbehindState();
        state.InBody = true;
        state.LargeRepeat = false;
        string inner;
        try { inner = Sequence(true, start); }
        finally { state.InBody = false; _depth--; }
        state.Widths.Add((_groupWidth, state.LargeRepeat));
        // 字符集合原样保留双端码点边界；不得用译后的UTF16字符数猜测Python宽度。
        return "(?<" + kind + inner + ")";
    }

    private void RepeatWidth(WidthSequence sequence, string quantifier)
    {
        BigInteger min;
        BigInteger? max;
        if (quantifier == "*") { min = 0; max = null; }
        else if (quantifier == "+") { min = 1; max = null; }
        else if (quantifier == "?") { min = 0; max = 1; }
        else
        {
            string bounds = quantifier[1..^1];
            int comma = bounds.IndexOf(',');
            min = ReadRepeatBound(comma < 0 ? bounds : bounds[..comma]);
            max = comma < 0 ? min : comma == bounds.Length - 1 ? null : ReadRepeatBound(bounds[(comma + 1)..]);
        }
        sequence.Repeat(min, max);
    }

    // 非法区间是语法错误，必须在整份模式的token处检查（包括后顾外部），
    // 不能留给宿主构造，否则deferred后顾宽度错误会吞掉它。普通路径只比较Span。
    private void ValidateRepeatRange(string bounds, int position)
    {
        int comma = bounds.IndexOf(',');
        if (comma < 0 || comma == bounds.Length - 1) return;
        ReadOnlySpan<char> min = bounds.AsSpan(0, comma), max = bounds.AsSpan(comma + 1);
        while (min.Length > 0 && min[0] == '0') min = min[1..];
        while (max.Length > 0 && max[0] == '0') max = max[1..];
        if (min.Length > max.Length || min.Length == max.Length && min.SequenceCompareTo(max) > 0)
            throw ErrorAt("min repeat greater than max repeat", position);
    }

    private BigInteger ReadRepeatBound(string digits)
    {
        // 超大数先标记资源边界，仍消费完整语法并保留后续语法错误的优先级。
        // 不宣称CP的所有超大数OverflowError或错误顺序已兼容。
        BigInteger value = BigInteger.Zero;
        foreach (char digit in digits) value = value * 10 + digit - '0';
        if (value > int.MaxValue) _lookbehind!.LargeRepeat = true;
        return value;
    }

    private void ValidateLookbehinds()
    {
        if (_lookbehind is null) return;
        foreach (var assertion in _lookbehind.Widths)
        {
            Width width = assertion.Width;
            if (width.Max is null || width.Min != width.Max.Value)
                throw (PyRegexError)PyErrors.PrepareRaise(PyRegexError.FromArguments(
                    "look-behind requires fixed-width pattern", PyNone.Instance, PyNone.Instance));
            // 单次界限与累计界限都不交给宿主溢出；先做完整宽度代数，允许正文中的零乘归零。
            // CP对极大宽度的looks too much behind和超大计数异常仍是明确pending域。
            if (assertion.LargeRepeat || width.Min > int.MaxValue)
                throw Unsupported("lookbehind repeat size limit");
        }
    }
}
