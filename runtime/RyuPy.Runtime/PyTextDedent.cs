// dedent的owner局部状态与ASCII/LF线性扫描；只优化两条真实固定Pattern，最终替换仍调用当前re.sub。
using System;
using System.Text;

namespace RyuPy.Runtime;

internal sealed class PyTextDedent
{
    private readonly PyModule owner;
    private readonly PyModule initialRe;
    private readonly PyRegexPattern? whitespace;
    private readonly PyRegexPattern? leading;

    private PyTextDedent(PyModule owner, PyModule re, PyTextDedentCompile witness)
    {
        this.owner = owner; initialRe = re;
        if (!witness.Supports(re)) return;
        // 这是新API的两次规范编译，不冒充CP TextWrapper在内的五次初始化回调。
        whitespace = witness.Compile("^[ \t]+$");
        leading = witness.Compile("(^[ \t]*)(?:[^ \t\n])");
    }

    internal static bool Owns(string name) =>
        name is "dedent" or "re" or "_whitespace_only_re" or "_leading_whitespace_re";

    internal static void Initialize(PyTextDedentCompile witness)
    {
        if (!PyModules.TryCached("textwrap", out PyModule? owner) || owner is null)
            throw new InvalidOperationException("textwrap initializer requires its cached owner");
        // 真实依赖导入先完成；原异常原样传播，不发布半套新字段或回填逃逸旧owner。
        PyModule re = PyModules.Import("re");
        var state = new PyTextDedent(owner, re, witness);
        PyFunction dedent = PyRegex.Function("dedent", ["text"], [null], 1, args => state.Invoke(args[0]));
        owner.DynamicAttributes["re"] = re;
        owner.DynamicAttributes["dedent"] = dedent;
        if (state.whitespace is not null && state.leading is not null)
        {
            owner.DynamicAttributes["_whitespace_only_re"] = state.whitespace;
            owner.DynamicAttributes["_leading_whitespace_re"] = state.leading;
        }
    }

    private object? Invoke(object? subject)
    {
        if (whitespace is null || leading is null)
            throw Unsupported("initial re compilation state");
        RequirePattern("_whitespace_only_re", whitespace);
        if (!PyStringLayout.TryGet(subject, out string text))
        {
            string message = subject is PyBytes or PyByteArray or PyMemoryView
                ? "cannot use a string pattern on a bytes-like object"
                : $"expected string or bytes-like object, got '{PyRepr.TypeName(subject)}'";
            throw (PyTypeError)PyErrors.PrepareRaise(new PyTypeError(message));
        }
        string normalized = Normalize(text);
        // 必须等第一阶段成功才消费第二个私有对象，类型错误不能被提前NI覆盖。
        RequirePattern("_leading_whitespace_re", leading);
        string margin = Margin(normalized);
        if (margin.Length == 0) return normalized;
        if (!ReferenceEquals(owner.GetAttr("re"), initialRe)) throw Unsupported("re binding replacement");
        // 恰三个位置实参、无keyword，不预删margin；任意返回对象/用户异常保原身份。
        return PyOps.CallValue(initialRe.GetAttr("sub"), ["(?m)^" + margin, "", normalized], [], []);
    }

    private void RequirePattern(string name, PyRegexPattern expected)
    {
        if (!ReferenceEquals(owner.GetAttr(name), expected)) throw Unsupported(name + " replacement");
    }

    private static PyNotImplementedError Unsupported(string detail) =>
        (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError("textwrap.dedent: " + detail + " is not supported"));

    private static bool Indent(char value) => value is ' ' or '\t';

    // 只识别LF分行和整行ASCII space/tab；首次删字才分配，无变化返回raw base string原对象。
    private static string Normalize(string text)
    {
        StringBuilder? result = null;
        int copied = 0, start = 0;
        while (start < text.Length)
        {
            int end = text.IndexOf('\n', start);
            if (end < 0) end = text.Length;
            int position = start;
            while (position < end && Indent(text[position])) position++;
            if (position == end && end > start)
            {
                result ??= new StringBuilder(text.Length);
                result.Append(text, copied, start - copied);
                copied = end;
            }
            start = end == text.Length ? end : end + 1;
        }
        return result is null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }

    // 只保首个非空行的区间；其它行逐字符缩短前缀，不复制每行/剩余全文，总工作量O(N)。
    private static string Margin(string text)
    {
        int first = -1, length = 0, start = 0;
        while (start < text.Length)
        {
            int end = text.IndexOf('\n', start);
            if (end < 0) end = text.Length;
            if (end > start)
            {
                int stop = start;
                while (stop < end && Indent(text[stop])) stop++;
                if (first < 0) { first = start; length = stop - start; }
                else
                {
                    length = Math.Min(length, stop - start);
                    int common = 0;
                    while (common < length && text[first + common] == text[start + common]) common++;
                    length = common;
                }
                if (length == 0) return "";
            }
            start = end == text.Length ? end : end + 1;
        }
        return first < 0 ? "" : text.Substring(first, length);
    }
}
