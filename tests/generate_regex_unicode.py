"""固定 CPython 3.12.13 / Unicode 15.0.0 的 re 分类与大小写等价数据。

只生成事实性字符表；运行时不调用 Python。_sre / re._casefix 属 CPython，
来源 https://github.com/python/cpython/tree/v3.12.13/Lib/re ，许可证 PSF-2.0。
复现：使用 tests/pinned-python.txt 对应解释器执行此脚本；输出为小于600行的C#表。
"""
import _sre
import re
import re._casefix
import sys
import unicodedata
from pathlib import Path

assert sys.version_info[:3] == (3, 12, 13)
assert unicodedata.unidata_version == "15.0.0"


def ranges(pattern):
    test = re.compile(pattern).fullmatch
    result = []
    start = None
    for codepoint in range(0x110000):
        if test(chr(codepoint)):
            if start is None:
                start = codepoint
        elif start is not None:
            result.extend((start, codepoint - 1))
            start = None
    if start is not None:
        result.extend((start, 0x10FFFF))
    return result


def declaration(name, values):
    lines = [f"    internal static readonly int[] {name} = ["]
    for offset in range(0, len(values), 20):
        lines.append("        " + ",".join(hex(value) for value in values[offset:offset+20]) + ",")
    return "\n".join(lines + ["    ];"])


pairs = []
for codepoint in range(0x110000):
    lower = _sre.unicode_tolower(codepoint)
    if lower != codepoint:
        pairs.extend((codepoint, lower))
for codepoint, others in sorted(re._casefix._EXTRA_CASES.items()):
    for other in others:
        pairs.extend((codepoint, other))
header = """// 由 tests/generate_regex_unicode.py 生成，不手工修改。
// CPython 3.12.13 / Unicode 15.0.0；CPython来源及PSF-2.0许可见生成脚本。
namespace RyuPy.Runtime;

/// <summary>Python正则字符分类和simple-case等价边；运行时与宿主Unicode版本解耦。</summary>
internal static class PyRegexUnicodeData
{
"""
output = header + "\n".join(declaration(name, values) for name, values in [
    ("Word", ranges(r"\w")), ("Digit", ranges(r"\d")),
    ("Space", ranges(r"\s")), ("CasePairs", pairs),
]) + "\n}\n"
assert len(output.splitlines()) <= 600
Path("runtime/RyuPy.Runtime/PyRegexUnicodeData.cs").write_text(output)
