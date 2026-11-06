"""F2/R3：现六载体 escape 全字节值、视图短租约与释放错误；全局 bytes 驻留身份独立 pending。"""
import re

# 逐字来自唯一67行参考 X.escape_array.256，不以另一份 escape 实现生成期待。
GOLDEN = (
    b'\x00\x01\x02\x03\x04\x05\x06\x07\x08\x5c\x09\x5c\x0a\x5c\x0b\x5c\x0c\x5c\x0d\x0e\x0f\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a'
    b'\x1b\x1c\x1d\x1e\x1f\x5c\x20\x21\x22\x5c\x23\x5c\x24\x25\x5c\x26\x27\x5c\x28\x5c\x29\x5c\x2a\x5c\x2b\x2c\x5c\x2d\x5c\x2e\x2f\x30'
    b'\x31\x32\x33\x34\x35\x36\x37\x38\x39\x3a\x3b\x3c\x3d\x3e\x5c\x3f\x40\x41\x42\x43\x44\x45\x46\x47\x48\x49\x4a\x4b\x4c\x4d\x4e\x4f'
    b'\x50\x51\x52\x53\x54\x55\x56\x57\x58\x59\x5a\x5c\x5b\x5c\x5c\x5c\x5d\x5c\x5e\x5f\x60\x61\x62\x63\x64\x65\x66\x67\x68\x69\x6a\x6b'
    b'\x6c\x6d\x6e\x6f\x70\x71\x72\x73\x74\x75\x76\x77\x78\x79\x7a\x5c\x7b\x5c\x7c\x5c\x7d\x5c\x7e\x7f\x80\x81\x82\x83\x84\x85\x86\x87'
    b'\x88\x89\x8a\x8b\x8c\x8d\x8e\x8f\x90\x91\x92\x93\x94\x95\x96\x97\x98\x99\x9a\x9b\x9c\x9d\x9e\x9f\xa0\xa1\xa2\xa3\xa4\xa5\xa6\xa7'
    b'\xa8\xa9\xaa\xab\xac\xad\xae\xaf\xb0\xb1\xb2\xb3\xb4\xb5\xb6\xb7\xb8\xb9\xba\xbb\xbc\xbd\xbe\xbf\xc0\xc1\xc2\xc3\xc4\xc5\xc6\xc7'
    b'\xc8\xc9\xca\xcb\xcc\xcd\xce\xcf\xd0\xd1\xd2\xd3\xd4\xd5\xd6\xd7\xd8\xd9\xda\xdb\xdc\xdd\xde\xdf\xe0\xe1\xe2\xe3\xe4\xe5\xe6\xe7'
    b'\xe8\xe9\xea\xeb\xec\xed\xee\xef\xf0\xf1\xf2\xf3\xf4\xf5\xf6\xf7\xf8\xf9\xfa\xfb\xfc\xfd\xfe\xff'
)


def carrier(kind, content):
    if kind == 'readonly_B' or kind == 'readonly_c':
        owner = bytes(content)
    else:
        owner = bytearray(content)
    if kind == 'array':
        return owner, owner
    if kind == 'view_slice':
        owner = bytearray(b'!' + content + b'?')
    root = memoryview(owner)
    if kind == 'view_c' or kind == 'readonly_c':
        value = root.cast('c')
        root.release()
    elif kind == 'view_slice':
        value = root[1:-1]
        root.release()
    else:
        value = root
    return value, owner


alphabet = bytes(range(256))
cases = [('empty', b'', b''), ('plain', b'abc', b'abc'),
         ('special', b' a.b#\\', br'\ a\.b\#\\'), ('all', alphabet, GOLDEN)]
saved_escape = re.escape
for kind in ('array', 'view_B', 'view_c', 'view_slice', 'readonly_B', 'readonly_c'):
    for label, content, expected in cases:
        value, owner = carrier(kind, content)
        direct = re.escape(value)
        saved = saved_escape(value)
        assert direct == saved == expected and type(direct) is bytes and type(saved) is bytes
        assert direct is not value and saved is not value
        assert re.fullmatch(direct, content).group() == content
        # 返回的 bytes 不再活读，导出只存活于调用内；offset/child 的父 view 已释放。
        if isinstance(owner, bytearray) and len(owner) != 0:
            owner[0] = 90
        assert direct == expected and saved == expected
        if isinstance(value, memoryview):
            value.release()
        if isinstance(owner, bytearray):
            owner.append(33)
        assert direct == expected and saved == expected
        print('F2.escape-' + kind + '-' + label, len(direct), direct[:8], direct[-8:])

# 同一 bytearray 及保存的模块方法按每次当前值读取，不以对象身份/长度缓存。
value = bytearray(b'X')
first = saved_escape(value)
value[0] = ord('.')
second = saved_escape(value)
value[0] = 92
third = saved_escape(value)
assert [first, second, third] == [b'X', br'\.', br'\\']
value.append(33)
print('F2.escape-current', [first, second, third], value)

# 原 X 的空 released-view 错误必须先检查活性；非空与现 view 载体为新 CP-first 组合。
for kind in ('view_B', 'view_c', 'view_slice', 'readonly_B', 'readonly_c'):
    for label, content in [('empty', b''), ('plain', b'X')]:
        value, owner = carrier(kind, content)
        value.release()
        try:
            re.escape(value)
        except TypeError as error:
            assert str(error) == 'decoding to str: need a bytes-like object, memoryview found'
            assert error.__context__ is None
            if isinstance(owner, bytearray):
                owner.append(33)
            print('F2.escape-released-' + kind + '-' + label, type(error).__name__, str(error))
        else:
            raise AssertionError('released view escaped')

# exact/text 旧路径只验已纳域值与3字节非原身份；0/1驻留事实保原 pending 断言。
plain = bytes([97, 98, 99])
escaped = re.escape(plain)
assert escaped == plain and type(escaped) is bytes and escaped is not plain
assert re.escape(alphabet) == GOLDEN
assert re.escape(b' a.b#\\') == br'\ a\.b\#\\'
assert re.escape('a b[c]#') == 'a\\ b\\[c\\]\\#'
print('F2.escape-exact-text', escaped, re.escape('a b[c]#'), len(GOLDEN))
p = re.compile(b'a')
replacement = bytes([88, 89, 90])
source = bytes([122, 122])
assert p.sub(replacement, b'a') is replacement
assert p.sub(replacement, source) is source
assert re.sub('(a)', r'<\1>', 'a') == '<a>'
print('F2.escape-regression', replacement, source, re.sub('(a)', r'<\1>', 'a'))
print('F2.escape-complete')
