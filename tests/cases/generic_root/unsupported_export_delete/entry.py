# 完整CP消费顺序；模块删除本身若未支持也保留，不裁尾冒充守卫已通过。
import typing as t
saved = t.Generic
del t.Generic
def check(label, call):
    try:
        call()
    except NameError as error:
        print(label, str(error))
    else:
        assert False
assert t.TypeVar('T').__bound__ is None
assert t.TypeVar('N', bound=None).__bound__ is None
assert t.NamedTuple('Empty', [])() == ()
check('bound', lambda: t.TypeVar('B', bound=int))
check('functional', lambda: t.NamedTuple('Filled', [('x', int)]))
try:
    class Empty(t.NamedTuple):
        print('empty-body')
except NameError as error:
    print('empty-finish', str(error))
else:
    assert False
try:
    class Filled(t.NamedTuple):
        print('filled-body')
        x: int
except NameError as error:
    print('annotation', str(error))
else:
    assert False
t.Generic = saved
print('export-delete', True)
