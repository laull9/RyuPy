# ryupy: pending
# CP 的精确删除错误保留为将来扩展 oracle。
import subject
try:
    subject.Child().deleted()
except RuntimeError as exc:
    assert str(exc) == 'super(): arg[0] deleted'
else:
    assert False
print('pending deleted first argument')
