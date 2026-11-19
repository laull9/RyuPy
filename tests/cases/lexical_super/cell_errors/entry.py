# 先缺参数再缺类 cell；同名 own cell 不能伪作 free cell。
import subject
obj = subject.Child()
for call in [obj.local, lambda: obj.parameter(subject.Base), obj.global_value,
             obj.own_cell, lambda: subject.missing_cell(None)]:
    try:
        call()
    except RuntimeError as exc:
        assert str(exc) == 'super(): __class__ cell not found'
    else:
        assert False
try:
    subject.no_arguments()
except RuntimeError as exc:
    assert str(exc) == 'super(): no arguments'
else:
    assert False
print('missing arguments and missing real free cell')
