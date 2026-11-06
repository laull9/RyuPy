# ryupy: pending
# 独立默认模块窄例：只检查真实属性和from import，不声称支持默认模块__dict__。
import subject
import observer
original = subject.payload
assert observer.saved is original and observer.read() is original
subject.remove()
assert not hasattr(subject, 'payload')
try:
    observer.read()
except AttributeError as error:
    assert str(error) == "module 'subject' has no attribute 'payload'"
    print('external attribute missing', type(error).__name__, str(error))
else:
    assert False
try:
    from subject import payload
except ImportError as error:
    assert "cannot import name 'payload' from 'subject'" in str(error)
    print('external new import missing', type(error).__name__, 'payload missing')
else:
    assert False
assert observer.saved is original and observer.saved == ['old']
replacement = ['new']
subject.payload = replacement
assert observer.read() is replacement
assert observer.saved is original and observer.saved is not replacement
from subject import payload
assert payload is replacement
print('external attributes restore', observer.saved, payload)
