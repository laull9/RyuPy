# ryupy: pending
# M4.2：压缩扩展往返与模块自定义异常。
import zlib
from zlib import decompress

source = b"ryupy\x00" * 10
print(decompress(zlib.compress(source)) == source)
try:
    decompress(b"invalid")
except zlib.error:
    print("zlib error")
