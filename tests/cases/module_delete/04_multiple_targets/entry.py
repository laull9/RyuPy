# ryupy: pending
# 多目标逐项删除，后项失败不回滚前项，重复目标和 builtin 缺绑定也遵守同一顺序。
first = ['first']
last = ['last']
try:
    del first, absent, last
except NameError as error:
    assert str(error) == "name 'absent' is not defined"
    print('middle failure', type(error).__name__, str(error))
else:
    assert False
try:
    first
except NameError:
    print('first stays deleted')
else:
    assert False
assert last == ['last']
repeat = ['repeat']
try:
    del repeat, repeat, last
except NameError as error:
    assert str(error) == "name 'repeat' is not defined"
    print('duplicate failure', type(error).__name__, str(error))
else:
    assert False
assert last == ['last']
prefix = 1
try:
    del prefix, len, last
except NameError as error:
    assert str(error) == "name 'len' is not defined"
    print('builtin middle failure', type(error).__name__, str(error))
else:
    assert False
try:
    prefix
except NameError:
    print('prefix stays deleted')
else:
    assert False
assert len(last) == 1 and last == ['last']
first, repeat = 10, 20
del (first, repeat), last
try:
    last
except NameError:
    print('nested target list completes')
else:
    assert False
