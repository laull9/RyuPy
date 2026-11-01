# ryupy: pending
# 显式兼容欠账：CPython JSON bytes使用surrogatepass，不等同Unicode严格解码。
import json
print(ord(json.loads(b'"\xed\xa0\x80"')))
print(ord(json.loads(b'\xff\xfe"\x00\x00\xd8"\x00')))
