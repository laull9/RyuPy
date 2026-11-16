# ryupy: pending
# 完整保留 cell 和 nonlocal 删除边界：本次普通模块名字删除不能假装覆盖此协议。
def build():
    value = ['cell']

    def read():
        return value

    def remove():
        nonlocal value
        del value

    def restore(replacement):
        nonlocal value
        value = replacement

    return read, remove, restore

read, remove, restore = build()
saved = read()
remove()
for operation in (read, remove):
    try:
        operation()
    except NameError as error:
        assert not isinstance(error, UnboundLocalError)
        print('empty free cell', type(error).__name__, str(error))
    else:
        assert False
restore(['restored'])
assert read() == ['restored'] and saved == ['cell']

def delete_local_cell():
    value = 1
    def capture():
        return value
    del value
    return value

try:
    delete_local_cell()
except UnboundLocalError as error:
    print('empty local cell', type(error).__name__, str(error))
else:
    assert False
print('cell restore remains live', read())
