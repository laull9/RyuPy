# 真正冷body的helper替换与TypeError二次调用；保留完整CP正例，不用NI代替。
import typing as t
log = []
T = t.TypeVar('Warm')
a = t.List[T]
original = t._type_check
def checking(value, message):
    log.append('type-check')
    return original(value, message)
t._type_check = checking
assert t.List[T] is a and log == []
t.List[t.TypeVar('Cold')]
print('cold-type-check', log)
t._type_check = original
original_collect = t._collect_parameters
def collecting(args):
    log.append('collect')
    return original_collect(args)
t._collect_parameters = collecting
log.clear()
assert t.List[T] is a and log == []
t.List[t.TypeVar('Collected')]
print('cold-collection', log)
t._collect_parameters = original_collect
original_check = t._check_generic
def failing(*args):
    log.append('arity-helper')
    raise TypeError('helper marker')
t._check_generic = failing
log.clear()
try:
    t.List[t.TypeVar('Failure')]
except TypeError as error:
    print('body-type-error-fallback', str(error), log)
else:
    assert False
t._check_generic = original_check
