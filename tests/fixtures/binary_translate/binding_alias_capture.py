
# 根描述符、保存方法、共享缓冲区及暂停前固定方法接收者。

identity = bytes(range(256))
source = bytearray(range(256))
alias = source
view = memoryview(source)
assert source.translate(source) == source
assert source.translate(source, source) == b''
assert source.translate(view, view) == b''
assert source.translate(None, view) == b''
assert source is alias and bytes(view) == identity
print('translate.shared-buffer', len(source), source is alias)

method = source.translate
source[65] = 66
expected = bytes([value if value != 65 else 66 for value in range(256)])
assert method(None) == expected
assert method(identity) == expected
assert bytearray.translate(source, None) == expected
assert bytes.translate(b'abca', None, delete=b'a') == b'bc'
root = bytes.translate
assert root(b'abc', identity, b'b') == b'ac'
saved = b'abca'.translate
assert saved(None, delete=b'a') == b'bc'
print('translate.saved-and-root', saved(None, delete=b'a'))

# table/delete都在执行时读取活动view；同长写不破坏导出别名。
backing = bytearray(identity)
table_view = memoryview(backing)
delete_view = table_view[97:98]
backing[97] = 98
assert b'abc'.translate(table_view, delete_view) == b'bc'
assert table_view[97] == 98
print('translate.live-view', b'abc'.translate(table_view, delete_view))

receiver = bytearray(b'abc')
original = receiver

def suspended():
    result = receiver.translate((yield 'table'), delete=(yield 'delete'))
    yield result

pending = suspended()
assert next(pending) == 'table'
receiver = bytearray(b'wrong')
original[0] = 120
assert pending.send(None) == 'delete'
original[1] = 121
answer = pending.send(b'c')
assert answer == b'xy'
assert type(answer) is bytearray
assert original == b'xyc' and receiver == b'wrong'
print('translate.capture-before-yield', answer, original, receiver)

EVENTS = []
def mark(label, value):
    EVENTS.append(label)
    return value
assert saved(mark('table', None), delete=mark('delete', b'a')) == b'bc'
assert saved(*[None], **{'delete': b'a'}) == b'bc'
assert EVENTS == ['table', 'delete']
print('translate.binding-order', EVENTS)
