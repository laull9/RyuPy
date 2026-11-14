# 验收磁盘二进制/文本上下文、追加成员、异常退出关闭与逐行读取。
import gzip
import os

path = "compression-example.gz"
with gzip.open(path, "wb", compresslevel=1) as stream:
    print(stream.write(b"a\nb\n"))
print(stream.closed)
with gzip.open(path, "ab") as stream:
    stream.write(b"last")
with gzip.open(path, "rb") as stream:
    print(stream.read(2), stream.readline(), stream.read())
    print(stream.read(), stream.readable(), stream.writable())
print(stream.closed)
with gzip.open(path, "wt", encoding="utf-8", newline="\n") as stream:
    print(stream.write("猫\r\n🎉\rlast\n"))
with gzip.open(path, "rt", encoding="utf-8") as stream:
    print(stream.readline(), stream.readline(), stream.read())
try:
    with gzip.open(path, "rb") as stream:
        raise ValueError("body")
except ValueError:
    print(stream.closed)
stream.close()
try:
    stream.read()
except ValueError:
    print("closed")
os.remove(path)
