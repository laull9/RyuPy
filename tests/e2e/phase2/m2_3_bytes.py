# M2.3：bytes / bytearray 的索引、共享修改与编码错误。
raw = b"a\x00z"
print(raw[0], raw[-1], raw[::-1])
data = bytearray(raw)
alias = data
alias[1] = 66
data.extend(b"!")
print(bytes(data), data is alias)
text = "猫🙂"
print(text.encode("utf-8").decode("utf-8") == text)
print(b"ab" < b"b", bytearray(b"a") == b"a", raw[1:])
del data[0:1]
print(bytes(data), list(data))
del data[:]
print(bytes(data), len(data), bytearray() == b"")
try:
    del data[3]
except IndexError:
    print("bytearray del index")
try:
    b"\xff".decode("utf-8")
except UnicodeDecodeError:
    print("decode error")
