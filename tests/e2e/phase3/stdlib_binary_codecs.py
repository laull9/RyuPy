# 编码合同：原始 buffer、替代字母表、宽松/严格解码与二进制异常身份。
import base64
import binascii

for data in [b"", b"f", b"fo", b"foo", b"foob", b"fooba", b"foobar", bytes(range(256))]:
    for encode, decode in [(base64.b64encode, base64.b64decode), (base64.urlsafe_b64encode, base64.urlsafe_b64decode), (base64.b16encode, base64.b16decode), (base64.b32encode, base64.b32decode)]:
        encoded = encode(memoryview(data))
        print(encoded, decode(encoded) == data, decode(encoded.decode("ascii")) == data)
print(base64.b64encode(b"\xfb\xff", b"-_"))
print(base64.b64decode(b"-_8=", altchars=b"-_", validate=True))
print(base64.standard_b64encode(b"\xfb\xff"), base64.standard_b64decode("+/8="))
print(base64.b64decode(b"! Zm9v\n??"))
print(base64.b16decode("deadBEEF", casefold=True))
print(base64.b32decode("mzxw6===", casefold=True))
print(base64.b32decode("M1======", map01=b"I"))
print(binascii.hexlify(b"\x00\xff\x12"), binascii.unhexlify("00ff12"))
print(binascii.b2a_hex(b"\x00\xff"), binascii.a2b_hex(b"00ff"))
print(binascii.hexlify(b"12345", b":", 2), binascii.hexlify(b"12345", "-", -2))
print(binascii.crc32(b"123456789"), binascii.crc32(b"456789", binascii.crc32(b"123")))
print(binascii.b2a_base64(b"foo"), binascii.b2a_base64(b"foo", newline=False))
print(binascii.a2b_base64("Zm9v", strict_mode=True))
for text in [b"a", b"aa", b"a===", b"=AAA", b"AA=A", b"AA==x", b"AA==\n", b"AAAA=", b"===="]:
    for strict in [False, True]:
        try:
            print(text, strict, binascii.a2b_base64(text, strict_mode=strict))
        except binascii.Error as error:
            print(text, strict, type(error).__name__, str(error))
for decode, text in [(base64.b16decode, b"ff"), (base64.b16decode, b"0"), (base64.b32decode, b"mzxw6==="), (base64.b32decode, b"MY====="), (binascii.unhexlify, b"zz")]:
    try:
        decode(text)
    except binascii.Error as error:
        print(type(error).__name__, str(error), isinstance(error, ValueError))
for function, value in [(base64.b64encode, "foo"), (base64.b64decode, "é"), (binascii.unhexlify, "é"), (binascii.crc32, [1, 2])]:
    try:
        function(value)
    except (TypeError, ValueError) as error:
        print(type(error).__name__)
