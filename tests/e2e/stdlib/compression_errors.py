# 验收损坏、逐字节截断、首成员与多成员尾部以及真实异常身份。
import zlib
import gzip
import re

payload = gzip.compress(b"hello" * 20, mtime=0)
rejected = 0
for end in range(1, len(payload)):
    try:
        gzip.decompress(payload[:end])
    except (EOFError, gzip.BadGzipFile, zlib.error):
        rejected += 1
print(rejected == len(payload) - 1)
for window in [15, -15, 31]:
    packed = zlib.compress(b"abc" * 30, wbits=window)
    rejected = 0
    for end in range(len(packed)):
        try:
            zlib.decompress(packed[:end], window)
        except zlib.error:
            rejected += 1
    print(rejected == len(packed))
for suffix in [b"x", b"\0x", b"\x1f\x8b"]:
    try:
        gzip.decompress(payload + suffix)
    except (EOFError, gzip.BadGzipFile):
        print("bad tail")
bad = bytearray(payload)
bad[-8] ^= 1
try:
    gzip.decompress(bad)
except gzip.BadGzipFile as error:
    print(isinstance(error, OSError), type(error) is gzip.BadGzipFile)
try:
    zlib.decompress(b"bad")
except zlib.error as error:
    print(type(error) is zlib.error, isinstance(error, re.error))
print(zlib.error.__name__, zlib.error.__module__)
print(gzip.BadGzipFile.__name__, gzip.BadGzipFile.__module__)
for call in [lambda: zlib.decompress(b"", bufsize=0), lambda: gzip.compress(b"", compresslevel=10)]:
    try:
        call()
    except (ValueError, OverflowError, zlib.error):
        print("bad argument")
from zlib import error as ZipError
from gzip import BadGzipFile as GzipError
for error in [ZipError("z"), GzipError("g"), MemoryError("limit")]:
    try:
        raise error
    except (ZipError, GzipError, MemoryError) as caught:
        print(caught is error, str(caught))
for flag in [2, 32]:
    header = bytearray(payload[:10])
    header[3] = flag
    if flag == 2:
        header += b"\0\0"
    changed = bytes(header) + payload[10:]
    print(gzip.decompress(changed) == b"hello" * 20)
    try:
        zlib.decompress(changed, 31)
    except ZipError:
        print("invalid header")
