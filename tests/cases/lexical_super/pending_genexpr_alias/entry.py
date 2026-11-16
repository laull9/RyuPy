# ryupy: pending
# 真实自由类 cell 已转发，但迭代器首参不符合该类。
import subject
try:
    next(subject.Child().values())
except TypeError as exc:
    assert str(exc) == 'super(type, obj): obj must be an instance or subtype of type'
else:
    assert False
print('pending genexpr alias cannot borrow outer frame')
