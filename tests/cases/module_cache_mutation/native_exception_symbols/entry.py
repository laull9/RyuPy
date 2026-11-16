# ryupy: pending
# 编译期原生异常识别不能把缓存替换后的自定义类误认成旧io类。
import sys
import io
original_io = io
original_error = io.UnsupportedOperation
import mc_replacement
sys.modules['io'] = mc_replacement
from io import UnsupportedOperation as Selected
import io as current
assert Selected is mc_replacement.UnsupportedOperation
assert Selected is not original_error and current is mc_replacement
try:
    raise Selected('chosen')
except Selected as exc:
    assert type(exc) is Selected and str(exc) == 'chosen'
else:
    raise AssertionError('selected exception must be caught')
def local_exception():
    from io import UnsupportedOperation as Local
    try:
        raise Local('local')
    except Local as exc:
        return type(exc) is mc_replacement.UnsupportedOperation
assert local_exception()
sys.modules['io'] = original_io
from io import UnsupportedOperation as Restored
assert Restored is original_error
print('native exception aliases keep runtime class identity', True)
