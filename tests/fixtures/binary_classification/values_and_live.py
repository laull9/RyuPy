# ryupy: pending
"""八项ASCII分类：冻结唯一CP位图，四条公开路线及bytearray保存方法活读；候选未执行。"""
METHODS = ('isalpha', 'isalnum', 'isdigit', 'isspace', 'isascii', 'islower', 'isupper', 'istitle')
DIRECT = {
    'isalpha': lambda value: value.isalpha(),
    'isalnum': lambda value: value.isalnum(),
    'isdigit': lambda value: value.isdigit(),
    'isspace': lambda value: value.isspace(),
    'isascii': lambda value: value.isascii(),
    'islower': lambda value: value.islower(),
    'isupper': lambda value: value.isupper(),
    'istitle': lambda value: value.istitle(),
}
ROOT_DIRECT = {
    'isalpha': lambda kind, value: kind.isalpha(value),
    'isalnum': lambda kind, value: kind.isalnum(value),
    'isdigit': lambda kind, value: kind.isdigit(value),
    'isspace': lambda kind, value: kind.isspace(value),
    'isascii': lambda kind, value: kind.isascii(value),
    'islower': lambda kind, value: kind.islower(value),
    'isupper': lambda kind, value: kind.isupper(value),
    'istitle': lambda kind, value: kind.istitle(value),
}

# 原流A的四路线/两类型完全相同；以下逐字取bytes/direct位图，不用另一分类算法造golden。
SINGLE = {
    "isalpha": "0000000000000000000000000000000000000000000000000000000000000000011111111111111111111111111000000111111111111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
    "isalnum": "0000000000000000000000000000000000000000000000001111111111000000011111111111111111111111111000000111111111111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
    "isdigit": "0000000000000000000000000000000000000000000000001111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
    "isspace": "0000000001111100000000000000000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
    "isascii": "1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
    "islower": "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000111111111111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
    "isupper": "0000000000000000000000000000000000000000000000000000000000000000011111111111111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
    "istitle": "0000000000000000000000000000000000000000000000000000000000000000011111111111111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
}
SAMPLES = (b'', b'abcXYZ', b'abc123', b'0123456789', b' \t\n\v\f\r', b'\x1c\x1d\x1e\x1f',
           b'\x00\x7f', b'\x80\xff', b'\x85\xa0', b'a\x80', b'A\xff', b'aA', b'a0!', b'A0!',
           b'123!', b'Ab Cd', b'AB', b'ab', b'A B', b'A1B', b'A1b', b'Ab\xffCd', b'Ab\xffcd',
           b'A\x00B', b'A\x00b', b'A_B', b'A_b', b"A'b", b"A'B", b'abc\x00', b'9a', b'a\n')
# 原流B的32个bool压成有序位图，不改变样本或内容。
MIXED = {
    "isalpha": "01000000000100001100000000000000",
    "isalnum": "01110000000100001101100000000010",
    "isdigit": "00010000000000000000000000000000",
    "isspace": "00001000000000000000000000000000",
    "isascii": "11111110000111111111100111111111",
    "islower": "00100000010010000100000000000111",
    "isupper": "00000000001001001011000101001000",
    "istitle": "00000000001001010011010101001000",
}
LIVE = {
    "isalpha": "11000",
    "isalnum": "11100",
    "isdigit": "00000",
    "isspace": "00000",
    "isascii": "11111",
    "islower": "10000",
    "isupper": "01100",
    "istitle": "01101",
}


def bit(answer):
    assert type(answer) is bool
    assert answer is True or answer is False
    return '1' if answer else '0'


# 全256单字节×8方法×2类型×4路线；每method一行位图，禁止展开数万行。
for kind in (bytes, bytearray):
    for name in METHODS:
        vectors = [[], [], [], []]
        root = getattr(kind, name)
        assert root is getattr(kind, name)
        for number in range(256):
            value = kind([number])
            saved = getattr(value, name)
            assert saved.__self__ is value
            answers = (DIRECT[name](value), saved(), ROOT_DIRECT[name](kind, value), root(value))
            for route, answer in enumerate(answers):
                vectors[route].append(bit(answer))
        actual = [''.join(vector) for vector in vectors]
        assert actual == [SINGLE[name], SINGLE[name], SINGLE[name], SINGLE[name]]
        print('V.single-' + kind.__name__ + '-' + name, actual[0], True)

for kind in (bytes, bytearray):
    for name in METHODS:
        root = getattr(kind, name)
        actual = []
        for index, source in enumerate(SAMPLES):
            value = kind(source)
            answers = (DIRECT[name](value), getattr(value, name)(), ROOT_DIRECT[name](kind, value), root(value))
            for answer in answers:
                assert bit(answer) == MIXED[name][index]
            actual.append(bit(answers[0]))
        print('V.mixed-' + kind.__name__ + '-' + name, ''.join(actual))

# 捕获后同长写/append/删除/extend均可见；保存方法没有长租约或内容缓存。
for name in METHODS:
    value = bytearray(b'a')
    saved = getattr(value, name)
    actual = [bit(saved())]
    value[0] = 65
    actual.append(bit(saved()))
    value.append(49)
    actual.append(bit(saved()))
    del value[:]
    actual.append(bit(saved()))
    value.extend(b'Ab Cd')
    actual.append(bit(saved()))
    assert saved.__self__ is value and ''.join(actual) == LIVE[name]
    assert value == bytearray(b'Ab Cd')
    print('V.live-' + name, ''.join(actual), value)

value = bytearray(b'a')
view = memoryview(value)
saved = value.isalpha
view[0] = 49
assert saved() is False and value.isalpha() is False
view.release()
value.append(65)
assert saved() is False and value == bytearray(b'1A')
print('V.view-alias', saved(), value)

# 名字重绑不改变已保存的接收者；root保存旧类型的行为在binder应用里另验。
original = bytearray(b'A')
receiver = original
saved = receiver.isalpha
receiver = bytearray(b'1')
assert saved.__self__ is original and saved() is True and receiver.isalpha() is False
print('V.saved-receiver', saved(), receiver.isalpha(), saved.__self__ is original)

# 邻接旧方法不受新族分派影响；不引入bytearray.clear、hex或根方法等旧未支持域。
assert b'Ab'.decode() == 'Ab' and bytearray(b'Ab').decode() == 'Ab'
assert b'\x00\xff'.hex() == '00ff'
assert b'Ab'.startswith(b'A') and bytearray(b'Ab').endswith(b'b')
assert '猫A'.lower() == '猫a' and '猫a'.upper() == '猫A'
print('V.old-methods', b'Ab'.decode(), b'\x00\xff'.hex(), bytearray(b'Ab').endswith(b'b'))
print('V.complete', True)
