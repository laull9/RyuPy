# ryupy: pending
# 更深完整合同：旧函数与默认值仍归属旧模块字典，不能转向新静态槽。
import sys
import mc_subject
old = mc_subject
old_read = old.read
old_store = old.store
assert old_read() == 1 and old_store('old') == ['old']
del sys.modules['mc_subject']
import mc_subject as fresh
assert fresh is not old and fresh.read is not old_read
assert fresh.read() == 2 and old_read() == 1
assert fresh.store('new') == ['new'] and old_store('again') == ['old', 'again']
old.value = 71
fresh.value = 82
assert old_read() == 71 and fresh.read() == 82
assert old_read.__globals__ is old.__dict__
assert fresh.read.__globals__ is fresh.__dict__
print('delete keeps escaped owners and defaults', True)
