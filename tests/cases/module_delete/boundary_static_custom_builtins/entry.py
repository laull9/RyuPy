# ryupy: pending
# 默认静态模块自定义 builtins 仍是完整边界，不能靠扩大全局删除声称已支持。
import subject
assert subject.top_level == 2
assert subject.measure([1, 2, 3]) == 70
assert subject.measure.__builtins__ is subject.custom
assert subject.saved_len is len
try:
    subject.remove_again()
except NameError as error:
    assert str(error) == "name 'len' is not defined"
    print('custom builtin cannot be deleted', type(error).__name__, str(error))
else:
    assert False
assert subject.measure([1]) == 70
subject.custom['len'] = len
assert subject.measure([1, 2, 3]) == 3
assert subject.top_level == 2
print('frame and function builtin owners', subject.top_level, subject.measure([1, 2, 3]))
