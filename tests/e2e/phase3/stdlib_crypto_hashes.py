# 摘要合同：增量状态、快照复制、算法元数据与 HMAC 的真实 BCL 路径。
import hashlib
import hmac

for name in ["sha256", "sha512", "sha1", "md5"]:
    print(name in hashlib.algorithms_available, name in hashlib.algorithms_guaranteed)
    constructor = getattr(hashlib, name)
    original = constructor(bytearray(b"ab"), usedforsecurity=False)
    copied = original.copy()
    print(original.name, original.digest_size, original.block_size)
    print(original.hexdigest(), original.digest().hex(), original.hexdigest())
    print(original.update(memoryview(b"c")))
    copied.update(b"d")
    print(original.hexdigest(), copied.hexdigest(), hashlib.new(name, b"abc").hexdigest())
    tag = hmac.new(bytearray(b"key"), b"ab", digestmod=name)
    branch = tag.copy()
    print(tag.name, tag.digest_size, tag.block_size, tag.hexdigest())
    tag.update(memoryview(b"c"))
    branch.update(b"d")
    print(tag.hexdigest(), branch.hexdigest(), tag.digest().hex())
    print(hmac.digest(b"key", b"abc", name).hex())
print(hmac.new(b"key", b"abc", hashlib.sha256).hexdigest())
print(hmac.digest(b"key", b"abc", hashlib.sha512).hex())
print(hashlib.new("SHA256", b"abc").name)
for alias in ["sha-256", "SHA-512", "sha-1", "MD5"]:
    print(hashlib.new(alias, data=b"abc", usedforsecurity=[]).name)
tag = hmac.HMAC(key=b"key", digestmod="sha256")
print(tag.update(msg=b"abc"), tag.hexdigest(), isinstance(tag, hmac.HMAC))
key = bytearray(b"key")
tag = hmac.new(key, digestmod=hashlib.sha256)
key[0] = 0
tag.update(b"abc")
print(tag.hexdigest())
for construct in [hashlib.sha256, hashlib.sha512, hashlib.sha1, hashlib.md5]:
    for invalid in ["abc", [1], None]:
        try:
            construct(invalid)
        except TypeError as error:
            print(type(error).__name__, str(error))
    value = construct(b"abc")
    try:
        value.update(data=b"d")
    except TypeError as error:
        print(type(error).__name__)
    released = memoryview(b"d")
    released.release()
    try:
        value.update(released)
    except ValueError as error:
        print(type(error).__name__, str(error))
    print(value.hexdigest())
for left, right in [(b"a", b"a"), (b"a", bytearray(b"b")), (memoryview(b"abc"), b"abc"), ("abc", "abc"), ("abc", "abcd")]:
    print(hmac.compare_digest(left, right))
for left, right in [("é", "é"), (b"a", "a"), ([1], [1])]:
    try:
        hmac.compare_digest(left, right)
    except TypeError as error:
        print(type(error).__name__)
for name in ["sha256", "sha512", "sha1", "md5"]:
    print(hmac.digest(b"k" * 200, b"", name).hex())
    for size in [0, 1, 55, 56, 63, 64, 65, 127, 128, 129, 4096]:
        message = b"x" * size
        print(hashlib.new(name, memoryview(message).cast("c")).hexdigest())
        print(hmac.digest(memoryview(b"key"), bytearray(message), name).hex())
try:
    hashlib.new("not-an-algorithm")
except ValueError as error:
    print(type(error).__name__)
try:
    hashlib.sha256("abc")
except TypeError as error:
    print(type(error).__name__)
try:
    hmac.new(memoryview(b"key"), digestmod="sha256")
except TypeError as error:
    print(type(error).__name__)
for operation in [lambda: hashlib.new("unknown", "text"),
                  lambda: hmac.digest("key", b"msg", "unknown"),
                  lambda: hmac.digest(b"key", "msg", "unknown")]:
    try:
        operation()
    except TypeError as error:
        print(type(error).__name__, str(error))
