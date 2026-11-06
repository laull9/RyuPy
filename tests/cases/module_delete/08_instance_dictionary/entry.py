# ryupy: pending
# 仅 subject 选择实例模块；入口负责异常观察，subject 保持现有 AST 守卫的其它范围。
import subject
assert 'token' not in subject.__dict__
assert subject.saved_token == []
assert subject.read() is subject.value
saved = subject.value
subject.remove()
assert 'value' not in subject.__dict__ and not hasattr(subject, 'value')
for operation in (subject.read, subject.remove):
    try:
        operation()
    except NameError as error:
        assert str(error) == "name 'value' is not defined"
        print('instance deleted', type(error).__name__, str(error))
    else:
        assert False
assert saved == ['old']
subject.__dict__['value'] = ['new']
assert subject.read() is subject.__dict__['value']
marker = ['injected']
subject.__dict__['injected'] = marker
assert subject.read_injected() is marker
subject.remove_injected()
assert 'injected' not in subject.__dict__ and marker == ['injected']
assert subject.read_len() == 91
subject.remove_len()
assert 'len' not in subject.__dict__
assert subject.read_len() is len and subject.read_len()([1, 2]) == 2
try:
    subject.remove_len()
except NameError as error:
    assert str(error) == "name 'len' is not defined"
    print('instance builtin protected', type(error).__name__, str(error))
else:
    assert False
assert subject.read_len() is len
print('instance real dictionary deletion and recovery', subject.read())
