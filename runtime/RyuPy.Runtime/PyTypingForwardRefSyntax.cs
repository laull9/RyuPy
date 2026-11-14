// ForwardRef的有限纯语法识别器：只接受已闭合的ASCII类型引用，不解析名字或执行表达式。
// 使用显式括号栈，无CLR递归、AST、正则回溯或永久字符串intern；超域明确NI。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyTypingForwardRefSyntax
{
    // 对齐固定CP3.12 tokenizer的MAXLEVEL；超过此域不冒精确SyntaxError的优先级/消息。
    private const int MaximumBrackets = 200;
    private enum Kind : byte { Root, Subscript, List }
    private enum Token : byte { End, Name, None, Text, Ellipsis, Dot, Open, Close, Comma, Union, Unsupported }
    private struct Frame(Kind kind)
    {
        internal Kind Context = kind;
        internal bool NeedOperand = true;
        internal bool MayClose = kind == Kind.List;
        internal bool MayPostfix;
    }

    private static readonly string[] Keywords =
    [
        "False", "True", "and", "as", "assert", "async", "await", "break", "class", "continue",
        "def", "del", "elif", "else", "except", "finally", "for", "from", "global", "if", "import",
        "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try", "while", "with", "yield",
    ];

    internal static void Validate(string text)
    {
        // 开头缩进与换行尚未校准；包括空串在内的未识别输入也不伪造SyntaxError家族。
        if (text.Length == 0 || text[0] is ' ' or '\t') throw Outside();
        int position = 0;
        var stack = new List<Frame> { new(Kind.Root) };
        Token token = Read(text, ref position);
        while (true)
        {
            int last = stack.Count - 1;
            Frame frame = stack[last];
            if (frame.NeedOperand)
            {
                if (token == Token.Open && frame.Context != Kind.Root)
                {
                    Push(stack, Kind.List);
                    token = Read(text, ref position);
                    continue;
                }
                if (token is Token.Name or Token.None or Token.Text or Token.Ellipsis)
                {
                    frame.NeedOperand = false;
                    frame.MayClose = true;
                    frame.MayPostfix = token == Token.Name;
                    stack[last] = frame;
                    token = Read(text, ref position);
                    continue;
                }
                // list允许空；subscript只允许已有元素后的尾逗号，不允许空下标或union缺右项。
                if (token != Token.Close || !frame.MayClose || frame.Context == Kind.Root) throw Outside();
            }
            else
            {
                if (token == Token.Dot && frame.MayPostfix)
                {
                    if (Read(text, ref position) != Token.Name) throw Outside();
                    token = Read(text, ref position);
                    continue;
                }
                if (token == Token.Open && frame.MayPostfix)
                {
                    Push(stack, Kind.Subscript);
                    token = Read(text, ref position);
                    continue;
                }
                if (token == Token.Union)
                {
                    frame.NeedOperand = true;
                    frame.MayClose = false;
                    frame.MayPostfix = false;
                    stack[last] = frame;
                    token = Read(text, ref position);
                    continue;
                }
                if (token == Token.Comma && frame.Context != Kind.Root)
                {
                    frame.NeedOperand = true;
                    frame.MayClose = true;
                    frame.MayPostfix = false;
                    stack[last] = frame;
                    token = Read(text, ref position);
                    continue;
                }
                if (token == Token.End && frame.Context == Kind.Root) return;
                if (token != Token.Close || frame.Context == Kind.Root) throw Outside();
            }
            // 关闭一层后，仅将完成状态写回父层；没有递归返回或AST节点。
            Kind completed = frame.Context;
            stack.RemoveAt(last);
            Frame parent = stack[last - 1];
            parent.NeedOperand = false;
            parent.MayClose = true;
            parent.MayPostfix = completed == Kind.Subscript;
            stack[last - 1] = parent;
            token = Read(text, ref position);
        }
    }

    private static void Push(List<Frame> stack, Kind kind)
    {
        // 根状态不算括号；在第201个括号入栈前拒绝，长bare name不受此界限影响。
        if (stack.Count - 1 >= MaximumBrackets)
            throw PyTypingForwardRef.Unsupported("ForwardRef bracket nesting beyond 200 is not supported");
        stack.Add(new Frame(kind));
    }

    private static Token Read(string text, ref int position)
    {
        while (position < text.Length && text[position] is ' ' or '\t') position++;
        if (position == text.Length) return Token.End;
        char current = text[position++];
        if (char.IsAsciiLetter(current) || current == '_')
        {
            int start = position - 1;
            while (position < text.Length && (char.IsAsciiLetterOrDigit(text[position]) || text[position] == '_')) position++;
            ReadOnlySpan<char> name = text.AsSpan(start, position - start);
            if (name.SequenceEqual("None")) return Token.None;
            foreach (string keyword in Keywords) if (name.SequenceEqual(keyword)) return Token.Unsupported;
            return Token.Name;
        }
        if (current is '\'' or '"')
        {
            while (position < text.Length)
            {
                char value = text[position++];
                if (value == current) return Token.Text;
                if (value < ' ' || value > '~' || value == '\\') return Token.Unsupported;
            }
            return Token.Unsupported;
        }
        if (current == '.' && text.Length - position >= 2 && text[position] == '.' && text[position + 1] == '.')
        {
            position += 2;
            return Token.Ellipsis;
        }
        return current switch
        {
            '.' => Token.Dot, '[' => Token.Open, ']' => Token.Close, ',' => Token.Comma, '|' => Token.Union,
            _ => Token.Unsupported,
        };
    }

    private static PyNotImplementedError Outside() =>
        PyTypingForwardRef.Unsupported("ForwardRef expression is outside the supported ASCII type-reference syntax");
}
