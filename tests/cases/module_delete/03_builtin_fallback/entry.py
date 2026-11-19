# ryupy: pending
# 删除只影响当前全局绑定；读可以回退 builtin，删除本身绝不能删除 builtin。
saved_len = len
saved_range = range
len: int = 41
range = ['shadow']

def read_len():
    return len

def remove_len():
    global len
    del len

assert read_len() == 41
del len, range
assert read_len() is saved_len and len([1, 2, 3]) == 3
assert range is saved_range and list(range(3)) == [0, 1, 2]
try:
    remove_len()
except NameError as error:
    assert str(error) == "name 'len' is not defined"
    print('builtin is not deletable global', type(error).__name__, str(error))
else:
    assert False
assert len is saved_len and len([1, 2]) == 2
len = None
remove_len()
assert len is saved_len
len = 19
assert read_len() == 19
del len
assert read_len() is saved_len
print('builtin fallback and typed shadow', len([1, 2]), list(range(3)))
