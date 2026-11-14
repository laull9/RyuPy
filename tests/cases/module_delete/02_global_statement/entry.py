# ryupy: pending
# 在调用方观察全局删除的错误相位，辅助模块正文不捕获异常。
import subject
assert subject.read() == 17
subject.remove()
assert subject.events == ['before', 'after']
for operation in (subject.read, subject.remove):
    try:
        operation()
    except NameError as error:
        assert str(error) == "name 'value' is not defined"
        print('global missing', type(error).__name__, str(error))
    else:
        assert False
assert subject.events == ['before', 'after', 'before']
subject.restore(23)
assert subject.read() == 23
subject.remove()
assert subject.events == ['before', 'after', 'before', 'before', 'after']
print('global restore and error phase', subject.events)

for operation, name in ((subject.remove_never_defined, 'never_defined'),
                        (subject.remove_partial, 'missing_middle')):
    try:
        operation()
    except NameError as error:
        assert str(error) == "name '" + name + "' is not defined"
        print('global partial or absent', type(error).__name__, str(error))
    else:
        assert False
assert not hasattr(subject, 'left') and subject.right == ['right']
print('global partial state', subject.right)
