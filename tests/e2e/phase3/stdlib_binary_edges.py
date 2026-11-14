# 穷举短 Base64 文本的 padding/garbage 状态；错误消息与固定 CPython 逐字比较。
import base64
import binascii
from binascii import Error as BinaryError

corpus = [b""]
level = [b""]
for length in range(5):
    following = []
    for prefix in level:
        for suffix in [b"A", b"B", b"=", b"!", b"\xff"]:
            following.append(prefix + suffix)
    corpus.extend(following)
    level = following
corpus.extend([b"AA===A", b"AAAA=", b"AA==x", b"A=A===", b"AB==", b"AAB=", b"====AAAA"])
for value in corpus:
    for strict in [False, True]:
        try:
            print(value, strict, binascii.a2b_base64(value, strict_mode=strict))
        except BinaryError as error:
            print(value, strict, type(error).__name__, str(error))

print(BinaryError.__module__, issubclass(BinaryError, ValueError))
error = BinaryError("bad", 3)
print(type(error).__name__, error.args, str(error))
print(binascii.b2a_base64(b"", newline=[]))
print(binascii.a2b_base64(b"", strict_mode=[]))
print(binascii.hexlify(data=b"abc", sep=":", bytes_per_sep=-2))
print(binascii.hexlify(b"abc", b"\xff"))
for initial in [-1, 2**100, -(2**100) + 5]:
    print(binascii.crc32(b"abc", initial))
for text in [b"MY======", b"MZ======", b"MZXW6===", b"========", b"AAA=====", b"M=AAAAAA"]:
    try:
        print(base64.b32decode(text))
    except BinaryError as error:
        print(type(error).__name__, str(error))
for function in [base64.b64encode, base64.b16encode, base64.b32encode, binascii.hexlify, binascii.crc32]:
    for data in ["abc", [1, 2, 3], 1, None]:
        try:
            function(data)
        except TypeError as error:
            print(type(error).__name__)
released = memoryview(b"a")
released.release()
for function in [base64.b64encode, base64.b64decode, base64.b16decode, base64.b32decode, binascii.hexlify]:
    try:
        function(released)
    except ValueError as error:
        print(type(error).__name__, str(error))
for text in ["é", "𝒜"]:
    for function in [base64.b64decode, base64.b16decode, base64.b32decode, binascii.unhexlify]:
        try:
            function(text)
        except ValueError as error:
            print(type(error).__name__, str(error))
