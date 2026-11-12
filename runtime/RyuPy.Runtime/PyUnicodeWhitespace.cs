// Python Unicode空白单一判定；与.NET差异为ASCII信息分隔符U+001C..001F。
namespace RyuPy.Runtime;

internal static class PyUnicodeWhitespace
{
    internal static bool Is(char value) => char.IsWhiteSpace(value) || value is >= '\u001c' and <= '\u001f';
    internal static bool Invoke(string text, object?[] args, string[] names, string method = "isspace")
    {
        if (names.Length != 0) throw (PyTypeError)PyErrors.PrepareRaise(new PyTypeError($"str.{method}() takes no keyword arguments"));
        if (args.Length != 0) throw (PyTypeError)PyErrors.PrepareRaise(new PyTypeError($"str.{method}() takes no arguments ({args.Length} given)"));
        return method == "isalnum" ? PyTextAlnum.All(text) : All(text);
    }
    internal static bool All(string text)
    {
        if (text.Length == 0) return false;
        foreach (char value in text) if (!Is(value)) return false;
        return true;
    }
}
