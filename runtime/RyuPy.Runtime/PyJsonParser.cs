using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>JSON递归下降解析：保留大整数、对象顺序和Python码点错误位置，不借.NET异常充当JSON诊断。</summary>
internal sealed class PyJsonParser
{
    private static readonly (string Word, object? Value)[] Literals =
    [ ("null", PyNone.Instance), ("true", true), ("false", false), ("NaN", double.NaN),
      ("Infinity", double.PositiveInfinity), ("-Infinity", double.NegativeInfinity) ];
    private readonly string _text;
    private readonly object? _objectHook, _pairsHook, _parseInt, _parseFloat, _parseConstant;
    private readonly bool _strict;
    private int _offset;

    internal PyJsonParser(string text, object? objectHook, object? pairsHook, object? parseInt,
        object? parseFloat, object? parseConstant, bool strict)
    {
        _text = text; _objectHook = objectHook; _pairsHook = pairsHook;
        _parseInt = parseInt; _parseFloat = parseFloat; _parseConstant = parseConstant; _strict = strict;
    }

    internal object? Parse()
    {
        if (_text.StartsWith('\ufeff')) Fail("Unexpected UTF-8 BOM (decode using utf-8-sig)", 0);
        Space(); object? result = Value(0); Space();
        if (_offset != _text.Length) Fail("Extra data", _offset);
        return result;
    }

    private object? Value(int depth)
    {
        if (depth > 256) throw new PyRecursionError("maximum recursion depth exceeded while decoding a JSON document");
        if (_offset == _text.Length) return Fail("Expecting value", _offset);
        char c = _text[_offset];
        if (c == '"') return String();
        if (c == '[') return Array(depth + 1);
        if (c == '{') return Object(depth + 1);
        foreach (var (word, result) in Literals)
        {
            if (!_text.AsSpan(_offset).StartsWith(word, StringComparison.Ordinal)) continue;
            _offset += word.Length;
            return word is "NaN" or "Infinity" or "-Infinity" && Has(_parseConstant)
                ? Call(_parseConstant, word) : result;
        }
        if (c == '-' || c is >= '0' and <= '9') return Number();
        return Fail("Expecting value", _offset);
    }

    private object Number()
    {
        int start = _offset;
        if (_text[_offset] == '-') _offset++;
        if (_offset == _text.Length || !Digit(_text[_offset])) return Fail("Expecting value", start);
        if (_text[_offset] == '0') _offset++;
        else while (_offset < _text.Length && Digit(_text[_offset])) _offset++;
        bool real = false;
        if (_offset + 1 < _text.Length && _text[_offset] == '.' && Digit(_text[_offset + 1]))
        {
            real = true; _offset += 2;
            while (_offset < _text.Length && Digit(_text[_offset])) _offset++;
        }
        if (_offset < _text.Length && _text[_offset] is 'e' or 'E')
        {
            int end = _offset + 1;
            if (end < _text.Length && _text[end] is '+' or '-') end++;
            if (end < _text.Length && Digit(_text[end]))
            {
                real = true; _offset = end + 1;
                while (_offset < _text.Length && Digit(_text[_offset])) _offset++;
            }
        }
        string token = _text[start.._offset];
        object? hook = real ? _parseFloat : _parseInt;
        if (Has(hook)) return Call(hook, token)!;
        if (real) return double.Parse(token, CultureInfo.InvariantCulture);
        if (token.Length - (token[0] == '-' ? 1 : 0) > 4300)
            throw new PyValueError("Exceeds the limit (4300 digits) for integer string conversion");
        return PyInt.Normalize(BigInteger.Parse(token, CultureInfo.InvariantCulture));
    }

    private PyList Array(int depth)
    {
        _offset++; Space(); var values = new List<object?>();
        if (Consume(']')) return new PyList(values.ToArray());
        while (true)
        {
            values.Add(Value(depth)); Space();
            if (Consume(']')) return new PyList(values.ToArray());
            if (!Consume(',')) return (PyList)Fail("Expecting ',' delimiter", _offset);
            Space();
        }
    }

    private object? Object(int depth)
    {
        _offset++; Space(); var result = new PyDict(); var pairs = new List<object?>();
        if (!Consume('}'))
        {
            while (true)
            {
                if (_offset == _text.Length || _text[_offset] != '"')
                    return Fail("Expecting property name enclosed in double quotes", _offset);
                string key = String(); Space();
                if (!Consume(':')) return Fail("Expecting ':' delimiter", _offset);
                Space(); object? value = Value(depth); result.Set(key, value);
                if (Has(_pairsHook)) pairs.Add(new PyTuple([key, value]));
                Space(); if (Consume('}')) break;
                if (!Consume(',')) return Fail("Expecting ',' delimiter", _offset);
                Space();
            }
        }
        if (Has(_pairsHook)) return Call(_pairsHook, new PyList(pairs.ToArray()));
        return Has(_objectHook) ? Call(_objectHook, result) : result;
    }

    private string String()
    {
        int start = _offset++; var output = new StringBuilder();
        while (_offset < _text.Length)
        {
            char c = _text[_offset++];
            if (c == '"') return output.ToString();
            if (c < 32 && _strict) return (string)Fail("Invalid control character at", _offset - 1);
            if (c != '\\') { output.Append(c); continue; }
            if (_offset == _text.Length) return (string)Fail("Unterminated string starting at", start);
            char escaped = _text[_offset++];
            switch (escaped)
            {
                case '"': case '\\': case '/': output.Append(escaped); break;
                case 'b': output.Append('\b'); break;
                case 'f': output.Append('\f'); break;
                case 'n': output.Append('\n'); break;
                case 'r': output.Append('\r'); break;
                case 't': output.Append('\t'); break;
                case 'u':
                    if (_offset + 4 > _text.Length || !ushort.TryParse(_text.AsSpan(_offset, 4),
                        NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code))
                        return (string)Fail("Invalid \\uXXXX escape", _offset - 1);
                    output.Append((char)code); _offset += 4; break;
                default: return (string)Fail("Invalid \\escape", _offset - 2);
            }
        }
        return (string)Fail("Unterminated string starting at", start);
    }

    private bool Consume(char c)
    {
        if (_offset >= _text.Length || _text[_offset] != c) return false;
        _offset++; return true;
    }
    private void Space() { while (_offset < _text.Length && _text[_offset] is ' ' or '\t' or '\n' or '\r') _offset++; }
    private static bool Digit(char c) => c is >= '0' and <= '9';
    private object Fail(string message, int offset) => throw new PyJSONDecodeError(message, _text, offset);
    private static bool Has(object? value) => value is not null and not PyNone;
    private static object? Call(object? callable, object? arg) => PyOps.CallValue(callable, [arg], [], []);
}

/// <summary>JSON错误保留原文、消息与码点位置；CLR继承ValueError使普通except也能捕获。</summary>
public sealed class PyJSONDecodeError : PyValueError
{
    /// <summary>未经位置修饰的错误消息。</summary>
    public string Msg { get; }
    /// <summary>原始JSON文档。</summary>
    public string Doc { get; }
    /// <summary>Python码点偏移与一基行列。</summary>
    public long Pos { get; }
    /// <summary>一基行号。</summary>
    public long LineNo { get; }
    /// <summary>一基码点列号。</summary>
    public long ColNo { get; }
    /// <summary>内部UTF16偏移仅用于定位，公开属性统一为码点计数。</summary>
    public PyJSONDecodeError(string message, string doc, int offset)
        : base("JSONDecodeError", Describe(message, doc, offset))
    {
        Msg = message; Doc = doc; (Pos, LineNo, ColNo) = Position(doc, offset);
    }
    private static string Describe(string message, string doc, int offset)
    {
        var (pos, line, column) = Position(doc, offset);
        return $"{message}: line {line} column {column} (char {pos})";
    }
    private static (long, long, long) Position(string doc, int offset)
    {
        long pos = 0, line = 1, column = 1;
        for (int i = 0; i < Math.Min(offset, doc.Length); i++)
        {
            if (doc[i] == '\n') { line++; column = 1; } else column++;
            if (char.IsHighSurrogate(doc[i]) && i + 1 < offset && char.IsLowSurrogate(doc[i + 1])) i++;
            pos++;
        }
        return (pos, line, column);
    }
}
