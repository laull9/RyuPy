# 错误策略按实际字节分组；覆盖截断、非法UTF8、UTF16代理项、BOM及ASCII替换字符。
samples = [b'', b'abc', b'\xff', b'\xff\xfe', b'\xfe\xff', b'\xc0\xaf', b'\xe0\x80\x80',
           b'\xed\xa0\x80', b'\xf4\x90\x80\x80', b'\xf0\x90\x80', b'\xe2\x82',
           b'\xe2A\x82', b'\x80\xbf', b'\x00\xd8', b'\x00\xd8A', b'\x00\xdc',
           b'\x00\xd8A\x00', b'\x00\xd8\x00\xd8', b'\x00\xd8\x00\xdc',
           b'\xff\xfe\x00\xd8A', b'\xfe\xff\xd8\x00A']
# 固定算式生成多样短字节，不依赖外部random版本。
for i in range(256):
    samples.append(bytes([i]))
    samples.append(bytes([i, (i * 73 + 29) % 256, (i * 19 + 7) % 256]))
for lead in (0xc2, 0xdf, 0xe0, 0xed, 0xef, 0xf0, 0xf4):
    for tail in (0x7f, 0x80, 0x8f, 0x90, 0x9f, 0xa0, 0xbf, 0xc0):
        samples.append(bytes([lead, tail]))
        samples.append(bytes([lead, tail, 0x80]))
        samples.append(bytes([lead, tail, 0x80, 0x80]))
for encoding in ('utf-8', 'ascii', 'latin-1', 'utf-16', 'utf-16-le', 'utf-16-be'):
    for policy in ('strict', 'replace', 'ignore'):
        for data in samples:
            for owner in (bytes, bytearray):
                try:
                    result = owner(data).decode(encoding=encoding, errors=policy)
                    print(encoding, policy, list(map(ord, result)))
                except UnicodeDecodeError:
                    print(encoding, policy, 'UnicodeDecodeError')

for policy in ('surrogateescape', 'surrogatepass', 'backslashreplace', 'unknown-handler'):
    print(b'valid'.decode(errors=policy))
try:
    b'\xff'.decode(errors='unknown-handler')
except LookupError:
    print('unknown-handler')
