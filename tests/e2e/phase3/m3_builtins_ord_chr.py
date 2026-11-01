# 固定项目wcwidth的首阻断：字符/字节码点内建，不把UTF16单元当字符。
for text in ["a", "汉", "🎉", "\0", "\U0010ffff"]:
    point = ord(text)
    print(point, chr(point) == text)
print(ord(b"A"), ord(bytearray(b"\xff")), ord(chr(0xd800)))
print(ord(chr(False)), ord(chr(True)))


class Index:
    def __index__(self):
        return 0x1f389


print(chr(Index()))
for text in ["", "ab", "🎉a", b"", b"ab", bytearray(b"ab"), 1, None]:
    try:
        ord(text)
    except TypeError:
        print("ord类型错误")
for point in [-1, 0x110000, 10 ** 50]:
    try:
        chr(point)
    except (ValueError, OverflowError):
        print("chr范围错误")
for point in [1.0, "65", None]:
    try:
        chr(point)
    except TypeError:
        print("chr类型错误")


class BadIndex:
    def __index__(self):
        return "65"


try:
    chr(BadIndex())
except TypeError:
    print("index返回类型错误")
# 分别直接调用，避免用未接入的一等内建函数值掩盖参数校验。
try:
    ord()
except TypeError:
    print("ord缺参数")
try:
    chr()
except TypeError:
    print("chr缺参数")
try:
    ord("a", "b")
except TypeError:
    print("ord多参数")
try:
    chr(1, 2)
except TypeError:
    print("chr多参数")
try:
    ord(value="a")
except TypeError:
    print("ord拒绝关键字")
try:
    chr(value=65)
except TypeError:
    print("chr拒绝关键字")
