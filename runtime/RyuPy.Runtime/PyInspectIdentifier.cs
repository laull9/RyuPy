// Parameter标识符按Unicode类别及NFKC闭包验证，保留原始名字，不把显示名字做归一化替换。
using System;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyInspectIdentifier
{
    internal static bool Valid(string name)
    {
        if (!IdSequence(name)) return false;
        try { return IdSequence(name.Normalize(NormalizationForm.FormKC)); }
        catch (ArgumentException) { return false; }
    }
    private static bool IdSequence(string name)
    {
        bool first = true;
        foreach (Rune rune in name.EnumerateRunes())
        {
            if (first ? !Start(rune) : !Continue(rune)) return false;
            first = false;
        }
        return !first;
    }
    private static bool Start(Rune rune) => rune.Value is '_' or 0x1885 or 0x1886 or 0x2118 or 0x212e or 0x309b or 0x309c
        || Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber;
    private static bool Continue(Rune rune) => Start(rune)
        || rune.Value is 0xb7 or 0x387 or >= 0x1369 and <= 0x1371 or 0x19da
        || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation;
}
