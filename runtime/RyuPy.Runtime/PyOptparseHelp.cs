// 默认帮助布局独立冷路径；不修改解析器协议或全局换行引擎。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
namespace RyuPy.Runtime;

public sealed partial class PyOptparseParser
{
    private BigInteger _helpWidth;
    private int _maxHelpPosition;
    private bool _formattingHelp;
    private readonly Dictionary<PyOptparseOption, string> _optionStrings = new();

    private void InitializeDefaultHelp()
    {
        BigInteger columns;
        long depth = PyTraceback.CaptureDepth();
        try
        {
            object? value = PyOps.GetItem(PyModules.Import("os").GetAttr("environ")!, "COLUMNS");
            object number = PyOps.ToIntValue(value!);
            columns = number switch { long n => n, int n => n, BigInteger n => n,
                _ => throw PyOptparse.Unsupported("COLUMNS integer representation") };
        }
        catch (PyBaseException error) when (error is PyKeyError or PyValueError)
        {
            PyTraceback.Attach(error); PyTraceback.PopTo(depth + 1); columns = 80;
        }
        _helpWidth = columns - 2;
        _maxHelpPosition = (int)BigInteger.Min(24, BigInteger.Max(_helpWidth - 20, 4));
    }

    private static long WrapWidth(BigInteger width)
    {
        width = BigInteger.Max(width, 11);
        if (width > long.MaxValue) throw PyOptparse.Unsupported("help width resource range");
        return (long)width;
    }
    private static List<string> HelpLines(string text, long width) =>
        PyTextWrapEngine.Wrap(PyTextWrapEngine.Normalize(text, true, true, 8), width, "", "", true, true, true);
    private static string HelpParagraph(object? value, long width)
    {
        if (!PyOps.Truthy(value)) return "";
        return string.Join("\n", HelpLines(PyOptparse.Text(value), width)) + "\n";
    }
    private static string OptionStrings(PyOptparseOption option)
    {
        string? metavar = null;
        if (option.ValueType is not null)
        {
            object? explicitValue = option.Data["metavar"];
            if (PyOps.Truthy(explicitValue)) metavar = PyOptparse.Text(explicitValue);
            else
            {
                string dest = PyOptparse.Text(option.Dest);
                foreach (char ch in dest)
                    if (ch > 127) throw PyOptparse.Unsupported("implicit non-ASCII metavar uppercase");
                metavar = dest.ToUpperInvariant();
            }
        }
        var strings = new List<string>();
        foreach (string flag in option.Short) strings.Add(metavar is null ? flag : flag + " " + metavar);
        foreach (string flag in option.Long) strings.Add(metavar is null ? flag : flag + "=" + metavar);
        return string.Join(", ", strings);
    }
    private string FormatHelp()
    {
        if (_formattingHelp) throw PyOptparse.Unsupported("recursive format_help on the same parser");
        _formattingHelp = true;
        try { return FormatDefaultHelp(); }
        finally { _formattingHelp = false; }
    }
    private string FormatDefaultHelp()
    {
        var result = new StringBuilder();
        if (PyOps.Truthy(Data["usage"])) result.Append(GetUsage()).Append('\n');
        if (PyOps.Truthy(Data["description"]))
            result.Append(HelpParagraph(ExpandProg(Data["description"]), WrapWidth(_helpWidth))).Append('\n');

        // 先生成全部选项串；默认值str回调发生在后续逐项渲染时。
        var strings = _optionStrings;
        int maxLength = 0;
        for (int i = 0; i < Options.Items.Count; i++)
        {
            var option = Options.Items[i] as PyOptparseOption ?? throw PyOptparse.Unsupported("non-Option option_list mutation");
            string text = OptionStrings(option); strings[option] = text;
            maxLength = Math.Max(maxLength, checked((int)PyText.Length(text) + 2));
        }
        int position = Math.Min(maxLength + 2, _maxHelpPosition);
        long width = WrapWidth(_helpWidth - position);
        if (Options.Items.Count != 0)
        {
            result.Append("Options:\n");
            for (int i = 0; i < Options.Items.Count; i++)
            {
                var option = Options.Items[i] as PyOptparseOption ?? throw PyOptparse.Unsupported("non-Option option_list mutation");
                if (ReferenceEquals(option.Data["help"], PyOptparse.SuppressHelp)) continue;
                if (!strings.TryGetValue(option, out string? text))
                    throw PyOptparse.Own(PyKeyError.ForKey(option));
                int available = position - 4;
                bool separate = PyText.Length(text) > available;
                result.Append("  ").Append(text);
                if (separate) result.Append('\n');
                else result.Append(' ', available - checked((int)PyText.Length(text)) + 2);
                if (PyOps.Truthy(option.Data["help"]))
                {
                    Defaults.TryGet(option.Dest, out object? value);
                    if (PyOptparse.None(value) || ReferenceEquals(value, PyOptparse.NoDefault)) value = "none";
                    // replace接收者先读取，再执行str(default)，不能按是否含占位符省略回调。
                    string help = PyOptparse.Text(option.Data["help"]);
                    string expanded = help.Replace("%default", PyRepr.Str(value), StringComparison.Ordinal);
                    var lines = HelpLines(expanded, width);
                    if (lines.Count == 0) throw PyOptparse.Own(new PyIndexError("list index out of range"));
                    if (separate) result.Append(' ', position);
                    result.Append(lines[0]).Append('\n');
                    for (int n = 1; n < lines.Count; n++) result.Append(' ', position).Append(lines[n]).Append('\n');
                }
                else if (!separate) result.Append('\n');
            }
        }
        if (PyOps.Truthy(Data["epilog"])) result.Append('\n').Append(HelpParagraph(Data["epilog"], WrapWidth(_helpWidth)));
        return result.ToString();
    }
    private object? PrintHelp(object? file)
    {
        object? target = PyOptparse.None(file) ? PySystem.Stdout : file;
        // CP先取write，再生成帮助；用户默认值回调不能替换本次已捕获的方法。
        object? write = PyObject.GetAttr(target!, "write");
        string text = FormatHelp();
        PyOps.CallValue(write, [text], [], []);
        return null;
    }
}
