# Unicode15.0.0数据来源

EastAsianWidth.txt、UnicodeData.txt来自Unicode官方固定版本目录：
https://www.unicode.org/Public/15.0.0/ucd/ 。LICENSE.txt保存下载时官方
https://www.unicode.org/license.txt 的完整Unicode License V3，保留版权许可通知。
源数据不适用项目代码600行上限；独立生成器/运行时文件均遵守上限。

manifest.json保存源URL/SHA256与固定CPython3.12.13全码点校验结果。
生成：固定解释器执行 `tests/generate_unicodedata.py --ucd-dir third_party/unicode/15.0.0
--output runtime/RyuPy.Runtime/PyUnicodeDataTables.cs`。
表来自UCD属性数据，不复制CPython实现；CPython仅作为独立行为对照。
