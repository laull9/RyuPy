# 直接exec失败不回滚、原缓存不动，旧逃逸函数仍保同一globals。
import sys
import control
import subject as original
import importlib.util as util
spec = util.find_spec('subject')
clone = util.module_from_spec(spec)
control.fail = True
try:
    spec.loader.exec_module(clone)
except RuntimeError as error:
    assert str(error) == 'body failed'
else:
    raise AssertionError('未抛正文异常')
assert clone.started and not hasattr(clone, 'finished')
assert sys.modules['subject'] is original
old = control.escaped[-1]
assert old.__globals__ is clone.__dict__
clone.value = 41
assert old() == 41 and original.read() == 17
control.fail = False
spec.loader.exec_module(clone)
assert clone.finished and old() == 17
assert clone.read is not old
print('公开直接失败与旧函数归属', True)
