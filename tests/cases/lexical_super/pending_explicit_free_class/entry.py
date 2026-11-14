# ryupy: pending
# 完整保留 CP 的值校验；S1 对 canonical 明确 NI，不得伪报缺 cell。
import subject
assert subject.shadow_free_class()(None, lambda: 'shadow') == 'shadow'
try:
    subject.wrong_free_class()(None)
except RuntimeError as exc:
    assert str(exc) == 'super(): __class__ is not a type (int)'
else:
    assert False
print('pending explicit outer class free cell source')
