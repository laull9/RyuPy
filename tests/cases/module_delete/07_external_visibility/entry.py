# ryupy: pending
# 模块删除对既有观察者与新 from import 可见，旧 from import 引用保持原身份。
import subject
import observer
original = subject.payload
assert observer.saved is original and observer.read() is original
subject.remove()
assert not hasattr(subject, 'payload')
assert 'payload' not in subject.__dict__
try:
    observer.read()
except AttributeError as error:
    assert str(error) == "module 'subject' has no attribute 'payload'"
    print('external lookup', type(error).__name__, str(error))
else:
    assert False
try:
    from subject import payload
except ImportError as error:
    assert "cannot import name 'payload' from 'subject'" in str(error)
    print('new import', type(error).__name__, 'payload missing')
else:
    assert False
assert observer.saved is original and observer.saved == ['old']
replacement = ['new']
subject.payload = replacement
assert observer.read() is replacement
assert subject.__dict__['payload'] is replacement
assert observer.saved is original and observer.saved is not replacement
from subject import payload
assert payload is replacement
print('external visibility and saved import', observer.saved, payload)
