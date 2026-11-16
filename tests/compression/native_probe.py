# 双向容器验收入口：读取 CPython 产物，生成待 CPython 解压的文件，并覆盖文件与错误协议。
import gzip
import zlib

for kind in ["empty", "mixed"]:
    with open(kind + ".bin", "rb") as source:
        original = source.read()
    for level in [-1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9]:
        for window in [15, -15, 31]:
            name = kind + "-" + str(level) + "-" + str(window)
            with open("cp-" + name, "rb") as source:
                print(zlib.decompress(source.read(), window) == original)
            with open("out-" + name, "wb") as target:
                target.write(zlib.compress(original, level, wbits=window))
    for stamp in [0, 1, 1234567890, 4294967295]:
        with open("out-" + kind + "-mtime-" + str(stamp), "wb") as target:
            target.write(gzip.compress(original, mtime=stamp))

with open("cp-members", "rb") as source:
    packed = source.read()
print(gzip.decompress(packed) == b"one\0two")
print(zlib.decompress(packed, 47) == b"one")
with open("cp-header", "rb") as source:
    packed = source.read()
print(gzip.decompress(packed), zlib.decompress(packed, 31))
with open("out-time", "wb") as target:
    target.write(gzip.compress(b"now"))
with gzip.open("out-file", "wb", compresslevel=-1) as target:
    target.write(b"first\n")
    target.flush()
with gzip.open("out-file", "ab") as target:
    target.writelines([b"second\n", b"end"])
with gzip.open("out-file", "rb") as source:
    print(list(source), source.read())
with gzip.open("out-empty", "wb") as target:
    pass
with gzip.open("out-text", "wt", encoding="utf-8", newline="\r\n") as target:
    target.write("猫\n🎉\n")
with gzip.open("out-text", "rt") as source:
    print(source.read(1), source.readline(), source.read())
try:
    with gzip.open("out-file", "rb") as source:
        raise ValueError("body")
except ValueError:
    print(source.closed)
for name in ["cp-truncated", "cp-bad-crc", "cp-tail", "cp-bad-size"]:
    try:
        with gzip.open(name, "rb") as source:
            source.read()
    except (EOFError, gzip.BadGzipFile) as error:
        print(name, type(error).__name__, source.closed)
try:
    zlib.decompress(b"bad")
except zlib.error as error:
    print(type(error) is zlib.error)
try:
    raise MemoryError("bounded")
except MemoryError as error:
    print(str(error))
print(zlib.crc32(b"123456789"), zlib.adler32(b"123456789"))
