
# 全字节转换、先删除后映射，以及精确不可变/可变接收者的身份合同。

source = bytes(range(256))
reverse = bytes(range(255, -1, -1))
deleted = bytes(range(0, 256, 3))
expected = bytes([255 - value for value in range(256) if value % 3 != 0])
for table in (reverse, bytearray(reverse), memoryview(reverse), memoryview(bytearray(reverse)).cast('c')):
    for delete in (deleted, bytearray(deleted), memoryview(deleted), memoryview(bytearray(deleted)).cast('c')):
        result = source.translate(table, delete)
        assert result == expected
        mutable = bytearray(source)
        changed = mutable.translate(table, delete=delete)
        assert changed == expected
        assert type(changed) is bytearray
        assert mutable == source
print('translate.all-bytes', len(expected), expected)

# 删除检查原字节而非映射后的字节；映射为零不等于删除。
table = bytearray(range(256))
table[97] = 98
table[99] = 0
assert b'abcab'.translate(table, b'b') == b'b\x00b'
assert b'abc'.translate(None, b'ac') == b'b'
assert b'abc'.translate(None, delete=b'abc') == b''
print('translate.delete-before-map', b'abcab'.translate(table, b'b'))

identity = bytes(range(256))
for original in (b'', b'a', b'unchanged\x00\xff', source):
    for table in (None, identity, bytearray(identity), memoryview(identity)):
        assert original.translate(table) is original
        assert original.translate(table, b'') is original
        assert original.translate(table, delete=b'') is original
        mutable = bytearray(original)
        first = mutable.translate(table)
        second = mutable.translate(table)
        assert first == mutable and second == mutable
        assert first is not mutable and second is not mutable and first is not second
        assert type(first) is bytearray
print('translate.identity', source.translate(None) is source)

untouched = b'abc'
assert untouched.translate(None, b'xyz') is untouched
assert untouched.translate(table) is untouched
empty = b''
assert empty.translate(reverse) is empty
print('translate.empty-and-unused-delete', untouched.translate(None, b'xyz'))
