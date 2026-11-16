// CPython3.12/Unicode15固定词字符表去下划线即isalnum；不依赖宿主Unicode分类版本。
namespace RyuPy.Runtime;

internal static class PyTextAlnum
{
    internal static bool All(string text)
    {
        if (text.Length == 0) return false;
        for (int i = 0; i < text.Length; i++)
        {
            int cp = text[i];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                cp = char.ConvertToUtf32(text[i], text[++i]);
            if (!Contains(cp)) return false;
        }
        return true;
    }
    internal static bool Contains(int cp)
    {
        if (cp == '_') return false;
        int[] ranges = PyRegexUnicodeData.Word;
        int low = 0, high = ranges.Length / 2 - 1;
        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (cp < ranges[mid * 2]) high = mid - 1;
            else if (cp > ranges[mid * 2 + 1]) low = mid + 1;
            else return true;
        }
        return false;
    }
}
