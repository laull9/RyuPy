// TextWrapper 可观察贪心算法：原地修改输入列表，动态调用长词 hook，不跨回调缓存配置。
using System;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

internal static partial class PyTextWrapper
{
    internal static PyList WrapChunks(object? self, object? chunks)
    {
        object? originalWidth = PyObject.GetAttr(self, "width");
        if (AlgorithmInteger(originalWidth, "width") <= 0)
            // 错误格式化在 CP 再次读取 width；property 可以在两次读取间改变值或抛错。
            throw new PyValueError($"invalid width {PyRepr.Repr(PyObject.GetAttr(self, "width"))} (must be > 0)");
        RequireUnlimitedLines(self);
        PyList remaining = AlgorithmList(chunks);
        remaining.Items.Reverse();
        var lines = new PyList();
        while (remaining.Count != 0)
        {
            var current = new PyList();
            long used = 0;
            string indent = AlgorithmText(PyObject.GetAttr(self,
                lines.Count == 0 ? "initial_indent" : "subsequent_indent"));
            long width = AlgorithmInteger(PyObject.GetAttr(self, "width"), "width");
            long indentLength = PyText.Length(indent);
            if (width < long.MinValue + indentLength)
                throw Unsupported("textwrap.TextWrapper width arithmetic exceeds the supported resource range");
            long available = width - indentLength;
            // strip 在 lines 真值之前求值，保持 getter/错误与 Python 相同的先后。
            if (Flag(self, "drop_whitespace") && Space(remaining.GetItem(-1)) && lines.Count != 0)
                remaining.DelItem(-1);
            while (remaining.Count != 0)
            {
                object? chunk = remaining.GetItem(-1);
                long length = PyText.Length(AlgorithmText(chunk));
                if (used + length > available) break;
                current.Items.Add(chunk); remaining.DelItem(-1); used += length;
            }
            if (remaining.Count != 0 && PyText.Length(AlgorithmText(remaining.GetItem(-1))) > available)
            {
                Call(self, "_handle_long_word", remaining, current, used, available);
                // 用户 hook 可改变内容和数量；必须重新从同一 current 列表读取。
                used = 0;
                foreach (object? chunk in current.Items) used += PyText.Length(AlgorithmText(chunk));
            }
            if (Flag(self, "drop_whitespace") && current.Count != 0 && Space(current.GetItem(-1)))
                current.DelItem(-1);
            if (current.Count == 0) continue;
            RequireUnlimitedLines(self);
            var line = new StringBuilder(indent);
            foreach (object? chunk in current.Items) line.Append(AlgorithmText(chunk));
            lines.Items.Add(line.ToString());
        }
        return lines;
    }

    internal static object? HandleLongWord(object? self, object? reversedChunks,
        object? currentLine, object? currentLength, object? width)
    {
        long available = AlgorithmInteger(width, "long-word width");
        // 用 BigInteger 仅保存一次减法，防显式 helper 实参溢出宿主 long；实际切片会钳到字符串边界。
        BigInteger room = available < 1 ? BigInteger.One :
            (BigInteger)available - AlgorithmInteger(currentLength, "current length");
        if (Flag(self, "break_long_words"))
        {
            PyList remaining = AlgorithmList(reversedChunks);
            string chunk = AlgorithmText(remaining.GetItem(-1));
            long length = PyText.Length(chunk);
            long cut = room < 0 ? (long)BigInteger.Max(BigInteger.Zero, length + room) :
                (long)BigInteger.Min(length, room);
            int offset = PyText.OffsetOf(chunk, cut);
            if (Flag(self, "break_on_hyphens") && length > room && offset > 0)
            {
                int hyphen = chunk.LastIndexOf('-', offset - 1, offset);
                if (hyphen > 0)
                {
                    bool hasOther = false;
                    for (int index = 0; index < hyphen; index++)
                        if (chunk[index] != '-') { hasOther = true; break; }
                    if (hasOther) offset = hyphen + 1;
                }
            }
            AlgorithmList(currentLine).Items.Add(chunk[..offset]);
            // currentLine 与 reversedChunks 可以是同一list；必须在append后重新写最后一项。
            remaining.SetItem(-1, chunk[offset..]);
        }
        else
        {
            PyList current = AlgorithmList(currentLine);
            if (current.Count == 0)
            {
                PyList remaining = AlgorithmList(reversedChunks);
                if (remaining.Count == 0) throw new PyIndexError("pop from empty list");
                object? chunk = remaining.GetItem(-1);
                remaining.DelItem(-1); current.Items.Add(chunk);
            }
        }
        return PyNone.Instance;
    }

    private static bool Flag(object? self, string name) => PyOps.Truthy(PyObject.GetAttr(self, name));
    private static bool Space(object? value) => PyTextWrapEngine.WhitespaceOnly(AlgorithmText(value));

    private static void RequireUnlimitedLines(object? self)
    {
        if (PyObject.GetAttr(self, "max_lines") is not (null or PyNone))
            throw Unsupported("textwrap.TextWrapper max_lines is not supported by the default chunk wrapper");
    }
}
