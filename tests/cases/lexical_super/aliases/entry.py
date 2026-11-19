# 有真实自由 cell 的别名成功，无 cell 的别名给精确 RuntimeError。
import subject
obj = subject.Child()
assert obj.local() is obj
assert obj.global_with_cell() is obj
assert obj.expression() is obj
try:
    obj.global_without_cell()
except RuntimeError as exc:
    assert str(exc) == 'super(): __class__ cell not found'
else:
    assert False
print('local and global aliases use actual lexical cells')
