// Scanner专用sealed载体及每次scan独占的具体cursor。复用真实Pattern/Match身份，
// 单个\G复合Regex保词条左优先与完整输入上下文；不增加普通Pattern/Match实例字段。
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

internal sealed class PyRegexScannerPattern : PyRegexPattern
{
    private const string Boundary = "(?!(?<=[\\uD800-\\uDBFF])[\\uDC00-\\uDFFF])";
    // \G(?: 与最终 ) 加双边界；非空wrapper本体、编号和分隔符按真实字符数累加。
    internal static int InitialSize => 6 + 2 * Boundary.Length;
    internal static int AddSize(int size, string fragment, int number)
    {
        int wrapper = 8 + number.ToString(System.Globalization.CultureInfo.InvariantCulture).Length + (number == 1 ? 0 : 1);
        if (wrapper > 200000 - size || fragment.Length > 200000 - size - wrapper)
            throw PyRegexScanner.Unsupported("translated pattern size resource limit (200000)");
        return size + wrapper + fragment.Length;
    }

    private readonly Regex _executor;
    private PyRegexScannerPattern(Regex executor, int flags, int groups) : base(executor, flags, groups)
    { _executor = executor; }

    internal static PyRegexScannerPattern Create(List<string> fragments, int flags)
    {
        // 公共构造已在最终消费点处理空lexicon；不允许内部误用产生可匹配空串的plan。
        if (fragments.Count == 0) throw PyRegexScanner.Unsupported("empty compiled plan");
        var source = new StringBuilder("\\G" + Boundary + "(?:");
        for (int index = 0; index < fragments.Count; index++)
        {
            if (index != 0) Append("|");
            Append("(?<ryu" + (index + 1) + ">"); Append(fragments[index]); Append(")");
        }
        Append(")"); Append(Boundary);
        RegexOptions options = RegexOptions.CultureInvariant
            | ((flags & 8) != 0 ? RegexOptions.Multiline : RegexOptions.None);
        try { return new(new Regex(source.ToString(), options, TimeSpan.FromSeconds(2)), flags, fragments.Count); }
        catch (ArgumentException)
        {
            // 受支持语法已由同一Python parser验证；宿主无法承载时明确能力拒绝，
            // 不把拼接译文冒充Python原模式，不吞任何用户异常。
            throw PyRegexScanner.Unsupported("compound backend expression");
        }

        void Append(string text)
        {
            if (text.Length > 200000 - source.Length)
                throw PyRegexScanner.Unsupported("translated pattern size resource limit (200000)");
            source.Append(text);
        }
    }

    // 白名单闭合：未来普通Pattern新增方法不会自动逃逸到这个复合载体。
    public override object? GetAttr(string name) => name switch
    {
        "pattern" => PyNone.Instance, "flags" => (long)Flags, "groups" => (long)Groups,
        "groupindex" => throw PyRegexScanner.Unsupported("Pattern.groupindex"),
        "split" => PyRegexSplit.Pattern(this),
        "match" or "search" or "fullmatch" or "findall" or "finditer" =>
            PyRegex.Function(name, ["string", "pos", "endpos"], [null, 0L, long.MaxValue], 1,
                _ => throw PyRegexScanner.Unsupported("Pattern." + name)),
        "sub" or "subn" => PyRegex.Function(name, ["repl", "string", "count"], [null, null, 0L], 2,
            _ => throw PyRegexScanner.Unsupported("Pattern." + name)),
        "scanner" => PyRegex.Function(name, ["string", "pos", "endpos"], [null, 0L, long.MaxValue], 1,
            _ => throw PyRegexScanner.Unsupported("Pattern.scanner private API")),
        "__copy__" or "__deepcopy__" or "__reduce__" or "__reduce_ex__" =>
            throw PyRegexScanner.Unsupported("Pattern copy or pickle"),
        _ => throw Missing(name),
    };

    public override string PythonRepr() => throw PyRegexScanner.Unsupported("Pattern repr");

    internal static void RejectConsumer(PyRegexPattern pattern)
    {
        if (pattern is PyRegexScannerPattern) throw PyRegexScanner.Unsupported("compound Pattern consumer");
    }

    // 无内部捕获时恰一个wrapper参与；公开lastindex允许线性读取，不给普通Match加槽。
    internal int Select(Match raw)
    {
        for (int number = 1; number <= Groups; number++)
            if (raw.Groups["ryu" + number].Success) return number;
        throw PyRegexScanner.Unsupported("compound match without wrapper");
    }

    internal ScannerCursor Cursor(object? value) => new(this, value);

    internal sealed class ScannerCursor
    {
        private readonly PyRegexScannerPattern _plan;
        private readonly PyRegexText? _text;
        private readonly PyBytes? _bytes;
        private readonly string _subject;
        private int _position;

        internal ScannerCursor(PyRegexScannerPattern plan, object? value)
        {
            _plan = plan;
            if (value is string text) { _text = new PyRegexText(text); _subject = text; }
            else if (value is PyBytes bytes) { _bytes = bytes; _subject = Encoding.Latin1.GetString(bytes.Bytes); }
            else if (PyRegexScanner.DefinitelyNotBuffer(value))
                throw PyRegexScanner.Own(new PyTypeError($"expected string or bytes-like object, got '{PyRepr.TypeName(value)}'"));
            else throw PyRegexScanner.Unsupported("subject buffer or subclass");
        }

        // 只调用一次锚定执行器；失败与零长立即停止，不做finditer的同位非空重试。
        internal bool Next(out PyRegexMatch? match, out int wrapper, out int end)
        {
            Match raw;
            try { raw = _plan._executor.Match(_subject, _position); }
            catch (RegexMatchTimeoutException)
            { throw PyRegexScanner.Own(new PyRuntimeError("re: supported matching time budget exceeded (2 seconds)")); }
            match = null; wrapper = 0; end = _position;
            if (!raw.Success || raw.Length == 0) return false;
            wrapper = _plan.Select(raw);
            end = raw.Index + raw.Length;
            match = _bytes is null ? new PyRegexMatch(_plan, _text!, raw, 0, _text!.Length)
                : new PyRegexMatch(_plan, _bytes, raw, 0, _bytes.Count);
            _position = end;
            return true;
        }

        internal object Remainder(int position) => _bytes is null ? _subject[position..]
            : PyRegexBytes.Slice(_bytes, position, _bytes.Count - position);
    }
}
