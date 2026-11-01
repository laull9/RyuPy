"""从官方UCD15.0.0数据独立生成有限属性表，并对固定CPython全部码点交叉验收。"""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import unicodedata

SOURCES = {
    'LICENSE.txt': 'e7a93b009565cfce55919a381437ac4db883e9da2126fa28b91d12732bc53d96',
    'EastAsianWidth.txt': '743e7bc435c04ab1a8459710b1c3cad56eedced5b806b4659b6e69b85d0adf2a',
    'UnicodeData.txt': '806e9aed65037197f1ec85e12be6e8cd870fc5608b4de0fffd990f689f376a73',
}
BASE = 'https://www.unicode.org/Public/15.0.0/ucd/'
WIDTHS = ('N', 'Na', 'A', 'H', 'W', 'F')
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--ucd-dir', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
if sys.version_info[:3] != (3, 12, 13) or unicodedata.unidata_version != '15.0.0':
    raise SystemExit('必须使用固定CPython3.12.13 / UCD15.0.0')
texts = {}
for name, expected in SOURCES.items():
    data = (args.ucd_dir / name).read_bytes()
    if hashlib.sha256(data).hexdigest() != expected:
        raise SystemExit('数据摘要不匹配: ' + name)
    texts[name] = data.decode('utf-8')

# 全空间先按UCD缺省N/0初始化，再覆盖显式区间，不能猜未分配字符一律全角。
width = bytearray(0x110000)
combining = bytearray(0x110000)
for line in texts['EastAsianWidth.txt'].splitlines():
    record = line.split('#', 1)[0].strip()
    if not record:
        continue
    bounds, kind = record.split(';')
    limits = bounds.split('..')
    start, end = int(limits[0], 16), int(limits[-1], 16)
    width[start:end + 1] = bytes([WIDTHS.index(kind.strip())]) * (end - start + 1)
first = None
for line in texts['UnicodeData.txt'].splitlines():
    fields = line.split(';')
    point, name, value = int(fields[0], 16), fields[1], int(fields[3])
    if name.endswith(', First>'):
        first = (point, value)
    elif name.endswith(', Last>'):
        assert first is not None and first[1] == value
        combining[first[0]:point + 1] = bytes([value]) * (point - first[0] + 1)
        first = None
    else:
        combining[point] = value
assert first is None
mismatches = []
behavior_hash = 14695981039346656037
for point in range(0x110000):
    char = chr(point)
    for value in (WIDTHS.index(unicodedata.east_asian_width(char)), unicodedata.combining(char)):
        behavior_hash = ((behavior_hash ^ value) * 1099511628211) & ((1 << 64) - 1)
    if WIDTHS[width[point]] != unicodedata.east_asian_width(char) or combining[point] != unicodedata.combining(char):
        mismatches.append(point)
if mismatches:
    raise SystemExit('UCD与固定基线差异，禁止静默修补: ' + repr(mismatches[:20]) + f' 共{len(mismatches)}')

def ranges(values):
    result = []
    start = 0
    while start < len(values):
        value = values[start]
        end = start
        while end + 1 < len(values) and values[end + 1] == value:
            end += 1
        if value:
            result.append((start, end, value))
        start = end + 1
    return result

lines = [
    '// 从官方UCD15.0.0数据生成；Unicode许可与源摘要须随数据保留。',
    '// 生成器tests/generate_unicodedata.py；不依赖宿主.NET Unicode分类。',
    'namespace RyuPy.Runtime;', '',
    '/// <summary>非默认闭区间三元组：起点、终点、属性值；默认宽度N、组合类0。</summary>',
    'internal static class PyUnicodeDataTables', '{',
]
counts = {}
for name, values in [('EastAsianWidth', width), ('Combining', combining)]:
    rows = ranges(values)
    counts[name] = len(rows)
    lines.append(f'    internal static readonly int[] {name} = [')
    for offset in range(0, len(rows), 4):
        lines.append('        ' + ', '.join(f'0x{a:X}, 0x{b:X}, {v}' for a, b, v in rows[offset:offset + 4]) + ',')
    lines.append('    ];')
lines.append('}')
assert len(lines) <= 600
args.output.write_text('\n'.join(lines) + '\n')
print(json.dumps({'python': sys.version.split()[0], 'unicode': unicodedata.unidata_version,
    'behavior_fnv64': behavior_hash, 'verified_codepoints': 0x110000, 'mismatches': 0, 'ranges': counts,
    'source_urls': {name: ('https://www.unicode.org/license.txt' if name == 'LICENSE.txt' else BASE + name) for name in SOURCES}, 'source_sha256': SOURCES,
    'generated_sha256': hashlib.sha256(args.output.read_bytes()).hexdigest()}, indent=2))
