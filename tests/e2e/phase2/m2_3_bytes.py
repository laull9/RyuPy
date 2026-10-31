# ryupy: pending
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
try:
    b"\xff".decode("utf-8")
except UnicodeDecodeError:
    print("decode error")
