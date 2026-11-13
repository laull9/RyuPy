# 验收压缩容器、显式时间戳、初始校验值与字节容器，不比较压缩器输出布局。
import zlib
import gzip

data = b"hello\x00world\n" * 50
for level in [-1, 0, 1, 6, 9]:
    for window in [15, -15, 31]:
        packed = zlib.compress(bytearray(data), level=level, wbits=window)
        print(zlib.decompress(packed + b"tail", window) == data)
print(zlib.decompress(zlib.compress(data), 0, 1) == data)
print(zlib.decompress(zlib.compress(data, 6, -15), -15) == data)
print(zlib.decompress(gzip.compress(data, mtime=123), 47) == data)
for stamp in [0, 1, 1234567890, 4294967295]:
    packed = gzip.compress(data, compresslevel=6, mtime=stamp)
    print(list(packed[4:8]), gzip.decompress(packed) == data)
print(gzip.decompress(gzip.compress(b"one", mtime=0) + gzip.compress(b"two", mtime=1) + b"\0\0"))
print(gzip.decompress(b""))
for seed in [0, 1, -1, 4294967296, 18446744073709551616]:
    print(zlib.adler32(data, seed), zlib.crc32(data, seed))
print(zlib.adler32(b"world", zlib.adler32(b"hello")) == zlib.adler32(b"helloworld"))
print(zlib.crc32(b"world", zlib.crc32(b"hello")) == zlib.crc32(b"helloworld"))
for seed in [-1, 65535, 4294967295]:
    print(zlib.adler32(b"", seed))
for window in [15, -15, 31]:
    print(zlib.decompress(zlib.compress(b"", wbits=window), window))
print(gzip.decompress(gzip.compress(b"", compresslevel=-1, mtime=12)))
with memoryview(data) as view:
    print(zlib.decompress(zlib.compress(view)) == data)
    print(gzip.decompress(gzip.compress(view, mtime=0)) == data)
    print(zlib.crc32(view) == zlib.crc32(data), zlib.adler32(view) == zlib.adler32(data))
with memoryview(zlib.compress(data)) as view:
    print(zlib.decompress(view) == data)
