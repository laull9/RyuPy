# M3.4：内存流的游标、逐行迭代、截断与关闭协议。
import io

text = io.StringIO("猫\r\nsecond\nlast", newline=None)
print(text.readline(), end="|")
print(text.tell())
print(text.readlines())
text.seek(0)
print(text.write("好"), text.getvalue())
text.seek(2)
print(text.truncate(), text.getvalue())
with io.BytesIO(b"ab\ncd") as raw:
    print(raw.readline(), raw.tell())
    raw.seek(0, 2)
    print(raw.write(b"!"), raw.getvalue())
    raw.seek(0)
    print(list(raw))
print(raw.closed)
try:
    raw.read()
except ValueError:
    print("closed")
with io.StringIO() as output:
    output.writelines(["a", "猫"])
    print(output.readable(), output.writable(), output.seekable())
    print(output.getvalue())

# None 作为读取大小与 readlines hint 都等价于无限制。
for stream in [io.StringIO("a\nb"), io.BytesIO(b"a\nb")]:
    print(stream.readlines(None))
    stream.seek(0)
    print(stream.read(None))
    stream.seek(0)
    print(stream.readline(None))
    stream.close()

# 错误参数和关闭状态同时出现时，保留各内存流的校验顺序。
for stream in [io.StringIO(), io.BytesIO()]:
    stream.close()
    try:
        stream.read("bad")
    except TypeError:
        print("read type first")
    try:
        stream.write(1)
    except TypeError:
        print("write type first")
    except ValueError:
        print("write closed first")

# None返回值存入模块槽后仍是合法None，不能与未绑定的CLR null混淆。
output = io.StringIO()
result = output.flush()
print(result is None)
result = output.writelines([])
print(result is None)
result = output.close()
print(result is None)
