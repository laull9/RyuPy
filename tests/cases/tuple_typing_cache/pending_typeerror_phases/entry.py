# TypeError正文重跑、其他异常原样传播、hash失败后不缓存，均保留完整CP行为。
import typing as t
log=[]
original_check=t._type_check
def failing(value,message):
    log.append('check')
    raise TypeError('body marker')
t._type_check=failing
try:
    t.Tuple[int,...]
except TypeError as e:
    assert str(e)=='body marker'
else:
    assert False
assert log==['check','check']
print('body-typeerror-retries-once',log)
log.clear()
def value_error(value,message):
    log.append('check')
    raise ValueError('value marker')
t._type_check=value_error
try:
    t.Tuple[str,...]
except ValueError as e:
    assert str(e)=='value marker'
else:
    assert False
assert log==['check']
print('other-error-not-retried',log)
t._type_check=original_check
class Key:
    def __hash__(self):
        log.append('hash')
        raise TypeError('unhashable marker')
k=Key()
log.clear()
a=t.Tuple[k]
b=t.Tuple[k]
assert a is not b and a.__args__[0] is k
assert log==['hash','hash']
print('hash-typeerror-uncached-once',log)
